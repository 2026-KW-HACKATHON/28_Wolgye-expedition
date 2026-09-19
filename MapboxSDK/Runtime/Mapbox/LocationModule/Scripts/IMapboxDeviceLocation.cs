using System;
using Mapbox.LocationModule.MapboxLocation;

namespace Mapbox.LocationModule
{
    public interface IMapboxDeviceLocation
    {
        event Action<Location> LocationUpdated;
        event Action<MapboxLocationServiceStatus> AuthorizationChanged;
        event Action<AccuracyAuthorization> AccuracyAuthorizationChanged;
        event Action<bool> AvailabilityChanged;
        void Update();
        void OnDestroy();
        void Initialize();
    }
}