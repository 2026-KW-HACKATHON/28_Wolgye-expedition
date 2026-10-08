using Mapbox.BaseModule.Data.Vector2d;
using Mapbox.BaseModule.Map;
using Mapbox.Example.Scripts.Map;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DalsuSpawner : MonoBehaviour
{
    [Header("Map")]
    [SerializeField]
    private MapboxMapBehaviour _mapBehaviour;


    [Header("Dalsu Spawn Areas")]
    [SerializeField]
    private List<DalsuSpawnArea> _spawnAreas = new();


    [Header("Default Dalsu")]
    [SerializeField]
    private DalsuData _defaultDalsu;


    [Header("Spawn Trigger Distance")]
    [Tooltip("다음 Dalsu Spawn까지 필요한 이동 거리의 최소값")]
    [SerializeField]
    private float _minSpawnTriggerDistance = 20f;

    [Tooltip("다음 Dalsu Spawn까지 필요한 이동 거리의 최대값")]
    [SerializeField]
    private float _maxSpawnTriggerDistance = 40f;


    [Header("Spawn Position")]
    [Tooltip("플레이어 주변에서 Dalsu가 생성될 최대 거리")]
    [SerializeField]
    private float _spawnRadius = 30f;

    [Tooltip("생성된 Dalsu끼리 유지해야 하는 최소 거리")]
    [SerializeField]
    private float _minSpawnDistance = 10f;

    [Tooltip("랜덤 위치를 찾기 위해 시도하는 최대 횟수")]
    [SerializeField]
    private int _maxPositionAttempts = 30;


    [Header("Dalsu Despawn")]
    [Tooltip("플레이어와 이 거리보다 멀어지면 Dalsu를 Despawn한다.")]
    [SerializeField]
    private float _despawnDistance = 50f;


    [Header("Dalsu Marker")]
    [SerializeField]
    private GameObject _dalsuMarker;


    [Header("Test Spawn")]
    [SerializeField]
    private bool _spawnTestDalsuOnFirstLocation = true;

    [SerializeField]
    private float _testSpawnDistance = 2f;


    // =========================================================
    // Runtime State
    // =========================================================

    // 현재 Map Scene에 실제로 생성되어 있는 GameObject
    //
    // instanceId -> Marker GameObject
    //
    // 주의:
    // 이것은 Scene 전용 상태다.
    // Map Scene이 파괴되면 같이 사라진다.
    private readonly Dictionary<string, GameObject> _spawnedInstances = new();


    // 현재 GPS 위치
    private LatitudeLongitude _currentLocation;


    // 마지막으로 Dalsu Spawn을 발생시킨 위치
    private LatitudeLongitude _lastSpawnLocation;


    // 현재 Spawn까지 필요한 랜덤 이동 거리
    private float _currentSpawnTriggerDistance;


    private bool _hasLocation;
    private bool _hasSpawnLocation;


    private DalsuSpawnStateManager StateManager
    {
        get
        {
            return DalsuSpawnStateManager.Instance;
        }
    }


    // =========================================================
    // Location
    // =========================================================

    public void UpdateLocation(
        LatitudeLongitude location)
    {
        _currentLocation = location;
        _hasLocation = true;


        // -----------------------------------------------------
        // 첫 GPS 위치
        // -----------------------------------------------------

        if (!_hasSpawnLocation)
        {
            _lastSpawnLocation = location;


            // 첫 번째 Spawn까지 필요한 이동 거리를 랜덤 결정
            SetNextSpawnTriggerDistance();


            _hasSpawnLocation = true;


            Debug.Log(
                "[DalsuSpawner] 최초 위치 저장 / " +
                $"다음 Spawn까지 {_currentSpawnTriggerDistance:F2}m");


            // -------------------------------------------------
            // Map Scene 재진입 시 기존 Dalsu 복원
            // -------------------------------------------------

            RestoreSpawnedDalsu();


            // -------------------------------------------------
            // 현재 위치 기준으로 너무 멀어진 Dalsu 제거
            // -------------------------------------------------

            CheckDalsuDespawn();


            // -------------------------------------------------
            // 테스트용 최초 Dalsu Spawn
            // -------------------------------------------------
            //
            // 이미 StateManager에 살아있는 Dalsu가 있다면
            // Scene 재진입 상황이므로 테스트 Dalsu를
            // 다시 생성하지 않는다.
            //

            if (_spawnTestDalsuOnFirstLocation &&
                StateManager != null &&
                StateManager.SpawnedDalsu.Count == 0)
            {
                SpawnTestDalsuNearPlayer();
            }


            return;
        }


        // -----------------------------------------------------
        // 기존 Dalsu 거리 검사
        // -----------------------------------------------------

        CheckDalsuDespawn();


        // -----------------------------------------------------
        // 새로운 Dalsu Spawn 검사
        // -----------------------------------------------------

        CheckDalsuSpawn();
    }


    // =========================================================
    // Spawn Check
    // =========================================================

    private void CheckDalsuSpawn()
    {
        if (!_hasLocation)
            return;


        if (!_hasSpawnLocation)
            return;


        if (_mapBehaviour == null)
        {
            Debug.LogWarning(
                "[DalsuSpawner] MapBehaviour가 없습니다.");

            return;
        }


        if (_mapBehaviour.MapboxMap == null)
        {
            Debug.LogWarning(
                "[DalsuSpawner] MapboxMap이 없습니다.");

            return;
        }


        if (StateManager == null)
        {
            Debug.LogError(
                "[DalsuSpawner] " +
                "DalsuSpawnStateManager가 없습니다.");

            return;
        }


        // -----------------------------------------------------
        // 마지막 Spawn 기준 위치와
        // 현재 플레이어 위치의 거리 계산
        // -----------------------------------------------------

        float distance =
            CalculateDistance(
                _lastSpawnLocation,
                _currentLocation);


        // 현재 랜덤 Spawn 거리만큼 이동하지 않았다면 종료
        if (distance < _currentSpawnTriggerDistance)
            return;


        Debug.Log(
            "[DalsuSpawner] Spawn 거리 도달 / " +
            $"distance = {distance:F2}m / " +
            $"required = {_currentSpawnTriggerDistance:F2}m");


        // Dalsu 하나 생성
        TrySpawnDalsu();


        // 현재 위치를 새로운 Spawn 기준 위치로 설정
        _lastSpawnLocation = _currentLocation;


        // 다음 Spawn까지 필요한 거리를 새로 랜덤 결정
        SetNextSpawnTriggerDistance();
    }


    // =========================================================
    // Next Spawn Distance
    // =========================================================

    private void SetNextSpawnTriggerDistance()
    {
        // Inspector 값이 뒤집혀 있어도 안전하게 처리
        float min =
            Mathf.Min(
                _minSpawnTriggerDistance,
                _maxSpawnTriggerDistance);


        float max =
            Mathf.Max(
                _minSpawnTriggerDistance,
                _maxSpawnTriggerDistance);


        _currentSpawnTriggerDistance =
            UnityEngine.Random.Range(
                min,
                max);


        Debug.Log(
            "[DalsuSpawner] " +
            $"다음 Spawn 거리 결정 / " +
            $"{_currentSpawnTriggerDistance:F2}m");
    }


    // =========================================================
    // Dalsu Spawn
    // =========================================================

    private void TrySpawnDalsu()
    {
        // -----------------------------------------------------
        // 1. 현재 플레이어가 어느 SpawnArea에 있는지 확인
        // -----------------------------------------------------

        DalsuSpawnArea currentArea =
            FindCurrentSpawnArea();


        // -----------------------------------------------------
        // 2. 등장할 Dalsu 선택
        // -----------------------------------------------------

        DalsuData selectedDalsu = null;


        if (currentArea != null)
        {
            selectedDalsu =
                SelectRandomDalsu(currentArea);


            Debug.Log(
                $"[DalsuSpawner] 현재 SpawnArea / " +
                $"area = {currentArea.areaName}");
        }
        else
        {
            selectedDalsu = _defaultDalsu;


            Debug.Log(
                "[DalsuSpawner] SpawnArea가 없습니다. " +
                "기본 Dalsu를 사용합니다.");
        }


        if (selectedDalsu == null)
        {
            Debug.LogWarning(
                "[DalsuSpawner] 생성할 DalsuData가 없습니다.");

            return;
        }


        // -----------------------------------------------------
        // 3. 플레이어 주변 랜덤 위치 선택
        // -----------------------------------------------------

        if (!TryGetRandomSpawnLocation(
                _currentLocation,
                out LatitudeLongitude spawnLocation))
        {
            Debug.LogWarning(
                "[DalsuSpawner] " +
                "Dalsu Spawn 위치를 찾지 못했습니다.");

            return;
        }


        // -----------------------------------------------------
        // 4. Runtime Instance ID 생성
        // -----------------------------------------------------

        string instanceId =
            Guid.NewGuid().ToString();


        // -----------------------------------------------------
        // 5. Area 정보
        // -----------------------------------------------------

        string areaId = null;


        if (currentArea != null)
        {
            areaId = currentArea.areaId;
        }


        // -----------------------------------------------------
        // 6. Spawn 데이터 생성
        // -----------------------------------------------------

        DalsuSpawnStateManager.SpawnedDalsuData spawnedDalsu =
            new DalsuSpawnStateManager.SpawnedDalsuData
            {
                instanceId = instanceId,
                data = selectedDalsu,
                areaId = areaId,
                location = spawnLocation
            };


        // -----------------------------------------------------
        // 7. Persistent State에 저장
        // -----------------------------------------------------

        StateManager.AddDalsu(
            spawnedDalsu);


        // -----------------------------------------------------
        // 8. 현재 Map Scene에 GameObject 생성
        // -----------------------------------------------------

        SpawnDalsu(
            spawnedDalsu);


        Debug.Log(
            $"[DalsuSpawner] Dalsu Spawn 완료 / " +
            $"name = {selectedDalsu.dalsuName} / " +
            $"instanceId = {instanceId} / " +
            $"area = {areaId ?? "DEFAULT"} / " +
            $"lat = {spawnLocation.Latitude:F6} / " +
            $"lon = {spawnLocation.Longitude:F6}");
    }


    // =========================================================
    // Test Spawn
    // =========================================================

    private void SpawnTestDalsuNearPlayer()
    {
        if (StateManager == null)
        {
            Debug.LogError(
                "[DalsuSpawner] " +
                "DalsuSpawnStateManager가 없습니다.");

            return;
        }


        if (_defaultDalsu == null)
        {
            Debug.LogWarning(
                "[DalsuSpawner] " +
                "테스트 Spawn할 Default Dalsu가 없습니다.");

            return;
        }


        // -----------------------------------------------------
        // 플레이어 바로 옆 위치
        // -----------------------------------------------------

        // Inspector에서 설정한 테스트 거리 사용
        double testDistance =
            Math.Max(
                0.0,
                _testSpawnDistance);


        const double metersPerDegreeLatitude =
            111320.0;


        // 북쪽 약 testDistance m 위치
        double latitude =
            _currentLocation.Latitude +
            testDistance /
            metersPerDegreeLatitude;


        double longitude =
            _currentLocation.Longitude;


        LatitudeLongitude spawnLocation =
            new LatitudeLongitude(
                latitude,
                longitude);


        // -----------------------------------------------------
        // Spawn 데이터 생성
        // -----------------------------------------------------

        string instanceId =
            Guid.NewGuid().ToString();


        DalsuSpawnStateManager.SpawnedDalsuData spawnedDalsu =
            new DalsuSpawnStateManager.SpawnedDalsuData
            {
                instanceId = instanceId,
                data = _defaultDalsu,
                areaId = null,
                location = spawnLocation
            };


        // -----------------------------------------------------
        // Persistent State 저장
        // -----------------------------------------------------

        StateManager.AddDalsu(
            spawnedDalsu);


        // -----------------------------------------------------
        // GameObject 생성
        // -----------------------------------------------------

        SpawnDalsu(
            spawnedDalsu);


        Debug.Log(
            "[DalsuSpawner] 테스트 Dalsu Spawn 완료 / " +
            $"name = {_defaultDalsu.dalsuName} / " +
            $"distance = {testDistance:F2}m / " +
            $"lat = {spawnLocation.Latitude:F6} / " +
            $"lon = {spawnLocation.Longitude:F6}");
    }


    // =========================================================
    // Find Current Spawn Area
    // =========================================================

    private DalsuSpawnArea FindCurrentSpawnArea()
    {
        if (_spawnAreas == null ||
            _spawnAreas.Count == 0)
        {
            return null;
        }


        DalsuSpawnArea closestArea = null;
        float closestDistance = float.MaxValue;


        foreach (DalsuSpawnArea area in _spawnAreas)
        {
            if (area == null)
                continue;


            LatitudeLongitude areaLocation =
                new LatitudeLongitude(
                    area.latitude,
                    area.longitude);


            float distance =
                CalculateDistance(
                    _currentLocation,
                    areaLocation);


            // 현재 Area 안에 있는지 확인
            if (distance > area.radius)
                continue;


            // 여러 Area가 겹치는 경우
            // 가장 가까운 Area를 사용
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestArea = area;
            }
        }


        return closestArea;
    }


    // =========================================================
    // Select Dalsu
    // =========================================================

    private DalsuData SelectRandomDalsu(
        DalsuSpawnArea area)
    {
        if (area == null)
            return null;


        if (area.spawnEntries == null ||
            area.spawnEntries.Count == 0)
        {
            Debug.LogWarning(
                $"[DalsuSpawner] SpawnEntry가 없습니다. " +
                $"area = {area.areaId}");

            return null;
        }


        float totalWeight = 0f;


        foreach (DalsuSpawnEntry entry in area.spawnEntries)
        {
            if (entry == null)
                continue;


            if (entry.dalsuData == null)
                continue;


            if (entry.weight <= 0f)
                continue;


            totalWeight += entry.weight;
        }


        if (totalWeight <= 0f)
        {
            Debug.LogWarning(
                $"[DalsuSpawner] 유효한 SpawnEntry가 없습니다. " +
                $"area = {area.areaId}");

            return null;
        }


        float randomValue =
            UnityEngine.Random.Range(
                0f,
                totalWeight);


        float currentWeight = 0f;


        foreach (DalsuSpawnEntry entry in area.spawnEntries)
        {
            if (entry == null)
                continue;


            if (entry.dalsuData == null)
                continue;


            if (entry.weight <= 0f)
                continue;


            currentWeight += entry.weight;


            if (randomValue <= currentWeight)
            {
                return entry.dalsuData;
            }
        }


        return null;
    }


    // =========================================================
    // Random Spawn Location
    // =========================================================

    private bool TryGetRandomSpawnLocation(
        LatitudeLongitude center,
        out LatitudeLongitude location)
    {
        const double metersPerDegreeLatitude =
            111320.0;


        for (int i = 0;
             i < _maxPositionAttempts;
             i++)
        {
            // 원 내부에서 균일한 위치를 선택
            float distance =
                Mathf.Sqrt(
                    UnityEngine.Random.value) *
                _spawnRadius;


            float angle =
                UnityEngine.Random.Range(
                    0f,
                    Mathf.PI * 2f);


            float northMeters =
                Mathf.Cos(angle) *
                distance;


            float eastMeters =
                Mathf.Sin(angle) *
                distance;


            double latitude =
                center.Latitude +
                northMeters /
                metersPerDegreeLatitude;


            double metersPerDegreeLongitude =
                metersPerDegreeLatitude *
                Math.Cos(
                    center.Latitude *
                    Mathf.Deg2Rad);


            double longitude =
                center.Longitude +
                eastMeters /
                metersPerDegreeLongitude;


            location =
                new LatitudeLongitude(
                    latitude,
                    longitude);


            if (IsValidSpawnPosition(location))
            {
                return true;
            }
        }


        location = default;
        return false;
    }


    // =========================================================
    // Spawn Position Validation
    // =========================================================

    private bool IsValidSpawnPosition(
        LatitudeLongitude location)
    {
        if (StateManager == null)
            return false;


        foreach (
            DalsuSpawnStateManager.SpawnedDalsuData spawnedDalsu
            in StateManager.SpawnedDalsu.Values)
        {
            if (spawnedDalsu == null)
                continue;


            float distance =
                CalculateDistance(
                    spawnedDalsu.location,
                    location);


            if (distance < _minSpawnDistance)
            {
                return false;
            }
        }


        return true;
    }


    // =========================================================
    // Restore Spawned Dalsu
    // =========================================================

    /// <summary>
    /// Map Scene이 다시 생성되었을 때
    /// StateManager에 남아있는 Dalsu를
    /// GameObject로 복원한다.
    /// </summary>
    private void RestoreSpawnedDalsu()
    {
        if (StateManager == null)
        {
            Debug.LogError(
                "[DalsuSpawner] " +
                "DalsuSpawnStateManager가 없습니다.");

            return;
        }


        if (_mapBehaviour == null ||
            _mapBehaviour.MapboxMap == null)
        {
            Debug.LogWarning(
                "[DalsuSpawner] " +
                "Map이 준비되지 않아 Dalsu 복원을 할 수 없습니다.");

            return;
        }


        int restoreCount = 0;


        foreach (
            DalsuSpawnStateManager.SpawnedDalsuData spawnedDalsu
            in StateManager.SpawnedDalsu.Values)
        {
            if (spawnedDalsu == null)
                continue;


            if (string.IsNullOrEmpty(
                    spawnedDalsu.instanceId))
            {
                continue;
            }


            // 이미 현재 Scene에 생성되어 있다면
            // 중복 생성하지 않는다.
            if (_spawnedInstances.ContainsKey(
                    spawnedDalsu.instanceId))
            {
                continue;
            }


            SpawnDalsu(
                spawnedDalsu);


            restoreCount++;
        }


        Debug.Log(
            "[DalsuSpawner] 기존 Dalsu 복원 완료 / " +
            $"restoreCount = {restoreCount} / " +
            $"stateCount = {StateManager.SpawnedDalsu.Count}");
    }


    // =========================================================
    // Dalsu Despawn
    // =========================================================

    /// <summary>
    /// 플레이어와 일정 거리 이상 멀어진 Dalsu를
    /// GameObject와 Persistent State에서 모두 제거한다.
    /// </summary>
    private void CheckDalsuDespawn()
    {
        if (!_hasLocation)
            return;


        if (StateManager == null)
            return;


        List<string> removeIds =
            new();


        // -----------------------------------------------------
        // 1. Despawn 대상 찾기
        // -----------------------------------------------------

        foreach (
            DalsuSpawnStateManager.SpawnedDalsuData spawnedDalsu
            in StateManager.SpawnedDalsu.Values)
        {
            if (spawnedDalsu == null)
                continue;


            float distance =
                CalculateDistance(
                    _currentLocation,
                    spawnedDalsu.location);


            if (distance > _despawnDistance)
            {
                removeIds.Add(
                    spawnedDalsu.instanceId);


                Debug.Log(
                    "[DalsuSpawner] " +
                    "Dalsu Despawn 대상 / " +
                    $"instanceId = {spawnedDalsu.instanceId} / " +
                    $"distance = {distance:F2}m / " +
                    $"limit = {_despawnDistance:F2}m");
            }
        }


        // -----------------------------------------------------
        // 2. GameObject + StateManager에서 제거
        // -----------------------------------------------------

        foreach (string instanceId in removeIds)
        {
            if (_spawnedInstances.TryGetValue(
                    instanceId,
                    out GameObject instance))
            {
                if (instance != null)
                {
                    Destroy(instance);
                }


                _spawnedInstances.Remove(
                    instanceId);
            }


            StateManager.RemoveDalsu(
                instanceId);
        }
    }


    // =========================================================
    // Spawn GameObject
    // =========================================================

    private void SpawnDalsu(
        DalsuSpawnStateManager.SpawnedDalsuData spawnedDalsu)
    {
        if (spawnedDalsu == null)
            return;


        if (spawnedDalsu.data == null)
        {
            Debug.LogError(
                "[DalsuSpawner] DalsuData가 null입니다.");

            return;
        }


        if (spawnedDalsu.data.prefab == null)
        {
            Debug.LogError(
                $"[DalsuSpawner] Prefab이 없습니다. " +
                $"dalsu = {spawnedDalsu.data.dalsuName}");

            return;
        }


        if (_dalsuMarker == null)
        {
            Debug.LogError(
                "[DalsuSpawner] DalsuMarker가 없습니다.");

            return;
        }


        if (_mapBehaviour == null ||
            _mapBehaviour.MapboxMap == null)
        {
            Debug.LogError(
                "[DalsuSpawner] MapboxMap이 준비되지 않았습니다.");

            return;
        }


        // 이미 현재 Scene에 생성되어 있다면
        // 중복 생성하지 않는다.
        if (_spawnedInstances.ContainsKey(
                spawnedDalsu.instanceId))
        {
            return;
        }


        Vector3 localPosition =
            _mapBehaviour.MapboxMap.MapInformation
                .ConvertLatLngToPosition(
                    spawnedDalsu.location);


        GameObject marker =
            Instantiate(
                _dalsuMarker,
                _mapBehaviour.MapboxMap
                    .UnityContext
                    .MapRoot,
                false);


        marker.transform.localPosition =
            localPosition;


        GameObject dalsu =
            Instantiate(
                spawnedDalsu.data.prefab,
                marker.transform,
                false);


        dalsu.transform.localPosition =
            Vector3.zero;


        dalsu.transform.localRotation =
            Quaternion.Euler(
                0f,
                UnityEngine.Random.Range(
                    0f,
                    360f),
                0f);


        dalsu.transform.localScale =
            Vector3.one;


        DalsuController controller =
            marker.GetComponent<DalsuController>();


        if (controller == null)
        {
            Debug.LogError(
                $"[DalsuSpawner] " +
                $"DalsuMarker에 DalsuController가 없습니다. " +
                $"{marker.name}");


            Destroy(marker);
            return;
        }


        controller.Initialize(
            spawnedDalsu.instanceId,
            this);


        _spawnedInstances.Add(
            spawnedDalsu.instanceId,
            marker);


        controller.SetCanCatch(true);
    }


    // =========================================================
    // Catch
    // =========================================================

    public void OnDalsuCaught(
        string instanceId)
    {
        if (StateManager == null)
        {
            Debug.LogError(
                "[DalsuSpawner] " +
                "DalsuSpawnStateManager가 없습니다.");

            return;
        }


        if (!StateManager.TryGetDalsu(
                instanceId,
                out DalsuSpawnStateManager.SpawnedDalsuData spawnedDalsu))
        {
            Debug.LogWarning(
                $"[DalsuSpawner] 잡힌 Dalsu를 찾을 수 없습니다. " +
                $"instanceId = {instanceId}");

            return;
        }


        DalsuData caughtDalsu =
            spawnedDalsu.data;


        if (caughtDalsu == null)
        {
            Debug.LogError(
                $"[DalsuSpawner] DalsuData가 null입니다. " +
                $"instanceId = {instanceId}");

            return;
        }


        Debug.Log(
            $"[DalsuSpawner] Dalsu Catch! " +
            $"name = {caughtDalsu.dalsuName} / " +
            $"instanceId = {instanceId}");


        // -----------------------------------------------------
        // 현재 Scene의 GameObject 제거
        // -----------------------------------------------------

        if (_spawnedInstances.TryGetValue(
                instanceId,
                out GameObject instance))
        {
            Destroy(instance);


            _spawnedInstances.Remove(
                instanceId);
        }


        // -----------------------------------------------------
        // Persistent State에서도 제거
        // -----------------------------------------------------

        StateManager.RemoveDalsu(
            instanceId);


        // -----------------------------------------------------
        // AR Scene으로 전달할 Dalsu 저장
        // -----------------------------------------------------

        DalsuSceneContext.SelectedDalsuId =
            caughtDalsu.id;


        SceneManager.LoadScene("AR");
    }


    // =========================================================
    // Despawn All Visible Dalsu
    // =========================================================

    /// <summary>
    /// 현재 Map Scene에 생성된 GameObject만 제거한다.
    ///
    /// 주의:
    /// StateManager의 Dalsu 데이터는 제거하지 않는다.
    ///
    /// Scene 전환 때문에 GameObject를 정리하는 용도다.
    /// Map Scene에 다시 들어오면 RestoreSpawnedDalsu()
    /// 를 통해 다시 생성된다.
    /// </summary>
    public void DespawnAllVisibleDalsu()
    {
        List<string> removeIds =
            new();


        foreach (
            KeyValuePair<string, GameObject> pair
            in _spawnedInstances)
        {
            removeIds.Add(
                pair.Key);
        }


        foreach (string instanceId in removeIds)
        {
            if (_spawnedInstances.TryGetValue(
                    instanceId,
                    out GameObject instance))
            {
                if (instance != null)
                {
                    Destroy(instance);
                }


                _spawnedInstances.Remove(
                    instanceId);
            }
        }
    }


    // =========================================================
    // Refresh
    // =========================================================

    public void RefreshSpawnedPositions()
    {
        if (_mapBehaviour == null ||
            _mapBehaviour.MapboxMap == null)
        {
            return;
        }


        if (StateManager == null)
            return;


        foreach (
            KeyValuePair<string, GameObject> pair
            in _spawnedInstances)
        {
            string instanceId =
                pair.Key;


            GameObject instance =
                pair.Value;


            if (instance == null)
                continue;


            if (!StateManager.TryGetDalsu(
                    instanceId,
                    out DalsuSpawnStateManager.SpawnedDalsuData spawnedDalsu))
            {
                continue;
            }


            Vector3 localPosition =
                _mapBehaviour.MapboxMap.MapInformation
                    .ConvertLatLngToPosition(
                        spawnedDalsu.location);


            instance.transform.localPosition =
                localPosition;
        }
    }


    // =========================================================
    // Distance
    // =========================================================

    private float CalculateDistance(
        LatitudeLongitude a,
        LatitudeLongitude b)
    {
        const float EarthRadius =
            6371000f;


        float lat1 =
            Mathf.Deg2Rad *
            (float)a.Latitude;


        float lat2 =
            Mathf.Deg2Rad *
            (float)b.Latitude;


        float deltaLat =
            Mathf.Deg2Rad *
            (float)(
                b.Latitude -
                a.Latitude);


        float deltaLon =
            Mathf.Deg2Rad *
            (float)(
                b.Longitude -
                a.Longitude);


        float sinLat =
            Mathf.Sin(
                deltaLat / 2f);


        float sinLon =
            Mathf.Sin(
                deltaLon / 2f);


        float h =
            sinLat * sinLat +
            Mathf.Cos(lat1) *
            Mathf.Cos(lat2) *
            sinLon * sinLon;


        return
            2f *
            EarthRadius *
            Mathf.Asin(
                Mathf.Sqrt(h));
    }
}