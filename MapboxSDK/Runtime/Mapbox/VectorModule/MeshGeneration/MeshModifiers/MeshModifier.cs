using System;
using Mapbox.BaseModule.Data;
using Mapbox.BaseModule.Map;
using Mapbox.BaseModule.Utilities;

namespace Mapbox.VectorModule.MeshGeneration.MeshModifiers
{
    public interface IMeshModifier
    {
        void Run(VectorFeatureUnity feature, MeshData md, IMapInformation mapInfo);
        void Initialize();
    }

    [Serializable]
    public class MeshModifier : ModifierBase, IMeshModifier
    {

        public virtual void Run(VectorFeatureUnity feature, MeshData md, IMapInformation mapInfo)
        {

        }

        public virtual void Initialize()
        {

        }
    }
}