using System;
using System.Collections;
using Mapbox.BaseModule.Data.DataFetchers;
using Mapbox.BaseModule.Data.Platform.Cache;
using Mapbox.BaseModule.Data.Platform.Cache.SQLiteCache;
using Mapbox.BaseModule.Map;
using Mapbox.BaseModule.Unity;
using Mapbox.BaseModule.Unity.ModuleBehaviours;
using Mapbox.BaseModule.Utilities;
using Mapbox.Example.Scripts.TileProviderBehaviours;
#if UNITY_RECORDER && UNITY_EDITOR
using Mapbox.MapDebug.Sequence;
#endif
using Mapbox.UnityMapService;
using Mapbox.UnityMapService.TileProviders;
using UnityEngine;

namespace Mapbox.MapDebug.Scripts.Logging
{
    public class LoggingMapBehaviour : MapBehaviourCore
    {
        private MapLogger _mapLogger;
        public UnityContext UnityContext;
        [SerializeField] protected MapboxCacheManagerBehaviour CacheManager;

        private MapService _mapService;
        private bool _waitForFirstLoadEvent = true;
        
        public bool InitializeOnStart = true;
        
        
        [SerializeField] protected TileCreatorBehaviour _tileCreatorBehaviour;
        [SerializeField] protected TileProviderBehaviour TileProvider;
        public Action<MapService> MapServiceReady = (v) => { };
        
#if UNITY_RECORDER && UNITY_EDITOR
        private SequenceControllerBehaviour _infoSequence;
#endif
        public virtual void Start()
        {
            if (InitializeOnStart)
                Initialize();
        }
        
        [ContextMenu("Initialize Map")]
        public override IEnumerator Initialize()
        {
            _mapLogger = FindObjectOfType<MapLogger>();
#if UNITY_RECORDER && UNITY_EDITOR
            _mapLogger?.AddLogger(_infoSequence);
            _infoSequence = FindObjectOfType<SequenceControllerBehaviour>() ?? gameObject.AddComponent<SequenceControllerBehaviour>();
#endif
            
            MapInformation.Initialize();
            
            var taskManager = new LoggingTaskManager();
            _mapLogger?.AddLogger(taskManager);
            yield return UnityContext.Initialize(taskManager);
            
            
            var mapboxContext = new MapboxContext();
            yield return mapboxContext.Initialize();
            _mapService = GetMapService(mapboxContext, UnityContext);
            MapServiceReady(_mapService);

            MapboxMap = new MapboxMap(MapInformation, UnityContext, _mapService);
            //passing map info to visualizer for root object, default tile material/texture
            var mapVisualizer = CreateMapVisualizer(MapInformation, UnityContext);
            var comps = GetComponents<ModuleConstructorScript>();
            foreach (var moduleBaseScript in comps)
            {
                if (moduleBaseScript.enabled)
                {
                    mapVisualizer.LayerModules.Add(moduleBaseScript.ConstructModule(_mapService, MapInformation, UnityContext));
                }
            }
            MapboxMap.MapVisualizer = mapVisualizer;
            MapboxMap.Initialized += InitializationCompleted;
             
            StartCoroutine(MapboxMap.Initialize());
        }
        
        private void InitializationCompleted()
        {
            Initialized(MapboxMap);
            if (!_waitForFirstLoadEvent)
            {
                //_readyForUpdates = true;
#if UNITY_RECORDER && UNITY_EDITOR
                _infoSequence.Record(MapboxMap, Camera.main);
#endif
            }
            else
            {
                MapboxMap.LoadMapView(() =>
                {
                    //_readyForUpdates = true;
                });
            }
        }
        
        private void OnValidate()
        {
            if(UnityContext == null)
                UnityContext = new UnityContext();
            if (UnityContext.MapRoot == null)
                UnityContext.MapRoot = transform;
            if (UnityContext.CoroutineStarter == null)
                UnityContext.CoroutineStarter = this;
        }

        private void OnDestroy()
        {
            MapboxMap?.OnDestroy();
        }

        protected virtual MapService GetMapService(MapboxContext mapboxContext, UnityContext unityContext)
        {
            var tileProvider = TileProvider != null ? TileProvider.Core : new UnityTileProvider(new UnityTileProviderSettings(Camera.main));
            var dataFetchingManager = CreateDataFetchingManager(mapboxContext);

            var cacheManager = GetCacheManager(unityContext, dataFetchingManager);

            return new MapUnityService(
                unityContext,
                mapboxContext,
                tileProvider,
                cacheManager,
                dataFetchingManager);
        }

        private MapboxCacheManager GetCacheManager(UnityContext unityContext, DataFetchingManager dataFetchingManager)
        {
            if (CacheManager != null)
                return CacheManager.GetCacheManager(unityContext, dataFetchingManager);
            
            var sqliteCache = new MockSqliteCache(unityContext.TaskManager, 1000);
            var fileCache = new MockFileCache(unityContext.TaskManager);
            
            _mapLogger?.AddLogger(fileCache);
            _mapLogger?.AddLogger(sqliteCache);
            
            var cacheManager = new LoggingCacheManager(
                unityContext,
                new MemoryCache(),
                fileCache,
                sqliteCache);
            _mapLogger?.AddLogger(cacheManager);
            return cacheManager;
        }

        protected virtual MapboxMapVisualizer CreateMapVisualizer(MapInformation mapInfo, UnityContext unityContext)
        {
            var tileCreator = _tileCreatorBehaviour != null
                ? _tileCreatorBehaviour.GetTileCreator(unityContext)
                : new TileCreator(unityContext);
            var mapVis = new LoggingMapVisualizer(mapInfo, unityContext, tileCreator);
            _mapLogger?.AddLogger(mapVis);
            return mapVis;
        }
        
        protected virtual DataFetchingManager CreateDataFetchingManager(MapboxContext mapboxContext)
        {
            var fetcher = new LoggingDataFetchingManager(mapboxContext.GetAccessToken(), mapboxContext.GetSkuToken);
            _mapLogger?.AddLogger(fetcher);
            return fetcher;
        }
    }
}