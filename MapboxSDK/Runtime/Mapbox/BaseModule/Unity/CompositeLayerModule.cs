using System.Collections;
using System.Collections.Generic;
using Mapbox.BaseModule.Data.Interfaces;
using Mapbox.BaseModule.Data.Tiles;
using Mapbox.BaseModule.Map;
using Mapbox.BaseModule.Unity;

public class CompositeLayerModule : ILayerModule
{
    public List<ILayerModule> LayerModules;

    public CompositeLayerModule(List<ILayerModule> layerModules)
    {
        LayerModules = layerModules;
    }
    
    public virtual IEnumerator Initialize()
    {
        foreach (var module in LayerModules)
        {
            yield return module.Initialize();
        }
    }

    public virtual void LoadTempTile(UnityMapTile tile)
    {
        foreach (var module in LayerModules)
        {
            module.LoadTempTile(tile);
        }
    }

    public virtual bool LoadInstant(UnityMapTile unityTile)
    {
        foreach (var module in LayerModules)
        {
            var moduleDone = module.LoadInstant(unityTile);
            if (!moduleDone) return false;
        }

        return true;
    }

    public IEnumerator LoadTiles(IEnumerable<CanonicalTileId> tiles)
    {
        foreach (var module in LayerModules)
        {
            module.LoadTiles(tiles);
        }

        yield return null;
    }

    public IEnumerable<IEnumerator> GetTileCoverCoroutines(IEnumerable<CanonicalTileId> tiles)
    {
        foreach (var module in LayerModules)
        {
            foreach (var routine in module.GetTileCoverCoroutines(tiles))
            {
                if (routine != null)
                {
                    yield return routine;
                }

            }
        }
    }

    public virtual bool RetainTiles(HashSet<CanonicalTileId> retainedTiles)
    {
        foreach (var module in LayerModules)
        {
            module.RetainTiles(retainedTiles);
        }

        return true;
    }
    
    public virtual void UpdatePositioning(IMapInformation mapInfo)
    {
        foreach (var module in LayerModules)
        {
            module.UpdatePositioning(mapInfo);
        }
    }

    public virtual void OnDestroy()
    {
        foreach (var module in LayerModules)
        {
            module.OnDestroy();
        }
    }
}