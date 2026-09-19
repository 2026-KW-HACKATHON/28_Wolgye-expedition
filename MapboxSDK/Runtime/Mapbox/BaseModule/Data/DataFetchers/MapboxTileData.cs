using System;
using Mapbox.BaseModule.Data.Tiles;
using UnityEngine;

namespace Mapbox.BaseModule.Data.DataFetchers
{
    public class MapboxTileData
    {
        public CanonicalTileId TileId;
        public string TilesetId;
        public CacheType CacheType;
        public string ETag;
        public DateTime? ExpirationDate;
        public bool HasError = false;
        [HideInInspector] public byte[] Data;
        
        // Multicast: a single TerrainData instance is shared across up to 16 render tiles
        // (data zoom = render zoom − 2), each of which needs its own cleanup callback on
        // eviction. The prior single-callback Set/overwrite pattern silently dropped 15 of
        // them. Subscribers attach with AddDisposeCallback and must symmetrically remove.
        private Action _onDispose;

        public virtual void Dispose()
        {
            _onDispose?.Invoke();
            _onDispose = null;
        }

        public void AddDisposeCallback(Action callback)
        {
            if (callback == null) return;
            _onDispose += callback;
        }

        public void RemoveDisposeCallback(Action callback)
        {
            if (callback == null) return;
            _onDispose -= callback;
        }
    }
}