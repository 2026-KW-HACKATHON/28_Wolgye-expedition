using System;
using Mapbox.BaseModule.Utilities.Attributes;
using UnityEngine;

namespace Mapbox.ImageModule.Terrain.Settings
{
	/// <summary>
	/// Unity Layer assignment for terrain tile GameObjects. Useful when you want to apply a
	/// custom raycast mask, lighting layer, or camera culling layer to terrain.
	/// </summary>
	[Serializable]
	public class UnityLayerOptions
	{
		[Tooltip("When enabled, every terrain tile GameObject is moved to the Unity Layer selected below. Use this to carve terrain out of a raycast mask or put it on a dedicated lighting/culling layer. Default: off.")]
		public bool addToLayer = false;

		[GameObjectLayer]
		[Tooltip("Unity Layer that terrain tiles are assigned to when Add To Layer is enabled. The dropdown shows layers defined in your Project's Tags & Layers settings.")]
		public int layerId = 0;
	}
}
