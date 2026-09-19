using UnityEngine;

namespace Mapbox.LocationModule.UnityLocationWrappers
{
	/// <summary>
	/// Wrap Unity's LocationService into MapboxLocationService
	/// </summary>
	public class MapboxLocationServiceUnityWrapper : IMapboxLocationService
	{

		public bool isEnabledByUser { get { return Input.location.isEnabledByUser; } }


		public UnityEngine.LocationServiceStatus status { get { return Input.location.status; } }


		public IMapboxLocationInfo lastData { get { return new MapboxLocationInfoUnityWrapper(Input.location.lastData); } }


		public void Start(float desiredAccuracyInMeters, float updateDistanceInMeters)
		{
			Input.location.Start(desiredAccuracyInMeters, updateDistanceInMeters);
		}


		public void Stop()
		{
			Input.location.Stop();
		}



	}
}
