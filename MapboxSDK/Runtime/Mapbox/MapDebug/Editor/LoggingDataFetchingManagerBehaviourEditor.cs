using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Mapbox.MapDebug.Scripts.Logging.Editor
{
    [CustomEditor(typeof(LoggingDataFetchingManagerBehaviour))]
    public class LoggingDataFetchingManagerBehaviourEditor : UnityEditor.Editor
    {
        private bool _dataFetcherFoldout;
        private bool _fileSourceFoldout;
    
        public override void OnInspectorGUI()
        {
            // Draw default inspector first (shows Fetcher field and others)
            DrawDefaultInspector();

            var behaviour = (LoggingDataFetchingManagerBehaviour)target;
            var fetcher = behaviour.Fetcher;

            // Add some space and a header
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Logging Data Fetching Info", EditorStyles.boldLabel);


            DataFetcherSection(fetcher);

            FileSourceSection(fetcher);

            // Refresh the inspector during play mode
            if (Application.isPlaying)
            {
                Repaint();
            }
        }

        private void FileSourceSection(LoggingDataFetchingManager fetcher)
        {
            _fileSourceFoldout = EditorGUILayout.Foldout(_fileSourceFoldout, "Web Source", true);
            if (!_fileSourceFoldout)
                return;

            EditorGUI.indentLevel++;
        
            if (fetcher == null)
            {
                EditorGUILayout.HelpBox("Web file source is null. Run the scene to inspect it.", MessageType.Info);
                EditorGUI.indentLevel--;
                return;
            }
        
            var fileSource = fetcher.FileSource;
        
            UIHelpers.DrawWideLabel("Image Request Count", fileSource.MapboxImageRequestCount.ToString());
            UIHelpers.DrawWideLabel("Data Request Count", fileSource.MapboxDataRequestCount.ToString());
            UIHelpers.DrawWideLabel("Aborted Request Count", fileSource.AbortedCount.ToString());

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Web response codes", EditorStyles.boldLabel);
            foreach (var codes in fileSource.ResponseCodeCounts)
            {
                UIHelpers.DrawWideLabel(codes.Key.ToString(), codes.Value.ToString() ?? "0");
            }

            if (GUILayout.Button("Copy request logs to Clipboard"))
            {
                CopyListToClipboard(fileSource.Logs);
            }

            EditorGUI.indentLevel--;
        }
        
        public static void CopyListToClipboard(List<string> lines)
        {
            if (lines == null || lines.Count == 0)
                return;

            string joined = string.Join("\n", lines);
            GUIUtility.systemCopyBuffer = joined;
            Debug.Log($"Copied {lines.Count} lines to clipboard.");
        }
        
        private void DataFetcherSection(LoggingDataFetchingManager fetcher)
        {
            _dataFetcherFoldout = EditorGUILayout.Foldout(_dataFetcherFoldout, "Data Fetcher", true);
            if (!_dataFetcherFoldout)
                return;

            EditorGUI.indentLevel++;
        
            if (fetcher == null)
            {
                EditorGUILayout.HelpBox("Data fetcher is null. Run the scene to inspect it.", MessageType.Info);
                EditorGUI.indentLevel--;
                return;
            }
        
            UIHelpers.DrawWideLabel("Added Count", fetcher.AddedCount.ToString());
            UIHelpers.DrawWideLabel("Initialized Count", fetcher.InitializedCount.ToString());
            UIHelpers.DrawWideLabel("Cancelled Count", fetcher.CancelledCount.ToString());

            EditorGUI.indentLevel--;
        }
    
    
    }
}