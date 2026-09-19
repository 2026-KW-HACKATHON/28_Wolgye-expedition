using System;
using Mapbox.BaseModule.Utilities;
using UnityEngine;

namespace Mapbox.ImageModule.Terrain.Settings
{
	/// <summary>
	/// Bundle of sub-option groups consumed by <see cref="TerrainStrategy"/> implementations
	/// to build per-tile terrain geometry. Passed through
	/// <c>TerrainStrategy.Initialize</c>.
	/// </summary>
	[Serializable]
	public class ElevationLayerProperties
	{
		[Tooltip("Controls the terrain render mesh grid density via a simplification factor. Expand to pick a quality preset.")]
		public ElevationModificationOptions modificationOptions = new ElevationModificationOptions();

		[Tooltip("Optional Unity layer assignment for every terrain tile GameObject (useful for culling, raycast masks, lighting).")]
		public UnityLayerOptions unityLayerOptions = new UnityLayerOptions();

		[Tooltip("Optional downward side-wall skirts around each tile that hide horizontal gaps between neighboring tiles at different elevations.")]
		public TerrainSideWallOptions sideWallOptions = new TerrainSideWallOptions();

		[Tooltip("Optional MeshCollider generation for terrain tiles so physics can interact with the map surface.")]
		public TerrainColliderOptions colliderOptions = new TerrainColliderOptions();

		[Tooltip("World-unit size of a single terrain tile quad along X and Z. Default 1 matches the map's internal tile scale; change only if you know why.")]
		public float TileMeshSize = 1;
	}
}
