using UnityEditor;

namespace Mapbox.CustomImageryModule.Editor
{
    [CustomEditor(typeof(CustomApiLayerModuleScript))]
    public class CustomApiLayerModuleScriptEditor : UnityEditor.Editor
    {
        SerializedProperty customSourceSettingsProp;

        SerializedProperty rejectTilesOutsideZoomProp;
        SerializedProperty clampDataLevelToMaxProp;

        void OnEnable()
        {
            // Root properties
            customSourceSettingsProp = serializedObject.FindProperty("CustomSourceSettings");

            // Settings.RejectTilesOutsideZoom
            rejectTilesOutsideZoomProp =
                serializedObject.FindProperty("Settings")
                    .FindPropertyRelative("RejectTilesOutsideZoom");

            // Settings.DataSettings.ClampDataLevelToMax
            clampDataLevelToMaxProp =
                serializedObject.FindProperty("Settings")
                    .FindPropertyRelative("DataSettings")
                    .FindPropertyRelative("ClampDataLevelToMax");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("Custom Source Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(customSourceSettingsProp, true);

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Tile Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(rejectTilesOutsideZoomProp);
            EditorGUILayout.PropertyField(clampDataLevelToMaxProp);

            serializedObject.ApplyModifiedProperties();
        }
    }
}