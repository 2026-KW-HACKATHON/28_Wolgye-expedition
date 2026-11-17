using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// QR 전용: 1손가락 이동 + 2손가락 핀치 거리 조절 → 손을 떼면 가상 바닥 갱신.
/// 일반 AR Plane 생성/추적은 수행하지 않는다.
/// </summary>
public class QRDalsooDragFloor : MonoBehaviour
{
    [Header("QR Floor")]
    [SerializeField, Min(0f)] private float floorBelowActor = 0.08f;
    [SerializeField, Min(0.3f)] private float movementRadius = 0.75f;
    [SerializeField] private bool keepMovementOnScreen = true;
    [SerializeField, Range(0.01f, 0.4f)] private float screenMargin = 0.13f;

    [Header("QR Visual Facing")]
    [SerializeField] private bool centerModelOnSpawn = true;
    [SerializeField] private bool faceCameraVertically = true;
    [SerializeField, Range(0f, 85f)] private float maxVisualTiltDegrees = 75f;
    [SerializeField, Min(0f)] private float visualTiltFollowSpeed = 12f;
    [SerializeField] private bool invertVisualTilt = false;

    [Header("Two Finger Pinch (QR Only)")]
    [SerializeField] private bool enablePinchZoom = true;
    [Tooltip("두 손가락을 벌리면 가까워지고, 모으면 멀어집니다. AR 월드 위치를 이동하며 모델 크기는 변경하지 않습니다.")]
    [SerializeField, Min(0.3f)] private float minPinchDistance = 0.65f;
    [SerializeField, Min(0.5f)] private float maxPinchDistance = 4f;
    [Tooltip("1 = 손가락 간격 변화에 비례해서 거리 이동")]
    [SerializeField, Range(0.25f, 3f)] private float pinchSensitivity = 1f;
    [Tooltip("줌 위치를 따라가는 속도. 값이 높을수록 즉각 반응합니다.")]
    [SerializeField, Min(0f)] private float pinchFollowSpeed = 18f;
    [Tooltip("ON이면 화면 어느 곳에서든 두 손가락으로 줌 가능. OFF면 달수를 한 손가락 이상 터치해야 줌됩니다.")]
    [SerializeField] private bool pinchAnywhereOnScreen = true;

    [Header("Touch")]
    [SerializeField] private bool addColliderIfMissing = true;
    [SerializeField, Min(0.1f)] private float maxRaycastDistance = 15f;

    private Camera arCamera;
    private DalsooBehaviorController behavior;
    private Transform virtualFloor;
    private float modelRotationOffsetY = 90f;
    private bool configured;
    private bool dragging;
    private bool pinching;
    private float pinchStartSpan;
    private float pinchStartDepth;
    private Vector2 pinchRootScreenPosition;
    private float heldDepth;
    private Vector3 touchOffset;
    private Transform visualPitchPivot;
    private DalsuActor actor;

    private enum PointerPhase { None, Began, Holding, Ended }

    public void Setup(
        Camera camera,
        float modelYawOffset = 90f,
        float floorOffset = 0.08f,
        float radius = 0.75f,
        bool constrainToScreen = true,
        float viewportMargin = 0.13f,
        bool autoCollider = true,
        bool centerOnSpawn = true,
        bool verticalFacing = true,
        float maxTilt = 75f,
        float tiltSmoothSpeed = 12f,
        bool reverseTilt = false)
    {
        arCamera = camera != null ? camera : Camera.main;
        modelRotationOffsetY = modelYawOffset;
        floorBelowActor = Mathf.Max(0f, floorOffset);
        movementRadius = Mathf.Max(0.3f, radius);
        keepMovementOnScreen = constrainToScreen;
        screenMargin = Mathf.Clamp(viewportMargin, 0.01f, 0.4f);
        addColliderIfMissing = autoCollider;
        centerModelOnSpawn = centerOnSpawn;
        faceCameraVertically = verticalFacing;
        maxVisualTiltDegrees = Mathf.Clamp(maxTilt, 0f, 85f);
        visualTiltFollowSpeed = Mathf.Max(0f, tiltSmoothSpeed);
        invertVisualTilt = reverseTilt;
        actor = GetComponent<DalsuActor>();
        behavior = GetComponent<DalsooBehaviorController>();

        if (arCamera == null || behavior == null)
        {
            Debug.LogError("[DALSU][QR] QRDalsooDragFloor에 Camera 또는 DalsooBehaviorController가 없습니다.");
            enabled = false;
            return;
        }

        if (virtualFloor == null)
        {
            // 실제 ARPlane이 아닌 위치 참조용 빈 Transform이다.
            GameObject holder = new GameObject("QR_Dalsoo_VirtualFloor");
            virtualFloor = holder.transform;
        }

        // 애니메이션 및 바닥 계산은 수평 루트를 그대로 사용하고,
        // 모델만 중심 피벗을 기준으로 살짝 기울인다.
        CreateVisualPitchPivot();
        ApplyVisualTilt(true);

        if (centerModelOnSpawn)
        {
            CenterVisibleModelOnScreen();
            transform.rotation = GetFaceCameraRotation(arCamera, transform.position, modelRotationOffsetY);
            ApplyVisualTilt(true);
            CenterVisibleModelOnScreen();
        }

        if (addColliderIfMissing)
            EnsureDragCollider();

        configured = true;
        RebuildFloorAndResume();
    }

