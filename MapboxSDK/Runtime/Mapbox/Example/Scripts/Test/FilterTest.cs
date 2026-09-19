using Mapbox.BaseModule.Utilities;
using Mapbox.Example.Scripts.Map;
using Mapbox.VectorModule;
using Mapbox.VectorModule.Filters;
using UnityEngine;

namespace Mapbox.Example.Scripts.Test
{
    public class FilterTest : MonoBehaviour
    {
        private void Awake()
        {
            var mapBehaviour = FindObjectOfType<MapboxMapBehaviour>();
            mapBehaviour.Initialized += (map) =>
            {
                var mapVisualizer = map.MapVisualizer;
                if (mapVisualizer.TryGetLayerModule<VectorLayerModule>(out var vectorModule))
                {
                    if (vectorModule.TryGetLayerVisualizer("building", out var layerVisualizer))
                    {
                        foreach (var stack in layerVisualizer.GetModStacks)
                        {
                            stack.Value.Filters?.Filters.Add(new MyFilter());
                        }
                    }
                    else
                    {
                        Debug.Log("Can't find building layer visualizer");
                    }
                }
                else
                {
                    Debug.Log("Can't find layer module");
                }
            };
        }

        private class MyFilter : FilterBase
        {
            public override bool Try(VectorFeatureUnity feature)
            {
                return base.Try(feature);
            }
        }
    }
}
