using Mapbox.BaseModule.Map;
using Mapbox.BaseModule.Utilities;
using UnityEngine;

namespace Mapbox.Example.Scripts.Map
{
    public class ShaderLineThickness : MonoBehaviour
    {
        public MapBehaviourCore MapBehaviour;
        private IMapInformation _mapInfo;
        public float Width;

        private string _lineThicknessFieldName = "_MapboxLineThickness";
        private string _mapScaleFieldName = "_MapboxMapScale";

        private void Awake()
        {
            MapBehaviour.Initialized += (map) =>
            {
                _mapInfo = map.MapInformation;
            };
        }

        void Update()
        {
            Shader.SetGlobalFloat(_lineThicknessFieldName, Width);
            if (_mapInfo != null)
            {
                Shader.SetGlobalFloat(_mapScaleFieldName, _mapInfo.Scale);
            }
        }
    }
}

