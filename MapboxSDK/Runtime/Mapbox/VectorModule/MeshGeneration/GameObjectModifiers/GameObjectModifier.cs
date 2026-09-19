using System;
using Mapbox.BaseModule.Data.Tiles;
using Mapbox.BaseModule.Map;
using Mapbox.BaseModule.Unity;
using Mapbox.BaseModule.Utilities;

namespace Mapbox.VectorModule.MeshGeneration.GameObjectModifiers
{
    public interface IGameObjectModifier
    {
        void Run(VectorEntity ve, IMapInformation mapInformation);
        void OnPoolItem(VectorEntity vectorEntity);
        void Clear();
        void ClearCaches();
        void Unregister(CanonicalTileId tileId);
        void Finalize(VectorEntity entity);
    }

    [Serializable]
    public class GameObjectModifier : ModifierBase, IGameObjectModifier
    {
        public virtual void Run(VectorEntity ve, IMapInformation mapInformation)
        {

        }

        public virtual void OnPoolItem(VectorEntity vectorEntity)
        {

        }

        public virtual void Clear()
        {

        }

        public virtual void ClearCaches()
        {

        }

        public virtual void Unregister(CanonicalTileId tileId)
        {
			
        }

        public virtual void Finalize(VectorEntity entity)
        {
            
        }
    }
}