using Mapbox.LocationModule;
using Mapbox.LocationModule;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(LocationProviderFactory))]
public class LocationProviderFactoryEditor : Editor
{
	private SerializedProperty _locationProviderTypeProp;
	private SerializedProperty _mapProp;
	private SerializedProperty _editorLatLngProp;
	private SerializedProperty _unitySettingsProp;
	private SerializedProperty _mapboxSettingsProp;

	private void OnEnable()
	{
		_locationProviderTypeProp = serializedObject.FindProperty("LocationProviderType");
		_editorLatLngProp = serializedObject.FindProperty("EditorLatitudeLongitude");
		_unitySettingsProp = serializedObject.FindProperty("_unityLocationProviderSettings");
		_mapboxSettingsProp = serializedObject.FindProperty("_mapboxLocationProviderSettings");
	}

	public override void OnInspectorGUI()
	{
		serializedObject.Update();

		EditorGUILayout.PropertyField(_locationProviderTypeProp);

		EditorGUILayout.Space();

		var providerType =
			(LocationProviderType)_locationProviderTypeProp.enumValueIndex;

		switch (providerType)
		{
			case LocationProviderType.StaticLocationProvider:
				DrawEditorProvider();
				break;

			case LocationProviderType.UnityLocationProvider:
				DrawUnityProvider();
				break;

			case LocationProviderType.MapboxLocationProvider:
				DrawMapboxProvider();
				break;

			// case LocationProviderType.CustomLocationProvider:
			// 	EditorGUILayout.HelpBox(
			// 		"Custom location provider not implemented yet.",
			// 		MessageType.Info
			// 	);
			// 	break;
		}

		serializedObject.ApplyModifiedProperties();
	}

	private void DrawEditorProvider()
	{
		EditorGUILayout.HelpBox(
			"Uses a fixed editor-only location.",
			MessageType.None
		);

		EditorGUILayout.PropertyField(_editorLatLngProp);
	}

	private void DrawUnityProvider()
	{
		EditorGUILayout.HelpBox(
			"Uses Unity Input.location service.",
			MessageType.None
		);

		EditorGUILayout.PropertyField(_unitySettingsProp);
	}

	private void DrawMapboxProvider()
	{
		EditorGUILayout.HelpBox(
			"Mapbox provider is experimental.",
			MessageType.Warning
		);

		EditorGUILayout.PropertyField(_mapboxSettingsProp);
	}
}
