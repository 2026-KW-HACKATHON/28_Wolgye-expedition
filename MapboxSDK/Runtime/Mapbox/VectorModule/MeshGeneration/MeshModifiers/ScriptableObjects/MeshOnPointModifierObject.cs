// using System.Collections.Generic;
// using System.ComponentModel;
// using Mapbox.BaseModule.Data;
// using Mapbox.BaseModule.Unity;
// using Mapbox.VectorModule.MeshGeneration.Unity;
// using UnityEngine;
// using Random = System.Random;
//
// namespace Mapbox.VectorModule.MeshGeneration.MeshModifiers.ScriptableObjects
// {
// 	[DisplayName("Mesh On Point Modifier")]
// 	[CreateAssetMenu(menuName = "Mapbox/Modifiers/Mesh Modifiers/Mesh On Point Modifier")]
// 	public class MeshOnPointModifierObject : ScriptableMeshModifierObject
// 	{
// 		public MeshOnPointSettings MeshOnPointSettings;
// 	
// 		private MeshOnPointModifier _meshOnPointModifierImplementation;
// 		private Random _random;
// 		// private List<List<Vector2>> _uvs = new List<List<Vector2>>()
// 		// {
// 		// 	new List<Vector2>(),
// 		// 	new List<Vector2>()
// 		// };
// 		
// 		protected override MeshModifier _meshModifierImplementation => _meshOnPointModifierImplementation;
//
// 		public override void Initialize()
// 		{
// 			_random = new System.Random();
// 			var prefabSet = MeshOnPointSettings.PrefabSet;
// 			prefabSet.MeshDatas = new List<MeshData>(prefabSet.Prefabs.Count);
// 			foreach (var prefab in prefabSet.Prefabs)
// 			{
// 				var mesh = prefab.GetComponent<MeshFilter>().sharedMesh;
// 				var meshData = new MeshData();
//
// 				var newVertices = new List<Vector3>();
// 				var newNormals = new List<Vector3>();
//
// 				var meshVertes = mesh.vertices;
// 				var meshNormals = mesh.normals;
// 				var rotation = prefab.transform.rotation;
// 				var pos = prefab.transform.position;
//
// 				for (int i = 0; i < meshVertes.Length; i++)
// 				{
// 					newVertices.Add(rotation * ((pos + meshVertes[i]) * prefab.transform.localScale.x));
// 					newNormals.Add(rotation * meshNormals[i]);
// 				}
// 				meshData.Vertices = newVertices;
// 				meshData.Normals = newNormals;
//
// 				for (int i = 0; i < mesh.subMeshCount; i++)
// 				{
// 					meshData.Triangles.Add(new List<int>());
// 					mesh.GetTriangles(meshData.Triangles[i], i);
// 				}
// 				for (int i = 0; i < mesh.subMeshCount; i++)
// 				{
// 					meshData.UV.Add(new List<Vector2>());
// 					mesh.GetUVs(i, meshData.UV[i]);
// 				}
// 				prefabSet.MeshDatas.Add(meshData);
// 			}
//
// 			if (prefabSet.ScaleVariety.x != 1 || prefabSet.ScaleVariety.y != 1)
// 				MeshOnPointSettings.ScaleObjects = true;
//
// 			if (prefabSet.RotationVariety.x != 0 || prefabSet.RotationVariety.y != 0)
// 				MeshOnPointSettings.RotateObjects = true;
//
// 			// foreach (var uv in PrefabSet.MeshDatas[0].UV[0])
// 			// {
// 			// 	_uvs[0].Add(uv/2);
// 			// 	_uvs[1].Add(new Vector2((uv.x / 2) + .5f, (uv.y / 2) + .5f));
// 			// }
// 		}
// 	
// 		public override void ConstructModifier(UnityContext unityContext)
// 		{
// 			Initialize();
// 			_meshOnPointModifierImplementation = new MeshOnPointModifier(MeshOnPointSettings);
// 		}
// 	}
// }