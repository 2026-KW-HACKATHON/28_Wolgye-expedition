using System.Collections.Generic;
using UnityEngine;

namespace Mapbox.VectorModule.ComponentSystem.Data
{
    public class FeatureVertexData
    {
        public Vector3[] Vertices;
        public List<int> Submeshes = new List<int>(4);
        public int VertexCount;
    }
}