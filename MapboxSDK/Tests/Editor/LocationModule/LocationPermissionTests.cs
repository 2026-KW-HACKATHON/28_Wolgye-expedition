using Mapbox.BaseModule.Unity;
using NUnit.Framework;

namespace Mapbox.LocationModule.Tests
{
    [TestFixture]
    public class LocationPermissionTests
    {
        #region LocationPermissionState Tests

        [Test]
        public void LocationPermissionState_HasExpectedValues()
        {
            // Assert - Verify enum has all expected values
            Assert.IsTrue(System.Enum.IsDefined(typeof(LocationPermissionState), LocationPermissionState.Waiting));
            Assert.IsTrue(System.Enum.IsDefined(typeof(LocationPermissionState), LocationPermissionState.Granted));
            Assert.IsTrue(System.Enum.IsDefined(typeof(LocationPermissionState), LocationPermissionState.Denied));
            Assert.IsTrue(System.Enum.IsDefined(typeof(LocationPermissionState), LocationPermissionState.DeniedPermanently));
        }

        [Test]
        public void LocationPermissionState_DeniedAndDeniedPermanentlyAreDifferent()
        {
            // Assert
            Assert.AreNotEqual(
                LocationPermissionState.Denied,
                LocationPermissionState.DeniedPermanently,
                "Denied and DeniedPermanently should be distinct states");
        }

        #endregion

        #region UnityContext Tests

        [Test]
        public void UnityContext_DefaultState_IsWaiting()
        {
            // Arrange & Act
            var context = new UnityContext();

            // Assert
            Assert.AreEqual(LocationPermissionState.Waiting, context.LocationPermissionState);
        }

        [Test]
        public void UnityContext_MapRootCanBeSet()
        {
            // Arrange
            var context = new UnityContext();
            var gameObject = new UnityEngine.GameObject("TestMapRoot");

            // Act
            context.MapRoot = gameObject.transform;

            // Assert
            Assert.AreEqual(gameObject.transform, context.MapRoot);

            // Cleanup
            UnityEngine.Object.DestroyImmediate(gameObject);
        }

        #endregion

        #region LocationPermissionHandler Tests

        [Test]
        public void LocationPermissionHandler_InitialState_IsWaiting()
        {
            // Arrange & Act
            var handler = new LocationPermissionHandler();

            // Assert
            Assert.AreEqual(LocationPermissionState.Waiting, handler.State);
        }

        #endregion
    }
}
