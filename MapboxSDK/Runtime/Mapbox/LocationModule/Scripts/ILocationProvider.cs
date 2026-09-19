using System;

namespace Mapbox.LocationModule
{
	/// <summary>
	/// Implement ILocationProvider to send Heading and Location updates.
	/// </summary>
	public interface ILocationProvider
	{
		event Action<Location> OnLocationUpdated;
		Location CurrentLocation { get; }
		bool HasReceivedFirstFix { get; }

		void Update();
		void OnDestroy();
	}
}