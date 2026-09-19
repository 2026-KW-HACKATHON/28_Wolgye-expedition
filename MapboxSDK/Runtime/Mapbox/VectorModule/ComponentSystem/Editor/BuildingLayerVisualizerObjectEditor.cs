using Mapbox.VectorModule.ComponentSystem.BuildingComponentVisualizer;
using UnityEditor;

namespace Mapbox.VectorModule.ComponentSystem.Editor
{
    [CustomEditor(typeof(BuildingComponentVisualizerObject))]
    public class BuildingComponentVisualizerObjectEditor : UnityEditor.Editor
    {
        private SerializedProperty _settingsProp;

        private void OnEnable()
        {
        
        }

        public override void OnInspectorGUI()
        {
            _settingsProp = serializedObject.FindProperty("Settings");
            serializedObject.Update();

            EditorGUILayout.LabelField("Building Layer Visualizer", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            if (_settingsProp != null)
            {
                DrawSettings(_settingsProp);
            }
            else
            {
                EditorGUILayout.HelpBox("No _settings field found.", MessageType.Warning);
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawSettings(SerializedProperty settingsProp)
        {
            var enableTerrainSnapping = settingsProp.FindPropertyRelative("EnableTerrainSnapping");
            var enableChamfer = settingsProp.FindPropertyRelative("RoundBuildingCorners");
            var chamferSettings = settingsProp.FindPropertyRelative("ChamferExtrusionSettings");
            var basicSettings = settingsProp.FindPropertyRelative("BasicExtrusionSettings");
            var material = settingsProp.FindPropertyRelative("Material");
        
            EditorGUILayout.PropertyField(material);
            EditorGUILayout.PropertyField(enableTerrainSnapping);
            EditorGUILayout.Space();
        
            enableChamfer.boolValue = EditorGUILayout.Toggle("Enable Rounded Corners", enableChamfer.boolValue);
            if (enableChamfer.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(chamferSettings, true);
                EditorGUI.indentLevel--;
            }
            else
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(basicSettings, true);
                EditorGUI.indentLevel--;
            }
        }
    }
}