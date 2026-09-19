using System;
using Mapbox.BaseModule.Map;
using UnityEngine;
using UnityEngine.Serialization;

namespace Mapbox.Example.Scripts.MapInput
{
    [Serializable]
    public class Moving3dCamera : MapInput
    {
        [Tooltip("Camera tilt angle. 90 = top-down, 15 = near-horizon")]
        [Range(15, 90)]
        public float Pitch;
        [Tooltip("Camera rotation in degrees. 0 = north up")]
        [Range(-180, 180)]
        public float Bearing;

        [NonSerialized] public float ZoomValue;
        [NonSerialized] public float CameraDistance;

        [Tooltip("Animation curve mapping zoom level to camera distance. X axis = zoom, Y axis = distance")]
        public AnimationCurveContainer CameraCurve;
        [Tooltip("Scales camera distance from curve output. Use to adjust height without re-authoring the curve. Default: 2")]
        [FormerlySerializedAs("CamDistanceMultiplier")]
        public float DistanceScale = 2;
        [Tooltip("Scroll wheel zoom sensitivity per step. Default: 0.25")]
        [FormerlySerializedAs("ZoomSpeed")]
        public float ZoomSensitivity = 0.25f;
        [Tooltip("Right-click rotation sensitivity. Higher = faster rotation. Default: 50")]
        public float RotationSpeed = 50.0f;

        private Vector3 _previousScreenPosition;
        private Vector3 _dragOrigin;
        private Vector3 _targetPosition;

        // Stored handlers so Teardown can detach from the IMapInformation events.
        // Without these, the closures root the camera (and _camera Transform) beyond
        // the owning behaviour's destruction.
        private Action<IMapInformation> _onLatLngChanged;
        private Action<IMapInformation> _onViewChanged;


        public override void Initialize(Camera camera, IMapInformation start)
        {
            base.Initialize(camera, start);
            Pitch = start.Pitch;
            Bearing = start.Bearing;
            ZoomValue = start.Zoom;
            SetCamera(start);
            _onLatLngChanged = information =>
            {
                _targetPosition = Vector3.zero;
                SetCamera(information);
            };
            _onViewChanged = SetCamera;
            start.LatitudeLongitudeChanged += _onLatLngChanged;
            start.ViewChanged += _onViewChanged;
        }

        public override void Teardown(IMapInformation mapInfo)
        {
            if (mapInfo == null) return;
            if (_onLatLngChanged != null) mapInfo.LatitudeLongitudeChanged -= _onLatLngChanged;
            if (_onViewChanged != null) mapInfo.ViewChanged -= _onViewChanged;
            _onLatLngChanged = null;
            _onViewChanged = null;
        }

        public override CameraOutput UpdateCamera(IMapInformation mapInformation)
        {
            _output.Reset();
            UpdateInputState();

            if (IsPointerOverUI())
                return _output;

            var pointerPos = GetPointerPosition();
            Vector3 cursorHit;
            if (!GetPlaneIntersection(pointerPos, out cursorHit))
                return _output;

            if (GetPointerDown() || GetSecondaryDown() || TouchCountDecreasedThisFrame)
            {
                _previousScreenPosition = pointerPos;
                _dragOrigin = cursorHit;
            }

            if (GetPointerHeld())
            {
                if (!GetPlaneIntersection(pointerPos, out var newPoint))
                    return _output;
                Vector3 pos = newPoint - _dragOrigin;
                // Skip the redraw when the held pointer didn't actually move (touch Stationary,
                // mouse-held-still). HasChanged=true here would trigger a full ChangeView+Load.
                if (pos.x != 0f || pos.z != 0f)
                {
                    _targetPosition -= new Vector3(pos.x, 0, pos.z);
                    _output.HasChanged = true;
                }
            }
            else if (GetSecondaryHeld())
            {
                var deltaMousePos = (pointerPos - _previousScreenPosition);
                var deltaAngleH = deltaMousePos.x;
                var deltaAngleV = deltaMousePos.y;
                if (deltaAngleH != 0 || deltaAngleV != 0)
                {
                    Pitch -= deltaAngleV * Time.deltaTime * RotationSpeed;
                    Pitch = ClampPitch(Pitch);
                    Bearing = ClampBearing(Bearing + deltaAngleH * Time.deltaTime * RotationSpeed);
                    _output.HasChanged = true;
                }
            }
            else if (GetPinchZoomDelta(out var zoomDelta))
            {
                var zoomCenter = GetZoomCenter();
                if (GetPlaneIntersection(zoomCenter, out var zoomHit))
                {
                    Zoom(mapInformation, zoomHit, zoomDelta);
                    _output.HasChanged = true;
                }
            }

            if (GetTwoFingerTiltDelta(out var tiltDelta))
            {
                Pitch -= tiltDelta * RotationSpeed;
                Pitch = ClampPitch(Pitch);
                _output.HasChanged = true;
            }

            GetPlaneIntersection(pointerPos, out _dragOrigin);
            _previousScreenPosition = pointerPos;

            if (_output.HasChanged)
            {
                SetCamPositionByMapInfo();
                _output.Zoom = ZoomValue;
                _output.Pitch = Pitch;
                _output.Bearing = Bearing;
            }
            return _output;
        }

