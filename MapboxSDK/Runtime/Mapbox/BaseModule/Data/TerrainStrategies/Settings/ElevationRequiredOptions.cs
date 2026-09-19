using System;
using UnityEngine;

namespace Mapbox.ImageModule.Terrain.Settings
{
	[Serializable]
	public class ElevationRequiredOptions
	{
		[Range(0, 100)]
		[Tooltip("Multiplication factor to vertically exaggerate elevation on terrain, does not work with Flat Terrain.")]
		public float exaggerationFactor = 1;
	}
}
