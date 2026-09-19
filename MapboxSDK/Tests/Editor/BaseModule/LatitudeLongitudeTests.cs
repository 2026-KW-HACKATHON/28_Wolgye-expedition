using System.Globalization;
using Mapbox.BaseModule.Data.Platform.Cache;
using Mapbox.BaseModule.Data.Vector2d;
using Mapbox.BaseModule.Map;
using Mapbox.BaseModule.Utilities;
using Mapbox.BaseModule.Utilities.JsonConverters;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Mapbox.BaseModuleTests
{
    public class LatitudeLongitudeTests
    {
        [Test]
        public void Constructor_SetsFieldsCorrectly()
        {
            var ll = new LatitudeLongitude(10.5, -20.25);
            Assert.AreEqual(10.5, ll.Latitude);
            Assert.AreEqual(-20.25, ll.Longitude);
        }

        [Test]
        public void Invalid_ReturnsValuesOutsideRange()
        {
            var invalid = LatitudeLongitude.Invalid;
            Assert.AreEqual(LatitudeLongitude.MAX_LATITUDE * 2, invalid.Latitude);
            Assert.AreEqual(LatitudeLongitude.MAX_LONGITUDE * 2, invalid.Longitude);
            Assert.IsFalse(invalid.IsValid());
        }

        [Test]
        public void ToString_ReturnsExpectedFormat()
        {
            var ll = new LatitudeLongitude(12.345, -67.89);
            var str = ll.ToString();
            Assert.AreEqual("12.345,-67.89", str);
        }

        [Test]
        public void ToStringLonLat_ReturnsFormattedWith5Decimals()
        {
            var ll = new LatitudeLongitude(45.1234567, -89.9876543);
            var str = ll.ToStringLonLat();
            Assert.AreEqual(
                string.Format(CultureInfo.InvariantCulture, "{0:F5},{1:F5}", -89.9876543, 45.1234567),
                str);
        }

        [Test]
        public void Equals_ReturnsTrueForEqualValues()
        {
            var a = new LatitudeLongitude(10, 20);
            var b = new LatitudeLongitude(10, 20);
            Assert.IsTrue(a.Equals(b));
            Assert.IsTrue(a == b);
            Assert.IsFalse(a != b);
        }

        [Test]
        public void Equals_ReturnsFalseForDifferentValues()
        {
            var a = new LatitudeLongitude(10, 20);
            var b = new LatitudeLongitude(10.0001, 20);
            Assert.IsFalse(a.Equals(b));
            Assert.IsFalse(a == b);
            Assert.IsTrue(a != b);
        }

        [Test]
        public void IsValid_ReturnsTrueForInRangeValues()
        {
            var ll = new LatitudeLongitude(45, -45);
            Assert.IsTrue(ll.IsValid());
        }

        [TestCase(91, 0)]   
        [TestCase(-91, 0)]
        [TestCase(0, 181)]  
        [TestCase(0, -181)] 
        public void IsValid_ReturnsFalseForOutOfRangeValues(double lat, double lon)
        {
            var ll = new LatitudeLongitude(lat, lon);
            Assert.IsFalse(ll.IsValid());
        }
        
        // Northern Hemisphere – Eastern Hemisphere
        [TestCase(51.5074, -0.1278)]    // London, UK
        [TestCase(48.8566, 2.3522)]     // Paris, France
        [TestCase(55.7558, 37.6173)]    // Moscow, Russia
        [TestCase(35.6895, 139.6917)]   // Tokyo, Japan
        [TestCase(39.9042, 116.4074)]   // Beijing, China

// Northern Hemisphere – Western Hemisphere
        [TestCase(38.9072, -77.0369)]   // Washington, D.C., USA
        [TestCase(45.4215, -75.6972)]   // Ottawa, Canada
        [TestCase(19.4326, -99.1332)]   // Mexico City, Mexico
        [TestCase(64.1466, -21.9426)]   // Reykjavik, Iceland

// Southern Hemisphere – Eastern Hemisphere
        [TestCase(-35.2809, 149.1300)]  // Canberra, Australia
        [TestCase(-25.7461, 28.1881)]   // Pretoria, South Africa
        [TestCase(-6.2088, 106.8456)]   // Jakarta, Indonesia
        [TestCase(-41.2865, 174.7762)]  // Wellington, New Zealand

// Southern Hemisphere – Western Hemisphere
        [TestCase(-34.6037, -58.3816)]  // Buenos Aires, Argentina
        [TestCase(-15.8267, -47.9218)]  // Brasília, Brazil
        [TestCase(-33.4489, -70.6693)]  // Santiago, Chile
        [TestCase(-12.0464, -77.0428)]  // Lima, Peru

// Near Equator (precision edge cases)
        [TestCase(0.3476, 32.5825)]     // Kampala, Uganda
        [TestCase(-0.1807, -78.4678)]   // Quito, Ecuador
        [TestCase(1.2921, 36.8219)]     // Nairobi, Kenya

// Extreme longitude tests (close to ±180)
        [TestCase(-9.4438, 147.1803)]   // Port Moresby, Papua New Guinea
        [TestCase(-13.8333, -171.7667)] // Apia, Samoa
        [TestCase(21.3069, -157.8583)]  // Honolulu, USA (near dateline influence)

// Extreme latitude tests
        [TestCase(64.9631, -19.0208)]   // Reykjavik (high north)
        [TestCase(-54.8019, -68.3030)]  // Ushuaia, Argentina (far south)
        public void IsValid_ReturnsTrueForCities(double lat, double lon)
        {
            var ll = new LatitudeLongitude(lat, lon);
            Assert.IsTrue(ll.IsValid());
        }
    }
}