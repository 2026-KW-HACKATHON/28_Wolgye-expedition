using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

/// <summary>
/// Android ARCore용 단계별 달수 배치.
/// 0~3초 실제 바닥/화면 밖 -> 3~5초 실제 바닥/화면 안도 허용
/// -> 5~7초 Depth 기반 수평 표면 추정 -> 7초 이후 카메라 높이로 가상 바닥 추정.
/// 시간을 기다리는 동안에도 매번 먼저 실제 바닥을 검사한다.
/// </summary>
public class ARRandomSpawner : MonoBehaviour
{
    private enum PlacementMode
    {
        DetectedPlane,
        DepthEstimated,
        VirtualFloor
    }

    [Header("AR")]
    [SerializeField] private ARPlaneManager planeManager;
    [SerializeField] private ARRaycastManager raycastManager;
    [SerializeField] private Camera arCamera;

    [Header("Dalsu Runtime / Data")]
    [SerializeField] private GameObject dalsuRuntimePrefab;
    [SerializeField] private DalsuDatabase dalsuDatabase;
    [SerializeField] private float visualYOffset = 0f;

    [Header("Spawn Range")]
    [SerializeField] private float minSpawnDistance = 1f;
    [SerializeField] private float maxSpawnDistance = 4f;
    [SerializeField] private float minimumBelowCamera = 0.5f;

    [Header("Plane Sampling")]
    [SerializeField] private float visibilityCheckHeight = 0.5f;
    [SerializeField] private float screenPadding = 0.05f;
    [SerializeField] private int samplesPerPlane = 30;
    [SerializeField] private float retryInterval = 0.25f;

    [Header("Fallback Timing: Tracking Start 기준")]
    [SerializeField] private float allowVisiblePlaneAfterSeconds = 3f;
    [SerializeField] private float useDepthAfterSeconds = 5f;
    [SerializeField] private float useVirtualFloorAfterSeconds = 7f;

    [Header("Depth Candidate Filtering")]
    [SerializeField] private float maximumBelowCamera = 2.2f;
    [Range(0f, 60f)]
    [SerializeField] private float maximumDepthFloorTilt = 30f;
    [SerializeField] private float depthHeightTolerance = 0.25f;
    [SerializeField] private int minimumConsistentDepthHits = 2;

    [Header("Virtual Floor")]
    [SerializeField] private float estimatedCameraHeight = 1.3f;
    [SerializeField] private float estimatedForwardDistance = 1.5f;
    [Tooltip("추정 바닥에서 달수가 움직일 최대 반경(m). 실제 안전 영역 인증 아님")]
    [SerializeField] private float estimatedMovementRadius = 0.75f;

    [Header("Upgrade Estimated Floor -> Real Plane")]
    [SerializeField] private float upgradeSearchInterval = 0.5f;
    [SerializeField] private float maxUpgradeHorizontalShift = 0.75f;
    [SerializeField] private float maxUpgradeVerticalShift = 1f;
    [SerializeField] private float upgradeMoveSpeed = 0.8f;
    [SerializeField] private float upgradePlaneEdgeMargin = 0.15f;

    [Header("Guide UI")]
    [SerializeField] private GameObject lookAroundText;
    [SerializeField] private AROffscreenArrow offscreenArrow;
    [SerializeField] private float confirmVisibleDuration = 0.35f;

    [Header("Test Dalsu")]
    [SerializeField] private bool useTestDalsu = true;
    [SerializeField] private string testDalsuId = "";

    private readonly List<ARRaycastHit> raycastHits = new List<ARRaycastHit>();
    private readonly List<Vector3> depthCandidates = new List<Vector3>();

    private static readonly float[] SampleXs = { 0.2f, 0.35f, 0.5f, 0.65f, 0.8f };
    private static readonly float[] SampleYs = { 0.15f, 0.3f, 0.45f };

    private GameObject spawnedObject;
    private GameObject placementRoot;
    private DalsuActor actor;
    private DalsuData currentDalsuData;
    private bool dalsuFound;
    private float visibleSince = -1f;

