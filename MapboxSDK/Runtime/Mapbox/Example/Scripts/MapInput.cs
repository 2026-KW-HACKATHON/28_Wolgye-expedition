using Mapbox.BaseModule.Data.Vector2d;
using Mapbox.BaseModule.Map;
using Mapbox.BaseModule.Utilities;
using UnityEngine;

namespace Mapbox.Example.Scripts.MapInput
{
	/// <summary>
	/// Reusable output from UpdateCamera. Populated each frame by the camera implementation.
	/// Passed to MapboxMap.ChangeView — null fields mean "no change".
	/// </summary>
	public class CameraOutput
	{
		public LatitudeLongitude? Center;
		public float? Zoom;
		public float? Pitch;
		public float? Bearing;
		public float? Scale;
		public bool HasChanged;

		public void Reset()
		{
			Center = null;
			Zoom = null;
			Pitch = null;
			Bearing = null;
			Scale = null;
			HasChanged = false;
		}
	}

	public abstract class MapInput
	{
		protected Camera _camera;
		protected Plane _controlPlane = new Plane(Vector3.up, Vector3.zero);
		protected readonly CameraOutput _output = new CameraOutput();

		// Per-platform handler. Editor and standalone builds use the mouse handler;
		// Android/iOS device builds use the touch handler. The split is compile-time
		// (no runtime detection) — each platform gets a single-purpose input path
		// instead of a unified shim that has to cover both worlds at once.
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
		private readonly IInputHandler _handler = new TouchInputHandler(new PointerInput());
#else
		private readonly IInputHandler _handler = new MouseInputHandler(new PointerInput());
#endif

		public virtual void Initialize(Camera camera, IMapInformation mapInfo)
		{
			_camera = camera ? camera : Camera.main;
			_handler.Initialize();
		}

		/// <summary>
		/// Called from the owning behaviour's OnDestroy. Override to unsubscribe from
		/// <see cref="IMapInformation"/> events you wired in <see cref="Initialize"/> —
		/// IMapInformation can outlive the camera (DontDestroyOnLoad / scene reload),
		/// and event closures otherwise root the destroyed camera + its Camera Transform.
		/// </summary>
		public virtual void Teardown(IMapInformation mapInfo) { }

		public abstract CameraOutput UpdateCamera(IMapInformation mapInfo);

		#region Plane Intersection

		protected bool GetPlaneIntersection(Vector3 screenPosition, out Vector3 hit)
		{
			return _camera.GetPlaneIntersection(_controlPlane, screenPosition, out hit);
		}

		#endregion

		#region Camera Utilities

		public Plane[] GetFrustumPlanes()
		{
			return GeometryUtility.CalculateFrustumPlanes(_camera);
		}

		public Transform GetTransform()
		{
			return _camera.transform;
		}

		#endregion

		#region Value Clamping

		// Absolute Mercator pyramid max. Mirrors MapboxMapVisualizer.MaxMercatorZoom
		// (`int 22`) — kept here as a float const because C# default-parameter values
		// must be compile-time constants of the parameter's exact type. If the pyramid
		// cap ever changes, update both this and MapboxMapVisualizer.MaxMercatorZoom.
		public const float MaxClampZoom = 22f;

		protected static float ClampZoom(float zoom, float min = 0f, float max = MaxClampZoom)
		{
			return Mathf.Clamp(zoom, min, max);
		}

		protected static float ClampPitch(float pitch, float min = 15f, float max = 90f)
		{
			return Mathf.Clamp(pitch, min, max);
		}

		protected static float ClampBearing(float bearing)
		{
			bearing %= 360f;
			if (bearing > 180f) bearing -= 360f;
			if (bearing < -180f) bearing += 360f;
			return bearing;
		}

		#endregion

		#region Input Helpers

		// All input reads delegate to _handler. Touch-only state (pinch / tilt
		// mutex, touch-pair tracking) lives entirely inside TouchInputHandler;
		// mouse-only state (RMB held) lives entirely inside MouseInputHandler.

		/// <summary>
		/// Call at the start of UpdateCamera to maintain input state between frames.
		/// On touch: resets pinch tracking, runs the per-frame pinch/tilt decision.
		/// On mouse: no-op.
		/// </summary>
		protected void UpdateInputState() => _handler.UpdateState();

		/// <summary>
		/// True on the frame the active touch count drops (e.g. 2→1 when one finger
		/// lifts during a pinch). Cameras should treat this as a fresh drag start so
		/// _dragOrigin is reset to the surviving finger's position. Always false on
		/// the mouse handler.
		/// </summary>
		protected bool TouchCountDecreasedThisFrame => _handler.TouchCountDecreasedThisFrame;

		private int GetTouchCount() => _handler.GetTouchCount();

		protected bool IsPointerOverUI() => _handler.IsPointerOverUI();
		protected Vector3 GetPointerPosition() => _handler.GetPointerPosition();
		protected bool GetPointerDown() => _handler.GetPointerDown();
		protected bool GetPointerHeld() => _handler.GetPointerHeld();
		protected bool GetSecondaryDown() => _handler.GetSecondaryDown();
		protected bool GetSecondaryHeld() => _handler.GetSecondaryHeld();

		/// <summary>
		/// Returns true if zoom input was detected this frame. On touch: from the
		/// pinch gesture cached by UpdateState. On mouse: from the scroll wheel.
		/// </summary>
		protected bool GetPinchZoomDelta(out float zoomDelta, float pinchSensitivity = 5f)
			=> _handler.TryGetPinchZoomDelta(out zoomDelta, pinchSensitivity);

		/// <summary>
		/// Returns true if a two-finger vertical-tilt gesture was detected this frame.
		/// Always false on the mouse handler.
		/// </summary>
		protected bool GetTwoFingerTiltDelta(out float tiltDelta, float tiltSensitivity = 0.5f)
			=> _handler.TryGetTwoFingerTiltDelta(out tiltDelta, tiltSensitivity);

		protected Vector3 GetZoomCenter() => _handler.GetZoomCenter();

		#endregion
	}
}
