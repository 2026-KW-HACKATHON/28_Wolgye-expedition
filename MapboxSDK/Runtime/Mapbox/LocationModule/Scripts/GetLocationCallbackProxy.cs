using System;
using Mapbox.BaseModule.Data.Vector2d;
using Mapbox.BaseModule.Utilities;
using UnityEngine;
using UnityEngine.Scripting;

namespace Mapbox.LocationModule
{
    public class GetLocationCallbackProxy : AndroidJavaProxy
    {
        private const string _logPrefix = "MAPBOX_UNITY_SDK: ";
        private const string _getLatitude =  "getLatitude";
        private const string _getLongitude = "getLongitude";
        
        private readonly Action<Location, bool> _callback;

        public GetLocationCallbackProxy(Action<Location, bool> callback) : base("com.mapbox.common.location.GetLocationCallback")
        {
            _callback = callback;
        }

        [Preserve]
        public void run(AndroidJavaObject locationObj)
        {
            if (locationObj == null)
            {
                _callback?.Invoke(default(Location), false);
                return;
            }

            try
            {
                double lat = locationObj.Call<double>(_getLatitude);
                double lon = locationObj.Call<double>(_getLongitude);

                Location location = new Location
                {
                    LatitudeLongitude = new LatitudeLongitude(lat, lon),
                    TimestampDevice = UnixTimestampUtils.To(DateTime.UtcNow)
                };

                _callback?.Invoke(location, true);
            }
            catch (Exception ex)
            {
                Debug.LogError(_logPrefix + $"GetLocationCallback error: {ex.Message}");
                _callback?.Invoke(default(Location), false);
            }
        }
    }
}