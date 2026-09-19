using Mapbox.BaseModule.Data.DataFetchers;
using Mapbox.BaseModule.Data.Platform.Cache;

public interface IUnityMapService
{
	public MapboxCacheManager GetCacheManager();
	public DataFetchingManager GetFetchingManager();
}
