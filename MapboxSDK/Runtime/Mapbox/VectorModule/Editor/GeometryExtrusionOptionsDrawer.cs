using System;
using Mapbox.BaseModule.Data.Interfaces;
using Mapbox.VectorModule.MeshGeneration.MeshModifiers;
using UnityEditor;
using UnityEngine;

namespace Mapbox.VectorModule.Editor
{
    [CustomPropertyDrawer(typeof(GeometryExtrusionOptions))]
    public class GeometryExtrusionOptionsDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            GUILayout.Space(-EditorGUIUtility.singleLineHeight - 2);
            
            // Find properties
            var extrusionTypeProp = property.FindPropertyRelative("extrusionType");
            var geomTypeProp = property.FindPropertyRelative("extrusionGeometryType");
            var propertyNameProp = property.FindPropertyRelative("propertyName");
            var minHeightProp = property.FindPropertyRelative("minimumHeight");
            var maxHeightProp = property.FindPropertyRelative("maximumHeight");
            var scaleFactorProp = property.FindPropertyRelative("extrusionScaleFactor");

            EditorGUILayout.PropertyField(geomTypeProp);
        
            //part to render extrusion type in dropbox without none setting
            {
                // Get all enum values except 'None' (index 0)
                var enumValues = (ExtrusionType[])Enum.GetValues(typeof(ExtrusionType));
                var enumNames = Enum.GetNames(typeof(ExtrusionType));

                // Skip "None"
                var displayedValues = new ExtrusionType[enumValues.Length - 1];
                var displayedNames = new string[enumValues.Length - 1];
                Array.Copy(enumValues, 1, displayedValues, 0, displayedValues.Length);
                Array.Copy(enumNames, 1, displayedNames, 0, displayedNames.Length);

                // Determine current value
                var currentType = (ExtrusionType)extrusionTypeProp.enumValueIndex;

                // If current value is None, default to first visible option
                if (currentType == ExtrusionType.None)
                    currentType = displayedValues[0];

                // Draw dropdown without "None"
                int selectedIndex = Array.IndexOf(displayedValues, currentType);
                int newIndex = EditorGUILayout.Popup("Extrusion Type", Mathf.Max(selectedIndex, 0), displayedNames);

                extrusionTypeProp.enumValueIndex = (int)displayedValues[newIndex];
            }
        
            // Determine which ExtrusionType is selected
            var extrusionType = (ExtrusionType)extrusionTypeProp.enumValueIndex;
        
            DrawReadOnlyProperty(propertyNameProp);
        
            // Show common field for all types that need propertyName
            switch (extrusionType)
            {
                case ExtrusionType.PropertyHeight:
                
                    break;

                case ExtrusionType.AbsoluteHeight:
                    EditorGUILayout.PropertyField(maxHeightProp, new GUIContent("Height"));
                    break;

                case ExtrusionType.MinHeight:
                    EditorGUILayout.PropertyField(minHeightProp);
                    break;

                case ExtrusionType.MaxHeight:
                    EditorGUILayout.PropertyField(maxHeightProp);
                    break;
                case ExtrusionType.RangeHeight:
                    EditorGUILayout.PropertyField(minHeightProp);
                    EditorGUILayout.PropertyField(maxHeightProp);
                    break;

                default:
                    break;
            }

            void DrawReadOnlyProperty(SerializedProperty prop)
            {
                GUI.enabled = false;
                EditorGUILayout.PropertyField(prop);
                GUI.enabled = true;
            }
        
            EditorGUI.EndProperty();
        }
    }
}
