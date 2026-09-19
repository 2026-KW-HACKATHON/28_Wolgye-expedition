using System;
using Mapbox.BaseModule.Data.Tiles;
using Mapbox.BaseModule.Data.Vector2d;
using Mapbox.BaseModule.Utilities;
using Mapbox.BaseModule.Utilities.Attributes;
using UnityEngine;

namespace Mapbox.BaseModule.Map
{
    [Serializable]
    public class DynamicScalingMapInformation : MapInformation
    {
        private float _initialScale;
        [SerializeField] private bool _useDynamicScaling = false;
        [SerializeField] private AnimationCurveContainer ScaleCurve;

        public override float Scale
        {
            get => _initialScale * ScaleCurve.Evaluate(Zoom);
            protected set => _scale = value;
        }
        
        public DynamicScalingMapInformation(string latitudeLongitudeString, float initialScale, bool useDynamicScaling, AnimationCurveContainer scaleCurve) : base(latitudeLongitudeString)
        {
            _initialScale = initialScale;
            _useDynamicScaling = useDynamicScaling;
            ScaleCurve = scaleCurve;
        }

        public override void Initialize()
        {
            if(_isInitialized) return;
            Initialize(Conversions.StringToLatLon(_latitudeLongitudeString));
        }
        
        public override void Initialize(LatitudeLongitude latitudeLongitude)
        {
            if(_isInitialized) return;
            
            SetLatitudeLongitude(latitudeLongitude);
            _initialScale = _scale;
            if (_useDynamicScaling)
            {
                Scale = _initialScale * ScaleCurve.Evaluate(Zoom);
            }
            _isInitialized = true;
        }

        public override void SetInformation(LatitudeLongitude? latlng, float? zoom = null, float? pitch = null, float? bearing = null,
            float? scale = null)
        {
            var worldScaleChanged = zoom.HasValue && !Mathf.Approximately(zoom.Value, Zoom);
            base.SetInformation(latlng, zoom, pitch, bearing, scale);

            if (worldScaleChanged)
            {
                OnWorldScaleChanged();
            }
        }


        public override float GetScaleFor(float zoomValue) => Scale = _initialScale * ScaleCurve.Evaluate(zoomValue);
    }
}