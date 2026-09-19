//-----------------------------------------------------------------------
// <copyright file="Directions.cs" company="Mapbox">
//     Copyright (c) 2016 Mapbox. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Text;
using Mapbox.BaseModule.Data.Platform;
using Mapbox.BaseModule.Utilities.JsonConverters;
using Mapbox.DirectionsApi;
using Mapbox.DirectionsApi.Response;
using Newtonsoft.Json;

namespace Mapbox.DirectionsApi
{
	/// <summary>
    ///     Wrapper around the <see href="https://www.mapbox.com/api-documentation/navigation/#directions">
    ///     Mapbox Directions API</see>. The Mapbox Directions API will show you how to get where
    ///     you're going.
    /// </summary>
    public sealed class MapboxDirectionsApi
	{
		private readonly IFileSource _fileSource;
		
		/// <summary> Initializes a new instance of the <see cref="MapboxDirectionsApi" /> class. </summary>
		/// <param name="fileSource"> Data source abstraction. </param>
		public MapboxDirectionsApi(IFileSource fileSource)
		{
			_fileSource = fileSource;
		}

		/// <summary> Performs asynchronously a directions lookup. </summary>
		/// <param name="direction"> Direction resource. </param>
		/// <param name="callback"> Callback to be called after the request is completed. </param>
		/// <returns>
		///     Returns a <see cref="IAsyncRequest" /> that can be used for canceling a pending
		///     request. This handle can be completely ignored if there is no intention of ever
		///     canceling the request.
		/// </returns>
		public IAsyncRequest Query(DirectionResource direction, Action<DirectionsResponse> callback)
		{
			return _fileSource.Request(
				direction.GetUrl(),
				response =>
				{
					var str = Encoding.UTF8.GetString(response.Data);

					var data = Deserialize(str);

					callback(data);
				});
		}

		/// <summary>
		/// Deserialize the geocode response string into a <see cref="DirectionsResponse"/>.
		/// </summary>
		/// <param name="str">JSON String.</param>
		/// <returns>A <see cref="DirectionsResponse"/>.</returns>
		private DirectionsResponse Deserialize(string str)
		{
			return JsonConvert.DeserializeObject<DirectionsResponse>(str, JsonConverters.Converters);
		}

		public string Serialize(DirectionsResponse response)
		{
			return JsonConvert.SerializeObject(response, JsonConverters.Converters);
		}

	}
}
