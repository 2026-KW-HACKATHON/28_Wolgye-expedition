using System;
using System.Collections;
using Mapbox.BaseModule.Map;
using UnityEngine;

namespace Mapbox.BaseModule.Utilities
{
    public abstract class MapBehaviourCore : MonoBehaviour
    {
        [Tooltip("Initial map view parameters")]
        public MapInformation MapInformation = new MapInformation("60.1710235,24.9432024", 90, 1, 16);
        
        [NonSerialized] public MapboxMap MapboxMap = null;
        public InitializationStatus InitializationStatus => MapboxMap != null
                                                                ? MapboxMap.Status
                                                                : InitializationStatus.WaitingForInitialization;
        public Action<MapboxMap> Initialized = (m) => { };

        public virtual IEnumerator Initialize()
        {
            yield return null;
        }
    }
}
