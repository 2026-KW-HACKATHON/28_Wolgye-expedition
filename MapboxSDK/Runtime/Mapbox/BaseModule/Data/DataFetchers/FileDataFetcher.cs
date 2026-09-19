using System;
using System.Collections;
using Mapbox.BaseModule.Data.Platform;
using Mapbox.BaseModule.Data.Tiles;
using Mapbox.BaseModule.Utilities;
using UnityEngine;
using UnityEngine.Networking;

namespace Mapbox.BaseModule.Data.DataFetchers
{
	public class FileDataFetcher
	{
		//TODO keep track of all fetchFile coroutines and cancel them on destroy/app stopped
		public void FetchData<T>(string filePath, CanonicalTileId tileId, string tilesetId, bool isTextureNonreadable, Action<T> callback) where T : RasterData, new()
		{
			var webRequest = UnityWebRequestTexture.GetTexture(filePath, isTextureNonreadable);
			Runnable.Run(FetchTextureFromFile(webRequest, (r) =>
			{
				var rasterData = new T()
				{
					TileId = tileId,
					TilesetId = tilesetId,
					Texture = DownloadHandlerTexture.GetContent(webRequest),
					CacheType = CacheType.FileCache
				};
				if (rasterData.Texture != null)
				{
					rasterData.Texture.wrapMode = TextureWrapMode.Clamp;
#if UNITY_EDITOR
					rasterData.Texture.name = string.Format("{0}_{1}", tileId.ToString(), tilesetId);
#endif
				}
				callback(rasterData);
			}));
		}
		
		public IEnumerator FetchDataCoroutine<T>(string filePath, CanonicalTileId tileId, string tilesetId, bool isTextureNonreadable, Action<T> callback) where T : RasterData, new()
		{
			var webRequest = UnityWebRequestTexture.GetTexture(filePath, isTextureNonreadable);
			yield return FetchTextureFromFile(webRequest, (r) =>
			{
				var rasterData = new T()
				{
					TileId = tileId,
					TilesetId = tilesetId,
					Texture = DownloadHandlerTexture.GetContent(webRequest),
					CacheType = CacheType.FileCache
				};

				if (rasterData.Texture != null)
				{
					rasterData.Texture.wrapMode = TextureWrapMode.Clamp;
#if UNITY_EDITOR
					rasterData.Texture.name = string.Format("{0}_{1}", tileId.ToString(), tilesetId);
#endif
				}
				callback(rasterData);
			});
		}
		
		private IEnumerator FetchTextureFromFile(UnityWebRequest webRequest, Action<WebRequestResponse> callback)
		{
			using (webRequest)
			{
				var response = new WebRequestResponse();
				yield return webRequest.SendWebRequest();
				
				#if UNITY_EDITOR
				//need this for tests
				while (webRequest.result == UnityWebRequest.Result.InProgress) yield return null;
				#endif
				
				if (webRequest != null &&
				    webRequest.result == UnityWebRequest.Result.ConnectionError ||
				    webRequest.result == UnityWebRequest.Result.ProtocolError)
				{
					//Debug.Log(webRequest.error);
					response.Result = WebResponseResult.Failed;
					response.AddException(new Exception(webRequest.error));
				}
				else
				{
					response.Result = WebResponseResult.Success;
					response.StatusCode = webRequest.responseCode;
					response.Data = webRequest.downloadHandler.data;
				}

				callback(response);
			}
		}
	}
}
