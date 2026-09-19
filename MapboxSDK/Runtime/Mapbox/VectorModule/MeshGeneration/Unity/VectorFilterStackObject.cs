using System;
using System.Collections.Generic;
using System.Linq;
using Mapbox.BaseModule.Utilities;
using Mapbox.VectorModule.Filters;
using UnityEngine;

namespace Mapbox.VectorModule.MeshGeneration.Unity
{
    [CreateAssetMenu(menuName = "Mapbox/Filters/Vector Filter Stack")]
    public class VectorFilterStackObject : ScriptableObject
    {
        public List<FilterBaseObject> Filters = new List<FilterBaseObject>(10);
        
        public LayerFilterCombinerOperationType Type;

        public VectorFilterStack GetCombiner()
        {
            return new VectorFilterStack()
            {
                Filters = Filters.Where(x => x != null).Select(x => x?.Filter).ToList(),
                Type = this.Type
            };
        }
    }
    
    [Serializable]
    public class VectorFilterStack : ILayerFeatureFilterComparer
    {
        public List<ILayerFeatureFilterComparer> Filters;
        public LayerFilterCombinerOperationType Type;

        public VectorFilterStack()
        {
            
        }

        public void Initialize()
        {
            
        }

        public bool Try(VectorFeatureUnity feature)
        {
            if (Filters == null || Filters.Count == 0)
                return true;

            switch (Type)
            {
                case LayerFilterCombinerOperationType.Any:
                    return Filters.Any(m => m.Try(feature));
                case LayerFilterCombinerOperationType.All:
                    return Filters.All(m => m.Try(feature));
                case LayerFilterCombinerOperationType.None:
                    return !Filters.Any(m => m.Try(feature));
                default:
                    return false;
            }
        }
    }
}