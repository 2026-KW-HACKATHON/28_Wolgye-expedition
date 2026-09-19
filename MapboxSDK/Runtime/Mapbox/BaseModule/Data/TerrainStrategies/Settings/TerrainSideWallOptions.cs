using System;
using UnityEngine;

namespace Mapbox.ImageModule.Terrain.Settings
{
	/// <summary>
	/// Optional downward skirts extended slightly past each terrain tile's edges to hide
	/// horizontal gaps that appear between neighboring tiles when their elevations don't
	/// line up exactly (float precision, texture filtering, different zoom parents).
	/// </summary>
	[Serializable]
	public class TerrainSideWallOptions
	{
		[Tooltip("Enable downward skirts along each tile's four edges. Hides visible seams between tiles at different heights. A small visual overhead (~1% tile-size outer offset, a handful of extra triangles per tile). Default: off.")]
		public bool isActive = false;

		[Tooltip("How far down (in world units) the skirts hang below the terrain surface. Larger = taller walls that hide bigger seams but may peek out from under thin terrain features. Default 0.1. Typical range 0.05 - 1.")]
		public float wallHeight = .1f;
	}
}
