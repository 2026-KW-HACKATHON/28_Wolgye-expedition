using System;
using Mapbox.BaseModule.Data.DataFetchers;
using Mapbox.BaseModule.Data.Platform.Cache;
using Mapbox.BaseModule.Data.Tiles;
using Mapbox.BaseModule.Map;

namespace Mapbox.UnityMapService.DataSources
{
    public class StaticSource : ImageSource<RasterData>
    {
        protected ImageSourceSettings _settings;
        
        public StaticSource(DataFetchingManager dataFetchingManager, MapboxCacheManager mapboxCacheManager, ImageSourceSettings settings) 
            : base(dataFetchingManager, mapboxCacheManager, settings)
        {
            _settings = settings;
        }

        protected override RasterTile CreateTile(CanonicalTileId tileId, string tilesetId)
        {
            RasterTile rasterTile;
            //`starts with` is weak and string operations are slow
            //but caching type and using Activator.CreateInstance (or caching func and calling it)  is even slower
            if (tilesetId.StartsWith("mapbox://", StringComparison.Ordinal))
            {
                rasterTile = _settings.UseRetinaTextures ? new RetinaRasterTile(tileId, tilesetId, _settings.UseNonReadableTextures) : new RasterTile(tileId, tilesetId, _settings.UseNonReadableTextures);
            }
            else
            {
                rasterTile = _settings.UseRetinaTextures ? new ClassicRetinaRasterTile(tileId, tilesetId, _settings.UseNonReadableTextures) : new ClassicRasterTile(tileId, tilesetId, _settings.UseNonReadableTextures);
            }

            return rasterTile;
        }

        protected override RasterData CreateRasterDataWrapper(RasterTile tile)
        {
            RasterData rasterData;
            rasterData = new RasterData()
            {
                TileId = tile.Id,
                TilesetId = tile.TilesetId,
                Texture = tile.Texture2D,
                CacheType = tile.FromCache,
                Data = tile.Data,
                ETag = tile.ETag,
                ExpirationDate = tile.ExpirationDate
            };
            //_dataTileMatch.Add(rasterData, tile);

            return rasterData;
        }
    }

    

    
}