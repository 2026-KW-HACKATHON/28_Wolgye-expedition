using System;
using UnityEngine;

namespace Mapbox.BaseModule.Data.DataFetchers
{
    [Serializable]
    public class BuildingData : MapboxTileData
    {
        public GameObject BuildingGameObject;
        public Action Updated = () => { };
        public Action MeshUpdated = () => { };
    }
}