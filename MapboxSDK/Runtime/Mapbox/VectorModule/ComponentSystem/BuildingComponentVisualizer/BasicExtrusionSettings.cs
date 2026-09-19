using System;
using Mapbox.BaseModule.Data.Interfaces;
using Mapbox.VectorModule.MeshGeneration.MeshModifiers;
using UnityEngine;

namespace Mapbox.VectorModule.ComponentSystem.BuildingComponentVisualizer
{
    public interface IHeightFilter
    {
        void FilterHeight(ref float height, ref float minHeight);
    }
    
    [Serializable]
    public class BasicExtrusionSettings : IHeightFilter
    {
        public ExtrusionOptions ExtrusionOptions = new ExtrusionOptions();
        public void FilterHeight(ref float height, ref float minHeight)
        {
            switch (ExtrusionOptions.extrusionType)
            {
                case ExtrusionType.AbsoluteHeight:
                    height = ExtrusionOptions.maximumHeight;
                    minHeight = 0;
                    break;
                case ExtrusionType.MaxHeight:
                    height = Mathf.Min(ExtrusionOptions.maximumHeight, height);
                    minHeight = 0;
                    break;
                case ExtrusionType.MinHeight:
                    height = Mathf.Max(ExtrusionOptions.minimumHeight, height);
                    minHeight = 0;
                    break;
                case ExtrusionType.RangeHeight:
                    height = Mathf.Clamp(height, ExtrusionOptions.minimumHeight, ExtrusionOptions.maximumHeight);
                    minHeight = 0;
                    break;
                default:
                    break;
            }
            
            height *= ExtrusionOptions.extrusionScaleFactor;
            minHeight *= ExtrusionOptions.extrusionScaleFactor;
        }   
    }
    
    [Serializable]
    public class ChamferSettings : IHeightFilter
    {
        public float OffsetInMeters;
        public ExtrusionOptions ExtrusionOptions = new ExtrusionOptions();
        public void FilterHeight(ref float height, ref float minHeight)
        {
            switch (ExtrusionOptions.extrusionType)
            {
                case ExtrusionType.AbsoluteHeight:
                    height = ExtrusionOptions.maximumHeight;
                    minHeight = 0;
                    break;
                case ExtrusionType.MaxHeight:
                    height = Mathf.Min(ExtrusionOptions.maximumHeight, height);
                    minHeight = 0;
                    break;
                case ExtrusionType.MinHeight:
                    height = Mathf.Max(ExtrusionOptions.minimumHeight, height);
                    minHeight = 0;
                    break;
                case ExtrusionType.RangeHeight:
                    height = Mathf.Clamp(height, ExtrusionOptions.minimumHeight, ExtrusionOptions.maximumHeight);
                    minHeight = 0;
                    break;
                case ExtrusionType.None:
                case ExtrusionType.PropertyHeight:
                default:
                    break;
            }
            
            height *= ExtrusionOptions.extrusionScaleFactor;
            minHeight *= ExtrusionOptions.extrusionScaleFactor;
        }
    }

    [Serializable]
    public class ExtrusionOptions
    {
        public ExtrusionType extrusionType = ExtrusionType.PropertyHeight;
        [Tooltip("Property name in feature layer to use for extrusion.")]
        public string propertyName = "height";
        public float minimumHeight = 0f;
        public float maximumHeight = 1000f;
        [Tooltip("Scale factor to multiply the extrusion value of the feature.")]
        public float extrusionScaleFactor = 1f;
    }
}