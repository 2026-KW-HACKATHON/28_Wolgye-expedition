using System;
using UnityEngine;

namespace Mapbox.VectorModule.ComponentSystem.RoadComponentVisualizer
{
    [Serializable]
    public class RoadStyle
    {
        [NonSerialized] public int RuntimeId;
        [Tooltip("The name of the road style (used only for the UI)")]
        public string Name;
        public ComponentFilterStack Filters;
        public float Width;
        public float PushUp;
        public Material Material;

        public void Initialize()
        {
            Filters.Initialize();
        }
    }
}