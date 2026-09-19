using System;
using System.Runtime.CompilerServices;
using Mapbox.BaseModule.Map;
using Mapbox.VectorModule.ComponentSystem.BuildingComponentVisualizer;
using Mapbox.VectorModule.ComponentSystem.Data;
using Mapbox.VectorModule.MeshGeneration.MeshModifiers;
using UnityEngine;

namespace Mapbox.VectorModule.ComponentSystem.Modifiers
{
    public class ArrayHeight : IPerformanceExtrusion
    {
        private readonly BasicExtrusionSettings _settings;

        public ArrayHeight(BasicExtrusionSettings settings)
        {
            _settings = settings;
        }

        public int Run(
            Span<Vector3> vertices,
            Span<Vector3> normals,
            int vertexAnchorIndex,
            int[] triList,
            int triIndex,
            float height,
            float minHeight,
            FeatureVertexData vertexData,
            float tileSizeX,
            float  scale)
        {
            if (vertexData == null || vertexData.Submeshes.Count < 1)
                return triIndex;

            var startIndex = vertexAnchorIndex;

            // Convert heights to local (tile) units once
            height = (height / scale) / tileSizeX;
            float minH   = (minHeight > 0)
                ? (minHeight / scale) / tileSizeX
                : 0;

            float wallHeight = height - minH;
            
            // Raise the roof (top ring/verts in-span)
            GenerateRoofMesh(vertices, height);

            // Build walls
            triIndex = GenerateWallMesh(vertices, normals, vertexData, triList, triIndex, startIndex, wallHeight, height);

            return triIndex;
        }

        public void GenerateRoofMesh(Span<Vector3> vertices, float maxHeight)
        {
            // Mutate by ref to avoid creating new Vector3
            for (int i = 0; i < vertices.Length; i++)
            {
                ref Vector3 v = ref vertices[i];
                v.y = maxHeight;
            }
        }
        
        private int GenerateWallMesh(
            Span<Vector3> vertices,
            Span<Vector3> normals,
            FeatureVertexData vertexData,
            int[] trilist,
            int triIndex,
            int startIndex,
            float wallHeight,
            float roofHeight)
        {
            // Aliases for quicker access
            var verts     = vertexData.Vertices;
            var submeshes = vertexData.Submeshes;

            int topPolygonVertexCount = vertexData.VertexCount;

            // For each ring (outer + holes)
            // We assume wall-vertex area starts after top polygon: each edge adds 4 vertices
            for (int i = 1; i < submeshes.Count; i++)
            {
                int start = submeshes[i - 1];
                int end   = submeshes[i];
                int count = end - start;

                var subSpan  = verts.AsSpan(start, count);
                int ringBase = topPolygonVertexCount + (start * 4); // base in the wall region for this submesh
                var vertSpan = vertices.Slice(ringBase, count * 4);
                var normSpan = normals.Slice(ringBase, count * 4);

                // Walk edges
                for (int j = 0; j < count; j++)
                {
                    int jNext = (j == count - 1) ? 0 : j + 1;

                    // edge direction (XZ)
                    Vector3 v1;
                    v1.x = subSpan[jNext].x - subSpan[j].x;
                    v1.y = 0f;
                    v1.z = subSpan[jNext].z - subSpan[j].z;

                    NormalizeXZInPlace(ref v1);
                    Vector3 n1 = PerpXZ(v1);

                    int vertBase = (j << 2); // j * 4

                    // Fetch top y from roofed vertex (already raised)
                    // All roof vertices were raised to the same height in GenerateRoofMesh
                    // Use the passed roofHeight directly to avoid indexing issues with multiple submeshes
                    float yTop = roofHeight;
                    float yMin = yTop - wallHeight;

                    // Write quads (curr -> next, top and bottom)
                    // next(1)---------curr(0)
                    //   |                |
                    // nextBot(3)----currBot(2)

                    // curr (top)
                    ref Vector3 v0 = ref vertSpan[vertBase];
                    v0.x = subSpan[j].x;
                    v0.y = yTop;
                    v0.z = subSpan[j].z;

                    // next (top)
                    ref Vector3 v1t = ref vertSpan[vertBase + 1];
                    v1t.x = subSpan[jNext].x;
                    v1t.y = yTop;
                    v1t.z = subSpan[jNext].z;

                    // curr (bottom)
                    ref Vector3 v2b = ref vertSpan[vertBase + 2];
                    v2b.x = subSpan[j].x;
                    v2b.y = yMin;
                    v2b.z = subSpan[j].z;

                    // next (bottom)
                    ref Vector3 v3b = ref vertSpan[vertBase + 3];
                    v3b.x = subSpan[jNext].x;
                    v3b.y = yMin;
                    v3b.z = subSpan[jNext].z;

                    // normals (same for all four)
                    normSpan[vertBase]     = n1;
                    normSpan[vertBase + 1] = n1;
                    normSpan[vertBase + 2] = n1;
                    normSpan[vertBase + 3] = n1;

                    // triangles
                    int si    = startIndex;
                    int baseB = si + ringBase + vertBase;          // this edge's first wall vertex
                    int baseA = si + ringBase + ((j == 0) ? ((count - 1) << 2) : ((j - 1) << 2)); // previous edge's base (for wrapping last pair)

                    if (j < count - 1)
                    {
                        // two tris for face between curr edge top/bot and next top/bot
                        trilist[triIndex++] = baseB;
                        trilist[triIndex++] = baseB + 2;
                        trilist[triIndex++] = baseB + 1;

                        trilist[triIndex++] = baseB + 1;
                        trilist[triIndex++] = baseB + 2;
                        trilist[triIndex++] = baseB + 3;
                    }
                    else
                    {
                        // close the ring to the first edge (wrap)
                        trilist[triIndex++] = baseB;
                        trilist[triIndex++] = baseB + 2;
                        trilist[triIndex++] = si + ringBase; // first edge's top-left

                        trilist[triIndex++] = si + ringBase;
                        trilist[triIndex++] = baseB + 2;
                        trilist[triIndex++] = si + ringBase + 2; // first edge's bottom-left
                    }
                }
            }

            return triIndex;
        }
        
        public int CalculateTriCountFor(int totalPointCount)
        {
            // 2 triangles per edge quad => 6 indices per edge
            // If totalPointCount is number of wall edges, return totalPointCount * 6
            return totalPointCount * 6;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void NormalizeXZInPlace(ref Vector3 v)
        {
            float lenSq = v.x * v.x + v.z * v.z;
            if (lenSq <= 1e-20f) { v.x = 0f; v.z = 0f; return; }
            float invLen = 1.0f / Mathf.Sqrt(lenSq);
            v.x *= invLen;
            v.z *= invLen;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector3 PerpXZ(in Vector3 v) => new Vector3(-v.z, 0f, v.x);
    }
}
