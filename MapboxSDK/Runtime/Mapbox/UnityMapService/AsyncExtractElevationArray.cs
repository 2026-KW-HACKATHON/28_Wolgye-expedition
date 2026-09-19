using System;
using Mapbox.BaseModule.Data.DataFetchers;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Rendering;
using TerrainData = Mapbox.BaseModule.Data.DataFetchers.TerrainData;

namespace Mapbox.UnityMapService
{
	/// <summary>
	/// Asynchronous terrain-RGB -> float[] elevation decoder using
	/// <c>AsyncGPUReadback</c>. The GPU readback callback fires on the main thread with a
	/// zero-copy <c>NativeArray&lt;Color32&gt;</c>; the decode loop runs there inside a
	/// Burst-compiled <see cref="IJob"/> via <c>Run()</c>.
	/// </summary>
	public class AsyncExtractElevationArray : IElevationDataExtractionStrategy
	{
		public void ExtractHeightData(Texture2D texture, Action<float[]> callback = null)
		{
			AsyncGPUReadback.Request(texture, 0, (t) =>
			{
				var width = t.width;
				var data = t.GetData<Color32>();
				// Fresh allocation, not pool-rented: this overload hands the buffer to
				// external code with no return contract. Pool-renting here would drain
				// the pool over the session as callers never call Return.
				var heightData = new float[width * width];
				RunDecodeJob(data, width, heightData, out _, out _);
				callback?.Invoke(heightData);
			});
		}

		public void ExtractHeightData(TerrainData terrainData)
		{
			ExtractHeightData(terrainData.Texture, terrainData);
		}

		public void ExtractHeightData(Texture2D texture, TerrainData terrainData)
		{
			AsyncGPUReadback.Request(texture, 0, (t) =>
			{
				// AsyncGPUReadback fires on the main thread, but possibly after the
				// owning TerrainData was disposed (cache eviction during fetch).
				// Guard before renting from the pool: assigning to disposed data would
				// leak the rented buffer and could corrupt a future Rent if the same
				// TerrainData later gets re-used.
				if (terrainData == null || terrainData.IsDisposed)
				{
					return;
				}
				var width = t.width;
				var data = t.GetData<Color32>();
				var heightData = ElevationArrayPool.Rent(width * width);
				RunDecodeJob(data, width, heightData, out var min, out var max);
				// Re-check after the (potentially long) decode: dispose may have arrived
				// in the interim. Return the buffer rather than assigning into the dead
				// data object.
				if (terrainData.IsDisposed)
				{
					ElevationArrayPool.Return(heightData);
					return;
				}
				terrainData.SetElevationValues(heightData, min, max);
			});
		}

		private static void RunDecodeJob(NativeArray<Color32> data, int width, float[] heightData, out float min, out float max)
		{
			var length = width * width;
			var heightNative = new NativeArray<float>(length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
			var minMax = new NativeArray<float>(2, Allocator.TempJob);
			new DecodeTerrainColor32Job
			{
				Data = data,
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
		/// Burst-compiled decode for the async path. Mapbox terrain-RGB in the Color32
		/// readback is stored as <c>(A, R, G, B)</c> in <c>(r, g, b, a)</c> positions
		/// (historical swizzle preserved from the pre-Burst implementation). Min/max are
		/// accumulated inline.
		/// </summary>
		[BurstCompile]
		private struct DecodeTerrainColor32Job : IJob
		{
			[ReadOnly] public NativeArray<Color32> Data;
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
						var c = Data[idx];
						float r = c.g;
						float g = c.b;
						float b = c.a;
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
