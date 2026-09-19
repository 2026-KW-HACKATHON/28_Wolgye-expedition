using System.ComponentModel;
using Mapbox.BaseModule.Unity;
using Mapbox.VectorModule.MeshGeneration.Unity;
using UnityEngine;

namespace Mapbox.VectorModule.MeshGeneration.GameObjectModifiers
{
	[DisplayName("Collider Modifier")]
	[CreateAssetMenu(menuName = "Mapbox/Modifiers/GameObject Modifiers/Collider Modifier")]
	public class ColliderModifierObject : ScriptableGameObjectModifierObject
	{
		private ColliderModifier _prefabModifierImplementation;
		protected override GameObjectModifier _gameObjectModifierImplementation => _prefabModifierImplementation;

		public override void ConstructModifier(UnityContext unityContext)
		{
			_prefabModifierImplementation = new ColliderModifier();
		}
	}
}