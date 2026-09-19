using UnityEngine.Networking;

namespace Mapbox.BaseModule.Data.Platform.Cache
{
    public interface IWebRequest
    {
        void Abort();
        UnityWebRequest Core { get; }

        int TryCount { get; }
    }
}