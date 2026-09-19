using System;
using System.Collections.Generic;
using System.ComponentModel;
using Mapbox.VectorModule.Filters;

namespace Mapbox.VectorModule.ComponentSystem.RoadComponentVisualizer
{
    [Serializable]
    public abstract class RoadFilter
    {
        public RoadFilter()
        {
            
        }
        
        public abstract void Initialize();
        public abstract bool Try(RoadFeatureUnity feature);
    }
    
    [Serializable]
    [DisplayName("Class Filter")]
    public class RoadClassFilter : RoadFilter
    {
        public StringCheckOperation CheckOperation = StringCheckOperation.Equals;
        public string FilterString;
        public bool Invert;
        private HashSet<string> _types = new HashSet<string>();
        
        public override void Initialize()
        {
            FilterString = FilterString.ToLowerInvariant();
            if (CheckOperation == StringCheckOperation.Contains)
            {
                _types = new HashSet<string>();
                foreach (var s in FilterString.Split(','))
                {
                    _types.Add(s.Trim().ToLowerInvariant());
                }
            }
        }
        
        public override bool Try(RoadFeatureUnity feature)
        {
            if (feature.Class == null) return false;

            var result = false;
            if (CheckOperation == StringCheckOperation.Equals)
            {
                result = FilterString == feature.Class;
            }
            else if (CheckOperation == StringCheckOperation.Contains)
            {
                result = _types.Contains(feature.Class);
            }

            return !Invert ? result : !result;
        }
    }

    [Serializable]
    [DisplayName("Structure Filter")]
    public class RoadStructureFilter : RoadFilter
    {
        public StringCheckOperation CheckOperation = StringCheckOperation.Equals;
        public string FilterString;
        public bool Invert;
        private HashSet<string> _types = new HashSet<string>();

        public override void Initialize()
        {
            FilterString = FilterString.ToLowerInvariant();
            if (CheckOperation == StringCheckOperation.Contains)
            {
                _types = new HashSet<string>();
                foreach (var s in FilterString.Split(','))
                {
                    _types.Add(s.Trim().ToLowerInvariant());
                }
            }
        }

        public override bool Try(RoadFeatureUnity feature)
        {
            if (feature.Structure == null) return false;

            var result = false;
            if (CheckOperation == StringCheckOperation.Equals)
            {
                result = FilterString == feature.Structure;
            }
            else if (CheckOperation == StringCheckOperation.Contains)
            {
                result = _types.Contains(feature.Structure);
            }

            return !Invert ? result : !result;
        }
    }

    [Serializable]
    [DisplayName("Type Filter")]
    public class RoadTypeFilter : RoadFilter
    {
        public StringCheckOperation CheckOperation = StringCheckOperation.Equals;
        public string FilterString;
        public bool Invert;
        private HashSet<string> _types = new HashSet<string>();

        public override void Initialize()
        {
            FilterString = FilterString.ToLowerInvariant();
            if (CheckOperation == StringCheckOperation.Contains)
            {
                _types = new HashSet<string>();
                foreach (var s in FilterString.Split(','))
                {
                    _types.Add(s.Trim().ToLowerInvariant());
                }
            }
        }

        public override bool Try(RoadFeatureUnity feature)
        {
            if (feature.Type == null) return false;

            var result = false;
            if (CheckOperation == StringCheckOperation.Equals)
            {
                result = FilterString == feature.Type;
            }
            else if (CheckOperation == StringCheckOperation.Contains)
            {
                result = _types.Contains(feature.Type);
            }

            return !Invert ? result : !result;
        }
    }
}