using System;
using UnityEngine;

namespace Mapbox.ImageModule.Terrain.Settings
{
	/// <summary>
	/// Grid density for the terrain render mesh. The value chosen here propagates to the
	/// collider and CPU elevation sampling logic, so changing it affects both visuals and
	/// physics resolution.
	/// </summary>
	[Serializable]
	public class ElevationModificationOptions
	{
		[Tooltip("Divisor applied to 128 to determine terrain render-mesh grid density. Pick from the dropdown: 1 = finest (129x129 verts, heaviest), 128 = coarsest (2x2 verts, fastest). Very coarse values show visible seams; the inspector warns you. Default: 1.")]
		[SimplificationFactor(1, 128)]
		public int SimplificationFactor = 1;

		/// <summary>
		/// Convenience accessor: number of grid segments per tile side. Equals
		/// <c>128 / SimplificationFactor</c>. Vertex count per side is <c>sampleCount + 1</c>.
		/// </summary>
		public int sampleCount => 128 / SimplificationFactor;
	}
}
