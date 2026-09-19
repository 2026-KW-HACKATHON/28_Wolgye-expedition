//-----------------------------------------------------------------------
// <copyright file="Geometry.cs" company="Mapbox">
//     Copyright (c) 2016 Mapbox. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using Mapbox.BaseModule.Data.Vector2d;
using Mapbox.BaseModule.Utilities.JsonConverters;
using Newtonsoft.Json;
using System;

namespace Mapbox.GeocodingApi
{
    [Serializable]
	public class Geometry {
		/// <summary>
		///     Gets or sets type. Geocode results will always be type: point.
		/// </summary>
		/// <value>The GeoJSON geometry type.</value>
		[JsonProperty("type")]
		public string Type;

		/// <summary>
		///     Gets or sets coordinates. Because they are points, Geocode results will always be  a single Geocoordinate.
		/// </summary>
		/// <value>The coordinates.</value>
		[JsonConverter(typeof(LonLatToVector2dConverter))]
		[JsonProperty("coordinates")]
		public Vector2d Coordinates;
	}
}
