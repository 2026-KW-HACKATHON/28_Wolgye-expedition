using Mapbox.BaseModule.Data.Vector2d;
using Mapbox.LocationModule;
using NUnit.Framework;

namespace Mapbox.LocationModule.Tests
{
    [TestFixture]
    public class LocationDataTests
    {
        #region Location Tests

        [Test]
        public void Location_DefaultConstructor_InitializesWithDefaults()
        {
            // Act - Location is a struct, so default constructor sets all fields to default values
            var location = new Location();

            // Assert
            Assert.AreEqual(0, location.LatitudeLongitude.Latitude);
            Assert.AreEqual(0, location.LatitudeLongitude.Longitude);
            Assert.AreEqual(0f, location.Accuracy);
            Assert.AreEqual(0.0, location.Timestamp);
            Assert.IsFalse(location.IsLocationUpdated);
            Assert.IsFalse(location.IsLocationServiceEnabled);
        }

        [Test]
        public void Location_SetLatitudeLongitude_UpdatesValue()
        {
            // Arrange
            var location = new Location();
            var latLng = new LatitudeLongitude(37.7749, -122.4194);

            // Act
            location.LatitudeLongitude = latLng;

            // Assert
            Assert.AreEqual(37.7749, location.LatitudeLongitude.Latitude, 0.0001);
            Assert.AreEqual(-122.4194, location.LatitudeLongitude.Longitude, 0.0001);
        }

        [Test]
        public void Location_Accuracy_CanBeSet()
        {
            // Arrange
            var location = new Location();

            // Act
            location.Accuracy = 15.5f;

            // Assert
            Assert.AreEqual(15.5f, location.Accuracy);
        }

        [Test]
        public void Location_Timestamp_CanBeSet()
        {
            // Arrange
            var location = new Location();
            double timestamp = 1234567890.0;

            // Act
            location.Timestamp = timestamp;

            // Assert
            Assert.AreEqual(timestamp, location.Timestamp);
        }

        [Test]
        public void Location_IsLocationUpdated_DefaultsToFalse()
        {
            // Arrange & Act
            var location = new Location();

            // Assert
            Assert.IsFalse(location.IsLocationUpdated);
        }

        [Test]
        public void Location_IsLocationServiceEnabled_CanBeSet()
        {
            // Arrange
            var location = new Location();

            // Act
            location.IsLocationServiceEnabled = true;

            // Assert
            Assert.IsTrue(location.IsLocationServiceEnabled);
        }

        [Test]
        public void Location_UserHeading_CanBeSet()
        {
            // Arrange
            var location = new Location();

            // Act
            location.UserHeading = 45.5f;

            // Assert
            Assert.AreEqual(45.5f, location.UserHeading);
        }

        [Test]
        public void Location_DeviceOrientation_CanBeSet()
        {
            // Arrange
            var location = new Location();

            // Act
            location.DeviceOrientation = 90.0f;

            // Assert
            Assert.AreEqual(90.0f, location.DeviceOrientation);
        }

        #endregion

        #region LatitudeLongitude Tests

        [Test]
        public void LatitudeLongitude_Constructor_SetsValues()
        {
            // Act
            var latLng = new LatitudeLongitude(37.7749, -122.4194);

            // Assert
            Assert.AreEqual(37.7749, latLng.Latitude, 0.0001);
            Assert.AreEqual(-122.4194, latLng.Longitude, 0.0001);
        }

        [Test]
        public void LatitudeLongitude_Equals_SameValues_ReturnsTrue()
        {
            // Arrange
            var latLng1 = new LatitudeLongitude(37.7749, -122.4194);
            var latLng2 = new LatitudeLongitude(37.7749, -122.4194);

            // Act & Assert
            Assert.That(latLng1.Equals(latLng2), Is.True);
        }

