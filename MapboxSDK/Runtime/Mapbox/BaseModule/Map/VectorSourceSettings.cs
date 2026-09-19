using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Mapbox.BaseModule.Map
{
    [Serializable]
    public class VectorSourceSettings
    {
        [NonSerialized] public string TilesetId;
        public int CacheSize = 100;
        
        [Tooltip("Maximum data level that'll be used for this module. Tiles can be higher zoom level but data will be lower level.")]
        public int ClampDataLevelToMax = 15;
    }
}