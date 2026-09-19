//-----------------------------------------------------------------------
// <copyright file="ReverseGeocodeResource.cs" company="Mapbox">
//     Copyright (c) 2016 Mapbox. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using Mapbox.BaseModule.Data.Vector2d;
using Mapbox.BaseModule.Utilities;

namespace Mapbox.GeocodingApi
{
    using System.Collections.Generic;

    /// <summary> A reverse geocode request. </summary>
    public sealed class ReverseGeocodeResource : GeocodeResource<LatitudeLongitude>
	{
		// Required
		private LatitudeLongitude query;

		/// <summary> Initializes a new instance of the <see cref="ReverseGeocodeResource" /> class.</summary>
		/// <param name="query"> Location to reverse geocode. </param>
		public ReverseGeocodeResource(LatitudeLongitude query)
		{
			this.Query = query;
		}

		/// <summary> Gets or sets the location. </summary>
		public override LatitudeLongitude Query {
			get {
				return this.query;
			}

			set {
				this.query = value;
			}
		}

		/// <summary> Builds a complete reverse geocode URL string. </summary>
		/// <returns> A complete, valid reverse geocode URL string. </returns>
		public override string GetUrl()
		{
			Dictionary<string, string> opts = new Dictionary<string, string>();

			if (this.Types != null)
			{
				opts.Add("types", GetUrlQueryFromArray(this.Types));
			}

			return Constants.Map.BaseAPI +
							this.ApiEndpoint +
							this.Mode +
							this.Query.ToStringLonLat() +
							".json" +
							EncodeQueryString(opts);
		}
	}
}