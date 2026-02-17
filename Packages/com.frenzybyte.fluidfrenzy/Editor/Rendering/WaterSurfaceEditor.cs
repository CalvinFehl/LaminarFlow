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

			// Main Toggle Labels
			public static GUIContent underwaterLabel = new GUIContent("Underwater Effects", "Controls the rendering settings when the camera is submerged.");
			public static GUIContent reflectionsLabel = new GUIContent("Surface Reflections", "Controls real-time reflections on the water surface.");
			public static GUIContent causticsLabel = new GUIContent("Caustics", "Controls the animated underwater light patterns projected onto the scene.");

			// Section Headers
			public static GUIContent headerAbsorption = new GUIContent("Absorption");
			public static GUIContent headerMeniscus = new GUIContent("Meniscus (Water Line)");
			public static GUIContent headerScatter = new GUIContent("Scattering");
			public static GUIContent headerPlanar = new GUIContent("Planar Reflection Settings");
			public static GUIContent headerCausticsAssets = new GUIContent("Assets & Animation");
			public static GUIContent headerCausticsVisuals = new GUIContent("Visual Settings");

			// Underwater Labels
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

			// Reflection Labels
			public static GUIContent refResolutionLabel = new GUIContent("Resolution", "The quality/resolution of the generated planar reflection texture.");
			public static GUIContent refCullingMaskLabel = new GUIContent("Culling Mask", "Which layers the planar reflection camera renders.");
			public static GUIContent refClearFlagsLabel = new GUIContent("Clear Flags", "What to display in empty areas of the planar reflection's view.");
			public static GUIContent refClipPlaneLabel = new GUIContent("Clip Plane Offset", "Vertical offset to apply to the reflection plane.");
			public static GUIContent refSmoothPosLabel = new GUIContent("Smooth Position", "Smoothes the reflection plane's height/position to prevent jitter.");
			public static GUIContent refRendererIDLabel = new GUIContent("Renderer ID (URP)", "SRP Renderer index to use for the planar reflection pass.");
			public static GUIContent refShadowQualityLabel = new GUIContent("Shadow Quality", "Controls shadow rendering in the reflection (BiRP Only).");

			// Caustics Labels
			public static class Caustics
			{
				public static GUIContent projectionHeader = new GUIContent("Texture Projection");
				public static GUIContent wavesHeader = new GUIContent("Wave Highlights");
				public static GUIContent globalHeader = new GUIContent("Global Settings");

				public static GUIContent textureIntensityLabel = new GUIContent(
					"Texture Intensity",
					@"The brightness multiplier for the projected caustics texture.

An independent scalar specifically for the animated texture component of the effect."
				);
				public static GUIContent fpsLabel = new GUIContent(
					"Animation FPS",
					@"The playback speed of the animated caustics texture sequence.

Defines how many frames per second the texture advances. Higher values result in faster, smoother motion."
				);
				public static GUIContent tilingLabel = new GUIContent(
					"Tiling",
					@"Controls the scale of the projected caustics pattern.

Higher values increase the tiling frequency, making the pattern appear smaller and more dense across the environment."
				);
				public static GUIContent chromaticAberrationLabel = new GUIContent(
					"Chromatic Aberration",
					@"The strength of the color splitting effect at the edges of the caustics.

Simulates light dispersion (prismatic effect), creating rainbow-like fringing around high-contrast areas of the pattern."
				);
				public static GUIContent waveUVOffsetLabel = new GUIContent(
					"Wave UV Distortion",
					@"The strength of the UV distortion applied to the caustics based on surface wave normals.

Simulates refractive warping by shifting the texture coordinates relative to the waves above."
				);
				public static GUIContent channelMaskLabel = new GUIContent(
					"Channel Mask",
					@"Defines which texture color channels contribute to the final caustics pattern.

Useful for isolating specific channels in packed textures or creating monochromatic effects."
				);

				public static GUIContent waveIntensityLabel = new GUIContent(
					"Wave Intensity",
					@"The brightness of the procedural glints generated by surface wave curvature.

Unlike the texture projection, these highlights are calculated analytically from wave refraction to provide a direct link between the surface and the seafloor."
				);
				public static GUIContent waveSharpnessLabel = new GUIContent(
					"Wave Sharpness",
					@"Controls the focus and size of the procedural wave highlights.

Higher values result in sharper, thinner glints (lensing effect), while lower values create broader, softer highlights."
				);

				public static GUIContent globalIntensityLabel = new GUIContent(
					"Global Intensity",
					@"A multiplier for all caustic lighting contributions.

Scales both the texture projection and the procedural wave highlights simultaneously."
				);
				public static GUIContent darknessLabel = new GUIContent(
					"Darkness",
					@"Controls how much the sea floor is darkened in the areas between light patterns.

Increasing this value darkens the ""caustic shadows,"" making the bright light patterns appear more high-contrast and prominent."
				);
				public static GUIContent shadowIntensityLabel = new GUIContent(
					"Shadow Intensity",
					@"Controls the visibility of caustics within areas shadowed by external light sources.

A value of 0 makes caustics completely invisible in shadow, while a value of 1 allows them to remain fully visible."
				);
				public static GUIContent fadeInRangeLabel = new GUIContent(
					"Surface Fade-In",
					@"Defines the depth range near the surface where the caustics begin to appear.

The X value represents the depth where the effect starts, and the Y value is where it reaches full intensity. This prevents visual ""popping"" at the water line."
				);
				public static GUIContent fadeOutRangeLabel = new GUIContent(
					"Depth Fade-Out",
					@"Defines the depth range where the caustics gradually disappear as light is absorbed.

The X value is the depth where fading begins, and the Y value is the depth where caustics are completely extinguished."
				);

				public static GUIContent triplanarLabel = new GUIContent(
					"Triplanar Projection",
					@"Enables triplanar projection to prevent texture stretching on vertical surfaces.

Projects the texture from three orthogonal axes (X, Y, Z) instead of a single top-down projection. Essential for maintaining pattern consistency on cliffs, walls, and steep underwater terrain."
				);
				public static GUIContent foamMaskingLabel = new GUIContent(
					"Foam Masking",
					@"Controls how much surface foam occludes the caustics on the seafloor.

Simulates the diffusive nature of bubbles. Thick foam scatters light, preventing sharp caustics from forming and casting a soft shadow on the environment below."
				);
			}
		}

		// Foam
		SerializedProperty m_foamMappingProperty;

		// Underwater
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

		// Caustics
		SerializedProperty m_causticsEnabledProperty;
		SerializedProperty m_causticsSettingsProperty;
		SerializedProperty m_cs_fps;
		SerializedProperty m_cs_tiling;
		SerializedProperty m_cs_triplanar;
		SerializedProperty m_cs_darkness;
		SerializedProperty m_cs_shadowIntensity;
		SerializedProperty m_cs_channelMask;
		SerializedProperty m_cs_chromaticAberration;
		SerializedProperty m_cs_fadeInRange;
		SerializedProperty m_cs_fadeOutRange;

		SerializedProperty m_cs_textureIntensity;
		SerializedProperty m_cs_waveUVOffset;
		SerializedProperty m_cs_waveIntensity;
		SerializedProperty m_cs_waveSharpness;
		SerializedProperty m_cs_globalIntensity;
		SerializedProperty m_cs_foamMasking;

		// Reflections
		SerializedProperty m_reflectionsEnabledProperty;
		SerializedProperty m_reflectionSettingsProperty;
		SerializedProperty m_rs_resolution;
		SerializedProperty m_rs_cullingMask;
		SerializedProperty m_rs_clearFlags;
		SerializedProperty m_rs_clipPlane;
		SerializedProperty m_rs_smoothPosition;
