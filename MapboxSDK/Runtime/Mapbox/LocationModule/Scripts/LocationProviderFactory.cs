using System;
using System.Collections;
using Mapbox.LocationModule.MapboxLocation;
using UnityEngine;

namespace Mapbox.LocationModule
{
	/// <summary>
	/// Factory to provide access to various LocationProviders.
	/// This is meant to be attached to a game object.
	/// </summary>
	public class LocationProviderFactory : MonoBehaviour
	{
		public LocationProviderType LocationProviderType;
		[NonSerialized] public bool IsLocationProviderReady = false;
		public Action<LocationProviderFactory> OnLocationProviderReady = (f) => { };
		
		[SerializeField]
		[Tooltip("Mapbox location provider for android and ios")]
		private MapboxLocationSettings _mapboxLocationProviderSettings = null;

		[SerializeField]
		[Tooltip("Provider using Unity's builtin 'Input.Location' service")]
		private UnityLocationProviderSettings _unityLocationProviderSettings;

		private AbstractLocationProvider _customLocationProvider;
		
		[SerializeField] private string EditorLatitudeLongitude;

		[SerializeField]
		bool _dontDestroyOnLoad;

		/// <summary>
		/// The default location provider. 
		/// Outside of the editor, this will be a <see cref="T:LocationModule.UnityLocationProvider"/>.
		/// In the Unity editor, this will be an <see cref="T:StaticLocationProvider"/>
		/// </summary>
		public ILocationProvider DefaultLocationProvider { get; set; }
		
		/// <summary>
		/// Initialize and inject the DefaultLocationProvider.
		/// </summary>
		public IEnumerator Initialize()
		{
			if (_dontDestroyOnLoad)
			{
				DontDestroyOnLoad(gameObject);
			}

			if(Application.isEditor || LocationProviderType == LocationProviderType.StaticLocationProvider)
			{
				DefaultLocationProvider = new StaticLocationProvider(EditorLatitudeLongitude);
			}
			else if(LocationProviderType == LocationProviderType.MapboxLocationProvider)
			{
				var mapboxLocationProvider = new MapboxLocationProvider(_mapboxLocationProviderSettings);
				mapboxLocationProvider.AvailabilityChanged += b => Debug.Log("MAPBOX_UNITY_SDK: LocationProviderFactory.AvailabilityChanged " + b);
				mapboxLocationProvider.AuthorizationChanged += b => Debug.Log("MAPBOX_UNITY_SDK: AuthorizationChanged " + b);
				mapboxLocationProvider.AccuracyAuthorizationChanged += b => Debug.Log("MAPBOX_UNITY_SDK: AuthorizationChanged " + b);
				DefaultLocationProvider = mapboxLocationProvider;
			}
			else if(LocationProviderType == LocationProviderType.UnityLocationProvider)
			{
				DefaultLocationProvider = new UnityLocationProvider(_unityLocationProviderSettings);
			}
			// else if(LocationProviderType == LocationProviderType.CustomLocationProvider)
			// {
			// 	DefaultLocationProvider = new UnityLocationProvider(_unityLocationProviderSettings);
			// }

			Debug.Log($"MAPBOX_UNITY_SDK:  LocationProviderFactory: Injected Location Provider - {DefaultLocationProvider.GetType()}");

			if (DefaultLocationProvider.GetType() != typeof(StaticLocationProvider))
			{
				if (Input.location.status != LocationServiceStatus.Running)
				{
					Input.location.Start();

					while (Input.location.status < LocationServiceStatus.Running)
						yield return null;
				}

				// Wait for first location update with timeout (10 seconds)
				float timeout = 10f;
				float elapsed = 0f;
				while (!DefaultLocationProvider.HasReceivedFirstFix &&
				       elapsed < timeout)
				{
					elapsed += UnityEngine.Time.deltaTime;
					yield return null;
				}
			}

			IsLocationProviderReady = true;
			OnLocationProviderReady(this);
		}

		private void Update()
		{
			DefaultLocationProvider?.Update();
		}

		private void OnDestroy()
		{
			DefaultLocationProvider?.OnDestroy();
		}
	}
	
	public enum LocationProviderType
	{
		[InspectorName("Static Location Provider")]
		StaticLocationProvider,

		[InspectorName("Unity Location Provider")]
		UnityLocationProvider,

		[InspectorName("Mapbox Location Provider (Experimental)")]
		MapboxLocationProvider,

		// [InspectorName("Custom Location Provider")]
		// CustomLocationProvider
	}
}


