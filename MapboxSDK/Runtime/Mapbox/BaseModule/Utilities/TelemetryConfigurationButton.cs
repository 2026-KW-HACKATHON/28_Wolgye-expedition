using System;
using System.Collections.Generic;
using System.Linq;
using Mapbox.BaseModule.Map;
using UnityEngine;
using UnityEngine.UI;

namespace Mapbox.BaseModule.Utilities
{
	public class TelemetryConfigurationButton : MonoBehaviour
	{
		private List<MapBehaviourCore> _core;
		private List<MapService> _mapServices;

		public Text KeepParticipatingLabel;
		private bool _isItEnabled = true;

		protected void Awake()
		{
			_mapServices = new List<MapService>();
			var cores = FindObjectsOfType<MapBehaviourCore>().ToList();
			foreach (var core in cores)
			{
				if (core.InitializationStatus < InitializationStatus.Initialized)
				{
					core.Initialized += map =>
					{
						_mapServices.Add(map.MapService);
						_isItEnabled &= map.MapService.GetTelemetryCollectionState();
					};
				}
				else
				{
					_mapServices.Add(core.MapboxMap.MapService);
					_isItEnabled &= core.MapboxMap.MapService.GetTelemetryCollectionState();
				}
			}
		}

		private void OnEnable()
		{
			KeepParticipatingLabel.text = _isItEnabled ? "Keep Participating" : "Participate";
		}

		public void SetTelemState(bool state)
		{
			foreach (var mapService in _mapServices)
			{
				var success = mapService.SetTelemetryCollectionState(state);
				if (!success)
				{
					Debug.Log("Couldn't save settings file");
				}
				else
				{
					_isItEnabled = state;
				}
			}
		}
	}
}
