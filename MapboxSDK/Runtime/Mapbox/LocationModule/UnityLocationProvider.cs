using System;
using Mapbox.BaseModule.Data.Vector2d;
using Mapbox.BaseModule.Utilities;
using Mapbox.LocationModule.AngleSmoothing;
using Mapbox.LocationModule.UnityLocationWrappers;
using UnityEngine;
using UnityEngine.Android;
using UnityEngine.Scripting;

namespace Mapbox.LocationModule
{
    [Serializable]
    public class UnityLocationProviderSettings
    {
        /// <summary>
        /// Using higher value like 500 usually does not require to turn GPS chip on and thus saves battery power. 
        /// Values like 5-10 could be used for getting best accuracy.
        /// </summary>
        [SerializeField]
        [Tooltip(
            "Using higher value like 500 usually does not require to turn GPS chip on and thus saves battery power. Values like 5-10 could be used for getting best accuracy.")]
        public float DesiredAccuracyInMeters = 1.0f;

        /// <summary>
        /// The minimum distance (measured in meters) a device must move laterally before Input.location property is updated. 
        /// Higher values like 500 imply less overhead.
        /// </summary>
        [SerializeField]
        [Tooltip(
            "The minimum distance (measured in meters) a device must move laterally before Input.location property is updated. Higher values like 500 imply less overhead.")]
        public float UpdateDistanceInMeters = 0.0f;

        [SerializeField]
        [Tooltip(
            "The minimum time interval between location updates, in milliseconds. It's reasonable to not go below 500ms.")]
        public long UpdateTimeInMilliSeconds = 500;

        [SerializeField] [Tooltip("Smoothing strategy to be applied to the UserHeading.")]
        public AngleSmoothingAbstractBase UserHeadingSmoothing;

        [SerializeField] [Tooltip("Smoothing strategy to applied to the DeviceOrientation.")]
        public AngleSmoothingAbstractBase DeviceOrientationSmoothing;
    }

    /// <summary>
    /// The DeviceLocationProvider is responsible for providing real world location and heading data,
    /// served directly from native hardware and OS. 
    /// This relies on Unity's <see href="https://docs.unity3d.com/ScriptReference/LocationService.html">LocationService</see> for location
    /// and <see href="https://docs.unity3d.com/ScriptReference/Compass.html">Compass</see> for heading.
    /// </summary>
    public class UnityLocationProvider : AbstractLocationProvider
    {
        private IMapboxLocationService _locationService;
        private double _lastLocationTimestamp;
        private float _lastPollTime;
        private float _updateInterval;

        /// <summary>list of positions to keep for calculations</summary>
        private CircularBuffer<LatitudeLongitude> _lastPositions;

        /// <summary>number of last positons to keep</summary>
        private int _maxLastPositions = 5;

        /// <summary>minimum needed distance between oldest and newest position before UserHeading is calculated</summary>
        private double _minDistanceOldestNewestPosition = 1.5;

        private readonly UnityLocationProviderSettings _unityLocationProviderSettings = new UnityLocationProviderSettings();

        private const string FineLocation = Permission.FineLocation;

        public UnityLocationProviderSettings UnityLocationProviderSettings => _unityLocationProviderSettings;

        /// <summary>
        /// Constructor for production use - uses Unity's real location service.
        /// </summary>
        [Preserve]
        public UnityLocationProvider(UnityLocationProviderSettings settings) : this(settings, new MapboxLocationServiceUnityWrapper())
        {
        }

        /// <summary>
        /// Constructor for testing - accepts mock location service.
        /// Use MapboxLocationServiceMock to replay location logs for testing.
        /// </summary>
        public UnityLocationProvider(UnityLocationProviderSettings settings, IMapboxLocationService locationService)
        {
            _unityLocationProviderSettings = settings;
            _locationService = locationService ?? throw new ArgumentNullException(nameof(locationService));
            //HandlePermission();

            _currentLocation.Provider = "unity";
            _updateInterval = UnityLocationProviderSettings.UpdateTimeInMilliSeconds < 500
                ? 0.5f
                : (float)UnityLocationProviderSettings.UpdateTimeInMilliSeconds / 1000.0f;

            if (null == UnityLocationProviderSettings.UserHeadingSmoothing)
            {
                UnityLocationProviderSettings.UserHeadingSmoothing = new AngleSmoothingNoOp();
            }

            if (null == UnityLocationProviderSettings.DeviceOrientationSmoothing)
            {
                UnityLocationProviderSettings.DeviceOrientationSmoothing = new AngleSmoothingNoOp();
            }

            _lastPositions = new CircularBuffer<LatitudeLongitude>(_maxLastPositions);
        }

