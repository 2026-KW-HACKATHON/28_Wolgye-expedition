using Mapbox.BaseModule.Data.Vector2d;
using Mapbox.BaseModule.Map;
using Mapbox.BaseModule.Utilities;
using Mapbox.LocationModule;
using UnityEngine;

namespace Mapbox.LocationModule.Helpers
{
    public class ShiftMapForDistance : MonoBehaviour
    {
        public MapBehaviourCore Map;
        public LocationProviderFactory LocationProviderFactory;
        [Tooltip("Shift whole map if the distance between last two location points is bigger than this value (in mercator units). ")]
        public float ShiftMapDistance;
    
        private MapboxMap _map;
        private ILocationProvider _locationProvider;
        private LatitudeLongitude _previousLocation = LatitudeLongitude.Invalid;
        private LatitudeLongitude _currentLocation;
    
        private void Start()
        {
            Map.Initialized += map =>
            {
                _map = map;
                _locationProvider = LocationProviderFactory.DefaultLocationProvider;
                if (_locationProvider != null)
                {
                    _locationProvider.OnLocationUpdated += LocationUpdated;
                }
            };
        }

        private void LocationUpdated(Location location)
        {
            _currentLocation = location.LatitudeLongitude;
            if (_previousLocation != LatitudeLongitude.Invalid)
            {
                var d1 = Conversions.LatitudeLongitudeToWebMercator(_previousLocation);
                var d2 = Conversions.LatitudeLongitudeToWebMercator(_currentLocation);
                if (Vector2d.Distance(d1, d2) > ShiftMapDistance)
                {
                    Debug.Log(string.Format("ShiftMapForDistance: d1: {0} d2: {1}", d1, d2));
                    _map.ChangeView(_currentLocation);
                }
            }

            _previousLocation = _currentLocation;
        }
    }
}
