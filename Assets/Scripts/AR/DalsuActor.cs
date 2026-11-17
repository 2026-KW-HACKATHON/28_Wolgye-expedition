using UnityEngine;
using UnityEngine.XR.ARFoundation;

public class DalsuActor : MonoBehaviour
{
    public enum SpawnMode
    {
        PlaneAR,
        QR,
        EstimatedFloor
    }

    [Header("Runtime")]
    [SerializeField] private Transform modelRoot;

    [Header("Common Controllers")]
    [SerializeField] private DalsooLookController lookController;
    [SerializeField] private DalsooBehaviorController behaviorController;
    [SerializeField] private DalsooCatchable catchable;

    private DalsuData data;
    private GameObject modelInstance;
    private Animator animator;
    private Transform hip;

    public DalsuData Data => data;
    public GameObject ModelInstance => modelInstance;
    public Animator Animator => animator;
    public Transform Hip => hip;
    public bool IsInitialized { get; private set; }
    public SpawnMode CurrentSpawnMode { get; private set; }


    private void Awake()
    {
        Debug.Log("[DALSU] DalsuActor Awake");

        if (lookController == null)
        {
            lookController =
                GetComponent<DalsooLookController>();
        }

        if (behaviorController == null)
        {
            behaviorController =
                GetComponent<DalsooBehaviorController>();
        }

        if (catchable == null)
        {
            catchable =
                GetComponent<DalsooCatchable>();
        }
    }


    public void Initialize(
        DalsuData newData,
        SpawnMode spawnMode,
        ARPlane plane = null,
        float virtualFloorRadius = 1.2f)
    {
        IsInitialized = false;
        Debug.Log("[DALSU] ==============================");
        Debug.Log("[DALSU] Initialize 시작");
        Debug.Log($"[DALSU] SpawnMode = {spawnMode}");


        // ------------------------------------------
        // Data 확인
        // ------------------------------------------

        if (newData == null)
        {
            Debug.LogError(
                "[DALSU] ❌ DalsuData가 NULL입니다."
            );

            return;
        }

        data = newData;

        Debug.Log(
            $"[DALSU] ✅ Data 확인 / " +
            $"ID = {data.id}, " +
            $"Name = {data.dalsuName}"
        );


        // ------------------------------------------
        // 모델 Prefab 확인
        // ------------------------------------------

        if (data.prefab == null)
        {
            Debug.LogError(
                $"[DALSU] ❌ {data.id}의 Prefab이 NULL입니다."
            );

            return;
        }

        Debug.Log(
            $"[DALSU] ✅ Model Prefab = {data.prefab.name}"
        );


        // ------------------------------------------
        // ModelRoot 확인
        // ------------------------------------------

        if (modelRoot == null)
        {
            Debug.LogError(
                "[DALSU] ❌ DalsuActor의 ModelRoot가 연결되지 않았습니다."
            );

            return;
        }

        Debug.Log(
            $"[DALSU] ✅ ModelRoot = {modelRoot.name}"
        );


        // ------------------------------------------
        // 모델 생성
        // ------------------------------------------

        CreateModel();

        if (modelInstance == null)
        {
            Debug.LogError(
                "[DALSU] ❌ 모델 Instantiate 실패"
            );

            return;
        }

        Debug.Log(
            $"[DALSU] ✅ 모델 Instantiate 성공 = {modelInstance.name}"
        );


        // ------------------------------------------
        // Animator / Hip 찾기
        // ------------------------------------------

        if (!FindModelComponents())
        {
            Debug.LogError(
                "[DALSU] ❌ 모델 컴포넌트 연결 실패"
            );

            return;
        }


        // ------------------------------------------
        // Controller 연결
        // ------------------------------------------

        BindControllers();


        // ------------------------------------------
        // 모드별 초기화
        // ------------------------------------------

        switch (spawnMode)
        {
            case SpawnMode.PlaneAR:

                Debug.Log(
                    "[DALSU] PlaneAR 모드 초기화"
                );

                if (plane != null)
                    SetupPlaneAR(plane);
                else
                {
                    Debug.LogWarning("[DALSU] PlaneAR에 Plane 없음: EstimatedFloor로 전환");
                    SetupEstimatedFloor(virtualFloorRadius);
                }

                break;


            case SpawnMode.QR:

                Debug.Log(
                    "[DALSU] QR 모드 초기화"
                );

                SetupQR();

                break;

            case SpawnMode.EstimatedFloor:
                Debug.Log("[DALSU] 추정 바닥 모드 초기화");
                SetupEstimatedFloor(virtualFloorRadius);
                break;
        }

        CurrentSpawnMode = spawnMode == SpawnMode.PlaneAR && plane == null
            ? SpawnMode.EstimatedFloor : spawnMode;
        IsInitialized = true;


        gameObject.name =
            $"DalsuRuntime_{data.id}";


        Debug.Log(
            $"[DALSU] ✅ 초기화 완료 / " +
            $"ID = {data.id}, " +
            $"Name = {data.dalsuName}"
        );

        Debug.Log("[DALSU] ==============================");
    }


