using Mapbox.BaseModule.Map;
using UnityEditor;
using UnityEngine;

namespace Mapbox.Example.Editor
{
    [CustomPropertyDrawer(typeof(MapInformation))]
    public class MapInformationDrawer : PropertyDrawer
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

            var pitchProp   = property.FindPropertyRelative("_pitch");
            var bearingProp = property.FindPropertyRelative("_bearing");
            var scaleProp   = property.FindPropertyRelative("_scale");
            var latLngStr   = property.FindPropertyRelative("_latitudeLongitudeString");
            var zoomProp    = property.FindPropertyRelative("_zoom");

            EditorGUILayout.PropertyField(
                latLngStr,
                new GUIContent("Latitude,Longitude",
                    "World center latitude longitude as comma-separated values (e.g., 41.015137, 28.979530)")
            );

            EditorGUILayout.PropertyField(zoomProp,    new GUIContent("Zoom",    "Initial zoom value"));
            EditorGUILayout.PropertyField(pitchProp,   new GUIContent("Pitch",   "Initial camera pitch (degrees)"));
            EditorGUILayout.PropertyField(bearingProp, new GUIContent("Bearing", "Initial camera bearing (degrees)"));
            EditorGUILayout.PropertyField(scaleProp,   new GUIContent("Scale",   "Initial world scale"));

            if (latLngStr != null)
            {
                double lat, lon; string err;
                bool ok = TryParseLatLon(latLngStr.stringValue, out lat, out lon, out err);
                if (!ok)
                {
                    EditorGUILayout.HelpBox(err ?? "Invalid coordinates. Use: lat,lon", MessageType.Warning);
                }
            }
        
            EditorGUI.indentLevel = indent;
            EditorGUI.EndProperty();
        }

        private static bool TryParseLatLon(string s, out double lat, out double lon, out string error)
        {
            lat = 0.0; lon = 0.0; error = null;

            if (string.IsNullOrEmpty(s)) { error = "Empty coordinate string."; return false; }

            string[] parts = s.Split(',');
            if (parts.Length != 2) { error = "Use: latitude,longitude"; return false; }

            double parsedLat, parsedLon;
            if (!double.TryParse(parts[0].Trim(),
                    System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out parsedLat))
            { error = "Latitude is not a number."; return false; }

            if (!double.TryParse(parts[1].Trim(),
                    System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out parsedLon))
            { error = "Longitude is not a number."; return false; }

            if (parsedLat < -90.0 || parsedLat > 90.0)   { error = "Latitude must be between -90 and 90."; return false; }
            if (parsedLon < -180.0 || parsedLon > 180.0) { error = "Longitude must be between -180 and 180."; return false; }

            lat = parsedLat; lon = parsedLon; return true;
        }
    }
}
