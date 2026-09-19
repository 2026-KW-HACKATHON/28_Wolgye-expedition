#if UNITY_EDITOR
using Mapbox.BaseModule.Data.Interfaces;
using Mapbox.BaseModule.Utilities;
using UnityEditor;
using UnityEngine;

namespace Mapbox.VectorModule.Editor
{
    [CustomPropertyDrawer(typeof(VectorModuleSettings))]
    public class VectorModuleSettingsDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            float line = EditorGUIUtility.singleLineHeight;
            float pad = EditorGUIUtility.standardVerticalSpacing;
            Rect cursor = new Rect(position.x, position.y, position.width, line);

            // Foldout
            property.isExpanded = EditorGUI.Foldout(cursor, property.isExpanded, label, true);
            cursor.y += line + pad;

            if (!property.isExpanded)
            {
                EditorGUI.EndProperty();
                return;
            }

            using (new EditorGUI.IndentLevelScope(1))
            {
                var sourceTypeProp = property.FindPropertyRelative("SourceType");
                var customSourceProp = property.FindPropertyRelative("CustomSourceId");
                var dataSettingsProp = property.FindPropertyRelative("DataSettings");
                var rejectZoomProp = property.FindPropertyRelative("RejectTilesOutsideZoom");

                // --- Custom enum popup (without "None") ---
                var allNames = sourceTypeProp.enumDisplayNames;
                var allValues = sourceTypeProp.enumNames;

                // Filter out "None" (assumed to be enum index 2)
                var names = new string[allNames.Length - 1];
                for (int i = 0; i < names.Length; i++)
                    names[i] = allNames[i];

                int currentIndex = sourceTypeProp.enumValueIndex;
                if (currentIndex < 0) currentIndex = 0;

                int newIndex = EditorGUI.Popup(cursor, "Source Type", currentIndex, names);
                sourceTypeProp.enumValueIndex = newIndex;
                cursor.y += line + pad;

                bool isCustom = false;
                try { isCustom = (VectorSourceType)sourceTypeProp.enumValueIndex == VectorSourceType.Custom; } catch { }

                // CustomSourceId + button
                if (isCustom)
                {
                    float customH = EditorGUI.GetPropertyHeight(customSourceProp, true);
                    var customRect = new Rect(cursor.x, cursor.y, cursor.width, customH);
                    EditorGUI.PropertyField(customRect, customSourceProp, true);
                    cursor.y += customH + pad;

                    // Button under textbox
                    var buttonRect = new Rect(cursor.x, cursor.y, cursor.width, line);
                    buttonRect = IndentedValueRect(buttonRect);

                    var StreetsV8 = MapboxDefaultVector.GetParameters(VectorSourceType.MapboxStreetsV8).Id; 
                    string cur = customSourceProp.stringValue ?? string.Empty;
                    bool hasStreets = cur.StartsWith(StreetsV8);
                    string btnLabel = hasStreets ? "Remove Mapbox Streets V8" : "Add Mapbox Streets V8";

                    if (GUI.Button(buttonRect, btnLabel))
                    {
                        if (hasStreets)
                        {
                            if (cur.StartsWith(StreetsV8 + ","))
                                cur = cur.Substring(StreetsV8.Length + 1);
                            else
                                cur = cur.Substring(StreetsV8.Length);
                        }
                        else
                        {
                            cur = string.IsNullOrEmpty(cur) ? StreetsV8 : (StreetsV8 + "," + cur);
                        }

                        customSourceProp.stringValue = cur;
                        GUI.FocusControl(null);
                    }

                    cursor.y += line + pad;
                }

                // DataSettings
                float dataH = EditorGUI.GetPropertyHeight(dataSettingsProp, true);
                EditorGUI.PropertyField(new Rect(cursor.x, cursor.y, cursor.width, dataH), dataSettingsProp, true);
                cursor.y += dataH + pad;

                // RejectTilesOutsideZoom
                float zoomH = EditorGUI.GetPropertyHeight(rejectZoomProp, true);
                EditorGUI.PropertyField(new Rect(cursor.x, cursor.y, cursor.width, zoomH), rejectZoomProp, true);
            }

            EditorGUI.EndProperty();
        }

        private static Rect IndentedValueRect(Rect row)
        {
            row = EditorGUI.IndentedRect(row);
            row.xMin += EditorGUIUtility.labelWidth;
            return row;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float line = EditorGUIUtility.singleLineHeight;
            float pad = EditorGUIUtility.standardVerticalSpacing;

            float total = line + pad; // foldout

            if (property.isExpanded)
            {
                var sourceTypeProp = property.FindPropertyRelative("SourceType");
                var customSourceProp = property.FindPropertyRelative("CustomSourceId");
                var dataSettingsProp = property.FindPropertyRelative("DataSettings");
                var rejectZoomProp = property.FindPropertyRelative("RejectTilesOutsideZoom");

                total += line + pad; // SourceType popup
                bool isCustom = false;
                try { isCustom = (VectorSourceType)sourceTypeProp.enumValueIndex == VectorSourceType.Custom; } catch { }

                if (isCustom)
                {
                    total += EditorGUI.GetPropertyHeight(customSourceProp, true) + pad;
                    total += line + pad;
                }

                total += EditorGUI.GetPropertyHeight(dataSettingsProp, true) + pad;
                total += EditorGUI.GetPropertyHeight(rejectZoomProp, true);
            }

            return total + 2f;
        }
    }
}
#endif