    // ==================================================
    // 모델 생성
    // ==================================================

    private void CreateModel()
    {
        Debug.Log(
            "[DALSU] 모델 Instantiate 시도..."
        );


        modelInstance =
            Instantiate(
                data.prefab,
                modelRoot
            );


        modelInstance.transform.localPosition =
            Vector3.zero;

        modelInstance.transform.localRotation =
            Quaternion.identity;

        modelInstance.transform.localScale =
            Vector3.one;
    }


    // ==================================================
    // Animator / Hip 자동 검색
    // ==================================================

    private bool FindModelComponents()
    {
        Debug.Log(
            "[DALSU] Animator 검색 중..."
        );


        animator =
            modelInstance
            .GetComponentInChildren<Animator>(true);


        if (animator == null)
        {
            Debug.LogError(
                $"[DALSU] ❌ Animator를 찾을 수 없습니다. ID = {data.id}"
            );

            return false;
        }


        Debug.Log(
            $"[DALSU] ✅ Animator 발견 = {animator.name}"
        );


        // 모든 달수가
        // Armature -> Hip 구조이므로 우선 정확한 경로 검색
        hip =
            modelInstance.transform.Find(
                "Armature/Hip"
            );


        // 정확한 경로에서 못 찾았을 경우
        // 전체 자식에서 Hip 검색
        if (hip == null)
        {
            Debug.LogWarning(
                "[DALSU] Armature/Hip 경로에서 못 찾음. 재귀 검색 시작..."
            );

            hip =
                FindChildRecursive(
                    modelInstance.transform,
                    "Hip"
                );
        }


        if (hip == null)
        {
            Debug.LogError(
                $"[DALSU] ❌ Hip을 찾을 수 없습니다. ID = {data.id}"
            );

            return false;
        }


        Debug.Log(
            $"[DALSU] ✅ Hip 발견 = {hip.name}"
        );


        return true;
    }


    // ==================================================
    // 기존 Controller에 모델 연결
    // ==================================================

    private void BindControllers()
    {
        Debug.Log(
            "[DALSU] Controller 연결 시작..."
        );


        if (lookController != null)
        {
            lookController.BindModel(
                animator,
                hip
            );

            Debug.Log(
                "[DALSU] ✅ LookController 연결 완료"
            );
        }
        else
        {
            Debug.LogWarning(
                "[DALSU] ⚠ LookController가 없습니다."
            );
        }


        if (behaviorController != null)
        {
            behaviorController.BindModel(
                animator,
                lookController
            );

            Debug.Log(
                "[DALSU] ✅ BehaviorController 연결 완료"
            );
        }
        else
        {
            Debug.LogWarning(
                "[DALSU] ⚠ BehaviorController가 없습니다."
            );
        }


        if (catchable != null)
        {
            Debug.Log(
                "[DALSU] ✅ Catchable 확인 완료"
            );
        }
        else
        {
            Debug.LogWarning(
                "[DALSU] ⚠ Catchable이 없습니다."
            );
        }
    }


