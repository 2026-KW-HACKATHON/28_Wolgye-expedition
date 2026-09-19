using Mapbox.BaseModule.Map;
using Mapbox.BaseModule.Unity;
using Mapbox.ImageModule.Terrain.Settings;
using UnityEngine;

namespace Mapbox.ImageModule.Terrain.TerrainStrategies
{
	public class TerrainStrategy
	{
		public virtual int RequiredVertexCount
		{
			get { return 0; }
		}

		public virtual void Initialize(ElevationLayerProperties elOptions)
		{
			
		}


		public virtual void RegisterTile(UnityMapTile tile, bool createElevatedMesh)
		{

		}

		/// <summary>
		/// Release any Unity objects (Meshes, textures) the strategy created. Called by
		/// <c>TerrainLayerModule.OnDestroy</c>.
		/// </summary>
		public virtual void OnDestroy()
		{

		}
	}
}
