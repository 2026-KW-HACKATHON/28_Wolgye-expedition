using Mapbox.BaseModule.Data.Interfaces;
using Mapbox.BaseModule.Map;
using UnityEngine;

namespace Mapbox.BaseModule.Unity
{
	public abstract class ModuleConstructorScript : MonoBehaviour, IModuleConstructor
	{
		public abstract ILayerModule ConstructModule(MapService service, IMapInformation mapInformation,
			UnityContext unityContext);

		public virtual void OnDestroy()
		{
			
		}

		public abstract ILayerModule ModuleImplementation { get; protected set; }
	}

	public interface IModuleConstructor
	{
		ILayerModule ModuleImplementation { get; }
	}
}