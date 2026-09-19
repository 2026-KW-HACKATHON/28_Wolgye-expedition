//-----------------------------------------------------------------------
// <copyright file="Tile.cs" company="Mapbox">
//     Copyright (c) 2016 Mapbox. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Mapbox.BaseModule.Data.DataFetchers;
using Mapbox.BaseModule.Data.Platform;
using Mapbox.BaseModule.Data.Platform.Cache;
using UnityEngine;

namespace Mapbox.BaseModule.Data.Tiles
{
	public enum CacheType
	{
		MemoryCache,
		FileCache,
		SqliteCache,
		NoCache,
		NoCacheUpdated
	}

	public enum TileState
	{
		New,/// <summary> New tile, not yet initialized. </summary>
		Loading,/// <summary> Loading data. </summary>
		Loaded,/// <summary> Data loaded and parsed. </summary>
		Canceled,/// <summary> Data loading cancelled. </summary>
		Errored, /// <summary> Data loading errored. </summary>
		Updated,/// <summary> Data has been loaded before and got updated. </summary>
		Processing,
		Destroyed
	}

	/// <summary>
	///    A Map tile, a square with vector or raster data representing a geographic
	///    bounding box. More info <see href="https://en.wikipedia.org/wiki/Tiled_web_map">
	///    here </see>.
	/// </summary>
	[Serializable]
	public abstract class Tile : IAsyncRequest
	{
		public CanonicalTileId Id;
		public string TilesetId;
		public DateTime ExpirationDate;
		public string ETag;
		public long StatusCode;
		public CacheType FromCache = CacheType.NoCache;
		
		protected TileState TileState = TileState.New;
		protected string _generatedUrl; 
		protected IAsyncRequest _request;
		protected IWebRequest _webRequest;
		protected Action<DataFetchingResult> _callback;
		protected List<string> _logs;

		/// <summary>
		///     Gets the current state. When fully loaded, you must
		///     check if the data actually arrived and if the tile
		///     is accusing any error.
		/// </summary>
		/// <value> The tile state. </value>
		public TileState CurrentTileState => TileState;
		public HttpRequestType RequestType => _request.RequestType;
		public bool IsCompleted => TileState == TileState.Loaded;
		
		
		protected Tile()
		{

		}

		protected Tile(CanonicalTileId tileId, string tilesetId)
		{
			TilesetId = tilesetId;
			Id = tileId;
#if DEBUG
			_logs = new List<string>();
			AddLog(string.Format("{0} - {1}", Time.unscaledTime, " tile created"));
#endif
		}
		
		public virtual void Initialize(IFileSource fileSource, Action<DataFetchingResult> p)
		{
			AddLog(string.Format("{0} - {1}", Time.unscaledTime, " tile initialized"));
			TileState = TileState.Loading;
			_callback = p;

			_generatedUrl = MakeTileResource(TilesetId).GetUrl();
			DoTheRequest(fileSource);
		}

		protected internal abstract void DoTheRequest(IFileSource fileSource);

		/// <summary>
		///     Cancels the request for the <see cref="T:Mapbox.BaseModule.Data.Tiles.Tile"/> object.
		///     It will stop a network request and set the tile's state to Canceled.
		/// </summary>
		/// <example>
		/// <code>
		/// // Do not request tiles that we are already requesting
		///	// but at the same time exclude the ones we don't need
		///	// anymore, cancelling the network request.
		///	tiles.RemoveWhere((T tile) =>
		///	{
		///		if (cover.Remove(tile.Id))
		///		{
		///			return false;
		///		}
		///		else
		///		{
		///			tile.Cancel();
		///			NotifyNext(tile);
		///			return true;
		/// 	}
		///	});
		/// </code>
		/// </example>
		public virtual void Cancel()
		{
			if (_request != null)
			{
				_request.Cancel();
				_request = null;
			}

			if (_webRequest != null)
			{
				_webRequest.Abort();
				_webRequest = null;
			}

			TileState = TileState.Canceled;
			AddLog(string.Format("{0} - {1}", Time.unscaledTime, " state cancelled with cancel call"));
		}

		// Get the tile resource (raster/vector/etc).
		protected abstract TileResource MakeTileResource(string tilesetId);

		public virtual void Clear()
		{
			if (_request != null)
			{
				_request.Cancel();
				_request = null;
			}

			if (_webRequest != null)
			{
				_webRequest.Abort();
				_webRequest = null;
			}
		}
		
#region Logs
		public List<string> GetLogs => _logs;
		public void AddLog(string text)
		{
	#if DEBUG
			_logs.Add(text);
	#endif
		}

		public void AddLog(string text, CanonicalTileId relatedTileId)
		{
	#if DEBUG
			_logs.Add(string.Format("{0} - {1}", text, relatedTileId));
	#endif
		}
#endregion

	}
}