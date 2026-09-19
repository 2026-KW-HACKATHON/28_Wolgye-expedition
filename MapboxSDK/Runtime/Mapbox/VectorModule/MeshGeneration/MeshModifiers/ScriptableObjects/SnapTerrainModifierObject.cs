using System.ComponentModel;
using Mapbox.BaseModule.Unity;
using Mapbox.VectorModule.MeshGeneration.Unity;
using UnityEngine;

namespace Mapbox.VectorModule.MeshGeneration.MeshModifiers.ScriptableObjects
{
	[DisplayName("Snap Terrain Modifier")]
	[CreateAssetMenu(menuName = "Mapbox/Modifiers/Mesh Modifiers/Snap Terrain Modifier")]
	public class SnapTerrainModifierObject : ScriptableMeshModifierObject, ISnapTerrainModifier
	{
		private SnapTerrainModifier _snapTerrainModifierImplementation;
		protected override MeshModifier _meshModifierImplementation => _snapTerrainModifierImplementation;

		public override void ConstructModifier(UnityContext unityContext)
		{
			_snapTerrainModifierImplementation = new SnapTerrainModifier();
		}
	}
}