    private PlacementMode placementMode;
    private bool upgrading;
    private ARPlane upgradePlane;
    private Vector2 upgradePlaneLocalPoint;
    private float nextUpgradeSearchTime;

    private void Awake()
    {
        if (arCamera == null)
            arCamera = Camera.main;

        if (lookAroundText != null)
            lookAroundText.SetActive(true);
    }

    private IEnumerator Start()
    {
        if (dalsuDatabase == null || dalsuRuntimePrefab == null || planeManager == null || arCamera == null)
        {
            Debug.LogError("[DALSU] 필수 Inspector 참조가 없습니다: Database/Runtime/PlaneManager/Camera");
            yield break;
        }

        // 이전 씬에서 선택한 달수 ID
        string selectedId = DalsuSceneContext.SelectedDalsuId;

        // 테스트 모드이거나 선택한 달수가 없으면 기본 달수 사용
        if (useTestDalsu || string.IsNullOrEmpty(selectedId))
        {
            selectedId = testDalsuId;

            Debug.Log(
                $"[DALSU] 테스트 달수 사용: {selectedId}"
            );
        }

        currentDalsuData = dalsuDatabase.GetById(selectedId);

        if (currentDalsuData == null)
        {
            Debug.LogError($"[DALSU] 데이터 검색 실패: {selectedId}");
            yield break;
        }

        // 실제 추적 좌표가 확보되기 전에는 가상 바닥도 배치할 수 없다.
        while (ARSession.state != ARSessionState.SessionTracking)
            yield return null;

        if (raycastManager == null)
            Debug.LogWarning("[DALSU] ARRaycastManager 미연결: Depth 단계는 생략됩니다.");

        float trackingStartTime = Time.time;
        Debug.Log("[DALSU] Tracking 성공, 단계별 바닥 탐색 시작");

        while (spawnedObject == null)
        {
            if (ARSession.state != ARSessionState.SessionTracking)
            {
                yield return new WaitForSeconds(retryInterval);
                continue;
            }

            float elapsed = Time.time - trackingStartTime;
            Vector3 position;
            ARPlane plane;

            // 우선순위 1: 실제 바닥 + 화면 밖
            if (TryFindPlanePosition(true, out position, out plane) &&
                SpawnDalsoo(position, plane, PlacementMode.DetectedPlane))
                yield break;

            // 우선순위 2: 실제 바닥이면 화면 안도 허용
            if (elapsed >= allowVisiblePlaneAfterSeconds &&
                TryFindPlanePosition(false, out position, out plane) &&
                SpawnDalsoo(position, plane, PlacementMode.DetectedPlane))
                yield break;

            // 우선순위 3: Depth hit pose의 법선/높이/일관성으로 수평 표면 추정
            if (elapsed >= useDepthAfterSeconds &&
                TryFindDepthFloorPosition(out position) &&
                SpawnDalsoo(position, null, PlacementMode.DepthEstimated))
                yield break;

            // 우선순위 4: 최종 Fallback (실측 바닥이 아닌 가정치)
            if (elapsed >= useVirtualFloorAfterSeconds)
            {
                position = EstimateVirtualFloorPosition();
                if (SpawnDalsoo(position, null, PlacementMode.VirtualFloor))
                    yield break;

                Debug.LogError("[DALSU] 가상 바닥 생성까지 실패하여 재시도를 중단합니다.");
                yield break;
            }

            yield return new WaitForSeconds(retryInterval);
        }
    }