        public override void Update()
        {
            if (Time.realtimeSinceStartup - _lastPollTime < _updateInterval)
                return;
            _lastPollTime = Time.realtimeSinceStartup;

            var lastData = _locationService.lastData;
            var timestamp = lastData.timestamp;

            _currentLocation.IsLocationServiceEnabled =
                _locationService.status == LocationServiceStatus.Running
                || timestamp > _lastLocationTimestamp;

            _currentLocation.IsUserHeadingUpdated = false;
            _currentLocation.IsLocationUpdated = false;

            if (!_currentLocation.IsLocationServiceEnabled)
                return;

            // device orientation, user heading get calculated below
            UnityLocationProviderSettings.DeviceOrientationSmoothing.Add(Input.compass.trueHeading);
            _currentLocation.DeviceOrientation =
                (float)UnityLocationProviderSettings.DeviceOrientationSmoothing.Calculate();

            double latitude = lastData.latitude;
            double longitude = lastData.longitude;
            Vector2d previousLocation = new Vector2d(_currentLocation.LatitudeLongitude.Latitude, _currentLocation.LatitudeLongitude.Longitude);
            _currentLocation.LatitudeLongitude = new LatitudeLongitude(latitude, longitude);

            _currentLocation.Accuracy = (float)Math.Floor(lastData.horizontalAccuracy);
            // sometimes Unity's timestamp doesn't seem to get updated, or even jump back in time
            // do an additional check if location has changed
            _currentLocation.IsLocationUpdated = timestamp > _lastLocationTimestamp || !_currentLocation.LatitudeLongitude.Equals(previousLocation);
            _currentLocation.Timestamp = timestamp;
            _lastLocationTimestamp = timestamp;

            if (_currentLocation.IsLocationUpdated)
            {
                if (_lastPositions.Count > 0)
                {
                    // only add position if user has moved +1m since we added the previous position to the list
                    CheapRuler cheapRuler = new CheapRuler(_currentLocation.LatitudeLongitude.Latitude, CheapRulerUnits.Meters);
                    var p = _currentLocation.LatitudeLongitude;
                    double distance = cheapRuler.Distance(
                        new double[] { p.Longitude, p.Latitude },
                        new double[] { _lastPositions[0].Longitude, _lastPositions[0].Latitude }
                    );
                    if (distance > 1.0)
                    {
                        _lastPositions.Add(_currentLocation.LatitudeLongitude);
                    }
                }
                else
                {
                    _lastPositions.Add(_currentLocation.LatitudeLongitude);
                }
            }

            // if we have enough positions calculate user heading ourselves.
            // Unity does not provide bearing based on GPS locations, just
            // device orientation based on Compass.Heading.
            // nevertheless, use compass for initial UserHeading till we have
            // enough values to calculate ourselves.
            if (_lastPositions.Count < _maxLastPositions)
            {
                _currentLocation.UserHeading = _currentLocation.DeviceOrientation;
                _currentLocation.IsUserHeadingUpdated = true;
            }
            else
            {
                var newestPos = _lastPositions[0];
                var oldestPos = _lastPositions[_maxLastPositions - 1];
                CheapRuler cheapRuler = new CheapRuler(newestPos.Latitude, CheapRulerUnits.Meters);
                double distance = cheapRuler.Distance(
                    new double[] { newestPos.Longitude, newestPos.Latitude },
                    new double[] { oldestPos.Longitude, oldestPos.Latitude }
                );
                // positions are minimum required distance apart (user is moving), calculate user heading
                if (distance >= _minDistanceOldestNewestPosition)
                {
                    float[] lastHeadings = new float[_maxLastPositions - 1];

                    for (int i = 1; i < _maxLastPositions; i++)
                    {
                        // atan2 increases angle CCW, flip sign of latDiff to get CW
                        double latDiff = -(_lastPositions[i].Latitude - _lastPositions[i - 1].Latitude);
                        double lngDiff = _lastPositions[i].Longitude - _lastPositions[i - 1].Longitude;
                        // +90.0 to make top (north) 0 degrees
                        double heading = (Math.Atan2(latDiff, lngDiff) * 180.0 / Math.PI) + 90.0f;
                        // stay within [0..360] range
                        if (heading < 0)
                        {
                            heading += 360;
                        }

                        if (heading >= 360)
                        {
                            heading -= 360;
                        }

                        lastHeadings[i - 1] = (float)heading;
                    }

                    UnityLocationProviderSettings.UserHeadingSmoothing.Add(lastHeadings[0]);
                    float finalHeading = (float)UnityLocationProviderSettings.UserHeadingSmoothing.Calculate();

                    //fix heading to have 0 for north, 90 for east, 180 for south and 270 for west
                    finalHeading = finalHeading >= 180.0f ? finalHeading - 180.0f : finalHeading + 180.0f;

                    _currentLocation.UserHeading = finalHeading;
                    _currentLocation.IsUserHeadingUpdated = true;
                }
            }

            _currentLocation.TimestampDevice = UnixTimestampUtils.To(DateTime.UtcNow);
            SendLocation(_currentLocation);
        }
    }
}