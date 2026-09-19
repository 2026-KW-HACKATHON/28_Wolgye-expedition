using System;
using System.Collections.Generic;
using System.Linq;
using Mapbox.BaseModule.Data.DataFetchers;
using Mapbox.BaseModule.Data.Interfaces;
using Mapbox.BaseModule.Map;
using Mapbox.BaseModule.Unity;
using UnityEngine;

public class CompositeLayerModuleScript : ModuleConstructorScript
{
    public List<ModuleConstructorScript> ModuleConstructorScripts;
    public override ILayerModule ModuleImplementation { get; protected set; }

    public override ILayerModule ConstructModule(MapService service, IMapInformation mapInformation, UnityContext unityContext)
    {
        ModuleImplementation = new CompositeLayerModule(ModuleConstructorScripts.Where(x => x != null).Select(x => x.ConstructModule(service, mapInformation, unityContext)).ToList());
        return ModuleImplementation;
    }
}