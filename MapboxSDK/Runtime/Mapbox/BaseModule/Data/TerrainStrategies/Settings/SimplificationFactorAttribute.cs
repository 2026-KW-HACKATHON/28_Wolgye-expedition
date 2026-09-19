using UnityEngine;

namespace Mapbox.ImageModule.Terrain.Settings
{
	/// <summary>
	/// Marks an <see cref="int"/> field as a terrain grid-simplification factor. In the
	/// Inspector this renders as a dropdown of presets (each halving the grid resolution)
	/// plus a live help box that shows the resulting grid size and warns at coarse values.
	/// Legacy non-preset values are snapped to the nearest preset on first inspector render.
	/// The drawer lives in the editor assembly.
	/// </summary>
	public class SimplificationFactorAttribute : PropertyAttribute
	{
		/// <summary>Smallest allowed factor value (finest grid).</summary>
		public readonly int Min;

		/// <summary>Largest allowed factor value (coarsest grid).</summary>
		public readonly int Max;

		/// <summary>Number divided by the factor to produce the sample (segment) count. Usually 128.</summary>
		public readonly int VertexBase;

		/// <param name="min">Smallest allowed factor value.</param>
		/// <param name="max">Largest allowed factor value.</param>
		/// <param name="vertexBase">Dividend for sample-count math. Usually <c>128</c>.</param>
		public SimplificationFactorAttribute(int min, int max, int vertexBase = 128)
		{
			Min = min;
			Max = max;
			VertexBase = vertexBase;
		}
	}
}
