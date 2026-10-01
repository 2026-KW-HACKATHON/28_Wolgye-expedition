using Mapbox.BaseModule.Data.Vector2d;
using Mapbox.BaseModule.Map;
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

    [Header("Debug")]
    [SerializeField]
    private TMPro.TextMeshProUGUI _debugText;

    [Header("Dalsu")]
    [SerializeField]
    private DalsuSpawner _dalsuSpawner;

    private ILocationProvider _locationProvider;

    private LatitudeLongitude _lastMapLocation;
    private bool _hasMapLocation;

    private int _gpsUpdateCount;

    private void Start()
    {
        if (_locationProviderFactory == null)
        {
            Debug.LogError(
                "PlayerLocation: LocationProviderFactory가 연결되지 않았습니다."
            );

            return;
        }

        if (_mapBehaviour == null)
        {
            Debug.LogError(
                "PlayerLocation: MapboxMapBehaviour가 연결되지 않았습니다."
            );

            return;
        }

        if (_playerArrow == null)
        {
            Debug.LogError(
                "PlayerLocation: Player가 연결되지 않았습니다."
            );

            return;
        }

        StartCoroutine(InitializeLocation());
    }

    private IEnumerator InitializeLocation()
    {
        // LocationProvider 준비 대기
        yield return new WaitUntil(() =>
            _locationProviderFactory.IsLocationProviderReady
        );

        _locationProvider =
            _locationProviderFactory.DefaultLocationProvider;

        if (_locationProvider == null)
        {
            Debug.LogError(
                "PlayerLocation: LocationProvider가 없습니다."
            );

            yield break;
        }

        // MapboxMap 준비 대기
        yield return new WaitUntil(() =>
            _mapBehaviour.MapboxMap != null
        );

        // Player를 MapRoot에 배치
        SetupPlayer();

        // 위치 이벤트 연결
        _locationProvider.OnLocationUpdated -= OnLocationUpdated;
        _locationProvider.OnLocationUpdated += OnLocationUpdated;

        Debug.Log(
            $"PlayerLocation: Provider 연결 완료 - " +
            $"{_locationProvider.GetType().Name}"
        );

        // 이미 받아온 현재 위치 처리
        Location currentLocation =
            _locationProvider.CurrentLocation;

        OnLocationUpdated(currentLocation);
    }

    private void OnDestroy()
    {
        if (_locationProvider != null)
        {
            _locationProvider.OnLocationUpdated -= OnLocationUpdated;
        }
    }

    private void OnLocationUpdated(Location location)
    {
        _gpsUpdateCount++;

        LatitudeLongitude currentLocation =
            location.LatitudeLongitude;

        UpdateDebugText(currentLocation);

        // Mapbox가 아직 준비되지 않았으면 종료
        if (_mapBehaviour.MapboxMap == null)
            return;

        // 최초 위치
        if (!_hasMapLocation)
        {
            _lastMapLocation = currentLocation;
            _hasMapLocation = true;

            // 최초 위치로 지도 중심 설정
            _mapBehaviour.MapboxMap.ChangeView(
                currentLocation
            );

            // Player는 항상 지도 중앙
            UpdatePlayerPosition();

            // Dalsu 갱신
            UpdateDalsuLocation(currentLocation);

            return;
        }

        // 마지막으로 지도를 이동시킨 위치와의 거리
        float distance = CalculateDistance(
            _lastMapLocation,
            currentLocation
        );

        // 일정 거리 이상 이동했으면 지도 이동
        if (distance >= _moveThreshold)
        {
            _mapBehaviour.MapboxMap.ChangeView(
                currentLocation
            );

            _lastMapLocation = currentLocation;
        }

        // Player는 항상 중앙
        UpdatePlayerPosition();

        // Dalsu 갱신
        UpdateDalsuLocation(currentLocation);
    }

    private void SetupPlayer()
    {
        Transform mapRoot =
            _mapBehaviour.MapboxMap.UnityContext.MapRoot;

        if (_playerArrow.parent != mapRoot)
        {
            _playerArrow.SetParent(
                mapRoot,
                false
            );
        }

        _playerArrow.localPosition = Vector3.zero;
    }

    private void UpdatePlayerPosition()
    {
        if (_mapBehaviour.MapboxMap == null)
            return;

        if (_playerArrow == null)
            return;

        Transform mapRoot =
            _mapBehaviour.MapboxMap.UnityContext.MapRoot;

        if (_playerArrow.parent != mapRoot)
        {
            _playerArrow.SetParent(
                mapRoot,
                false
            );
        }

        // Player는 항상 지도 중심에 고정
        _playerArrow.localPosition = Vector3.zero;
    }

    private void UpdateDalsuLocation(
        LatitudeLongitude location)
    {
        _dalsuSpawner?.UpdateLocation(location);
    }

    private void UpdateDebugText(
        LatitudeLongitude location)
    {
        if (_debugText == null)
            return;

        _debugText.text =
            $"GPS Count : {_gpsUpdateCount}\n" +
            $"Lat : {location.Latitude:F7}\n" +
            $"Lon : {location.Longitude:F7}";
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
            Mathf.Asin(
                Mathf.Sqrt(h)
            );
    }
}