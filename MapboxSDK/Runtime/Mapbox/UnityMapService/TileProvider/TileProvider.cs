using Mapbox.BaseModule.Data.Interfaces;
using Mapbox.BaseModule.Map;

public abstract class TileProvider
{
    public abstract bool GetTileCover(IMapInformation mapInformation, TileCover tileCover);
}