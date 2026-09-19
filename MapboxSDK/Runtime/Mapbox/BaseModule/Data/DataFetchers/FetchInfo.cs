using System;
using Mapbox.BaseModule.Data.Tiles;

namespace Mapbox.BaseModule.Data.DataFetchers
{
    public class FetchInfo
    {
        public Action<DataFetchingResult> Callback;
        public Tile Tile;
        public float QueueTime;
        public bool IsUpdate = false;

        public FetchInfo(Tile tile, Action<DataFetchingResult> callback = null)
        {
            Tile = tile;
            Callback = callback;
        }

    }
}