    private void Update()
    {
        if (spawnedObject == null)
            return;

        // AR 추적이 일시적으로 끊기면 랜덤 이동을 멈춘다.
        if (actor != null)
            actor.SetTrackingPresentationPaused(ARSession.state != ARSessionState.SessionTracking);

        if (!dalsuFound)
        {
            if (IsDalsuInCameraView())
            {
                if (visibleSince < 0f)
                    visibleSince = Time.time;

                if (Time.time - visibleSince >= confirmVisibleDuration)
                {
                    dalsuFound = true;
                    if (lookAroundText != null)
                        lookAroundText.SetActive(false);
                    if (offscreenArrow != null)
                        offscreenArrow.MarkFound();
                    Debug.Log("[DALSU] 달수 화면 발견: 안내 종료");
                }
            }
            else
            {
                visibleSince = -1f;
            }
        }

        // 추정 바닥으로 배치한 달수는 실제 Plane 발견 시 부드럽게 보정한다.
        if (placementMode == PlacementMode.DetectedPlane ||
            placementRoot == null || planeManager == null ||
            ARSession.state != ARSessionState.SessionTracking)
            return;

        if (upgrading)
        {
            if (actor == null || !IsUsableFloor(upgradePlane) ||
                !IsSafePlaneLocalPoint(upgradePlane, upgradePlaneLocalPoint, upgradePlaneEdgeMargin))
            {
                CancelUpgrade();
                return;
            }

            Vector3 floorTarget = upgradePlane.transform.TransformPoint(
                new Vector3(upgradePlaneLocalPoint.x, 0f, upgradePlaneLocalPoint.y));
            Vector3 actorTarget = floorTarget + Vector3.up * visualYOffset;

            // 핵심: 가상 바닥에서 Actor가 걸어간 위치에서 바로 보간한다.
            // placementRoot만 옮기면 현재 배우의 localPosition 오프셋 때문에
            // 실제 Plane의 경계를 벗어날 수 있다.
            actor.transform.position = Vector3.MoveTowards(
                actor.transform.position, actorTarget,
                Mathf.Max(0.05f, upgradeMoveSpeed) * Time.deltaTime);

            if (Vector3.Distance(actor.transform.position, actorTarget) <= 0.025f)
            {
                // 최종 위치를 바닥과 일치시키고 Runtime localOffset 재정렬.
                placementRoot.transform.position = floorTarget;
                actor.transform.localPosition = Vector3.up * visualYOffset;

                if (actor.UpgradeToPlaneAR(upgradePlane))
                {
                    placementMode = PlacementMode.DetectedPlane;
                    Debug.Log("[DALSU] 추정 바닥 → 실제 Plane 보정 완료, 행동 재개");
                }
                CancelUpgrade();
            }
        }
        else if (Time.time >= nextUpgradeSearchTime)
        {
            nextUpgradeSearchTime = Time.time + Mathf.Max(0.1f, upgradeSearchInterval);
            ARPlane targetPlane;
            Vector2 localPoint;
            if (TryFindUpgradePlane(out targetPlane, out localPoint))
            {
                upgradePlane = targetPlane;
                upgradePlaneLocalPoint = localPoint;
                upgrading = true;
                actor.SetPlacementTransitionPaused(true);
            }
        }
    }

    private bool TryFindPlanePosition(bool requireOffscreen,
        out Vector3 result, out ARPlane resultPlane)
    {
        result = Vector3.zero;
        resultPlane = null;

        foreach (ARPlane plane in planeManager.trackables)
        {
            if (!IsUsableFloor(plane))
                continue;

            NativeArray<Vector2> boundary = plane.boundary;
            if (!boundary.IsCreated || boundary.Length < 3)
                continue;

            // 먼저 중심을 시도한다. 랜덤 sampling만으로 좁은 Plane을 놓치지 않도록.
            Vector2 center = GetBoundaryVertexAverage(boundary);
            Vector3 candidate;
            if (IsPointInsidePolygon(center, boundary) &&
                TryAcceptPlanePoint(plane, center, requireOffscreen, out candidate))
            {
                result = candidate;
                resultPlane = plane;
                return true;
            }

            Vector2 min = boundary[0];
            Vector2 max = boundary[0];
            for (int i = 1; i < boundary.Length; ++i)
            {
                min = Vector2.Min(min, boundary[i]);
                max = Vector2.Max(max, boundary[i]);
            }

            for (int i = 0; i < samplesPerPlane; ++i)
            {
                Vector2 localPoint = new Vector2(
                    Random.Range(min.x, max.x), Random.Range(min.y, max.y));

                if (!IsPointInsidePolygon(localPoint, boundary))
                    continue;

                if (TryAcceptPlanePoint(plane, localPoint, requireOffscreen, out candidate))
                {
                    result = candidate;
                    resultPlane = plane;
                    return true;
                }
            }
        }
        return false;
    }