    private void LateUpdate()
    {
        if (!configured || arCamera == null) return;
        ApplyVisualTilt(false);
    }

    // QR용 시각 피벗: 모델의 바운딩 중심을 기준으로 기울여 발에서 통째로 꺾이지 않게 한다.
    private void CreateVisualPitchPivot()
    {
        if (visualPitchPivot != null || actor == null || actor.ModelInstance == null)
            return;

        Transform model = actor.ModelInstance.transform;
        Transform originalParent = model.parent;
        if (originalParent == null) return;

        Vector3 visualCenter = model.position;
        Bounds bounds;
        if (TryGetVisibleBounds(out bounds))
            visualCenter = bounds.center;

        GameObject pivotObject = new GameObject("QR_VisualPitchPivot");
        visualPitchPivot = pivotObject.transform;
        visualPitchPivot.SetParent(originalParent, false);
        visualPitchPivot.position = visualCenter;
        visualPitchPivot.rotation = originalParent.rotation;
        model.SetParent(visualPitchPivot, true);
    }

    // 카메라보다 달수가 낮아졌을 때, 루트 회전은 건드리지 않고
    // 모델의 위/아래 기울기만 조정한다. 달수 정면 축(-right)에 맞춰 local Z를 사용.
    private void ApplyVisualTilt(bool immediately)
    {
        if (visualPitchPivot == null || arCamera == null) return;

        float tilt = 0f;
        if (faceCameraVertically)
        {
            Vector3 toCamera = arCamera.transform.position - visualPitchPivot.position;
            float horizontalDistance = new Vector2(toCamera.x, toCamera.z).magnitude;
            tilt = Mathf.Atan2(toCamera.y, Mathf.Max(0.001f, horizontalDistance))
                * Mathf.Rad2Deg;
            tilt = Mathf.Clamp(tilt, -maxVisualTiltDegrees, maxVisualTiltDegrees);
            if (invertVisualTilt) tilt = -tilt;
        }

        Quaternion desiredRotation = Quaternion.AngleAxis(-tilt, Vector3.forward);
        if (immediately || visualTiltFollowSpeed <= 0f)
            visualPitchPivot.localRotation = desiredRotation;
        else
        {
            float t = 1f - Mathf.Exp(-visualTiltFollowSpeed * Time.deltaTime);
            visualPitchPivot.localRotation = Quaternion.Slerp(
                visualPitchPivot.localRotation, desiredRotation, t);
        }
    }

    // Prefab 루트가 아니라 실제 렌더링되는 모델의 중심을 화면 (0.5, 0.5)에 둔다.
    private void CenterVisibleModelOnScreen()
    {
        Bounds bounds;
        if (arCamera == null || !TryGetVisibleBounds(out bounds)) return;

        Vector3 viewport = arCamera.WorldToViewportPoint(bounds.center);
        if (viewport.z <= arCamera.nearClipPlane) return;

        Vector3 desiredCenter = arCamera.ViewportToWorldPoint(
            new Vector3(0.5f, 0.5f, viewport.z));
        transform.position += desiredCenter - bounds.center;
        Debug.Log("[DALSU][QR] 달수 모델 중심 화면 중앙 배치 완료");
    }

