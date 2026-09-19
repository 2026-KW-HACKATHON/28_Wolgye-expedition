using Mapbox.ImageModule.Editor;
using UnityEditor;
using UnityEngine;

namespace Mapbox.CustomImageryModule.Editor
{
	/// <summary>
	/// Custom inspector for <c>CustomTerrainLayerModuleScript</c>. Shares rendering logic
	/// with the main terrain editor via <see cref="TerrainSettingsInspectorHelper"/>.
	/// </summary>
	[CustomEditor(typeof(CustomTerrainLayerModuleScript))]
	public class CustomTerrainLayerModuleScriptEditor : UnityEditor.Editor
	{
		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			using (new EditorGUI.DisabledScope(true))
			{
				EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Script"));
			}

			DrawPropertiesExcluding(serializedObject, "m_Script", "Settings");

			var settingsProp = serializedObject.FindProperty("Settings");
			if (settingsProp != null)
			{
				TerrainSettingsInspectorHelper.Draw(settingsProp);
			}

			serializedObject.ApplyModifiedProperties();
		}
	}
}
