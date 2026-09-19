using Mapbox.VectorModule.ComponentSystem.Data;
using Mapbox.VectorTile.Geometry;
using UnityEngine;
using DecodeGeometry = Mapbox.VectorModule.ComponentSystem.Data.DecodeGeometry;

namespace Mapbox.VectorModule.ComponentSystem.RoadComponentVisualizer
{
    public class RoadFeatureUnity
    {
        public ulong Id;
        public GeomType GeometryType;
        public uint[] GeometryCommands;
        public FeatureVertexData VertexData;
        public int[] Tags;
        public string Class;
        public string Type;
        public string Structure;

        public void SetProperties(ref VectorTileLayer layer, ref int classTagIndex, ref int typeTagIndex, ref int structureTagIndex)
            {
                if (Tags == null) return; // Some features have no tags

                var tagCount = Tags.Length;
                //some features have odd number of tags
                //not sure if it's a bug or data issue
                //so -1 here to skip last single tag
                for (int i = 0; i < tagCount - 1; i += 2)
                {
                    if (classTagIndex != -1 && Tags[i] == classTagIndex)
                    {
                        Class = layer.Values[Tags[i + 1]].ToString();
                    }
                    else if (typeTagIndex != -1 && Tags[i] == typeTagIndex)
                    {
                        Type = layer.Values[Tags[i + 1]].ToString();
                    }
                    else if (structureTagIndex != -1 && Tags[i] == structureTagIndex)
                    {
                        Structure = layer.Values[Tags[i + 1]].ToString();
                    }
                }
            }

        public FeatureVertexData Geometry(Vector3 scale)
        {
            return DecodeGeometry.GetGeometry(GeometryCommands, scale);
        }
    }
}