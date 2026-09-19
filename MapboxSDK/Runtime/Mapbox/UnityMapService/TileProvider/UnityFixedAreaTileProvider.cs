using System;
using System.Collections.Generic;
using System.Security.Principal;
using Mapbox.BaseModule.Data.Interfaces;
using Mapbox.BaseModule.Data.Tiles;
using Mapbox.BaseModule.Data.Vector2d;
using Mapbox.BaseModule.Map;
using Mapbox.BaseModule.Utilities;
using UnityEngine;
using UnityEngine.Serialization;
using Object = System.Object;

namespace Mapbox.UnityMapService.TileProviders
{
    [Serializable]
    public class UnityFixedAreaTileProvider : TileProvider
    {
        [Tooltip("Number of tiles to load westward direction")]
        public int TilesToWest = 2;
        [FormerlySerializedAs("MaxX")] [Tooltip("Number of tiles to load eastward direction")]
        public int TilesToEast = 2;
        [Tooltip("Number of tiles to load northward direction")]
        public int TilesToNorth = 2;
        [Tooltip("Number of tiles to load southward direction")]
        public int TilesToSouth = 2;
        
        public override bool GetTileCover(IMapInformation mapInformation, TileCover tileCover)
        {
            tileCover.Tiles.Clear();
            var centerTileId = Conversions.CoordinateToTileId(mapInformation.LatitudeLongitude, (int)mapInformation.Zoom);
            for (int i = -TilesToWest; i <= TilesToEast; i++)
            {
                for (int j = -TilesToNorth; j <= TilesToSouth; j++)
                {
                    var tileId = new UnwrappedTileId(centerTileId.Z, centerTileId.X + i, centerTileId.Y + j);
                    tileCover.Tiles.Add(tileId);
                }
            }

            return true;
        }
    }
    
    [Serializable]
    public class TransformBasedTileProvider : TileProvider
    {
        [Tooltip("The object to use as reference for map center")]
        public Transform Transform;
        [FormerlySerializedAs("MinX")] [Tooltip("Number of tiles to load westward direction")]
        public int TilesToWest = 2;
        [FormerlySerializedAs("MaxX")] [Tooltip("Number of tiles to load eastward direction")]
        public int TilesToEast = 2;
        [FormerlySerializedAs("MinY")] [Tooltip("Number of tiles to load northward direction")]
        public int TilesToNorth = 2;
        [FormerlySerializedAs("MaxY")] [Tooltip("Number of tiles to load southward direction")]
        public int TilesToSouth = 2;
        
        public override bool GetTileCover(IMapInformation mapInformation, TileCover tileCover)
        {
            tileCover.Tiles.Clear();
            var centerTileId = Conversions.LatitudeLongitudeToTileId(mapInformation.ConvertPositionToLatLng(Transform.position), (int) mapInformation.Zoom);
            for (int i = -TilesToWest; i <= TilesToEast; i++)
            {
                for (int j = -TilesToNorth; j <= TilesToSouth; j++)
                {
                    var tileId = new UnwrappedTileId(centerTileId.Z, centerTileId.X + i, centerTileId.Y + j);
                    tileCover.Tiles.Add(tileId);
                }
            }

            return true;
        }
    }

    public class UnityRangeTileProvider : TileProvider
    {
        private Transform _transform;
        private Vector3[] _ranges = new[]
        {
            new Vector3(1500, 15, 1000),
            new Vector3(1000, 16, 500),
            new Vector3(500, 17,300),
            new Vector3(300, 18, 0)
        };

        private Queue<UnwrappedTileId> _tileCoverSearchQueue;

        public UnityRangeTileProvider(Transform transform)
        {
            _transform = transform;
            _tileCoverSearchQueue = new Queue<UnwrappedTileId>();
        }
    
        public override bool GetTileCover(IMapInformation mapInformation, TileCover tileCover)
        {
            tileCover.Tiles.Clear();
            var latlng = mapInformation.ConvertPositionToLatLng(_transform.position);
            var mercator = Conversions.LatitudeLongitudeToWebMercator(latlng);
            var centerTileId = Conversions.CoordinateToTileId(latlng, (int)_ranges[0].y);
            _tileCoverSearchQueue.Clear();
            for (int i = -3; i <= 3; i++)
            {
                for (int j = -3; j <= 3; j++)
                {
                    var tileId = new UnwrappedTileId(centerTileId.Z, centerTileId.X + i, centerTileId.Y + j);
                    var delta = Vector2d.Distance(Conversions.TileBoundsInWebMercator(tileId).Center, mercator);
                    if (delta < _ranges[0].x)
                    {
                        _tileCoverSearchQueue.Enqueue(tileId);
                    }
                }
            }

            for (int i = 0; i < _ranges.Length; i++)
            {
                var poolSize = _tileCoverSearchQueue.Count;
                for (int j = 0; j < poolSize; j++)
                {
                    var tileId = _tileCoverSearchQueue.Dequeue();
                    var delta = Vector2d.Distance(Conversions.TileBoundsInWebMercator(tileId).Center, mercator);
                    if (delta < _ranges[i].z)
                    {
                        _tileCoverSearchQueue.Enqueue(new UnwrappedTileId(tileId.Z + 1, tileId.X * 2, tileId.Y * 2));
                        _tileCoverSearchQueue.Enqueue(new UnwrappedTileId(tileId.Z + 1, tileId.X * 2 + 1, tileId.Y * 2));
                        _tileCoverSearchQueue.Enqueue(new UnwrappedTileId(tileId.Z + 1, tileId.X * 2 + 1, tileId.Y * 2 + 1));
                        _tileCoverSearchQueue.Enqueue(new UnwrappedTileId(tileId.Z + 1, tileId.X * 2, tileId.Y * 2 + 1));
                    }
                    else
                    {
                        tileCover.Tiles.Add(tileId);
                    }
                }
            }
        
            return true;
        }
    }
}