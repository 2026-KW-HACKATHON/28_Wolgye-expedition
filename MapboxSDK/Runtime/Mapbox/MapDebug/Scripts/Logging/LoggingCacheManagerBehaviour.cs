using Mapbox.BaseModule.Data.DataFetchers;
using Mapbox.BaseModule.Data.Platform.Cache;
using Mapbox.BaseModule.Unity;
using Mapbox.BaseModule.Unity.ModuleBehaviours;
using Mapbox.MapDebug.Scripts.Logging;
using UnityEngine;

public class LoggingCacheManagerBehaviour : MapboxCacheManagerBehaviour
{
    public MapboxCacheManager CacheManager;
    public MemoryCache MemoryCache;
    public LoggingSqliteCache SqliteCache = null;
    public LoggingFileCache FileCache = null;
    
    public MapboxCacheManager GetCacheManager() => CacheManager;

    public bool CreateSqliteCache = true;
    public bool CreateFileCache = true;
        
    public override MapboxCacheManager GetCacheManager(UnityContext unityContext, DataFetchingManager dataFetchingManager)
    {
        if (CacheManager == null)
        {
            SqliteCache = CreateSqliteCache ? new LoggingSqliteCache(unityContext.TaskManager, 1000) : null;
            FileCache = CreateFileCache ? new LoggingFileCache(unityContext.TaskManager) : null;
            MemoryCache = new MemoryCache();
                
            CacheManager = new MapboxCacheManager(
                unityContext,
                MemoryCache,
                FileCache,
                SqliteCache);
        }

        return CacheManager;
    }
}