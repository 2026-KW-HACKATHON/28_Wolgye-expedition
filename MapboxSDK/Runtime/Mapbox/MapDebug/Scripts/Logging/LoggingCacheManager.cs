using System;
using System.Collections.Generic;
using Mapbox.BaseModule.Data.DataFetchers;
using Mapbox.BaseModule.Data.Platform.Cache;
using Mapbox.BaseModule.Data.Platform.Cache.SQLiteCache;
using Mapbox.BaseModule.Data.Tasks;
using Mapbox.BaseModule.Data.Tiles;
using Mapbox.BaseModule.Unity;
using Newtonsoft.Json.Linq;

namespace Mapbox.MapDebug.Scripts.Logging
{
    public class LoggingCacheManager : MapboxCacheManager, ILogWriter
    {
        public ISqliteCache SqLiteCache => _sqLiteCache;
        public List<string> Logs = new List<string>();
        
        public LoggingCacheManager(UnityContext unityContext, MemoryCache memoryCache, FileCache fileCache = null, SqliteCache cache = null) : base(unityContext, memoryCache, fileCache, cache)
        {
        }

        public override void SaveBlob(MapboxTileData vectorCacheItem, bool forceInsert)
        {
            Logs.Add($"Save Vector for {vectorCacheItem.TileId}");
            base.SaveBlob(vectorCacheItem, forceInsert);
        }

        public override void SaveImage(RasterData textureCacheItem, bool forceInsert)
        {
            Logs.Add($"Save Image for {textureCacheItem.TileId} {textureCacheItem.TilesetId}");
            base.SaveImage(textureCacheItem, forceInsert);
        }

        public override void GetImageAsync<T>(CanonicalTileId tileId, string tilesetId, bool isTextureNonreadable, Action<T> callback)
        {
            Logs.Add($"Get Image for {tileId}");
            base.GetImageAsync(tileId, tilesetId, isTextureNonreadable, callback);
        }

        public override DataTaskWrapper<T> GetTileInfoTask<T>(CanonicalTileId tileId, string tilesetid, int priority = 1,
            T data = default(T))
        {
            Logs.Add($"Get Data for {tileId} {tilesetid}");
            return base.GetTileInfoTask(tileId, tilesetid, priority, data);
        }

        public override void UpdateExpiration(CanonicalTileId tileId, string tilesetId, DateTime date)
        {
            Logs.Add($"Update Expiration for {tileId} {tilesetId}");
            base.UpdateExpiration(tileId, tilesetId, date);
        }

        public JObject DumpLogs()
        {
            var dataLog = new JObject();
            var jArray = new JArray();
            foreach (var log in Logs)
            {
                var recordData = new JObject();
                recordData["Log"] = log;
                jArray.Add(recordData);
            }

            dataLog["CacheManagerLogs"] = jArray;
            return dataLog;
        }

        public string PrintScreen()
        {
            return "";
        }

        public void ResetStats()
        {
            Logs.Clear();
        }
    }
}