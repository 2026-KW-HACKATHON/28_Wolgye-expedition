using System;
using System.Collections;
using System.Collections.Generic;
using Mapbox.BaseModule.Data.DataFetchers;
using Mapbox.BaseModule.Map;
using Mapbox.BaseModule.Unity;
using Mapbox.BaseModule.Utilities;
using UnityEngine;
using TerrainData = Mapbox.BaseModule.Data.DataFetchers.TerrainData;

namespace Mapbox.ImageModule.Terrain
{
	/// <summary>
	/// Watches the visualizer's tile lifecycle events and keeps
	/// <see cref="IMapInformation.Terrain"/> Min/MaxElevation in sync with the
	/// observed elevation range across loaded tiles.
	///
	/// Owned by <see cref="TerrainLayerModule"/> and constructed when the module
	/// is attached to a visualizer. The visualizer itself doesn't know this exists.
	///
	/// Three lifecycle hooks feed dirty-flag updates:
	/// <list type="bullet">
	/// <item><c>TileLoaded</c>: data may be ready (mark dirty) or shader-mode pending
	///   CPU decode (subscribe a one-shot widening to <c>ElevationValuesUpdated</c>).</item>
	/// <item><c>TileUnloading</c>: if the unloading tile held a boundary value, or is
	///   the last contributor, mark dirty so the next drain re-derives from scratch.</item>
	/// <item>Per-frame drain: a coroutine recomputes from <c>ActiveTiles</c> when the
	///   flag is set — one O(N) pass per frame regardless of event volume.</item>
	/// </list>
	/// </summary>
	internal sealed class TerrainBoundsTracker : IDisposable
	{
		private readonly ITileLifecycleSource _source;

		// One-shot widening subscriptions for shader-mode tiles whose CPU decode
		// hasn't arrived yet. Stored by (data, handler) so Dispose can detach the
		// closures and so the dedup loop can avoid attaching twice for the same
		// shared data tile.
		private readonly List<(TerrainData data, Action handler)> _pendingWatches =
			new List<(TerrainData, Action)>();

		private bool _dirty;
		private bool _disposed;
		private Coroutine _drainCoroutine;

		public TerrainBoundsTracker(ITileLifecycleSource source)
		{
			_source = source;
			_source.TileLoaded += OnTileLoaded;
			_source.TileUnloading += OnTileUnloading;
			if (Runnable.Instance != null)
			{
				_drainCoroutine = Runnable.Instance.StartCoroutine(DrainLoop());
			}
			else
			{
				// Should never happen in normal play (Runnable is a global singleton).
				// Fail loud so it's obvious if the construction order ever changes.
				Debug.LogWarning("TerrainBoundsTracker: Runnable.Instance is null at construction; bounds recompute coroutine not started. Terrain.Min/MaxElevation will not update.");
			}
		}

		public void Dispose()
		{
			if (_disposed) return;
			_disposed = true;

			if (_source != null)
			{
				_source.TileLoaded -= OnTileLoaded;
				_source.TileUnloading -= OnTileUnloading;
			}

			// Detach pending watches. Both closures dropped — the dispose callback
			// keeps a Dictionary reference alive otherwise.
			for (int i = 0; i < _pendingWatches.Count; i++)
			{
				var (data, handler) = _pendingWatches[i];
				if (data != null && handler != null)
				{
					data.ElevationValuesUpdated -= handler;
				}
			}
			_pendingWatches.Clear();

			if (_drainCoroutine != null && Runnable.Instance != null)
			{
				Runnable.Instance.StopCoroutine(_drainCoroutine);
				_drainCoroutine = null;
			}
		}

		private IEnumerator DrainLoop()
		{
			while (!_disposed)
			{
				if (_dirty)
				{
					Recompute();
					_dirty = false;
				}
				yield return null;
			}
		}

		private void OnTileLoaded(UnityMapTile tile)
		{
			if (_disposed) return;
			var terrainData = tile.TerrainContainer?.TerrainData;
			if (terrainData == null) return;

			if (terrainData.IsElevationDataReady)
			{
				// Data already decoded — mark dirty for the next drain tick.
				_dirty = true;
			}
			else
			{
				// Shader-mode tiles finish on texture-ready, before the CPU decode arrives.
				// Hook a one-shot watch so the bounds widen when the decode lands.
				WatchForAsyncDecode(terrainData);
			}
		}

