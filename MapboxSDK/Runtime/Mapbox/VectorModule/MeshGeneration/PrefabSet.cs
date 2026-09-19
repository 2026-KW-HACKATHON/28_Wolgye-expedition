using System;
using System.Collections.Generic;
using Mapbox.BaseModule.Data;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Mapbox.VectorModule.MeshGeneration
{
	[Serializable]
	public class PrefabSet
	{
		public List<GameObject> Prefabs;
		public Vector2 ScaleVariety = Vector2.one;
		public Vector2 RotationVariety = new Vector2(0, 360);

		public List<MeshData> MeshDatas;

		public GameObject GetRandom()
		{
			return Prefabs[(int)(Random.value * Prefabs.Count)];
		}
	}
}