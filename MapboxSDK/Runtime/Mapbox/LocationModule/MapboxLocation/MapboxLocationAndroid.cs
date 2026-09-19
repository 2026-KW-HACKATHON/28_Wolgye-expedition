#if !UNITY_EDITOR && UNITY_ANDROID

using System;
using System.Collections.Generic;
using Mapbox.LocationModule;
using UnityEngine;
using UnityEngine.Scripting;

namespace Mapbox.LocationModule.MapboxLocation
{
    [Preserve]
    public class MapboxLocationAndroid : IMapboxDeviceLocation
    {
        [Preserve] public event Action<Location> LocationUpdated;
        [Preserve] public event Action<MapboxLocationServiceStatus> AuthorizationChanged;
        [Preserve] public event Action<AccuracyAuthorization> AccuracyAuthorizationChanged;
        [Preserve] public event Action<bool> AvailabilityChanged;

        public MapboxLocationSettings _mapboxLocationSettings;

        private readonly Queue<Action> _mainThreadQueue = new Queue<Action>();
        private readonly object _queueLock = new object();

        private AndroidJavaObject _permissionsManager;
        private AndroidJavaObject _unityActivity;

        private string _mapboxLocationServiceFactoryClassName = "com.mapbox.common.location.LocationServiceFactory";
        private string _mapboxLocationServiceFactoryGetMethodName = "getOrCreate";
        private string _mapboxLocationServiceGetProviderMethodName = "getDeviceLocationProvider";
        private string _mapboxLocationProviderRequestClassName = "com.mapbox.common.location.LocationProviderRequest";
        private string _mapboxLocationInvervalClassName = "com.mapbox.common.location.IntervalSettings";


        private AndroidJavaClass _locationServiceFactory;
        private AndroidJavaObject _locationService;
        private AndroidJavaObject _locationProvider;
        private MapboxLocationObserverProxy _locationObserver;
        private MapboxLocationServiceObserverProxy _serviceObserver;
        private AndroidJavaObject _accuracyLevelHigh;

        private float _lastLocationPollTime;
        private float _locationPollInterval = 1.0f; // Poll every 1 second

        private string _intervalSettingsBuilderClassName = "com.mapbox.common.location.IntervalSettings$Builder";
        private string _minimumIntervalFieldName = "minimumInterval";
        private string _maximumIntervalFieldName = "maximumInterval";
        private string _intervalFieldName = "interval";

        private string _locationProviderRequestBuilderClassName = "com.mapbox.common.location.LocationProviderRequest$Builder";

        private string _accuracyLevelClassName = "com.mapbox.common.location.AccuracyLevel";
        private string _accuracyFieldName = "accuracy";
        private string _displacementFieldName = "displacement";
        private string _intervalSettingFieldName = "interval";

        private string _addServiceObserverMethodName = "registerObserver";
        private string _addObserverMethodName = "addLocationObserver";

        private string javaLangLong = "java.lang.Long";
        private string javaLangFloat = "java.lang.Float";

        // Android class names
        private const string _unityPlayerClassName = "com.unity3d.player.UnityPlayer";
        private const string _permissionsManagerClassName = "com.mapbox.android.core.permissions.PermissionsManager";
        private const string _packageManagerClassName = "android.content.pm.PackageManager";
        private const string _deviceLocationProviderTypeClassName = "com.mapbox.common.location.DeviceLocationProviderType";

        // Android method names
        private const string _currentActivityFieldName = "currentActivity";
        private const string _areLocationPermissionsGrantedMethodName = "areLocationPermissionsGranted";
        private const string _requestLocationPermissionsMethodName = "requestLocationPermissions";
        private const string _buildMethodName = "build";
        private const string _isValueMethodName = "isValue";
        private const string _getErrorMethodName = "getError";
        private const string _toStringMethodName = "toString";
        private const string _getValueMethodName = "getValue";
        private const string _valuesMethodName = "values";
        private const string _getLastLocationMethodName = "getLastLocation";
        private const string _checkSelfPermissionMethodName = "checkSelfPermission";
        private const string _getClassMethodName = "getClass";
        private const string _getNameMethodName = "getName";
        

