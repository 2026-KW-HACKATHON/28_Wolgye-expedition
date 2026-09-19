using System;
using System.ComponentModel;
using Mapbox.BaseModule.Utilities;
using UnityEngine;

namespace Mapbox.VectorModule.Filters
{
    [CreateAssetMenu(menuName = "Mapbox/Filters/Geometry Type Filter")]
    [DisplayName("Geometry Type Filter")]
    public class GeometryTypeFilterObject : FilterBaseObject
    {
        [SerializeField] private GeometryTypeEnum Value;
        [NonSerialized] private GeometryTypeFilter _filter;

        public override ILayerFeatureFilterComparer Filter
        {
            get
            {
                if (_filter == null)
                    _filter = new GeometryTypeFilter(Value);
                return _filter;
            }
        }
    }
    
    [Serializable]
    public class GeometryTypeFilter : FilterBase
    {
        private GeometryTypeEnum _value;

        public GeometryTypeFilter(GeometryTypeEnum value)
        {
            _value = value;
        }

        public override bool Try(VectorFeatureUnity feature)
        {
            //this is a slightly cheesy way to negate by a flag
            //so right side is simply negated by the first part and equality check

            var geoType = feature.Data.GeometryType.ToString().ToLowerInvariant();

            switch (geoType)
            {
                case "polygon":
                case" multipolygon":
                    return (_value == GeometryTypeEnum.Polygon);
                    break;
                case "linestring":
                case "multilinestring":
                    return (_value == GeometryTypeEnum.Line);
                    break;
                case "point":
                case "multipoint":
                    return (_value == GeometryTypeEnum.Point);
                    break;
            }

            return false;
        }
    }

    public enum GeometryTypeEnum
    {
        Polygon,
        Line,
        Point
    }


    
}