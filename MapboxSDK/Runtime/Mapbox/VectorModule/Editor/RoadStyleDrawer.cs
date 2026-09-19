#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using Mapbox.VectorModule.ComponentSystem.RoadComponentVisualizer;
using Mapbox.VectorModule.MeshGeneration.Unity;

[CustomPropertyDrawer(typeof(RoadStyle))]
public class RoadStyleDrawer : PropertyDrawer
{
    private const float LineSpacing = 2f;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        var nameProp      = property.FindPropertyRelative("Name");
        var widthProp     = property.FindPropertyRelative("Width");
        var pushUpProp     = property.FindPropertyRelative("PushUp");
        var materialProp  = property.FindPropertyRelative("Material");
        var filtersProp   = property.FindPropertyRelative("Filters");

        // Foldout
        Rect foldoutRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, nameProp.stringValue, true);

        if (!property.isExpanded)
        {
            EditorGUI.EndProperty();
            return;
        }

        EditorGUI.indentLevel++;

        float y = foldoutRect.y + EditorGUIUtility.singleLineHeight + LineSpacing;
        float width = position.width;

        // ClassName
        if (nameProp != null)
        {
            Rect r = new Rect(position.x, y, width, EditorGUIUtility.singleLineHeight);
            EditorGUI.PropertyField(r, nameProp);
            y += EditorGUIUtility.singleLineHeight + LineSpacing;
        }

        // Width
        if (widthProp != null)
        {
            Rect r = new Rect(position.x, y, width, EditorGUIUtility.singleLineHeight);
            EditorGUI.PropertyField(r, widthProp);
            y += EditorGUIUtility.singleLineHeight + LineSpacing;
        }
        
        // Width
        if (pushUpProp != null)
        {
            Rect r = new Rect(position.x, y, width, EditorGUIUtility.singleLineHeight);
            EditorGUI.PropertyField(r, pushUpProp);
            y += EditorGUIUtility.singleLineHeight + LineSpacing;
        }

        // Material
        if (materialProp != null)
        {
            Rect r = new Rect(position.x, y, width, EditorGUIUtility.singleLineHeight);
            EditorGUI.PropertyField(r, materialProp);
            y += EditorGUIUtility.singleLineHeight + LineSpacing;
        }

        // Filters: delegate to VectorFilterStackDrawer via PropertyField(rect,..., includeChildren: true)
        if (filtersProp != null)
        {
            float filtersHeight = EditorGUI.GetPropertyHeight(filtersProp, true);
            Rect r = new Rect(position.x, y, width, filtersHeight);
            EditorGUI.PropertyField(r, filtersProp, new GUIContent("Filters"), true);
            y += filtersHeight + LineSpacing;
        }

        EditorGUI.indentLevel--;
        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float height = EditorGUIUtility.singleLineHeight; // foldout

        if (!property.isExpanded)
            return height;

        float spacing = LineSpacing;
        var nameProp = property.FindPropertyRelative("Name");
        var widthProp     = property.FindPropertyRelative("Width");
        var pushUpProp     = property.FindPropertyRelative("PushUp");
        var materialProp  = property.FindPropertyRelative("Material");
        var filtersProp   = property.FindPropertyRelative("Filters");

        if (nameProp != null)
            height += EditorGUIUtility.singleLineHeight + spacing;

        if (widthProp != null)
            height += EditorGUIUtility.singleLineHeight + spacing;
        
        if (pushUpProp != null)
            height += EditorGUIUtility.singleLineHeight + spacing;

        if (materialProp != null)
            height += EditorGUIUtility.singleLineHeight + spacing;

        if (filtersProp != null)
        {
            float filtersHeight = EditorGUI.GetPropertyHeight(filtersProp, true);
            height += filtersHeight + spacing;
        }

        return height;
    }
}
#endif