        // Android constants
        private const string _permissionGrantedFieldName = "PERMISSION_GRANTED";
        private const string _fineLocationPermission = "android.permission.ACCESS_FINE_LOCATION";
        private const string _coarseLocationPermission = "android.permission.ACCESS_COARSE_LOCATION";
        private const string _androidProviderTypeName = "ANDROID";

        // Log prefix
        private const string _logPrefix = "MAPBOX_UNITY_SDK: ";

        public MapboxLocationAndroid(MapboxLocationSettings settings)
        {
            _mapboxLocationSettings = settings;
        }

        public void Initialize()
        {
            //Get Unity activity
            using (var unityPlayer = new AndroidJavaClass(_unityPlayerClassName))
            {
                _unityActivity = unityPlayer.GetStatic<AndroidJavaObject>(_currentActivityFieldName);
            }

            InitializeMapboxLocation();
        }
        
        public void Update()
        {
            // Poll location at intervals
            if (_locationProvider != null && Time.realtimeSinceStartup - _lastLocationPollTime >= _locationPollInterval)
            {
                _lastLocationPollTime = Time.realtimeSinceStartup;
                //GetLastLocation();
            }

            // Process main thread queue
            lock (_queueLock)
            {
                while (_mainThreadQueue.Count > 0)
                {
                    var action = _mainThreadQueue.Dequeue();
                    try
                    {
                        action?.Invoke();
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError(_logPrefix + $"Error processing main thread action: {ex.Message}");
                    }
                }
            }
        }
        