#if FLUIDFRENZY_EDITOR_URP_SUPPORT
		SerializedProperty m_rs_rendererID;
#else
		SerializedProperty m_rs_shadowQuality;
#endif

		public override void OnEnable()
		{
			base.OnEnable();

			// Foam
			m_foamMappingProperty = serializedObject.FindProperty("foamLayer");

			// Underwater
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

			// Caustics
			m_causticsEnabledProperty = serializedObject.FindProperty("causticsEnabled");
			m_causticsSettingsProperty = serializedObject.FindProperty("causticsSettings");

			m_cs_fps = m_causticsSettingsProperty.FindPropertyRelative("fps");
			m_cs_tiling = m_causticsSettingsProperty.FindPropertyRelative("tiling");
			m_cs_triplanar = m_causticsSettingsProperty.FindPropertyRelative("triplanar");
			m_cs_darkness = m_causticsSettingsProperty.FindPropertyRelative("darkness");
			m_cs_shadowIntensity = m_causticsSettingsProperty.FindPropertyRelative("shadowIntensity");
			m_cs_channelMask = m_causticsSettingsProperty.FindPropertyRelative("channelMask");
			m_cs_chromaticAberration = m_causticsSettingsProperty.FindPropertyRelative("chromaticAberration");
			m_cs_fadeInRange = m_causticsSettingsProperty.FindPropertyRelative("fadeInRange");
			m_cs_fadeOutRange = m_causticsSettingsProperty.FindPropertyRelative("fadeOutRange");

			m_cs_textureIntensity = m_causticsSettingsProperty.FindPropertyRelative("textureIntensity");
			m_cs_waveUVOffset = m_causticsSettingsProperty.FindPropertyRelative("waveUVOffset");
			m_cs_waveIntensity = m_causticsSettingsProperty.FindPropertyRelative("waveIntensity");
			m_cs_waveSharpness = m_causticsSettingsProperty.FindPropertyRelative("waveSharpness");
			m_cs_globalIntensity = m_causticsSettingsProperty.FindPropertyRelative("globalIntensity");
			m_cs_foamMasking = m_causticsSettingsProperty.FindPropertyRelative("foamMasking");

			// Reflections
			m_reflectionsEnabledProperty = serializedObject.FindProperty("reflectionsEnabled");
			m_reflectionSettingsProperty = serializedObject.FindProperty("reflectionSettings");

			m_rs_resolution = m_reflectionSettingsProperty.FindPropertyRelative("resolution");
			m_rs_cullingMask = m_reflectionSettingsProperty.FindPropertyRelative("cullingMask");
			m_rs_clearFlags = m_reflectionSettingsProperty.FindPropertyRelative("clearFlags");
			m_rs_clipPlane = m_reflectionSettingsProperty.FindPropertyRelative("clipPlane");
			m_rs_smoothPosition = m_reflectionSettingsProperty.FindPropertyRelative("smoothPosition");
#if FLUIDFRENZY_EDITOR_URP_SUPPORT
			m_rs_rendererID = m_reflectionSettingsProperty.FindPropertyRelative("rendererID");
#else
			m_rs_shadowQuality = m_reflectionSettingsProperty.FindPropertyRelative("shadowQuality");
#endif

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
			WaterSurface surface = target as WaterSurface;
			if (surface != null)
			{
				//surface.OnUnderwaterChanged();
				//surface.OnReflectionsChanged();
			}
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
			DrawCausticsSettings();
			DrawReflectionSettings();

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
				//surface.OnUnderwaterChanged();
			}

			if (isExpanded)
			{
				using (new EditorGUI.IndentLevelScope())
				{
					if (!Application.isPlaying)
						EditorGUILayout.HelpBox("Underwater effects are only active during Play Mode.", MessageType.Info);

					if (DrawFoldoutHeader(m_us_waterColor, Styles.headerAbsorption, true))
					{
						using (new EditorGUI.IndentLevelScope())
						{
							EditorGUILayout.PropertyField(m_us_waterColor, Styles.waterColorLabel);
							EditorGUILayout.PropertyField(m_us_absorptionDepthScale, Styles.absorptionDepthScaleLabel);
							MinMaxSlider(m_us_absorptionLimits, 0.0f, 1.0f, Styles.absorptionLimitsLabel);
						}
					}

					GUILayout.Space(2);
					if (DrawFoldoutHeader(m_us_meniscusThickness, Styles.headerMeniscus, true))
					{
						using (new EditorGUI.IndentLevelScope())
						{
							EditorGUILayout.PropertyField(m_us_meniscusThickness, Styles.meniscusThicknessLabel);
							EditorGUILayout.PropertyField(m_us_meniscusBlur, Styles.meniscusBlurLabel);
							EditorGUILayout.PropertyField(m_us_meniscusDarkness, Styles.meniscusDarknessLabel);
						}
					}

					GUILayout.Space(2);
					if (DrawFoldoutHeader(m_us_scatterColor, Styles.headerScatter, true))
					{
						using (new EditorGUI.IndentLevelScope())
						{
							EditorGUILayout.PropertyField(m_us_scatterColor, Styles.scatterColorLabel);
							EditorGUILayout.PropertyField(m_us_scatterAmbientIntensity, Styles.scatterAmbientIntensityLabel);
							EditorGUILayout.PropertyField(m_us_scatterLightIntensity, Styles.scatterLightIntensityLabel);
							EditorGUILayout.PropertyField(m_us_scatterIntensity, Styles.scatterIntensityLabel);
						}
					}
				}
			}
		}
		protected virtual void DrawCausticsSettings()
		{
			bool toggleChanged;
			bool isExpanded = DrawFoldoutHeaderToggle(m_causticsEnabledProperty, Styles.causticsLabel, out toggleChanged);

			if (isExpanded)
			{
				using (new EditorGUI.IndentLevelScope())
				{
					if (!Application.isPlaying)
						EditorGUILayout.HelpBox("Caustics load from Resources and are only active during Play Mode.", MessageType.Info);

					// Texture Projection
					if (DrawFoldoutHeader(m_cs_textureIntensity, Styles.Caustics.projectionHeader, true))
					{
						using (new EditorGUI.IndentLevelScope())
						{
							EditorGUILayout.PropertyField(m_cs_textureIntensity, Styles.Caustics.textureIntensityLabel);
							EditorGUILayout.PropertyField(m_cs_fps, Styles.Caustics.fpsLabel);
							EditorGUILayout.PropertyField(m_cs_tiling, Styles.Caustics.tilingLabel);
							EditorGUILayout.PropertyField(m_cs_triplanar, Styles.Caustics.triplanarLabel);
							EditorGUILayout.PropertyField(m_cs_chromaticAberration, Styles.Caustics.chromaticAberrationLabel);
							EditorGUILayout.PropertyField(m_cs_waveUVOffset, Styles.Caustics.waveUVOffsetLabel);
							EditorGUILayout.PropertyField(m_cs_channelMask, Styles.Caustics.channelMaskLabel);
						}
					}

					GUILayout.Space(2);

					// Wave Highlights
					if (DrawFoldoutHeader(m_cs_waveIntensity, Styles.Caustics.wavesHeader, true))
					{
						using (new EditorGUI.IndentLevelScope())
						{
							EditorGUILayout.PropertyField(m_cs_waveIntensity, Styles.Caustics.waveIntensityLabel);
							EditorGUILayout.PropertyField(m_cs_waveSharpness, Styles.Caustics.waveSharpnessLabel);
						}
					}

					GUILayout.Space(2);

					// Global Settings
					if (DrawFoldoutHeader(m_cs_globalIntensity, Styles.Caustics.globalHeader, true))
					{
						using (new EditorGUI.IndentLevelScope())
						{
							EditorGUILayout.PropertyField(m_cs_globalIntensity, Styles.Caustics.globalIntensityLabel);
							EditorGUILayout.PropertyField(m_cs_darkness, Styles.Caustics.darknessLabel);
							EditorGUILayout.PropertyField(m_cs_shadowIntensity, Styles.Caustics.shadowIntensityLabel);
							EditorGUILayout.PropertyField(m_cs_foamMasking, Styles.Caustics.foamMaskingLabel);

							// MinMax Sliders for Depth Ranges
							MinMaxSlider(m_cs_fadeInRange, 0.0f, 10.0f, Styles.Caustics.fadeInRangeLabel);
							MinMaxSlider(m_cs_fadeOutRange, 0.0f, 100.0f, Styles.Caustics.fadeOutRangeLabel);
						}
					}
				}
			}
		}

		protected virtual void DrawReflectionSettings()
		{
			WaterSurface surface = target as WaterSurface;

			bool toggleChanged;
			bool isExpanded = DrawFoldoutHeaderToggle(m_reflectionsEnabledProperty, Styles.reflectionsLabel, out toggleChanged);

			if (toggleChanged)
			{
				serializedObject.ApplyModifiedProperties();
				//surface.OnReflectionsChanged();
			}

			if (isExpanded)
			{
				// Use ChangeCheck to update settings live during PlayMode
				using (var check = new EditorGUI.ChangeCheckScope())
				{
					using (new EditorGUI.IndentLevelScope())
					{
						if (!Application.isPlaying)
							EditorGUILayout.HelpBox("Reflections are generated only during Play Mode.", MessageType.Info);

						if (DrawFoldoutHeader(m_rs_resolution, Styles.headerPlanar, true))
						{
							using (new EditorGUI.IndentLevelScope())
							{
								EditorGUILayout.PropertyField(m_rs_resolution, Styles.refResolutionLabel);
								EditorGUILayout.PropertyField(m_rs_cullingMask, Styles.refCullingMaskLabel);
								EditorGUILayout.PropertyField(m_rs_clearFlags, Styles.refClearFlagsLabel);
								EditorGUILayout.PropertyField(m_rs_clipPlane, Styles.refClipPlaneLabel);
								EditorGUILayout.PropertyField(m_rs_smoothPosition, Styles.refSmoothPosLabel);

#if FLUIDFRENZY_EDITOR_URP_SUPPORT
								EditorGUILayout.PropertyField(m_rs_rendererID, Styles.refRendererIDLabel);
#else
								EditorGUILayout.PropertyField(m_rs_shadowQuality, Styles.refShadowQualityLabel);
#endif
							}
						}
					}

					if (check.changed)
					{
						serializedObject.ApplyModifiedProperties();
						//surface.OnReflectionsChanged();
					}
				}
			}
		}
	}
}
#endif