using Mapbox.LocationModule;
using Mapbox.LocationModule.AngleSmoothing;
using NUnit.Framework;

namespace Mapbox.LocationModule.Tests
{
    /// <summary>
    /// NOTE: UnityLocationProvider requires Unity's runtime (MonoBehaviour, coroutines) to function.
    /// These EditMode tests are limited to testing settings and data structures.
    /// Full integration tests for UnityLocationProvider would need to be PlayMode tests.
    /// </summary>
    [TestFixture]
    public class UnityLocationProviderTests
    {
        #region Settings Tests

        [Test]
        public void UnityLocationProviderSettings_DefaultValues()
        {
            // Act
            var settings = new UnityLocationProviderSettings();

            // Assert
            Assert.IsNotNull(settings);
            Assert.AreEqual(1.0f, settings.DesiredAccuracyInMeters);
            Assert.AreEqual(0.0f, settings.UpdateDistanceInMeters);
            Assert.AreEqual(500, settings.UpdateTimeInMilliSeconds);
        }

        [Test]
        public void UnityLocationProviderSettings_CanSetDesiredAccuracy()
        {
            // Arrange
            var settings = new UnityLocationProviderSettings();

            // Act
            settings.DesiredAccuracyInMeters = 25.5f;

            // Assert
            Assert.AreEqual(25.5f, settings.DesiredAccuracyInMeters);
        }

        [Test]
        public void UnityLocationProviderSettings_CanSetUpdateDistance()
        {
            // Arrange
            var settings = new UnityLocationProviderSettings();

            // Act
            settings.UpdateDistanceInMeters = 10.0f;

            // Assert
            Assert.AreEqual(10.0f, settings.UpdateDistanceInMeters);
        }

        [Test]
        public void UnityLocationProviderSettings_CanSetUpdateTime()
        {
            // Arrange
            var settings = new UnityLocationProviderSettings();

            // Act
            settings.UpdateTimeInMilliSeconds = 1000;

            // Assert
            Assert.AreEqual(1000, settings.UpdateTimeInMilliSeconds);
        }

        [Test]
        public void UnityLocationProviderSettings_CanSetSmoothingStrategies()
        {
            // Arrange
            var settings = new UnityLocationProviderSettings();
            var headingSmoothing = new AngleSmoothingNoOp();
            var orientationSmoothing = new AngleSmoothingNoOp();

            // Act
            settings.UserHeadingSmoothing = headingSmoothing;
            settings.DeviceOrientationSmoothing = orientationSmoothing;

            // Assert
            Assert.AreEqual(headingSmoothing, settings.UserHeadingSmoothing);
            Assert.AreEqual(orientationSmoothing, settings.DeviceOrientationSmoothing);
        }

        #endregion
    }
}
