using System;
using UnityEngine;
using UnityEngine.Scripting;

namespace Mapbox.LocationModule.MapboxLocation
{
    public class MapboxLocationServiceObserverProxy : AndroidJavaProxy
    {
        private const string _logPrefix = "MAPBOX_UNITY_SDK: ";

        private readonly Action<MapboxLocationServiceStatus> _authorizationChanged;
        private readonly Action<AccuracyAuthorization> _accuracyAuthorizationChanged;
        private readonly Action<bool> _availabilityChanged;
        private readonly Action<Action> _enqueueOnMainThread;

        public MapboxLocationServiceObserverProxy(
            Action<MapboxLocationServiceStatus> authorizationChanged,
            Action<AccuracyAuthorization> accuracyAuthorizationChanged,
            Action<bool> availabilityChanged,
            Action<Action> enqueueOnMainThread) : base("com.mapbox.common.location.LocationServiceObserver")
        {
            _authorizationChanged = authorizationChanged;
            _accuracyAuthorizationChanged = accuracyAuthorizationChanged;
            _availabilityChanged = availabilityChanged;
            _enqueueOnMainThread = enqueueOnMainThread;
        }


        [Preserve]
        public void onAvailabilityChanged(bool isAvailable)
        {
            _enqueueOnMainThread?.Invoke(() => _availabilityChanged?.Invoke(isAvailable));
        }

        [Preserve]
        public void onPermissionStatusChanged(AndroidJavaObject permission)
        {
            if (permission == null)
            {
                Debug.LogError(_logPrefix + "onPermissionStatusChanged received null");
                return;
            }

            int ordinal = permission.Call<int>("ordinal");
            var status = (MapboxLocationServiceStatus)ordinal;
            _enqueueOnMainThread?.Invoke(() => _authorizationChanged?.Invoke(status));
        }

        [Preserve]
        public void onAccuracyAuthorizationChanged(AndroidJavaObject authorization)
        {
            if (authorization == null)
            {
                Debug.LogError(_logPrefix + "onAccuracyAuthorizationChanged received null");
                return;
            }

            int ordinal = authorization.Call<int>("ordinal");
            var accuracy = (AccuracyAuthorization)ordinal;
            _enqueueOnMainThread?.Invoke(() => _accuracyAuthorizationChanged?.Invoke(accuracy));
        }
    }
}