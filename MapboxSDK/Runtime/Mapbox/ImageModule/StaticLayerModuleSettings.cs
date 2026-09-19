using System;
using Mapbox.BaseModule.Data.Interfaces;
using Mapbox.BaseModule.Map;
using UnityEngine;

namespace Mapbox.ImageModule
{
    [Serializable]
    public class StaticLayerModuleSettings
    {
        public ImagerySourceType SourceType;
        public string CustomSourceId;
        public bool LoadBackgroundTextures = false;
        public ImageSourceSettings DataSettings;
        
        [Tooltip("Tile outside this range will be rejected.")]
        public Vector2 RejectTilesOutsideZoom = new Vector2(12, 16);
    }
}