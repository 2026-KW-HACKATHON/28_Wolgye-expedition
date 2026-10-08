using UnityEngine;

public class QRDalsooSpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera arCamera;

    [Header("Dalsu Data")]
    [SerializeField] private DalsuDatabase dalsuDatabase;

    [Header("Spawn")]
    [Tooltip("카메라 앞 몇 m에 띄울지")]
    [SerializeField] private float spawnDistance = 1.5f;

    [Tooltip("화면 중앙에서 위/아래 위치 보정")]
    [SerializeField] private float verticalOffset = -0.2f;

    [Tooltip("모델 자체 정면 보정. 달수 모델이 Y 90도가 정면이면 90")]
    [SerializeField] private float modelRotationOffsetY = 90f;

    [Header("Runtime")]
    [SerializeField]
    private GameObject dalsuRuntimePrefab;

    private GameObject spawnedDalsoo;
    private bool hasSpawned = false;

    private DalsuData currentDalsuData;


    private void Awake()
    {
        if (arCamera == null)
            arCamera = Camera.main;
    }
    private void Start()
    {
        Debug.LogError("[DALSU] QRSpawner Start 실행됨");
        // QR 머지 전 임시 테스트
        //DalsuSceneContext.SelectedDalsuId = "Dalsu_001";
        SpawnDalsoo();
    }

    // ========================================
    // 친구의 QR 인식 코드에서 이 함수 호출
    // ========================================

    public void SpawnDalsoo()
    {
        if (dalsuDatabase == null)
        {
            Debug.LogError(
                "[DALSU] QRSpawner에 DalsuDatabase가 연결되지 않았습니다."
            );

            return;
        }

        Debug.LogError("[DALSU] SpawnDalsoo 호출됨");
        if (hasSpawned)
            return;

        if (arCamera == null)
            arCamera = Camera.main;


        // 이전 씬에서 넘어온 ID
        string selectedId =
            DalsuSceneContext.SelectedDalsuId;


        if (string.IsNullOrEmpty(selectedId))
        {
            Debug.LogError(
                "이전 씬에서 달수 ID가 넘어오지 않았습니다!"
            );

            return;
        }


        // ID에 맞는 DalsuData 찾기
        currentDalsuData =
            dalsuDatabase.GetById(selectedId);


        if (currentDalsuData == null)
        {
            Debug.LogError(
                $"DalsuData를 찾을 수 없습니다. ID: {selectedId}"
            );

            return;
        }


        if (currentDalsuData.prefab == null)
        {
            Debug.LogError(
                $"{currentDalsuData.dalsuName}의 Prefab이 없습니다."
            );

            return;
        }


        // ========================================
        // 카메라 정면 위치 계산
        // ========================================

        Vector3 spawnPosition =
            arCamera.transform.position
            + arCamera.transform.forward * spawnDistance
            + arCamera.transform.up * verticalOffset;


        // 카메라를 향하는 기본 방향
        Vector3 directionToCamera =
            arCamera.transform.position - spawnPosition;

        directionToCamera.y = 0f;

        Quaternion spawnRotation =
            Quaternion.LookRotation(
                directionToCamera,
                Vector3.up
            )
            *
            Quaternion.Euler(
                0f,
                modelRotationOffsetY,
                0f
            );


        // ========================================
        // ScriptableObject의 prefab 생성
        // ========================================

        Debug.LogError(
            $"[DALSU] Runtime Instantiate 직전 / prefab = " +
            $"{(dalsuRuntimePrefab != null ? dalsuRuntimePrefab.name : "NULL")}"
        );

        spawnedDalsoo =
            Instantiate(
                dalsuRuntimePrefab,
                spawnPosition,
                spawnRotation
            );


        DalsuActor actor =
            spawnedDalsoo.GetComponent<DalsuActor>();

        if (actor == null)
        {
            Debug.LogError(
                "DalsuRuntime에 DalsuActor가 없습니다!"
            );

            Destroy(spawnedDalsoo);
            spawnedDalsoo = null;

            return;
        }


        actor.Initialize(
            currentDalsuData,
            DalsuActor.SpawnMode.QR
        );


        hasSpawned = true;

        
        Debug.Log(
            $"QR 달수 등장! " +
            $"ID: {currentDalsuData.id}, " +
            $"이름: {currentDalsuData.dalsuName}"
        );
    }


    // ========================================
    // 필요할 때 초기화
    // ========================================

    public void ResetDalsoo()
    {
        hasSpawned = false;
        spawnedDalsoo = null;
        currentDalsuData = null;
    }
}