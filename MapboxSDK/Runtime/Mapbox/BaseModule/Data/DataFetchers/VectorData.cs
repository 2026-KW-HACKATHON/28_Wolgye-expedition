using System;

namespace Mapbox.BaseModule.Data.DataFetchers
{
    [Serializable]
    public class VectorData : MapboxTileData
    {
        public VectorTile.VectorTile VectorTileData;
    }
}