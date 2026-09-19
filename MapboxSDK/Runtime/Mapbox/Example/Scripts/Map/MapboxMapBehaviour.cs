using System;
using System.Collections;
using System.Linq;
using Mapbox.BaseModule;
using Mapbox.BaseModule.Data.DataFetchers;
using Mapbox.BaseModule.Data.Platform.Cache;
using Mapbox.BaseModule.Data.Platform.Cache.SQLiteCache;
using Mapbox.BaseModule.Map;
using Mapbox.BaseModule.Unity;
using Mapbox.BaseModule.Unity.ModuleBehaviours;
using Mapbox.BaseModule.Utilities;
using Mapbox.Example.Scripts.TileProviderBehaviours;
using Mapbox.ImageModule.Terrain.TerrainStrategies;
using Mapbox.LocationModule;
using Mapbox.UnityMapService;
using Mapbox.UnityMapService.TileProviders;
using UnityEngine;

namespace Mapbox.Example.Scripts.Map
{
    public class MapboxMapBehaviour : MapBehaviourCore
    {
        [Tooltip("Unity tools for map to use")]
        public UnityContext UnityContext;

        [SerializeField] protected TileCreatorBehaviour _tileCreatorBehaviour;
        [Tooltip("Material used when no TileCreatorBehaviour is assigned. Asset reference keeps the shader alive in player builds. If you swap in a TileCreatorBehaviour, this field is ignored.")]
        [SerializeField] protected Material _defaultTileMaterial;
        [SerializeField] protected TileProviderBehaviour TileProvider;
        [SerializeField] protected DataFetchingManagerBehaviour DataFetcher;
        [SerializeField] protected MapboxCacheManagerBehaviour CacheManager;
        [SerializeField] protected LocationProviderFactory LocationFactory;
        private MapService _mapService;
        
        public bool InitializeOnStart = true;
        public Action<MapService> MapServiceReady = (v) => { };

        
        public virtual void Start()
        {
            if (InitializeOnStart)
                StartCoroutine(Initialize());
        }

        [ContextMenu("Initialize")]
        public override IEnumerator Initialize()
        {
            if (InitializationStatus != InitializationStatus.WaitingForInitialization)
                yield break;

            MapInformation.Initialize();
            
            yield return UnityContext.Initialize();
            //we handle permission via unity, instead of using location providers themselves
            yield return UnityContext.HandlePermission();
            
            if (Application.isEditor || UnityContext.LocationPermissionState == LocationPermissionState.Granted)
            {
                if (LocationFactory != null)
                {
                    yield return LocationFactory.Initialize();
                    var locationProvider = LocationFactory.DefaultLocationProvider;
                    MapInformation.SetLatitudeLongitude(locationProvider.CurrentLocation.LatitudeLongitude);
                }
            }
            else
            {
                Debug.Log("Location permission is " + UnityContext.LocationPermissionState);
            }
            
            var mapboxContext = new MapboxContext();
            yield return mapboxContext.Initialize();
            _mapService = GetMapService(mapboxContext, UnityContext);
            MapServiceReady(_mapService);
            
            MapboxMap = CreateMapObject();
            MapboxMap.Initialized += InitializationCompleted;
            yield return MapboxMap.Initialize();
        }
        

        private void InitializationCompleted()
        {
            Initialized(MapboxMap);
            MapboxMap.LoadMapView();
        }
        
        private void OnValidate()
        {
            if (UnityContext == null) 
                UnityContext = new UnityContext();
            if (UnityContext.MapRoot == null) 
                UnityContext.MapRoot = transform;
            if (UnityContext.CoroutineStarter == null) 
                UnityContext.CoroutineStarter = this;
        }

        private void OnDestroy()
        {
            MapboxMap?.OnDestroy();
            UnityContext.OnDestroy();
        }
        
        protected virtual MapboxMap CreateMapObject()
        {
            MapboxMap = new MapboxMap(MapInformation, UnityContext, _mapService);
            //passing map info to visualizer for root object, default tile material/texture
            var mapVisualizer = CreateMapVisualizer(MapInformation, UnityContext);
            foreach (var moduleBaseScript in GetComponents<ModuleConstructorScript>())
            {
                if (!moduleBaseScript.enabled) continue;
                mapVisualizer.LayerModules.Add(moduleBaseScript.ConstructModule(_mapService, MapInformation, UnityContext));
            }
            MapboxMap.MapVisualizer = mapVisualizer;
            return MapboxMap;
        }
        
        protected virtual MapboxMapVisualizer CreateMapVisualizer(IMapInformation mapInfo, UnityContext unityContext)
        {
            ITileCreator tileCreator;
            if (_tileCreatorBehaviour != null)
            {
                tileCreator = _tileCreatorBehaviour.GetTileCreator(unityContext);
            }
            else
            {
                // Fallback when no TileCreatorBehaviour is assigned on the GameObject.
                // The Material is taken from a direct asset reference (_defaultTileMaterial)
                // so its shader survives player-build shader stripping — Shader.Find can't
                // resolve shaders that aren't referenced from a shipped Material or listed
                // in Project Settings → Graphics → Always Included Shaders.
                if (_defaultTileMaterial == null)
                {
                    throw new InvalidOperationException(
                        $"MapboxMapBehaviour on '{name}' has no TileCreator assigned and no default tile Material set. " +
                        "Assign a TileCreatorBehaviour on this GameObject, or drag a Material (e.g. ElevatedTerrainMaterial) " +
                        "into the 'Default Tile Material' field on MapboxMapBehaviour.");
                }
                tileCreator = new TileCreator(unityContext, new[] { _defaultTileMaterial });
            }
            return new MapboxMapVisualizer(mapInfo, unityContext, tileCreator);
        }

        protected virtual MapService GetMapService(MapboxContext mapboxContext, UnityContext unityContext)
        {
            var mapCamera = FindCamera();
            var tileProvider = TileProvider != null ? TileProvider.Core : new UnityTileProvider(new UnityTileProviderSettings(mapCamera));
            var dataFetchingManager = CreateDataFetchingManager(mapboxContext);
            var cacheManager = GetCacheManager(unityContext, dataFetchingManager);

            return new MapUnityService(
                unityContext,
                mapboxContext,
                tileProvider,
                cacheManager,
                dataFetchingManager);
        }

        protected virtual MapboxCacheManager GetCacheManager(UnityContext unityContext, DataFetchingManager dataFetchingManager)
        {
            if (CacheManager != null)
                return CacheManager.GetCacheManager(unityContext, dataFetchingManager);
            
            SqliteCache sqliteCache = null;
            FileCache fileCache = null;
            sqliteCache = new SqliteCache(unityContext.TaskManager, 1000);
            fileCache = new FileCache(unityContext.TaskManager);

            var cacheManager = new MapboxCacheManager(
                unityContext,
                new MemoryCache(),
                fileCache,
                sqliteCache);
            return cacheManager;
        }
        
        protected virtual DataFetchingManager CreateDataFetchingManager(MapboxContext mapboxContext)
        {
            return DataFetcher != null
                ? DataFetcher.GetDataFetchingManager(mapboxContext.GetAccessToken(), mapboxContext.GetSkuToken)
                : new DataFetchingManager(mapboxContext.GetAccessToken(), mapboxContext.GetSkuToken);
        }
        
        private Camera FindCamera()
        {
            var mapCamera = Camera.main;
            if (mapCamera == null)
            {
                Debug.Log("No camera is tagged as Main Camera. Using the first one found in the scene.");
            }

            return mapCamera;
        }
    }
}