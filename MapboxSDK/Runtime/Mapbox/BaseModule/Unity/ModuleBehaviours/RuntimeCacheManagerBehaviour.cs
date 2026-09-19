using Mapbox.BaseModule.Data.DataFetchers;
using Mapbox.BaseModule.Data.Platform.Cache;
using Mapbox.BaseModule.Data.Platform.Cache.SQLiteCache;

namespace Mapbox.BaseModule.Unity.ModuleBehaviours
{
    public class RuntimeCacheManagerBehaviour : MapboxCacheManagerBehaviour
    {
        public MapboxCacheManager CacheManager;
        public MemoryCache MemoryCache;

        public MapboxCacheManager GetCacheManager() => CacheManager;

        public bool CreateSqliteCache = true;
        
        public bool CreateFileCache = true;
        
        public bool UseCustomName = false;
        public string CustomName;
        
        public override MapboxCacheManager GetCacheManager(UnityContext unityContext, DataFetchingManager dataFetchingManager)
        {
            if (CacheManager == null)
            {
                SqliteCache sqliteCache = null;
                FileCache fileCache = null;
                if (CreateSqliteCache)
                {
                    if (UseCustomName)
                    {
                        sqliteCache = new SqliteCache(unityContext.TaskManager, 1000, CustomName);
                    }
                    else
                    {
                        sqliteCache = new SqliteCache(unityContext.TaskManager, 1000);
                    }
                }

                if (CreateFileCache)
                {
                    if (UseCustomName)
                    {
                        fileCache = new FileCache(unityContext.TaskManager, CustomName);
                    }
                    else
                    {
                        fileCache = new FileCache(unityContext.TaskManager);
                    }
                }
                
                MemoryCache = new MemoryCache();
                CacheManager = new MapboxCacheManager(
                    unityContext,
                    MemoryCache,
                    fileCache,
                    sqliteCache);
            }

            return CacheManager;
        }
    }
}