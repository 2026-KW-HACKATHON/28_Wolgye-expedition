using Newtonsoft.Json.Linq;

namespace Mapbox.MapDebug.Scripts
{
    public interface ILogWriter
    {
        JObject DumpLogs();
        string PrintScreen();
        void ResetStats();
    }
}