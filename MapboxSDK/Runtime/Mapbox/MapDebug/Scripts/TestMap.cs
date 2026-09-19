using System.Collections;
using Mapbox.BaseModule.Data.DataFetchers;
using Mapbox.BaseModule.Data.Interfaces;
using Mapbox.BaseModule.Data.Platform.Cache;
using Mapbox.BaseModule.Data.Tasks;
using Mapbox.BaseModule.Map;
using Mapbox.BaseModule.Unity;
using Mapbox.BaseModule.Utilities;
using Mapbox.MapDebug.Scripts.Logging;
using Mapbox.UnityMapService;
using Mapbox.UnityMapService.DataSources;
using Mapbox.UnityMapService.TileProviders;
using Mapbox.VectorModule;
using UnityEngine;

namespace Mapbox.MapDebug.Scripts
{
    public class TestMap : MonoBehaviour
    {
        public IEnumerator Start()
        {
            var mapInfo = new MapInformation("60.1734031,24.9428875");
            var mapboxContext = new MapboxContext();
            yield return mapboxContext.Initialize();
            var unityContext = new UnityContext();

            var taskManager = new TaskManager();
            taskManager.Initialize();
            unityContext.TaskManager = taskManager;
            var dataManager = new DataFetchingManager(mapboxContext.GetAccessToken(), mapboxContext.GetSkuToken);

            var mapService = new MapUnityService(
                unityContext,
                mapboxContext,
                new UnityTileProvider(new UnityTileProviderSettings(Camera.main)),
                new MapboxCacheManager(
                    unityContext, 
                    new MemoryCache(),
                    new MockFileCache(taskManager),
                    new MockSqliteCache(taskManager)),
                dataManager);

            // var map = new MapboxMap(mapInfo, mapService);
            // map.MapVisualizer = new MapboxMapVisualizer(mapInfo, unityContext);

            // var vectorModule = new VectorLayerModule(mapInfo, mapService.GetVectorSource(new VectorSourceSettings(){ SourceType = VectorSourceType.MapboxStreetsV8}), null);
            // var vectorSource = mapService.GetVectorSource(new VectorSourceSettings(){ SourceType = VectorSourceType.MapboxStreetsV8});
        
            var imageryTileset = MapboxDefaultImagery.GetParameters(ImagerySourceType.MapboxSatellite);
            var imageSource = mapService.GetStaticRasterSource(new ImageSourceSettings() { TilesetId = imageryTileset.Id });
            var terrainTileset = MapboxDefaultElevation.GetParameters(ElevationSourceType.MapboxTerrain);
            var terrainSource = (TerrainSource) mapService.GetTerrainRasterSource(new ImageSourceSettings() { TilesetId = terrainTileset.Id });
            //map.MapVisualizer.LayerModules.Add(vectorModule);

            // Debug.Log("starting tile load");
            // await vectorModule.LoadTile(new CanonicalTileId(15, 5241, 12662));
            // Debug.Log("finished tile load 1");
            // await vectorSource.LoadTile(new CanonicalTileId(15, 5242, 12662));
            // Debug.Log("finished tile load 2");
            // await imageSource.LoadTile(new CanonicalTileId(15, 5240, 12662));
            // Debug.Log("finished image too");
            // await terrainSource.LoadTile(new CanonicalTileId(15, 5240, 12662));
            // var elevation = await terrainSource.ReadElevationValue(new LatitudeLongitude(42.0838943,14.0877331));
            // Debug.Log(elevation);
        }
    }
}
