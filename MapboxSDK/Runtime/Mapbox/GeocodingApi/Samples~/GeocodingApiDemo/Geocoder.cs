using System.Collections;
using System.Collections.Generic;
using Mapbox.BaseModule.Data.Platform;
using Mapbox.BaseModule.Data.Vector2d;
using Mapbox.BaseModule.Map;
using Mapbox.Example.Scripts.Map;
using Mapbox.GeocodingApi;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class Geocoder : MonoBehaviour
{
    public MapboxMapBehaviour MapCore;
    public GameObject Label;
    public GameObject ForwardLabel;

    private GameObject startMarker;
    private GameObject finishMarker;
    private Material _material;
    private Camera _camera;
    private IFileSource _fileSource;
    private MapboxGeocodingApi _mapboxGeocoder;
    private IMapInformation _mapInformation;

    private Vector3 FirstPoint;
    private LatitudeLongitude FirstLatLng;
    private Vector3 SecondPoint;
    private LatitudeLongitude SecondLatLng;
    private string _currentValue;

    public void Start()
    {
        startMarker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        startMarker.transform.parent = transform;
        startMarker.transform.localScale = Vector3.one * 0.2f;
        startMarker.SetActive(false);
        var mat1 = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        mat1.color = new Color(134f / 255, 255f / 255, 121f / 255, 1);
        startMarker.GetComponent<MeshRenderer>().material = mat1;

        finishMarker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        finishMarker.transform.parent = transform;
        finishMarker.transform.localScale = Vector3.one * 0.2f;
        finishMarker.SetActive(false);
        var mat2 = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        mat2.color = new Color(85f / 255, 217f / 255, 248f / 255, 1);
        finishMarker.GetComponent<MeshRenderer>().material = mat2;


        _material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        _material.color = new Color(1, 132f / 255, 0, 1);
        _camera = Camera.main;

        MapCore.Initialized += map =>
        {
            if (_fileSource == null)
                _fileSource = map.MapService.FileSource;
            _mapboxGeocoder = new MapboxGeocodingApi(_fileSource);
            _mapInformation = map.MapInformation;
        };
    }

    public void Update()
    {
        if (_mapboxGeocoder == null) return;

        Label.transform.position = _camera.WorldToScreenPoint(FirstPoint);

        if (Input.GetKey(KeyCode.LeftCommand) && Input.GetMouseButtonDown(0))
        {
            FirstPoint = GetPlaneIntersection(UnityEngine.Input.mousePosition);
            FirstLatLng = _mapInformation.ConvertPositionToLatLng(FirstPoint);
            Label.SetActive(false);
            ForwardLabel.SetActive(false);
            
            _mapboxGeocoder.Geocode(new ReverseGeocodeResource(FirstLatLng), (ReverseGeocodeResponse response) =>
            {
                _currentValue = response.Features[0].PlaceName;
                Label.GetComponentInChildren<Text>().text = _currentValue;
                Label.SetActive(true);
            });
        }
    }
    
    private Vector3 GetPlaneIntersection(Vector3 screenPosition)
    {
        var ray = _camera.ScreenPointToRay(screenPosition);
        var dirNorm = ray.direction / ray.direction.y;
        var intersectionPos = ray.origin - dirNorm * ray.origin.y;
        return intersectionPos;
    }

    public void ForwardGeocode()
    {
        _mapboxGeocoder.Geocode(new ForwardGeocodeResource(_currentValue), (ForwardGeocodeResponse response) =>
        {
            ForwardLabel.SetActive(true);
            Label.GetComponentInChildren<Text>().text = response.Features[0].Center.ToString();
            Label.SetActive(true);
        });
    }
}
