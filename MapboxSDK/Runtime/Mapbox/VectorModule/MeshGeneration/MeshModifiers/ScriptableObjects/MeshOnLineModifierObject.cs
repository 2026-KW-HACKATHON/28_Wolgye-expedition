// using System;
// using System.ComponentModel;
// using Mapbox.BaseModule.Unity;
// using Mapbox.VectorModule.MeshGeneration.Unity;
// using UnityEngine;
//
// namespace Mapbox.VectorModule.MeshGeneration.MeshModifiers.ScriptableObjects
// {
// 	[DisplayName("Mesh On Line Modifier")]
// 	[CreateAssetMenu(menuName = "Mapbox/Modifiers/Mesh Modifiers/Mesh On Line Modifier")]
// 	public class MeshOnLineModifierObject : ScriptableMeshModifierObject
// 	{
// 		public PrefabSet PrefabSet;
// 		[NonSerialized] private MeshOnLineModifier _meshOnLineModifierImplementation;
// 		protected override MeshModifier _meshModifierImplementation => _meshOnLineModifierImplementation;
//
// 		public override void ConstructModifier(UnityContext unityContext)
// 		{
// 			_meshOnLineModifierImplementation = new MeshOnLineModifier(PrefabSet);
// 			_meshOnLineModifierImplementation.Initialize();
// 		}
//
// 		public override void Initialize()
// 		{
// 			
// 		}
// 	}
// }