using Mapbox.BaseModule.Data.Vector2d;
using Mapbox.BaseModule.Map;
using Mapbox.BaseModule.Utilities;
using Mapbox.Example.Scripts.Map;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DalsuSpawner : MonoBehaviour
{
    [Header("Map")]
    [SerializeField]
    private MapboxMapBehaviour _mapBehaviour;

    [Header("Dalsu Database")]
    [SerializeField]
    private DalsuDatabase _dalsuDatabase;

    [Header("Spawn")]
    [SerializeField]
    private Transform _spawnRoot;

    [SerializeField]
    private GameObject _dalsuMarker;

    [SerializeField]
    private float _despawnExtraDistance = 10f;

    [Header("Rarity Probability")]
    [SerializeField]
    private float _rarity1Probability = 80f;

    [SerializeField]
    private float _rarity2Probability = 40f;

    [SerializeField]
    private float _rarity3Probability = 10f;


    // 현재 Scene에 생성된 GameObject만 관리
    private readonly Dictionary<string, GameObject> _spawnedInstances = new();

    private LatitudeLongitude _currentLocation;
    private bool _hasLocation;


    private DalsuSpawnStateManager StateManager
    {
        get
        {
            return DalsuSpawnStateManager.Instance;
        }
    }


    public void UpdateLocation(
        LatitudeLongitude location)
    {
        _currentLocation = location;
        _hasLocation = true;

        CheckDalsuSpawn();
    }

    public void OnDalsuCaught(string id)
    {
        if (StateManager == null)
        {
            Debug.LogError(
                "[DalsuSpawner] " +
                "DalsuSpawnStateManager가 없습니다.");

            return;
        }

        if (!_spawnedInstances.TryGetValue(
                id,
                out GameObject instance))
        {
            Debug.LogWarning(
                $"[DalsuSpawner] " +
                $"잡힌 Dalsu를 찾을 수 없습니다. id = {id}");

            return;
        }

        DalsuData caughtDalsu =
            _dalsuDatabase.GetById(id);

        if (caughtDalsu == null)
        {
            Debug.LogError(
                $"[DalsuSpawner] " +
                $"DalsuData를 찾을 수 없습니다. id = {id}");

            return;
        }

        Debug.Log(
            $"[DalsuSpawner] Dalsu Catch! " +
            $"name = {caughtDalsu.dalsuName} / " +
            $"id = {id}");

        // 현재 Scene의 GameObject 제거
        Destroy(instance);

        _spawnedInstances.Remove(id);

        // 영구 상태에 잡힌 Dalsu 기록
        StateManager.AddCaughtDalsu(id);

        // AR Scene으로 전달
        DalsuSceneContext.SelectedDalsuId =
            caughtDalsu.id;

        SceneManager.LoadScene("AR");
    }


    private void CheckDalsuSpawn()
    {
        if (!_hasLocation)
        {
            return;
        }

        if (_mapBehaviour == null)
        {
            Debug.LogError(
                "[DalsuSpawner] " +
                "MapBehaviour가 연결되지 않았습니다.");

            return;
        }

        if (_mapBehaviour.MapboxMap == null)
        {
            Debug.LogError(
                "[DalsuSpawner] " +
                "MapboxMap이 null입니다.");

            return;
        }

        if (_dalsuDatabase == null)
        {
            Debug.LogError(
                "[DalsuSpawner] " +
                "DalsuDatabase가 연결되지 않았습니다.");

            return;
        }

        if (StateManager == null)
        {
            Debug.LogError(
                "[DalsuSpawner] " +
                "DalsuSpawnStateManager가 없습니다.");

            return;
        }

        var dalsuDatas =
            _dalsuDatabase.GetDalsuDatas();

        if (dalsuDatas == null)
        {
            Debug.LogError(
                "[DalsuSpawner] " +
                "DalsuData 배열이 null입니다.");

            return;
        }


        foreach (DalsuData data in dalsuDatas)
        {
            if (data == null)
            {
                continue;
            }

            // 이미 잡힌 Dalsu
            if (StateManager.IsCaught(data.id))
            {
                continue;
            }


            LatitudeLongitude dalsuLocation =
                new LatitudeLongitude(
                    data.latitude,
                    data.longitude);


            float distance =
                CalculateDistance(
                    _currentLocation,
                    dalsuLocation);


            // =====================================================
            // Spawn 가능 구역 안
            // =====================================================

            if (distance <= data.spawnRadius)
            {
                // 이미 이 구역에서 Spawn 판정을 했다면
                // 다시 확률을 굴리지 않는다.
                if (StateManager.IsAreaEntered(data.id))
                {
                    if (StateManager.IsSpawned(data.id) &&
                        !_spawnedInstances.ContainsKey(data.id))
                    {
                        Spawn(
                            data,
                            dalsuLocation);
                    }

                    continue;
                }


                // Spawn 가능 구역 최초 진입
                StateManager.EnterArea(data.id);

                // 아직 Spawn되지 않은 경우에만 확률 판정
                if (!StateManager.IsSpawned(data.id))
                {
                    if (RollSpawnProbability(
                            data.rarity))
                    {
                        Debug.Log(
                            $"[DalsuSpawner] Spawn 성공! " +
                            $"{data.dalsuName} / " +
                            $"rarity = {data.rarity}");

                        Spawn(
                            data,
                            dalsuLocation);
                    }
                    else
                    {
                        Debug.Log(
                            $"[DalsuSpawner] Spawn 실패! " +
                            $"{data.dalsuName} / " +
                            $"rarity = {data.rarity}");
                    }
                }

                continue;
            }


            // =====================================================
            // Spawn 가능 구역 완전히 이탈
            // =====================================================

            if (distance >
                data.spawnRadius +
                _despawnExtraDistance)
            {
                // 다음 진입 시
                // 다시 확률을 판정할 수 있도록 초기화
                StateManager.ExitArea(data.id);


                // 현재 Spawn되어 있다면 Despawn
                if (StateManager.IsSpawned(data.id))
                {
                    Despawn(data);
                }

                continue;
            }
        }
    }


    private void Spawn(
        DalsuData data,
        LatitudeLongitude location)
    {
        if (_spawnedInstances.ContainsKey(data.id))
        {
            return;
        }


        Vector3 localPosition =
            _mapBehaviour.MapboxMap.MapInformation
                .ConvertLatLngToPosition(location);


        // DalsuMarker 생성
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
                data.prefab,
                marker.transform,
                false);


        dalsu.transform.localPosition =
            Vector3.zero;


        dalsu.transform.localRotation =
            Quaternion.Euler(
                0f,
                Random.Range(
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
                $"DalsuMarker에 " +
                $"DalsuController가 없습니다. " +
                $"{marker.name}");

            Destroy(marker);

            return;
        }


        controller.Initialize(
            data.id,
            this);


        controller.SetCanCatch(true);


        // 현재 Scene의 GameObject 기록
        _spawnedInstances.Add(
            data.id,
            marker);


        // Persistent Spawn 상태 기록
        StateManager.AddSpawnedDalsu(
            data.id);
    }


    private void Despawn(
        DalsuData data)
    {
        if (!_spawnedInstances.TryGetValue(
                data.id,
                out GameObject instance))
        {
            return;
        }


        Destroy(instance);

        _spawnedInstances.Remove(
            data.id);


        // 현재 Spawn 상태 제거
        StateManager.RemoveSpawnedDalsu(
            data.id);


        Debug.Log(
            $"[DalsuSpawner] " +
            $"Dalsu Despawn : {data.dalsuName}");
    }


    public void RefreshSpawnedPositions()
    {
        var dalsuDatas =
            _dalsuDatabase.GetDalsuDatas();


        foreach (DalsuData data in dalsuDatas)
        {
            if (!_spawnedInstances.TryGetValue(
                    data.id,
                    out GameObject instance))
            {
                continue;
            }


            LatitudeLongitude location =
                new LatitudeLongitude(
                    data.latitude,
                    data.longitude);


            Vector3 localPosition =
                _mapBehaviour.MapboxMap
                    .MapInformation
                    .ConvertLatLngToPosition(
                        location);


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
            Random.Range(
                0f,
                100f);


        return roll < probability;
    }
}