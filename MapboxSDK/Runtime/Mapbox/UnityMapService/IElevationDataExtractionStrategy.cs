using System;
using UnityEngine;
using TerrainData = Mapbox.BaseModule.Data.DataFetchers.TerrainData;

namespace Mapbox.UnityMapService
{
    public interface IElevationDataExtractionStrategy
    {
        void ExtractHeightData(Texture2D data, Action<float[]> callback);
        void ExtractHeightData(Texture2D data, TerrainData terrainData);
        void ExtractHeightData(TerrainData terrainData);
    }
}