    private bool TryAcceptPlanePoint(ARPlane plane, Vector2 localPoint,
        bool requireOffscreen, out Vector3 position)
    {
        position = plane.transform.TransformPoint(new Vector3(localPoint.x, 0f, localPoint.y));
        if (!IsAcceptableHeightAndDistance(position))
            return false;
        if (requireOffscreen && IsCurrentlyVisible(position))
            return false;
        return true;
    }

    private bool TryFindDepthFloorPosition(out Vector3 result)
    {
        result = Vector3.zero;
        if (raycastManager == null || !raycastManager.isActiveAndEnabled)
            return false;

        depthCandidates.Clear();
        float minNormalY = Mathf.Cos(maximumDepthFloorTilt * Mathf.Deg2Rad);

        foreach (float y in SampleYs)
        {
            foreach (float x in SampleXs)
            {
                Vector2 screenPoint = new Vector2(Screen.width * x, Screen.height * y);
                raycastHits.Clear();

                if (!raycastManager.Raycast(screenPoint, raycastHits, TrackableType.Depth))
                    continue;

                foreach (ARRaycastHit hit in raycastHits)
                {
                    Vector3 hitPosition = hit.pose.position;
                    float down = arCamera.transform.position.y - hitPosition.y;
                    if (down < minimumBelowCamera || down > maximumBelowCamera)
                        continue;

                    if (!IsAcceptableHeightAndDistance(hitPosition))
                        continue;

                    // Depth pose의 +Y 법선이 위쪽에 가깝다면 수평 표면 후보.
                    // 단, '바닥'이라는 의미론적 분류까지 보장하지는 않는다.
                    if (Vector3.Dot(hit.pose.up, Vector3.up) < minNormalY)
                        continue;

                    depthCandidates.Add(hitPosition);
                    break;
                }
            }
        }

        if (depthCandidates.Count < minimumConsistentDepthHits)
            return false;

        // 서로 높이가 비슷한 후보가 여러 개 있을 때만 채택한다.
        int bestAgreement = 0;
        float bestDistanceError = float.PositiveInfinity;
        int bestIndex = -1;
        for (int i = 0; i < depthCandidates.Count; i++)
        {
            int agreement = 0;
            for (int j = 0; j < depthCandidates.Count; j++)
            {
                if (Mathf.Abs(depthCandidates[i].y - depthCandidates[j].y)
                    <= depthHeightTolerance)
                    agreement++;
            }

            Vector3 delta = depthCandidates[i] - arCamera.transform.position;
            delta.y = 0f;
            float distanceError = Mathf.Abs(delta.magnitude - estimatedForwardDistance);
            if (agreement > bestAgreement ||
                (agreement == bestAgreement && distanceError < bestDistanceError))
            {
                bestAgreement = agreement;
                bestDistanceError = distanceError;
                bestIndex = i;
            }
        }

        if (bestAgreement < minimumConsistentDepthHits || bestIndex < 0)
            return false;

        result = depthCandidates[bestIndex];
        return true;
    }

    private Vector3 EstimateVirtualFloorPosition()
    {
        Vector3 forward = Vector3.ProjectOnPlane(arCamera.transform.forward, Vector3.up);
        if (forward.sqrMagnitude < 0.001f)
            forward = Vector3.ProjectOnPlane(arCamera.transform.up, Vector3.up);
        if (forward.sqrMagnitude < 0.001f)
            forward = Vector3.forward;

        Vector3 cameraPosition = arCamera.transform.position;
        Vector3 floorPosition = cameraPosition +
            forward.normalized * Mathf.Clamp(estimatedForwardDistance,
                minSpawnDistance, maxSpawnDistance);
        floorPosition.y = cameraPosition.y - estimatedCameraHeight;
        return floorPosition;
    }

