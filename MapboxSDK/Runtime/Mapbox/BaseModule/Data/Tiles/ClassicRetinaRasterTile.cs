//-----------------------------------------------------------------------
// <copyright file="ClassicRetinaRasterTile.cs" company="Mapbox">
//     Copyright (c) 2016 Mapbox. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace Mapbox.BaseModule.Data.Tiles
{
	/// <summary>
	///    A retina-resolution raster tile from the Mapbox Map API, a encoded image representing a geographic
	///    bounding box. Usually JPEG or PNG encoded.
	/// Like <see cref="T:Mapbox.BaseModule.Data.Tiles.ClassicRasterTile"/>, but higher resolution.
    /// See <see href="https://www.mapbox.com/api-documentation/#high-dpi-images"> retina documentation </see>.
	/// </summary>
    public class ClassicRetinaRasterTile : ClassicRasterTile
	{
		public ClassicRetinaRasterTile(CanonicalTileId tileId, string tilesetId, bool useNonReadableTexture) : base(tileId, tilesetId, useNonReadableTexture)
		{

		}

		protected override TileResource MakeTileResource(string tilesetId)
		{
			return TileResource.MakeClassicRetinaRaster(Id, tilesetId);
		}
	}
}
