using System;
using Mapbox.BaseModule.Data.DataFetchers;
using Mapbox.BaseModule.Data.Tiles;
using UnityEngine;

namespace Mapbox.BaseModule.Unity
{
    [Serializable]
    public class UnityTileImageContainer
    {
        public TileContainerState State { get; private set; } = TileContainerState.Final;
        [field: SerializeField] public RasterData ImageData { get; private set; }
        
        private Action _onDispose;
        private UnityMapTile _unityMapTile;

        private const string MainTexFieldNameID = "_MainTex";
        private const string MainTexStFieldNameID = "_MainTex_ST";
        private const string MainTextureChangeTimeFieldNameID = "_MainTextureChangeTime";
        
        private static readonly int MainTex = Shader.PropertyToID(MainTexFieldNameID);
        private static readonly int MainTexSt = Shader.PropertyToID(MainTexStFieldNameID);
        private static readonly int MainTextureChangeTime = Shader.PropertyToID(MainTextureChangeTimeFieldNameID);

        public UnityTileImageContainer(UnityMapTile unityMapTile, Action onDispose)
        {
            _unityMapTile = unityMapTile;
            _onDispose = onDispose;
        }

        public void SetImageData(RasterData imageData, TileContainerState state = TileContainerState.Final)
        {
            if (ImageData != null)
            {
                ImageData.RemoveDisposeCallback(_onDispose);
            }

            State = state;
            if (imageData.Texture == null || imageData.TileId.Z == 0)
            {
                Debug.Log("no texture?");
            }

            ImageData = imageData;
            ImageData.AddDisposeCallback(_onDispose);
            OnImageryUpdated();
        }

        public void OnImageryUpdated()
        {
            if (ImageData == null)
                return;

            var scaleOffset = _unityMapTile.CanonicalTileId.CalculateScaleOffsetAtZoom(ImageData.TileId.Z);

            var block = _unityMapTile.PropertyBlock;
            block.SetTexture(MainTex, ImageData.Texture);
            block.SetVector(MainTexSt, scaleOffset);
            block.SetFloat(MainTextureChangeTime, Time.time);
            _unityMapTile.ApplyPropertyBlock();
        }

        public RasterData GetAndClearImageData()
        {
            if (ImageData == null)
                return null;

            _unityMapTile.PropertyBlock.SetTexture(MainTex, Texture2D.blackTexture);
            _unityMapTile.ApplyPropertyBlock();
            var rd = ImageData;
            ImageData.RemoveDisposeCallback(_onDispose);
            ImageData = null;
            return rd;
        }

        public void DisableImagery()
        {
            State = TileContainerState.Final;
            _unityMapTile.PropertyBlock.SetTexture(MainTex, null);
            _unityMapTile.ApplyPropertyBlock();
        }

        public void OnDestroy()
        {
            // Symmetric detach to mirror UnityTileTerrainContainer's fix: the multicast
            // _onDispose callback we added at SetImageData must be removed here, otherwise
            // a future RasterData eviction fires the closure into a destroyed tile and
            // cascades through OnDataDisposed → OnTileBroken → PoolTile on a dead tile.
            if (ImageData != null)
            {
                ImageData.RemoveDisposeCallback(_onDispose);
                ImageData = null;
            }
        }
    }
}