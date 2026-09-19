using Mapbox.BaseModule.Data.DataFetchers;
using Mapbox.BaseModule.Data.Platform.Cache;
using UnityEngine;

namespace Mapbox.BaseModule.Unity.ModuleBehaviours
{
    public abstract class MapboxCacheManagerBehaviour : MonoBehaviour
    {
        public abstract MapboxCacheManager GetCacheManager(UnityContext unityContext, DataFetchingManager dataFetchingManager);
    }
}