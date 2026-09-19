using System;
using Mapbox.BaseModule.Data.DataFetchers;

namespace Mapbox.BaseModule.Unity
{
    [Serializable]
    public class UnityTileVectorContainer
    {
        private UnityMapTile _unityMapTile;
        public VectorData Data;
        public Action CachedMeshGenerationAction;
		
        public UnityTileVectorContainer(UnityMapTile unityMapTile)
        {
            _unityMapTile = unityMapTile;
        }

        public void SetVectorData(VectorData vectorData)
        {
            Data = vectorData;
        }

        public VectorData GetAndClearVectorData()
        {
            if (Data == null)
                return null;

            var rd = Data;
            Data = null;
            return rd;
        }

        public void OnDestroy()
        {
            //anything to finalize?   
        }
    }
}