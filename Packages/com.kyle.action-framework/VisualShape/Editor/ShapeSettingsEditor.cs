using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace VisualShape {
	/// <summary>Helper for adding project settings</summary>
	static class VisualShapeSettingsRegister {
		const string PROVIDER_PATH = "Project/VisualShape";
		const string SETTINGS_LABEL = "VisualShape";

		[SettingsProvider]
		public static SettingsProvider CreateMyCustomSettingsProvider () {
			var provider = new SettingsProvider(PROVIDER_PATH, SettingsScope.Project) {
				label = SETTINGS_LABEL,
				guiHandler = (searchContext) =>
				{
					var settings = new SerializedObject(ShapeSettings.GetSettingsAsset());
					EditorGUILayout.HelpBox("Opacity of lines, solid objects and text drawn using VisualShape. When drawing behind other objects, an additional opacity multiplier is applied.", MessageType.None);
					EditorGUILayout.Separator();
					EditorGUILayout.LabelField("Lines", EditorStyles.boldLabel);
					EditorGUILayout.Slider(settings.FindProperty("settings.lineOpacity"), 0, 1, new GUIContent("Opacity", "Opacity of lines when in front of objects"));
					EditorGUILayout.Slider(settings.FindProperty("settings.lineOpacityBehindObjects"), 0, 1, new GUIContent("Opacity (occluded)", "Additional opacity multiplier of lines when behind or inside objects"));
					EditorGUILayout.Separator();
					EditorGUILayout.LabelField("Solids", EditorStyles.boldLabel);
					EditorGUILayout.Slider(settings.FindProperty("settings.solidOpacity"), 0, 1, new GUIContent("Opacity", "Opacity of solid objects when in front of other objects"));
					EditorGUILayout.Slider(settings.FindProperty("settings.solidOpacityBehindObjects"), 0, 1, new GUIContent("Opacity (occluded)", "Additional opacity multiplier of solid objects when behind or inside other objects"));
					EditorGUILayout.Separator();
					EditorGUILayout.LabelField("Text", EditorStyles.boldLabel);
					EditorGUILayout.Slider(settings.FindProperty("settings.textOpacity"), 0, 1, new GUIContent("Opacity", "Opacity of text when in front of other objects"));
					EditorGUILayout.Slider(settings.FindProperty("settings.textOpacityBehindObjects"), 0, 1, new GUIContent("Opacity (occluded)", "Additional opacity multiplier of text when behind or inside other objects"));
					EditorGUILayout.Separator();
					EditorGUILayout.Slider(settings.FindProperty("settings.curveResolution"), 0.1f, 3f, new GUIContent("Curve resolution", "Higher values will make curves smoother, but also a bit slower to draw."));

					settings.ApplyModifiedProperties();
					if (GUILayout.Button("Reset to default")) {
						var def = ShapeSettings.DefaultSettings;
						var current = ShapeSettings.GetSettingsAsset();
						current.settings.lineOpacity = def.lineOpacity;
						current.settings.lineOpacityBehindObjects = def.lineOpacityBehindObjects;
						current.settings.solidOpacity = def.solidOpacity;
						current.settings.solidOpacityBehindObjects = def.solidOpacityBehindObjects;
						current.settings.textOpacity = def.textOpacity;
						current.settings.textOpacityBehindObjects = def.textOpacityBehindObjects;
						current.settings.curveResolution = def.curveResolution;
						EditorUtility.SetDirty(current);
					}
				},

				keywords = new HashSet<string>(new[] { "VisualShape", "Wire", "opacity" })
			};

			return provider;
		}
	}
}
