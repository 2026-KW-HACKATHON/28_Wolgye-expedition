using System.Collections.Generic;
using Mapbox.BaseModule.Data.Tiles;
using Mapbox.BaseModule.Map;
using Mapbox.BaseModule.Unity;
using Mapbox.ImageModule;
using Mapbox.ImageModule.Terrain;
using Mapbox.VectorModule;

namespace Mapbox.Example.Scripts.ModuleBehaviours
{
    public class ElevatedVectorMediator
    {
        private VectorLayerModule _vectorLayer;
        private TerrainLayerModule _terrainLayer;
    
        public ElevatedVectorMediator(MapService mapService, VectorLayerModule vectorLayer, TerrainLayerModule terrainModule) : base()
        {
            _vectorLayer = vectorLayer;
            _terrainLayer = terrainModule;
        }

        public virtual bool LoadInstant(UnityMapTile unityTile)
        {
            return _terrainLayer.LoadInstant(unityTile) && _vectorLayer.LoadInstant(unityTile);
        }

        public virtual bool RetainTiles(HashSet<CanonicalTileId> retainedTiles,
            Dictionary<UnwrappedTileId, UnityMapTile> activeTiles)
        {
            return _terrainLayer.RetainTiles(retainedTiles) && _vectorLayer.RetainTiles(retainedTiles);
        }
    }
}