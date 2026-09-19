using System;
using System.Collections;
using System.Collections.Generic;
using Mapbox.BaseModule.Data.DataFetchers;
using Mapbox.BaseModule.Data.Tiles;
using Mapbox.BaseModule.Map;
using Mapbox.BaseModule.Unity;

namespace Mapbox.BaseModule.Data.Interfaces
{
	public interface ILayerModule
	{
		bool LoadInstant(UnityMapTile unityTile);
		IEnumerator LoadTiles(IEnumerable<CanonicalTileId> tiles);
		
		IEnumerable<IEnumerator> GetTileCoverCoroutines(IEnumerable<CanonicalTileId> tiles);
		
		bool RetainTiles(HashSet<CanonicalTileId> retainedTiles);
		IEnumerator Initialize();
		void OnDestroy();
		void UpdatePositioning(IMapInformation mapInfo);
		void LoadTempTile(UnityMapTile tile);
	}

	public interface ILayerModuleScript : ILayerModule
	{
		bool enabled { get; }
		ILayerModule ConstructModule(MapService service, MapInformation mapInformation, UnityContext unityContext);
	}
}