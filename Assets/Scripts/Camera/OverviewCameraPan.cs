using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.Serialization;
using Unity.Cinemachine;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

/// <summary>
/// Overview 모드 카메라 조작
/// - 한 손가락 / 마우스 드래그: 이동
/// - 두 손가락 핀치: 줌 (두 손가락 중심 기준) + 두 손가락 이동
/// - 마우스 휠: 줌 (커서 위치 기준)
///
/// Pan Plane
/// - XY (2D) : 카메라가 Z축 방향을 바라보는 2D/사이드뷰. X·Y로 이동
/// - XZ (3D 지면) : 카메라가 위에서 내려다보는 3D. X·Z로 이동
/// </summary>
public class OverviewCameraPan : MonoBehaviour
{
    public enum PanPlane
    {
        XY_2D,
        XZ_Ground
    }

    [Header("References")]
    [SerializeField] private CinemachineCamera overviewCamera;
    [SerializeField] private CameraModeSwitcher modeSwitcher;
    [SerializeField] private CinemachineBrain brain;

    [Header("Pan Plane")]
    [SerializeField] private PanPlane panPlane = PanPlane.XY_2D;
    [Tooltip("XY: 평면의 Z 위치 / XZ: 지면의 Y 높이")]
    [FormerlySerializedAs("groundHeight")]
    [SerializeField] private float planeOffset = 0f;

    [Header("Bounds (XY 모드: x,y / XZ 모드: x,z)")]
    [SerializeField] private bool useBounds = true;
    [FormerlySerializedAs("minXZ")]
    [SerializeField] private Vector2 boundsMin = new Vector2(-50f, -50f);
    [FormerlySerializedAs("maxXZ")]
    [SerializeField] private Vector2 boundsMax = new Vector2(50f, 50f);

    [Header("Zoom - Perspective (평면까지의 거리)")]
    [FormerlySerializedAs("minHeight")]
    [SerializeField] private float minDistance = 5f;
    [FormerlySerializedAs("maxHeight")]
    [SerializeField] private float maxDistance = 60f;

    [Header("Zoom - Orthographic (OrthographicSize)")]
    [SerializeField] private float minOrthoSize = 3f;
    [SerializeField] private float maxOrthoSize = 30f;

    [Header("Zoom - Mouse Wheel")]
    [Tooltip("휠 한 칸당 줌 비율 (0.1 = 10%)")]
    [Range(0.01f, 0.5f)]
    [SerializeField] private float wheelZoomStep = 0.1f;

    private Camera outputCamera;
    private bool isDragging;
    private bool wasPinching;
    private Vector3 dragStartWorld;
    private readonly List<RaycastResult> uiHits = new List<RaycastResult>();

    private Vector3 PlaneNormal => panPlane == PanPlane.XY_2D ? Vector3.forward : Vector3.up;

    private Vector3 PlanePoint => panPlane == PanPlane.XY_2D
        ? new Vector3(0f, 0f, planeOffset)
        : new Vector3(0f, planeOffset, 0f);

    private Camera OutputCam
    {
        get
        {
            if (outputCamera == null)
            {
                if (brain != null)
                {
                    outputCamera = brain.OutputCamera;
                    if (outputCamera == null) brain.TryGetComponent(out outputCamera);
                }
                if (outputCamera == null) outputCamera = Camera.main;
            }
            return outputCamera;
        }
    }

    private void OnEnable() => EnhancedTouchSupport.Enable();
    private void OnDisable() => EnhancedTouchSupport.Disable();

    private void Update()
    {
        // 팔로우 모드이거나 카메라 전환(블렌드) 중에는 조작 막기
        if ((modeSwitcher != null && modeSwitcher.IsFollowing) ||
            (brain != null && brain.IsBlending))
        {
            isDragging = false;
            wasPinching = false;
            return;
        }

        // 1) 핀치 (두 손가락 이상)
        if (HandlePinch())
        {
            isDragging = false;
            wasPinching = true;
            return;
        }

        var pointer = Pointer.current;
        if (pointer == null) return;
        Vector2 screenPos = pointer.position.ReadValue();

        // 핀치 후 한 손가락이 남아 있으면 그 위치로 드래그 기준점을 다시 잡아 튐 방지
        if (wasPinching)
        {
            wasPinching = false;
            if (pointer.press.isPressed && TryGetPlanePoint(screenPos, out dragStartWorld))
                isDragging = true;
            return;
        }

        // 2) 마우스 휠 줌
        HandleMouseWheel();

        // 3) 한 손가락 / 마우스 드래그 이동
        if (pointer.press.wasPressedThisFrame)
        {
            if (IsOverUI(screenPos)) return;
            if (TryGetPlanePoint(screenPos, out dragStartWorld))
                isDragging = true;
        }
        else if (pointer.press.wasReleasedThisFrame)
        {
            isDragging = false;
        }

        if (isDragging && pointer.press.isPressed &&
            TryGetPlanePoint(screenPos, out Vector3 current))
        {
            // 두 점 모두 평면 위의 점이므로 delta도 평면 방향 성분만 가진다
            Vector3 delta = dragStartWorld - current;
            SetCameraPosition(overviewCamera.transform.position + delta);
        }
    }

    // ───────────────────────── Pinch ─────────────────────────

