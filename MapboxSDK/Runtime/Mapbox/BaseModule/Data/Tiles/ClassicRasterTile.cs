//-----------------------------------------------------------------------
// <copyright file="ClassicRasterTile.cs" company="Mapbox">
//     Copyright (c) 2016 Mapbox. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace Mapbox.BaseModule.Data.Tiles
{
	/// <summary>
	///    A raster tile from the Mapbox Map API, a encoded image representing a geographic
	///    bounding box. Usually JPEG or PNG encoded.
	/// See <see cref="T:Mapbox.BaseModule.Data.Tiles.RasterTile"/> for usage.
    /// Read more about <see href="https://www.mapbox.com/api-documentation/legacy/static-classic/"> static classic maps </see>.
	/// </summary>
	public class ClassicRasterTile : RasterTile
	{
		public ClassicRasterTile()
		{
			
		}

		public ClassicRasterTile(CanonicalTileId tileId, string tilesetId, bool useNonReadableTexture) : base(tileId, tilesetId, useNonReadableTexture)
		{

		}

		protected override TileResource MakeTileResource(string tilesetId)
		{
			return TileResource.MakeClassicRaster(Id, tilesetId);
		}
	}
}
