using System;
using UnityEngine;

namespace Mapbox.LocationModule.MapboxLocation
{
    [Serializable]
    public class MapboxLocationSettings
    {
        // The accuracy of the observed location
        [Tooltip("The accuracy of the observed location")]
        public MapboxLocationAccuracyLevel AccuracyLevel;
        
        // Minimum displacement between location updates in meters.
        [Tooltip("Minimum displacement between location updates in meters.")]
        public float Displacement;
        
        /// <summary>
        /// The fastest rate at which the application will receive location updates, which might be faster than the `Interval`.
        /// Unlike `Interval` this parameter is exact.
        /// </summary>
        [Tooltip("The fastest rate at which the application will receive location updates, which might be faster than the `Interval`. Unlike `Interval` this parameter is exact.")]
        public long MinimumInterval;
        /// <summary>
        /// Maximum wait time for location updates. If it's at least 2x larger then `Interval`, then location delivery may be delayed and multiple locations can be delivered at once.
        /// </summary>
        [Tooltip("Maximum wait time for location updates. If it's at least 2x larger then `Interval`, then location delivery may be delayed and multiple locations can be delivered at once.")]
        public long MaximumInterval;
        /// <summary>
        /// Desired interval for active location updates.
        /// </summary>
        [Tooltip("Desired interval for active location updates.")]
        public long Interval;
    }

    public enum MapboxLocationAccuracyLevel
    {
        Passive,
        // Low accuracy requirement (typically greater than 500 meters).
        Low,
        // Medium accuracy requirement (typically between 100 and 500 meters). 
        Medium,
        // High accuracy requirement.
        High,
        //The highest possible accuracy requirement that uses additional sensors (if possible) to facilitate navigation use case. 
        Highest
    }
    
    /// <summary>
    /// Location permissions granted by user to the app.
    /// Maps to MBXPermissionStatus from MapboxCommon.
    /// </summary>
    public enum MapboxLocationServiceStatus
    {
        /// <summary>Access to location is not allowed.</summary>
        Denied = 0,
        /// <summary>
        /// Access to location is allowed.
        /// This type of permission is defined for platforms that
        /// do not have foreground/background access granularity.
        /// </summary>
        Granted = 1,
        /// <summary>Access to location is allowed only while an app is in use.</summary>
        Foreground = 2,
        /// <summary>Access to location is allowed all the time.</summary>
        Background = 3
    }

    /// <summary>
    /// Accuracy authorization granted by user to the app.
    /// Maps to MBXAccuracyAuthorization from MapboxCommon.
    /// </summary>
    public enum AccuracyAuthorization
    {
        /// <summary>An app is not authorized to access location.</summary>
        None = 0,
        /// <summary>An app is authorized to received as precise as possible location.</summary>
        Exact = 1,
        /// <summary>
        /// An app is authorized to receive rough location only.
        /// Depends on a platform the accuracy is within a city block.
        /// </summary>
        Inexact = 2
    }
}