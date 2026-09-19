//-----------------------------------------------------------------------
// <copyright file="VectorTile.cs" company="Mapbox">
//     Copyright (c) 2016 Mapbox. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.ObjectModel;
using Mapbox.BaseModule.Data.DataFetchers;
using Mapbox.BaseModule.Data.Platform;
using Mapbox.VectorTile;
using UnityEngine;

namespace Mapbox.BaseModule.Data.Tiles
{
	/// <summary>
	///    A decoded vector tile, as specified by the
	///    <see href="https://www.mapbox.com/vector-tiles/specification/">
	///    Mapbox Vector Tile specification</see>.
	///    See available layers and features <see href="https://www.mapbox.com/vector-tiles/mapbox-streets-v7/">here</see>.
	///    The tile might be incomplete if the network request and parsing are still pending.
	/// </summary>
	///  <example>
	/// Making a VectorTile request:
	/// <code>
	/// var parameters = new Tile.Parameters();
	/// parameters.Fs = MapboxAccess.Instance;
	/// parameters.Id = new CanonicalTileId(_zoom, _tileCoorindateX, _tileCoordinateY);
	/// parameters.TilesetId = "mapbox.mapbox-streets-v7";
	/// var vectorTile = new VectorTile();
	///
	/// // Make the request.
	/// vectorTile.Initialize(parameters, (Action)(() =>
	/// {
	/// 	if (!string.IsNullOrEmpty(vectorTile.Error))
	/// 	{
	///			// Handle the error.
	///		}
	///
	/// 	// Consume the <see cref="Data"/>.
	///	}));
	/// </code>
	/// </example>
	public abstract class ByteArrayTile : Tile
	{
		
		public Action<bool> DataProcessingFinished = (success) => { };
		// FIXME: Namespace here is very confusing and conflicts (sematically)
		// with his class. Something has to be renamed here.
		private VectorTile data;

		protected bool _isStyleOptimized = false;

		protected string _optimizedStyleId;

		protected string _modifiedDate;

		private bool isDisposed = false;

		private byte[] byteData;
		//private TaskWrapper _task;

		public byte[] ByteData
		{
			get { return this.byteData; }
		}

		/// <summary> Gets the vector decoded using Mapbox.VectorTile library. </summary>
		/// <value> The GeoJson data. </value>
		public VectorTile Data
		{
			get
			{
				return this.data;
			}
			set { this.data = value; }
		}

		public ByteArrayTile()
		{
			_isStyleOptimized = false;
		}

		public ByteArrayTile(CanonicalTileId tileId, string tilesetId) : base(tileId, tilesetId)
		{
		}

		public ByteArrayTile(CanonicalTileId tileId, string tilesetId, string styleId, string modifiedDate) : base(tileId, tilesetId)
		{
			if (string.IsNullOrEmpty(styleId) || string.IsNullOrEmpty(modifiedDate))
			{
				UnityEngine.Debug.LogWarning("Style Id or Modified Time cannot be empty for style optimized tilesets. Switching to regular tilesets!");
				_isStyleOptimized = false;
			}
			else
			{
				_isStyleOptimized = true;
				_optimizedStyleId = styleId;
				_modifiedDate = modifiedDate;
			}
		}

		protected internal override void DoTheRequest(IFileSource fileSource)
		{
			_webRequest = fileSource.MapboxDataRequest(_generatedUrl, HandleTileResponse, ETag, 2);
		}

		public override void Cancel()
		{
			base.Cancel();
			// if (_task != null)
			// {
			// 	MapboxAccess.Instance.TaskManager.CancelTask(_task);
			// }
		}

