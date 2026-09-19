using System.Collections.Generic;
using System.Linq;
using Mapbox.BaseModule.Data;
using Mapbox.BaseModule.Map;
using Mapbox.BaseModule.Unity;
using Mapbox.BaseModule.Utilities;
using Mapbox.VectorModule.MeshGeneration.GameObjectModifiers;
using Mapbox.VectorModule.MeshGeneration.MeshModifiers;
using UnityEngine;

namespace Mapbox.VectorModule.MeshGeneration.Unity
{
	[CreateAssetMenu(menuName = "Mapbox/Modifiers/Modifier Stack")]
	public class ModifierStackObject : ScriptableObject, IModifierStack
	{
		private ModifierStack _modifierStack;
		public ModifierStack GetModifierStack => _modifierStack;

		public ModifierStackSettings Settings;
		public VectorFilterStackObject Filters;
		public List<ScriptableMeshModifierObject> MeshModifiers = new List<ScriptableMeshModifierObject>();
		public List<ScriptableGameObjectModifierObject> GoModifiers = new List<ScriptableGameObjectModifierObject>();

		public void Initialize(UnityContext unityContext = null)
		{
			var filterCombiner = Filters == null ? null : Filters.GetCombiner();
			_modifierStack = new ModifierStack(Settings, filterCombiner)
			{
				MeshModifiers = MeshModifiers.Select(x => x as IMeshModifier).ToList(), 
				GoModifiers = GoModifiers.Select(x => x as IGameObjectModifier).ToList()
			};
			foreach (var modifier in MeshModifiers)
			{
				modifier.ConstructModifier(unityContext);
			}
				
			foreach (var modifier in GoModifiers)
			{
				modifier.ConstructModifier(unityContext);
			}
		}

		public MeshData RunMeshModifiers(VectorFeatureUnity feature, MeshData meshData, IMapInformation mapInfo)
		{
			return _modifierStack.RunMeshModifiers(feature, meshData, mapInfo);
		}

		public void RunGoModifiers(VectorEntity entity, IMapInformation mapInformation)
		{
			_modifierStack.RunGoModifiers(entity, mapInformation);
		}

		public void Finalize(VectorEntity entity)
		{
			_modifierStack.Finalize(entity);
		}
	}
}