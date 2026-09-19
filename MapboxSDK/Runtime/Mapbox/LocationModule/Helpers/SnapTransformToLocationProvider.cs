using Mapbox.BaseModule.Map;
using Mapbox.BaseModule.Utilities;
using Mapbox.LocationModule;
using UnityEngine;
//using Mapbox.LocationModule;

namespace Mapbox.LocationModule.Helpers
{
    public class SnapTransformToLocationProvider : MonoBehaviour
    {
        public Transform _transform;
        
        [SerializeField]
        private LocationProviderFactory _locationProvider;
        [SerializeField] private MapBehaviourCore _mapCore;
        private MapboxMap _map;
        
        private void Start()
        {
            if(_transform == null)
                _transform = transform;

            if (_locationProvider == null)
            {
                Debug.Log("_locationProvider null");
                return;
            }
        
            _mapCore.Initialized += (map) =>
            {
                _map = map;
                if (_locationProvider.DefaultLocationProvider != null)
                {
                    _locationProvider.DefaultLocationProvider.OnLocationUpdated += OnDefaultLocationProviderOnOnLocationUpdated;
                }
            };
        }

        private void OnDefaultLocationProviderOnOnLocationUpdated(Location s)
        {
            if (_map.Status >= InitializationStatus.ReadyForUpdates && enabled)
            {
                _transform.position = _map.MapInformation.ConvertLatLngToPosition(s.LatitudeLongitude);
            }
        }
    }
}
