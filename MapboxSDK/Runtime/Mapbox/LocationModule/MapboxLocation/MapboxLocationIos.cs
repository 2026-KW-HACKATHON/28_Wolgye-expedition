using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using Mapbox.BaseModule.Data.Vector2d;
using Mapbox.BaseModule.Utilities;

#if UNITY_IOS
namespace Mapbox.LocationModule.MapboxLocation
{
    public class MapboxLocationIos : IMapboxDeviceLocation
    {
        public event Action<Location> LocationUpdated = delegate { };
        public event Action<MapboxLocationServiceStatus> AuthorizationChanged = delegate { };
        public event Action<AccuracyAuthorization> AccuracyAuthorizationChanged = delegate { };
        public event Action<bool> AvailabilityChanged = delegate { };

        public MapboxLocationSettings _mapboxLocationSettings;

        private Location _currentLocation;
        // Static reference for IL2CPP callback compatibility
        private static MapboxLocationIos _instance;
        private GCHandle _gcHandle;

        // Queue for marshaling callbacks to main thread
        private Queue<Action> _mainThreadQueue = new Queue<Action>();
        private object _queueLock = new object();

        // Callback delegate for location updates
        private delegate void LocationUpdateCallback(
            double latitude, double longitude,
            float accuracy, double timestamp,
            double altitude, float speed, float bearing);

        // Callback delegates for service observer
        private delegate void AuthorizationStatusCallback(int status);
        private delegate void AccuracyAuthorizationCallback(int accuracy);
        private delegate void AvailabilityCallback(bool available);

        // DllImport declarations
        [DllImport("__Internal")]
        private static extern void requestLocationAuthorization();

        [DllImport("__Internal")]
        private static extern IntPtr startLocationUpdatesWithSettings(
            long minimumInterval, long maximumInterval, long interval,
            int accuracyLevel, float displacement,
            LocationUpdateCallback callback);

        [DllImport("__Internal")]
        private static extern void stopLocationUpdates(IntPtr provider);

        [DllImport("__Internal")]
        private static extern void addLocationServiceObserver(
            AuthorizationStatusCallback authCallback,
            AccuracyAuthorizationCallback accuracyCallback,
            AvailabilityCallback availabilityCallback);

        [DllImport("__Internal")]
        private static extern void removeLocationServiceObserver();

        // Implementation details
        private IntPtr _locationProvider;
        private static LocationUpdateCallback _callback;
        private static AuthorizationStatusCallback _authCallback;
        private static AccuracyAuthorizationCallback _accuracyCallback;
        private static AvailabilityCallback _availabilityCallback;
        private bool _isStarted = false;
        private bool _observerAdded = false;

        public MapboxLocationIos(MapboxLocationSettings settings)
        {
            _mapboxLocationSettings = settings;
            // Request location authorization first
            requestLocationAuthorization();

            // Replace the static instance first so native callbacks always have a
            // valid target, then tear down the old instance's native resources.
            var previous = _instance;
            _instance = this;
            _gcHandle = GCHandle.Alloc(this);

            if (previous != null)
            {
                Debug.LogWarning("MapboxLocationIos: Replacing existing instance. Cleaning up previous native resources.");
                previous.StopNativeUpdates();
            }

            // Create static callback delegates
            _callback = OnLocationUpdateStatic;
            _authCallback = OnAuthorizationChangedStatic;
            _accuracyCallback = OnAccuracyAuthorizationChangedStatic;
            _availabilityCallback = OnAvailabilityChangedStatic;

            // Add service observer for permission changes
            addLocationServiceObserver(_authCallback, _accuracyCallback, _availabilityCallback);
            _observerAdded = true;

            // Map accuracy level enum to integer
            int accuracyLevel = (int)_mapboxLocationSettings.AccuracyLevel;

            // Start location updates with settings
            _locationProvider = startLocationUpdatesWithSettings(
                _mapboxLocationSettings.MinimumInterval,
                _mapboxLocationSettings.MaximumInterval,
                _mapboxLocationSettings.Interval,
                accuracyLevel,
                _mapboxLocationSettings.Displacement,
                _callback);

            if (_locationProvider != IntPtr.Zero)
            {
                _isStarted = true;

                // Initialize location struct
                _currentLocation.IsLocationServiceInitializing = false;
                _currentLocation.IsLocationServiceEnabled = true;
                _currentLocation.Provider = "MapboxCommon";
                _currentLocation.ProviderClass = "LocationIos";
            }
            else
            {
                Debug.LogError("Failed to start iOS location service");
                _currentLocation.IsLocationServiceInitializing = false;
                _currentLocation.IsLocationServiceEnabled = false;
            }
        }

        public void Initialize()
        {

        }

