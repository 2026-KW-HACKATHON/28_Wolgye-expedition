using System;
using Mapbox.BaseModule.Data.Tiles;
using Mapbox.BaseModule.Data.Vector2d;
using Mapbox.BaseModule.Map;
using Mapbox.BaseModule.Utilities;
using Mapbox.Example.Scripts.Map;
using Mapbox.Utils;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Mapbox.Example.Scripts
{
    public class DrawTileBorders : MonoBehaviour
    {
        public Camera Camera;
        public MapBehaviourCore MapBehaviour;
        public Color Color;
        public Color Color2;
        private MapboxMap _map;
        private Camera _camera;
        private GUIStyle _style;
        private GUIStyle _style2;
        public int TileIdFontSize = 40;
        
        private void Start()
        {
            _style = new GUIStyle()
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = TileIdFontSize, wordWrap = false, normal =
                {
                    textColor = Color.black,
                    background = Texture2D.whiteTexture
                }
            };
            _style2 = new GUIStyle()
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = TileIdFontSize, wordWrap = false, normal =
                {
                    textColor = Color.black,
                    background = Texture2D.grayTexture
                }
            };
            _camera = Camera;
            if (MapBehaviour == null)
                MapBehaviour = FindObjectOfType<MapboxMapBehaviour>();
        
            MapBehaviour.Initialized += (map) =>
            {
                _map = map;
            };
        }
    
        public void Update()
        {
            _style.fontSize = TileIdFontSize;
            _style2.fontSize = TileIdFontSize;
            
            if (_map == null)
                return;
        
            var center = _map.MapInformation.CenterMercator;
            var scale = _map.MapInformation.Scale;
            foreach (var tile in _map.TileCover.Tiles)
            {
                var rectd = Conversions.TileBoundsInWebMercator(tile);
                var bottomRight = (rectd.BottomRight - center) / scale;
                var topLeft = (rectd.TopLeft - center) / scale;
                var topRight = (new Vector2d(rectd.BottomRight.x, rectd.TopLeft.y) - center) / scale;
                var bottomLeft = (new Vector2d(rectd.TopLeft.x, rectd.BottomRight.y) - center) / scale;
                Debug.DrawLine(topLeft.ToVector3xz(),    topRight.ToVector3xz(),    Color);
                Debug.DrawLine(topLeft.ToVector3xz(),    bottomLeft.ToVector3xz(),  Color);
                Debug.DrawLine(bottomLeft.ToVector3xz(), bottomRight.ToVector3xz(), Color);
                Debug.DrawLine(topRight.ToVector3xz(),   bottomRight.ToVector3xz(), Color);
            }
            
            foreach (var tile in _map.MapVisualizer.ActiveTiles.Values)
            {
                var rectd = Conversions.TileBoundsInWebMercator(tile.UnwrappedTileId);
                var bottomRight = (rectd.BottomRight - center) / scale;
                var topLeft = (rectd.TopLeft - center) / scale;
                var topRight = (new Vector2d(rectd.BottomRight.x, rectd.TopLeft.y) - center) / scale;
                var bottomLeft = (new Vector2d(rectd.TopLeft.x, rectd.BottomRight.y) - center) / scale;
                Debug.DrawLine(topLeft.ToVector3xz(),    bottomRight.ToVector3xz(),    Color2);
                // Debug.DrawLine(topLeft.ToVector3xz(),    bottomLeft.ToVector3xz(),  Color2);
                // Debug.DrawLine(bottomLeft.ToVector3xz(), bottomRight.ToVector3xz(), Color2);
                // Debug.DrawLine(topRight.ToVector3xz(),   bottomRight.ToVector3xz(), Color2);
            }
        }
        
        private void OnGUI()
        {
            if (_map == null)
                return;
        
            var center = _map.MapInformation.CenterMercator;
            var scale = _map.MapInformation.Scale;
            foreach (var tile in _map.TileCover.Tiles)
            {
                NewMethod(tile, center, scale, Color, _style);
            }
            
            foreach (var tile in _map.MapVisualizer.ActiveTiles.Values)
            {
                NewMethod(tile.UnwrappedTileId, center, scale, Color2, _style2, true);
            }
        }

        private void NewMethod(UnwrappedTileId tile, Vector2d center, float scale, Color color, GUIStyle style, bool bottom = false)
        {
            var rectd = Conversions.TileBoundsInWebMercator(tile);
            var bottomRight = (rectd.BottomRight - center) / scale;
            var topLeft = (rectd.TopLeft - center) / scale;
            var topRight = (new Vector2d(rectd.BottomRight.x, rectd.TopLeft.y) - center) / scale;
            var bottomLeft = (new Vector2d(rectd.TopLeft.x, rectd.BottomRight.y) - center) / scale;
            Debug.DrawLine(topLeft.ToVector3xz(),    topRight.ToVector3xz(),    color);
            Debug.DrawLine(topLeft.ToVector3xz(),    bottomLeft.ToVector3xz(),  color);
            Debug.DrawLine(bottomLeft.ToVector3xz(), bottomRight.ToVector3xz(), color);
            Debug.DrawLine(topRight.ToVector3xz(),   bottomRight.ToVector3xz(), color);

            var matrix = GUI.matrix;
            var topLeftScreen = bottom ? _camera.WorldToScreenPoint(bottomLeft.ToVector3xz()) : _camera.WorldToScreenPoint(topLeft.ToVector3xz());
            var topRightScreen = bottom ? _camera.WorldToScreenPoint(bottomRight.ToVector3xz()) : _camera.WorldToScreenPoint(topRight.ToVector3xz());
                
            var angle = Vector2.SignedAngle(Vector2.right, (topRightScreen - topLeftScreen).normalized);
            GUIUtility.RotateAroundPivot(-angle, new Vector2(topLeftScreen.x, Screen.height - topLeftScreen.y));
                
            var content = new GUIContent(tile.Canonical.ToString());
            Vector2 size = style.CalcSize(content);
            GUI.color = color;
            var newY = Screen.height - topLeftScreen.y;
            if(bottom) newY -= size.y;
            GUI.Label(new Rect(new Vector2(topLeftScreen.x, newY), size), content, style);
            GUI.matrix = matrix;
        }
    }
}
