using Mapbox.LocationModule.MapboxLocation;
using Mapbox.LocationModule;
using NUnit.Framework;
using System;

namespace Mapbox.LocationModule.Tests
{
    [TestFixture]
    public class MapboxLocationProviderTests
    {
        private MapboxLocationSettings _settings;

        [SetUp]
        public void SetUp()
        {
            _settings = new MapboxLocationSettings
            {
                AccuracyLevel = MapboxLocationAccuracyLevel.High,
                Displacement = 10f,
                Interval = 1000,
                MinimumInterval = 500,
                MaximumInterval = 5000
            };
        }

        #region Constructor Tests

        [Test]
        public void Constructor_WithValidSettings_CreatesProvider()
        {
            // Act
            var provider = new MapboxLocationProvider(_settings);

            // Assert
            Assert.IsNotNull(provider);
        }

        [Test]
        public void Constructor_InEditor_DoesNotThrow()
        {
            // In editor, _mapboxDeviceLocation will be null since platform #if guards prevent instantiation
            // The null guard added in our fix should prevent crashes
            // Act & Assert
            Assert.DoesNotThrow(() => new MapboxLocationProvider(_settings));
        }

        #endregion

        #region Update Tests

        [Test]
        public void Update_WithNullDeviceLocation_DoesNotThrow()
        {
            // Arrange
            var provider = new MapboxLocationProvider(_settings);
            // In editor, _mapboxDeviceLocation is null

            // Act & Assert
            Assert.DoesNotThrow(() => provider.Update());
        }

        #endregion

        #region OnDestroy Tests

        [Test]
        public void OnDestroy_WithNullDeviceLocation_DoesNotThrow()
        {
            // Arrange
            var provider = new MapboxLocationProvider(_settings);
            // In editor, _mapboxDeviceLocation is null

            // Act & Assert
            Assert.DoesNotThrow(() => provider.OnDestroy());
        }

        #endregion

        #region CurrentLocation Tests

        [Test]
        public void CurrentLocation_InitialState_ReturnsDefaultLocation()
        {
            // Arrange
            var provider = new MapboxLocationProvider(_settings);

            // Act
            var location = provider.CurrentLocation;

            // Assert
            Assert.AreEqual(0, location.LatitudeLongitude.Latitude);
            Assert.AreEqual(0, location.LatitudeLongitude.Longitude);
        }

        #endregion
    }

    [TestFixture]
    public class MapboxLocationSettingsTests
    {
        #region Settings Tests

        [Test]
        public void MapboxLocationSettings_DefaultValues()
        {
            // Act
            var settings = new MapboxLocationSettings();

            // Assert
            Assert.IsNotNull(settings);
        }

        [Test]
        public void MapboxLocationSettings_AccuracyLevel_CanBeSet()
        {
            // Arrange
            var settings = new MapboxLocationSettings();

            // Act
            settings.AccuracyLevel = MapboxLocationAccuracyLevel.Highest;

            // Assert
            Assert.AreEqual(MapboxLocationAccuracyLevel.Highest, settings.AccuracyLevel);
        }

        [Test]
        public void MapboxLocationSettings_Displacement_CanBeSet()
        {
            // Arrange
            var settings = new MapboxLocationSettings();

            // Act
            settings.Displacement = 25.5f;

            // Assert
            Assert.AreEqual(25.5f, settings.Displacement);
        }

        #endregion
    }

    [TestFixture]
    public class AccuracyAuthorizationTests
    {
        #region Enum Tests

        [Test]
        public void AccuracyAuthorization_HasExpectedValues()
        {
            // Assert - Verify enum has expected values
            Assert.That(Enum.IsDefined(typeof(AccuracyAuthorization), AccuracyAuthorization.None), Is.True);
            Assert.That(Enum.IsDefined(typeof(AccuracyAuthorization), AccuracyAuthorization.Exact), Is.True);
            Assert.That(Enum.IsDefined(typeof(AccuracyAuthorization), AccuracyAuthorization.Inexact), Is.True);
        }

        #endregion
    }

    [TestFixture]
    public class MapboxLocationServiceStatusTests
    {
        #region Enum Tests

        [Test]
        public void MapboxLocationServiceStatus_HasExpectedValues()
        {
            // Assert - Verify enum has expected status values
            Assert.That(Enum.IsDefined(typeof(MapboxLocationServiceStatus), MapboxLocationServiceStatus.Denied), Is.True);
            Assert.That(Enum.IsDefined(typeof(MapboxLocationServiceStatus), MapboxLocationServiceStatus.Granted), Is.True);
            Assert.That(Enum.IsDefined(typeof(MapboxLocationServiceStatus), MapboxLocationServiceStatus.Foreground), Is.True);
            Assert.That(Enum.IsDefined(typeof(MapboxLocationServiceStatus), MapboxLocationServiceStatus.Background), Is.True);
        }

        #endregion
    }

    [TestFixture]
    public class MapboxLocationAccuracyLevelTests
    {
        #region Enum Tests

        [Test]
        public void MapboxLocationAccuracyLevel_HasExpectedValues()
        {
            // Assert - Verify enum has all accuracy levels
            Assert.That(Enum.IsDefined(typeof(MapboxLocationAccuracyLevel), MapboxLocationAccuracyLevel.Passive), Is.True);
            Assert.That(Enum.IsDefined(typeof(MapboxLocationAccuracyLevel), MapboxLocationAccuracyLevel.Low), Is.True);
            Assert.That(Enum.IsDefined(typeof(MapboxLocationAccuracyLevel), MapboxLocationAccuracyLevel.Medium), Is.True);
            Assert.That(Enum.IsDefined(typeof(MapboxLocationAccuracyLevel), MapboxLocationAccuracyLevel.High), Is.True);
            Assert.That(Enum.IsDefined(typeof(MapboxLocationAccuracyLevel), MapboxLocationAccuracyLevel.Highest), Is.True);
        }

        #endregion
    }
}
