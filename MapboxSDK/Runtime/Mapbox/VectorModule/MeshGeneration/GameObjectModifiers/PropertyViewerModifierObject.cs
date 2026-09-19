using System.ComponentModel;
using Mapbox.BaseModule.Unity;
using Mapbox.VectorModule.MeshGeneration.Unity;
using UnityEngine;

namespace Mapbox.VectorModule.MeshGeneration.GameObjectModifiers
{
    [DisplayName("Property Viewer Modifier")]
    [CreateAssetMenu(menuName = "Mapbox/Modifiers/GameObject Modifiers/Property Viewer Modifier")]
    public class PropertyViewerModifierObject : ScriptableGameObjectModifierObject
    {
        private PropertiesViewerModifier _propertyModifierImplementation;
        protected override GameObjectModifier _gameObjectModifierImplementation => _propertyModifierImplementation;
		
        public override void ConstructModifier(UnityContext unityContext)
        {
            _propertyModifierImplementation = new PropertiesViewerModifier();
        }
    }
}