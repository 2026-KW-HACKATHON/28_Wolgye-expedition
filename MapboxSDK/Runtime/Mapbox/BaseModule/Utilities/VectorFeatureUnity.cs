using System.Collections.Generic;
using Mapbox.BaseModule.Data.Tiles;
using Mapbox.VectorTile;
using UnityEngine;

namespace Mapbox.BaseModule.Utilities
{
	public class VectorFeatureUnity
	{
		public CanonicalTileId TileId;
		public VectorTileFeature Data;
		public Dictionary<string, object> Properties;
		
		/// <summary>
		/// topLeft is (0.0) bottomRight is (1,-1).
		/// so it follow unity world space coordinates.
		/// </summary>
		public List<List<Vector3>> Points = new List<List<Vector3>>();
	}
}
