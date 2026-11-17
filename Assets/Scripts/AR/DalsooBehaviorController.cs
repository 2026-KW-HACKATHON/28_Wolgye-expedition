using System.Collections;
using Unity.Collections;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class DalsooBehaviorController : MonoBehaviour
{
    public enum DalsooState
    {
        Idle,
        LookAtCamera,
        Sit,
        Turn,
        Wander,
        ApproachCamera
    }

    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private DalsooLookController lookController;
    [SerializeField] private Camera arCamera;


    // =========================================================
    // Idle
    // =========================================================

    [Header("Idle")]
    [SerializeField] private float minIdleTime = 2f;
    [SerializeField] private float maxIdleTime = 5f;

    [Range(0f, 1f)]
    [SerializeField] private float lookAtCameraChance = 0.35f;

    [SerializeField] private float minLookTime = 1f;
    [SerializeField] private float maxLookTime = 3f;


    // =========================================================
    // 행동 확률
    // =========================================================

    [Header("Action Weights")]
    [SerializeField] private float wanderWeight = 40f;
    [SerializeField] private float turnWeight = 25f;
    [SerializeField] private float sitWeight = 20f;
    [SerializeField] private float approachCameraWeight = 15f;


    // =========================================================
    // Turn
    // =========================================================

    [Header("Turn")]
    [SerializeField] private float minTurnAngle = 40f;
    [SerializeField] private float maxTurnAngle = 140f;

    [SerializeField] private float turnSpeed = 100f;


    // =========================================================
    // Wander
    // =========================================================

    [Header("Wander")]
    [SerializeField] private float walkSpeed = 0.35f;

    [SerializeField] private float minWanderDistance = 0.35f;
    [SerializeField] private float maxWanderDistance = 1.5f;

    [SerializeField] private int destinationAttempts = 40;


    // =========================================================
    // Plane Safety
    // =========================================================

    [Header("Plane Safety")]

    [Tooltip("Plane 가장자리로부터 이 거리만큼 안쪽에만 이동")]
    [SerializeField] private float planeEdgeMargin = 0.15f;

    [Tooltip("다음 이동 위치가 Plane 밖이면 즉시 멈춤")]
    [SerializeField] private bool stopAtPlaneEdge = true;


    // =========================================================
    // 카메라 쪽으로 다가오기
    // =========================================================

    [Header("Approach Camera")]

    [Tooltip("한 번 다가올 때 최소 이동 거리")]
    [SerializeField] private float minApproachDistance = 0.4f;

    [Tooltip("한 번 다가올 때 최대 이동 거리")]
    [SerializeField] private float maxApproachDistance = 1.0f;

    [Tooltip("카메라와 이 거리보다 가까워지지 않음")]
    [SerializeField] private float cameraStopDistance = 1.2f;

    [Tooltip("다가온 뒤 카메라를 바라보는 시간")]
    [SerializeField] private float minApproachLookTime = 1f;
    [SerializeField] private float maxApproachLookTime = 2f;


    // =========================================================
    // Sit
    // =========================================================

    [Header("Sit")]
    [SerializeField] private float minSitTime = 2f;
    [SerializeField] private float maxSitTime = 4f;

    [SerializeField] private float standUpWaitTime = 0.8f;


    // =========================================================

    public DalsooState CurrentState { get; private set; }

    // 두 종류의 이동 공간을 지원한다.
    // - 실제 ARPlane: 기존 경계 검사 사용
    // - EstimatedFloor: 추정한 위치 중심의 제한된 원형 영역 사용
    // 가상 영역은 실제 지형을 인증하지 않는다. 계단/장애물 주의.
    private enum FloorSource { None, TrackedPlane, EstimatedFloor }
    private FloorSource floorSource = FloorSource.None;
    private ARPlane movementPlane;
    private float planeHeightOffset;
    private Vector3 virtualFloorCenter;
    private float virtualFloorRadius;
    private bool initialized;

    private Coroutine behaviorRoutine;
    private bool trackingPresentationPaused;
    private bool placementTransitionPaused;

    public bool UsingEstimatedFloor => floorSource == FloorSource.EstimatedFloor;

    public void SetTrackingPresentationPaused(bool paused)
    {
        if (trackingPresentationPaused == paused) return;
        trackingPresentationPaused = paused;
        RefreshBehaviorState();
    }

    // 실제 Plane에 스냅 이동 중에는 동작을 멈추고 이중 이동을 방지한다.
    public void SetPlacementTransitionPaused(bool paused)
    {
        if (placementTransitionPaused == paused) return;
        placementTransitionPaused = paused;
        RefreshBehaviorState();
    }

    private void Awake()
    {
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (lookController == null) lookController = GetComponent<DalsooLookController>();
        if (arCamera == null) arCamera = Camera.main;
    }

    private void OnEnable()
    {
        RefreshBehaviorState();
    }

    private void OnDisable()
    {
        StopBehavior();
    }

    private void OnDestroy()
    {
        StopBehavior();
    }

    private void RefreshBehaviorState()
    {
        if (!initialized || !isActiveAndEnabled ||
            trackingPresentationPaused || placementTransitionPaused)
        {
            StopBehavior();
            return;
        }

        if (behaviorRoutine == null)
            behaviorRoutine = StartCoroutine(BehaviorLoop());
    }

    private void StopBehavior()
    {
        if (behaviorRoutine != null)
        {
            StopCoroutine(behaviorRoutine);
            behaviorRoutine = null;
        }
        SetWalking(false);
        SetLookAtCamera(false);
        if (animator != null) animator.SetBool("Sitting", false);
        CurrentState = DalsooState.Idle;
    }

    // 기존 호출부 호환: 실제 ARPlane 영역 내에서 걷는다.
    public void Initialize(ARPlane plane)
    {
        if (plane == null)
        {
            Debug.LogWarning("[DALSU] Initialize(null) 대신 InitializeVirtualFloor()를 사용하세요.");
            return;
        }

        StopBehavior();
        floorSource = FloorSource.TrackedPlane;
        movementPlane = plane;
        planeHeightOffset = Vector3.Dot(
            transform.position - plane.transform.position,
            plane.transform.up);
        initialized = true;
        if (arCamera == null) arCamera = Camera.main;
        RefreshBehaviorState();
        Debug.Log("[DALSU] 실제 ARPlane 행동 시작");
    }

    // 추정 바닥용 이동 반경은 실제로 스캔된 안전 공간이 아니다.
    public void InitializeVirtualFloor(Vector3 center, float radius)
    {
        StopBehavior();
        movementPlane = null;
        floorSource = FloorSource.EstimatedFloor;
        virtualFloorCenter = center;
        virtualFloorRadius = Mathf.Max(0.25f, radius);
        initialized = true;
        if (arCamera == null) arCamera = Camera.main;
        RefreshBehaviorState();
        Debug.Log($"[DALSU] 가상 바닥 행동 시작 / radius={virtualFloorRadius:F2}m");
    }


    // =========================================================
    // 메인 행동 Loop
    // =========================================================

    private IEnumerator BehaviorLoop()
    {
        yield return new WaitForSeconds(
            Random.Range(0.5f, 1.2f)
        );

        while (true)
        {
            // 모든 행동 사이에는 Idle
            yield return IdleRoutine();


            float totalWeight = Mathf.Max(0f, wanderWeight) +
                Mathf.Max(0f, turnWeight) +
                Mathf.Max(0f, sitWeight) +
                Mathf.Max(0f, approachCameraWeight);

            if (totalWeight <= 0f)
                continue;

            float roll = Random.Range(0f, totalWeight);


            if (roll < wanderWeight)
            {
                yield return WanderRoutine();
            }

            else if (
                roll <
                wanderWeight +
                turnWeight
            )
            {
                yield return TurnRoutine();
            }

            else if (
                roll <
                wanderWeight +
                turnWeight +
                sitWeight
            )
            {
                yield return SitRoutine();
            }

            else
            {
                yield return ApproachCameraRoutine();
            }
        }
    }


    // =========================================================
    // Idle
    // =========================================================

    private IEnumerator IdleRoutine()
    {
        CurrentState =
            DalsooState.Idle;


        SetWalking(false);
        SetLookAtCamera(false);


        float idleTime =
            Random.Range(
                minIdleTime,
                maxIdleTime
            );


        // 일정 확률로 카메라 쳐다보기
        if (Random.value < lookAtCameraChance)
        {
            float beforeLook =
                Random.Range(
                    0.3f,
                    Mathf.Min(
                        1.2f,
                        idleTime
                    )
                );


            yield return
                new WaitForSeconds(
                    beforeLook
                );


            CurrentState =
                DalsooState.LookAtCamera;


            SetLookAtCamera(true);


            float lookTime =
                Random.Range(
                    minLookTime,
                    maxLookTime
                );


            yield return
                new WaitForSeconds(
                    lookTime
                );


            SetLookAtCamera(false);
            SetWalking(false);


            CurrentState =
                DalsooState.Idle;


            float remaining =
                idleTime -
                beforeLook -
                lookTime;


            if (remaining > 0f)
            {
                yield return
                    new WaitForSeconds(
                        remaining
                    );
            }
        }

        else
        {
            yield return
                new WaitForSeconds(
                    idleTime
                );
        }
    }


    // =========================================================
    // Sit
    // =========================================================

    private IEnumerator SitRoutine()
    {
        CurrentState =
            DalsooState.Sit;


        SetLookAtCamera(false);
        SetWalking(false);


        if (animator == null)
            yield break;


        animator.SetBool(
            "Sitting",
            true
        );


        animator.ResetTrigger("Sit");
        animator.SetTrigger("Sit");


        yield return
            new WaitForSeconds(
                Random.Range(
                    minSitTime,
                    maxSitTime
                )
            );


        // 다시 일어나기
        animator.SetBool(
            "Sitting",
            false
        );


        yield return
            new WaitForSeconds(
                standUpWaitTime
            );
    }


    // =========================================================
    // Turn
    // =========================================================

    private IEnumerator TurnRoutine()
    {
        CurrentState =
            DalsooState.Turn;


        SetLookAtCamera(false);
        SetWalking(true);


        float randomAngle =
            Random.Range(
                minTurnAngle,
                maxTurnAngle
            );


        if (Random.value < 0.5f)
            randomAngle *= -1f;


        Quaternion targetRotation =
            Quaternion.AngleAxis(
                randomAngle,
                Vector3.up
            )
            *
            transform.rotation;


        while (
            Quaternion.Angle(
                transform.rotation,
                targetRotation
            ) > 1f
        )
        {
            transform.rotation =
                Quaternion.RotateTowards(
                    transform.rotation,
                    targetRotation,
                    turnSpeed *
                    Time.deltaTime
                );


            yield return null;
        }


        transform.rotation =
            targetRotation;


        SetWalking(false);
    }


    // =========================================================
    // Wander
    // =========================================================

    private IEnumerator WanderRoutine()
    {
        CurrentState =
            DalsooState.Wander;


        SetLookAtCamera(false);


        if (!TryGetRandomDestination(
                out Vector3 destination))
        {
            yield break;
        }


        yield return
            WalkToDestination(
                destination
            );
    }


    // =========================================================
    // 카메라 쪽으로 다가오기
    // =========================================================

    private IEnumerator ApproachCameraRoutine()
    {
        CurrentState =
            DalsooState.ApproachCamera;


        SetLookAtCamera(false);


        if (!TryGetCameraApproachDestination(
                out Vector3 destination))
        {
            yield break;
        }


        yield return
            WalkToDestination(
                destination
            );


        // 다가온 뒤 잠깐 바라보기
        SetLookAtCamera(true);


        yield return
            new WaitForSeconds(
                Random.Range(
                    minApproachLookTime,
                    maxApproachLookTime
                )
            );


        SetLookAtCamera(false);
        SetWalking(false);
    }


    // =========================================================
    // 공용 걷기
    // =========================================================

    private IEnumerator WalkToDestination(
        Vector3 destination)
    {
        SetWalking(true);


        while (true)
        {
            Vector3 toDestination =
                destination -
                transform.position;


            Vector3 flatDirection =
                new Vector3(
                    toDestination.x,
                    0f,
                    toDestination.z
                );


            float distance =
                flatDirection.magnitude;


            if (distance <= 0.04f)
                break;


            // ---------------------------------
            // 목적지 쪽으로 회전
            // ---------------------------------

            if (
                flatDirection.sqrMagnitude >
                0.0001f
            )
            {
                Vector3 desiredDirection =
                    flatDirection.normalized;


                // 네 달수 실제 정면
                Vector3 dalsooForward =
                    -transform.right;


                dalsooForward.y = 0f;


                if (
                    dalsooForward.sqrMagnitude >
                    0.0001f
                )
                {
                    dalsooForward.Normalize();


                    float angle =
                        Vector3.SignedAngle(
                            dalsooForward,
                            desiredDirection,
                            Vector3.up
                        );


                    float turnAmount =
                        Mathf.Clamp(
                            angle,

                            -turnSpeed *
                            Time.deltaTime,

                            turnSpeed *
                            Time.deltaTime
                        );


                    transform.Rotate(
                        0f,
                        turnAmount,
                        0f,
                        Space.World
                    );
                }
            }


            // ---------------------------------
            // 다음 프레임 위치 계산
            // ---------------------------------

            Vector3 nextPosition =
                Vector3.MoveTowards(
                    transform.position,
                    destination,
                    walkSpeed *
                    Time.deltaTime
                );


            // ★ 핵심
            // 걸으면서도 Plane 밖인지 계속 검사
            if (
                (UsingEstimatedFloor || stopAtPlaneEdge) &&
                !IsSafeMovementPoint(nextPosition, planeEdgeMargin)
            )
            {
                Debug.Log(
                    "달수가 Plane 가장자리에 도착해서 멈춤"
                );

                break;
            }


            transform.position =
                nextPosition;


            yield return null;
        }


        SetWalking(false);
    }


    // =========================================================
    // 랜덤 Wander 목적지
    // =========================================================

    private bool TryGetRandomDestination(
        out Vector3 result)
    {
        result = Vector3.zero;

        if (UsingEstimatedFloor)
            return TryGetVirtualWanderDestination(out result);

        ARPlane plane =
            GetActivePlane();


        if (plane == null)
            return false;


        if (
            plane.trackingState !=
            TrackingState.Tracking
        )
        {
            return false;
        }


        NativeArray<Vector2> boundary =
            plane.boundary;


        if (
            !boundary.IsCreated ||
            boundary.Length < 3
        )
        {
            return false;
        }


        Vector2 min = boundary[0];
        Vector2 max = boundary[0];


        for (
            int i = 1;
            i < boundary.Length;
            i++
        )
        {
            min =
                Vector2.Min(
                    min,
                    boundary[i]
                );


            max =
                Vector2.Max(
                    max,
                    boundary[i]
                );
        }


        for (
            int attempt = 0;
            attempt <
            destinationAttempts;
            attempt++
        )
        {
            Vector2 localPoint =
                new Vector2(
                    Random.Range(
                        min.x,
                        max.x
                    ),

                    Random.Range(
                        min.y,
                        max.y
                    )
                );


            if (
                !IsPointSafelyInsidePolygon(
                    localPoint,
                    boundary,
                    planeEdgeMargin
                )
            )
            {
                continue;
            }


            Vector3 worldPoint =
                plane.transform.TransformPoint(
                    new Vector3(
                        localPoint.x,
                        0f,
                        localPoint.y
                    )
                );


            worldPoint +=
                plane.transform.up *
                planeHeightOffset;


            Vector3 difference =
                worldPoint -
                transform.position;


            difference =
                Vector3.ProjectOnPlane(
                    difference,
                    plane.transform.up
                );


            float distance =
                difference.magnitude;


            if (
                distance <
                minWanderDistance
            )
            {
                continue;
            }


            if (
                distance >
                maxWanderDistance
            )
            {
                continue;
            }


            result =
                worldPoint;


            return true;
        }


        return false;
    }


    // =========================================================
    // 카메라 방향 목적지
    // =========================================================

    private bool TryGetCameraApproachDestination(
        out Vector3 result)
    {
        result = Vector3.zero;


        if (arCamera == null)
            return false;

        if (UsingEstimatedFloor)
            return TryGetVirtualApproachDestination(out result);

        ARPlane plane =
            GetActivePlane();


        if (plane == null)
            return false;


        if (
            plane.trackingState !=
            TrackingState.Tracking
        )
        {
            return false;
        }


        Vector3 toCamera =
            arCamera.transform.position -
            transform.position;


        // Plane 위 방향만 사용
        toCamera =
            Vector3.ProjectOnPlane(
                toCamera,
                plane.transform.up
            );


        float distanceToCamera =
            toCamera.magnitude;


        // 이미 충분히 가까우면
        // 더 안 다가감
        if (
            distanceToCamera <=
            cameraStopDistance
        )
        {
            return false;
        }


        Vector3 direction =
            toCamera.normalized;


        float availableDistance =
            distanceToCamera -
            cameraStopDistance;


        float desiredMove =
            Random.Range(
                minApproachDistance,
                maxApproachDistance
            );


        desiredMove =
            Mathf.Min(
                desiredMove,
                availableDistance
            );


        // 카메라가 Plane 바깥에 있을 수도 있기 때문에
        // 이동 거리를 조금씩 줄이면서
        // 안전한 지점을 찾는다.
        for (
            int attempt = 0;
            attempt < 12;
            attempt++
        )
        {
            Vector3 candidate =
                transform.position +
                direction *
                desiredMove;


            candidate =
                PutPointOnPlaneHeight(
                    candidate,
                    plane
                );


            if (
                IsSafeWorldPointOnPlane(
                    candidate,
                    planeEdgeMargin
                )
            )
            {
                result =
                    candidate;

                return true;
            }


            desiredMove *= 0.75f;


            if (
                desiredMove <
                minApproachDistance
            )
            {
                break;
            }
        }


        return false;
    }


    // =========================================================
    // 현재 Plane 높이 맞추기
    // =========================================================

    private Vector3 PutPointOnPlaneHeight(
        Vector3 worldPoint,
        ARPlane plane)
    {
        Vector3 local =
            plane.transform.InverseTransformPoint(
                worldPoint
            );


        local.y = 0f;


        Vector3 result =
            plane.transform.TransformPoint(
                local
            );


        result +=
            plane.transform.up *
            planeHeightOffset;


        return result;
    }


    // =========================================================
    // 추정 바닥: 제한된 가상 원형 영역에서 Wander / Approach
    // =========================================================

    private bool TryGetVirtualWanderDestination(out Vector3 result)
    {
        result = Vector3.zero;
        float safeRadius = Mathf.Max(0.1f, virtualFloorRadius - planeEdgeMargin);

        for (int i = 0; i < destinationAttempts; i++)
        {
            Vector2 disk = Random.insideUnitCircle * safeRadius;
            Vector3 candidate = new Vector3(
                virtualFloorCenter.x + disk.x,
                virtualFloorCenter.y,
                virtualFloorCenter.z + disk.y);

            Vector3 diff = candidate - transform.position;
            diff.y = 0f;
            if (diff.magnitude < minWanderDistance ||
                diff.magnitude > maxWanderDistance)
                continue;

            if (IsSafeMovementPoint(candidate, planeEdgeMargin))
            {
                result = candidate;
                return true;
            }
        }
        return false;
    }

    private bool TryGetVirtualApproachDestination(out Vector3 result)
    {
        result = Vector3.zero;
        if (arCamera == null) return false;

        Vector3 toCamera = arCamera.transform.position - transform.position;
        toCamera.y = 0f;
        float distToCamera = toCamera.magnitude;
        if (distToCamera <= cameraStopDistance || distToCamera < 0.001f)
            return false;

        float allowed = distToCamera - cameraStopDistance;
        float desiredMove = Mathf.Min(
            Random.Range(minApproachDistance, maxApproachDistance), allowed);
        Vector3 direction = toCamera.normalized;

        for (int i = 0; i < 12; i++)
        {
            Vector3 candidate = transform.position + direction * desiredMove;
            candidate.y = virtualFloorCenter.y;
            if (IsSafeMovementPoint(candidate, planeEdgeMargin))
            {
                result = candidate;
                return true;
            }
            desiredMove *= 0.75f;
            if (desiredMove < 0.05f) break;
        }
        return false;
    }

    private bool IsSafeMovementPoint(Vector3 worldPosition, float margin)
    {
        if (UsingEstimatedFloor)
        {
            Vector3 delta = worldPosition - virtualFloorCenter;
            if (Mathf.Abs(delta.y) > 0.25f)
                return false;
            delta.y = 0f;
            float usableRadius = Mathf.Max(0.1f, virtualFloorRadius - Mathf.Max(0f, margin));
            return delta.sqrMagnitude <= usableRadius * usableRadius;
        }
        return IsSafeWorldPointOnPlane(worldPosition, margin);
    }

    // =========================================================
    // World 위치가 안전하게 실제 Plane 안인지 확인
    // =========================================================

    private bool IsSafeWorldPointOnPlane(Vector3 worldPosition, float margin)
    {
        ARPlane plane = GetActivePlane();
        if (plane == null || plane.trackingState != TrackingState.Tracking)
            return false;

        NativeArray<Vector2> boundary = plane.boundary;
        if (!boundary.IsCreated || boundary.Length < 3)
            return false;

        Vector3 local = plane.transform.InverseTransformPoint(worldPosition);
        Vector2 point = new Vector2(local.x, local.z);
        return IsPointSafelyInsidePolygon(point, boundary, margin);
    }


    // =========================================================
    // Polygon 내부 + 가장자리 Margin 검사
    // =========================================================

    private bool IsPointSafelyInsidePolygon(
        Vector2 point,
        NativeArray<Vector2> polygon,
        float margin)
    {
        // 우선 중앙점 자체가 내부인지 확인
        if (
            !IsPointInsidePolygon(
                point,
                polygon
            )
        )
        {
            return false;
        }


        if (margin <= 0f)
            return true;


        // 중앙점 주변 여러 방향도 Plane 내부인지 검사
        // → 가장자리에서 너무 가까우면 false

        const int checkCount = 8;


        for (
            int i = 0;
            i < checkCount;
            i++
        )
        {
            float angle =
                (Mathf.PI * 2f) *
                i /
                checkCount;


            Vector2 offset =
                new Vector2(
                    Mathf.Cos(angle),
                    Mathf.Sin(angle)
                )
                *
                margin;


            if (
                !IsPointInsidePolygon(
                    point + offset,
                    polygon
                )
            )
            {
                return false;
            }
        }


        return true;
    }


    // =========================================================
    // Plane 합쳐짐 처리
    // =========================================================

    private ARPlane GetActivePlane()
    {
        if (floorSource != FloorSource.TrackedPlane) return null;
        while (
            movementPlane != null &&
            movementPlane.subsumedBy != null
        )
        {
            movementPlane =
                movementPlane.subsumedBy;
        }


        return movementPlane;
    }


    // =========================================================
    // Polygon 내부 판정
    // =========================================================

    private bool IsPointInsidePolygon(
        Vector2 point,
        NativeArray<Vector2> polygon)
    {
        bool inside = false;


        int j =
            polygon.Length - 1;


        for (
            int i = 0;
            i < polygon.Length;
            i++
        )
        {
            Vector2 pi =
                polygon[i];

            Vector2 pj =
                polygon[j];


            bool crosses =
                ((pi.y > point.y) !=
                 (pj.y > point.y))
                &&
                (
                    point.x <
                    (pj.x - pi.x) *
                    (point.y - pi.y) /
                    (pj.y - pi.y) +
                    pi.x
                );


            if (crosses)
                inside = !inside;


            j = i;
        }


        return inside;
    }


    // =========================================================
    // Animator
    // =========================================================

    private void SetWalking(
        bool walking)
    {
        if (animator != null)
        {
            animator.SetBool(
                "Walk",
                walking
            );
        }
    }


    // =========================================================
    // Look Controller
    // =========================================================

    private void SetLookAtCamera(
        bool enabled)
    {
        if (lookController != null)
        {
            lookController.SetLookAtCamera(
                enabled
            );
        }
    }

    public void BindModel(
    Animator newAnimator,
    DalsooLookController newLookController)
    {
        animator = newAnimator;
        lookController = newLookController;

        Debug.Log(
            $"BehaviorController 모델 연결 완료: {newAnimator.name}"
        );
    }
}