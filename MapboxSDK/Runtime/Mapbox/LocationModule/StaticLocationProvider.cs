using System;
using Mapbox.BaseModule.Data.Vector2d;
using Mapbox.BaseModule.Utilities;
using Mapbox.BaseModule.Utilities.Attributes;
using UnityEngine;

namespace Mapbox.LocationModule
{
	/// <summary>
	/// The EditorLocationProvider is responsible for providing mock location and heading data
	/// for testing purposes in the Unity editor.
	/// </summary>
	public class StaticLocationProvider : AbstractEditorLocationProvider
	{
		private LatitudeLongitude _latLng;

		public StaticLocationProvider(LatitudeLongitude latLng)
		{
			_latLng = latLng;
			_currentLocation = new Location()
			{
				LatitudeLongitude = _latLng
			};
		}
		
		public StaticLocationProvider(string latLng)
		{
			_latLng = Conversions.StringToLatLon(latLng);
			_currentLocation = new Location()
			{
				LatitudeLongitude = _latLng
			};
		}

		protected override void SetLocation()
		{
			//_currentLocation.UserHeading = transform.eulerAngles.y;
			_currentLocation.LatitudeLongitude = _latLng;
			_currentLocation.Accuracy = _accuracy;
			_currentLocation.Timestamp = UnixTimestampUtils.To(DateTime.UtcNow);
			_currentLocation.IsLocationUpdated = true;
			_currentLocation.IsUserHeadingUpdated = true;
			_currentLocation.IsLocationServiceEnabled = true;
		}
	}
}
