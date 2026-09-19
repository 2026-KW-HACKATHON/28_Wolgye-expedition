using UnityEngine;
using UnityEngine.EventSystems;

namespace Mapbox.Example.Scripts.MapInput
{
	/// <summary>
	/// Mode-specific input handler. One implementation covers keyboard/mouse and
	/// another covers touch; <see cref="MapInput"/> picks one at compile time based
	/// on the build target. Each implementation covers the full vocabulary — methods
	/// that don't make sense for the mode (e.g. secondary button on touch, two-finger
	/// tilt on mouse) just return false / zero.
	/// </summary>
	internal interface IInputHandler
	{
		void Initialize();
		void UpdateState();

		bool IsPointerOverUI();
		Vector3 GetPointerPosition();
		bool GetPointerDown();
		bool GetPointerHeld();
		bool GetSecondaryDown();
		bool GetSecondaryHeld();
		bool TryGetPinchZoomDelta(out float zoomDelta, float pinchSensitivity);
		bool TryGetTwoFingerTiltDelta(out float tiltDelta, float tiltSensitivity);
		Vector3 GetZoomCenter();
		bool TouchCountDecreasedThisFrame { get; }
		int GetTouchCount();
	}

	/// <summary>
	/// Keyboard / mouse handler. Used on Editor and desktop standalone builds.
	/// Touch-only concepts (touch count, pinch, tilt, etc.) are no-ops.
	/// </summary>
	internal sealed class MouseInputHandler : IInputHandler
	{
		private readonly IPointerInput _input;

		public MouseInputHandler(IPointerInput input) { _input = input; }

		public void Initialize() { /* legacy mouse needs no setup */ }
		public void UpdateState() { /* mouse has no per-frame gesture mutex */ }

		public bool TouchCountDecreasedThisFrame => false;
		public int GetTouchCount() => 0;

		public bool IsPointerOverUI()
		{
			if (EventSystem.current == null) return false;
			return EventSystem.current.IsPointerOverGameObject();
		}

		public Vector3 GetPointerPosition() => _input.MousePosition;
		public bool GetPointerDown() => _input.MouseLeftPressedThisFrame;
		public bool GetPointerHeld() => _input.MouseLeftHeld;
		public bool GetSecondaryDown() => _input.MouseRightPressedThisFrame;
		public bool GetSecondaryHeld() => _input.MouseRightHeld;

		public bool TryGetPinchZoomDelta(out float zoomDelta, float pinchSensitivity)
		{
			// Scroll wheel; pinchSensitivity is unused on mouse — the wheel already
			// emits per-notch deltas via IPointerInput.MouseScrollY.
			var scrollY = _input.MouseScrollY;
			if (Mathf.Abs(scrollY) > 0f)
			{
				zoomDelta = scrollY;
				return true;
			}
			zoomDelta = 0f;
			return false;
		}

		public bool TryGetTwoFingerTiltDelta(out float tiltDelta, float tiltSensitivity)
		{
			tiltDelta = 0f;
			return false;
		}

		public Vector3 GetZoomCenter() => _input.MousePosition;
	}

	/// <summary>
	/// Touch handler. Used on Android / iOS device builds. Mouse-only concepts
	/// (secondary button, scroll wheel) are no-ops; pinch + tilt are recognised
	/// via a per-frame two-finger gesture mutex run in <see cref="UpdateState"/>.
	/// </summary>
	internal sealed class TouchInputHandler : IInputHandler
	{
		private readonly IPointerInput _input;

		// Per-frame two-finger gesture decision. Pinch vs tilt is exclusive; the
		// winner is selected once per frame in UpdateState so the two query helpers
		// (TryGetPinchZoomDelta / TryGetTwoFingerTiltDelta) read consistent state.
		private enum Gesture { None, Pinch, Tilt }
		private Gesture _gesture;
		private float _pinchDelta;
		private float _tiltAvgDeltaY;

		// Pinch state tracking. _previousTouch{0,1}Id let us detect 2→3→2 finger
		// transitions where EnhancedTouch shifted which physical touches occupy
		// slots [0..1] — without this, the next frame compares distance against
		// a different pair and spikes a phantom zoom.
		private float _previousPinchDistance;
		private bool _pinchActive;
		private int _previousTouchCount;
		private int _previousTouch0Id = int.MinValue;
		private int _previousTouch1Id = int.MinValue;

		public bool TouchCountDecreasedThisFrame { get; private set; }

		public TouchInputHandler(IPointerInput input) { _input = input; }

		public void Initialize() { _input.EnableTouchSupport(); }
		public int GetTouchCount() => _input.TouchCount;

