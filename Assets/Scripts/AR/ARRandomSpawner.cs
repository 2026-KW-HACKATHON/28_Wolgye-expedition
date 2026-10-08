using System.Collections;
using Unity.Collections;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class ARRandomSpawner : MonoBehaviour
{
    [Header("AR")]
    [SerializeField] private ARPlaneManager planeManager;
    [SerializeField] private Camera arCamera;

    [Header("Dalsu Runtime")]
    [SerializeField] private GameObject dalsuRuntimePrefab;

    [Header("Dalsu Data")]
    [SerializeField] private DalsuDatabase dalsuDatabase;

    [Header("Visual")]
    [SerializeField] private float visualYOffset = 0f;

    [Header("Spawn Area")]
    [SerializeField] private float minSpawnDistance = 1.0f;
    [SerializeField] private float maxSpawnDistance = 4.0f;
    [SerializeField] private float minimumBelowCamera = 0.5f;

    [Header("Hidden Spawn")]
    [SerializeField] private float visibilityCheckHeight = 0.5f;
    [SerializeField] private float screenPadding = 0.05f;
    [SerializeField] private int samplesPerPlane = 30;
    [SerializeField] private float retryInterval = 0.25f;

    [Header("Guide UI")]
    [SerializeField] private GameObject lookAroundText;

    private bool dalsuFound = false;

    private GameObject spawnedObject;
    private GameObject anchorRoot;

    private string spawnDalsuId;
    private DalsuData currentDalsuData;

    private void Awake()
    {
        dalsuFound = false;

        if (lookAroundText != null)
        {
            lookAroundText.SetActive(true);
        }
    }

    private void Update()
    {
        if (dalsuFound)
            return;

        // 바닥 인식 중이거나 아직 스폰되지 않았다면
        // 안내 UI를 그대로 유지
        if (spawnedObject == null || arCamera == null)
            return;

        if (IsDalsuInCameraView())
        {
            dalsuFound = true;

            if (lookAroundText != null)
            {
                lookAroundText.SetActive(false);
            }

            Debug.Log("[DALSU] 달수 발견! 안내 UI 비활성화");
        }
    }

    private bool IsDalsuInCameraView()
    {
        Plane[] planes =
            GeometryUtility.CalculateFrustumPlanes(arCamera);

        Renderer[] renderers =
            spawnedObject.GetComponentsInChildren<Renderer>();

        foreach (Renderer renderer in renderers)
        {
            if (!renderer.enabled ||
                !renderer.gameObject.activeInHierarchy)
                continue;

            if (GeometryUtility.TestPlanesAABB(
                planes,
                renderer.bounds))
            {
                return true;
            }
        }

        return false;
    }

    private IEnumerator Start()
    {
        // -----------------------------------------
        // 먼저 달수 데이터 찾기
        // -----------------------------------------

        spawnDalsuId = DalsuSceneContext.SelectedDalsuId;
        currentDalsuData = dalsuDatabase.GetById(spawnDalsuId);

        if (currentDalsuData == null)
        {
            Debug.LogError(
                $"[DALSU] ARSpawner에서 DalsuData를 찾을 수 없음: {spawnDalsuId}"
            );

            yield break;
        }


        if (dalsuRuntimePrefab == null)
        {
            Debug.LogError(
                "[DALSU] ARSpawner의 DalsuRuntimePrefab이 연결되지 않았습니다."
            );

            yield break;
        }


        // -----------------------------------------
        // AR Tracking 대기
        // -----------------------------------------

        while (
            ARSession.state
            != ARSessionState.SessionTracking)
        {
            yield return null;
        }


        Debug.Log(
            "[DALSU] AR Tracking 시작"
        );


        yield return new WaitForSeconds(1f);


        // -----------------------------------------
        // 화면 밖 바닥 찾기
        // -----------------------------------------

        while (spawnedObject == null)
        {
            if (
                TryFindHiddenFloorPosition(
                    out Vector3 spawnPosition,
                    out ARPlane spawnPlane))
            {
                SpawnDalsoo(
                    spawnPosition,
                    spawnPlane
                );

                yield break;
            }


            Debug.Log(
                "[DALSU] 화면 밖의 인식된 바닥 찾는 중..."
            );


            yield return new WaitForSeconds(
                retryInterval
            );
        }
    }




    // ==================================================
    // 화면 밖 Plane 위치 찾기
    // ==================================================

    private bool TryFindHiddenFloorPosition(
        out Vector3 result,
        out ARPlane resultPlane)
    {
        result = Vector3.zero;
        resultPlane = null;


        if (
            planeManager == null
            || arCamera == null)
        {
            return false;
        }


        foreach (
            ARPlane plane
            in planeManager.trackables)
        {
            if (plane.subsumedBy != null)
                continue;


            if (
                plane.alignment
                != PlaneAlignment.HorizontalUp)
            {
                continue;
            }


            NativeArray<Vector2> boundary =
                plane.boundary;


            if (
                !boundary.IsCreated
                || boundary.Length < 3)
            {
                continue;
            }


            Vector2 min = boundary[0];
            Vector2 max = boundary[0];


            for (
                int i = 1;
                i < boundary.Length;
                i++)
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
                attempt < samplesPerPlane;
                attempt++)
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
                    !IsPointInsidePolygon(
                        localPoint,
                        boundary))
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


                float belowCamera =
                    arCamera.transform.position.y
                    - worldPoint.y;


                if (
                    belowCamera
                    < minimumBelowCamera)
                {
                    continue;
                }


                Vector3 horizontalDifference =
                    worldPoint
                    - arCamera.transform.position;


                horizontalDifference.y = 0f;


                float distance =
                    horizontalDifference.magnitude;


                if (
                    distance < minSpawnDistance
                    || distance > maxSpawnDistance)
                {
                    continue;
                }


                if (
                    IsCurrentlyVisible(
                        worldPoint))
                {
                    continue;
                }


                result = worldPoint;
                resultPlane = plane;


                Debug.Log(
                    $"[DALSU] 숨은 Spawn 위치 발견 / 거리 = {distance:F2}m"
                );


                return true;
            }
        }


        return false;
    }


    // ==================================================
    // 현재 화면에 보이는지
    // ==================================================

    private bool IsCurrentlyVisible(
        Vector3 floorPosition)
    {
        Vector3 bottomViewport =
            arCamera.WorldToViewportPoint(
                floorPosition
            );


        Vector3 centerPosition =
            floorPosition
            + Vector3.up
            * visibilityCheckHeight;


        Vector3 centerViewport =
            arCamera.WorldToViewportPoint(
                centerPosition
            );


        return
            IsViewportPointVisible(
                bottomViewport)
            ||
            IsViewportPointVisible(
                centerViewport);
    }


    private bool IsViewportPointVisible(
        Vector3 viewport)
    {
        if (viewport.z <= 0f)
            return false;


        return
            viewport.x >= -screenPadding
            &&
            viewport.x <= 1f + screenPadding
            &&
            viewport.y >= -screenPadding
            &&
            viewport.y <= 1f + screenPadding;
    }


    // ==================================================
    // 실제 달수 생성
    // ==================================================

    private void SpawnDalsoo(
        Vector3 position,
        ARPlane spawnPlane)
    {
        if (spawnedObject != null)
            return;


        // -----------------------------------------
        // Anchor 생성
        // -----------------------------------------

        anchorRoot =
            new GameObject(
                "DalsooAnchor"
            );


        anchorRoot.transform
            .SetPositionAndRotation(
                position,
                Quaternion.identity
            );


        anchorRoot.AddComponent<ARAnchor>();


        // -----------------------------------------
        // 공통 Runtime 생성
        // -----------------------------------------

        spawnedObject =
            Instantiate(
                dalsuRuntimePrefab,
                anchorRoot.transform
            );


        spawnedObject.transform.localPosition =
            new Vector3(
                0f,
                visualYOffset,
                0f
            );


        spawnedObject.transform.localRotation =
            Quaternion.identity;


        // -----------------------------------------
        // DalsuActor 초기화
        // -----------------------------------------

        DalsuActor actor =
            spawnedObject
            .GetComponent<DalsuActor>();


        if (actor == null)
        {
            Debug.LogError(
                "[DALSU] DalsuRuntime에 DalsuActor가 없습니다."
            );


            Destroy(spawnedObject);
            Destroy(anchorRoot);

            spawnedObject = null;
            anchorRoot = null;

            return;
        }


        actor.Initialize(
            currentDalsuData,
            DalsuActor.SpawnMode.PlaneAR,
            spawnPlane
        );


        // Share the existing actor and floor with the S25 front-view experiment.
        var floorTarget = spawnedObject.GetComponent<S25FloorTarget>();
        if (floorTarget == null) floorTarget = spawnedObject.AddComponent<S25FloorTarget>();
        floorTarget.Initialize(spawnPlane);

        Debug.Log(
            $"[DALSU] AR 달수 생성 완료 / " +
            $"ID = {currentDalsuData.id}, " +
            $"Name = {currentDalsuData.dalsuName}"
        );
    }


    // ==================================================
    // Polygon 내부 판정
    // ==================================================

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
            i++)
        {
            Vector2 pi =
                polygon[i];

            Vector2 pj =
                polygon[j];


            bool crosses =
                ((pi.y > point.y)
                != (pj.y > point.y))
                &&
                (
                    point.x
                    <
                    (pj.x - pi.x)
                    *
                    (point.y - pi.y)
                    /
                    (pj.y - pi.y)
                    +
                    pi.x
                );


            if (crosses)
            {
                inside = !inside;
            }


            j = i;
        }


        return inside;
    }
}