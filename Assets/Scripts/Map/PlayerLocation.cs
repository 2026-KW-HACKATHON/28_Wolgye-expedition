using Mapbox.BaseModule.Data.Vector2d;
using Mapbox.BaseModule.Map;
using Mapbox.BaseModule.Utilities;
using Mapbox.Example.Scripts.Map;
using Mapbox.LocationModule;
using System.Collections;
using UnityEngine;

public class PlayerLocation : MonoBehaviour
{
    [Header("Map")]
    [SerializeField]
    private MapboxMapBehaviour _mapBehaviour;

    [Header("Location")]
    [SerializeField]
    private LocationProviderFactory _locationProviderFactory;

    [Header("Player")]
    [SerializeField]
    private Transform _playerArrow;
    [SerializeField]
    private float _moveThreshold = 3f;

    [Header("Dalsu")]
    [SerializeField]
    private DalsuSpawner _dalsuSpawner;

    private ILocationProvider _locationProvider;

    private Location _lastLocation;
    private bool _hasLocation;

    private LatitudeLongitude _lastMapLocation;
    private bool _hasMapLocation;

    private void Start()
    {
        if (_locationProviderFactory == null)
        {
            Debug.LogError("LocationProviderFactory가 연결되지 않았습니다.");
            return;
        }

        if (_mapBehaviour == null)
        {
            Debug.LogError("MapboxMapBehaviour가 연결되지 않았습니다.");
            return;
        }

        if (_playerArrow == null)
        {
            Debug.LogError("PlayerArrow가 연결되지 않았습니다.");
            return;
        }

        _locationProviderFactory.OnLocationProviderReady += OnLocationProviderReady;

        StartCoroutine(WaitForMap());
    }

    private void OnDestroy()
    {
        if (_locationProviderFactory != null)
        {
            _locationProviderFactory.OnLocationProviderReady -= OnLocationProviderReady;
        }

        if (_locationProvider != null)
        {
            _locationProvider.OnLocationUpdated -= OnLocationUpdated;
        }
    }

    private void OnLocationProviderReady(LocationProviderFactory factory)
    {
        _locationProvider = factory.DefaultLocationProvider;

        if (_locationProvider == null)
        {
            Debug.LogError("LocationProvider가 없습니다.");
            return;
        }

        _locationProvider.OnLocationUpdated += OnLocationUpdated;

        Debug.Log(
            $"PlayerLocation: Location Provider 연결 완료 / " +
            $"Type = {_locationProvider.GetType().Name}"
        );

        Location currentLocation = _locationProvider.CurrentLocation;

        Debug.Log(
            $"현재 위치 : " +
            $"Lat = {currentLocation.LatitudeLongitude.Latitude}, " +
            $"Lon = {currentLocation.LatitudeLongitude.Longitude}"
        );

        OnLocationUpdated(currentLocation);
    }

    private void OnLocationUpdated(Location location)
    {
        _lastLocation = location;
        _hasLocation = true;

        //Debug.Log(
        //    $"Player GPS : " +
        //    $"Lat = {location.LatitudeLongitude.Latitude}, " +
        //    $"Lon = {location.LatitudeLongitude.Longitude}"
        //);

        if (_mapBehaviour.MapboxMap == null)
            return;

        LatitudeLongitude currentLocation =
        location.LatitudeLongitude;

        // 최초 위치
        if (!_hasMapLocation)
        {
            _mapBehaviour.MapboxMap.LoadMapView(
                currentLocation,
                () =>
                {
                    UpdatePlayerPosition(location);
                }
            );

            _lastMapLocation = currentLocation;
            _hasMapLocation = true;

            return;
        }

        // 마지막으로 지도를 이동시킨 위치와 현재 GPS 위치의 거리
        float distance = CalculateDistance(
            _lastMapLocation,
            currentLocation
        );

        // 3m 미만이면 지도 이동하지 않음
        if (distance < _moveThreshold)
            return;

        // 3m 이상 이동했으면 지도 이동
        _mapBehaviour.MapboxMap.LoadMapView(
            currentLocation,
            () =>
            {
                UpdatePlayerPosition(location);
            }
        );

        _lastMapLocation = currentLocation;
    }

    private IEnumerator WaitForMap()
    {
        Debug.Log("MapboxMap 초기화를 기다리는 중...");

        yield return new WaitUntil(() =>
            _mapBehaviour.MapboxMap != null
        );

        Debug.Log("MapboxMap 초기화 완료!");

        if (_hasLocation)
        {
            OnLocationUpdated(_lastLocation);
        }
    }

    private void UpdatePlayerPosition(Location location)
    {
        Vector3 localPosition =
            _mapBehaviour.MapboxMap.MapInformation
                .ConvertLatLngToPosition(
                    location.LatitudeLongitude
                );

        _playerArrow.SetParent(
            _mapBehaviour.MapboxMap.UnityContext.MapRoot,
            false
        );

        _playerArrow.localPosition = localPosition;

        _dalsuSpawner?.UpdateLocation(
            location.LatitudeLongitude
        );
    }



    private float CalculateDistance(
    LatitudeLongitude a,
    LatitudeLongitude b)
    {
        const float EarthRadius = 6371000f;

        float lat1 = Mathf.Deg2Rad * (float)a.Latitude;
        float lat2 = Mathf.Deg2Rad * (float)b.Latitude;

        float deltaLat =
            Mathf.Deg2Rad * (float)(b.Latitude - a.Latitude);

        float deltaLon =
            Mathf.Deg2Rad * (float)(b.Longitude - a.Longitude);

        float sinLat = Mathf.Sin(deltaLat / 2f);
        float sinLon = Mathf.Sin(deltaLon / 2f);

        float h =
            sinLat * sinLat +
            Mathf.Cos(lat1) *
            Mathf.Cos(lat2) *
            sinLon * sinLon;

        float distance =
            2f * EarthRadius * Mathf.Asin(Mathf.Sqrt(h));

        return distance;
    }
}