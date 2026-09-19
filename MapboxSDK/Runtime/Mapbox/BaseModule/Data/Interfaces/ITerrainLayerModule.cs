using System;
using System.Collections;
using Mapbox.BaseModule.Data.DataFetchers;
using Mapbox.BaseModule.Data.Tiles;

namespace Mapbox.BaseModule.Data.Interfaces
{
	public interface ITerrainLayerModule : ILayerModule
	{
		float QueryElevation(CanonicalTileId tileId, float x, float y);
		IEnumerator LoadTileData(CanonicalTileId tileId, Action<TerrainData> callback = null);
	}
}