using System;
using UnityEngine;
using UnityEngine.Scripting;

namespace Mapbox.LocationModule.MapboxLocation
{
    public class MapboxLocationPermissionsListenerProxy : AndroidJavaProxy
    {
        private readonly Action<string[]> _onExplanation;
        private readonly Action<bool> _onResult;

        public MapboxLocationPermissionsListenerProxy(Action<string[]> onExplanation, Action<bool> onResult) : base("com.mapbox.android.core.permissions.PermissionsListener")
        {
            _onExplanation = onExplanation;
            _onResult = onResult;
        }

        [Preserve]
        public void onExplanationNeeded(AndroidJavaObject permissionsToExplain)
        {
            if (permissionsToExplain != null)
            {
                int size = permissionsToExplain.Call<int>("size");
                string[] permissions = new string[size];
                for (int i = 0; i < size; i++)
                {
                    permissions[i] = permissionsToExplain.Call<string>("get", i);
                }
                _onExplanation?.Invoke(permissions);
            }
        }

        [Preserve]
        public void onPermissionResult(bool granted)
        {
            _onResult?.Invoke(granted);
        }
    }
}