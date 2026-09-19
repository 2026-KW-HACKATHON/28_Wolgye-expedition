#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using Mapbox.VectorModule.ComponentSystem.RoadComponentVisualizer;

[CustomEditor(typeof(RoadStyleSheet))]
public class RoadStyleSheetEditor : Editor
{
    private SerializedProperty _stylesProp;
    private ReorderableList _stylesList;

    private void OnEnable()
    {
        _stylesProp = serializedObject.FindProperty("Styles");

        if (_stylesProp == null)
            return;

        _stylesList = new ReorderableList(serializedObject, _stylesProp, true, true, true, true);

        _stylesList.drawHeaderCallback = rect =>
        {
            EditorGUI.LabelField(rect, "Road Styles");
        };

        _stylesList.elementHeightCallback = index =>
        {
            var element = _stylesProp.GetArrayElementAtIndex(index);
            float h = EditorGUI.GetPropertyHeight(element, true);
            return h + 4f;
        };

        _stylesList.drawElementCallback = (rect, index, isActive, isFocused) =>
        {
            var element = _stylesProp.GetArrayElementAtIndex(index);
            rect.y += 2f;
            rect.height = EditorGUI.GetPropertyHeight(element, true);
            EditorGUI.PropertyField(rect, element, new GUIContent("Style " + index), true);
        };

        _stylesList.onAddCallback = list =>
        {
            int index = _stylesProp.arraySize;
            _stylesProp.InsertArrayElementAtIndex(index);

            var newElement = _stylesProp.GetArrayElementAtIndex(index);
            var classNameProp = newElement.FindPropertyRelative("ClassName");
            var widthProp     = newElement.FindPropertyRelative("Width");
            var materialProp  = newElement.FindPropertyRelative("Material");
            var filtersProp   = newElement.FindPropertyRelative("Filters");

            if (classNameProp != null) classNameProp.stringValue = string.Empty;
            if (widthProp     != null) widthProp.floatValue      = 1f;
            if (materialProp  != null) materialProp.objectReferenceValue = null;

            // Make sure Filters exists
            if (filtersProp != null)
            {
                var typeProp      = filtersProp.FindPropertyRelative("Type");
                var listProp      = filtersProp.FindPropertyRelative("Filters");

                if (typeProp != null)
                    typeProp.enumValueIndex = 0; // default enum

                if (listProp != null && listProp.isArray)
                    listProp.arraySize = 0;
            }

            serializedObject.ApplyModifiedProperties();
        };
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.HelpBox(
            "Road Style Sheet.\nConfigure styles per road class. Each style has a filter stack.",
            MessageType.Info);

        if (_stylesList != null)
        {
            _stylesList.DoLayoutList();
        }
        else
        {
            EditorGUILayout.PropertyField(_stylesProp, true);
        }

        serializedObject.ApplyModifiedProperties();
    }
}
#endif
