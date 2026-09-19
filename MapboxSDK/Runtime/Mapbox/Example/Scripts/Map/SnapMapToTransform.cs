using Mapbox.BaseModule.Map;
using Mapbox.BaseModule.Utilities;
using UnityEngine;

namespace Mapbox.Example.Scripts.Map
{
    public class SnapMapToTransform : MonoBehaviour
    {
        public Transform Transform;
        [SerializeField] private MapBehaviourCore _mapCore;
        private MapboxMap _map;
    
        void Start()
        {
            _mapCore.Initialized += (map) => _map = map;
        }

        void Update()
        {
            if (_map != null && _map.Status >= InitializationStatus.ReadyForUpdates)
            {
                var latlng = _map.MapInformation.ConvertPositionToLatLng(Transform.position);
                _map.ChangeView(latlng);
            }
        }
    }
}
