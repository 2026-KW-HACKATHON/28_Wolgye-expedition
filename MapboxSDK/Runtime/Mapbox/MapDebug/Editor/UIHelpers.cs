using UnityEditor;
using UnityEngine;

namespace Mapbox.MapDebug.Scripts.Logging.Editor
{
    public static class UIHelpers
    {
        /// <summary>
        /// Draws a full-width label with left-aligned name and right-aligned value.
        /// </summary>
        public static void DrawWideLabel(string label, string value, float labelPortion = 0.5f)
        {
            if (string.IsNullOrEmpty(label))
                label = "-";
            if (value == null)
                value = "";

            Rect rect = GUILayoutUtility.GetRect(EditorGUIUtility.currentViewWidth - 40, EditorGUIUtility.singleLineHeight);
            rect.x += 10; 
            rect.width -= 20;

            float labelWidth = rect.width * Mathf.Clamp01(labelPortion);
            float valueWidth = rect.width - labelWidth;

            var labelRect = new Rect(rect.x, rect.y, labelWidth, rect.height);
            var valueRect = new Rect(rect.x + labelWidth, rect.y, valueWidth, rect.height);

            var labelStyle = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleLeft,
                fontStyle = FontStyle.Normal
            };

            var valueStyle = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleRight,
                fontStyle = FontStyle.Bold
            };

            EditorGUI.LabelField(labelRect, label, labelStyle);
            EditorGUI.LabelField(valueRect, value, valueStyle);
        }
    }
}