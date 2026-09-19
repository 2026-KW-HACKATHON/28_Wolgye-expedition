using System.Collections.Generic;
using Mapbox.BaseModule.Data.Tiles;

namespace Mapbox.BaseModule.Map
{
    public class TileCover
    {
        public HashSet<UnwrappedTileId> Tiles;

        public TileCover()
        {
            Tiles = new HashSet<UnwrappedTileId>();
        }
    }
}