using System;
using Mapbox.BaseModule.Data.Vector2d;
using Mapbox.BaseModule.Map;
using Mapbox.BaseModule.Utilities;
using UnityEngine;

namespace Mapbox.Example.Scripts.MapInput
{
    [Serializable]
    public class SlippyMapCamera : MapInput
    {
        public enum RotationMode
        {
            RotateTheCamera,
            RotateTheMap
        }

        [Serializable]
        private class RotationSettings
        {
            [Tooltip("Enable right-click drag to rotate the camera or map")]
            public bool Enabled = true;
            [Tooltip("Rotation sensitivity. Higher values = faster rotation. Default: 50")]
            public float Speed = 50.0f;
            [Tooltip("RotateTheCamera: camera orbits the map. RotateTheMap: map rotates under a fixed camera")]
            public RotationMode rotationMode;
            [Tooltip("Root transform of the map. Auto-detected from MapBehaviourCore if left empty")]
            public Transform MapRoot;
        }

        [Serializable]
        private class ZoomSettings
        {
            [Tooltip("Enable scroll wheel to zoom in/out")]
            public bool Enabled = true;
            [Tooltip("Zoom sensitivity per scroll step. Default: 0.25")]
            public float Speed = 0.25f;
            [Tooltip("When enabled, zoom centers on cursor position. When disabled, zoom centers on map center")]
            public bool ZoomAtCursor = true;
        }

        [Tooltip("Initial map center in latitude/longitude. Example: 40.7128, -74.0060 for New York")]
        public LatitudeLongitude CenterLatitudeLongitude;
        [Tooltip("Camera tilt angle. 90 = top-down, 15 = near-horizon")]
        [Range(15, 90)]
        public float Pitch;
        [Tooltip("Camera rotation in degrees. 0 = north up")]
        [Range(-180, 180)]
        public float Bearing;
        [Tooltip("Distance from ground target to camera along its forward axis. Higher = further away. Typical range: 100–1000 depending on scale")]
        public float CameraDistance = 200f;
        [NonSerialized] public float ZoomValue;
        [NonSerialized] public float ScaleValue;

        private float _initialZoom;
        private float _initialScale;

        [Tooltip("Enable left-click drag to pan the map")]
        public bool PanEnabled = true;

        [Tooltip("Camera rotation settings")]
        [SerializeField] private RotationSettings _rotationSettings;
        [Tooltip("Zoom behaviour settings")]
        [SerializeField] private ZoomSettings _zoomSettings;
        private float _initialPitch;
        
        private Vector3 _previousScreenPosition;
        private Vector3 _dragOrigin;
        private Vector3 _targetPosition;

        public override void Initialize(Camera camera, IMapInformation start)
        {
            Initialize(camera, start, null);
        }

        public void Initialize(Camera camera, IMapInformation start, Plane? controlPlane)
        {
            base.Initialize(camera, start);
            if (controlPlane.HasValue) _controlPlane = controlPlane.Value;
            if (_rotationSettings.MapRoot == null)
            {
                var foundRoot = GameObject.FindObjectOfType<MapBehaviourCore>();
                if (foundRoot == null)
                {
                    Debug.LogError("SlippyMapCamera.Initialize: rotationSettings.MapRoot is unset and no MapBehaviourCore could be found in the scene. Camera rotation will not work; assign a MapRoot explicitly.");
                    return;
                }
                _rotationSettings.MapRoot = foundRoot.transform;
            }
            Pitch = start.Pitch;
            Bearing = start.Bearing;
            _initialZoom = start.Zoom;
            _initialScale = start.Scale;
            _initialPitch = Pitch;
            ZoomValue = start.Zoom;
            ScaleValue = start.Scale;
            SetCamPositionByMapInfo();
            start.SetView += SetCamera;
        }

        public override void Teardown(IMapInformation mapInfo)
        {
            // Detach the SetView subscription wired in Initialize. IMapInformation can
            // outlive this camera (DontDestroyOnLoad / scene reload), so without the
            // detach the closure roots us and _camera Transform indefinitely.
            if (mapInfo != null) mapInfo.SetView -= SetCamera;
        }
        
        private const float MaxMercatorLatitude = 85.06f;

