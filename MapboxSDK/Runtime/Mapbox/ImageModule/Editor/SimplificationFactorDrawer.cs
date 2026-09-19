using System.Linq;
using Mapbox.ImageModule.Terrain.Settings;
using UnityEditor;
using UnityEngine;

namespace Mapbox.ImageModule.Editor
{
	/// <summary>
	/// Inspector drawer for fields tagged with <see cref="SimplificationFactorAttribute"/>.
	/// Renders a dropdown of distinct-grid presets (each halving the resolution) and a
	/// live HelpBox describing the resulting grid, upgrading to a warning at coarse values.
	/// Legacy non-preset serialized values are snapped to the nearest preset.
	/// </summary>
	[CustomPropertyDrawer(typeof(SimplificationFactorAttribute))]
	public class SimplificationFactorDrawer : PropertyDrawer
	{
		private const float Spacing = 2f;

		// Only factors that produce a distinct grid size are worth exposing — everything in
		// between collapses via integer division. Each entry halves the grid resolution.
		private static readonly int[] PresetFactors = { 1, 2, 4, 8, 16, 32, 64, 128 };

		// Cached preset+label arrays per attribute. OnGUI runs every repaint; without this
		// the LINQ chain allocates fresh arrays continuously while the Inspector is open.
		private int[] _cachedFactors;
		private GUIContent[] _cachedLabels;
		private int _cachedMin = -1;
		private int _cachedMax = -1;
		private int _cachedVertexBase = -1;

		private void EnsurePresetsCached(SimplificationFactorAttribute attr)
		{
			if (_cachedFactors != null && _cachedMin == attr.Min && _cachedMax == attr.Max && _cachedVertexBase == attr.VertexBase)
			{
				return;
			}
			_cachedFactors = PresetFactors.Where(f => f >= attr.Min && f <= attr.Max).ToArray();
			_cachedLabels = _cachedFactors.Select(f => new GUIContent(BuildLabel(f, attr.VertexBase))).ToArray();
			_cachedMin = attr.Min;
			_cachedMax = attr.Max;
			_cachedVertexBase = attr.VertexBase;
		}

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			label = EditorGUI.BeginProperty(position, label, property);

			var attr = (SimplificationFactorAttribute)attribute;
			EnsurePresetsCached(attr);
			var factors = _cachedFactors;
			var labels = _cachedLabels;

			var current = property.intValue;
			var inPresets = System.Array.IndexOf(factors, current) >= 0;
			// Don't write through SerializedProperty during paint just because the
			// value isn't in the preset list — that silently dirties every legacy
			// asset on first open. Show a warning and let the user pick a preset
			// (which Unity then records as a deliberate, undoable change).
			var fieldRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
			EditorGUI.BeginChangeCheck();
			EditorGUI.showMixedValue = property.hasMultipleDifferentValues;
			var newValue = EditorGUI.IntPopup(fieldRect, label, current, labels, factors);
			EditorGUI.showMixedValue = false;
			if (EditorGUI.EndChangeCheck())
			{
				property.intValue = newValue;
			}

			string message;
			MessageType severity;
			if (!inPresets)
			{
				message = $"Legacy value {current} is not a preset. Pick a preset above to migrate; the value is otherwise left untouched.";
				severity = MessageType.Warning;
			}
			else
			{
				(message, severity) = BuildMessage(property.intValue, attr.VertexBase);
			}
			var helpHeight = CalcHelpBoxHeight(message, position.width);
			var helpRect = new Rect(position.x, fieldRect.yMax + Spacing, position.width, helpHeight);
			EditorGUI.HelpBox(helpRect, message, severity);

			EditorGUI.EndProperty();
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			var attr = (SimplificationFactorAttribute)attribute;
			EnsurePresetsCached(attr);
			// Mirror OnGUI's branch so the help-box height matches the actual rendered
			// message. The legacy-value warning is longer than the preset description,
			// and using the preset string for height while painting the legacy string
			// caused the HelpBox to clip and overlap the next inspector row.
			var current = property.intValue;
			var inPresets = System.Array.IndexOf(_cachedFactors, current) >= 0;
			string message;
			if (!inPresets)
			{
				message = $"Legacy value {current} is not a preset. Pick a preset above to migrate; the value is otherwise left untouched.";
			}
			else
			{
				(message, _) = BuildMessage(current, attr.VertexBase);
			}
			return EditorGUIUtility.singleLineHeight + Spacing + CalcHelpBoxHeight(message, EditorGUIUtility.currentViewWidth - 40f);
		}

		private static string BuildLabel(int factor, int vertexBase)
		{
			var sampleCount = vertexBase / Mathf.Max(1, factor);
			var gridSide = sampleCount + 1;
			return $"{factor}  ({gridSide}x{gridSide} verts)";
		}

		private static (string message, MessageType severity) BuildMessage(int factor, int vertexBase)
		{
			var clampedFactor = Mathf.Max(1, factor);
			var sampleCount = vertexBase / clampedFactor;
			var gridSide = sampleCount + 1;
			var verts = gridSide * gridSide;
			var baseLine = $"{gridSide}x{gridSide} vertex grid per tile ({verts} verts, {sampleCount}x{sampleCount} quads).";

			if (sampleCount >= 64)
			{
				return (baseLine + " Highest visual quality — heaviest CPU/GPU cost, especially with colliders enabled.", MessageType.Info);
			}
			if (sampleCount >= 16)
			{
				return (baseLine + " Good balance of detail and performance.", MessageType.Info);
			}
			if (sampleCount >= 8)
			{
				return (baseLine + " Lower-density geometry — fine terrain details will be lost.", MessageType.Info);
			}
			return (baseLine + " Very coarse — terrain will look blocky and neighboring tiles may visibly mismatch at seams.", MessageType.Warning);
		}

		// Reused across CalcHelpBoxHeight calls. GetPropertyHeight runs multiple times
		// per repaint while the inspector is open; allocating a fresh GUIContent each
		// call shows up as steady GC churn even in idle Editor sessions.
		private static readonly GUIContent _helpBoxContent = new GUIContent();

		private static float CalcHelpBoxHeight(string message, float width)
		{
			_helpBoxContent.text = message;
			var style = EditorStyles.helpBox;
			var textWidth = Mathf.Max(40f, width - 32f);
			return Mathf.Max(EditorGUIUtility.singleLineHeight * 2f, style.CalcHeight(_helpBoxContent, textWidth));
		}
	}
}
