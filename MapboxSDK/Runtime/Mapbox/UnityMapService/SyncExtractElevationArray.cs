using System;
using Mapbox.BaseModule.Data.DataFetchers;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using TerrainData = Mapbox.BaseModule.Data.DataFetchers.TerrainData;

namespace Mapbox.UnityMapService
{
	/// <summary>
	/// Synchronous terrain-RGB -> float[] elevation decoder. Used on platforms that don't
	/// support <c>AsyncGPUReadback</c>. The decode loop runs inside a Burst-compiled
	/// <see cref="IJob"/> via <c>Run()</c> — main-thread execution preserves the sync
	/// contract, but the hot loop is SIMD-vectorized native code rather than managed.
	/// </summary>
	public class SyncExtractElevationArray : IElevationDataExtractionStrategy
	{
		public void ExtractHeightData(Texture2D texture, Action<float[]> callback)
		{
			var rgbData = texture.GetRawTextureData<byte>();
			var width = texture.width;
			// Fresh allocation, not pool-rented: this overload hands the buffer to
			// external code with no return contract. Pool-renting here would drain
			// the pool over the session as callers never call Return.
			var heightData = new float[width * width];
			RunDecodeJob(rgbData, width, heightData, out _, out _);
			callback?.Invoke(heightData);
		}

		public void ExtractHeightData(TerrainData terrainData)
		{
			ExtractHeightData(terrainData.Texture, terrainData);
		}

		public void ExtractHeightData(Texture2D texture, TerrainData terrainData)
		{
			// Even the sync path can be called on a disposed TerrainData from cache-fetch
			// race paths. Skip + don't rent if already disposed; otherwise we'd
			// SetElevationValues on a dead object and leak the rented buffer.
			if (terrainData == null || terrainData.IsDisposed)
			{
				return;
			}
			var rgbData = texture.GetRawTextureData<byte>();
			var width = texture.width;
			var heightData = ElevationArrayPool.Rent(width * width);
			RunDecodeJob(rgbData, width, heightData, out var min, out var max);
			if (terrainData.IsDisposed)
			{
				ElevationArrayPool.Return(heightData);
				return;
			}
			terrainData.SetElevationValues(heightData, min, max);
		}

		// Copies the Burst-decoded result (NativeArray) back into the pooled managed array
		// our TerrainData API still uses. The NativeArray lives only for the job's
		// lifetime; one temp alloc + memcpy per tile extraction. The job itself computes
		// min/max in the same pass so we don't re-iterate on the C# side.
		private static void RunDecodeJob(NativeArray<byte> rgbData, int width, float[] heightData, out float min, out float max)
		{
			var length = width * width;
			var heightNative = new NativeArray<float>(length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
			var minMax = new NativeArray<float>(2, Allocator.TempJob);
			new DecodeTerrainRgbJob
			{
				RgbData = rgbData,
				Width = width,
				HeightData = heightNative,
				MinMax = minMax
			}.Run();
			heightNative.CopyTo(heightData);
			min = minMax[0];
			max = minMax[1];
			heightNative.Dispose();
			minMax.Dispose();
		}

		/// <summary>
		/// Burst-compiled decode of Mapbox terrain-RGB into linear elevation (meters).
		/// Four-byte RGBA layout, formula <c>h = -10000 + (r*65536 + g*256 + b) * 0.1</c>.
		/// Min/max are accumulated in the same pass to save a second iteration.
		/// </summary>
		[BurstCompile]
		private struct DecodeTerrainRgbJob : IJob
		{
			[ReadOnly] public NativeArray<byte> RgbData;
			public int Width;
			public NativeArray<float> HeightData;
			public NativeArray<float> MinMax;

			public void Execute()
			{
				var min = float.MaxValue;
				var max = float.MinValue;
				int idx = 0;
				for (int y = 0; y < Width; y++)
				{
					for (int x = 0; x < Width; x++, idx++)
					{
						int rgbIdx = idx * 4;
						float r = RgbData[rgbIdx + 1];
						float g = RgbData[rgbIdx + 2];
						float b = RgbData[rgbIdx + 3];
						var value = -10000f + (r * 65536f + g * 256f + b) * 0.1f;
						HeightData[idx] = value;
						if (value < min) min = value;
						if (value > max) max = value;
					}
				}
				MinMax[0] = min;
				MinMax[1] = max;
			}
		}
	}
}