    private bool HandlePinch()
    {
        var touches = Touch.activeTouches;
        if (touches.Count < 2) return false;

        Touch t0 = touches[0];
        Touch t1 = touches[1];

        // UI 위에서 시작한 손가락이 있으면 무시
        if (IsOverUI(t0.startScreenPosition) || IsOverUI(t1.startScreenPosition))
            return true;

        Vector2 curr0 = t0.screenPosition;
        Vector2 curr1 = t1.screenPosition;
        Vector2 prev0 = curr0 - t0.delta;
        Vector2 prev1 = curr1 - t1.delta;

        float prevDist = Vector2.Distance(prev0, prev1);
        float currDist = Vector2.Distance(curr0, curr1);
        if (prevDist < 1f || currDist < 1f) return true;

        Vector2 prevMid = (prev0 + prev1) * 0.5f;
        Vector2 currMid = (curr0 + curr1) * 0.5f;

        // 두 손가락 이동(팬)
        if (TryGetPlanePoint(prevMid, out Vector3 prevWorld) &&
            TryGetPlanePoint(currMid, out Vector3 currWorld))
        {
            SetCameraPosition(overviewCamera.transform.position + (prevWorld - currWorld));
        }

        // 손가락이 벌어지면 factor < 1 → 줌 인
        ZoomAt(currMid, prevDist / currDist);
        return true;
    }

    // ─────────────────────── Mouse Wheel ───────────────────────

    private void HandleMouseWheel()
    {
        var mouse = Mouse.current;
        if (mouse == null) return;

        float scroll = mouse.scroll.ReadValue().y;
        if (Mathf.Approximately(scroll, 0f)) return;

        Vector2 mousePos = mouse.position.ReadValue();
        if (IsOverUI(mousePos)) return;

        // 플랫폼마다 휠 값 크기가 달라서 방향만 사용
        float factor = scroll > 0f ? 1f - wheelZoomStep : 1f + wheelZoomStep;
        ZoomAt(mousePos, factor);
    }

    // ───────────────────────── Zoom ─────────────────────────

    /// <summary>
    /// screenPos 아래의 평면 지점을 고정한 채로 줌한다.
    /// factor &lt; 1 이면 줌 인, &gt; 1 이면 줌 아웃.
    /// </summary>
    private void ZoomAt(Vector2 screenPos, float factor)
    {
        if (!TryGetPlanePoint(screenPos, out Vector3 focus)) return;

        Transform t = overviewCamera.transform;
        Vector3 offset = t.position - focus;

        if (OutputCam.orthographic)
        {
            float size = overviewCamera.Lens.OrthographicSize;
            float newSize = Mathf.Clamp(size * factor, minOrthoSize, maxOrthoSize);
            factor = newSize / size;
            overviewCamera.Lens.OrthographicSize = newSize;

            // 시선 방향 성분은 그대로 두고, 화면 평면 성분만 스케일
            Vector3 along = Vector3.Project(offset, t.forward);
            Vector3 perp = offset - along;
            SetCameraPosition(focus + along + perp * factor);
        }
        else
        {
            // 평면까지의 수직 거리 기준으로 제한
            float distance = Mathf.Abs(Vector3.Dot(offset, PlaneNormal));
            if (distance <= 0.01f) return;

            float newDistance = Mathf.Clamp(distance * factor, minDistance, maxDistance);
            factor = newDistance / distance;

            SetCameraPosition(focus + offset * factor);
        }
    }

    // ───────────────────────── Utils ─────────────────────────

    private void SetCameraPosition(Vector3 pos)
    {
        if (useBounds)
        {
            pos.x = Mathf.Clamp(pos.x, boundsMin.x, boundsMax.x);

            if (panPlane == PanPlane.XY_2D)
                pos.y = Mathf.Clamp(pos.y, boundsMin.y, boundsMax.y);
            else
                pos.z = Mathf.Clamp(pos.z, boundsMin.y, boundsMax.y);
        }
        overviewCamera.transform.position = pos;
    }

    private bool TryGetPlanePoint(Vector2 screenPos, out Vector3 point)
    {
        point = default;
        Camera cam = OutputCam;
        if (cam == null) return false;

        var plane = new Plane(PlaneNormal, PlanePoint);
        Ray ray = cam.ScreenPointToRay(screenPos);
        if (plane.Raycast(ray, out float enter))
        {
            point = ray.GetPoint(enter);
            return true;
        }
        return false;
    }

    private bool IsOverUI(Vector2 screenPos)
    {
        if (EventSystem.current == null) return false;

        var data = new PointerEventData(EventSystem.current) { position = screenPos };
        uiHits.Clear();
        EventSystem.current.RaycastAll(data, uiHits);
        return uiHits.Count > 0;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (!useBounds) return;
        Gizmos.color = Color.cyan;

        Vector2 c = (boundsMin + boundsMax) * 0.5f;
        Vector2 s = boundsMax - boundsMin;

        if (panPlane == PanPlane.XY_2D)
            Gizmos.DrawWireCube(new Vector3(c.x, c.y, planeOffset), new Vector3(s.x, s.y, 0.1f));
        else
            Gizmos.DrawWireCube(new Vector3(c.x, planeOffset, c.y), new Vector3(s.x, 0.1f, s.y));
    }
#endif
}