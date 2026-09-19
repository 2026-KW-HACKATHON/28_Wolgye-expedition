using System;
using UnityEngine;

namespace Mapbox.VectorModule
{
    [Serializable]
    public class VectorLayerVisualizerSettings
    {
        [Tooltip("Visuals will be offset by this value (in example, push things above ground level).")]
        public Vector3 Offset = Vector3.zero;
        public ModifierStackExecutionMode StackExecutionMode;
    }
}