    private bool TryGetVisibleBounds(out Bounds combined)
    {
        combined = new Bounds(transform.position, Vector3.zero);
        if (actor == null || actor.ModelInstance == null) return false;
        Renderer[] renderers = actor.ModelInstance.GetComponentsInChildren<Renderer>(true);
        bool hasBounds = false;
        foreach (Renderer r in renderers)
        {
            if (!r.enabled || !r.gameObject.activeInHierarchy) continue;
            if (!hasBounds)
            {
                combined = r.bounds;
                hasBounds = true;
            }
            else combined.Encapsulate(r.bounds);
        }
        return hasBounds;
    }

    private void Update()
    {
        if (!configured || arCamera == null) return;

        // 멀티터치를 먼저 검사한다. 기존 1손가락 드래그가 핀치 중에
        // 동시에 실행되어 좌표를 덮어쓰는 문제를 방지한다.
        Vector2 first, second;
        int touchCount = ReadTwoFingerPositions(out first, out second);
        if (enablePinchZoom && touchCount >= 2)
        {
            if (!pinching) BeginPinch(first, second);
            else UpdatePinch(first, second);
            return;
        }

        if (pinching)
        {
            EndPinch();
            // 한 손가락만 남아도 즉시 드래그로 전환하지 않는다.
            // 다시 새로 터치하면 1손가락 드래그가 시작된다.
            return;
        }

        // 핀치가 비활성화된 경우에도 두 손가락을 동시에 드래그하지 않는다.
        if (touchCount >= 2) return;

        Vector2 screen;
        PointerPhase phase = ReadPointer(out screen);
        if (phase == PointerPhase.Began)
            BeginDrag(screen);
        else if (phase == PointerPhase.Holding && dragging)
            MoveDrag(screen);
        else if (phase == PointerPhase.Ended && dragging)
            EndDrag();
    }

    private void BeginPinch(Vector2 first, Vector2 second)
    {
        if (IsPointerOverUI(first) || IsPointerOverUI(second)) return;
        if (!pinchAnywhereOnScreen && !dragging &&
            !IsTouchingActor(first) && !IsTouchingActor(second)) return;

        float span = Vector2.Distance(first, second);
        if (span < 12f) return;

        Vector3 projected = arCamera.WorldToScreenPoint(transform.position);
        if (projected.z <= arCamera.nearClipPlane) return;

        pinching = true;
        dragging = false;
        pinchStartSpan = span;
        pinchStartDepth = projected.z;
        // 화면 중앙 배치된 모델의 위치가 줌 도중 좌우/상하로 튀지 않게
        // 루트의 기존 화면 좌표를 유지한 채 카메라 거리만 변경한다.
        pinchRootScreenPosition = new Vector2(projected.x, projected.y);
        behavior.SetTrackingPresentationPaused(true);
    }

    private void UpdatePinch(Vector2 first, Vector2 second)
    {
        if (!pinching) return;
        float currentSpan = Vector2.Distance(first, second);
        if (currentSpan < 12f) return;

        // 벌리면 span이 커지므로 depth가 작아져 카메라와 가까워진다.
        // 모으면 depth가 커져 멀어진다. transform.localScale은 유지한다.
        float scale = currentSpan / pinchStartSpan;
        float desiredDepth = pinchStartDepth /
            Mathf.Pow(Mathf.Max(0.01f, scale), pinchSensitivity);
        desiredDepth = Mathf.Clamp(desiredDepth,
            minPinchDistance, Mathf.Max(minPinchDistance, maxPinchDistance));

        float currentDepth = arCamera.WorldToScreenPoint(transform.position).z;
        float blend = pinchFollowSpeed <= 0f ? 1f :
            1f - Mathf.Exp(-pinchFollowSpeed * Time.deltaTime);
        float nextDepth = Mathf.Lerp(currentDepth, desiredDepth, blend);

        transform.position = arCamera.ScreenToWorldPoint(
            new Vector3(pinchRootScreenPosition.x,
                pinchRootScreenPosition.y, nextDepth));
        transform.rotation = GetFaceCameraRotation(
            arCamera, transform.position, modelRotationOffsetY);
    }

    private void EndPinch()
    {
        if (!pinching) return;
        pinching = false;
        transform.rotation = GetFaceCameraRotation(
            arCamera, transform.position, modelRotationOffsetY);
        // 1손가락 이동과 동일하게 새로운 위치 아래에 바닥 재배치.
        RebuildFloorAndResume();
    }

