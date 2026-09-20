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

    private ILocationProvider _locationProvider;
    private Location _lastLocation;
    private bool _hasLocation;

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
            UpdatePlayerPosition(_lastLocation);
        }
    }

    private void UpdatePlayerPosition(Location location)
    {
        Vector3 localPosition =
            Conversions.LatitudeLongitudeToWorldPosition(
                location.LatitudeLongitude,
                _mapBehaviour.MapboxMap.MapInformation.CenterMercator,
                _mapBehaviour.MapboxMap.MapInformation.Scale
            );

        _playerArrow.SetParent(
            _mapBehaviour.MapboxMap.UnityContext.MapRoot,
            false
        );

        _playerArrow.localPosition = localPosition;

        // Debug.Log($"Player Position = {localPosition}");
    }
}