using System;
using UnityEngine.Networking;

namespace Mapbox.BaseModule.Data.Platform.Cache
{
    public class ResilientWebRequest : IDisposable, IWebRequest
    {
        protected UnityWebRequest _request;
        public string RawUri;
        protected int _timeout;
        protected string _etag;

        public bool IsAborted = false;

        public int TryCount { get; protected set; }

        public UnityWebRequest Core => _request;
        public DownloadHandler downloadHandler => _request.downloadHandler;
        public long responseCode => _request.responseCode;
        public string error => _request.error;
        public UnityWebRequest.Result result => _request.result;
        public string Url => _request.url;

        private string EtagHeaderName = "ETag";
        private string CacheControlHeaderName = "Cache-Control";
		
        public ResilientWebRequest(string rawUri, int timeout, string etag = "")
        {
            RawUri = rawUri;
            _timeout = timeout;
            _etag = etag;
            _request = null;
        }

        public virtual ResilientWebRequest Ready()
        {
            if (_request != null)
            {
                _request.Abort();
                _request.Dispose();
                _request = null;
            }
			
            _request = UnityWebRequest.Get(RawUri);
            _request.timeout = _timeout;
            if (!string.IsNullOrEmpty(_etag))
            {
                _request.SetRequestHeader("If-None-Match", _etag);
            }

            TryCount++;
            return this;
        }

        public void Abort()
        {
            IsAborted = true;
            _request?.Abort();
            Dispose();
        }

        public void Dispose()
        {
            _request?.Dispose();
            _request = null;
        }

        public UnityWebRequestAsyncOperation SendWebRequest()
        {
            return _request.SendWebRequest();
        }

        public string GetETag()
        {
            string eTag = _request.GetResponseHeader(EtagHeaderName);
            if (string.IsNullOrEmpty(eTag))
            {
                //Debug.LogWarning("no 'ETag' header present in response");
            }

            return eTag;
        }

        public DateTime GetExpirationDate()
        {
            DateTime expirationDate = DateTime.Now;
            var headerValue = _request.GetResponseHeader(CacheControlHeaderName);
            if (!string.IsNullOrEmpty(headerValue))
            {
                var cacheEntries = headerValue.Split(',');
                if (cacheEntries.Length > 0)
                {
                    foreach (var entry in cacheEntries)
                    {
                        var value = entry.Split('=');
                        if (value[0] == "max-age")
                        {
                            expirationDate = expirationDate + TimeSpan.FromSeconds(int.Parse(value[1]));
                            return expirationDate;
                        }
                    }
                }
            }

            return expirationDate;
        }
    }
}