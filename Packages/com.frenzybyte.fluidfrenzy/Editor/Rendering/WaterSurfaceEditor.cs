using UnityEditor;
using UnityEngine;
using static FluidFrenzy.Editor.EditorExtensions;

namespace FluidFrenzy.Editor
{
#if UNITY_EDITOR
	[CustomEditor(typeof(WaterSurface))]
	public class WaterSurfaceEditor : FluidRendererEditor
	{
		class Styles
		{
			public static GUIContent foamLayerLabel = new GUIContent(
				"Foam Layer",
				@"A FoamLayer component that provides the dynamically generated foam mask texture for water rendering effects.

The component's primary role is to update and supply the dynamic foam mask texture, ensuring foam is applied accurately to the water material. It also handles necessary adjustments to the mask's texture coordinates (UVs) to maintain alignment across different rendering setups."
			);

			// Main Toggle Label
			public static GUIContent underwaterLabel = new GUIContent("Underwater Effects", "Controls the rendering settings when the camera is submerged.");

			// Section Headers
			public static GUIContent headerAbsorption = new GUIContent("Absorption");
			public static GUIContent headerMeniscus = new GUIContent("Meniscus (Water Line)");
			public static GUIContent headerScatter = new GUIContent("Scattering");
			public static GUIContent absorptionDepthScaleLabel = new GUIContent(
				"Depth Transparency",
				@"Controls the rate at which light is absorbed as it travels through the water.

Higher values result in darker water where light cannot penetrate as deeply. This scaling factor applies to the exponential decay of the waterColor."
			);
			public static GUIContent absorptionLimitsLabel = new GUIContent(
				"Depth Limits",
				@"Clamps the calculated absorption to a specific range (Min, Max).

Useful for preventing the water from becoming completely black at extreme depths or ensuring a minimum amount of visibility."
			);
			public static GUIContent waterColorLabel = new GUIContent(
				"Color",
				@"The base transmission color of the water.

This defines the color of the water as light passes through it. Brighter colors make the water look clear while darker colors make the water look thick and deep. This works with the alpha value and the absorption depth scale to decide how much the scene behind the water is tinted."
			);
			public static GUIContent meniscusThicknessLabel = new GUIContent(
				"Thickness",
				"The vertical thickness of the meniscus line (the water-air boundary) on the camera lens."
			);
			public static GUIContent meniscusBlurLabel = new GUIContent(
				"Blur",
				"The amount of blur applied to the meniscus line to soften the transition between underwater and above-water."
			);
			public static GUIContent meniscusDarknessLabel = new GUIContent(
				"Darkness",
				"Controls the intensity/darkness of the meniscus line effect."
			);
			public static GUIContent scatterColorLabel = new GUIContent(
				"Color",
				"The color of the light scattered within the water volume (subsurface scattering/fog color)."
			);
			public static GUIContent scatterAmbientIntensityLabel = new GUIContent(
				"Ambient Intensity",
				"The base ambient contribution to the scattering effect, independent of direct lighting."
			);
			public static GUIContent scatterLightIntensityLabel = new GUIContent(
				"Light Intensity",
				"Scales the influence of the main directional light on the scattering effect."
			);
			public static GUIContent scatterIntensityLabel = new GUIContent(
				"Total Intensity",
				"A global multiplier for the overall scattering intensity."
			);
		}

		SerializedProperty m_foamMappingProperty;
		SerializedProperty m_underWaterSettingsProperty;
		SerializedProperty m_underWaterEnabledProperty;
		SerializedProperty m_us_absorptionDepthScale;
		SerializedProperty m_us_absorptionLimits;
		SerializedProperty m_us_waterColor;
		SerializedProperty m_us_meniscusThickness;
		SerializedProperty m_us_meniscusBlur;
		SerializedProperty m_us_meniscusDarkness;
		SerializedProperty m_us_scatterColor;
		SerializedProperty m_us_scatterAmbientIntensity;
		SerializedProperty m_us_scatterLightIntensity;
		SerializedProperty m_us_scatterIntensity;

