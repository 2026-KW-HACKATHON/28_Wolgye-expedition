using Mapbox.BaseModule.Unity.ModuleBehaviours;
using UnityEditor;

namespace Mapbox.BaseModule.Editor
{
    [CustomEditor(typeof(RuntimeCacheManagerBehaviour))]
    public class RuntimeCacheManagerBehaviourEditor : UnityEditor.Editor
    {
        SerializedProperty createSqliteCache;
        SerializedProperty useCustomName;
        SerializedProperty customName;
        SerializedProperty createFileCache;

        void OnEnable()
        {
            createSqliteCache  = serializedObject.FindProperty("CreateSqliteCache");
            useCustomName      = serializedObject.FindProperty("UseCustomName");
            customName   = serializedObject.FindProperty("CustomName");
            createFileCache    = serializedObject.FindProperty("CreateFileCache");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(useCustomName);
            if (useCustomName.boolValue)
            {
                EditorGUILayout.PropertyField(customName);
            }
            serializedObject.ApplyModifiedProperties();
        }
    }
}