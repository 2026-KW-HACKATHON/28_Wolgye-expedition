using Mapbox.BaseModule.Map;
using NUnit.Framework;

namespace Mapbox.BaseModuleTests
{
    public class TerrainInfoTests
    {
        [Test]
        public void DefaultConstants_HaveExpectedValues()
        {
            // Documented contract: Min=0, Max=5000. Conservative bounds that admit any
            // plausible mountain range so steep terrain isn't frustum-culled on first paint.
            Assert.AreEqual(0f, TerrainInfo.DefaultMinElevation);
            Assert.AreEqual(5000f, TerrainInfo.DefaultMaxElevation);
        }

        [Test]
        public void NewInstance_FieldsInitializedToDefaults()
        {
            var info = new TerrainInfo();

            Assert.AreEqual(TerrainInfo.DefaultMinElevation, info.MinElevation);
            Assert.AreEqual(TerrainInfo.DefaultMaxElevation, info.MaxElevation);
        }
    }
}