		private void OnTileUnloading(UnityMapTile tile)
		{
			if (_disposed) return;

			// Snapshot the pooled tile's contribution to the current bounds. Tile.Recycle()
			// (called by PoolTile after this event fires) clears TerrainData, so we have to
			// check up front. Skip when both endpoints are 0 — the tile is flat and didn't
			// actually contribute, so a recompute would just produce the same values.
			var terrain = _source.MapInformation.Terrain;
			var terrainData = tile.TerrainContainer?.TerrainData;
			// Exact equality: terrain.MaxElevation was assigned directly from some tile's
			// MaxElevation in the previous Recompute, so `==` matches when this tile is
			// that contributor. Mathf.Approximately's magnitude-scaled tolerance would
			// also accept near-matches from sibling tiles, triggering redundant recomputes.
			bool contributedMax = terrainData != null && terrainData.IsElevationDataReady &&
				terrainData.MaxElevation != 0f &&
				terrainData.MaxElevation == terrain.MaxElevation;
			bool contributedMin = terrainData != null && terrainData.IsElevationDataReady &&
				terrainData.MinElevation != 0f &&
				terrainData.MinElevation == terrain.MinElevation;

			// Also dirty when this is the last tile being pooled — an all-flat session
			// (every loaded tile had Min=Max=0, or no terrain data at all) wouldn't trip
			// the non-zero checks above, so the empty-fallback (reset to TerrainInfo
			// defaults) would never run when the map empties out. ActiveTiles.Count == 1
			// here because TileUnloading fires before the visualizer's ActiveTiles.Remove.
			// No terrainData requirement: shader-only tiles count too.
			var activeTiles = _source.ActiveTiles;
			bool wasLastActive = activeTiles.Count == 1 &&
				activeTiles.ContainsKey(tile.UnwrappedTileId);

			if (contributedMax || contributedMin || wasLastActive)
			{
				_dirty = true;
			}
		}

		private void WatchForAsyncDecode(TerrainData data)
		{
			// De-dup: shared TerrainData is referenced by up to 16 render tiles (data zoom
			// = render zoom − 2). One watch per data is enough — the dirty flag fires once.
			for (int i = 0; i < _pendingWatches.Count; i++)
			{
				if (ReferenceEquals(_pendingWatches[i].data, data)) return;
			}

			Action handler = null;
			Action onDispose = null;
			handler = () =>
			{
				data.ElevationValuesUpdated -= handler;
				data.RemoveDisposeCallback(onDispose);
				RemoveWatchEntry(data, handler);
				if (!_disposed) _dirty = true;
			};
			// TerrainData.Dispose doesn't fire ElevationValuesUpdated, so without this hook
			// the watch entry would leak (rooting both data and the closure) until our own
			// Dispose. Eviction during the decode window is the trigger.
			onDispose = () =>
			{
				data.ElevationValuesUpdated -= handler;
				data.RemoveDisposeCallback(onDispose);
				RemoveWatchEntry(data, handler);
			};

			_pendingWatches.Add((data, handler));
			data.ElevationValuesUpdated += handler;
			data.AddDisposeCallback(onDispose);
		}

		private void RemoveWatchEntry(TerrainData data, Action handler)
		{
			for (int i = _pendingWatches.Count - 1; i >= 0; i--)
			{
				if (ReferenceEquals(_pendingWatches[i].data, data) &&
				    ReferenceEquals(_pendingWatches[i].handler, handler))
				{
					_pendingWatches.RemoveAt(i);
					break;
				}
			}
		}

		private void Recompute()
		{
			var terrain = _source.MapInformation.Terrain;
			float max = 0f;
			float min = 0f;
			bool any = false;
			foreach (var kv in _source.ActiveTiles)
			{
				var td = kv.Value.TerrainContainer?.TerrainData;
				if (td == null || !td.IsElevationDataReady) continue;
				if (!any)
				{
					max = td.MaxElevation;
					min = td.MinElevation;
					any = true;
				}
				else
				{
					if (td.MaxElevation > max) max = td.MaxElevation;
					if (td.MinElevation < min) min = td.MinElevation;
				}
			}
			// When no tile is contributing, fall back to TerrainInfo's conservative defaults
			// rather than 0 — a flat AABB would frustum-cull plausible mountains on the next
			// frame before any sample arrives.
			terrain.MaxElevation = any ? max : TerrainInfo.DefaultMaxElevation;
			terrain.MinElevation = any ? min : TerrainInfo.DefaultMinElevation;
		}
	}
}