        [Test]
        public void LatitudeLongitude_Equals_DifferentLatitude_ReturnsFalse()
        {
            // Arrange
            var latLng1 = new LatitudeLongitude(37.7749, -122.4194);
            var latLng2 = new LatitudeLongitude(37.7750, -122.4194);

            // Act & Assert
            Assert.That(latLng1.Equals(latLng2), Is.False);
        }

        [Test]
        public void LatitudeLongitude_Equals_DifferentLongitude_ReturnsFalse()
        {
            // Arrange
            var latLng1 = new LatitudeLongitude(37.7749, -122.4194);
            var latLng2 = new LatitudeLongitude(37.7749, -122.4195);

            // Act & Assert
            Assert.That(latLng1.Equals(latLng2), Is.False);
        }

        [Test]
        public void LatitudeLongitude_ZeroZero_IsValid()
        {
            // Act - 0,0 is Gulf of Guinea, a valid location
            var latLng = new LatitudeLongitude(0, 0);

            // Assert
            Assert.AreEqual(0, latLng.Latitude, 0.0001);
            Assert.AreEqual(0, latLng.Longitude, 0.0001);
            Assert.That(latLng.IsValid(), Is.True);
        }

        [Test]
        public void LatitudeLongitude_ExtremeValues_AreValid()
        {
            // Act - Test near poles and date line
            var northPole = new LatitudeLongitude(90, 0);
            var southPole = new LatitudeLongitude(-90, 0);
            var dateLineWest = new LatitudeLongitude(0, -180);
            var dateLineEast = new LatitudeLongitude(0, 180);

            // Assert
            Assert.AreEqual(90, northPole.Latitude, 0.0001);
            Assert.AreEqual(-90, southPole.Latitude, 0.0001);
            Assert.AreEqual(-180, dateLineWest.Longitude, 0.0001);
            Assert.AreEqual(180, dateLineEast.Longitude, 0.0001);

            Assert.That(northPole.IsValid(), Is.True);
            Assert.That(southPole.IsValid(), Is.True);
            Assert.That(dateLineWest.IsValid(), Is.True);
            Assert.That(dateLineEast.IsValid(), Is.True);
        }

        #endregion

        #region StaticLocationProvider Tests

        [Test]
        public void StaticLocationProvider_StringConstructor_ParsesValidCoordinates()
        {
            // Arrange & Act
            var provider = new StaticLocationProvider("37.7749,-122.4194");

            // Assert
            Assert.AreEqual(37.7749, provider.CurrentLocation.LatitudeLongitude.Latitude, 0.001);
            Assert.AreEqual(-122.4194, provider.CurrentLocation.LatitudeLongitude.Longitude, 0.001);
        }

        [Test]
        public void StaticLocationProvider_LatLngConstructor_SetsLocation()
        {
            // Arrange & Act
            var latLng = new LatitudeLongitude(37.7749, -122.4194);
            var provider = new StaticLocationProvider(latLng);

            // Assert
            Assert.AreEqual(37.7749, provider.CurrentLocation.LatitudeLongitude.Latitude, 0.001);
            Assert.AreEqual(-122.4194, provider.CurrentLocation.LatitudeLongitude.Longitude, 0.001);
        }

        [Test]
        public void StaticLocationProvider_AfterSendLocationEvent_IsEnabled()
        {
            // Arrange
            var provider = new StaticLocationProvider("37.7749,-122.4194");

            // Act
            provider.SendLocationEvent();

            // Assert
            Assert.That(provider.CurrentLocation.IsLocationServiceEnabled, Is.True);
        }

        #endregion

        #region ILocationProvider Tests

        [Test]
        public void AbstractLocationProvider_OnLocationUpdated_EventCanBeSubscribed()
        {
            // Arrange
            var provider = new StaticLocationProvider("37.7749,-122.4194");
            bool eventRaised = false;

            provider.OnLocationUpdated += (location) =>
            {
                eventRaised = true;
            };

            // Act - StaticLocationProvider requires SendLocationEvent() to trigger the event
            provider.SendLocationEvent();

            // Assert
            Assert.That(eventRaised, Is.True);
        }

        #endregion
    }
}
