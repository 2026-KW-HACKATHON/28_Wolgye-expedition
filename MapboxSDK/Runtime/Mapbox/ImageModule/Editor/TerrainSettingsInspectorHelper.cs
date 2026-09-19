using UnityEditor;
using UnityEngine;

namespace Mapbox.ImageModule.Editor
{
	/// <summary>
	/// Renders a <c>TerrainLayerModuleSettings</c> SerializedProperty with two inspector
	/// enhancements: (1) the <c>ExtractCpuElevationData</c> toggle is force-checked and
	/// disabled when another setting requires CPU elevation; (2) a warning HelpBox appears
	/// when the scene contains a module that typically needs CPU elevation but extraction
	/// is user-disabled. Shared by both terrain-settings editors.
	/// </summary>
	public static class TerrainSettingsInspectorHelper
	{
		private const string ExtractFieldName = "ExtractCpuElevationData";
		private const string UseShaderFieldName = "UseShaderTerrain";
		private const string ElevationPropsFieldName = "ElevationLayerProperties";
		private const string ColliderOptionsFieldName = "colliderOptions";
		private const string AddColliderFieldName = "addCollider";

		public static void Draw(SerializedProperty settingsProp)
		{
			settingsProp.isExpanded = EditorGUILayout.Foldout(settingsProp.isExpanded, settingsProp.displayName, true);
			if (!settingsProp.isExpanded)
			{
				return;
			}

			var useShaderProp = settingsProp.FindPropertyRelative(UseShaderFieldName);
			var colliderProp = settingsProp
				.FindPropertyRelative(ElevationPropsFieldName)
				?.FindPropertyRelative(ColliderOptionsFieldName)
				?.FindPropertyRelative(AddColliderFieldName);

			var useShaderOff = useShaderProp != null && !useShaderProp.boolValue;
			var colliderOn = colliderProp != null && colliderProp.boolValue;
			var forcedOn = useShaderOff || colliderOn;
			var reason = useShaderOff
				? "CPU-elevation rendering (UseShaderTerrain is off)"
				: "the terrain collider (addCollider is on)";

			EditorGUI.indentLevel++;
			var child = settingsProp.Copy();
			var end = settingsProp.GetEndProperty();
			child.NextVisible(true);
			while (!SerializedProperty.EqualContents(child, end))
			{
				if (child.name == ExtractFieldName && forcedOn)
				{
					// Display-only override. We do NOT write child.boolValue = true here:
					// the runtime evaluates `TerrainLayerModuleSettings.NeedsCpuElevation`
					// (an OR over the forcing flags) directly, so the persisted value
					// stays as the user set it. Writing during OnGUI would silently dirty
					// every asset/scene on inspection.
					using (new EditorGUI.DisabledScope(true))
					{
						EditorGUI.showMixedValue = false;
						EditorGUILayout.Toggle(new GUIContent(child.displayName, child.tooltip), true);
						EditorGUI.showMixedValue = child.hasMultipleDifferentValues;
					}
					EditorGUILayout.HelpBox(
						$"Extraction is force-enabled by {reason}. CPU elevation will be decoded regardless of this checkbox.",
						MessageType.Info);
				}
				else
				{
					EditorGUILayout.PropertyField(child, true);
				}

				if (!child.NextVisible(false))
				{
					break;
				}
			}
			EditorGUI.indentLevel--;

			DrawSceneNeedsExtractionWarning(settingsProp);
		}

		// Warns if extraction is genuinely user-disabled (box off AND nothing forces it on)
		// while the scene contains modules that typically need ground heights.
		private static void DrawSceneNeedsExtractionWarning(SerializedProperty settingsProp)
		{
			var extractProp = settingsProp.FindPropertyRelative(ExtractFieldName);
			if (extractProp == null || extractProp.boolValue)
			{
				return;
			}
			var useShaderProp = settingsProp.FindPropertyRelative(UseShaderFieldName);
			var colliderProp = settingsProp
				.FindPropertyRelative(ElevationPropsFieldName)
				?.FindPropertyRelative(ColliderOptionsFieldName)
				?.FindPropertyRelative(AddColliderFieldName);
			var forcedOn =
				(useShaderProp != null && !useShaderProp.boolValue) ||
				(colliderProp != null && colliderProp.boolValue);
			if (forcedOn)
			{
				return;
			}

			if (SceneHasFeaturesNeedingCpuElevation(out var detected))
			{
				EditorGUILayout.HelpBox(
					$"CPU elevation extraction is disabled, but this scene contains a {detected} that typically snaps to terrain. " +
					"Ground-clamped features will be placed at Y=0 and the elevation query APIs will return 0. " +
					"Re-enable ExtractCpuElevationData unless you are sure nothing in this scene needs ground heights.",
					MessageType.Warning);
			}
		}

		private static readonly string[] WatchedTypeNames =
		{
			"VectorLayerModuleScript",
			"MapboxComponentsModuleScript"
		};

		// Cached scene-scan result, invalidated whenever the hierarchy changes. Without this,
		// every Inspector OnGUI repaint runs FindObjectsByType<MonoBehaviour> across the
		// whole active scene — visibly stalls when many tiles are alive.
		private static bool _sceneScanValid;
		private static bool _sceneScanResult;
		private static string _sceneScanDetectedTypeName;
		private static bool _hierarchyHookInstalled;

		private static bool SceneHasFeaturesNeedingCpuElevation(out string detectedTypeName)
		{
			if (!_hierarchyHookInstalled)
			{
				EditorApplication.hierarchyChanged += () => _sceneScanValid = false;
				_hierarchyHookInstalled = true;
			}
			if (_sceneScanValid)
			{
				detectedTypeName = _sceneScanDetectedTypeName;
				return _sceneScanResult;
			}

			_sceneScanDetectedTypeName = null;
			_sceneScanResult = false;
			var all = Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
			foreach (var mb in all)
			{
				if (mb == null) continue;
				var name = mb.GetType().Name;
				for (int i = 0; i < WatchedTypeNames.Length; i++)
				{
					if (name == WatchedTypeNames[i])
					{
						_sceneScanDetectedTypeName = name;
						_sceneScanResult = true;
						goto done;
					}
				}
			}
			done:
			_sceneScanValid = true;
			detectedTypeName = _sceneScanDetectedTypeName;
			return _sceneScanResult;
		}
	}
}
