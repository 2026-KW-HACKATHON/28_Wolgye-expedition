using System;
using Mapbox.BaseModule.Data.Vector2d;
using Mapbox.BaseModule.Utilities;
using Mapbox.LocationModule;
using UnityEngine;
using UnityEngine.Scripting;

namespace Mapbox.LocationModule.MapboxLocation
{
    public class MapboxLocationObserverProxy : AndroidJavaProxy
    {
        private const string _logPrefix = "MAPBOX_UNITY_SDK: ";
        private const string _getLatitude = "getLatitude";
        private const string _getLongitude = "getLongitude";
        private const string _getAltitude = "getAltitude";
        private const string _getSpeed = "getSpeed";
        private const string _getBearing = "getBearing";
        private const string _get = "get";
        private const string _size = "size";

        public Action<Location> SendLocation;
        private readonly Action<Action> _enqueueOnMainThread;

        public MapboxLocationObserverProxy(Action<Action> enqueueOnMainThread, Action<Location> locationUpdated) : base("com.mapbox.common.location.LocationObserver")
        {
            _enqueueOnMainThread = enqueueOnMainThread;
            SendLocation =  locationUpdated;
        }

        [Preserve]
        public void onLocationUpdateReceived(AndroidJavaObject locations)
        {
            if (locations == null)
            {
                Debug.LogWarning(_logPrefix + "locations list is null");
                return;
            }

            try
            {
                var size = locations.Call<int>(_size);
                for (var i = 0; i < size; i++)
                {
                    var loc = locations.Call<AndroidJavaObject>(_get, i);

                    if (loc == null)
                    {
                        Debug.LogWarning(_logPrefix + $"Location at index {i} is null");
                        continue;
                    }

                    var lat = loc.Call<double>(_getLatitude);
                    var lon = loc.Call<double>(_getLongitude);

                    // Get optional properties - Mapbox returns @Nullable Double for these
                    // double? altitude = null;
                    // float? speed = null;
                    // float? bearing = null;
                    //
                    // // Get altitude - returns null if not available
                    // var altitudeObj = loc.Call<AndroidJavaObject>(_getAltitude);
                    // if (altitudeObj != null)
                    // {
                    //     altitude = altitudeObj.Call<double>("doubleValue");
                    // }
                    //
                    // // Get speed - returns null if not available
                    // var speedObj = loc.Call<AndroidJavaObject>(_getSpeed);
                    // if (speedObj != null)
                    // {
                    //     speed = (float)speedObj.Call<double>("doubleValue");
                    // }
                    //
                    // // Get bearing - returns null if not available
                    // var bearingObj = loc.Call<AndroidJavaObject>(_getBearing);
                    // if (bearingObj != null)
                    // {
                    //     bearing = (float)bearingObj.Call<double>("doubleValue");
                    // }

                    var location = new Location
                    {
                        LatitudeLongitude = new LatitudeLongitude(lat, lon),
                        TimestampDevice = UnixTimestampUtils.To(DateTime.UtcNow),
                        IsLocationUpdated = true,
                        IsLocationServiceEnabled = true,
                        //Altitude = altitude,
                        //SpeedMetersPerSecond = speed,
                        //Bearing = bearing
                    };

                    // Invoke on main thread
                    _enqueueOnMainThread?.Invoke(() =>
                    {
                        SendLocation?.Invoke(location);
                    });
                }
            }
            catch (Exception ex)
            {
                Debug.LogError(_logPrefix + $"Error in onLocationUpdateReceived: {ex.Message}\n{ex.StackTrace}");
            }
        }

    }
}