        // Static callback for IL2CPP compatibility
        [AOT.MonoPInvokeCallback(typeof(LocationUpdateCallback))]
        private static void OnLocationUpdateStatic(
            double latitude, double longitude,
            float accuracy, double timestamp,
            double altitude, float speed, float bearing)
        {
            if (_instance != null)
            {
                _instance.OnLocationUpdate(latitude, longitude, accuracy, timestamp, altitude, speed, bearing);
            }
        }

        // Static callback for authorization status changes
        [AOT.MonoPInvokeCallback(typeof(AuthorizationStatusCallback))]
        private static void OnAuthorizationChangedStatic(int status)
        {
            if (_instance != null)
            {
                _instance.OnAuthorizationChanged(status);
            }
        }

        // Static callback for accuracy authorization changes
        [AOT.MonoPInvokeCallback(typeof(AccuracyAuthorizationCallback))]
        private static void OnAccuracyAuthorizationChangedStatic(int accuracy)
        {
            if (_instance != null)
            {
                _instance.OnAccuracyAuthorizationChanged(accuracy);
            }
        }

        // Static callback for availability changes
        [AOT.MonoPInvokeCallback(typeof(AvailabilityCallback))]
        private static void OnAvailabilityChangedStatic(bool available)
        {
            if (_instance != null)
            {
                _instance.OnAvailabilityChanged(available);
            }
        }

        // Instance method to handle the actual update
        private void OnLocationUpdate(double latitude, double longitude, float accuracy, double timestamp, double altitude, float speed, float bearing)
        {
            // Queue the update to be processed on the main thread
            lock (_queueLock)
            {
                _mainThreadQueue.Enqueue(() =>
                {
                    try
                    {
                        // Update location struct
                        _currentLocation.LatitudeLongitude = new LatitudeLongitude(latitude, longitude);
                        _currentLocation.Accuracy = accuracy;
                        _currentLocation.Timestamp = timestamp;
                        _currentLocation.TimestampDevice = UnixTimestampUtils.To(DateTime.UtcNow);

                        // Set optional properties
                        _currentLocation.SpeedMetersPerSecond = speed > 0 ? speed : (float?)null;
                        _currentLocation.UserHeading = bearing;
                        _currentLocation.IsUserHeadingUpdated = bearing > 0;

                        _currentLocation.IsLocationUpdated = true;
                        _currentLocation.IsLocationServiceEnabled = true;
                        _currentLocation.IsLocationServiceInitializing = false;

                        // Send location update event
                        LocationUpdated(_currentLocation);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Exception in iOS location update: {ex.Message}");
                    }
                });
            }
        }

        // Instance method to handle authorization status changes
        private void OnAuthorizationChanged(int status)
        {
            lock (_queueLock)
            {
                _mainThreadQueue.Enqueue(() =>
                {
                    try
                    {
                        AuthorizationChanged((MapboxLocationServiceStatus)status);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Exception in authorization changed: {ex.Message}");
                    }
                });
            }
        }

        // Instance method to handle accuracy authorization changes
        private void OnAccuracyAuthorizationChanged(int accuracy)
        {
            lock (_queueLock)
            {
                _mainThreadQueue.Enqueue(() =>
                {
                    try
                    {
                        AccuracyAuthorizationChanged((AccuracyAuthorization)accuracy);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Exception in accuracy authorization changed: {ex.Message}");
                    }
                });
            }
        }

        // Instance method to handle availability changes
        private void OnAvailabilityChanged(bool available)
        {
            lock (_queueLock)
            {
                _mainThreadQueue.Enqueue(() =>
                {
                    try
                    {
                        AvailabilityChanged(available);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Exception in availability changed: {ex.Message}");
                    }
                });
            }
        }

        public void Update()
        {
            // Process queued location updates on the main thread
            lock (_queueLock)
            {
                while (_mainThreadQueue.Count > 0)
                {
                    var action = _mainThreadQueue.Dequeue();
                    action?.Invoke();
                }
            }
        }

        /// <summary>
        /// Stop native location updates and free the GCHandle.
        /// Does not touch the static _instance — safe to call on a replaced instance.
        /// </summary>
        private void StopNativeUpdates()
        {
            if (_isStarted && _locationProvider != IntPtr.Zero)
            {
                stopLocationUpdates(_locationProvider);
                _isStarted = false;
            }

            if (_observerAdded)
            {
                removeLocationServiceObserver();
                _observerAdded = false;
            }

            if (_gcHandle.IsAllocated)
            {
                _gcHandle.Free();
            }
        }

        public void OnDestroy()
        {
            StopNativeUpdates();

            // Clear the static reference only if it still points to this instance.
            // If another instance replaced us, leave it alone — it owns the callbacks now.
            if (_instance == this)
            {
                _instance = null;
                _callback = null;
                _authCallback = null;
                _accuracyCallback = null;
                _availabilityCallback = null;
            }
        }
    }
}
#endif