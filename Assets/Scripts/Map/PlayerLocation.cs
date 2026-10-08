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
    private Transform _playerCharacter;

    [SerializeField]
    private Animator _playerAnimator;

    [SerializeField]
    private float _moveThreshold = 3f;

    [SerializeField]
    private float _moveAnimationDuration = 1f;

    [Header("Dalsu")]
    [SerializeField]
    private DalsuSpawner _dalsuSpawner;


    private ILocationProvider _locationProvider;

    private LatitudeLongitude _lastMapLocation;
    private bool _hasMapLocation;

    private int _gpsUpdateCount;

    private Coroutine _mapMoveCoroutine;
    private Coroutine _moveAnimationCoroutine;


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

        if (_playerCharacter == null)
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
        SceneLoader.NotifySceneReady(); // 여기서 로딩 완료

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

        // 일정 거리 이상 이동했으면 지도 이동 및 애니메이션 처리
        if (distance >= _moveThreshold)
        {
            UpdatePlayerRotation(
                _lastMapLocation,
                currentLocation,
                _mapBehaviour.MapboxMap.MapInformation.Bearing
            );

            // 이전 이동이 아직 진행 중이라면 중단
            if (_mapMoveCoroutine != null)
            {
                StopCoroutine(_mapMoveCoroutine);
            }

            // 이전 위치 → 현재 GPS 위치까지 보간 이동
            _mapMoveCoroutine =
                StartCoroutine(
                    MoveMapSmoothly(
                        _lastMapLocation,
                        currentLocation
                    )
                );

            _lastMapLocation = currentLocation;

            // 이동하는 동안 Walk
            SetPlayerMoving();

            UpdatePlayerPosition();
        }

        // Dalsu 갱신
        UpdateDalsuLocation(currentLocation);
    }

    private void SetupPlayer()
    {
        Transform mapRoot =
            _mapBehaviour.MapboxMap.UnityContext.MapRoot;

        if (_playerCharacter.parent != mapRoot)
        {
            _playerCharacter.SetParent(
                mapRoot,
                false
            );
        }

        _playerCharacter.localPosition = Vector3.zero;
    }

    private void UpdatePlayerPosition()
    {
        if (_mapBehaviour.MapboxMap == null)
            return;

        if (_playerCharacter == null)
            return;

        Transform mapRoot =
            _mapBehaviour.MapboxMap.UnityContext.MapRoot;

        if (_playerCharacter.parent != mapRoot)
        {
            _playerCharacter.SetParent(
                mapRoot,
                false
            );
        }

        // Player는 항상 지도 중심에 고정
        _playerCharacter.localPosition = Vector3.zero;
    }

    private void UpdateDalsuLocation(
        LatitudeLongitude location)
    {
        _dalsuSpawner?.UpdateLocation(location);
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

    private void SetPlayerMoving()
    {
        if (_playerAnimator == null)
            return;

        _playerAnimator.SetBool("IsMoving", true);

        if (_moveAnimationCoroutine != null)
        {
            StopCoroutine(_moveAnimationCoroutine);
        }

        _moveAnimationCoroutine =
            StartCoroutine(StopMovingAnimation());
    }

    private IEnumerator StopMovingAnimation()
    {
        yield return new WaitForSeconds(
            _moveAnimationDuration
        );

        _playerAnimator.SetBool(
            "IsMoving",
            false
        );

        _moveAnimationCoroutine = null;
    }

    private void UpdatePlayerRotation(
    LatitudeLongitude previous,
    LatitudeLongitude current,
    float mapBearing)
    {
        if (_playerCharacter == null)
            return;

        float movementBearing =
            CalculateBearing(
                previous,
                current
            );

        float screenAngle =
            movementBearing - mapBearing;

        screenAngle =
            Mathf.Repeat(
                screenAngle + 180f,
                360f
            ) - 180f;

        //Debug.Log(
        //    $"[Rotation Test] " +
        //    $"Movement Bearing = {movementBearing:F1}, " +
        //    $"Map Bearing = {mapBearing:F1}, " +
        //    $"Screen Angle = {screenAngle:F1}"
        //);

        Quaternion targetRotation =
            Quaternion.Euler(
                0f,
                screenAngle,
                0f
            );

        _playerCharacter.localRotation =
            targetRotation;
    }

    private float CalculateBearing(
    LatitudeLongitude previous,
    LatitudeLongitude current)
    {
        double lat1 =
            Mathf.Deg2Rad * previous.Latitude;

        double lat2 =
            Mathf.Deg2Rad * current.Latitude;

        double deltaLon =
            Mathf.Deg2Rad *
            (current.Longitude - previous.Longitude);

        double y =
            System.Math.Sin(deltaLon) *
            System.Math.Cos(lat2);

        double x =
            System.Math.Cos(lat1) *
            System.Math.Sin(lat2) -
            System.Math.Sin(lat1) *
            System.Math.Cos(lat2) *
            System.Math.Cos(deltaLon);

        double bearing =
            System.Math.Atan2(y, x) *
            Mathf.Rad2Deg;

        return (float)((bearing + 360.0) % 360.0);
    }

    private IEnumerator MoveMapSmoothly(
    LatitudeLongitude start,
    LatitudeLongitude target)
    {
        float elapsed = 0f;

        while (elapsed < _moveAnimationDuration)
        {
            elapsed += Time.deltaTime;

            float t =
                Mathf.Clamp01(
                    elapsed / _moveAnimationDuration
                );

            LatitudeLongitude current =
                new LatitudeLongitude(
                    Mathf.Lerp(
                        (float)start.Latitude,
                        (float)target.Latitude,
                        t
                    ),
                    Mathf.Lerp(
                        (float)start.Longitude,
                        (float)target.Longitude,
                        t
                    )
                );

            // 지도 이동
            _mapBehaviour.MapboxMap.ChangeView(
                current
            );

            // 현재 지도 기준으로 Dalsu 위치 갱신
            _dalsuSpawner?.RefreshSpawnedPositions();

            yield return null;
        }

        // 마지막에는 정확히 목표 위치
        _mapBehaviour.MapboxMap.ChangeView(
            target
        );

        // 마지막 위치 기준으로 한 번 더 갱신
        _dalsuSpawner?.RefreshSpawnedPositions();

        _mapMoveCoroutine = null;
    }


#if UNITY_EDITOR
    [ContextMenu("Test GPS / North")]
    private void TestGPSNorth()
    {
        TestGPSMove(0.0001, 0.0);
    }

    [ContextMenu("Test GPS / East")]
    private void TestGPSEast()
    {
        TestGPSMove(0.0, 0.0001);
    }

    [ContextMenu("Test GPS / South")]
    private void TestGPSSouth()
    {
        TestGPSMove(-0.0001, 0.0);
    }

    [ContextMenu("Test GPS / West")]
    private void TestGPSWest()
    {
        TestGPSMove(0.0, -0.0001);
    }

    private void TestGPSMove(
        double latitudeOffset,
        double longitudeOffset)
    {
        if (!_hasMapLocation)
        {
            Debug.LogWarning(
                "아직 GPS 초기 위치가 없습니다."
            );

            return;
        }

        LatitudeLongitude currentLocation =
            new LatitudeLongitude(
                _lastMapLocation.Latitude + latitudeOffset,
                _lastMapLocation.Longitude + longitudeOffset
            );

        float distance =
            CalculateDistance(
                _lastMapLocation,
                currentLocation
            );

        Debug.Log(
            $"[Test GPS] " +
            $"Distance = {distance:F2}m"
        );

        if (distance >= _moveThreshold)
        {
            float mapBearing =
                _mapBehaviour.MapboxMap.MapInformation.Bearing;

            UpdatePlayerRotation(
                _lastMapLocation,
                currentLocation,
                mapBearing
            );

            // 이전 이동이 아직 진행 중이라면 중단
            if (_mapMoveCoroutine != null)
            {
                StopCoroutine(_mapMoveCoroutine);
            }

            // 이전 위치 → 현재 테스트 위치까지 보간 이동
            _mapMoveCoroutine =
                StartCoroutine(
                    MoveMapSmoothly(
                        _lastMapLocation,
                        currentLocation
                    )
                );

            _lastMapLocation =
                currentLocation;

            // 이동하는 동안 Walk
            SetPlayerMoving();
        }
    }
#endif

}