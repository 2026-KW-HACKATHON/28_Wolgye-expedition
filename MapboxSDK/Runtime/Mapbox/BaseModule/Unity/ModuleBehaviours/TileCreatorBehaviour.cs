using System;
using Mapbox.BaseModule.Map;
using UnityEngine;

namespace Mapbox.BaseModule.Unity.ModuleBehaviours
{
    public class TileCreatorBehaviour : MonoBehaviour
    {
        [NonSerialized] private ITileCreator _tileCreator;
        
        [Tooltip("Materials for base map tile mesh and gameobject")]
        public Material[] TileMaterials;

        public bool InstanceMaterials = true;
        public int CacheSize = 25;
    
        public ITileCreator GetTileCreator(UnityContext unityContext)
        {
            if (_tileCreator != null) return _tileCreator;

            _tileCreator = new TileCreator(unityContext, TileMaterials, CacheSize, InstanceMaterials);
            return _tileCreator;
        }
    }
}
