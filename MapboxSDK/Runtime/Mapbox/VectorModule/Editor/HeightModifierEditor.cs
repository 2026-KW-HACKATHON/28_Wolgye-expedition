using System;
using Mapbox.BaseModule.Data.Interfaces;
using Mapbox.VectorModule.MeshGeneration.MeshModifiers.ScriptableObjects;
using UnityEditor;
using UnityEngine;

namespace Mapbox.VectorModule.Editor
{
    [CustomEditor(typeof(HeightModifierObject))]
    public class HeightModifierObjectEditor : UnityEditor.Editor
    {
        SerializedProperty extrusionOptionsProp;
        SerializedProperty extrusionTypeProp;
        SerializedProperty extrusionGeometryTypeProp;
        SerializedProperty propertyNameProp;
        SerializedProperty minimumHeightProp;
        SerializedProperty maximumHeightProp;

        void OnEnable()
        {
            if (targets[0] == null)
            {
                return;
            }
            
            // Cache the main SerializedProperties
            extrusionOptionsProp = serializedObject.FindProperty("ExtrusionOptions");
            extrusionTypeProp = extrusionOptionsProp.FindPropertyRelative("extrusionType");
            extrusionGeometryTypeProp = extrusionOptionsProp.FindPropertyRelative("extrusionGeometryType");
            propertyNameProp = extrusionOptionsProp.FindPropertyRelative("propertyName");
            minimumHeightProp = extrusionOptionsProp.FindPropertyRelative("minimumHeight");
            maximumHeightProp = extrusionOptionsProp.FindPropertyRelative("maximumHeight");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

        
            EditorGUILayout.PropertyField(extrusionGeometryTypeProp);

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
                    EditorGUILayout.PropertyField(maximumHeightProp, new GUIContent("Height"));
                    break;

                case ExtrusionType.MinHeight:
                    EditorGUILayout.PropertyField(minimumHeightProp);
                    break;

                case ExtrusionType.MaxHeight:
                    EditorGUILayout.PropertyField(maximumHeightProp);
                    break;
                case ExtrusionType.RangeHeight:
                    EditorGUILayout.PropertyField(minimumHeightProp);
                    EditorGUILayout.PropertyField(maximumHeightProp);
                    break;

                default:
                    break;
            }

            serializedObject.ApplyModifiedProperties();
        
            void DrawReadOnlyProperty(SerializedProperty prop)
            {
                GUI.enabled = false;
                EditorGUILayout.PropertyField(prop);
                GUI.enabled = true;
            }
        }
    }
}
