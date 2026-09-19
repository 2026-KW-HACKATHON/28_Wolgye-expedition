using Mapbox.BaseModule.Map;
using Mapbox.BaseModule.Unity;
using UnityEngine;

namespace Mapbox.BaseModule.Utilities
{
    public class MapShifterCore
    {
        private IMapInformation _mapInformation;
        private Vector2 _shiftRange;
        private Camera _camera;
        private UnityContext _unityContext;

        public MapShifterCore(IMapInformation mapInfo, UnityContext unityContext, Camera camera, Vector2 shiftRange)
        {
            _unityContext = unityContext;
            _mapInformation = mapInfo;
            _camera = camera;
            _shiftRange = shiftRange;
        }

        public void Update()
        {
            if (_mapInformation == null)
                return;

            var viewCenterPosition = GetViewCenterPosition();
            if(float.IsNaN(viewCenterPosition.x) || float.IsNaN(viewCenterPosition.y) || float.IsInfinity(viewCenterPosition.x) || float.IsInfinity(viewCenterPosition.y))
                return;
            
            if (Mathf.Abs(viewCenterPosition.x) > _shiftRange.x ||
                Mathf.Abs(viewCenterPosition.z) > _shiftRange.y)
            {
                var centerLatLng = _mapInformation.ConvertPositionToLatLng(viewCenterPosition);
                _mapInformation.SetLatitudeLongitude(centerLatLng);
                // localPosition (not position) so the shift stays in MapRoot-local space
                // and composes with any parent transform. Writing world-space here would
                // anchor RuntimeGenerationRoot in world coordinates — Unity would
                // back-compute a localPosition that counters the parent rotation, and
                // vector content would visually stop rotating with the rest of the map.
                _unityContext.RuntimeGenerationRoot.localPosition -= viewCenterPosition;
            }
        }

        private Vector3 GetPlaneIntersection(Vector3 screenPosition)
        {
            var ray = _camera.ScreenPointToRay(screenPosition);
            var dirNorm = ray.direction / ray.direction.y;
            var intersectionPos = ray.origin - dirNorm * ray.origin.y;
            return intersectionPos;
        }

        public Vector3 GetViewCenterPosition()
        {
            return GetPlaneIntersection(_camera.ViewportToScreenPoint(new Vector3(.5f, .5f, 0f)));
        }
    }
}