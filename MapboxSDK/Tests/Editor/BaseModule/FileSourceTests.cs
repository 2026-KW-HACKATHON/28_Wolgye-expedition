using System.Collections;
using Mapbox.BaseModule.Data.Platform;
using Mapbox.BaseModule.Data.Platform.Cache;
using Mapbox.BaseModule.Map;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Mapbox.BaseModuleTests.DataTests
{
    public class FileSourceTests
    {
        private ResilientWebRequestFileSource _fs;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            var mapboxContext = new MapboxContext();
            yield return mapboxContext.LoadConfigurationCoroutine(false);
            _fs = new ResilientWebRequestFileSource(mapboxContext.GetAccessToken(), mapboxContext.GetSkuToken);
        }
        
        [UnityTest]
        public IEnumerator Request()
        {
            Response resultData = null;
            var request = _fs.Request("https://api.mapbox.com/geocoding/v5/mapbox.places/helsinki.json", response =>
            {
                resultData = response;
            });

            while (!request.IsCompleted)
            {
                yield return null;
            }
        
            Assert.IsNotNull(resultData.Data, "No data received from the servers.");
        }
        
        [UnityTest]
        public IEnumerator RequestDnsError()
        {
            Response resultData = null;
            var request = _fs.Request("https://dnserror.shouldnotwork", response =>
            {
                resultData = response;
            });
        
            while (!request.IsCompleted)
            {
                yield return null;
            }
            Assert.IsTrue(resultData.HasError);
        }
    }
}