    // ==================================================
    // 일반 AR 모드
    // ==================================================

    private void SetupPlaneAR(
        ARPlane plane)
    {
        if (plane == null)
        {
            Debug.LogWarning("[DALSU] PlaneAR에 ARPlane이 없어서 추정 바닥 모드로 전환합니다.");
            Debug.LogError("[DALSU] PlaneAR 초기화에 Plane이 필요합니다.");
            return;
        }

        if (lookController != null)
        {
            lookController.enabled = true;
            lookController.SetLookAtCamera(true);

            Debug.Log(
                "[DALSU] ✅ PlaneAR LookController ON"
            );
        }


        if (behaviorController != null)
        {
            // EstimatedFloor에서 비활성화한 컨트롤러를 다시 켤 때,
            // OnEnable보다 먼저 실제 Plane 정보를 전달한다.
            behaviorController.Initialize(plane);
            behaviorController.enabled = true;

            Debug.Log(
                "[DALSU] ✅ PlaneAR BehaviorController ON"
            );
        }
    }


    // ==================================================
    // Depth / 카메라 높이 기반 추정 바닥 모드
    // 추정 바닥에서도 Wander, Approach를 제한된 반경 안에서 실행한다.
    // ==================================================

    private void SetupEstimatedFloor(float radius)
    {
        if (lookController != null)
        {
            lookController.enabled = true;
            lookController.SetLookAtCamera(true);
        }

        if (behaviorController != null)
        {
            // 현재 달수의 발 위치 (Runtime 루트)를 중심으로 이동 영역 생성.
            behaviorController.InitializeVirtualFloor(transform.position, radius);
            behaviorController.enabled = true;
        }
    }

    public void SetPlacementTransitionPaused(bool paused)
    {
        if (behaviorController != null)
            behaviorController.SetPlacementTransitionPaused(paused);
    }

    public void SetTrackingPresentationPaused(bool paused)
    {
        if (behaviorController != null)
            behaviorController.SetTrackingPresentationPaused(paused);
    }

    /// <summary>
    /// 추정 바닥에 배치한 달수가 실제 ARPlane을 찾았을 때,
    /// 모델을 재생성하지 않고 행동 컨트롤러만 활성화한다.
    /// </summary>
    public bool UpgradeToPlaneAR(ARPlane plane)
    {
        if (!IsInitialized || plane == null)
            return false;

        if (CurrentSpawnMode == SpawnMode.PlaneAR)
            return true;
        if (CurrentSpawnMode != SpawnMode.EstimatedFloor)
            return false; // QR 모드는 임의 전환 금지

        SetupPlaneAR(plane);
        CurrentSpawnMode = SpawnMode.PlaneAR;
        Debug.Log("[DALSU] 추정 바닥 → 실제 PlaneAR 전환 완료");
        return true;
    }


    // ==================================================
    // QR 모드
    // ==================================================

    private void SetupQR()
    {
        // QR에서는 랜덤 행동 X
        if (behaviorController != null)
        {
            behaviorController.enabled = false;

            Debug.Log(
                "[DALSU] ✅ QR BehaviorController OFF"
            );
        }


        // 카메라는 바라봄
        if (lookController != null)
        {
            lookController.enabled = true;
            lookController.SetLookAtCamera(true);

            Debug.Log(
                "[DALSU] ✅ QR LookController ON"
            );
        }
    }


    // ==================================================
    // 이름으로 자식 재귀 검색
    // ==================================================

    private Transform FindChildRecursive(
        Transform parent,
        string childName)
    {
        foreach (Transform child in parent)
        {
            if (child.name == childName)
            {
                return child;
            }


            Transform result =
                FindChildRecursive(
                    child,
                    childName
                );


            if (result != null)
            {
                return result;
            }
        }


        return null;
    }
}