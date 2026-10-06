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

    [Header("Dalsu Data")]
    [SerializeField]
    private DalsuData[] _dalsuDatas;

    [Header("Spawn")]
    [SerializeField]
    private Transform _spawnRoot;
    [SerializeField]
    private GameObject _dalsuMarker;
    [SerializeField]
    private float _despawnExtraDistance = 10f;

    [Header("Catch")]
    [SerializeField]
    private float _catchRadius = 10f;

    [Header("Rarity Probability")]
    [SerializeField]
    private float _rarity1Probability = 80f;

    [SerializeField]
    private float _rarity2Probability = 40f;

    [SerializeField]
    private float _rarity3Probability = 10f;


    private readonly Dictionary<string, DalsuData> _dalsuDataById = new();
    private readonly Dictionary<string, GameObject> _spawnedDalsu = new();
    private readonly HashSet<string> _caughtDalsu = new();
    private readonly HashSet<string> _spawnAreaEntered = new();

    private LatitudeLongitude _currentLocation;
    private bool _hasLocation;

    private void Awake()
    {
        foreach (DalsuData data in _dalsuDatas)
        {
            if (data == null)
                continue;

            if (_dalsuDataById.ContainsKey(data.id))
            {
                Debug.LogWarning(
                    $"[DalsuSpawner] 중복된 Dalsu ID가 있습니다. id = {data.id}"
                );

                continue;
            }

            _dalsuDataById.Add(data.id, data);
        }
    }

    public void UpdateLocation(LatitudeLongitude location)
    {
        //Debug.Log(
        //$"[DalsuSpawner] 위치 업데이트 / " +
        //$"Lat = {location.Latitude}, " +
        //$"Lon = {location.Longitude}"
        //);

        _currentLocation = location;
        _hasLocation = true;

        CheckDalsuSpawn();
    }

    public void OnDalsuCaught(string id)
    {
        if (!_spawnedDalsu.TryGetValue(
                id,
                out GameObject instance))
        {
            Debug.LogWarning(
                $"[DalsuSpawner] 잡힌 Dalsu를 찾을 수 없습니다. id = {id}"
            );

            return;
        }

        if (!_dalsuDataById.TryGetValue(
                id,
                out DalsuData caughtDalsu))
        {
            Debug.LogError(
                $"[DalsuSpawner] DalsuData를 찾을 수 없습니다. id = {id}"
            );

            return;
        }

        _spawnedDalsu.Remove(id);
        _caughtDalsu.Add(id);

        Destroy(instance);

        // TODO: AR 화면으로 이동
        DalsuSceneContext.SelectedDalsuId = caughtDalsu.id;
        SceneManager.LoadScene("AR");
    }

    private void CheckDalsuSpawn()
    {
        Debug.Log("[DalsuSpawner] CheckDalsuSpawn 시작");

        if (!_hasLocation)
        {
            Debug.LogWarning("[DalsuSpawner] 위치가 없습니다.");
            return;
        }

        if (_mapBehaviour == null)
        {
            Debug.LogError("[DalsuSpawner] MapBehaviour가 연결되지 않았습니다.");
            return;
        }

        if (_mapBehaviour.MapboxMap == null)
        {
            Debug.LogError("[DalsuSpawner] MapboxMap이 null입니다.");
            return;
        }

        if (_dalsuDatas == null)
        {
            Debug.LogError("[DalsuSpawner] DalsuData 배열이 null입니다.");
            return;
        }

        foreach (DalsuData data in _dalsuDatas)
        {
            if (data == null)
            {
                Debug.LogWarning("[DalsuSpawner] null인 DalsuData가 있습니다.");
                continue;
            }

            if (_caughtDalsu.Contains(data.id))
            {
                continue;
            }

            LatitudeLongitude dalsuLocation =
                new LatitudeLongitude(
                    data.latitude,
                    data.longitude
                );

            float distance = CalculateDistance(
                _currentLocation,
                dalsuLocation
            );

            //Debug.Log(
            //    $"[DalsuSpawner] {data.dalsuName} / " +
            //    $"거리 = {distance:F1}m / " +
            //    $"SpawnRadius = {data.spawnRadius}m"
            //);

            // 현재 Spawn 가능 구역 안에 있는 경우
            if (distance <= data.spawnRadius)
            {
                // 이미 이 구역에서 확률 판정을 했다면
                // 다시 판정하지 않는다.
                if (_spawnAreaEntered.Contains(data.id))
                {
                    if (_spawnedDalsu.ContainsKey(data.id))
                    {
                        UpdateCatchState(data.id, distance);
                    }

                    continue;
                }

                // Spawn 가능 구역 최초 진입
                _spawnAreaEntered.Add(data.id);

                if (!_spawnedDalsu.ContainsKey(data.id))
                {
                    if (RollSpawnProbability(data.rarity))
                    {
                        Debug.Log(
                            $"[DalsuSpawner] Spawn 성공! " +
                            $"{data.dalsuName} / rarity = {data.rarity}"
                        );

                        Spawn(data, dalsuLocation);
                    }
                    else
                    {
                        Debug.Log(
                            $"[DalsuSpawner] Spawn 실패! " +
                            $"{data.dalsuName} / rarity = {data.rarity}"
                        );
                    }
                }

                continue;
            }

            // Spawn 가능 구역을 완전히 벗어난 경우
            if (distance > data.spawnRadius + _despawnExtraDistance)
            {
                // 다시 들어왔을 때 확률을 새로 굴릴 수 있도록 초기화
                _spawnAreaEntered.Remove(data.id);

                // 현재 Spawn되어 있다면 Despawn
                if (_spawnedDalsu.ContainsKey(data.id))
                {
                    Despawn(data);
                }
            }
            else
            {
                // Spawn되어 있는 Dalsu라면 잡기 가능 여부만 갱신
                if (_spawnedDalsu.ContainsKey(data.id))
                {
                    UpdateCatchState(data.id, distance);
                }
            }
        }
    }

    private void Spawn(
    DalsuData data,
    LatitudeLongitude location)
    {
        if (_spawnedDalsu.ContainsKey(data.id))
            return;

        Vector3 localPosition =
            _mapBehaviour.MapboxMap.MapInformation
                .ConvertLatLngToPosition(location);

        // DalsuMarker 생성
        GameObject marker = Instantiate(
            _dalsuMarker,
            _mapBehaviour.MapboxMap.UnityContext.MapRoot,
            false
        );

        marker.transform.localPosition = localPosition;

        // DalsuMarker의 자식으로 실제 Dalsu 생성
        GameObject dalsu = Instantiate(
            data.prefab,
            marker.transform,
            false
        );

        dalsu.transform.localPosition = Vector3.zero;
        dalsu.transform.localRotation =
        Quaternion.Euler(
            0f,
            Random.Range(0f, 360f),
            0f
        );
        dalsu.transform.localScale = Vector3.one;

        DalsuController controller =
            marker.GetComponent<DalsuController>();

        if (controller == null)
        {
            Debug.LogError(
                $"[DalsuSpawner] DalsuMarker에 " +
                $"DalsuController가 없습니다. {marker.name}"
            );

            Destroy(marker);
            return;
        }

        controller.Initialize(
            data.id,
            this
        );

        controller.SetCanCatch(
            CalculateDistance(
                _currentLocation,
                location
            ) <= _catchRadius
        );

        _spawnedDalsu.Add(data.id, marker);
    }

    private void Despawn(DalsuData data)
    {
        if (!_spawnedDalsu.TryGetValue(
                data.id,
                out GameObject instance))
        {
            return;
        }

        Destroy(instance);
        _spawnedDalsu.Remove(data.id);

        Debug.Log($"Dalsu Despawn : {data.dalsuName}");
    }

    public void RefreshSpawnedPositions()
    {
        foreach (DalsuData data in _dalsuDatas)
        {
            if (!_spawnedDalsu.TryGetValue(
                    data.id,
                    out GameObject instance))
            {
                continue;
            }

            LatitudeLongitude location =
                new LatitudeLongitude(
                    data.latitude,
                    data.longitude
                );

            Vector3 localPosition =
                _mapBehaviour.MapboxMap.MapInformation
                    .ConvertLatLngToPosition(location);

            instance.transform.localPosition = localPosition;
        }
    }

    private void UpdateCatchState(
    string id,
    float distance)
    {
        if (!_spawnedDalsu.TryGetValue(
                id,
                out GameObject instance))
        {
            return;
        }

        DalsuController controller =
            instance.GetComponent<DalsuController>();

        if (controller == null)
        {
            Debug.LogError(
                $"[DalsuSpawner] DalsuController가 없습니다. " +
                $"{instance.name}"
            );

            return;
        }

        bool canCatch = distance <= _catchRadius;

        Debug.Log(
            $"[DalsuSpawner] Catch 상태 업데이트 / " +
            $"id = {id} / " +
            $"distance = {distance:F1}m / " +
            $"catchRadius = {_catchRadius:F1}m / " +
            $"canCatch = {canCatch}"
        );

        controller.SetCanCatch(canCatch);
    }

    private float CalculateDistance(
        LatitudeLongitude a,
        LatitudeLongitude b)
    {
        const float EarthRadius = 6371000f;

        float lat1 =
            Mathf.Deg2Rad * (float)a.Latitude;

        float lat2 =
            Mathf.Deg2Rad * (float)b.Latitude;

        float deltaLat =
            Mathf.Deg2Rad *
            (float)(b.Latitude - a.Latitude);

        float deltaLon =
            Mathf.Deg2Rad *
            (float)(b.Longitude - a.Longitude);

        float sinLat =
            Mathf.Sin(deltaLat / 2f);

        float sinLon =
            Mathf.Sin(deltaLon / 2f);

        float h =
            sinLat * sinLat +
            Mathf.Cos(lat1) *
            Mathf.Cos(lat2) *
            sinLon * sinLon;

        return
            2f *
            EarthRadius *
            Mathf.Asin(Mathf.Sqrt(h));
    }

    private bool RollSpawnProbability(int rarity)
    {
        float probability = rarity switch
        {
            1 => _rarity1Probability,
            2 => _rarity2Probability,
            3 => _rarity3Probability,
            _ => 0f
        };

        float roll = Random.Range(0f, 100f);

        //Debug.Log(
        //    $"[DalsuSpawner] 확률 판정 / " +
        //    $"rarity = {rarity} / " +
        //    $"probability = {probability}% / " +
        //    $"roll = {roll:F1}"
        //);

        return roll < probability;
    }
}