    // Input System과 구 Input Manager(Both 포함) 모두 대응.
    // Two Finger Touch를 개별적으로 읽고, Mouse는 핀치로 취급하지 않는다.
    private static int ReadTwoFingerPositions(out Vector2 first, out Vector2 second)
    {
        first = Vector2.zero;
        second = Vector2.zero;
        int count = 0;
#if ENABLE_INPUT_SYSTEM
        Touchscreen touchscreen = Touchscreen.current;
        if (touchscreen != null)
        {
            foreach (var finger in touchscreen.touches)
            {
                if (!finger.press.isPressed) continue;
                if (count == 0) first = finger.position.ReadValue();
                else if (count == 1) second = finger.position.ReadValue();
                count++;
                if (count >= 2) return count;
            }
        }
        if (count > 0) return count;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        for (int i = 0; i < Input.touchCount; i++)
        {
            UnityEngine.Touch touch = Input.GetTouch(i);
            if (touch.phase == UnityEngine.TouchPhase.Ended ||
                touch.phase == UnityEngine.TouchPhase.Canceled) continue;
            if (count == 0) first = touch.position;
            else if (count == 1) second = touch.position;
            count++;
            if (count >= 2) return count;
        }
#endif
        return count;
    }

    private void BeginDrag(Vector2 screen)
    {
        if (IsPointerOverUI(screen)) return;

        if (!IsTouchingActor(screen)) return;

        dragging = true;
        heldDepth = Mathf.Max(0.3f,
            arCamera.WorldToScreenPoint(transform.position).z);
        Vector3 screenPoint = new Vector3(screen.x, screen.y, heldDepth);
        touchOffset = transform.position - arCamera.ScreenToWorldPoint(screenPoint);
        behavior.SetTrackingPresentationPaused(true);
    }

