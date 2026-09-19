using System;
using System.Collections.Generic;
using Mapbox.BaseModule.Data.Vector2d;
using Mapbox.BaseModule.Map;
using Mapbox.BaseModule.Utilities;
using Mapbox.Utils;
using UnityEngine;

namespace Mapbox.Example.Scripts.Map
{
    public class HeroBuilding : MonoBehaviour
    {
        public MapboxMap Map;
        public List<HeroBuildingInfo> Buildings;
        private bool _isInitialized = false;

        private void Awake()
        {
            var mapBehaviour = FindObjectOfType<MapboxMapBehaviour>();
            mapBehaviour.Initialized += (map) =>
            {
                Map = map;

                if (enabled)
                {
                    foreach (var buildingInfo in Buildings)
                    {
                        buildingInfo.LatitudeLongitude = Conversions.StringToLatLon(buildingInfo.LatLng);
                        buildingInfo.Object = Instantiate(buildingInfo.Prefab);
                    }
                }
                _isInitialized = true;
            };

        }

        public void LateUpdate()
        {
            if (!_isInitialized)
                return;

            foreach (var buildingInfo in Buildings)
            {
                var worldPosition = Map.MapInformation.ConvertLatLngToPosition(buildingInfo.LatitudeLongitude);
                buildingInfo.Object.transform.position = worldPosition;
                buildingInfo.Object.transform.localScale = Vector3.one / Map.MapInformation.Scale * (1 / Mathf.Cos(Mathf.Deg2Rad * (float)Map.MapInformation.LatitudeLongitude.Latitude));
            }
        }
    }

    [Serializable]
    public class HeroBuildingInfo
    {
        public string LatLng;
        public GameObject Prefab;
        public float Rotation;

        [NonSerialized]
        public LatitudeLongitude LatitudeLongitude;
        [NonSerialized]
        public GameObject Object;
    }
}