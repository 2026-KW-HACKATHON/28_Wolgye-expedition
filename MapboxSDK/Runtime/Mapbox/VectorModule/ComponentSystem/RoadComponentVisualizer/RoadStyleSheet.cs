using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using UnityEngine;

namespace Mapbox.VectorModule.ComponentSystem.RoadComponentVisualizer
{
    [DisplayName("Road Style Sheet")]
    [CreateAssetMenu(menuName = "Mapbox/Road Style Sheet")]
    public class RoadStyleSheet : ScriptableObject
    {
        public List<RoadStyle> Styles;

        public void Initialize()
        {
            foreach (var style in Styles)
            {
                style.Initialize();
            }
        }
        
        public bool TryGetStyle(RoadFeatureUnity feature, out RoadStyle style)
        {
            for (int i = 0; i < Styles.Count; i++)
            {
                if (Styles[i].Filters.Try(feature))
                {
                    style = Styles[i];
                    return true;
                }
            }
            style = null;
            return false;
        }

        public bool Contains(RoadFeatureUnity feature)
        {
            for (int i = 0; i < Styles.Count; i++)
            {
                if (Styles[i].Filters.Try(feature))
                {
                    return true;
                }
            }
            return false;
        }
    }
}