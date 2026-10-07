using Mapbox.BaseModule.Data.Vector2d;
using Mapbox.BaseModule.Map;
using Mapbox.BaseModule.Utilities;
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

    [Header("Spawn")]
    [SerializeField]
    private GameObject _dalsuMarker;

    [SerializeField]
    private float _despawnExtraDistance = 10f;

    [SerializeField]
    private float _minSpawnDistance = 10f;

    [SerializeField]
    private int _maxPositionAttempts = 30;

    [Header("Visible")]
    [SerializeField]
    private float _visibleDistance = 30f;

    [Header("Rarity Probability")]
    [SerializeField]
    private float _rarity1Probability = 80f;

    [SerializeField]
    private float _rarity2Probability = 40f;

    [SerializeField]
    private float _rarity3Probability = 10f;

    // 현재 Map Scene에 생성되어 있는 GameObject만 관리한다.
    // 실제 Dalsu 데이터는 DalsuSpawnStateManager가 관리한다.
    private readonly Dictionary<string, GameObject> _spawnedInstances = new();

    private LatitudeLongitude _currentLocation;
    private bool _hasLocation;

    private DalsuSpawnStateManager StateManager
    {
        get { return DalsuSpawnStateManager.Instance; }
    }

    public void UpdateLocation(LatitudeLongitude location)
    {
        _currentLocation = location;
        _hasLocation = true;

        CheckDalsuSpawn();
    }

    public void OnDalsuCaught(string instanceId)
    {
        if (StateManager == null)
        {
            Debug.LogError("[DalsuSpawner] DalsuSpawnStateManager가 없습니다.");
            return;
        }

        if (!StateManager.TryGetDalsu(
                instanceId,
                out DalsuSpawnStateManager.SpawnedDalsuData spawnedDalsu))
        {
            Debug.LogWarning(
                $"[DalsuSpawner] 잡힌 Dalsu를 찾을 수 없습니다. instanceId = {instanceId}");

            return;
        }

        DalsuData caughtDalsu = spawnedDalsu.data;

        if (caughtDalsu == null)
        {
            Debug.LogError(
                $"[DalsuSpawner] DalsuData가 null입니다. instanceId = {instanceId}");

            return;
        }

        Debug.Log(
            $"[DalsuSpawner] Dalsu Catch! " +
            $"name = {caughtDalsu.dalsuName} / " +
            $"instanceId = {instanceId}");

        // 현재 Scene의 GameObject 제거
        if (_spawnedInstances.TryGetValue(
                instanceId,
                out GameObject instance))
        {
            Destroy(instance);
            _spawnedInstances.Remove(instanceId);
        }

        // 실제 Spawn 데이터도 제거
        // → Map Scene으로 돌아와도 다시 생성되지 않는다.
        StateManager.RemoveDalsu(instanceId);

        // AR Scene에 어떤 Dalsu를 잡았는지 전달
        DalsuSceneContext.SelectedDalsuId = caughtDalsu.id;

        SceneManager.LoadScene("AR");
    }

    private void CheckDalsuSpawn()
    {
        if (!_hasLocation)
            return;

        if (_mapBehaviour == null)
        {
            Debug.LogWarning("[DalsuSpawner] MapBehaviour가 없습니다.");
            return;
        }

        if (_mapBehaviour.MapboxMap == null)
        {
            Debug.LogWarning("[DalsuSpawner] MapboxMap이 없습니다.");
            return;
        }

        if (_spawnAreas == null || _spawnAreas.Count == 0)
            return;

        if (StateManager == null)
        {
            Debug.LogError(
                "[DalsuSpawner] DalsuSpawnStateManager가 없습니다.");

            return;
        }

        foreach (DalsuSpawnArea area in _spawnAreas)
        {
            if (area == null)
                continue;

            LatitudeLongitude areaLocation =
                new LatitudeLongitude(
                    area.latitude,
                    area.longitude);

            float distanceToArea =
                CalculateDistance(
                    _currentLocation,
                    areaLocation);

            // Spawn Area 내부
            if (distanceToArea <= area.radius)
            {
                // 처음 들어왔을 때만 Spawn 시도
                if (!StateManager.IsAreaEntered(area.areaId))
                {
                    StateManager.EnterArea(area.areaId);

                    Debug.Log(
                        $"[DalsuSpawner] SpawnArea 진입 / " +
                        $"area = {area.areaId}");

                    CreateSpawnPositions(area);
                }

                // 이미 생성된 Spawn 데이터를 기준으로
                // 현재 위치에서 30m 이내의 Dalsu만 GameObject 생성
                UpdateAreaVisibility(area);

                continue;
            }

            // Spawn Area + 여유 거리보다 멀어짐
            if (distanceToArea >
                area.radius + _despawnExtraDistance)
            {
                if (StateManager.IsAreaEntered(area.areaId))
                {
                    Debug.Log(
                        $"[DalsuSpawner] SpawnArea 이탈 / " +
                        $"area = {area.areaName}");

                    StateManager.ExitArea(area.areaId);
                }

                // 해당 Area의 Spawn 데이터까지 제거
                DespawnArea(area);
            }
            else
            {
                // Area 바로 바깥쪽에서는 기존 Spawn 데이터를 유지
                UpdateAreaVisibility(area);
            }
        }
    }

    private void CreateSpawnPositions(DalsuSpawnArea area)
    {
        if (area.spawnEntries == null ||
            area.spawnEntries.Count == 0)
        {
            Debug.LogWarning(
                $"[DalsuSpawner] SpawnEntry가 없습니다. " +
                $"area = {area.areaId}");

            return;
        }

        int spawnCount = 0;

        while (spawnCount < area.maxSpawnCount)
        {
            // Weight를 기준으로 Dalsu 종류 선택
            DalsuData data = SelectRandomDalsu(area);

            if (data == null)
                break;

            // 희귀도에 따른 Spawn 확률
            if (!RollSpawnProbability(data.rarity))
            {
                spawnCount++;
                continue;
            }

            // Spawn Area 내부 랜덤 위치
            if (!TryGetRandomSpawnLocation(
                    area,
                    out LatitudeLongitude location))
            {
                spawnCount++;
                continue;
            }

            // 실제 Spawn 하나를 식별하기 위한 Runtime ID
            string instanceId =
                Guid.NewGuid().ToString();

            DalsuSpawnStateManager.SpawnedDalsuData spawnedDalsu =
                new DalsuSpawnStateManager.SpawnedDalsuData
                {
                    instanceId = instanceId,
                    data = data,
                    areaId = area.areaId,
                    location = location
                };

            // GameObject는 생성하지 않고
            // Spawn 데이터만 저장한다.
            StateManager.AddDalsu(spawnedDalsu);

            spawnCount++;
        }

        Debug.Log(
            $"[DalsuSpawner] Spawn 데이터 생성 완료 / " +
            $"area = {area.areaId}");
    }

    private DalsuData SelectRandomDalsu(
        DalsuSpawnArea area)
    {
        float totalWeight = 0f;

        foreach (DalsuSpawnEntry entry in area.spawnEntries)
        {
            if (entry == null ||
                entry.dalsuData == null ||
                entry.weight <= 0f)
            {
                continue;
            }

            totalWeight += entry.weight;
        }

        if (totalWeight <= 0f)
            return null;

        float randomValue =
            UnityEngine.Random.Range(
                0f,
                totalWeight);

        float currentWeight = 0f;

        foreach (DalsuSpawnEntry entry in area.spawnEntries)
        {
            if (entry == null ||
                entry.dalsuData == null ||
                entry.weight <= 0f)
            {
                continue;
            }

            currentWeight += entry.weight;

            if (randomValue <= currentWeight)
                return entry.dalsuData;
        }

        return null;
    }

    private bool TryGetRandomSpawnLocation(
        DalsuSpawnArea area,
        out LatitudeLongitude location)
    {
        const double metersPerDegreeLatitude = 111320.0;

        for (int i = 0;
             i < _maxPositionAttempts;
             i++)
        {
            // 원 안에서 균일하게 뽑기 위해 sqrt 사용
            float distance =
                Mathf.Sqrt(
                    UnityEngine.Random.value) *
                area.radius;

            float angle =
                UnityEngine.Random.Range(
                    0f,
                    Mathf.PI * 2f);

            float northMeters =
                Mathf.Cos(angle) * distance;

            float eastMeters =
                Mathf.Sin(angle) * distance;

            double latitude =
                area.latitude +
                northMeters /
                metersPerDegreeLatitude;

            double metersPerDegreeLongitude =
                metersPerDegreeLatitude *
                Math.Cos(
                    area.latitude *
                    Mathf.Deg2Rad);

            double longitude =
                area.longitude +
                eastMeters /
                metersPerDegreeLongitude;

            location =
                new LatitudeLongitude(
                    latitude,
                    longitude);

            // 다른 Dalsu와 너무 가까우면 다시 뽑는다.
            if (IsValidSpawnPosition(location))
                return true;
        }

        location = default;
        return false;
    }

    private bool IsValidSpawnPosition(
        LatitudeLongitude location)
    {
        foreach (
            DalsuSpawnStateManager.SpawnedDalsuData spawnedDalsu
            in StateManager.SpawnedDalsu.Values)
        {
            float distance =
                CalculateDistance(
                    spawnedDalsu.location,
                    location);

            if (distance < _minSpawnDistance)
                return false;
        }

        return true;
    }

    private void UpdateAreaVisibility(
        DalsuSpawnArea area)
    {
        foreach (
            DalsuSpawnStateManager.SpawnedDalsuData spawnedDalsu
            in StateManager.SpawnedDalsu.Values)
        {
            if (spawnedDalsu.areaId != area.areaId)
                continue;

            float distance =
                CalculateDistance(
                    _currentLocation,
                    spawnedDalsu.location);

            // 30m 이내
            if (distance <= _visibleDistance)
            {
                // 데이터는 있지만 GameObject가 없다면 생성
                if (!_spawnedInstances.ContainsKey(
                        spawnedDalsu.instanceId))
                {
                    SpawnDalsu(spawnedDalsu);
                }

                continue;
            }

            // 30m 밖으로 나갔다면
            // GameObject만 제거한다.
            // Spawn 데이터는 유지한다.
            if (_spawnedInstances.ContainsKey(
                    spawnedDalsu.instanceId))
            {
                DespawnDalsu(
                    spawnedDalsu.instanceId);
            }
        }
    }

    private void SpawnDalsu(
        DalsuSpawnStateManager.SpawnedDalsuData spawnedDalsu)
    {
        if (_spawnedInstances.ContainsKey(
                spawnedDalsu.instanceId))
        {
            return;
        }

        Vector3 localPosition =
            _mapBehaviour.MapboxMap.MapInformation
                .ConvertLatLngToPosition(
                    spawnedDalsu.location);

        // Marker 생성
        GameObject marker =
            Instantiate(
                _dalsuMarker,
                _mapBehaviour.MapboxMap
                    .UnityContext
                    .MapRoot,
                false);

        marker.transform.localPosition =
            localPosition;

        // 실제 Dalsu 생성
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

        // DalsuController에 Runtime ID와 Spawner 전달
        controller.Initialize(
            spawnedDalsu.instanceId,
            this);

        // 현재 Scene에서 생성된 GameObject만 관리
        _spawnedInstances.Add(
            spawnedDalsu.instanceId,
            marker);

        // 현재 구조에서는 30m 안에 들어온 순간 바로 잡을 수 있다.
        controller.SetCanCatch(true);
    }

    private void DespawnDalsu(
        string instanceId)
    {
        if (!_spawnedInstances.TryGetValue(
                instanceId,
                out GameObject instance))
        {
            return;
        }

        Destroy(instance);

        _spawnedInstances.Remove(
            instanceId);

        Debug.Log(
            $"[DalsuSpawner] " +
            $"Dalsu GameObject Despawn / " +
            $"instanceId = {instanceId}");
    }

    private void DespawnArea(
        DalsuSpawnArea area)
    {
        // 수정 중 Dictionary를 직접 순회하지 않도록
        // 삭제할 ID 목록을 먼저 가져온다.
        List<string> removeIds =
            StateManager.GetDalsuIdsByArea(
                area.areaId);

        foreach (string instanceId in removeIds)
        {
            // 현재 Scene의 GameObject 제거
            if (_spawnedInstances.TryGetValue(
                    instanceId,
                    out GameObject instance))
            {
                Destroy(instance);
                _spawnedInstances.Remove(
                    instanceId);
            }

            // Persistent Spawn 데이터 제거
            StateManager.RemoveDalsu(
                instanceId);
        }
    }

    public void RefreshSpawnedPositions()
    {
        foreach (
            KeyValuePair<string, GameObject> pair
            in _spawnedInstances)
        {
            string instanceId = pair.Key;
            GameObject instance = pair.Value;

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

    private float CalculateDistance(
        LatitudeLongitude a,
        LatitudeLongitude b)
    {
        const float EarthRadius = 6371000f;

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

    private bool RollSpawnProbability(
        int rarity)
    {
        float probability =
            rarity switch
            {
                1 => _rarity1Probability,
                2 => _rarity2Probability,
                3 => _rarity3Probability,
                _ => 0f
            };

        float roll =
            UnityEngine.Random.Range(
                0f,
                100f);

        return roll < probability;
    }
}