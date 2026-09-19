//-----------------------------------------------------------------------
// <copyright file="Route.cs" company="Mapbox">
//     Copyright (c) 2016 Mapbox. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Mapbox.BaseModule.Data.Vector2d;
using Mapbox.BaseModule.Utilities.JsonConverters;
using Newtonsoft.Json;

namespace Mapbox.DirectionsApi.Response
{
	/// <summary>
    /// A Route from a Directions API call.
    /// </summary>
    [Serializable]
	public class Route
	{
		/// <summary>
		/// Gets or sets the legs.
		/// </summary>
		/// <value>The legs.</value>
		[JsonProperty("legs")] 
		public List<Leg> Legs;

		/// <summary>
		/// Gets or sets the geometry. Polyline is an array of LatLng's.
		/// </summary>
		/// <value>The geometry.</value>
		[JsonProperty("geometry")] 
		[JsonConverter(typeof(PolylineToVector2dListConverter))]
		public List<Vector2d> Geometry;

		/// <summary>
		/// Gets or sets the duration.
		/// </summary>
		/// <value>The duration.</value>
		[JsonProperty("duration")] 
		public double Duration;

		/// <summary>
		/// Gets or sets the distance.
		/// </summary>
		/// <value>The distance.</value>
		[JsonProperty("distance")] 
		public double Distance;

		/// <summary>
		/// Float indicating the weight in units described by 'weight_name'.
		/// </summary>
		[JsonProperty("weight")] 
		public float Weight;

		/// <summary>
		/// String indicating which weight was used. The default is routability which is duration based, with additional penalties for less desirable maneuvers.
		/// </summary>
		[JsonProperty("weight_name")] 
		public string WeightName;

	}
}