//-----------------------------------------------------------------------
// <copyright file="Leg.cs" company="Mapbox">
//     Copyright (c) 2016 Mapbox. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Mapbox.DirectionsApi.Response
{
	/// <summary>
	/// A Leg from a Directions API call.
	/// </summary>
	[Serializable]
	public class Leg
	{
		/// <summary>
		/// Depending on the steps parameter, either an Array of RouteStep objects (true, default) or an empty array (false)
		/// </summary>
		/// <value>The steps.</value>
		[JsonProperty("steps")] 
		public List<Step> Steps;


		/// <summary>
		/// Depending on the summary parameter, either a String summarizing the route (true, default) or an empty String (false).
		/// </summary>
		/// <value>The summary.</value>
		[JsonProperty("summary")] 
		public string Summary;


		/// <summary>
		/// Number indicating the estimated travel time in seconds.
		/// </summary>
		[JsonProperty("duration")] 
		public double Duration;


		/// <summary>
		/// Number indicating the distance traveled in meters.
		/// </summary>
		[JsonProperty("distance")] 
		public double Distance;


		/// <summary>
		/// An annotations object that contains additional details about each line segment along the route geometry. Each entry in an annotations field corresponds to a coordinate along the route geometry.
		/// </summary>
		[JsonProperty("annotation")] 
		public Annotation Annotation;


	}
}
