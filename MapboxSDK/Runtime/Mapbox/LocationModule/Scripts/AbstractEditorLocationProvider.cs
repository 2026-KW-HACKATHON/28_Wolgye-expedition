using UnityEngine;

namespace Mapbox.LocationModule
{
	public abstract class AbstractEditorLocationProvider : AbstractLocationProvider
	{
		[SerializeField]
		protected int _accuracy;

		[SerializeField]
		bool _autoFireEvent;

		[SerializeField]
		float _updateInterval;

		[SerializeField]
		bool _sendEvent;

		WaitForSeconds _wait = new WaitForSeconds(0);

#if UNITY_EDITOR
		protected virtual void Awake()
		{
		}
#endif


		// Added to support TouchCamera script. 
		public void SendLocationEvent()
		{
			SetLocation();
			SendLocation(_currentLocation);
		}


		protected virtual void OnValidate()
		{
			if (_sendEvent)
			{
				_sendEvent = false;
				SendLocation(_currentLocation);
			}
		}

		protected abstract void SetLocation();
	}
}
