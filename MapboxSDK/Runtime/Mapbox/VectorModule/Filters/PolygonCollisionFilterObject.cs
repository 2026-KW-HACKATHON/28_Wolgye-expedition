// using System;
// using System.Collections.Generic;
// using System.ComponentModel;
// using Mapbox.BaseModule.Data.Tiles;
// using Mapbox.BaseModule.Utilities;
// using UnityEngine;
//
// namespace Mapbox.VectorModule.Filters
// {
//     [CreateAssetMenu(menuName = "Mapbox/Filters/Mesh Collider Filter")]
//     [DisplayName("Polygon Collision Filter")]
//
//     public class PolygonCollisionFilterObject : FilterBaseObject
//     {
//         public bool ShowDebugLines = true;
//         private PolygonCollisionFilter _filter;
//         public override ILayerFeatureFilterComparer Filter
//         {
//             get
//             {
//                 if(_filter == null)
//                     _filter = new PolygonCollisionFilter(ShowDebugLines);
//                 return _filter;
//             }
//         }
//         
//         public void AddMeshCollider8192(List<Vector3> poly, CanonicalTileId tileId)
//         {
//             _filter.AddMeshCollider8192(poly, tileId);
//         }
//     }
//     
//     public class PolygonCollisionFilter : FilterBase
//     {
//
//         private bool _showDebugLines = false;
//         [NonSerialized] public Dictionary<CanonicalTileId, List<Tuple<Bounds,List<Vector3>>>> PolygonsPerTile;
//         
//         [NonSerialized] public Dictionary<CanonicalTileId, List<Tuple<Bounds,List<Vector3>>>> Zoom14PolygonsPerTile;
//
//         public PolygonCollisionFilter(bool showDebugLines = false)
//         {
//             PolygonsPerTile = new Dictionary<CanonicalTileId, List<Tuple<Bounds, List<Vector3>>>>();
//             _showDebugLines = showDebugLines;
//         }
//
//         public override bool Try(VectorFeatureUnity feature)
//         {
//             if (PolygonsPerTile.TryGetValue(feature.TileId, out var colliders))
//             {
//                 foreach (var collider in colliders)
//                 {
//                     foreach (var submesh in feature.Points)
//                     {
//                         var bounds = BoundsOfVertices(submesh);
//                         if (collider.Item1.Intersects(bounds))
//                         {
//                             if (PolygonIntersection2D.ArePolygonsIntersecting(collider.Item2, submesh))
//                                 return false;
//                         }
//                     }
//                 }
//             }
//             return true;
//         }
//         
//         public void AddMeshCollider8192(List<Vector3> poly, CanonicalTileId tileId)
//         {
//             if(!PolygonsPerTile.ContainsKey(tileId))
//                 PolygonsPerTile.Add(tileId, new List<Tuple<Bounds, List<Vector3>>>());
//
//             var bounds = BoundsOfVertices(poly);
//             PolygonsPerTile[tileId].Add(Tuple.Create(bounds, poly));
//         }
//
//         public void AddZoom14Polygon(CanonicalTileId tileId, Vector3[] polygon)
//         {
//             if (tileId.Z != 14)
//             {
//                 Debug.LogWarning("Only Zoom 14 polygons are supported for this method");
//                 return;
//             }
//             
//             Zoom14PolygonsPerTile.Add(tileId, new List<Tuple<Bounds, List<Vector3>>>());
//         }
//         
//         public Bounds BoundsOfVertices(List<Vector3> vertices)
//         {
//             var minX = float.MaxValue;
//             var minY = float.MaxValue;
//             var maxX = float.MinValue;
//             var maxY = float.MinValue;
//             
//             foreach (var vertex in vertices)
//             {
//                 if(vertex.x < minX) minX = vertex.x;
//                 if(vertex.x > maxX) maxX = vertex.x;
//                 if(vertex.z < minY) minY = vertex.z;
//                 if(vertex.z > maxY) maxY = vertex.z;
//             }
//
//             var center = new Vector3((maxX + minX) / 2, 0, (maxY + minY) / 2);
//             var size = new Vector3(maxX - minX, 1, maxY - minY);
//             return new Bounds(center, size);
//         }
//     }
//     
//     public class PolygonIntersection2D
//     {
//         public static bool ArePolygonsIntersecting(List<Vector3> polygon1, List<Vector3> polygon2)
//         {
//             return IsSeparatingAxisFound(polygon1, polygon2) == false && IsSeparatingAxisFound(polygon2, polygon1) == false;
//         }
//
//         private static bool IsSeparatingAxisFound(List<Vector3> polygonA, List<Vector3> polygonB)
//         {
//             // Iterate through each edge of polygonA
//             for (int i = 0; i < polygonA.Count; i++)
//             {
//                 // Get the current edge in the XZ plane
//                 Vector2 edge = new Vector2(
//                     polygonA[(i + 1) % polygonA.Count].x - polygonA[i].x,
//                     polygonA[(i + 1) % polygonA.Count].z - polygonA[i].z
//                 );
//
//                 // Find the axis perpendicular to the edge
//                 Vector2 axis = new Vector2(-edge.y, edge.x);
//
//                 // Project both polygons onto this axis
//                 (float minA, float maxA) = ProjectPolygonOnAxis(axis, polygonA);
//                 (float minB, float maxB) = ProjectPolygonOnAxis(axis, polygonB);
//
//                 // Check for gap
//                 if (maxA < minB || maxB < minA)
//                 {
//                     // If there's a gap, then there's a separating axis
//                     return true;
//                 }
//             }
//             return false;
//         }
//
//         private static (float min, float max) ProjectPolygonOnAxis(Vector2 axis, List<Vector3> polygon)
//         {
//             // Project the first point of the polygon onto the axis
//             float min = Vector2.Dot(axis, new Vector2(polygon[0].x, polygon[0].z));
//             float max = min;
//
//             // Project the rest of the points
//             for (int i = 1; i < polygon.Count; i++)
//             {
//                 float projection = Vector2.Dot(axis, new Vector2(polygon[i].x, polygon[i].z));
//                 if (projection < min) min = projection;
//                 if (projection > max) max = projection;
//             }
//
//             return (min, max);
//         }
//     }
// }