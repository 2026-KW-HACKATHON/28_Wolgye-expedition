using System;
using System.Collections;
using Mapbox.BaseModule.Map;
using Mapbox.BaseModule.Utilities;
using UnityEngine;

namespace Mapbox.Example.Scripts.Map
{
    public class LoadingScreen : MonoBehaviour
    {
        public MapBehaviourCore MapBehaviourCore;
        private CanvasGroup _canvasGroup;
        private MapboxMap _map;

        private void Start()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            MapBehaviourCore.Initialized += map =>
            {
                _map = map;
                InitializeScreen(_map);
            };
            
        }

        private void InitializeScreen(MapboxMap map)
        {
            map.LoadViewCompleted += OnMapFirstViewCompleted;
        }

        private void OnMapFirstViewCompleted()
        {
            StartCoroutine(FadeAway());
        }

        private IEnumerator FadeAway()
        {
            while (_canvasGroup.alpha > 0)
            {
                _canvasGroup.alpha -= Time.deltaTime;
                yield return null;
            }
            gameObject.SetActive(false);
            _map.LoadViewCompleted -= OnMapFirstViewCompleted;
        }

        private void OnDestroy()
        {
            if(MapBehaviourCore)
                MapBehaviourCore.Initialized -= InitializeScreen;
            if(_map != null)
                _map.LoadViewCompleted -= OnMapFirstViewCompleted;
        }
    }
}