        public void Zoom(IMapInformation mapInformation, Vector3 position, float zoomAction)
        {
            var postZoom = ClampZoom(ZoomValue + zoomAction * ZoomSensitivity);
				
            //to be able to achieve zoom on mouse cursor, we have to move camera on mouse world pos - camera pos line (b-c)
            //but our camera distance uses camera target (mid screen) to camera pos distance (a-c)
            //and there'll be a difference between (ac) and (bc) distance
            //so we calculate new distance and then use pre/post distance ratio to calculate the value on mouse-camera line
            //we then use this new (bc) distance for final pos calculation
            /// a----b
            /// |   / 
            /// |  / 
            /// | /
            /// c
            var newScaleWillBe = mapInformation.GetScaleFor(postZoom);
            var latlng = mapInformation.ConvertPositionToLatLng(position);
            var postZoomPos = mapInformation.ConvertLatLngToPositionForScale(latlng, newScaleWillBe);
            
            var targetPoslatlng = mapInformation.ConvertPositionToLatLng(_targetPosition);
            var postZoomTarget = mapInformation.ConvertLatLngToPositionForScale(targetPoslatlng, newScaleWillBe);
            
            
            var preDistance = CalculateCameraDistance(mapInformation, ZoomValue);
            var camDistanceToMouse = Vector3.Distance(_camera.transform.position, position);
            ZoomValue = postZoom;
            CameraDistance = CalculateCameraDistance(mapInformation, ZoomValue);

            // Guard against div-by-zero AND non-finite Scale: top-down orthographic,
            // first-frame state, a camera curve that evaluates to 0, or mapInformation.Scale
            // being 0 (early init) can make CalculateCameraDistance return Inf, which
            // passes a MinDistance check but later turns into Inf - Inf = NaN in the lerp.
            // Fall back to a straight target snap when the cursor-zoom math has nothing
            // meaningful to interpolate.
            const float MinDistance = 0.0001f;
            if (preDistance <= MinDistance || camDistanceToMouse <= MinDistance ||
                !float.IsFinite(preDistance) || !float.IsFinite(CameraDistance))
            {
                _targetPosition = postZoomTarget;
                return;
            }

            var postDistance = CameraDistance;
            var newCamDistanceToMouse = camDistanceToMouse * (postDistance / preDistance);
            var lerped = Vector3.LerpUnclamped(postZoomTarget, postZoomPos, (camDistanceToMouse - newCamDistanceToMouse) / camDistanceToMouse);
            // Belt-and-suspenders: if anything snuck a non-finite component through,
            // keep _targetPosition stable instead of corrupting it.
            if (float.IsFinite(lerped.x) && float.IsFinite(lerped.y) && float.IsFinite(lerped.z))
            {
                _targetPosition = lerped;
            }
            else
            {
                _targetPosition = postZoomTarget;
            }
        }

        private void SetCamPositionByMapInfo()
        {
            _camera.transform.position = _targetPosition;
            _camera.transform.rotation = Quaternion.Euler(Pitch, Bearing, 0);
            _camera.transform.position += _camera.transform.forward * (-1f * CameraDistance);
            GetPlaneIntersection(GetPointerPosition(), out _dragOrigin);
        }

        public void SetCamera(IMapInformation mapInfo)
        {
            Pitch = mapInfo.Pitch;
            Bearing = mapInfo.Bearing;

            CameraDistance = CalculateCameraDistance(mapInfo, mapInfo.Zoom);
            _camera.transform.position = _targetPosition;
            _camera.transform.rotation = Quaternion.Euler(Pitch, Bearing, 0);
            _camera.transform.position += _camera.transform.forward * (-1f * CameraDistance);
            GetPlaneIntersection(GetPointerPosition(), out _dragOrigin);
        }

        private float CalculateCameraDistance(IMapInformation mapInformation, float postZoom)
        {
            var distance = CameraCurve.Evaluate(postZoom);
            return DistanceScale * distance / mapInformation.Scale;
        }

        public Vector3 GetViewCenterPosition()
        {
            GetPlaneIntersection(_camera.ViewportToScreenPoint(new Vector3(.5f, .5f, 0f)), out var center);
            return center;
        }

    }
}