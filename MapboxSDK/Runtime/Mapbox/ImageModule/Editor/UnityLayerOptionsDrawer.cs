using Mapbox.ImageModule.Terrain.Settings;
using UnityEditor;
using UnityEngine;

namespace Mapbox.ImageModule.Editor
{
	/// <summary>
	/// Inspector drawer for <see cref="UnityLayerOptions"/>. Shows <c>addToLayer</c> as a
	/// checkbox and the <c>layerId</c> dropdown (via the shared <c>GameObjectLayer</c>
	/// drawer) grayed-out when <c>addToLayer</c> is off, so users can see the selected
	/// layer but can't change it until they opt in.
	/// </summary>
	[CustomPropertyDrawer(typeof(UnityLayerOptions))]
	public class UnityLayerOptionsDrawer : PropertyDrawer
	{
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.BeginProperty(position, label, property);

			var line = EditorGUIUtility.singleLineHeight;
			var spacing = EditorGUIUtility.standardVerticalSpacing;

			var foldoutRect = new Rect(position.x, position.y, position.width, line);
			property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, label, true);

			if (property.isExpanded)
			{
				var addToLayer = property.FindPropertyRelative("addToLayer");
				var layerId = property.FindPropertyRelative("layerId");

				EditorGUI.indentLevel++;
				var y = position.y + line + spacing;

				var addRect = new Rect(position.x, y, position.width, line);
				EditorGUI.PropertyField(addRect, addToLayer);
				y += line + spacing;

				var layerRect = new Rect(position.x, y, position.width, EditorGUI.GetPropertyHeight(layerId, true));
				using (new EditorGUI.DisabledScope(!addToLayer.boolValue))
				{
					EditorGUI.PropertyField(layerRect, layerId);
				}
				EditorGUI.indentLevel--;
			}

			EditorGUI.EndProperty();
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			var line = EditorGUIUtility.singleLineHeight;
			var spacing = EditorGUIUtility.standardVerticalSpacing;

			if (!property.isExpanded)
			{
				return line;
			}

			var layerId = property.FindPropertyRelative("layerId");
			// foldout + addToLayer + layerId (always rendered, just grayed out when inactive).
			return line + spacing + line + spacing + EditorGUI.GetPropertyHeight(layerId, true);
		}
	}
}
