using Mapbox.BaseModule.Data.DataFetchers;
using Mapbox.BaseModule.Data.Platform.Cache;
using Mapbox.BaseModule.Data.Tiles;
using Mapbox.BaseModule.Map;
using UnityEngine;

namespace Mapbox.UnityMapService.DataSources
{
    public class VectorSource : PbfSource<VectorData>
    {
        public VectorSource(DataFetchingManager dataFetchingManager, MapboxCacheManager mapboxCacheManager, VectorSourceSettings settings) : base(dataFetchingManager, mapboxCacheManager, settings)
        {
        }

        protected override ByteArrayTile CreateTile(CanonicalTileId canonicalTileId, string tilesetId)
        {
            var vectorTile = new BaseModule.Data.Tiles.VectorTile(canonicalTileId, tilesetId);
            return vectorTile;
        }

        protected override VectorData CreateVectorData(ByteArrayTile vectorCacheItem)
        {
            VectorData data = null;
            //if (!_activeDatas.ContainsKey(tile.Id))
            {
                data = new VectorData()
                {
                    TileId = vectorCacheItem.Id,
                    TilesetId = vectorCacheItem.TilesetId,
                    Data = vectorCacheItem.ByteData,
                    ETag = vectorCacheItem.ETag,
                    CacheType = vectorCacheItem.FromCache,
                    ExpirationDate = vectorCacheItem.ExpirationDate
                };
            }

            return data;
        }

    }
}