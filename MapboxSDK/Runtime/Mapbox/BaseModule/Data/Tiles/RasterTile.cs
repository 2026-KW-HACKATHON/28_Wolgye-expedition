//-----------------------------------------------------------------------
// <copyright file="RasterTile.cs" company="Mapbox">
//     Copyright (c) 2016 Mapbox. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using Mapbox.BaseModule.Data.DataFetchers;
using Mapbox.BaseModule.Data.Platform;
using UnityEngine;
using UnityEngine.Networking;

namespace Mapbox.BaseModule.Data.Tiles
{
	/// <summary>
	/// A raster tile from the Mapbox Style API, an encoded image representing a geographic
	/// bounding box. Usually JPEG or PNG encoded.
	/// </summary>
	/// <example>
	/// Making a RasterTile request:
	/// <code>
	/// var parameters = new Tile.Parameters();
	/// parameters.Fs = MapboxAccess.Instance;
	/// parameters.Id = new CanonicalTileId(_zoom, _tileCoorindateX, _tileCoordinateY);
	/// parameters.TilesetId = "mapbox://styles/mapbox/satellite-v9";
	/// var rasterTile = new RasterTile();
	///
	/// // Make the request.
	/// rasterTile.Initialize(parameters, (Action)(() =>
	/// {
	/// 	if (!string.IsNullOrEmpty(rasterTile.Error))
	/// 	{
	///			// Handle the error.
	///		}
	///
	/// 	// Consume the <see cref="Data"/>.
	///	}));
	/// </code>
	/// </example>
	public class RasterTile : Tile
	{
		private byte[] data;

		/// <summary> Gets the raster tile raw data. This field is only used if texture is fetched/stored as byte array. Otherwise, if it's fetched as texture, you should use Texture2D.</summary>
		/// <value> The raw data, usually an encoded JPEG or PNG. </value>
		/// <example>
		/// Consuming data in Unity to create a Texture2D:
		/// <code>
		/// var texture = new Texture2D(0, 0);
		/// texture.LoadImage(rasterTile.Data);
		/// _sampleMaterial.mainTexture = texture;
		/// </code>
		/// </example>
		public byte[] Data
		{
			get { return this.data; }
		}

		/// <summary> Gets the imagery as Texture2d object. This field is only used if texture is fetched/stored as Texture2d. Otherwise, if it's fetched as byte array, you should use Data. </summary>
		/// <value> The raw data, usually an encoded JPEG or PNG. </value>
		/// <example>
		/// <code>
		/// _sampleMaterial.mainTexture = rasterTile.Texture2D;
		/// </code>
		/// </example>
		public Texture2D Texture2D { get; private set; }

		public bool IsTextureNonreadable;
		public bool IsBackgroundData = false;
		public RasterTile()
		{

		}

		public RasterTile(CanonicalTileId tileId, string tilesetId, bool useNonReadableTexture) : base(tileId, tilesetId)
		{
			IsTextureNonreadable = useNonReadableTexture;
		}

		protected internal override void DoTheRequest(IFileSource fileSource)
		{
			_webRequest = fileSource.MapboxImageRequest(_generatedUrl, HandleTileResponse, ETag, 10, IsTextureNonreadable);
		}

		protected void HandleTileResponse(WebRequestResponse webRequestResponse)
		{
			//this is a callback and after this chain, unity web request will be aborted
			//and disposed as it'll hit the end of the using block in CachingWebFileSource

			if (webRequestResponse.Result == WebResponseResult.Success)
			{
				StatusCode = webRequestResponse.StatusCode;
				data = webRequestResponse.Data;
				ETag = webRequestResponse.ETag;
				ExpirationDate = webRequestResponse.ExpirationDate;

				// Cancelled is not the same as loaded!
				if (TileState != TileState.Canceled)
				{
					TileState = TileState.Loaded;
				}
			}
			else if (webRequestResponse.Result == WebResponseResult.Cancelled)
			{
				TileState = TileState.Canceled;
				AddLog(string.Format("{0} - {1}", Time.unscaledTime, " tile cancelled"));
			}
			else if (webRequestResponse.Result == WebResponseResult.Retrying)
			{
				TileState = TileState.Loading;
				AddLog("Retry " + _webRequest?.TryCount);
				return;
			}
			else if (webRequestResponse.Result == WebResponseResult.Failed)
			{
				TileState = TileState.Errored;
				AddLog(string.Format("{0} - {1}", Time.unscaledTime, " tile errored"));
			}
			else if (webRequestResponse.Result == WebResponseResult.NoData)
			{
				TileState = TileState.Loaded;
			}
			
			AddLog(string.Format("{0} - {1}", Time.unscaledTime, " tile finished"));
			_callback(new DataFetchingResult(this, webRequestResponse));
			_webRequest = null;
			//have to null the unity request AFTER the callback as texture itself is kept
			//in the request object and request object should be kept until that's done.
			//we need to null the unity request after we are done with it though because
			//if we don't, Request.Abort line in Tile.Cancel will pop nonsense errors
			//because obviously you cannot call abort on a disposed object. It's disposed
			//as we are using `using` for webrequest objects which disposes objects in the end.
			//anyway if it's disposed but not null, `Tile.Cancel` will try to Abort() it and
			//Unity will go crazy because Unity is like that sometimes.
		}

		protected override TileResource MakeTileResource(string tilesetId)
		{
			return TileResource.MakeRaster(Id, tilesetId);
		}

		public override void Clear()
		{
			base.Clear();
			//clearing references for simplicity. It doesn't really block GC but it's clearer this way
			data = null;
			Texture2D = null;
		}

		public void SetTextureFromCache(Texture2D texture)
		{
			Texture2D = texture;
			TileState = TileState.Loaded;
		}

		public void ExtractTextureFromRequest()
		{
			if (_webRequest != null)
			{
				Texture2D = DownloadHandlerTexture.GetContent(_webRequest.Core);
				if (Texture2D != null)
				{
					Texture2D.wrapMode = TextureWrapMode.Clamp;
#if UNITY_EDITOR
					Texture2D.name = string.Format("{0}_{1}", Id.ToString(), TilesetId);
#endif
				}
				else
				{
					Debug.Log("here");
				}
			}
		}
	}
}
