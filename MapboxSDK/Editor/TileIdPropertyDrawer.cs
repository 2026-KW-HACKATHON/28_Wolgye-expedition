using Mapbox.BaseModule.Data.Tiles;
using UnityEditor;
using UnityEngine;

namespace MapboxUnitySDK.Editor
{
    [InitializeOnLoad]
    [CustomPropertyDrawer (typeof (CanonicalTileId)), CustomPropertyDrawer(typeof (UnwrappedTileId))]
    public class TileIdPropertyDrawer : PropertyDrawer {

        // TODO: this is sort of like, but not exactly like the X positional handling of Vector3
        public override void OnGUI (Rect pos, SerializedProperty prop, GUIContent label) {

            float nameWidth = pos.width * .31f;

            float labelWidth = 12f;
            float fieldWidth = (pos.width - nameWidth) - labelWidth;

            SerializedProperty x = prop.FindPropertyRelative ("X");
            SerializedProperty y = prop.FindPropertyRelative ("Y");
            SerializedProperty z = prop.FindPropertyRelative ("Z");

            float posx = pos.x;

            int indent = EditorGUI.indentLevel;

            EditorGUI.LabelField (new Rect (pos.x, pos.y, nameWidth, pos.height), prop.displayName);
            posx += nameWidth;

            EditorGUI.LabelField(new Rect(posx, pos.y, fieldWidth, pos.height), string.Format(
                "{0}, {1}, {2}", 
                z.intValue.ToString(),
                x.intValue.ToString(),
                y.intValue.ToString()));
            posx += fieldWidth;
            // // Z
            // //EditorGUI.indentLevel = 0;
            // //EditorGUI.LabelField (new Rect (posx, pos.y, labelWidth, pos.height), "Z"); posx += labelWidth;
            // EditorGUI.LabelField(new Rect(posx, pos.y, fieldWidth, pos.height), z.intValue.ToString());
            // posx += fieldWidth;
            // // EditorGUI.DoubleField (
            // //     new Rect (posx, pos.y, fieldWidth, pos.height), z.doubleValue); posx += fieldWidth;
            //
            // // Draw X
            // //EditorGUI.LabelField (new Rect (posx, pos.y, labelWidth, pos.height), "X"); posx += labelWidth;
            // EditorGUI.LabelField(new Rect(posx, pos.y, fieldWidth, pos.height), x.intValue.ToString());
            // posx += fieldWidth;
            // // EditorGUI.DoubleField (
            // //     new Rect (posx, pos.y, fieldWidth, pos.height), x.doubleValue);  posx += fieldWidth;
            //
            // // Y
            // //EditorGUI.indentLevel = 0;
            // //EditorGUI.LabelField (new Rect (posx, pos.y, labelWidth, pos.height), "Y"); posx += labelWidth;
            // EditorGUI.LabelField(new Rect(posx, pos.y, fieldWidth, pos.height), y.intValue.ToString());
            // posx += fieldWidth;
            // // EditorGUI.DoubleField (
            // //     new Rect (posx, pos.y, fieldWidth, pos.height), y.doubleValue); posx += fieldWidth;

            
       
            EditorGUI.indentLevel = indent;
        }
    }
}