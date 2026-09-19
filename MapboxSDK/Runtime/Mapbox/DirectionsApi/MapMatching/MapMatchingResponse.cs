//-----------------------------------------------------------------------
// <copyright file="MapMatchingResponse.cs" company="Mapbox">
//     Copyright (c) 2016 Mapbox. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Newtonsoft.Json;

namespace Mapbox.DirectionsApi.MapMatching
{
	[Serializable]
	public class MapMatchingResponse
	{
		/// <summary>Simple constructor for deserialization </summary>
		public MapMatchingResponse() { }

		[JsonProperty("code")]
		public string Code;
		[JsonProperty("message")]
		public string Message;
		[JsonProperty("tracepoints")]
		public Tracepoint[] Tracepoints;
		[JsonProperty("matchings")]
		public MatchObject[] Matchings;

		public bool HasMatchingError { get { return !"ok".Equals(Code, StringComparison.InvariantCultureIgnoreCase); } }

		public string MatchingError
		{
			get
			{
				string matchCode = Code.ToLower();
				switch (matchCode)
				{
					case "ok": return "";
					case "nomatch": return "The input did not produce any matches. features will be an empty array.";
					case "toomanycoordinates": return "There are to many points in the request.";
					case "InvalidInput": return "Invalid input: 'message' will hold an explanation of the invalid input.";
					case "ProfileNotFound": return "Invalid profile.";
					case "nosegment": return "Could not find a matching segment for input coordinates.";
					default:
						return "Unexpected error: check 'message'";
				}
			}
		}

		/// <summary>Errors occured during web request </summary>
		public bool HasRequestError { get { return _requestExceptions.Count > 0; } }

		private ReadOnlyCollection<Exception> _requestExceptions = new List<Exception>().AsReadOnly();
		/// <summary>Errors of underlying web request </summary>
		public ReadOnlyCollection<Exception> RequestExceptions { get { return _requestExceptions; } }
		/// <summary>Assign errors of underlying web request </summary>
		public void SetRequestExceptions(ReadOnlyCollection<Exception> exceptions) { _requestExceptions = exceptions; }
	}




}