		public override void OnEnable()
		{
			base.OnEnable();
			m_foamMappingProperty = serializedObject.FindProperty("foamLayer");
			m_underWaterSettingsProperty = serializedObject.FindProperty("underWaterSettings");
			m_underWaterEnabledProperty = serializedObject.FindProperty("underWaterEnabled");

			m_us_absorptionDepthScale = m_underWaterSettingsProperty.FindPropertyRelative("absorptionDepthScale");
			m_us_absorptionLimits = m_underWaterSettingsProperty.FindPropertyRelative("absorptionLimits");
			m_us_waterColor = m_underWaterSettingsProperty.FindPropertyRelative("waterColor");
			m_us_meniscusThickness = m_underWaterSettingsProperty.FindPropertyRelative("meniscusThickness");
			m_us_meniscusBlur = m_underWaterSettingsProperty.FindPropertyRelative("meniscusBlur");
			m_us_meniscusDarkness = m_underWaterSettingsProperty.FindPropertyRelative("meniscusDarkness");
			m_us_scatterColor = m_underWaterSettingsProperty.FindPropertyRelative("scatterColor");
			m_us_scatterAmbientIntensity = m_underWaterSettingsProperty.FindPropertyRelative("scatterAmbientIntensity");
			m_us_scatterLightIntensity = m_underWaterSettingsProperty.FindPropertyRelative("scatterLightIntensity");
			m_us_scatterIntensity = m_underWaterSettingsProperty.FindPropertyRelative("scatterIntensity");

			Undo.undoRedoPerformed += OnUndoRedo;
		}

		private void OnDisable()
		{
			Undo.undoRedoPerformed -= OnUndoRedo;
		}

		private void OnUndoRedo()
		{
			if (target == null) return;
			serializedObject.Update();
			(target as WaterSurface).OnUnderwaterChanged();
		}

		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();
			serializedObject.UpdateIfRequiredOrScript();

			// Foam property change check
			using (var check = new EditorGUI.ChangeCheckScope())
			{
				EditorGUILayout.PropertyField(m_foamMappingProperty, Styles.foamLayerLabel);
				if (check.changed) serializedObject.ApplyModifiedProperties();
			}

			GUILayout.Space(5);

			DrawUnderwaterSettings();

			serializedObject.ApplyModifiedProperties();
		}

		protected virtual void DrawUnderwaterSettings()
		{
			WaterSurface surface = target as WaterSurface;

			bool toggleChanged;
			bool isExpanded = DrawFoldoutHeaderToggle(m_underWaterEnabledProperty, Styles.underwaterLabel, out toggleChanged);

			if (toggleChanged)
			{
				serializedObject.ApplyModifiedProperties();
				surface.OnUnderwaterChanged();
			}

			if (isExpanded)
			{
				using (new EditorGUI.IndentLevelScope())
				{
					if (!Application.isPlaying)
						EditorGUILayout.HelpBox("Underwater effects are only active during Play Mode.", MessageType.Info);

					EditorGUILayout.LabelField(Styles.headerAbsorption, EditorStyles.boldLabel);
					EditorGUILayout.PropertyField(m_us_waterColor, Styles.waterColorLabel);
					EditorGUILayout.PropertyField(m_us_absorptionDepthScale, Styles.absorptionDepthScaleLabel);
					MinMaxSlider(m_us_absorptionLimits, 0.0f, 1.0f, Styles.absorptionLimitsLabel);

					GUILayout.Space(5);
					EditorGUILayout.LabelField(Styles.headerMeniscus, EditorStyles.boldLabel);
					EditorGUILayout.PropertyField(m_us_meniscusThickness, Styles.meniscusThicknessLabel);
					EditorGUILayout.PropertyField(m_us_meniscusBlur, Styles.meniscusBlurLabel);
					EditorGUILayout.PropertyField(m_us_meniscusDarkness, Styles.meniscusDarknessLabel);

					GUILayout.Space(5);
					EditorGUILayout.LabelField(Styles.headerScatter, EditorStyles.boldLabel);
					EditorGUILayout.PropertyField(m_us_scatterColor, Styles.scatterColorLabel);
					EditorGUILayout.PropertyField(m_us_scatterAmbientIntensity, Styles.scatterAmbientIntensityLabel);
					EditorGUILayout.PropertyField(m_us_scatterLightIntensity, Styles.scatterLightIntensityLabel);
					EditorGUILayout.PropertyField(m_us_scatterIntensity, Styles.scatterIntensityLabel);
				}
			}
		}
	}
}
#endif