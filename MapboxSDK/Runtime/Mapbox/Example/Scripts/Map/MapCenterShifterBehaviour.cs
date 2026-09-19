using Mapbox.BaseModule.Map;
using Mapbox.BaseModule.Utilities;
using UnityEngine;

namespace Mapbox.Example.Scripts.Map
{
    public class MapCenterShifterBehaviour : MonoBehaviour
    {
        public MapBehaviourCore MapBehaviour;
        public Camera Camera;
        private MapShifterCore _mapShifterCore;
        public Vector2 ShiftRange = new Vector2(5000, 5000);
        private MapboxMap _map;

        private void Awake()
        {
            MapBehaviour.Initialized += (map) =>
            {
                _map = map;
                _mapShifterCore = new MapShifterCore(_map.MapInformation, _map.UnityContext, Camera, ShiftRange);
            };
        }

        private void Update()
        {
            if (_mapShifterCore != null)
            {
                _mapShifterCore.Update();
            }
        }
    }
}