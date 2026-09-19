using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Mapbox.BaseModule.Data.Platform.Cache;
using UnityEditor;
using UnityEngine;

namespace Mapbox.MapDebug.Scripts.Logging.Editor
{
    [CustomEditor(typeof(LoggingCacheManagerBehaviour))]
    public class LoggingCacheManagerBehaviourEditor : UnityEditor.Editor
    {
        private bool _memoryCacheFoldout;
        private bool _sqliteLoggingFoldout;
        private bool _fileLoggingFoldout;

        private Dictionary<int, ITypeCache> _subCaches;

        public override void OnInspectorGUI()
        {
            var behaviour = (LoggingCacheManagerBehaviour)target;

            // Handle CreateSqliteCache and LoggingModeEnabled logic manually
            behaviour.CreateSqliteCache = EditorGUILayout.Toggle("Create Sqlite Cache", behaviour.CreateSqliteCache);
            behaviour.CreateFileCache = EditorGUILayout.Toggle("Create File Cache", behaviour.CreateFileCache);

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Runtime Cache Details", EditorStyles.boldLabel);

            DrawMemoryCacheSection(behaviour);

            if (behaviour.CreateSqliteCache)
            {
                DrawLoggingSqliteCacheSection(behaviour);
            }
        
            if (behaviour.CreateFileCache)
            {
                DrawLoggingFileCacheSection(behaviour);
            }

            if (Application.isPlaying)
                Repaint();
        }

        private void DrawMemoryCacheSection(LoggingCacheManagerBehaviour behaviour)
        {
            var memoryCache = behaviour.MemoryCache;

            _memoryCacheFoldout = EditorGUILayout.Foldout(_memoryCacheFoldout, "MemoryCache", true);
            if (!_memoryCacheFoldout)
                return;

            EditorGUI.indentLevel++;

            if (memoryCache == null)
            {
                EditorGUILayout.HelpBox("MemoryCache is null. Run the scene to inspect it.", MessageType.Info);
                EditorGUI.indentLevel--;
                return;
            }

            // Get private _subCaches field
            var field = typeof(MemoryCache).GetField("_subCaches", BindingFlags.NonPublic | BindingFlags.Instance);
            _subCaches = field?.GetValue(memoryCache) as Dictionary<int, ITypeCache>;

            if (_subCaches == null)
            {
                EditorGUILayout.HelpBox("Could not access _subCaches.", MessageType.Warning);
                EditorGUI.indentLevel--;
                return;
            }

            if (_subCaches.Count == 0)
            {
                EditorGUILayout.LabelField("No sub-caches.");
            }
            else
            {
                foreach (var kvp in _subCaches)
                {
                    DrawSubCache(kvp.Key, kvp.Value);
                }
            }

            EditorGUI.indentLevel--;
        }

        private void DrawSubCache(int key, object cache)
        {
            if (cache == null)
                return;

            var type = cache.GetType();
            if (!type.IsGenericType)
                return;

            if (!type.GetInterfaces().Any(i => i.Name == "ITypeCache"))
                return;

            var genericArg = type.GetGenericArguments().FirstOrDefault();
            string typeName = genericArg != null ? genericArg.Name : "(Unknown)";

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField($"{typeName} Cache", EditorStyles.boldLabel);

            var activeProp = type.GetProperty("ActiveCount", BindingFlags.Public | BindingFlags.Instance);
            var inactiveProp = type.GetProperty("InactiveCount", BindingFlags.Public | BindingFlags.Instance);

            if (activeProp != null)
            {
                var activeVal = activeProp.GetValue(cache);
                UIHelpers.DrawWideLabel("ActiveCount", activeVal?.ToString() ?? "N/A");
            }

            if (inactiveProp != null)
            {
                var inactiveVal = inactiveProp.GetValue(cache);
                UIHelpers.DrawWideLabel("InactiveCount", inactiveVal?.ToString() ?? "N/A");
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawLoggingSqliteCacheSection(LoggingCacheManagerBehaviour behaviour)
        {
            var cacheManager = behaviour.CacheManager;
        
            _sqliteLoggingFoldout = EditorGUILayout.Foldout(_sqliteLoggingFoldout, "Sqlite Cache", true);
            if (!_sqliteLoggingFoldout)
                return;

            EditorGUI.indentLevel++;
            var sqliteCache = behaviour.SqliteCache;
        
            if (cacheManager == null || sqliteCache == null)
            {
                EditorGUILayout.HelpBox("SqliteCache is null. Run the scene to inspect it.", MessageType.Info);
                EditorGUI.indentLevel--;
                return;
            }

            DrawSqliteLoggingCounters(sqliteCache);

            EditorGUI.indentLevel--;
        }

        private void DrawSqliteLoggingCounters(object sqliteCache)
        {
            string[] fieldNames = { "_addCount", "_getCount", "_updateCount", "_removeCount" };

            foreach (var name in fieldNames)
            {
                var field = sqliteCache.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
                if (field == null) continue;

                var val = field.GetValue(sqliteCache);
                UIHelpers.DrawWideLabel(ObjectNames.NicifyVariableName(name), val?.ToString() ?? "0");
            }
        }

        private void DrawLoggingFileCacheSection(LoggingCacheManagerBehaviour behaviour)
        {
            var cacheManager = behaviour.CacheManager;
        
            _fileLoggingFoldout = EditorGUILayout.Foldout(_fileLoggingFoldout, "File Cache", true);
            if (!_fileLoggingFoldout)
                return;

            EditorGUI.indentLevel++;
            var fileCache = behaviour.FileCache;
        
            if (cacheManager == null || fileCache == null)
            {
                EditorGUILayout.HelpBox("FileCache is null. Run the scene to inspect it.", MessageType.Info);
                EditorGUI.indentLevel--;
                return;
            }

            DrawFileLoggingCounters(fileCache);

            EditorGUI.indentLevel--;
        }
    
        private void DrawFileLoggingCounters(LoggingFileCache fileCache)
        {
            string[] fieldNames = { "_savedCount", "_readCount"};

            foreach (var name in fieldNames)
            {
                var field = fileCache.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
                if (field == null) continue;

                var val = field.GetValue(fileCache);
                UIHelpers.DrawWideLabel(ObjectNames.NicifyVariableName(name), val?.ToString() ?? "0");
            }
        }
    }
}
