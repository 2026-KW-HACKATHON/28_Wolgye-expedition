using Mapbox.BaseModule.Unity;
using Mapbox.ImageModule.Terrain.Settings;
using Mapbox.ImageModule.Terrain.TerrainStrategies;
using UnityEngine;

namespace Mapbox.BaseModule.Utilities
{
	/// <summary>
	/// Terrain strategy used when no terrain module is present. Writes a shared 4-vertex
	/// flat quad into each tile's mesh and (optionally) shares that quad as a MeshCollider.
	/// Has no elevation sampling; ignores <c>SimplificationFactor</c>.
	/// </summary>
	public class FlatTerrainStrategy : TerrainStrategy
	{
		MeshDataArray _cachedQuad;
		private TerrainColliderOptions _colliderOptions;

		public int RequiredVertexCount => 4;

		public FlatTerrainStrategy()
		{
			BuildQuad();
		}

		/// <summary>
		/// Captures the subset of <see cref="ElevationLayerProperties"/> this strategy cares
		/// about (currently just collider options). Safe to pass <c>null</c>.
		/// </summary>
		public override void Initialize(ElevationLayerProperties elOptions)
		{
			if (elOptions != null)
			{
				_colliderOptions = elOptions.colliderOptions;
			}
		}

		/// <summary>
		/// Writes the flat quad into <paramref name="tile"/>'s mesh if not already present,
		/// and optionally attaches a MeshCollider backed by the same quad. The
		/// <paramref name="createElevatedMesh"/> flag is unused — flat terrain has no
		/// elevation.
		/// </summary>
		public override void RegisterTile(UnityMapTile tile, bool createElevatedMesh)
		{
			var meshFilter = tile.MeshFilter;
			if (meshFilter.sharedMesh.vertexCount != RequiredVertexCount)
			{

				var sharedMesh = tile.MeshFilter.sharedMesh;
				sharedMesh.Clear();

				sharedMesh.vertices = _cachedQuad.Vertices;
				sharedMesh.normals = _cachedQuad.Normals;
				for (var i = 0; i < _cachedQuad.Triangles.Count; i++)
				{
					var triangle = _cachedQuad.Triangles[i];
					sharedMesh.SetTriangles(triangle, i);
				}
				sharedMesh.uv = _cachedQuad.Uvs;
			}

			tile.MeshVertexCount = RequiredVertexCount;

			if (_colliderOptions != null && _colliderOptions.addCollider)
			{
				var meshCollider = tile.GetComponent<MeshCollider>();
				if (meshCollider == null)
				{
					meshCollider = tile.gameObject.AddComponent<MeshCollider>();
				}
				// The flat quad is already the collision surface; reuse it directly rather
				// than allocating a dedicated mesh as ElevatedTerrainStrategy does.
				meshCollider.sharedMesh = meshFilter.sharedMesh;
			}
		}

		private void BuildQuad()
		{
			var size = 1;

			//32
			//01
			var verts = new Vector3[4];
			var norms = new Vector3[4];
			verts[0] = new Vector3(0, 0, 0); //tile.TileScale * ((tile.Rect.Min - tile.Rect.Center).ToVector3xz());
			verts[1] = new Vector3(size, 0, 0); //tile.TileScale * (new Vector3((float)(tile.Rect.Max.x - tile.Rect.Center.x), 0, (float)(tile.Rect.Min.y - tile.Rect.Center.y)));
			verts[2] = new Vector3(size, 0, -size); //tile.TileScale * ((tile.Rect.Max - tile.Rect.Center).ToVector3xz());
			verts[3] = new Vector3(0, 0, -size); //tile.TileScale * (new Vector3((float)(tile.Rect.Min.x - tile.Rect.Center.x), 0, (float)(tile.Rect.Max.y - tile.Rect.Center.y)));
			norms[0] = Vector3.up; //Constants.Math.Vector3Up;
			norms[1] = Vector3.up; //Constants.Math.Vector3Up;
			norms[2] = Vector3.up; //Constants.Math.Vector3Up;
			norms[3] = Vector3.up; //Constants.Math.Vector3Up;

			var trilist = new int[6] { 0, 1, 2, 0, 2, 3 };

			var uvlist = new Vector2[4]
			{
				new Vector2(0,1),
				new Vector2(1,1),
				new Vector2(1,0),
				new Vector2(0,0)
			};

			_cachedQuad = new MeshDataArray()
			{
				Vertices =  verts,
				Normals = norms,
				Uvs = uvlist
			};
			_cachedQuad.Triangles.Add(trilist);
		}
	}
}
