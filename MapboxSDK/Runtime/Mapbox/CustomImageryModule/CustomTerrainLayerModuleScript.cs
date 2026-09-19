using Mapbox.BaseModule.Data.Interfaces;
using Mapbox.BaseModule.Map;
using Mapbox.BaseModule.Unity;
using Mapbox.ImageModule.Terrain;
using Mapbox.UnityMapService;
using UnityEngine;

namespace Mapbox.CustomImageryModule
{
    public class CustomTerrainLayerModuleScript : ModuleConstructorScript
    {
        public CustomSourceSettings CustomSourceSettings;
        public TerrainLayerModuleSettings Settings = new TerrainLayerModuleSettings()
        {
            RejectTilesOutsideZoom = new Vector2(2, 25),
            DataSettings = new ImageSourceSettings()
            {
                ClampDataLevelToMax = 25
            }
        };
        public override ILayerModule ModuleImplementation { get; protected set; }

        private void Start()
        {
			
        }

        public override ILayerModule ConstructModule(MapService service, IMapInformation mapInformation,
            UnityContext unityContext)
        {
            Settings.DataSettings.TilesetId = "CustomTerrain";

            // Pass Settings.DataSettings through so cache size, retina flag, non-readable
            // textures, and data-zoom clamp from the inspector reach CustomTerrainSource.
            // Previously a throwaway ImageSourceSettings was used here and the user's
            // inspector values never took effect.
            var unityService = service as MapUnityService;
            var source = new CustomTerrainSource(CustomSourceSettings, unityService.FetchingManager, unityService.CacheManager, Settings.DataSettings);
            ModuleImplementation = new TerrainLayerModule(source, Settings);
            return ModuleImplementation;
        }
    }
}