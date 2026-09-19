using Mapbox.BaseModule.Data;
using Mapbox.BaseModule.Map;
using Mapbox.BaseModule.Unity;
using Mapbox.BaseModule.Utilities;
using Mapbox.VectorModule.MeshGeneration.MeshModifiers;
using UnityEngine;

namespace Mapbox.VectorModule.MeshGeneration.Unity
{
    public abstract class ScriptableMeshModifierObject : ScriptableObject, IMeshModifier
    {
        [SerializeField, HideInInspector] private bool m_Active = true;
        
        protected abstract MeshModifier _meshModifierImplementation { get; }

        public virtual void ConstructModifier(UnityContext unityContext)
        {
			
        }
		
        public virtual void Initialize()
        {
            _meshModifierImplementation.Initialize();
        }
		
        public void Run(VectorFeatureUnity feature, MeshData md, IMapInformation mapInfo)
        {
            _meshModifierImplementation.Run(feature, md, mapInfo);
        }
    }
}