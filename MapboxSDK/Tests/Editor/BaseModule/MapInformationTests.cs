using Mapbox.BaseModule.Data.Vector2d;
using Mapbox.BaseModule.Map;
using NUnit.Framework;

namespace Mapbox.BaseModuleTests
{
    public class MapInformationTests
    {
        [Test]
        public void Initialize_ResetsTerrainBoundsToDefaults()
        {
            // Simulate a fresh instance with pre-mutated terrain bounds — proves the
            // Initialize() reset is doing its job. Today's field initializers cover
            // the common case, but this guards against future paths that reuse an
            // existing MapInformation instance.
            var info = new MapInformation("0,0");
            info.Terrain.MinElevation = -999f;
            info.Terrain.MaxElevation = 9999f;

            info.Initialize(new LatitudeLongitude(0, 0));

            Assert.AreEqual(TerrainInfo.DefaultMinElevation, info.Terrain.MinElevation);
            Assert.AreEqual(TerrainInfo.DefaultMaxElevation, info.Terrain.MaxElevation);
        }

        [Test]
        public void Initialize_SetsLatitudeLongitude()
        {
            var info = new MapInformation("0,0");
            var target = new LatitudeLongitude(40.7484, -73.9857);

            info.Initialize(target);

            Assert.AreEqual(target.Latitude, info.LatitudeLongitude.Latitude);
            Assert.AreEqual(target.Longitude, info.LatitudeLongitude.Longitude);
        }

        [Test]
        public void Initialize_IsIdempotent()
        {
            var info = new MapInformation("0,0");
            info.Initialize(new LatitudeLongitude(10, 20));

            // Mutate terrain after first init, then call Initialize again — the
            // guard at the top should short-circuit and leave our mutation in place.
            info.Terrain.MinElevation = -500f;
            info.Initialize(new LatitudeLongitude(50, 50));

            Assert.AreEqual(-500f, info.Terrain.MinElevation,
                "Second Initialize call should be a no-op and must not reset terrain bounds.");
            Assert.AreEqual(10, info.LatitudeLongitude.Latitude,
                "Second Initialize call should not overwrite the lat/lon from the first call.");
        }
    }
}
