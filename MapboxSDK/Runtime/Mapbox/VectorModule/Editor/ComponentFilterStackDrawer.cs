#if UNITY_EDITOR
using System;
using System.ComponentModel;
using System.Linq;
using Mapbox.VectorModule.ComponentSystem.RoadComponentVisualizer;
using UnityEditor;
using UnityEngine;
using Mapbox.VectorModule.MeshGeneration.Unity;
using Mapbox.VectorModule.Filters; // where FilterBase lives

[CustomPropertyDrawer(typeof(ComponentFilterStack))]
public class ComponentFilterStackDrawer : PropertyDrawer
{
    private const float LineSpacing  = 2f;
    private const float BoxPadding   = 4f;
    private const float HeaderHeight = 18f;

    // Cached FilterBase-derived types
    private static Type[] _filterTypes;
    private static string[] _filterTypePaths;
    private static bool _typesInitialized;

    private static void EnsureFilterTypes()
    {
        if (_typesInitialized)
            return;

        _typesInitialized = true;

        var baseType = typeof(RoadFilter);

        var types = TypeCache.GetTypesDerivedFrom<RoadFilter>()
            .Where(t => t != null && !t.IsAbstract)
            .ToArray();

        _filterTypes = types;

        _filterTypePaths = _filterTypes
            .Select(t =>
            {
                // Use DisplayNameAttribute if present for pretty menu paths
                var attr = t
                    .GetCustomAttributes(typeof(DisplayNameAttribute), true)
                    .FirstOrDefault() as DisplayNameAttribute;
                return attr != null && !string.IsNullOrEmpty(attr.DisplayName)
                    ? attr.DisplayName
                    : t.Name;
            })
            .ToArray();
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        EnsureFilterTypes();

        var typeProp    = property.FindPropertyRelative("Type");
        var filtersProp = property.FindPropertyRelative("Filters");

        // Foldout
        Rect foldoutRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, label, true);

        if (!property.isExpanded)
        {
            EditorGUI.EndProperty();
            return;
        }

        EditorGUI.indentLevel++;

        float y     = foldoutRect.y + EditorGUIUtility.singleLineHeight + LineSpacing;
        float width = position.width;

        // Combiner type
        if (typeProp != null)
        {
            Rect typeRect = new Rect(position.x, y, width, EditorGUIUtility.singleLineHeight);
            EditorGUI.PropertyField(typeRect, typeProp, new GUIContent("Process if"));
            y += EditorGUIUtility.singleLineHeight + LineSpacing;
        }

        if (filtersProp == null)
        {
            Rect warnRect = new Rect(position.x, y, width, EditorGUIUtility.singleLineHeight * 2f);
            EditorGUI.HelpBox(warnRect,
                "Filters list not found.\n" +
                "Ensure it's declared as:\n[SerializeReference] public List<RoadFilter> Filters;",
                MessageType.Warning);
            y += warnRect.height + LineSpacing;
        }
        else
        {
            // Filters label
            // Rect labelRect = new Rect(position.x, y, width, EditorGUIUtility.singleLineHeight);
            // EditorGUI.LabelField(labelRect, "Filters", EditorStyles.boldLabel);
            // y += EditorGUIUtility.singleLineHeight + LineSpacing;

            if (filtersProp.arraySize == 0)
            {
                Rect infoRect = new Rect(position.x, y, width, EditorGUIUtility.singleLineHeight * 1.5f);
                EditorGUI.HelpBox(infoRect, "No filters. Use 'Add Filter' to create one.", MessageType.Info);
                y += infoRect.height + LineSpacing;
            }
            else
            {
                // Draw each filter as a nice box
                for (int i = 0; i < filtersProp.arraySize; i++)
                {
                    SerializedProperty element = filtersProp.GetArrayElementAtIndex(i);
                    float elementHeight = EditorGUI.GetPropertyHeight(element, true);

                    float boxHeight = HeaderHeight + elementHeight + BoxPadding * 2f;
                    Rect boxRect = new Rect(position.x, y, width, boxHeight);
                    GUI.Box(boxRect, GUIContent.none);

                    float innerX = boxRect.x + BoxPadding;
                    float innerY = boxRect.y + BoxPadding;
                    float innerW = boxRect.width - BoxPadding * 2f;

                    if (element != null)
                    {
                        // Header: "Filter i (TypeName)" + Remove button
                        string typeName = GetManagedReferenceTypeName(element) ?? "Null";
                        string header = string.Format("{0} - {1}", typeName, element.FindPropertyRelative("FilterString")?.stringValue);

                        Rect headerLabelRect = new Rect(innerX, innerY, innerW - 70f, EditorGUIUtility.singleLineHeight);
                        EditorGUI.LabelField(headerLabelRect, header, EditorStyles.boldLabel);
                    }

                    Rect removeRect = new Rect(innerX + innerW - 65f, innerY, 60f,
                        EditorGUIUtility.singleLineHeight);
                    if (GUI.Button(removeRect, "Remove"))
                    {
                        filtersProp.DeleteArrayElementAtIndex(i);
                        property.serializedObject.ApplyModifiedProperties();
                        EditorGUI.indentLevel--;
                        EditorGUI.EndProperty();
                        return;
                    }

                    innerY += HeaderHeight - (EditorGUIUtility.singleLineHeight - LineSpacing);

                    // Filter fields
                    Rect contentRect = new Rect(innerX, innerY, innerW, elementHeight);
                    EditorGUI.PropertyField(contentRect, element, GUIContent.none, true);

                    y += boxHeight + LineSpacing;
                }
            }

            // Add / Clear buttons
            float buttonHeight = EditorGUIUtility.singleLineHeight;
            float halfWidth    = (width - 2f) * 0.5f;

            Rect addRect   = new Rect(position.x, y, halfWidth, buttonHeight);
            //Rect clearRect = new Rect(position.x + halfWidth + 2f, y, halfWidth, buttonHeight);

            if (GUI.Button(addRect, "Add Filter"))
            {
                ShowAddMenu(addRect, filtersProp, property.serializedObject);
            }

            //GUI.enabled = filtersProp.arraySize > 0;
            // if (GUI.Button(clearRect, "Clear All"))
            // {
            //     filtersProp.ClearArray();
            //     property.serializedObject.ApplyModifiedProperties();
            // }
            //GUI.enabled = true;

            y += buttonHeight + LineSpacing;
        }

