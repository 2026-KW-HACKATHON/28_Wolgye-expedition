using System;
using System.Collections.Generic;
using UnityEngine;

namespace Mapbox.VectorModule.ComponentSystem.Data
{
    public ref struct VectorTileLayer
    {
        public ReadOnlySpan<byte> Data;

        /// <summary>
        /// Class to access a vector tile layer
        /// </summary>
        public VectorTileLayer(ref ReadOnlySpan<byte> data)
        {
            Data = data;
            _FeaturesData = new List<Vector2Int>();
            Keys = new List<string>();
            Values = new List<object>();
            Name = null;
            Version = 0;
            Extent = 0;
        }

        /// <summary>
        /// Get number of features.
        /// </summary>
        /// <returns>Number of features in this layer</returns>
        public int FeatureCount()
        {
            return _FeaturesData.Count;
        }

        public Vector2Int GetViewFor(int i)
        {
            return _FeaturesData[i];
        }

        public void AddFeatureData(Vector2Int data)
        {
            _FeaturesData.Add(data);
        }


        /// <summary>Name of this layer https://github.com/mapbox/vector-tile-spec/blob/master/2.1/vector_tile.proto#L57</summary>
        public string Name { get; set; }


        /// <summary>Version of this layer https://github.com/mapbox/vector-tile-spec/blob/master/2.1/vector_tile.proto#L55</summary>
        public long Version { get; set; }


        /// <summary>Extent of this layer https://github.com/mapbox/vector-tile-spec/blob/master/2.1/vector_tile.proto#L70</summary>
        public float Extent;


        /// <summary>Raw data of the features contained in this layer</summary>
        private List<Vector2Int> _FeaturesData { get; set; }


        /// <summary>
        /// TODO: switch to 'dynamic' when Unity supports .Net 4.5
        /// Values contained in this layer
        /// </summary>
        public List<object> Values { get; set; }


        /// <summary>
        /// Keys contained in this layer
        /// </summary>
        public List<string> Keys { get; set; }
    }
}