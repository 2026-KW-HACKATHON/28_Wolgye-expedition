using UnityEngine;
using UnityEngine.Rendering;

namespace Mapbox.BaseModule.Data
{
    public static class MeshExtensions
    {
        public static void SetMeshValues(this Mesh mesh, MeshData data)
        {
            mesh.indexFormat = IndexFormat.UInt32;
            mesh.subMeshCount = data.Triangles.Count;
            mesh.SetVertices(data.Vertices);
            mesh.SetNormals(data.Normals);
            if (data.Tangents.Count > 0)
            {
                mesh.SetTangents(data.Tangents);
            }

            var counter = data.Triangles.Count;
            mesh.subMeshCount = counter;
            for (int i = 0; i < counter; i++)
            {
                mesh.SetTriangles(data.Triangles[i], i);
            }

            counter = data.UV.Count;
            for (int i = 0; i < counter; i++)
            {
                mesh.SetUVs(i, data.UV[i]);
            }
        }
    }
}