    private bool IsTouchingActor(Vector2 screen)
    {
        Ray ray = arCamera.ScreenPointToRay(screen);
        RaycastHit[] hits = Physics.RaycastAll(ray, maxRaycastDistance,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide);

        // 카메라에 가장 가까운 Collider가 달수의 자식일 때만 인정.
        float nearestDistance = float.PositiveInfinity;
        Transform nearest = null;
        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i].distance < nearestDistance)
            {
                nearestDistance = hits[i].distance;
                nearest = hits[i].transform;
            }
        }
        return nearest != null &&
            (nearest == transform || nearest.IsChildOf(transform));
    }

    private void MoveDrag(Vector2 screen)
    {
        Vector3 screenPoint = new Vector3(screen.x, screen.y, heldDepth);
        transform.position = arCamera.ScreenToWorldPoint(screenPoint) + touchOffset;
        // 드래그 중에는 수평 정면도 카메라를 향하게 유지한다.
        transform.rotation = GetFaceCameraRotation(
            arCamera, transform.position, modelRotationOffsetY);
    }

    private void EndDrag()
    {
        dragging = false;
        transform.rotation = GetFaceCameraRotation(
            arCamera, transform.position, modelRotationOffsetY);
        RebuildFloorAndResume();
    }

    // 드래그를 놓을 때마다 새 높이에 바닥이 생긴다고 가정하지만,
    // Actor는 놓은 세계 좌표를 유지한다. (아래로 순간이동 X)
    private void RebuildFloorAndResume()
    {
        if (virtualFloor == null || behavior == null) return;
        virtualFloor.position = transform.position - Vector3.up * floorBelowActor;
        virtualFloor.rotation = Quaternion.identity;

        if (!behavior.enabled)
            behavior.enabled = true;

        behavior.InitializeVirtualFloor(
            virtualFloor.position,
            movementRadius,
            floorBelowActor,
            arCamera,
            keepMovementOnScreen,
            screenMargin);
        behavior.SetTrackingPresentationPaused(false);
    }

    // 기존 QR 모델 정면 오프셋과 동일한 방향 사용.
    public static Quaternion GetFaceCameraRotation(
        Camera camera, Vector3 actorPosition, float yawOffset)
    {
        Vector3 toCamera = camera.transform.position - actorPosition;
        toCamera.y = 0f;

        if (toCamera.sqrMagnitude < 0.0001f)
        {
            toCamera = -camera.transform.forward;
            toCamera.y = 0f;
        }
        if (toCamera.sqrMagnitude < 0.0001f)
            toCamera = Vector3.back;

        return Quaternion.LookRotation(toCamera.normalized, Vector3.up)
            * Quaternion.Euler(0f, yawOffset, 0f);
    }

    private static bool IsPointerOverUI(Vector2 screenPosition)
    {
        if (EventSystem.current == null) return false;
        var data = new PointerEventData(EventSystem.current);
        data.position = screenPosition;
        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(data, results);
        return results.Count > 0;
    }

    // 모델에 Collider가 없을 때만, 렌더러 Bounds를 감싸는 루트 BoxCollider 생성.
    private void EnsureDragCollider()
    {
        if (GetComponentInChildren<Collider>(true) != null) return;

        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            Debug.LogWarning("[DALSU][QR] Renderer가 없어 Collider를 자동 생성하지 못했습니다.");
            return;
        }

        Vector3 localMin = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
        Vector3 localMax = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);
        bool found = false;

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer r = renderers[i];
            if (!r.gameObject.activeInHierarchy) continue;
            if (!(r is MeshRenderer) && !(r is SkinnedMeshRenderer)) continue;

            Bounds b = r.bounds;
            Vector3 min = b.min;
            Vector3 max = b.max;
            for (int j = 0; j < 8; j++)
            {
                Vector3 worldCorner = new Vector3(
                    (j & 1) == 0 ? min.x : max.x,
                    (j & 2) == 0 ? min.y : max.y,
                    (j & 4) == 0 ? min.z : max.z);
                Vector3 localCorner = transform.InverseTransformPoint(worldCorner);
                localMin = Vector3.Min(localMin, localCorner);
                localMax = Vector3.Max(localMax, localCorner);
                found = true;
            }
        }

        if (!found)
        {
            Debug.LogWarning("[DALSU][QR] 모델 렌더러를 찾을 수 없어 Collider를 만들지 않았습니다.");
            return;
        }

        BoxCollider box = gameObject.AddComponent<BoxCollider>();
        box.center = (localMin + localMax) * 0.5f;
        box.size = Vector3.Max(localMax - localMin + Vector3.one * 0.16f,
            Vector3.one * 0.15f);
        Debug.Log("[DALSU][QR] 드래그용 BoxCollider 자동 추가");
    }

    private PointerPhase ReadPointer(out Vector2 screen)
    {
        screen = Vector2.zero;

#if ENABLE_INPUT_SYSTEM
        Touchscreen ts = Touchscreen.current;
        if (ts != null)
        {
            var touch = ts.primaryTouch;
            screen = touch.position.ReadValue();
            if (touch.press.wasPressedThisFrame) return PointerPhase.Began;
            if (touch.press.wasReleasedThisFrame) return PointerPhase.Ended;
            if (touch.press.isPressed) return PointerPhase.Holding;
        }

        Mouse mouse = Mouse.current;
        if (mouse != null)
        {
            screen = mouse.position.ReadValue();
            if (mouse.leftButton.wasPressedThisFrame) return PointerPhase.Began;
            if (mouse.leftButton.wasReleasedThisFrame) return PointerPhase.Ended;
            if (mouse.leftButton.isPressed) return PointerPhase.Holding;
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.touchCount > 0)
        {
            UnityEngine.Touch touch = Input.GetTouch(0);
            screen = touch.position;
            if (touch.phase == UnityEngine.TouchPhase.Began) return PointerPhase.Began;
            if (touch.phase == UnityEngine.TouchPhase.Ended || touch.phase == UnityEngine.TouchPhase.Canceled)
                return PointerPhase.Ended;
            return PointerPhase.Holding;
        }
        if (Application.isEditor)
        {
            screen = Input.mousePosition;
            if (Input.GetMouseButtonDown(0)) return PointerPhase.Began;
            if (Input.GetMouseButtonUp(0)) return PointerPhase.Ended;
            if (Input.GetMouseButton(0)) return PointerPhase.Holding;
        }
#endif
        return PointerPhase.None;
    }

    private void OnDisable()
    {
        if (dragging || pinching)
        {
            dragging = false;
            pinching = false;
            if (configured) RebuildFloorAndResume();
        }
    }

    private void OnDestroy()
    {
        if (virtualFloor != null)
            Destroy(virtualFloor.gameObject);
    }
}
