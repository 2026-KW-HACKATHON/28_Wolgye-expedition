using Mapbox.ImageModule.Terrain.Settings;
using UnityEditor;
using UnityEngine;

namespace Mapbox.ImageModule.Editor
{
	/// <summary>
	/// Inspector drawer for <see cref="TerrainColliderOptions"/>. Grays out the
	/// dedicated-layer options until <c>addCollider</c> is enabled, and further grays out
	/// the layer dropdown until <c>useDedicatedColliderLayer</c> is enabled. Same reveal
	/// pattern used by <see cref="UnityLayerOptionsDrawer"/> and
	/// <see cref="TerrainSideWallOptionsDrawer"/>.
	/// </summary>
	[CustomPropertyDrawer(typeof(TerrainColliderOptions))]
	public class TerrainColliderOptionsDrawer : PropertyDrawer
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
				var addCollider = property.FindPropertyRelative("addCollider");
				var asyncBake = property.FindPropertyRelative("asyncBakeCollider");
				var useDedicated = property.FindPropertyRelative("useDedicatedColliderLayer");
				var colliderLayer = property.FindPropertyRelative("colliderLayerId");

				EditorGUI.indentLevel++;
				var y = position.y + line + spacing;

				EditorGUI.PropertyField(new Rect(position.x, y, position.width, line), addCollider);
				y += line + spacing;

				using (new EditorGUI.DisabledScope(!addCollider.boolValue))
				{
					EditorGUI.PropertyField(new Rect(position.x, y, position.width, line), asyncBake);
					y += line + spacing;

					EditorGUI.PropertyField(new Rect(position.x, y, position.width, line), useDedicated);
					y += line + spacing;

					using (new EditorGUI.DisabledScope(!useDedicated.boolValue))
					{
						var layerHeight = EditorGUI.GetPropertyHeight(colliderLayer, true);
						EditorGUI.PropertyField(new Rect(position.x, y, position.width, layerHeight), colliderLayer);
					}
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

			var colliderLayer = property.FindPropertyRelative("colliderLayerId");
			// foldout + addCollider + asyncBake + useDedicated + colliderLayer = 4 line-rows + 1 var-row
			return line + spacing + line + spacing + line + spacing + line + spacing + EditorGUI.GetPropertyHeight(colliderLayer, true);
		}
	}
}
