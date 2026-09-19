using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Mapbox.MapDebug.Scripts
{
    public class MapLogger : MonoBehaviour
    {
        public Text LogText;
        public bool PrintScreen = true;
        public List<ILogWriter> LogWriters = new List<ILogWriter>();
        private string _path;
        private GUIStyle style;
        private List<string> _screenLogs;
        public InputField Dump;
    
        private void Start()
        {
            _screenLogs = new List<string>();
            _path = GetFullLogPath(DateTime.Now.ToString("yyyyMMddTHHmmss"));
            if (style == null)
            {
                style = new GUIStyle();
                style.normal = new GUIStyleState();
                style.normal.textColor = Color.black;
                style.normal.background = Texture2D.whiteTexture;
                style.fontSize = 30;
                style.padding = new RectOffset(4, 4, 4, 4);
            }
        }

        public void ResetStats()
        {
            foreach (var logger in LogWriters)
            {
                logger.ResetStats();
            }
        }

        public void OnGUI()
        {
            if (PrintScreen)
            {
                _screenLogs.Clear();
                foreach (var logger in LogWriters)
                {
                    _screenLogs.Add(logger.PrintScreen());
                }

                LogText.text = "";
                var index = 0;
                var fromTop = 0f;
                foreach (var screenLog in _screenLogs)
                {
                    if (!string.IsNullOrEmpty(screenLog))
                    {
                        LogText.text += screenLog + Environment.NewLine;
                        // var content = new GUIContent(screenLog);
                        // var v2 = style.CalcSize(content);
                        // GUI.Label(new Rect(0, fromTop, v2.x, v2.y), content, style);
                        // fromTop += v2.y;
                        // index++;
                    }
                }
            }
        }

        public void AddLogger(ILogWriter logger)
        {
            if (LogWriters == null) LogWriters = new List<ILogWriter>();
            if (logger != null)
            {
                LogWriters.Add(logger);
            }
        }

        public void DumpLog()
        {
            var root = new JObject();
            foreach (var logger in LogWriters)
            {
                root.Add(logger.GetType().Name.ToString(), logger.DumpLogs());
            }

            Dump.text = root.ToString();
            // using (StreamWriter writer = new StreamWriter(_path, true))
            // {
            //     var root = new JObject();
            //     foreach (var logger in LogWriters)
            //     {
            //         var jobject = logger.DumpLogs();
            //         root.Add(logger.GetType().Name.ToString(), jobject);
            //     }
            //     writer.Write(root.ToString());
            //     writer.Close();
            // }
        }
        

        private static string GetFullLogPath(string dbName)
        {
            string dbPath = Path.Combine(Application.persistentDataPath, "log");
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || UNITY_WSA
			dbPath = Path.GetFullPath(dbPath);
#endif
            if (!Directory.Exists(dbPath)) { Directory.CreateDirectory(dbPath); }
            dbPath = Path.Combine(dbPath, dbName);

            return dbPath;
        }
    }
}
