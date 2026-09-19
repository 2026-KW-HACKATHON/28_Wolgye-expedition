using Mapbox.BaseModule.Utilities;

namespace Mapbox.VectorModule.Filters
{
    public interface ILayerFeatureFilterComparer
    {
        void Initialize();
        bool Try(VectorFeatureUnity feature);
    }
}