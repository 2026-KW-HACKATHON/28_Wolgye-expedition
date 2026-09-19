#if UNITY_RECORDER && UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mapbox.BaseModule.Map;
using Mapbox.BaseModule.Utilities;
using Mapbox.MapDebug.Scripts;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.Recorder;
using UnityEngine;

namespace Mapbox.MapDebug.Sequence
{
    public class SequenceControllerBehaviour : MonoBehaviour, ILogWriter
    {
        public string FilePath;
        
        [Range(0.1f,1.5f)] public float Speed;
        private MapboxMap Map;
        private Coroutine _run;
        private bool _isRecording = false;
        private float _recordTime;
        private Camera _camera;
        [NonSerialized] private List<SequenceCommand> _recordedViewChanges;

        private MapLogger _mapLogger;
        [SerializeField] private RecorderController _testRecorderController;

        private void Start()
        {
            _mapLogger = FindObjectOfType<MapLogger>();
        }

        public async void Load()
        {
            var mapCore = FindObjectOfType<MapBehaviourCore>();
            Map = mapCore.MapboxMap;
            string jsonString = File.ReadAllText(FilePath);
            var json = JObject.Parse(jsonString);
            var mapInfo = json["SequenceControllerBehaviour"];
        
            var parser = new SequenceParser();
            _recordedViewChanges = parser.ParseSequence((JObject) mapInfo);

            var firstView = _recordedViewChanges.FirstOrDefault(x => x is SetCameraSequenceCommand) as SetCameraSequenceCommand;
            Map.MapInformation.SetInformation(firstView.center, firstView.zoom, firstView.pitch, firstView.bearing, firstView.scale);
            Map.LoadMapView(() => Debug.Log("file loaded"));
        }

        public void Run(bool saveVideo = false)
        {
            _isRecording = false;
            _mapLogger.ResetStats();
            _mapLogger.PrintScreen = true;
            if(saveVideo) StartRecorder();
            if (_run != null)
            {
                StopCoroutine(_run);
                _run = null;
            }
            
            _run = StartCoroutine(RunRun(_recordedViewChanges));
        }

        private void StartRecorder()
        {
            var controllerSettings = ScriptableObject.CreateInstance<RecorderControllerSettings>();
            _testRecorderController = new RecorderController(controllerSettings);

            var videoRecorder = ScriptableObject.CreateInstance<MovieRecorderSettings>();
            videoRecorder.name = "MapboxDebugVideoRecorder";
            videoRecorder.Enabled = true;
            videoRecorder.VideoBitRateMode = VideoBitrateMode.Low;
            videoRecorder.AudioInputSettings.PreserveAudio = false;

            videoRecorder.AudioInputSettings.PreserveAudio = true;
            videoRecorder.OutputFile = FilePath + "_" + DateTime.Now.ToString("ddMMyyy_hhmm");

            controllerSettings.AddRecorderSettings(videoRecorder);
            controllerSettings.FrameRate = 10;

            RecorderOptions.VerboseMode = false;
            _testRecorderController.PrepareRecording();
            _testRecorderController.StartRecording();

// Wait a while
        }

        public void Stop()
        {
            if (_run != null)
            {
                _testRecorderController.StopRecording();
                StopCoroutine(_run);
                TestFinished();
            }
        }

        private IEnumerator RunRun(List<SequenceCommand> sequenceCommands)
        {
            TestStarted();
            while (true)
            {
                foreach (var command in sequenceCommands)
                {
                    yield return StartCoroutine(command.Run(Map, Speed));
                }

                break;
            }

            _testRecorderController?.StopRecording();
            TestFinished();
        }

        public void Record(MapboxMap map, Camera cam)
        {
            Map = map;
            _camera = cam;
            
            if (!_isRecording)
            {
                _isRecording = true;
                _recordTime = Time.realtimeSinceStartup;
                _recordedViewChanges = new List<SequenceCommand>();

                var wait = new WaitSequenceCommand() {duration = 0};
                var set = new SetCameraSequenceCommand()
                {
                    center = Conversions.WebMercatorToLatLon((GetCenterPosition() * Map.MapInformation.Scale).ToVector2d() + Map.MapInformation.CenterMercator),
                    bearing = Map.MapInformation.Bearing,
                    pitch = 90 - Map.MapInformation.Pitch,
                    zoom = Map.MapInformation.Zoom,
                    scale = map.MapInformation.Scale
                };
                _recordedViewChanges.Add(wait);
                _recordedViewChanges.Add(set);
            
                Map.MapInformation.ViewChanged += RecordViewChanges;
            }
        }

        public void ResetStats()
        {
            
        }

        public JObject DumpLogs()
        {
            if (_isRecording)
            {
                var commandArray = new JArray();
                foreach (var command in _recordedViewChanges)
                {
                    var token = JObject.FromObject(command); //command.Serialize();
                    commandArray.Add(token);
                }

                var main = new JObject(
                    new JProperty("version", 1),
                    new JProperty("sequence", commandArray));
                //var json = main.ToString();
            
                //File.WriteAllText(FilePath, main.ToString());
            
                Map.MapInformation.ViewChanged -= RecordViewChanges;
                //_isRecording = false;
                return main;
            }

            return null;
        }

        public string PrintScreen()
        {
            return String.Empty;
        }

        private void RecordViewChanges(IMapInformation info)
        {
            if (!_isRecording)
                return;

            var deltaTime = (Time.realtimeSinceStartup - _recordTime);
            _recordTime = Time.realtimeSinceStartup;

            var wait = new WaitSequenceCommand() {duration = deltaTime};
        
            var command = new SetCameraSequenceCommand();
            command.center = Conversions.WebMercatorToLatLon((GetCenterPosition() * Map.MapInformation.Scale).ToVector2d() + Map.MapInformation.CenterMercator);
            command.pitch = 90 - info.Pitch;
            command.bearing = info.Bearing;
            command.zoom = info.Zoom;
            command.scale = info.Scale;
                
            _recordedViewChanges.Add(wait);
            _recordedViewChanges.Add(command);
        }

        public Action TestStarted = () => { };
        public Action TestFinished = () => { };

        private Vector3 GetCenterPosition()
        {
            var ray = _camera.ScreenPointToRay(_camera.ViewportToScreenPoint(new Vector3(.5f, .5f, 0f)));
            var dirNorm = ray.direction / ray.direction.y;
            var intersectionPos = ray.origin - dirNorm * ray.origin.y;
            return intersectionPos;
        }
    }
}
#endif
