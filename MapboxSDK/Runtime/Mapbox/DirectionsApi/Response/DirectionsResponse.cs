//-----------------------------------------------------------------------
// <copyright file="DirectionsResponse.cs" company="Mapbox">
//     Copyright (c) 2016 Mapbox. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Mapbox.DirectionsApi.Response
{
	[Serializable]
	public class DirectionsResponse
	{
		/// <summary>
		/// Gets or sets the routes.
		/// </summary>
		/// <value>The routes.</value>
		[JsonProperty("routes")]
		public List<Route> Routes;

		/// <summary>
		/// Gets or sets the waypoints.
		/// </summary>
		/// <value>The waypoints.</value>
		[JsonProperty("waypoints")]
		public List<Waypoint> Waypoints;

		/// <summary>
		/// Gets or sets the code.
		/// </summary>
		/// <value>The code.</value>
		[JsonProperty("code")] 
		public string Code;
	}
}