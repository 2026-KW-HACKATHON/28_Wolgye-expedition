using Mapbox.Example.Scripts.ModuleBehaviours;
using UnityEditor;
using UnityEngine;

namespace Mapbox.ImageModule.Editor
{
	/// <summary>
	/// Custom inspector for <c>TerrainLayerModuleScript</c>. Delegates settings rendering
	/// to <see cref="TerrainSettingsInspectorHelper"/> so the extract-on-override pattern
	/// and scene-aware warnings are consistent between terrain variants.
	/// </summary>
	[CustomEditor(typeof(TerrainLayerModuleScript))]
	public class TerrainLayerModuleScriptEditor : UnityEditor.Editor
	{
		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			// Standard script field.
			using (new EditorGUI.DisabledScope(true))
			{
				EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Script"));
			}

			// Draw everything except Settings with the default renderer.
			DrawPropertiesExcluding(serializedObject, "m_Script", "Settings");

			// Custom rendering for Settings so we can intercept ExtractCpuElevationData.
			var settingsProp = serializedObject.FindProperty("Settings");
			if (settingsProp != null)
			{
				TerrainSettingsInspectorHelper.Draw(settingsProp);
			}

			serializedObject.ApplyModifiedProperties();
		}
	}
}