		public bool IsPointerOverUI()
		{
			if (EventSystem.current == null) return false;
			if (_input.TouchCount > 0)
				return EventSystem.current.IsPointerOverGameObject(_input.GetTouchPointerId(0));
			return EventSystem.current.IsPointerOverGameObject();
		}

		public Vector3 GetPointerPosition()
		{
			return _input.TouchCount > 0 ? (Vector3)_input.GetTouchPosition(0) : Vector3.zero;
		}

		public bool GetPointerDown()
		{
			if (_input.TouchCount <= 0) return false;
			return _input.GetTouchPhase(0) == PointerTouchPhase.Began;
		}

		public bool GetPointerHeld()
		{
			if (_input.TouchCount != 1) return false;
			var phase = _input.GetTouchPhase(0);
			return phase == PointerTouchPhase.Moved || phase == PointerTouchPhase.Stationary;
		}

		// Touch has no secondary pointer; the camera's rotate-via-secondary code
		// (SlippyMapCamera right-click rotation, Moving3dCamera right-click pitch+yaw)
		// stays dormant on this handler.
		public bool GetSecondaryDown() => false;
		public bool GetSecondaryHeld() => false;

		public bool TryGetPinchZoomDelta(out float zoomDelta, float pinchSensitivity)
		{
			if (_gesture == Gesture.Pinch)
			{
				zoomDelta = _pinchDelta / Mathf.Max(Screen.height, 1) * pinchSensitivity;
				return true;
			}
			zoomDelta = 0f;
			return false;
		}

		public bool TryGetTwoFingerTiltDelta(out float tiltDelta, float tiltSensitivity)
		{
			if (_gesture == Gesture.Tilt)
			{
				tiltDelta = _tiltAvgDeltaY / Mathf.Max(Screen.height, 1) * tiltSensitivity;
				return true;
			}
			tiltDelta = 0f;
			return false;
		}

		public Vector3 GetZoomCenter()
		{
			if (_input.TouchCount >= 2)
			{
				Vector3 t0 = _input.GetTouchPosition(0);
				Vector3 t1 = _input.GetTouchPosition(1);
				return (t0 + t1) / 2f;
			}
			return Vector3.zero;
		}

		public void UpdateState()
		{
			var touchCount = _input.TouchCount;
			TouchCountDecreasedThisFrame = touchCount < _previousTouchCount;
			_previousTouchCount = touchCount;

			_gesture = Gesture.None;
			_pinchDelta = 0f;
			_tiltAvgDeltaY = 0f;

			if (touchCount < 2)
			{
				_pinchActive = false;
				_previousTouch0Id = int.MinValue;
				_previousTouch1Id = int.MinValue;
				return;
			}

			var touch0Pos = _input.GetTouchPosition(0);
			var touch1Pos = _input.GetTouchPosition(1);
			var touch0DeltaY = _input.GetTouchDeltaY(0);
			var touch1DeltaY = _input.GetTouchDeltaY(1);
			var t0Id = _input.GetTouchId(0);
			var t1Id = _input.GetTouchId(1);
			var currentDistance = Vector2.Distance(touch0Pos, touch1Pos);

			bool pairChanged = !_pinchActive ||
			                   t0Id != _previousTouch0Id || t1Id != _previousTouch1Id;
			if (pairChanged)
			{
				_pinchActive = true;
				_previousPinchDistance = currentDistance;
				_previousTouch0Id = t0Id;
				_previousTouch1Id = t1Id;
				return;
			}

			var pinchDelta = currentDistance - _previousPinchDistance;
			_previousPinchDistance = currentDistance;

			var pinchMag = Mathf.Abs(pinchDelta);
			var avgDeltaY = (touch0DeltaY + touch1DeltaY) / 2f;
			var tiltMag = Mathf.Abs(avgDeltaY);
			var bothFingersSameDirection = touch0DeltaY * touch1DeltaY > 0f;

			// Normalize both magnitudes to Screen.height so the thresholds are DPI-
			// independent. MinFrac ≈ 1 pixel on a 1080-height screen. Pinch wins
			// ties (pinchFrac >= tiltFrac) to avoid "None" frames when magnitudes
			// are exactly equal (common on near-pure vertical drags).
			var screenH = Mathf.Max(Screen.height, 1);
			var pinchFrac = pinchMag / screenH;
			var tiltFrac = tiltMag / screenH;
			const float MinFrac = 1f / 1080f;

			if (pinchFrac > MinFrac && pinchFrac >= tiltFrac)
			{
				_gesture = Gesture.Pinch;
				_pinchDelta = pinchDelta;
			}
			else if (bothFingersSameDirection && tiltFrac > MinFrac)
			{
				_gesture = Gesture.Tilt;
				_tiltAvgDeltaY = avgDeltaY;
			}
		}
	}
}
