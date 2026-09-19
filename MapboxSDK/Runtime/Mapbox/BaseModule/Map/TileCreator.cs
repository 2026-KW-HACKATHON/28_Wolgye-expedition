using System;
using System.Collections;
using Mapbox.BaseModule.Data.Tiles;
using Mapbox.BaseModule.Unity;
using Mapbox.BaseModule.Utilities;
using Mapbox.ImageModule.Terrain.Settings;
using Mapbox.ImageModule.Terrain.TerrainStrategies;
using UnityEngine;

namespace Mapbox.BaseModule.Map
{
    public interface ITileCreator
    {
        IEnumerator Initialize(TerrainStrategy terrainStrategy = null);
        UnityMapTile GetTile();
        void PutTile(UnityMapTile tile);

        /// <summary>
        /// Some tile elements are destroyed so tile isn't good for use/reuse anymore
        /// </summary>
        event Action<UnwrappedTileId> OnTileBroken;
    }
    public class TileCreator : ITileCreator
    {
        public Material[] TileMaterials;
        private ObjectPool<UnityMapTile> _tilePool;
        private UnityContext _unityContext;
        private int _cacheSize;
        //private FlatTerrainStrategy _flatTerrainStrategy;
        private TerrainStrategy _terrainStrategy;
        private bool _instanceMaterial;
        
        public UnityMapTile GetTile() => _tilePool.GetObject();
        public void PutTile(UnityMapTile tile) => _tilePool.Put(tile);

        public TileCreator(UnityContext unityContext, Material[] tileMaterials = null, int cacheSize = 25, bool instanceMaterials = true)
        {
            TileMaterials = tileMaterials;
            _unityContext = unityContext;
            _cacheSize = cacheSize;
            _instanceMaterial = instanceMaterials;
        }

        public IEnumerator Initialize(TerrainStrategy terrainStrategy = null)
        {
            _terrainStrategy = terrainStrategy;
            _tilePool = new ObjectPool<UnityMapTile>(() => CreateTile(_unityContext));
            yield return _tilePool.InitializeItems(_cacheSize);
        }

        private UnityMapTile CreateTile(UnityContext unityContext)
        {
            var tile = new GameObject("TilePoolObject").AddComponent<UnityMapTile>();
            if (_unityContext.BaseTileRoot != null)
            {
                tile.gameObject.layer = _unityContext.BaseTileRoot.gameObject.layer;
                tile.transform.SetParent(_unityContext.BaseTileRoot, false);
            }

            if (TileMaterials?.Length == 1)
            {
                // Always use sharedMaterial. Per-tile state goes through the tile's
                // MaterialPropertyBlock (UnityTileTerrainContainer / UnityTileImageContainer
                // mutate it). The win is avoiding a Material clone per renderer — not SRP
                // Batcher batching (SetPropertyBlock disqualifies a renderer from the SRP
                // Batcher) and not GPU instancing (off in the current materials/shaders).
                // The legacy _instanceMaterial flag is retained on the constructor for
                // back-compat but no longer differentiates: per-tile state lives on the
                // property block, not on a unique Material instance.
                tile.MeshRenderer.sharedMaterial = TileMaterials[0];
                tile.Material = tile.MeshRenderer.sharedMaterial;
            }
            else if (TileMaterials?.Length > 1)
            {
                tile.MeshRenderer.sharedMaterials = TileMaterials;
                tile.Material = tile.MeshRenderer.sharedMaterial;
            }
            
            tile.gameObject.SetActive(false);
            _terrainStrategy?.RegisterTile(tile, false);
            tile.OnDataDisposed += OnTileBroken;
            
            return tile;
        }

        public event Action<UnwrappedTileId> OnTileBroken = (id) => { };
    }
}