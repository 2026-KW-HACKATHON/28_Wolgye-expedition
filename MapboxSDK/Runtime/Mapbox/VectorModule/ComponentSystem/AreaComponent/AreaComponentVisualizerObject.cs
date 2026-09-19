using System;
using System.Collections.Generic;
using System.ComponentModel;
using Mapbox.BaseModule.Map;
using Mapbox.BaseModule.Unity;
using Mapbox.VectorModule.ComponentSystem.BuildingComponentVisualizer;
using Mapbox.VectorModule.Unity;
using UnityEngine;

namespace Mapbox.VectorModule.ComponentSystem.AreaComponent
{
    [DisplayName("Area Component")]
    [CreateAssetMenu(menuName = "Mapbox/Layer Visualizers/Area Component Visualizer")]
    public class AreaComponentVisualizerObject : LayerVisualizerConstructor, IPerformanceLayerVisualizer
    {
        public AreaComponentSettings Settings;
        public bool IsActive = true;

        private AreaComponentVisualizer _layerVisualizer;
		
        public IVectorLayerVisualizer GetLayerVisualizer()
        {
            return _layerVisualizer;
        }
		
        public override IVectorLayerVisualizer ConstructLayerVisualizer(IMapInformation mapInformation, UnityContext unityContext)
        {
            Settings.StyleDictionary = new Dictionary<string, AreaStyle>();
            var index = 0;
            foreach (var areaStyle in Settings.Styles)
            {
                areaStyle.Index = index;
                Settings.StyleDictionary.Add(areaStyle.ClassName, areaStyle);
                index++;
            }
            
            _layerVisualizer = new AreaComponentVisualizer(Settings.LayerName, mapInformation, unityContext, Settings);
            _layerVisualizer.Active = IsActive;

            _layerVisualizer.OnVectorMeshCreated += OnVectorMeshCreated;
            _layerVisualizer.OnVectorMeshDestroyed += OnVectorMeshDestroyed;
			
            return _layerVisualizer;
        }

        public Action<GameObject> OnVectorMeshCreated = list => { };
        public Action<GameObject> OnVectorMeshDestroyed = go => { };
    }
    
    [Serializable]
    public class AreaComponentSettings
    {
        public string LayerName;
        [Tooltip("Vertical offset to be applied to the road component.")]
        public float GameObjectOffset;
        public Dictionary<string, AreaStyle> StyleDictionary = new Dictionary<string, AreaStyle>();
        public List<AreaStyle> Styles;
    }
    
    [Serializable]
    public class AreaStyle
    {
        [NonSerialized] public int Index;
        public string ClassName;
        public Material Material;
        public float PushUp;
    }
}