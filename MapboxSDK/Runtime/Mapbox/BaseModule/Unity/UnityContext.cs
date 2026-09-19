using System;
using System.Collections;
using Mapbox.BaseModule.Data.Tasks;
using UnityEngine;
using UnityEngine.Android;

namespace Mapbox.BaseModule.Unity
{
    [Serializable]
    public class UnityContext
    {
        public Action LocationPermissionStateChanged;

        public TaskManager TaskManager;
        public LocationPermissionState LocationPermissionState = LocationPermissionState.Waiting;
        [NonSerialized] public MonoBehaviour CoroutineStarter;

        [Tooltip("Root object to hold all map related game objects")]
        public Transform MapRoot;

        [Tooltip("Root object for all tile objects which created the base map")]
        public Transform BaseTileRoot;

        [Tooltip("Root object for all runtime generated visuals. Mainly the vector feature visuals.")]
        public Transform RuntimeGenerationRoot;


        private LocationPermissionHandler _locationPermissionHandler = new();

        public IEnumerator Initialize(TaskManager providedTaskManager = null)
        {
            if (TaskManager == null)
            {
                TaskManager = providedTaskManager ?? new TaskManager();
            }

            TaskManager.Initialize();

            // worldPositionStays: false on the SetParent calls below. Without that flag
            // (default true), Unity preserves world transform by writing to localRotation —
            // so if MapRoot is rotated when Initialize runs, auto-created roots end up
            // with localRotation = inverse(MapRoot.rotation), counter-rotating any
            // children they hold. Explicit localRotation = identity afterwards is
            // defensive in case the scene-authored root has a non-default rotation we
            // want to ignore.
            BaseTileRoot = BaseTileRoot == null ? new GameObject("BaseTiles").transform : BaseTileRoot;
            BaseTileRoot.SetParent(MapRoot, worldPositionStays: false);
            BaseTileRoot.transform.localPosition = Vector3.zero;
            BaseTileRoot.transform.localRotation = Quaternion.identity;

            RuntimeGenerationRoot = RuntimeGenerationRoot == null
                ? new GameObject("RuntimeObjectsRoot").transform
                : RuntimeGenerationRoot;
            RuntimeGenerationRoot.SetParent(MapRoot, worldPositionStays: false);
            RuntimeGenerationRoot.transform.localPosition = Vector3.zero;
            RuntimeGenerationRoot.transform.localRotation = Quaternion.identity;
            yield return null;
        }

        public void OnDestroy()
        {
            TaskManager.OnDestroy();
        }

        public IEnumerator HandlePermission()
        {
            yield return _locationPermissionHandler.HandlePermission();
            LocationPermissionState = _locationPermissionHandler.State;
            LocationPermissionStateChanged?.Invoke();
        }
    }

    public enum LocationPermissionState
    {
        Waiting,
        Granted,
        Denied,
        DeniedPermanently
    }

    public class LocationPermissionHandler
    {
        public LocationPermissionState State = LocationPermissionState.Waiting;

        public IEnumerator HandlePermission()
        {
            if (Permission.HasUserAuthorizedPermission(Permission.FineLocation))
            {
                State = LocationPermissionState.Granted;
                yield break;
            }

#if UNITY_ANDROID && !UNITY_EDITOR
			yield return RequestLocationPermissionIfNeeded();
#elif UNITY_IOS && !UNITY_EDITOR
			if (!Input.location.isEnabledByUser)
			{
				yield return iOSAskPermission();
			}
#elif UNITY_EDITOR
            // Editor / non-Android: assume granted
            OnPermissionGranted();
#endif
        }

#if UNITY_IOS && !UNITY_EDITOR
		public IEnumerator iOSAskPermission()
		{
			Input.location.Start();
				
			int waitTime = 10;
			while (Input.location.status == LocationServiceStatus.Initializing && waitTime > 0)
			{
				yield return new WaitForSeconds(1);
				waitTime--;
			}

			if (waitTime <= 0)
			{
				Debug.LogWarning("Location service init timeout");
				OnPermissionDenied();
				yield break;
			}

			if (Input.location.status == LocationServiceStatus.Failed)
			{
				Debug.LogWarning("Location service failed (likely denied)");
				OnPermissionDenied();
				yield break;
			}

			OnPermissionGranted();
		}
#endif

#if UNITY_ANDROID
        private IEnumerator RequestLocationPermissionIfNeeded()
        {
            var permissionDone = false;
            var callbacks = new PermissionCallbacks();
            callbacks.PermissionGranted += permission =>
            {
                if (permission == Permission.FineLocation)
                {
                    permissionDone = true;
                    OnPermissionGranted();
                }
            };

            callbacks.PermissionDenied += permission =>
            {

                if (permission == Permission.FineLocation)
                {
                    permissionDone = true;
                    OnPermissionDenied();
                }
            };

            callbacks.PermissionDeniedAndDontAskAgain += permission =>
            {
                if (permission == Permission.FineLocation)
                {
                    permissionDone = true;
                    OnPermissionDeniedPermanently();
                }
            };
            Permission.RequestUserPermission(Permission.FineLocation, callbacks);

            float elapsed = 0f;
            const float timeout = 60f;
            while (!permissionDone)
            {
                elapsed += Time.unscaledDeltaTime;
                if (elapsed >= timeout)
                {
                    Debug.LogWarning("Location permission request timed out — no callback received.");
                    OnPermissionDenied();
                    yield break;
                }
                yield return null;
            }
        }
#endif

        private void OnPermissionGranted()
        {
            State = LocationPermissionState.Granted;
            Debug.Log("Location permission GRANTED");
        }

        private void OnPermissionDenied()
        {
            State = LocationPermissionState.Denied;
            Debug.Log("Location permission denied");
        }

        private void OnPermissionDeniedPermanently()
        {
            State = LocationPermissionState.DeniedPermanently;
            Debug.LogWarning("Location permission denied permanently. User must enable location in system settings.");
        }
    }
}