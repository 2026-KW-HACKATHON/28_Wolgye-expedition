using System;
using System.ComponentModel;
using Mapbox.BaseModule.Utilities;
using UnityEngine;

namespace Mapbox.VectorModule.Filters
{
    [CreateAssetMenu(menuName = "Mapbox/Filters/Has Property Filter")]
    [DisplayName("Has Property Filter")]
    public class HasPropertyFilterObject : FilterBaseObject
    {
        public string PropertyName;
        [NonSerialized] private HasPropertyFilter _filter;

        public override ILayerFeatureFilterComparer Filter
        {
            get
            {
                if (_filter == null)
                    _filter = new HasPropertyFilter(PropertyName);
                return _filter;
            }
        }
    }
    
    [Serializable]
    public class HasPropertyFilter : FilterBase
    {
        private string _value;

        public HasPropertyFilter(string propertyName)
        {
            _value = propertyName;
        }

        public override bool Try(VectorFeatureUnity feature)
        {
            return feature.Properties.ContainsKey(_value);
        }
    }
}