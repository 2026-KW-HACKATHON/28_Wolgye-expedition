using System;
using System.ComponentModel;
using Mapbox.BaseModule.Data.Interfaces;
using Mapbox.BaseModule.Map;
using Mapbox.BaseModule.Unity;
using Mapbox.VectorModule.MeshGeneration.MeshModifiers;
using Mapbox.VectorModule.Unity;
using UnityEngine;

namespace Mapbox.VectorModule.ComponentSystem.BuildingComponentVisualizer
{
    [DisplayName("Building Component")]
    [CreateAssetMenu(menuName = "Mapbox/Layer Visualizers/Buildings Component Visualizer")]
    public class BuildingComponentVisualizerObject : LayerVisualizerConstructor, IPerformanceLayerVisualizer
    {
        public BuildingComponentSettings Settings;
        public bool IsActive = true;

        private BuildingComponentVisualizer _layerVisualizer;
		
        public IVectorLayerVisualizer GetLayerVisualizer()
        {
            return _layerVisualizer;
        }
		
        public override IVectorLayerVisualizer ConstructLayerVisualizer(IMapInformation mapInformation, UnityContext unityContext)
        {
            _layerVisualizer = new BuildingComponentVisualizer("building", mapInformation, unityContext, Settings);
            _layerVisualizer.Active = IsActive;

            _layerVisualizer.OnVectorMeshCreated += OnVectorMeshCreated;
            _layerVisualizer.OnVectorMeshDestroyed += OnVectorMeshDestroyed;
			
            return _layerVisualizer;
        }

        public Action<GameObject> OnVectorMeshCreated = list => { };
        public Action<GameObject> OnVectorMeshDestroyed = go => { };
    }

    [Serializable]
    public class BuildingComponentSettings
    {
        public bool EnableTerrainSnapping = false;
        public bool RoundBuildingCorners = false;
        public Material Material;

        public ChamferSettings ChamferExtrusionSettings;
        public BasicExtrusionSettings BasicExtrusionSettings;

        public BuildingComponentSettings()
        {
            EnableTerrainSnapping = false;
            ChamferExtrusionSettings = new ChamferSettings() { OffsetInMeters = 1 };
            BasicExtrusionSettings = new BasicExtrusionSettings();
        }
    }

    //necessary of editor script stuff
    public interface IPerformanceLayerVisualizer
    {
        
    }
}