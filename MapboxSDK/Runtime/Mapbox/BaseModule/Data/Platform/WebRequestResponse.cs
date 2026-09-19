using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Mapbox.BaseModule.Data.Platform
{
	public class WebRequestResponse
	{
		public WebResponseResult Result;
			
		private List<Exception> _exceptions;
		/// <summary> Exceptions that might have occured during the request. </summary>
		public ReadOnlyCollection<Exception> Exceptions
		{
			get { return null == _exceptions ? null : _exceptions.AsReadOnly(); }
		}

		/// <summary> Messages of exceptions otherwise empty string. </summary>
		public string ExceptionsAsString
		{
			get
			{
				if (null == _exceptions || _exceptions.Count == 0) { return string.Empty; }
				return string.Join(Environment.NewLine, _exceptions.Select(e => e.Message).ToArray());
			}
		}

		public bool LoadedFromCache = false;
		public long StatusCode;

		public bool RateLimitHit
		{
			get { return StatusCode == 429; }
		}

		public bool HasError
		{
			get { return _exceptions == null ? false : _exceptions.Count > 0; }
		}

		public byte[] Data;
		public DateTime ExpirationDate;
		public string ETag;

		public void AddException(Exception exception)
		{
			if (null == _exceptions) { _exceptions = new List<Exception>(); }
			_exceptions.Add(exception);
		}

		public bool IsRetrying = false;
	}

	public enum WebResponseResult
	{
		Cancelled,
		Success,
		Retrying,
		Failed,
		NoData
	}
}