		private void HandleTileResponse(WebRequestResponse webRequestResponse)
		{
			//unlike rastertile, we are able to null this field at the beginning of this method
			//as the data we need is stored in the response anyway and we are pretty much done with
			//request object.
			//to understand why we need to null the request object,
			//please check RasterTile.cs HandleTileResponse


			//callback has to be called here
			//otherwise requests are never complete (success or failure) and pipeline gets blocked
			
			if (webRequestResponse.Result == WebResponseResult.Success ||
			    webRequestResponse.Result == WebResponseResult.NoData)
			{
				StatusCode = webRequestResponse.StatusCode;
				byteData = webRequestResponse.Data;
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
			
			AddLog(string.Format("{0} - {1}", Time.unscaledTime, " tile finished"));
			_callback(new DataFetchingResult(this, webRequestResponse));
			_webRequest = null;
		}

		//TODO: change signature if 'VectorTile' class changes from 'sealed'
		//protected override void Dispose(bool disposeManagedResources)
		public void Dispose(bool disposeManagedResources)
		{
			if (!isDisposed)
			{
				if (disposeManagedResources)
				{
					//TODO implement IDisposable with Mapbox.VectorTile.VectorTile
					if (null != data)
					{
						data = null;
					}
				}
			}
		}

		/// <summary>
		/// Gets all availble layer names.
		/// See available layers and features <see href="https://www.mapbox.com/vector-tiles/mapbox-streets-v7/">here</see>.
		/// </summary>
		/// <returns>Collection of availble layers.</returns>
		/// <example>
		/// Inspect the LayerNames.
		/// <code>
		/// var layerNames = vectorTile.LayerNames();
		/// foreach (var layer in layerNames)
		/// {
		/// 	Console.Write("Layer: " + layer);
		/// }
		/// </code>
		/// </example>
		public ReadOnlyCollection<string> LayerNames()
		{
			return this.data.LayerNames();
		}

		// FIXME: Why don't these work?
		/// <summary>
		/// Decodes the requested layer.
		/// </summary>
		/// <param name="layerName">Name of the layer to decode.</param>
		/// <returns>Decoded VectorTileLayer or 'null' if an invalid layer name was specified.</returns>
		/// <example>
		/// Inspect a layer of the vector tile.
		/// <code>
		/// var countryLabelLayer = vectorTile.GetLayer("country_label");
		/// var count = countryLabelLayer.Keys.Count;
		/// for (int i = 0; i &lt; count; i++)
		/// {
		/// 	Console.Write(string.Format("{0}:{1}", countryLabelLayer.Keys[i], countryLabelLayer.Values[i]));
		/// }
		/// </code>
		/// </example>
		public VectorTileLayer GetLayer(string layerName)
		{
			return this.data.GetLayer(layerName);
		}

		protected override TileResource MakeTileResource(string tilesetId)
		{

			return (_isStyleOptimized) ?
				TileResource.MakeStyleOptimizedVector(Id, tilesetId, _optimizedStyleId, _modifiedDate)
			  : TileResource.MakeVector(Id, tilesetId);
		}

		public void SetVectorFromCache(ByteArrayTile byteArrayTile)
		{
			byteData = byteArrayTile.byteData;
			data = byteArrayTile.Data;
			TileState = TileState.Loaded;
		}
	}

	public class VectorTile : ByteArrayTile
	{
		public VectorTile(CanonicalTileId canonicalTileId, string tilesetId) : base(canonicalTileId, tilesetId)
		{
			
		}
		
		protected override TileResource MakeTileResource(string tilesetId)
		{ 
			return (_isStyleOptimized) ?
				TileResource.MakeStyleOptimizedVector(Id, tilesetId, _optimizedStyleId, _modifiedDate)
				: TileResource.MakeVector(Id, tilesetId);
		}
	}
	
	public class BuildingTile : ByteArrayTile
	{
		public BuildingTile(CanonicalTileId canonicalTileId, string tilesetId) : base(canonicalTileId, tilesetId)
		{
			
		}

		protected override TileResource MakeTileResource(string tilesetId)
		{ 
			return TileResource.MakeBuilding(Id, tilesetId);
		}
	}
}
