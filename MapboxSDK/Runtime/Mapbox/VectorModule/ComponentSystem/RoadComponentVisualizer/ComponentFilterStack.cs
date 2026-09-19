using System;
using System.Collections.Generic;
using Mapbox.VectorModule.Filters;
using UnityEngine;

namespace Mapbox.VectorModule.ComponentSystem.RoadComponentVisualizer
{
    [Serializable]
    public class ComponentFilterStack
    {
        [SerializeReference]
        public List<RoadFilter> Filters = new List<RoadFilter>();
        public LayerFilterCombinerOperationType Type;

        public ComponentFilterStack()
        {

        }

        public void Initialize()
        {
            foreach (var filter in Filters)
            {
                filter.Initialize();
            }
        }

        public bool Try(RoadFeatureUnity feature)
        {
            if (Filters == null || Filters.Count == 0)
                return true;

            switch (Type)
            {
                case LayerFilterCombinerOperationType.Any:
                    for (int i = 0; i < Filters.Count; i++)
                    {
                        if (Filters[i].Try(feature)) return true;
                    }
                    return false;
                case LayerFilterCombinerOperationType.All:
                    for (int i = 0; i < Filters.Count; i++)
                    {
                        if (!Filters[i].Try(feature)) return false;
                    }
                    return true;
                case LayerFilterCombinerOperationType.None:
                    for (int i = 0; i < Filters.Count; i++)
                    {
                        if (Filters[i].Try(feature)) return false;
                    }
                    return true;
                default:
                    return false;
            }
        }
    }
}