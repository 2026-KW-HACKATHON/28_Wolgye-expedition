//-----------------------------------------------------------------------
// <copyright file="Leg.cs" company="Mapbox">
//     Copyright (c) 2016 Mapbox. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using Newtonsoft.Json;

namespace Mapbox.DirectionsApi.Response
{
	/// <summary>
	/// <para>An annotations object contains additional details about each line segment along the route geometry.</para>
	/// <para></para>Each entry in an annotations field corresponds to a coordinate along the route geometry.
	/// </summary>
	[Serializable]
	public class Annotation
	{
		[JsonProperty("distance")] 
		public double[] Distance;
		
		[JsonProperty("duration")] 
		public double[] Duration;
		
		[JsonProperty("speed")] 
		public string[] Speed;
		
		[JsonProperty("congestion")] 
		public string[] Congestion;
	}
}
