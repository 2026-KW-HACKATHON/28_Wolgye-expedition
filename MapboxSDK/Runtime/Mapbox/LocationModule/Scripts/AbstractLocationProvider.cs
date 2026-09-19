using System;

namespace Mapbox.LocationModule
{
	public abstract class AbstractLocationProvider : ILocationProvider
	{
		protected Location _currentLocation;

		/// <summary>
		/// Gets the last known location.
		/// </summary>
		/// <value>The current location.</value>
		public Location CurrentLocation
		{
			get
			{
				return _currentLocation;
			}
		}

		/// <summary>
		/// Has at least one location update been received from the provider?
		/// Set once and never reset.
		/// </summary>
		public bool HasReceivedFirstFix { get; protected set; }

		public event Action<Location> OnLocationUpdated = delegate { };

		protected virtual void SendLocation(Location location)
		{
			HasReceivedFirstFix = true;
			OnLocationUpdated(location);
		}

		public virtual void Update()
		{
			
		}
		
		public virtual void OnDestroy()
		{
		}
	}
}