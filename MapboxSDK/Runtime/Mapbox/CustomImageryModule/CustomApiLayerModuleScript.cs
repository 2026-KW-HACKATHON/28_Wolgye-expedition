using Mapbox.BaseModule.Data.Interfaces;
using Mapbox.BaseModule.Map;
using Mapbox.BaseModule.Unity;
using Mapbox.BaseModule.Utilities;
using Mapbox.ImageModule;
using Mapbox.UnityMapService;
using UnityEngine;

namespace Mapbox.CustomImageryModule
{
    public class CustomApiLayerModuleScript : ModuleConstructorScript
    {
        public CustomSourceSettings CustomSourceSettings;
        public StaticLayerModuleSettings Settings = new StaticLayerModuleSettings()
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
            if (Settings.SourceType == ImagerySourceType.None)
            {

            }
            else if (Settings.SourceType == ImagerySourceType.Custom)
            {
                Settings.DataSettings.TilesetId = Settings.CustomSourceId;
            }
            else
            {
                var imageryTileset = MapboxDefaultImagery.GetParameters(Settings.SourceType);
                Settings.DataSettings.TilesetId = imageryTileset.Id;
            }

            var unityService = service as MapUnityService;
            var source = new CustomSource(CustomSourceSettings, unityService.FetchingManager, unityService.CacheManager, new ImageSourceSettings()
            {
                TilesetId = "CustomImagery"
            });
            ModuleImplementation = new StaticApiLayerModule(source, Settings);
            return ModuleImplementation;
        }
    }
}