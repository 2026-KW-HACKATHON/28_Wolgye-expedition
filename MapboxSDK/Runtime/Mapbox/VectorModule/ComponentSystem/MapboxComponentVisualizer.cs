using System;
using System.Collections.Generic;
using Mapbox.BaseModule.Data.Tiles;
using Mapbox.BaseModule.Map;
using Mapbox.BaseModule.Unity;
using Mapbox.BaseModule.Utilities;
using Mapbox.VectorModule.ComponentSystem.Data;
using UnityEngine;

namespace Mapbox.VectorModule.ComponentSystem
{
    public abstract class MapboxComponentVisualizer : VectorLayerVisualizer
    {
        public int VisualizerId => _id;
        private static int _nextId = 0;
        private readonly int _id;

        protected ObjectPool<VectorEntity> _buildingObjectPool;
        
        public MapboxComponentVisualizer(string name, IMapInformation mapInformation, UnityContext unityContext = null) : base(name, mapInformation, unityContext, null)
        {
            _id = System.Threading.Interlocked.Increment(ref _nextId);
            _buildingObjectPool = new ObjectPool<VectorEntity>(VectorEntityGenerator, 20);
        }
        
        public virtual MeshData CreateMesh(CanonicalTileId tileId, VectorTileLayer layer)
        {
            return null;
        }
        
        public virtual List<GameObject> CreateGo(CanonicalTileId tileId, MeshData meshData)
        {
            return null;
        }

        public override void UpdateForView(CanonicalTileId canonicalTileId, IMapInformation information)
        {
            if (_results.TryGetValue(canonicalTileId, out var visuals))
            {
                _mapInformation.PositionObjectFor(canonicalTileId, out var position, out var scale);
                foreach (var entity in visuals)
                {
                    entity.GameObject.transform.localPosition = position;
                    entity.GameObject.transform.localScale = scale;
                }
            }
        }
        
        public override void UnregisterTile(CanonicalTileId tileId)
        {
            if (_results.ContainsKey(tileId))
            {
                foreach (var entity in _results[tileId])
                {
                    entity.GameObject.SetActive(false);
                    _buildingObjectPool.Put(entity);
                    //TODO call finalize for gameobject modifiers here
                    
                    OnBuildingMeshDestroyed(tileId, entity.GameObject);
                }

                _results.Remove(tileId);
            }

            //TODO call unregister for gameobject modifiers here
        }

        public override void SetActive(CanonicalTileId canonicalTileId, bool isActive, IMapInformation mapInformation)
        {
            if (_results.TryGetValue(canonicalTileId, out var visuals))
            {
                foreach (var entity in visuals)
                {
                    entity.GameObject.SetActive(isActive);
                }
            }
        }
        
        public override void ClearCaches()
        {
            base.ClearCaches();
        }
        
        private VectorEntity VectorEntityGenerator()
        {
            var go = new GameObject();
            go.transform.SetParent(_layerRootObject, worldPositionStays: false);
            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = new Mesh();
            var mr = go.AddComponent<MeshRenderer>();
            var tempVectorEntity = new VectorEntity()
            {
                GameObject = go,
                Transform = go.transform,
                MeshFilter = mf,
                MeshRenderer = mr,
                Mesh = mf.sharedMesh
            };
            return tempVectorEntity;
        }
        
        public Action<CanonicalTileId, GameObject, MeshData> OnBuildingMeshCreated = (id, list, info) => { };
        public Action<CanonicalTileId, GameObject> OnBuildingMeshDestroyed = (id, list) => { };
    }
}