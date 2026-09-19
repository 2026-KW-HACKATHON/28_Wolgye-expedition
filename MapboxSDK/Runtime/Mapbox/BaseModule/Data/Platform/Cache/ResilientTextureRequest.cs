using UnityEngine.Networking;

namespace Mapbox.BaseModule.Data.Platform.Cache
{
    public class ResilientTextureRequest : ResilientWebRequest
    {
        private bool _isTextureNonreadable;
        public ResilientTextureRequest(string rawUri, bool isNonReadable, int timeout, string etag = "") : base(rawUri, timeout, etag)
        {
            _isTextureNonreadable = isNonReadable;
            _request = null;
        }

        public override ResilientWebRequest Ready()
        {
            base.Ready();
            _request.downloadHandler = new DownloadHandlerTexture(!_isTextureNonreadable);
            return this;
        }
    }
}