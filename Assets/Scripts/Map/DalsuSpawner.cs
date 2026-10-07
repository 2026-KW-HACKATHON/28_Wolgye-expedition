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


    /// <summary>
    /// 실제 GameObject가 생성되어 있는지와 관계없이
    /// 하나의 달수 스폰 정보를 관리한다.
    /// </summary>
    private class SpawnedDalsu
    {
        public string instanceId;
        public DalsuData data;
        public string areaId;
        public LatitudeLongitude location;

        // 30m 이내에 들어왔을 때 생성되는 실제 GameObject
        public GameObject instance;
    }


    // instanceId -> SpawnedDalsu
    private readonly Dictionary<string, SpawnedDalsu> _spawnedDalsu = new();

    // 플레이어가 현재 들어와 있는 SpawnArea
    private readonly HashSet<string> _spawnAreaEntered = new();

    // 현재 위치
    private LatitudeLongitude _currentLocation;

    private bool _hasLocation;


    // =========================================================
    // Location
    // =========================================================

    public void UpdateLocation(LatitudeLongitude location)
    {
        _currentLocation = location;
        _hasLocation = true;

        CheckDalsuSpawn();
    }


    // =========================================================
    // Catch
    // =========================================================

    public void OnDalsuCaught(string instanceId)
    {
        if (!_spawnedDalsu.TryGetValue(
                instanceId,
                out SpawnedDalsu spawnedDalsu))
        {
            Debug.LogWarning(
                $"[DalsuSpawner] 잡힌 Dalsu를 찾을 수 없습니다. " +
                $"instanceId = {instanceId}");

            return;
        }

        DalsuData caughtDalsu = spawnedDalsu.data;

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

        if (spawnedDalsu.instance != null)
        {
            Destroy(spawnedDalsu.instance);
        }

        _spawnedDalsu.Remove(instanceId);


        // 잡은 달수의 "종류 ID"를 AR에 전달
        // instanceId가 아니라 DalsuData의 id를 전달한다.
        DalsuSceneContext.SelectedDalsuId = caughtDalsu.id;

        SceneManager.LoadScene("AR");
    }


    // =========================================================
    // Spawn Check
    // =========================================================

    private void CheckDalsuSpawn()
    {
        if (!_hasLocation)
        {
            Debug.LogWarning("[DalsuSpawner] 위치가 없습니다.");
            return;
        }

        if (_mapBehaviour == null)
        {
            Debug.LogError(
                "[DalsuSpawner] MapBehaviour가 연결되지 않았습니다.");

            return;
        }

        if (_mapBehaviour.MapboxMap == null)
        {
            Debug.LogError(
                "[DalsuSpawner] MapboxMap이 null입니다.");

            return;
        }

        if (_spawnAreas == null || _spawnAreas.Count == 0)
        {
            Debug.LogWarning(
                "[DalsuSpawner] 등록된 SpawnArea가 없습니다.");

            return;
        }


        // -----------------------------------------------------
        // 1. SpawnArea 확인
        // -----------------------------------------------------

        foreach (DalsuSpawnArea area in _spawnAreas)
        {
            if (area == null)
            {
                continue;
            }

            float distanceToArea = CalculateDistance(
                _currentLocation,
                new LatitudeLongitude(
                    area.latitude,
                    area.longitude
                )
            );


            // =================================================
            // SpawnArea 안으로 들어온 경우
            // =================================================

            if (distanceToArea <= area.radius)
            {
                if (!_spawnAreaEntered.Contains(area.areaId))
                {
                    // 처음 들어온 경우
                    _spawnAreaEntered.Add(area.areaId);

                    Debug.Log(
                        $"[DalsuSpawner] SpawnArea 진입 / " +
                        $"area = {area.areaId}");

                    CreateSpawnPositions(area);
                }

                // 이미 생성된 GameObject가 있다면
                // 현재 위치에 따라 Visible / Despawn 처리
                UpdateAreaVisibility(area);

                continue;
            }


            // =================================================
            // SpawnArea에서 완전히 벗어난 경우
            // =================================================

            if (distanceToArea >
                area.radius + _despawnExtraDistance)
            {
                if (_spawnAreaEntered.Contains(area.areaId))
                {
                    Debug.Log(
                        $"[DalsuSpawner] SpawnArea 이탈 / " +
                        $"area = {area.areaName}");

                    _spawnAreaEntered.Remove(area.areaId);
                }

                DespawnArea(area);
            }
            else
            {
                // SpawnArea 주변에서는 기존 달수들의
                // Visible 상태만 계속 확인
                UpdateAreaVisibility(area);
            }
        }
    }


    // =========================================================
    // Spawn Position Creation
    // =========================================================

    /// <summary>
    /// SpawnArea에 들어왔을 때 호출된다.
    ///
    /// 여기서는 GameObject를 생성하지 않는다.
    /// 달수 종류와 실제 GPS 위치만 결정한다.
    /// </summary>
    private void CreateSpawnPositions(
        DalsuSpawnArea area)
    {
        if (area.spawnEntries == null ||
            area.spawnEntries.Count == 0)
        {
            Debug.LogWarning(
                $"[DalsuSpawner] SpawnEntry가 없습니다. " +
                $"area = {area.areaName}");

            return;
        }


        int spawnCount = 0;

        while (spawnCount < area.maxSpawnCount)
        {
            DalsuData data = SelectRandomDalsu(area);

            if (data == null)
            {
                Debug.LogWarning(
                    $"[DalsuSpawner] 랜덤 Dalsu 선택 실패. " +
                    $"area = {area.areaName}");

                break;
            }


            // ---------------------------------------------
            // 희귀도 확률
            // ---------------------------------------------

            if (!RollSpawnProbability(data.rarity))
            {
                Debug.Log(
                    $"[DalsuSpawner] Spawn 확률 실패 / " +
                    $"{data.dalsuName} / " +
                    $"rarity = {data.rarity}");

                spawnCount++;
                continue;
            }


            // ---------------------------------------------
            // 랜덤 GPS 위치 생성
            // ---------------------------------------------

            if (!TryGetRandomSpawnLocation(
                    area,
                    out LatitudeLongitude location))
            {
                Debug.LogWarning(
                    $"[DalsuSpawner] 랜덤 Spawn 위치 생성 실패 / " +
                    $"area = {area.areaName}");

                spawnCount++;
                continue;
            }


            // ---------------------------------------------
            // 런타임 인스턴스 생성
            // ---------------------------------------------

            string instanceId =
                Guid.NewGuid().ToString();

            SpawnedDalsu spawnedDalsu = new SpawnedDalsu
            {
                instanceId = instanceId,
                data = data,
                areaId = area.areaId,
                location = location,
                instance = null
            };

            _spawnedDalsu.Add(
                instanceId,
                spawnedDalsu
            );


            Debug.Log(
                $"[DalsuSpawner] Spawn 위치 결정 / " +
                $"name = {data.dalsuName} / " +
                $"lat = {location.Latitude:F6} / " +
                $"lon = {location.Longitude:F6} / " +
                $"instanceId = {instanceId}");

            spawnCount++;
        }
    }


    // =========================================================
    // Random Dalsu
    // =========================================================

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
        {
            return null;
        }


        float randomValue =
            UnityEngine.Random.Range(
                0f,
                totalWeight
            );

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
            {
                return entry.dalsuData;
            }
        }

        return null;
    }


    // =========================================================
    // Random GPS Position
    // =========================================================

    private bool TryGetRandomSpawnLocation(
        DalsuSpawnArea area,
        out LatitudeLongitude location)
    {
        const double metersPerDegreeLatitude =
            111320.0;

        for (int i = 0;
             i < _maxPositionAttempts;
             i++)
        {
            // 원 내부에 균등하게 배치하기 위해 sqrt 사용
            float distance =
                Mathf.Sqrt(
                    UnityEngine.Random.value
                ) * area.radius;

            float angle =
                UnityEngine.Random.Range(
                    0f,
                    Mathf.PI * 2f
                );

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
                    Mathf.Deg2Rad
                );

            double longitude =
                area.longitude +
                eastMeters /
                metersPerDegreeLongitude;


            location = new LatitudeLongitude(
                latitude,
                longitude
            );


            // 다른 달수와 너무 가까우면 다시 뽑는다.
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
        foreach (SpawnedDalsu spawnedDalsu
                 in _spawnedDalsu.Values)
        {
            float distance = CalculateDistance(
                spawnedDalsu.location,
                location
            );

            if (distance < _minSpawnDistance)
            {
                return false;
            }
        }

        return true;
    }


    // =========================================================
    // Visibility
    // =========================================================

    /// <summary>
    /// 실제 플레이어와 달수의 거리를 확인해서
    /// 30m 이내에 들어왔을 때 GameObject를 생성한다.
    /// </summary>
    private void UpdateAreaVisibility(
        DalsuSpawnArea area)
    {
        foreach (SpawnedDalsu spawnedDalsu
                 in _spawnedDalsu.Values)
        {
            if (spawnedDalsu.areaId != area.areaId)
            {
                continue;
            }


            float distance = CalculateDistance(
                _currentLocation,
                spawnedDalsu.location
            );


            // ---------------------------------------------
            // 30m 이내 → 생성
            // ---------------------------------------------

            if (distance <= _visibleDistance)
            {
                if (spawnedDalsu.instance == null)
                {
                    SpawnDalsu(spawnedDalsu);
                }

                continue;
            }


            // ---------------------------------------------
            // 30m 밖 → 제거
            // ---------------------------------------------

            if (spawnedDalsu.instance != null)
            {
                DespawnDalsu(spawnedDalsu);
            }
        }
    }


    // =========================================================
    // Actual GameObject Spawn
    // =========================================================

    private void SpawnDalsu(
        SpawnedDalsu spawnedDalsu)
    {
        if (spawnedDalsu.instance != null)
        {
            return;
        }


        Vector3 localPosition =
            _mapBehaviour.MapboxMap.MapInformation
                .ConvertLatLngToPosition(
                    spawnedDalsu.location
                );


        // DalsuMarker 생성
        GameObject marker = Instantiate(
            _dalsuMarker,
            _mapBehaviour.MapboxMap.UnityContext.MapRoot,
            false
        );

        marker.transform.localPosition =
            localPosition;


        // 실제 Dalsu 생성
        GameObject dalsu = Instantiate(
            spawnedDalsu.data.prefab,
            marker.transform,
            false
        );

        dalsu.transform.localPosition =
            Vector3.zero;

        dalsu.transform.localRotation =
            Quaternion.Euler(
                0f,
                UnityEngine.Random.Range(
                    0f,
                    360f
                ),
                0f
            );

        dalsu.transform.localScale =
            Vector3.one;


        DalsuController controller =
            marker.GetComponent<DalsuController>();

        if (controller == null)
        {
            Debug.LogError(
                $"[DalsuSpawner] DalsuMarker에 " +
                $"DalsuController가 없습니다. " +
                $"{marker.name}");

            Destroy(marker);
            return;
        }


        // 중요:
        // 이제 data.id가 아니라
        // 런타임 instanceId를 전달한다.
        controller.Initialize(
            spawnedDalsu.instanceId,
            this
        );


        spawnedDalsu.instance =
            marker;

        // 보이는 순간 바로 Catch 가능
        controller.SetCanCatch(true);

        //Debug.Log(
        //    $"[DalsuSpawner] Dalsu GameObject 생성 / " +
        //    $"{spawnedDalsu.data.dalsuName} / " +
        //    $"distance = " +
        //    $"{CalculateDistance(_currentLocation, spawnedDalsu.location):F1}m");
    }


    // =========================================================
    // Actual GameObject Despawn
    // =========================================================

    private void DespawnDalsu(
        SpawnedDalsu spawnedDalsu)
    {
        if (spawnedDalsu.instance == null)
        {
            return;
        }


        Destroy(
            spawnedDalsu.instance
        );

        spawnedDalsu.instance =
            null;

        Debug.Log(
            $"[DalsuSpawner] Dalsu GameObject Despawn / " +
            $"{spawnedDalsu.data.dalsuName}");
    }


    // =========================================================
    // Area Despawn
    // =========================================================

    private void DespawnArea(
        DalsuSpawnArea area)
    {
        List<string> removeIds =
            new List<string>();


        foreach (KeyValuePair<string, SpawnedDalsu>
                 pair in _spawnedDalsu)
        {
            SpawnedDalsu spawnedDalsu =
                pair.Value;

            if (spawnedDalsu.areaId != area.areaId)
            {
                continue;
            }


            if (spawnedDalsu.instance != null)
            {
                Destroy(
                    spawnedDalsu.instance
                );
            }

            removeIds.Add(
                spawnedDalsu.instanceId
            );
        }


        foreach (string instanceId in removeIds)
        {
            _spawnedDalsu.Remove(
                instanceId
            );
        }
    }


    // =========================================================
    // Map Refresh
    // =========================================================

    public void RefreshSpawnedPositions()
    {
        foreach (SpawnedDalsu spawnedDalsu
                 in _spawnedDalsu.Values)
        {
            if (spawnedDalsu.instance == null)
            {
                continue;
            }


            Vector3 localPosition =
                _mapBehaviour.MapboxMap.MapInformation
                    .ConvertLatLngToPosition(
                        spawnedDalsu.location
                    );


            spawnedDalsu.instance.transform.localPosition =
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
                a.Latitude
            );

        float deltaLon =
            Mathf.Deg2Rad *
            (float)(
                b.Longitude -
                a.Longitude
            );


        float sinLat =
            Mathf.Sin(
                deltaLat / 2f
            );

        float sinLon =
            Mathf.Sin(
                deltaLon / 2f
            );


        float h =
            sinLat * sinLat +
            Mathf.Cos(lat1) *
            Mathf.Cos(lat2) *
            sinLon * sinLon;


        return
            2f *
            EarthRadius *
            Mathf.Asin(
                Mathf.Sqrt(h)
            );
    }


    // =========================================================
    // Rarity Probability
    // =========================================================

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
                100f
            );


        return roll < probability;
    }
}