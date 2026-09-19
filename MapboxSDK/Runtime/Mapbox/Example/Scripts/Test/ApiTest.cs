using System.Collections;
using Mapbox.BaseModule.Map;
using Mapbox.BaseModule.Utilities;
using Mapbox.Example.Scripts.Map;
using UnityEngine;

namespace Mapbox.Example.Scripts.Test
{
    public class ApiTest : MonoBehaviour
    {
        public string LatLng1;
        public string LatLng2;

        private MapboxMap _map;

        public void SetLatLng1()
        {
            _map = FindObjectOfType<MapboxMapBehaviour>().MapboxMap;
            _map.MapInformation.SetInformation(Conversions.StringToLatLon(LatLng1));
        }
    
        public void SetLatLng2()
        {
            _map = FindObjectOfType<MapboxMapBehaviour>().MapboxMap;
            _map.MapInformation.SetInformation(Conversions.StringToLatLon(LatLng2));
        }

        public void SetZoomRandom()
        {
            _map = FindObjectOfType<MapboxMapBehaviour>().MapboxMap;
            _map.MapInformation.SetInformation(null, (Random.value * 15) + 1);
        }

        private Coroutine clickToLatLngCoroutine;
        public void ToggleClickToLatLon()
        {
            if (clickToLatLngCoroutine != null)
            {
                StopCoroutine(clickToLatLngCoroutine);
                clickToLatLngCoroutine = null;
                return;
            }
            else
            {
                clickToLatLngCoroutine = StartCoroutine(clickToLatLng());    
            }
        
            IEnumerator clickToLatLng()
            {
                _map = FindObjectOfType<MapboxMapBehaviour>().MapboxMap;
                var camera = Camera.main;
                while (true)
                {
                    if (UnityEngine.Input.GetMouseButtonDown(0))
                    {
                        var latlng = _map.MapInformation.ConvertPositionToLatLng(GetPlaneIntersection(camera, UnityEngine.Input.mousePosition));
                        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                        go.name = latlng.ToString();
                        go.transform.position = _map.MapInformation.ConvertLatLngToPosition(latlng);
                    }

                    yield return null;
                }
            }
        
            Vector3 GetPlaneIntersection(Camera cam, Vector3 screenPosition)
            {
                var ray = cam.ScreenPointToRay(screenPosition);
                var dirNorm = ray.direction / ray.direction.y;
                var intersectionPos = ray.origin - dirNorm * ray.origin.y;
                return intersectionPos;
            }
        }
    }
}
