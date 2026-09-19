using Mapbox.BaseModule.Map;

namespace Mapbox.BaseModule.Data.Interfaces
{
    public interface IMapContainer
    {
        MapboxMap MapboxMap { get; }
    }
}