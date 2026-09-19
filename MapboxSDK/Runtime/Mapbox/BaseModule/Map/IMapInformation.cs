using System;
using Mapbox.BaseModule.Data.Tiles;
using Mapbox.BaseModule.Data.Vector2d;

namespace Mapbox.BaseModule.Map
{
    public interface IMapInformation
    {
        LatitudeLongitude LatitudeLongitude { get; }
        float Pitch { get; }
        float Bearing { get; }
        float Scale { get; }
        int AbsoluteZoom { get; }
        float Zoom { get; }
        Vector2d CenterMercator { get; }
        void Initialize();
        void Initialize(LatitudeLongitude latitudeLongitude);
        void SetLatitudeLongitude(LatitudeLongitude latlng);
        void SetInformation(LatitudeLongitude? latlng, float? zoom = null, float? pitch = null, float? bearing = null, float? scale = null);
        event Action<IMapInformation> SetView;
        event Action<IMapInformation> ViewChanged; 
        event Action<IMapInformation> LatitudeLongitudeChanged;
        event Action<IMapInformation> WorldScaleChanged;
        Func<CanonicalTileId, float, float, float> QueryElevation { get; set; }
        TerrainInfo Terrain { get; }
        float GetLatitudeCompensationForLocation { get; }
        float GetScaleFor(float zoomValue);
    }
}