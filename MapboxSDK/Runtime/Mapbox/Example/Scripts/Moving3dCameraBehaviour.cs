using Mapbox.BaseModule.Map;
using UnityEngine;
using UnityEngine.Serialization;

namespace Mapbox.Example.Scripts.MapInput
{
	public class Moving3dCameraBehaviour : MapCameraBehaviour<Moving3dCamera>
	{
		[Tooltip("3D camera settings. Camera moves through world space while the map stays in place")]
		[FormerlySerializedAs("Core")]
		[SerializeField] private Moving3dCamera _core;

		public override Moving3dCamera Core => _core;

		public Vector3 GetViewCenterPosition()
		{
			return _core.GetViewCenterPosition();
		}

		public void Zoom(MapInformation mapInformation, Vector3 point, float zoomAction)
		{
			_core.Zoom(mapInformation, point, zoomAction);
		}

		public void Zoom(MapInformation mapInformation, float zoomAction)
		{
			_core.Zoom(mapInformation, GetViewCenterPosition(), zoomAction);
		}
	}
}
