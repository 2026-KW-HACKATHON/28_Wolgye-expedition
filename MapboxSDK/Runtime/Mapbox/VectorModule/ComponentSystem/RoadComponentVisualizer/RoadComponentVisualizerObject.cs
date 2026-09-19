using System;
using System.Collections.Generic;
using System.ComponentModel;
using Mapbox.BaseModule.Map;
using Mapbox.BaseModule.Unity;
using Mapbox.VectorModule.ComponentSystem.BuildingComponentVisualizer;
using Mapbox.VectorModule.Filters;
using Mapbox.VectorModule.Unity;
using UnityEngine;

namespace Mapbox.VectorModule.ComponentSystem.RoadComponentVisualizer
{
    [DisplayName("Road Component")]
    [CreateAssetMenu(menuName = "Mapbox/Layer Visualizers/Road Component Visualizer")]
    public class RoadComponentVisualizerObject : LayerVisualizerConstructor, IPerformanceLayerVisualizer
    {
        public RoadComponentSettings Settings;
        public bool IsActive = true;

        private RoadComponentVisualizer _layerVisualizer;
		
        public IVectorLayerVisualizer GetLayerVisualizer()
        {
            return _layerVisualizer;
        }
		
        public override IVectorLayerVisualizer ConstructLayerVisualizer(IMapInformation mapInformation, UnityContext unityContext)
        {
            Settings.RoadStyleSheet.Initialize();
            _layerVisualizer = new RoadComponentVisualizer("road", mapInformation, unityContext, Settings);
            _layerVisualizer.Active = IsActive;

            _layerVisualizer.OnVectorMeshCreated += OnVectorMeshCreated;
            _layerVisualizer.OnVectorMeshDestroyed += OnVectorMeshDestroyed;
			
            return _layerVisualizer;
        }

        public Action<GameObject> OnVectorMeshCreated = list => { };
        public Action<GameObject> OnVectorMeshDestroyed = go => { };
    }

    [Serializable]
    public class RoadComponentSettings
    {
        [Tooltip("Vertical offset to be applied to the road component.")]
        public float GameObjectOffset;
        [Tooltip("Road segments will be offset by a small amount to prevent z-fighting issues.")]
        public float RandomOffsetRange = 0.0001f;
        public RoadStyleSheet RoadStyleSheet;
        
        public RoadComponentSettings()
        {
        }
    }
}