        EditorGUI.indentLevel--;
        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float height = EditorGUIUtility.singleLineHeight; // foldout

        if (!property.isExpanded)
            return height;

        float spacing    = LineSpacing;
        var typeProp     = property.FindPropertyRelative("Type");
        var filtersProp  = property.FindPropertyRelative("Filters");

        height += spacing; // after foldout

        if (typeProp != null)
            height += EditorGUIUtility.singleLineHeight + spacing;

        if (filtersProp == null)
        {
            height += EditorGUIUtility.singleLineHeight * 2f + spacing;
            return height;
        }

        // "Filters" label
        height += EditorGUIUtility.singleLineHeight + spacing;

        if (filtersProp.arraySize == 0)
        {
            height += EditorGUIUtility.singleLineHeight * 1.5f + spacing;
        }
        else
        {
            for (int i = 0; i < filtersProp.arraySize; i++)
            {
                var element = filtersProp.GetArrayElementAtIndex(i);
                float elementHeight = EditorGUI.GetPropertyHeight(element, true);
                float boxHeight = HeaderHeight + elementHeight + BoxPadding * 2f;
                height += boxHeight + spacing;
            }
        }

        // Add / Clear buttons
        height += EditorGUIUtility.singleLineHeight + spacing;

        return height;
    }

    // --------- Add menu + helpers ---------

    private static void ShowAddMenu(Rect buttonRect, SerializedProperty filtersProp, SerializedObject serializedObject)
    {
        EnsureFilterTypes();

        if (_filterTypes == null || _filterTypes.Length == 0)
        {
            EditorUtility.DisplayDialog(
                "No filter types found",
                "No non-abstract RoadFilter implementations found in loaded assemblies.",
                "OK");
            return;
        }

        // Need a stable reference to re-find the property later
        var target     = serializedObject.targetObject;
        string propPath = filtersProp.propertyPath;

        GenericMenu menu = new GenericMenu();

        for (int i = 0; i < _filterTypes.Length; i++)
        {
            Type type      = _filterTypes[i];
            string path    = _filterTypePaths[i];
            menu.AddItem(new GUIContent(path), false, () =>
            {
                AddFilterOfType(target, propPath, type);
            });
        }

        menu.DropDown(buttonRect);
    }

    private static void AddFilterOfType(UnityEngine.Object target, string filtersPropPath, Type type)
    {
        if (target == null || type == null)
            return;

        var so = new SerializedObject(target);
        var filtersProp = so.FindProperty(filtersPropPath);
        if (filtersProp == null || !filtersProp.isArray)
            return;

        int index = filtersProp.arraySize;
        filtersProp.InsertArrayElementAtIndex(index);
        SerializedProperty element = filtersProp.GetArrayElementAtIndex(index);

        if (element.propertyType == SerializedPropertyType.ManagedReference)
        {
            object instance = Activator.CreateInstance(type);
            element.managedReferenceValue = instance;
        }
        else
        {
            // Wrong setup; clean up and bail
            filtersProp.DeleteArrayElementAtIndex(index);
        }

        so.ApplyModifiedProperties();
    }

    private static string GetManagedReferenceTypeName(SerializedProperty prop)
    {
        if (!prop.hasMultipleDifferentValues && !string.IsNullOrEmpty(prop.managedReferenceFullTypename))
        {
            // "AssemblyName TypeFullName"
            string fullName = prop.managedReferenceFullTypename;
            int spaceIndex  = fullName.IndexOf(' ');
            if (spaceIndex >= 0 && spaceIndex < fullName.Length - 1)
            {
                string typeName = fullName.Substring(spaceIndex + 1);
                int lastDot     = typeName.LastIndexOf('.');
                if (lastDot >= 0 && lastDot < typeName.Length - 1)
                    return typeName.Substring(lastDot + 1);
                return typeName;
            }
        }
        return null;
    }
}
#endif
