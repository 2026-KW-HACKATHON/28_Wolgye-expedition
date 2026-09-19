using System;
using System.Collections.Generic;
using System.Linq;
using Mapbox.BaseModule.Map;
using Mapbox.BaseModule.Unity;
using Mapbox.BaseModule.Utilities;
using UnityEngine;

namespace Mapbox.VectorModule.MeshGeneration.GameObjectModifiers
{
	[Serializable]
	public class PrefabModifierSettings
	{
		public GameObject Prefab;
		[Tooltip("Scale prefab up or down as if it's size is in mercator units (close to real world meters).")]
		public bool ScalePrefabToWorld = true;
	}

	[Serializable]
	public class PrefabModifier : GameObjectModifier
	{
		public Action<GameObject> PrefabCreated = (s) => { };

		private UnityContext _unityContext;
		private Dictionary<VectorEntity, GameObject> _objects;
		private readonly PrefabModifierSettings _settings;
		
		public PrefabModifier(UnityContext unityContext, PrefabModifierSettings settings)
		{
			_unityContext = unityContext;
			_settings = settings;
			if (_objects == null)
			{
				_objects = new Dictionary<VectorEntity, GameObject>();
			}
		}
		
		public override void Run(VectorEntity ve, IMapInformation mapInformation)
		{
			if (_settings.Prefab == null)
				return;
			
			var tileSize = Conversions.TileEdgeSizeInMercator(ve.Feature.TileId);
			//we first move position to (0-1) range, then scale it up to tile size
			var met = (ve.Feature.Points[0][0] / mapInformation.Scale) * tileSize;
			
			GameObject go;
			
			if (_objects.ContainsKey(ve))
			{
				go = _objects[ve];
				go.name = ve.Feature.Data.Id.ToString();
				go.transform.localPosition = met;
				return;
			}
			
			go = GameObject.Instantiate(_settings.Prefab, ve.Transform);
			go.transform.localPosition = met;
			if (_settings.ScalePrefabToWorld)
			{
				go.transform.localScale = Constants.Math.Vector3One * 1 / mapInformation.Scale;
			}
			
			_objects.Add(ve, go);
			go.name = ve.Feature.Data.Id.ToString();
			

			PrefabCreated(go);
		}

		public override void Finalize(VectorEntity entity)
		{
			base.Finalize(entity);
			if (_objects.TryGetValue(entity, out var poiGo))
			{
				GameObject.Destroy(poiGo);
				_objects.Remove(entity);
			}
		}
	}
}
