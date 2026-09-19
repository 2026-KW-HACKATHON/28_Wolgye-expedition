//-----------------------------------------------------------------------
// <copyright file="GeocodeResponse.cs" company="Mapbox">
//     Copyright (c) 2016 Mapbox. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Mapbox.GeocodingApi
{
	[Serializable]
	public abstract class GeocodeResponse
	{
		/// <summary>
		/// Gets or sets the type.
		/// </summary>
		/// <value>The type.</value>
		[JsonProperty("type", Order = 0)]
		public string Type;

		/// <summary>
		/// Gets or sets the features.
		/// </summary>
		/// <value>The features.</value>
		[JsonProperty("features", Order = 2)]
		public List<Feature> Features;

		/// <summary>
		/// Gets or sets the attribution.
		/// </summary>
		/// <value>The attribution.</value>
		[JsonProperty("attribution", Order = 3)]
		public string Attribution;
	}
	
	[Serializable]
	public class ReverseGeocodeResponse : GeocodeResponse
	{
		/// <summary>
		/// Gets or sets the query.
		/// </summary>
		/// <value>The query.</value>
		[JsonProperty("query", Order = 1)]
		public List<double> Query;
	}

	[Serializable]
	public class ForwardGeocodeResponse : GeocodeResponse
	{
		/// <summary>
		/// Gets or sets the query.
		/// </summary>
		/// <value>The query.</value>
		[JsonProperty("query", Order = 1)]
		public List<string> Query;
	}
}