        public void OnDestroy()
        {
            try
            {
                // Clean up location provider
                if (_locationProvider != null)
                {
                    _locationProvider.Dispose();
                    _locationProvider = null;
                }

                // Clean up location service
                if (_locationService != null)
                {
                    _locationService.Dispose();
                    _locationService = null;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError(_logPrefix + $"Error during cleanup: {ex.Message}");
            }
        }
        
        
        
        private void RequestLocationPermissions()
        {
            var permissionsListenerProxy = new MapboxLocationPermissionsListenerProxy(
                onExplanation: (permissions) =>
                {
                },
                onResult: (granted) =>
                {
                    if (granted)
                    {
                        InitializeMapboxLocation();
                    }
                    else
                    {
                        Debug.LogError(_logPrefix + "Location permissions denied");
                    }
                });

            _permissionsManager = new AndroidJavaObject(_permissionsManagerClassName, permissionsListenerProxy);
            _permissionsManager.Call(_requestLocationPermissionsMethodName, _unityActivity);
        }

        private void InitializeMapboxLocation()
        {
            _locationServiceFactory = new AndroidJavaClass(_mapboxLocationServiceFactoryClassName);
            _locationService = _locationServiceFactory.CallStatic<AndroidJavaObject>(_mapboxLocationServiceFactoryGetMethodName);

            var intervalSettings = new AndroidJavaObject(_intervalSettingsBuilderClassName)
                .Call<AndroidJavaObject>(_minimumIntervalFieldName,
                    new AndroidJavaObject(javaLangLong, _mapboxLocationSettings.MinimumInterval))
                .Call<AndroidJavaObject>(_maximumIntervalFieldName,
                    new AndroidJavaObject(javaLangLong, _mapboxLocationSettings.MaximumInterval))
                .Call<AndroidJavaObject>(_intervalFieldName,
                    new AndroidJavaObject(javaLangLong, _mapboxLocationSettings.Interval))
                .Call<AndroidJavaObject>(_buildMethodName);

            GetAccuracyLevel();

            var requestSettings = new AndroidJavaObject(_locationProviderRequestBuilderClassName)
                .Call<AndroidJavaObject>(_accuracyFieldName, _accuracyLevelHigh)
                .Call<AndroidJavaObject>(_displacementFieldName,
                    new AndroidJavaObject(javaLangFloat, _mapboxLocationSettings.Displacement))
                .Call<AndroidJavaObject>(_intervalSettingFieldName, intervalSettings)
                .Call<AndroidJavaObject>(_buildMethodName);

            //AndroidJavaObject nullRequest = null;
            var androidType = GetProviderType();
            var expected = _locationService.Call<AndroidJavaObject>(_mapboxLocationServiceGetProviderMethodName, androidType, requestSettings, true);
            var hasValue = expected.Call<bool>(_isValueMethodName);

            if (!hasValue)
            {
                AndroidJavaObject error = expected.Call<AndroidJavaObject>(_getErrorMethodName);
                Debug.LogError(_logPrefix + "Mapbox error: " + error.Call<string>(_toStringMethodName));
                return;
            }

            _serviceObserver = new MapboxLocationServiceObserverProxy(AuthorizationChanged, AccuracyAuthorizationChanged, AvailabilityChanged, EnqueueOnMainThread);

            try
            {
                _locationService.Call(_addServiceObserverMethodName, _serviceObserver);
            }
            catch (Exception ex)
            {
                Debug.LogError(_logPrefix + $"Failed to register service observer: {ex.Message}");
            }

            _locationProvider = expected.Call<AndroidJavaObject>(_getValueMethodName);
            _locationObserver = new MapboxLocationObserverProxy(EnqueueOnMainThread, LocationUpdated);

            try
            {
                // Keep a strong reference to prevent GC
                System.GC.KeepAlive(_locationObserver);

                _locationProvider.Call(_addObserverMethodName, _locationObserver);

                // Verify Android location permissions
                try
                {
                    var permissionClass = new AndroidJavaClass(_packageManagerClassName);
                    int permissionGranted = permissionClass.GetStatic<int>(_permissionGrantedFieldName);

                    int fineStatus = _unityActivity.Call<int>(_checkSelfPermissionMethodName, _fineLocationPermission);
                    int coarseStatus = _unityActivity.Call<int>(_checkSelfPermissionMethodName, _coarseLocationPermission);

                    if (fineStatus != permissionGranted && coarseStatus != permissionGranted)
                    {
                        Debug.LogError(_logPrefix + "No location permissions! This is why observer won't receive updates.");
                    }
                }
                catch (Exception permEx)
                {
                    Debug.LogWarning(_logPrefix + $"Could not verify permissions: {permEx.Message}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError(_logPrefix + $"Failed to add location observer: {ex.Message}\n{ex.StackTrace}");
            }

            GetLastLocation();
        }

        private void GetLastLocation()
        {
            try
            {
                var callback = new GetLocationCallbackProxy((location, isValid) =>
                {
                    if (isValid)
                    {
                        EnqueueOnMainThread(() =>
                        {
                            LocationUpdated?.Invoke(location);
                        });
                    }
                });

                _locationProvider.Call<AndroidJavaObject>(_getLastLocationMethodName, callback);
            }
            catch (Exception ex)
            {
                Debug.LogError(_logPrefix + $"getLastLocation failed: {ex.Message}");
            }
        }

        private void EnqueueOnMainThread(Action action)
        {
            lock (_queueLock)
            {
                _mainThreadQueue.Enqueue(action);
            }
        }
        
        private void GetAccuracyLevel()
        {
            var accuracyLevel = new AndroidJavaClass(_accuracyLevelClassName);
            var accuArray = accuracyLevel.CallStatic<AndroidJavaObject>(_valuesMethodName);
            var convertedArray = AndroidJNIHelper.ConvertFromJNIArray<AndroidJavaObject[]>(accuArray.GetRawObject());
            foreach (var javaEnumObject in convertedArray)
            {
                var enumString = javaEnumObject.Call<string>(_toStringMethodName);
                if (enumString == _mapboxLocationSettings.AccuracyLevel.ToString())
                {
                    _accuracyLevelHigh = javaEnumObject;
                    break;
                }
            }
        }

        private AndroidJavaObject GetProviderType()
        {
            var accuracyLevel = new AndroidJavaClass(_deviceLocationProviderTypeClassName);
            var accuArray = accuracyLevel.CallStatic<AndroidJavaObject>(_valuesMethodName);
            var convertedArray = AndroidJNIHelper.ConvertFromJNIArray<AndroidJavaObject[]>(accuArray.GetRawObject());
            foreach (var javaEnumObject in convertedArray)
            {
                var enumString = javaEnumObject.Call<string>(_toStringMethodName);
                if (enumString == _androidProviderTypeName)
                {
                    return javaEnumObject;
                }
            }

            return null;
        }

        private string GetJavaClassName(AndroidJavaObject obj)
        {
            if (obj == null)
                return "<null>";

            using var clazz = obj.Call<AndroidJavaObject>(_getClassMethodName);
            return clazz.Call<string>(_getNameMethodName);
        }
    }
}
#endif