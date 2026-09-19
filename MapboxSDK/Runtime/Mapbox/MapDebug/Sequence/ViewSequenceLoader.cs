#if UNITY_RECORDER && UNITY_EDITOR
using System.IO;
using System.Linq;
using Mapbox.BaseModule.Map;
using Mapbox.MapDebug.Scripts.Logging;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Mapbox.MapDebug.Sequence
{
    public class ViewSequenceLoader : MonoBehaviour
    {
        public string Path;
        public LoggingMapBehaviour Map;

        public void Load()
        {
            string jsonString = File.ReadAllText(Path);
            var json = JObject.Parse(jsonString);
            var mapInfo = json["SequenceControllerBehaviour"];
        
            var parser = new SequenceParser();
            var sequence = parser.ParseSequence((JObject) mapInfo);

            var firstView = sequence.FirstOrDefault(x => x is SetCameraSequenceCommand) as SetCameraSequenceCommand;
            Map.MapboxMap.MapInformation.SetInformation(firstView.center, firstView.zoom, firstView.pitch, firstView.bearing, firstView.scale);
            Map.MapboxMap.LoadMapView(() =>
            {
                Debug.Log("file loaded");
            }); 
        }
    }
}
#endif
