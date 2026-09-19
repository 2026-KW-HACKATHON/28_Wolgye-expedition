using System;

namespace Mapbox.BaseModule.Map
{
    /// <summary>
    /// Runtime terrain elevation information for the current map view.
    /// Updated automatically as terrain tiles load and unload.
    /// </summary>
    [Serializable]
    public class TerrainInfo
    {
        // Conservative defaults used on the first frame before any terrain tile is
        // sampled, and again whenever ActiveTiles drops to zero. The MaxElevation
        // floor keeps the tile-provider's bounds-height AABB tall enough to admit
        // plausible mountain ranges (above Mt. Whitney) so steep terrain isn't
        // frustum-culled on first paint.
        public const float DefaultMinElevation = 0f;
        public const float DefaultMaxElevation = 5000f;

        /// <summary>
        /// Lowest observed elevation in meters across currently loaded terrain tiles.
        /// Initialized to <see cref="DefaultMinElevation"/>.
        /// </summary>
        public float MinElevation = DefaultMinElevation;

        /// <summary>
        /// Highest observed elevation in meters across currently loaded terrain tiles.
        /// Initialized to <see cref="DefaultMaxElevation"/>.
        /// </summary>
        public float MaxElevation = DefaultMaxElevation;
    }
}
