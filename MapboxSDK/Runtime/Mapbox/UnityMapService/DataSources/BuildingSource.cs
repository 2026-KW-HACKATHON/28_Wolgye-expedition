using Mapbox.BaseModule.Data.DataFetchers;
using Mapbox.BaseModule.Data.Platform.Cache;
using Mapbox.BaseModule.Data.Tiles;
using Mapbox.BaseModule.Map;

namespace Mapbox.UnityMapService.DataSources
{
    public class BuildingSource : PbfSource<BuildingData>
    {
        public BuildingSource(DataFetchingManager dataFetchingManager, MapboxCacheManager mapboxCacheManager, VectorSourceSettings settings) : base(dataFetchingManager, mapboxCacheManager, settings)
        {
        }

        protected override ByteArrayTile CreateTile(CanonicalTileId canonicalTileId, string tilesetId)
        {
            var vectorTile = new BuildingTile(canonicalTileId, tilesetId);
            return vectorTile;
        }

        protected override BuildingData CreateVectorData(ByteArrayTile cacheItem)
        {
            BuildingData data = null;
            //if (!_activeDatas.ContainsKey(tile.Id))
            {
                data = new BuildingData()
                {
                    TileId = cacheItem.Id,
                    TilesetId = cacheItem.TilesetId,
                    Data = cacheItem.ByteData,
                    ETag = cacheItem.ETag,
                    CacheType = cacheItem.FromCache,
                    ExpirationDate = cacheItem.ExpirationDate
                };
            }

            return data;
        }
    }
}