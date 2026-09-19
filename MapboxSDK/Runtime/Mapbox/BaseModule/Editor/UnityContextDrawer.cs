using Mapbox.BaseModule.Unity;
using UnityEditor;
using UnityEngine;

namespace Mapbox.Example.Editor
{
    [CustomPropertyDrawer(typeof(UnityContext))]
    public class UnityContextDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            // Consume the reserved rect with the foldout (prevents blank line)
            property.isExpanded = EditorGUI.Foldout(position, property.isExpanded, label, true);
            if (!property.isExpanded)
            {
                EditorGUI.EndProperty();
                return;
            }

            int indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = indent + 1;

            var mapRootProp  = property.FindPropertyRelative("MapRoot");
            var baseRootProp = property.FindPropertyRelative("BaseTileRoot");
            var runtimeRoot  = property.FindPropertyRelative("RuntimeGenerationRoot");

            EditorGUILayout.PropertyField(mapRootProp,  new GUIContent("Map Root",                "Root object to hold all map related game objects"));
            EditorGUILayout.PropertyField(baseRootProp, new GUIContent("Base Tile Root",          "Root object for all tile objects which created the base map"));
            EditorGUILayout.PropertyField(runtimeRoot,  new GUIContent("Runtime Generation Root", "Root object for all runtime generated visuals (vector feature visuals)"));

            EditorGUI.indentLevel = indent;
            EditorGUI.EndProperty();
        }
    }
}