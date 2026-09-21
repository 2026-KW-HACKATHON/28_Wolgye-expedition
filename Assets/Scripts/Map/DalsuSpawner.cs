using System.Collections.Generic;
using Mapbox.BaseModule.Data.Vector2d;
using Mapbox.BaseModule.Map;
using Mapbox.BaseModule.Utilities;
using Mapbox.Example.Scripts.Map;
using UnityEngine;

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

    private readonly Dictionary<string, GameObject> _spawnedDalsu = new();

    private LatitudeLongitude _currentLocation;
    private bool _hasLocation;

    public void UpdateLocation(LatitudeLongitude location)
    {
        Debug.Log(
        $"[DalsuSpawner] 위치 업데이트 / " +
        $"Lat = {location.Latitude}, " +
        $"Lon = {location.Longitude}"
    );

        _currentLocation = location;
        _hasLocation = true;

        CheckDalsuSpawn();
        RefreshSpawnedPositions();
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

        Debug.Log(
            $"[DalsuSpawner] DalsuData 개수 = {_dalsuDatas.Length}"
        );

        foreach (DalsuData data in _dalsuDatas)
        {
            if (data == null)
            {
                Debug.LogWarning("[DalsuSpawner] null인 DalsuData가 있습니다.");
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

            Debug.Log(
                $"[DalsuSpawner] {data.dalsuName} / " +
                $"거리 = {distance:F1}m / " +
                $"SpawnRadius = {data.spawnRadius}m"
            );

            if (!_spawnedDalsu.ContainsKey(data.id))
            {
                if (distance <= data.spawnRadius)
                {
                    Debug.Log(
                        $"[DalsuSpawner] Spawn 조건 만족! " +
                        $"{data.dalsuName}"
                    );

                    Spawn(data, dalsuLocation);
                }
            }
            else
            {
                if (distance > data.spawnRadius + _despawnExtraDistance)
                {
                    Despawn(data);
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

        //Debug.Log(
        //    $"[DalsuSpawner] Spawn 좌표 확인\n" +
        //    $"GPS Current : {_currentLocation.Latitude}, {_currentLocation.Longitude}\n" +
        //    $"GPS Dalsu   : {location.Latitude}, {location.Longitude}\n" +
        //    $"Distance    : {CalculateDistance(_currentLocation, location):F3}m\n" +
        //    $"Local Pos   : {localPosition:F5}\n" +
        //    $"Map Scale   : {_mapBehaviour.MapboxMap.MapInformation.Scale}\n" +
        //    $"Map Center  : {_mapBehaviour.MapboxMap.MapInformation.CenterMercator}"
        //);

        GameObject instance = Instantiate(
            _dalsuMarker,
            _mapBehaviour.MapboxMap.UnityContext.MapRoot,
            false
        );

        instance.transform.localPosition = localPosition;

        _spawnedDalsu.Add(data.id, instance);
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

    private void RefreshSpawnedPositions()
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

}