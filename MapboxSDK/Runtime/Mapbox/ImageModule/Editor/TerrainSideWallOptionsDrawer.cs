using Mapbox.ImageModule.Terrain.Settings;
using UnityEditor;
using UnityEngine;

namespace Mapbox.ImageModule.Editor
{
	/// <summary>
	/// Inspector drawer for <see cref="TerrainSideWallOptions"/>. Mirrors the
	/// <see cref="UnityLayerOptionsDrawer"/> pattern: <c>wallHeight</c> is always visible
	/// but grayed out until <c>isActive</c> is enabled.
	/// </summary>
	[CustomPropertyDrawer(typeof(TerrainSideWallOptions))]
	public class TerrainSideWallOptionsDrawer : PropertyDrawer
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
				var isActive = property.FindPropertyRelative("isActive");
				var wallHeight = property.FindPropertyRelative("wallHeight");

				EditorGUI.indentLevel++;
				var y = position.y + line + spacing;

				var activeRect = new Rect(position.x, y, position.width, line);
				EditorGUI.PropertyField(activeRect, isActive);
				y += line + spacing;

				var heightRect = new Rect(position.x, y, position.width, EditorGUI.GetPropertyHeight(wallHeight, true));
				using (new EditorGUI.DisabledScope(!isActive.boolValue))
				{
					EditorGUI.PropertyField(heightRect, wallHeight);
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

			var wallHeight = property.FindPropertyRelative("wallHeight");
			return line + spacing + line + spacing + EditorGUI.GetPropertyHeight(wallHeight, true);
		}
	}
}
