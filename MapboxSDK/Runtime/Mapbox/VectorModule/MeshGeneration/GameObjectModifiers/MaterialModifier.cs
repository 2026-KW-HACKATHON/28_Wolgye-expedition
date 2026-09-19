using System;
using Mapbox.BaseModule.Map;
using Mapbox.BaseModule.Utilities;
using UnityEngine;

namespace Mapbox.VectorModule.MeshGeneration.GameObjectModifiers
{
    [Serializable]
    public class MaterialModifier : GameObjectModifier
    {
        [SerializeField]
        private Material[] _material;
    
        public MaterialModifier(Material[] material)
        {
            _material = material;
        }

        public override void Run(VectorEntity ve, IMapInformation mapInformation)
        {
            if (ve.MeshRenderer != null)
            {
                ve.MeshRenderer.materials = _material;
            }
            else
            {
                ve.MeshRenderer = ve.GameObject.AddComponent<MeshRenderer>();
                if(ve.MeshRenderer != null)
                    ve.MeshRenderer.materials = _material;
            }
        }
    }
}