    private bool SpawnDalsoo(Vector3 position, ARPlane plane, PlacementMode mode)
    {
        if (spawnedObject != null)
            return true;

        // 일부 AR Foundation 버전에서는 ARAnchor 추가 실패 시 GameObject가
        // 비활성화될 수 있으므로, 여기서는 일단 일반 Transform으로 배치한다.
        // 안정적인 anchor가 필요하면 프로젝트 AR Foundation 버전에 맞춰 별도 연동.
        placementRoot = new GameObject("DalsooPlacementRoot");
        placementRoot.transform.SetPositionAndRotation(position, Quaternion.identity);
        spawnedObject = Instantiate(dalsuRuntimePrefab, placementRoot.transform);
        spawnedObject.transform.localPosition = new Vector3(0f, visualYOffset, 0f);
        spawnedObject.transform.localRotation = Quaternion.identity;

        actor = spawnedObject.GetComponent<DalsuActor>();
        if (actor == null)
        {
            Debug.LogError("[DALSU] Runtime Prefab에 DalsuActor가 없습니다.");
            ClearFailedSpawn();
            return false;
        }

        DalsuActor.SpawnMode actorMode = plane != null
            ? DalsuActor.SpawnMode.PlaneAR
            : DalsuActor.SpawnMode.EstimatedFloor;

        actor.Initialize(currentDalsuData, actorMode, plane, estimatedMovementRadius);
        if (!actor.IsInitialized)
        {
            Debug.LogError("[DALSU] DalsuActor 초기화 실패");
            ClearFailedSpawn();
            return false;
        }

        placementMode = mode;
        nextUpgradeSearchTime = Time.time + Mathf.Max(0.1f, upgradeSearchInterval);
        if (offscreenArrow != null)
            offscreenArrow.SetTarget(spawnedObject.transform);

        Debug.Log($"[DALSU] 생성 성공: {currentDalsuData.dalsuName}, placement={mode}");
        return true;
    }

    private void ClearFailedSpawn()
    {
        if (spawnedObject != null)
            Destroy(spawnedObject);
        if (placementRoot != null)
            Destroy(placementRoot);
        spawnedObject = null;
        placementRoot = null;
        actor = null;
    }

    private void CancelUpgrade()
    {
        upgrading = false;
        upgradePlane = null;
        if (actor != null)
            actor.SetPlacementTransitionPaused(false);
    }

