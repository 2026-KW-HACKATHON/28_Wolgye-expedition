#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using Mapbox.VectorModule.MeshGeneration.GameObjectModifiers;

[CustomEditor(typeof(PropertiesViewerBehaviour))]
public class PropertiesViewerBehaviourEditor : Editor
{
    public override void OnInspectorGUI()
    {
        var behaviour = (PropertiesViewerBehaviour)target;

        if (behaviour.Properties == null)
            behaviour.Properties = new Dictionary<string, string>();

        EditorGUILayout.LabelField("Properties", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;

        var keys = new List<string>(behaviour.Properties.Keys);
        foreach (var key in keys)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(key, GUILayout.MaxWidth(150));
            string newValue = EditorGUILayout.TextField(behaviour.Properties[key]);
            if (newValue != behaviour.Properties[key])
            {
                Undo.RecordObject(behaviour, "Edit Property Value");
                behaviour.Properties[key] = newValue;
                EditorUtility.SetDirty(behaviour);
            }
            EditorGUILayout.EndHorizontal();
        }

        EditorGUI.indentLevel--;
    }
}
#endif