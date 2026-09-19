using System;
using Mapbox.BaseModule.Data.Tiles;
using Mapbox.BaseModule.Map;
using UnityEngine;

namespace Mapbox.VectorModule.ComponentSystem.Modifiers
{
    public class ArraySnapTerrain
    {
        private IMapInformation _mapInformation;
        public ArraySnapTerrain(IMapInformation mapInformation)
        {
            _mapInformation = mapInformation;
        }

        public void SnapTerrain(Span<Vector3> vertices, int topPolyLength, CanonicalTileId tileId, float tileSize)
        {
            for (var i = 0; i < topPolyLength; i++)
            {
                ref Vector3 vertex = ref vertices[i];
                var h = 0f;
                if (_mapInformation.QueryElevation != null)
                {
                    h = (_mapInformation.QueryElevation(
                            tileId,
                            (vertex.x),
                            (vertex.z + 1)) / _mapInformation.Scale
                                            / tileSize);
                }
                vertex.Set(vertex.x, h, vertex.z);
            }
        }

    }
}