    private bool IsSafePlaneLocalPoint(ARPlane plane, Vector2 point, float margin)
    {
        if (!IsUsableFloor(plane)) return false;
        NativeArray<Vector2> boundary = plane.boundary;
        if (!boundary.IsCreated || boundary.Length < 3 ||
            !IsPointInsidePolygon(point, boundary)) return false;

        // 원래 BehaviorController의 가장자리 여유와 같은 방식.
        for (int i = 0; i < 8; ++i)
        {
            float angle = 2f * Mathf.PI * i / 8f;
            Vector2 neighbor = point + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) *
                Mathf.Max(0f, margin);
            if (!IsPointInsidePolygon(neighbor, boundary)) return false;
        }
        return true;
    }

    private bool TryFindUpgradePlane(out ARPlane bestPlane, out Vector2 bestLocal)
    {
        bestPlane = null;
        bestLocal = Vector2.zero;
        float closest = float.PositiveInfinity;

        foreach (ARPlane plane in planeManager.trackables)
        {
            if (!IsUsableFloor(plane))
                continue;

            NativeArray<Vector2> boundary = plane.boundary;
            if (!boundary.IsCreated || boundary.Length < 3)
                continue;

            // Root가 아니라 실제 걷고 있는 Actor의 현재 위치를 기준으로 판단.
            Vector3 actorFloorPoint = actor.transform.position - Vector3.up * visualYOffset;
            Vector3 local3 = plane.transform.InverseTransformPoint(actorFloorPoint);
            Vector2 local = new Vector2(local3.x, local3.z);

            if (!IsSafePlaneLocalPoint(plane, local, upgradePlaneEdgeMargin))
            {
                // 가까운 Plane 중심이 충분히 넓은 경우만 대체한다.
                local = GetBoundaryVertexAverage(boundary);
                if (!IsSafePlaneLocalPoint(plane, local, upgradePlaneEdgeMargin))
                    continue;
            }

            Vector3 world = plane.transform.TransformPoint(new Vector3(local.x, 0f, local.y));
            Vector3 offset = world - actorFloorPoint;
            float heightDelta = Mathf.Abs(offset.y);
            offset.y = 0f;
            float horizontalDelta = offset.magnitude;

            if (heightDelta > maxUpgradeVerticalShift ||
                horizontalDelta > maxUpgradeHorizontalShift)
                continue;

            float score = heightDelta + horizontalDelta;
            if (score < closest)
            {
                closest = score;
                bestPlane = plane;
                bestLocal = local;
            }
        }
        return bestPlane != null;
    }

    private bool IsDalsuInCameraView()
    {
        if (spawnedObject == null || arCamera == null)
            return false;

        Renderer[] renderers = spawnedObject.GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            if (!renderer.enabled || !renderer.gameObject.activeInHierarchy)
                continue;

            // 렌더러의 '중심'이 화면 안으로 들어온 상태를 발견 조건으로 본다.
            // 실제 차폐(occlusion)까지 검사하는 것은 아니다.
            Vector3 viewport = arCamera.WorldToViewportPoint(renderer.bounds.center);
            if (viewport.z > 0f &&
                viewport.x > 0.08f && viewport.x < 0.92f &&
                viewport.y > 0.08f && viewport.y < 0.92f)
                return true;
        }
        return false;
    }

    private bool IsCurrentlyVisible(Vector3 floorPosition)
    {
        Vector3 feet = arCamera.WorldToViewportPoint(floorPosition);
        Vector3 middle = arCamera.WorldToViewportPoint(
            floorPosition + Vector3.up * visibilityCheckHeight);
        return IsViewportVisible(feet) || IsViewportVisible(middle);
    }

    private bool IsViewportVisible(Vector3 viewport)
    {
        return viewport.z > 0f &&
            viewport.x >= -screenPadding && viewport.x <= 1f + screenPadding &&
            viewport.y >= -screenPadding && viewport.y <= 1f + screenPadding;
    }

    private bool IsAcceptableHeightAndDistance(Vector3 worldPoint)
    {
        Vector3 delta = worldPoint - arCamera.transform.position;
        float down = -delta.y;
        delta.y = 0f;
        return down >= minimumBelowCamera &&
            delta.magnitude >= minSpawnDistance &&
            delta.magnitude <= maxSpawnDistance;
    }

    private static bool IsUsableFloor(ARPlane plane)
    {
        return plane != null && plane.subsumedBy == null &&
            plane.trackingState == TrackingState.Tracking &&
            plane.alignment == PlaneAlignment.HorizontalUp;
    }

    private static Vector2 GetBoundaryVertexAverage(NativeArray<Vector2> polygon)
    {
        Vector2 average = Vector2.zero;
        if (!polygon.IsCreated || polygon.Length == 0) return average;
        for (int i = 0; i < polygon.Length; ++i) average += polygon[i];
        return average / polygon.Length;
    }

    private static bool IsPointInsidePolygon(Vector2 point, NativeArray<Vector2> polygon)
    {
        bool inside = false;
        int j = polygon.Length - 1;

        for (int i = 0; i < polygon.Length; ++i)
        {
            Vector2 a = polygon[i];
            Vector2 b = polygon[j];
            if ((a.y > point.y) != (b.y > point.y) &&
                point.x < (b.x - a.x) * (point.y - a.y) / (b.y - a.y) + a.x)
                inside = !inside;
            j = i;
        }
        return inside;
    }
}
