using System;
using Mapbox.BaseModule.Map;
using Mapbox.BaseModule.Utilities;
using UnityEngine;

namespace Mapbox.VectorModule.MeshGeneration.GameObjectModifiers
{
	[Serializable]
	public class ColliderModifier : GameObjectModifier
	{
		public override void Run(VectorEntity ve, IMapInformation mapInformation)
		{
			if (ve.Mesh.vertexCount > 0)
			{
				var meshCollider = ve.GameObject.GetComponent<MeshCollider>();
				if (meshCollider == null)
					meshCollider = ve.GameObject.AddComponent<MeshCollider>();
				meshCollider.sharedMesh = ve.Mesh;
			}
		}
	}
}
