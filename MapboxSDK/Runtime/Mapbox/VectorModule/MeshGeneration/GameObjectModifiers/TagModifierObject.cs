using System.ComponentModel;
using Mapbox.BaseModule.Unity;
using Mapbox.BaseModule.Utilities.Attributes;
using Mapbox.VectorModule.MeshGeneration.Unity;
using UnityEngine;

namespace Mapbox.VectorModule.MeshGeneration.GameObjectModifiers
{
    [DisplayName("Tag Modifier")]
    [CreateAssetMenu(menuName = "Mapbox/Modifiers/GameObject Modifiers/Tag Modifier")]
    public class TagModifierObject : ScriptableGameObjectModifierObject
    {
        [GameObjectTag]public string tag;
        
        private TagModifier _prefabModifierImplementation;
        protected override GameObjectModifier _gameObjectModifierImplementation => _prefabModifierImplementation;

        public override void ConstructModifier(UnityContext unityContext)
        {
            _prefabModifierImplementation = new TagModifier(tag);
        }
    }
}