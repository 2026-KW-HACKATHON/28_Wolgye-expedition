using System;
using Mapbox.BaseModule.Data.DataFetchers;
using Mapbox.BaseModule.Unity.ModuleBehaviours;
using UnityEngine;

namespace Mapbox.MapDebug.Scripts.Logging
{
    public class LoggingDataFetchingManagerBehaviour : DataFetchingManagerBehaviour
    {
        public LoggingDataFetchingManager Fetcher;
       
        public override DataFetchingManager GetDataFetchingManager(string accessToken, Func<string> skuTokenFunc)
        {
            Fetcher = new LoggingDataFetchingManager(accessToken, skuTokenFunc);
            return Fetcher;
        }
    }
}