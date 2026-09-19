using System;
using System.Collections.Generic;
using Mapbox.BaseModule.Data.Tiles;
using Mapbox.BaseModule.Data.Vector2d;
using Mapbox.BaseModule.Utilities;
using Mapbox.BaseModule.Utilities.Attributes;
using UnityEngine;

namespace Mapbox.BaseModule.Map
{
    [Serializable]
    public class MapInformation : IMapInformation
    {
        private LatitudeLongitude _latitudeLongitude;
        private Vector2d _centerMercator;
        [Tooltip("Initial camera position pitch angle")]
        [SerializeField] private float _pitch;
        [Tooltip("Initial camera position bearing")]
        [SerializeField] private float _bearing;
        [Tooltip("Initial world scale")]
        [SerializeField] protected float _scale = 1000;
        
        [Tooltip("World center latitude longitude as comma separated values")]
        [Geocode] [SerializeField] protected string _latitudeLongitudeString;
        [Tooltip("Initial zoom value for the map")]
        [SerializeField] private float _zoom;
        protected bool _isInitialized = false;
        
        public MapInformation(string latitudeLongitudeString)
        {
            _latitudeLongitudeString = latitudeLongitudeString;
        }
        
        public MapInformation(string latitudeLongitudeString, float pitch, float scale, float zoom)
        {
            _latitudeLongitudeString = latitudeLongitudeString;
            _pitch = pitch;
            _scale = scale;
            _zoom = zoom;
        }
        
        public virtual void Initialize()
        {
            if(_isInitialized) return;
            Initialize(Conversions.StringToLatLon(_latitudeLongitudeString));
        }
        
        public virtual void Initialize(LatitudeLongitude latitudeLongitude)
        {
            if(_isInitialized) return;

            SetLatitudeLongitude(latitudeLongitude);
            _isInitialized = true;
            QueryElevation = (tileId, x, y) => 0;
            // Reset terrain bounds to TerrainInfo defaults so a previous map's Min/Max
            // never bleeds into a fresh session. Field initializers already set defaults
            // on new MapInformation instances, but explicitly resetting here guards
            // against future paths that reuse an existing instance.
            Terrain.MinElevation = TerrainInfo.DefaultMinElevation;
            Terrain.MaxElevation = TerrainInfo.DefaultMaxElevation;
        }

        //PROPERTIES
        public LatitudeLongitude LatitudeLongitude => _latitudeLongitude;
        public float Pitch
        {
            get => _pitch;
            private set => _pitch = value;
        }
        public float Bearing
        {
            get => _bearing;
            private set => _bearing = value;
        }
        public virtual float Scale
        {
            get => _scale; 
            protected set => _scale = value;
        }
        
        public int AbsoluteZoom => (int) Math.Floor(Zoom);
        public virtual float Zoom
        {
            get => _zoom;
            private set => _zoom = value;
        }
        public Vector2d CenterMercator => _centerMercator;
        
        //METHODS
        public virtual float GetScaleFor(float zoomValue) => _scale;

        public virtual float GetLatitudeCompensationForLocation => Conversions.LatitudeElevationCompensation(_latitudeLongitude.Latitude);
        
        public void SetLatitudeLongitude(LatitudeLongitude latlng)
        {
            _latitudeLongitude = latlng;
            _latitudeLongitudeString = _latitudeLongitude.ToString();
            _centerMercator = Conversions.LatitudeLongitudeToWebMercator(LatitudeLongitude);
            LatitudeLongitudeChanged?.Invoke(this);
        }

        public virtual void SetInformation(LatitudeLongitude? latlng, float? zoom = null, float? pitch = null, float? bearing = null, float? scale = null)
        {
            if(latlng.HasValue) SetLatitudeLongitude(latlng.Value);
            if(zoom.HasValue) Zoom = zoom.Value;
            if (pitch.HasValue) Pitch = pitch.Value;
            if (bearing.HasValue) Bearing = bearing.Value;
            if (scale.HasValue && !Mathf.Approximately(scale.Value, Scale))
            {
                Scale = scale.Value;
                OnWorldScaleChanged();
            }

            if (SetView != null)
            {
                SetView(this);
            }

            if (ViewChanged != null)
            {
                ViewChanged(this);
            }
        }

        public Func<CanonicalTileId, float, float, float> QueryElevation { get; set; }
        public TerrainInfo Terrain { get; } = new TerrainInfo();
        public event Action<IMapInformation> SetView = (t) => {};
        public event Action<IMapInformation> ViewChanged = (t) => {};
        public event Action<IMapInformation> LatitudeLongitudeChanged = (t) => {};
        public event Action<IMapInformation> WorldScaleChanged = (t) => {};
        
        protected void OnWorldScaleChanged() => WorldScaleChanged?.Invoke(this);
    }
}