        public override CameraOutput UpdateCamera(IMapInformation mapInformation)
        {
            _output.Reset();
            UpdateInputState();

            if (IsPointerOverUI())
                return _output;

            var pointerPos = GetPointerPosition();
            Vector3 cursorHit;
            Vector2d newMercatorCenter = mapInformation.CenterMercator;

            if (!GetPlaneIntersection(pointerPos, out cursorHit))
                return _output;

            if (GetPointerDown() || GetSecondaryDown() || TouchCountDecreasedThisFrame)
            {
                _previousScreenPosition = pointerPos;
                _dragOrigin = cursorHit;
            }

            if (GetPointerHeld() && PanEnabled)
            {
                var oldCursorLatLng = Conversions.LatitudeLongitudeToWebMercator(mapInformation.ConvertPositionToLatLng(_rotationSettings.MapRoot.InverseTransformPoint(_dragOrigin)));
                var newCursorLatLng = Conversions.LatitudeLongitudeToWebMercator(mapInformation.ConvertPositionToLatLng(_rotationSettings.MapRoot.InverseTransformPoint(cursorHit)));
                var mercatorDelta = newCursorLatLng - oldCursorLatLng;
                // Skip the redraw when the cursor hit didn't actually move (Stationary
                // touch, mouse-held-still). Avoids per-frame ChangeView round-trips.
                if (mercatorDelta.x != 0 || mercatorDelta.y != 0)
                {
                    newMercatorCenter = mapInformation.CenterMercator - mercatorDelta;
                    _output.HasChanged = true;
                }
            }
            else if (GetSecondaryHeld() && _rotationSettings.Enabled)
            {
                var deltaMousePos = (pointerPos - _previousScreenPosition);
                var deltaAngleH = deltaMousePos.x;
                var deltaAngleV = deltaMousePos.y;
                if (deltaAngleH != 0 || deltaAngleV != 0)
                {
                    var currentPitch = deltaAngleV * Time.deltaTime * _rotationSettings.Speed;
                    Pitch -= currentPitch;
                    Pitch = ClampPitch(Pitch);
                    var currentBearing = deltaAngleH * Time.deltaTime * _rotationSettings.Speed;
                    Bearing = ClampBearing(Bearing + currentBearing);
                    _output.HasChanged = true;
                }
            }
            else if (GetPinchZoomDelta(out var zoomDelta) && _zoomSettings.Enabled)
            {
                var zoomCenter = GetZoomCenter();
                if (!_zoomSettings.ZoomAtCursor)
                {
                    ZoomValue = ClampZoom(ZoomValue + zoomDelta * _zoomSettings.Speed);
                    ScaleValue = _initialScale / Mathf.Pow(2, (ZoomValue - _initialZoom));
                    _output.HasChanged = true;
                }
                else if (GetPlaneIntersection(zoomCenter, out var zoomHit))
                {
                    var startingMercatorCursor = Conversions.LatitudeLongitudeToWebMercator(mapInformation.ConvertPositionToLatLng(_rotationSettings.MapRoot.InverseTransformPoint(zoomHit)));
                    ZoomValue = ClampZoom(ZoomValue + zoomDelta * _zoomSettings.Speed);
                    ScaleValue = _initialScale / Mathf.Pow(2, (ZoomValue - _initialZoom));
                    var newMercatorCursor = Conversions.LatitudeLongitudeToWebMercator(mapInformation.ConvertPositionToLatLngForScale(_rotationSettings.MapRoot.InverseTransformPoint(zoomHit), ScaleValue));
                    var change = newMercatorCursor - startingMercatorCursor;
                    newMercatorCenter = mapInformation.CenterMercator - change;
                    _output.HasChanged = true;
                }
            }

            if (GetTwoFingerTiltDelta(out var tiltDelta))
            {
                Pitch -= tiltDelta * _rotationSettings.Speed;
                Pitch = ClampPitch(Pitch);
                _output.HasChanged = true;
            }

            _dragOrigin = cursorHit;
            _previousScreenPosition = pointerPos;

            if (_output.HasChanged)
            {
                var latLng = Conversions.WebMercatorToLatLon(newMercatorCenter);
                CenterLatitudeLongitude = new LatitudeLongitude(
                    System.Math.Clamp(latLng.Latitude, -(double)MaxMercatorLatitude, MaxMercatorLatitude),
                    latLng.Longitude);
                SetCamPositionByMapInfo();
                _output.Center = CenterLatitudeLongitude;
                _output.Zoom = ZoomValue;
                _output.Pitch = Pitch;
                _output.Bearing = Bearing;
                _output.Scale = ScaleValue;
            }

            return _output;
        }

        private void SetCamPositionByMapInfo()
        {
            _camera.transform.position = _targetPosition;

            if (_rotationSettings.rotationMode == RotationMode.RotateTheCamera)
            {
                _camera.transform.rotation = Quaternion.Euler(Pitch, Bearing, 0);
            }
            else if (_rotationSettings.rotationMode == RotationMode.RotateTheMap)
            {
                var projectedForward = Vector3.ProjectOnPlane(_camera.transform.forward, _controlPlane.normal);
                var verticalRotation = Quaternion.AngleAxis(Pitch-_initialPitch, projectedForward.Perpendicular());
                _rotationSettings.MapRoot.rotation = verticalRotation * Quaternion.Euler(0, Bearing, 0);
            }

            _camera.transform.position += _camera.transform.forward * (-1f * CameraDistance);
        }

        public void SetCamera(IMapInformation mapInfo)
        {
            _targetPosition = Vector3.zero;
            Pitch = mapInfo.Pitch;
            Bearing = mapInfo.Bearing;

            if (_rotationSettings.rotationMode == RotationMode.RotateTheCamera)
            {
                SetCamPositionByMapInfo();
            }
            else if (_rotationSettings.rotationMode == RotationMode.RotateTheMap)
            {
                var projectedForward = Vector3.ProjectOnPlane(_camera.transform.forward, _controlPlane.normal);
                var verticalRotation = Quaternion.AngleAxis(Pitch-_initialPitch, projectedForward.Perpendicular());
                _rotationSettings.MapRoot.rotation = verticalRotation * Quaternion.Euler(0, Bearing, 0);
            }
        }

    }
}