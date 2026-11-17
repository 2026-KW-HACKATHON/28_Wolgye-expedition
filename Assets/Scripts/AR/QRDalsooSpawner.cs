using UnityEngine;

/// <summary>
/// QR 전용 스포너. 일반 ARRandomSpawner는 변경하지 않는다.
/// QR 스캔 후 씬 진입 시 SelectedDalsuId로 자동 소환하거나 외부에서 SpawnDalsoo() 호출.
/// </summary>
public class QRDalsooSpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera arCamera;
    [SerializeField] private DalsuDatabase dalsuDatabase;
    [SerializeField] private GameObject dalsuRuntimePrefab;

    [Header("QR Spawn")]
    [Tooltip("카메라 전방 생성 거리(m)")]
    [SerializeField, Min(0.3f)] private float spawnDistance = 1.5f;
    [Tooltip("카메라 로컬 up 방향 오프셋(m)")]
    [SerializeField] private float verticalOffset = 0f;
    [Tooltip("실제 모델 정면 축 보정. 현재 프로젝트의 -right 정면에는 90도 사용")]
    [SerializeField] private float modelRotationOffsetY = 90f;

    [Header("QR 화면 표시 (QR 전용)")]
    [Tooltip("생성 직후 달수 모델의 보이는 중심을 화면 정중앙에 맞춥니다. 저장된 Vertical Offset 값보다 우선합니다.")]
    [SerializeField] private bool centerModelOnSpawn = true;
    [Tooltip("아래/위에서 보는 경우에도 얼굴이 보이도록, 3D 모델만 카메라를 향해 기울입니다. 이동 루트는 수평 유지.")]
    [SerializeField] private bool faceCameraVertically = true;
    [SerializeField, Range(0f, 85f)] private float maxVisualTiltDegrees = 75f;
    [SerializeField, Min(0f)] private float visualTiltFollowSpeed = 12f;
    [Tooltip("모델 정면 축이 반대일 때만 체크하세요.")]
    [SerializeField] private bool invertVisualTilt = false;

    [Header("QR Virtual Floor")]
    [Tooltip("달수 루트보다 몇 m 아래에 가상 바닥을 둘지")]
    [SerializeField, Min(0f)] private float floorBelowActor = 0.08f;
    [SerializeField, Min(0.3f)] private float movementRadius = 0.75f;
    [SerializeField] private bool keepMovementOnScreen = true;
    [SerializeField, Range(0.01f, 0.4f)] private float screenMargin = 0.13f;
    [Tooltip("달수에 Collider가 없으면 드래그용 BoxCollider 자동 생성")]
    [SerializeField] private bool addDragColliderIfMissing = true;

    [Header("Scene Direct Test (Optional)")]
    [Tooltip("QR 인식 없이 AR 씬에서 바로 실행할 때만 켜기")]
    [SerializeField] private bool spawnOnStartForTest = false;
    [Tooltip("테스트 시에만 DalsuSceneContext 대신 사용할 ID. 비워두면 이전 씬 ID")]
    [SerializeField] private string testDalsuId = "";

    [Header("QR Scene Auto Spawn")]
    [Tooltip("QR을 찍고 이 씬으로 넘어왔을 때 SelectedDalsuId가 있으면 자동 소환. ID가 늦게 들어와도 감지합니다.")]
    [SerializeField] private bool autoSpawnWhenSelectedIdAvailable = true;

    private string lastAutoAttemptedId = null;
    private GameObject spawnedDalsoo;
    private DalsuData currentDalsuData;
    private bool hasSpawned;

    private void Awake()
    {
        if (arCamera == null)
            arCamera = Camera.main;
    }

    private void Start()
    {
        Debug.Log($"[DALSU][QR] 씬 시작 / SelectedDalsuId='{DalsuSceneContext.SelectedDalsuId}'");
        if (spawnOnStartForTest)
            SpawnDalsoo();
        else
            TryAutoSpawnFromContext();
    }

    // QR 인식 후 씬이 먼저 열리고, SelectedDalsuId가 나중에 설정되는 경우도 대응.
    // 값이 그대로일 때는 반복 생성/에러 로그를 발생시키지 않습니다.
    private void Update()
    {
        if (!spawnOnStartForTest && !hasSpawned)
            TryAutoSpawnFromContext();
    }

    private void TryAutoSpawnFromContext()
    {
        if (!autoSpawnWhenSelectedIdAvailable || hasSpawned)
            return;

        string id = DalsuSceneContext.SelectedDalsuId;
        if (string.IsNullOrWhiteSpace(id) || id == lastAutoAttemptedId)
            return;

        lastAutoAttemptedId = id;
        Debug.Log($"[DALSU][QR] 선택된 달수 ID 감지: '{id}' / 자동 소환 시도");
        SpawnDalsoo();
    }

    // QR 인식 스크립트에서 직접 호출.
    public void SpawnDalsoo()
    {
        Debug.Log("[DALSU][QR] SpawnDalsoo() 호출 확인");
        if (hasSpawned && spawnedDalsoo != null)
            return;

        if (arCamera == null)
            arCamera = Camera.main;

        if (arCamera == null || dalsuDatabase == null || dalsuRuntimePrefab == null)
        {
            Debug.LogError("[DALSU][QR] Camera / DalsuDatabase / RuntimePrefab 연결을 확인하세요.");
            return;
        }

        string selectedId = DalsuSceneContext.SelectedDalsuId;
        if (spawnOnStartForTest && !string.IsNullOrWhiteSpace(testDalsuId))
            selectedId = testDalsuId;

        Debug.Log($"[DALSU][QR] 사용할 달수 ID: '{selectedId}'");
        if (string.IsNullOrWhiteSpace(selectedId))
        {
            Debug.LogError("[DALSU][QR] 달수 ID가 없습니다. QR 선택 정보 또는 Test Dalsu Id를 확인하세요.");
            return;
        }

        currentDalsuData = dalsuDatabase.GetById(selectedId);
        if (currentDalsuData == null || currentDalsuData.prefab == null)
        {
            Debug.LogError($"[DALSU][QR] 달수 데이터 또는 모델 Prefab을 찾을 수 없음: {selectedId}");
            return;
        }

        // 생성 기준은 화면 중앙의 시선(Ray). 모델의 실제 렌더링 중심은
        // Initialize 후 QRDalsooDragFloor.Setup에서 다시 정확히 중앙으로 맞춘다.
        Vector3 spawnPosition = arCamera.ViewportToWorldPoint(
            new Vector3(0.5f, 0.5f, Mathf.Max(0.3f, spawnDistance)))
            + arCamera.transform.up * verticalOffset;

        Quaternion spawnRotation = QRDalsooDragFloor.GetFaceCameraRotation(
            arCamera, spawnPosition, modelRotationOffsetY);

        spawnedDalsoo = Instantiate(dalsuRuntimePrefab, spawnPosition, spawnRotation);
        DalsuActor actor = spawnedDalsoo.GetComponent<DalsuActor>();

        if (actor == null)
        {
            Debug.LogError("[DALSU][QR] RuntimePrefab 루트에 DalsuActor가 없습니다.");
            Destroy(spawnedDalsoo);
            spawnedDalsoo = null;
            return;
        }

        actor.Initialize(currentDalsuData, DalsuActor.SpawnMode.QR);

        if (actor.ModelInstance == null)
        {
            Debug.LogError("[DALSU][QR] 모델 초기화 실패. ModelRoot/Animator/Hip 설정을 확인하세요.");
            Destroy(spawnedDalsoo);
            spawnedDalsoo = null;
            return;
        }

        // SetupQR은 기본 랜덤 행동을 비활성화한다.
        // 아래 QR 전용 컴포넌트가 가상 바닥을 설정한 뒤 다시 켠다.
        QRDalsooDragFloor qrFloor = spawnedDalsoo.GetComponent<QRDalsooDragFloor>();
        if (qrFloor == null)
            qrFloor = spawnedDalsoo.AddComponent<QRDalsooDragFloor>();

        qrFloor.Setup(arCamera, modelRotationOffsetY, floorBelowActor,
            movementRadius, keepMovementOnScreen, screenMargin,
            addDragColliderIfMissing,
            centerModelOnSpawn, faceCameraVertically,
            maxVisualTiltDegrees, visualTiltFollowSpeed,
            invertVisualTilt);

        hasSpawned = true;
        Debug.Log($"[DALSU][QR] 소환 완료: {currentDalsuData.dalsuName} / {selectedId}");
    }

    public void ResetDalsoo()
    {
        if (spawnedDalsoo != null)
            Destroy(spawnedDalsoo);
        spawnedDalsoo = null;
        currentDalsuData = null;
        hasSpawned = false;
        lastAutoAttemptedId = null;
    }
}
