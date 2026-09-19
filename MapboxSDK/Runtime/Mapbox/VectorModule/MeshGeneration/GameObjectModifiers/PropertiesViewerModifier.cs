using System;
using Mapbox.BaseModule.Map;
using Mapbox.BaseModule.Utilities;

namespace Mapbox.VectorModule.MeshGeneration.GameObjectModifiers
{
    [Serializable]
    public class PropertiesViewerModifier : GameObjectModifier
    {
        public PropertiesViewerModifier()
        {
        }

        public override void Run(VectorEntity ve, IMapInformation mapInformation)
        {
            if (ve.GameObject != null && ve.Feature != null)
            {
                var behaviour = ve.GameObject.GetComponent<PropertiesViewerBehaviour>();
                if (behaviour == null)
                    behaviour = ve.GameObject.AddComponent<PropertiesViewerBehaviour>();
                behaviour.Show(ve.Feature.Properties);
            }
        }
    }
}