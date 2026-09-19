using System.Collections.Generic;
using Mapbox.LocationModule.UnityLocationWrappers;
using UnityEngine;

namespace Mapbox.LocationModule.Tests
{
    /// <summary>
    /// Simple mock location service for unit tests.
    /// Allows queueing specific location data to be returned on demand.
    /// </summary>
    public class TestLocationServiceMock : IMapboxLocationService
    {
        private readonly Queue<IMapboxLocationInfo> _locations = new Queue<IMapboxLocationInfo>();
        private IMapboxLocationInfo _currentLocation;
        private LocationServiceStatus _status = LocationServiceStatus.Stopped;
        private bool _isEnabledByUser = true;

        public bool isEnabledByUser => _isEnabledByUser;
        public LocationServiceStatus status => _status;
        public IMapboxLocationInfo lastData => _currentLocation;

        public void SetEnabledByUser(bool enabled)
        {
            _isEnabledByUser = enabled;
        }

        public void SetStatus(LocationServiceStatus newStatus)
        {
            _status = newStatus;
        }

        public void QueueLocation(IMapboxLocationInfo location)
        {
            _locations.Enqueue(location);
        }

        public void QueueLocation(float latitude, float longitude, float accuracy = 10f, double timestamp = 0)
        {
            var location = new TestLocationInfo
            {
                latitude = latitude,
                longitude = longitude,
                accuracy = accuracy,
                timestamp = timestamp > 0 ? timestamp : System.DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };
            _locations.Enqueue(location);
        }

        public void Start(float desiredAccuracyInMeters, float updateDistanceInMeters)
        {
            _status = LocationServiceStatus.Running;
            AdvanceToNextLocation();
        }

        public void Stop()
        {
            _status = LocationServiceStatus.Stopped;
        }

        /// <summary>
        /// Simulates location service update by advancing to next queued location.
        /// </summary>
        public void AdvanceToNextLocation()
        {
            if (_locations.Count > 0)
            {
                _currentLocation = _locations.Dequeue();
            }
        }
    }

    /// <summary>
    /// Simple test implementation of IMapboxLocationInfo.
    /// </summary>
    public class TestLocationInfo : IMapboxLocationInfo
    {
        public float latitude { get; set; }
        public float longitude { get; set; }
        public float altitude { get; set; }
        public float horizontalAccuracy { get; set; }
        public float verticalAccuracy { get; set; }
        public float accuracy
        {
            get => horizontalAccuracy;
            set => horizontalAccuracy = value;
        }
        public double timestamp { get; set; }
    }
}
