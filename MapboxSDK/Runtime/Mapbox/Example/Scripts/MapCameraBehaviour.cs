using Mapbox.BaseModule.Map;
using Mapbox.BaseModule.Utilities;
using Mapbox.Example.Scripts.Map;
using UnityEngine;

namespace Mapbox.Example.Scripts.MapInput
{
    /// <summary>
    /// Generic base MonoBehaviour for any MapInput-based camera.
    /// Handles initialization, the Update loop, and ChangeView dispatch.
    /// Subclass with a concrete camera type, or use directly if no extra behaviour is needed.
    /// </summary>
    public abstract class MapCameraBehaviour<T> : MonoBehaviour where T : MapInput
    {
        [Tooltip("Reference to the map behaviour that provides map state and tile management")]
        public MapBehaviourCore MapBehaviour;
        [Tooltip("Camera used for map rendering and input raycasting. Falls back to Camera.main if not set")]
        public Camera Camera;

        protected MapboxMap Map;
        protected bool IsInitialized;

        public abstract T Core { get; }

        protected virtual void Awake()
        {
            if (MapBehaviour == null) MapBehaviour = FindObjectOfType<MapBehaviourCore>();
            if (Camera == null) Camera = Camera.main;

            if (MapBehaviour == null)
            {
                Debug.LogError($"{GetType().Name} could not find a MapBehaviourCore in the scene. Assign the MapBehaviour field or add a Mapbox map to the scene; the camera will not initialize.", this);
                enabled = false;
                return;
            }

            MapBehaviour.Initialized += OnMapInitialized;
        }

        protected virtual void OnDestroy()
        {
            if (MapBehaviour != null)
                MapBehaviour.Initialized -= OnMapInitialized;

            // Tear down the camera core's IMapInformation subscriptions. MapInformation
            // can outlive the behaviour (DontDestroyOnLoad / scene reload); without the
            // detach, the camera core and its captured _camera Transform stay rooted.
            if (IsInitialized && Map != null)
            {
                Core?.Teardown(Map.MapInformation);
            }
        }

        protected virtual void OnMapInitialized(MapboxMap map)
        {
            // Defensive: if MapBehaviour.Initialized fires twice (re-init, swapped map
            // asset), unsubscribe from the previous MapInformation before binding to
            // the new one. Otherwise the prior subscriptions leak until OnDestroy.
            if (IsInitialized && Map != null && Map.MapInformation != null)
            {
                Core?.Teardown(Map.MapInformation);
            }

            Map = map;
            IsInitialized = true;
            Core.Initialize(Camera, map.MapInformation);
        }

        protected virtual void Update()
        {
            if (!IsInitialized || Map.MapInformation == null)
                return;

            var output = Core.UpdateCamera(Map.MapInformation);
            if (output.HasChanged)
                Map.ChangeView(output.Center, output.Zoom, output.Pitch, output.Bearing, output.Scale);
        }
    }
}
