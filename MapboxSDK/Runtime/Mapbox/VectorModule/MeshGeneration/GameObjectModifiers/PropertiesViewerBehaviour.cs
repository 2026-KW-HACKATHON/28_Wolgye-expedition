using System.Collections.Generic;
using UnityEngine;

namespace Mapbox.VectorModule.MeshGeneration.GameObjectModifiers
{
    public class PropertiesViewerBehaviour : MonoBehaviour
    {
        public Dictionary<string, string> Properties = new Dictionary<string, string>();
        
        public void Show(Dictionary<string,object> featureProperties)
        {
            Properties.Clear();
            foreach (var prop in featureProperties)
            {
                Properties.Add(prop.Key, prop.Value.ToString());
            }
        }
    }
}