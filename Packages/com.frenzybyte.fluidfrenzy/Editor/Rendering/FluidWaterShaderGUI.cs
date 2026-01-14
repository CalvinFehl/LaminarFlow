using UnityEditor;
using UnityEngine;

namespace FluidFrenzy.Editor
{
#if FLUIDFRENZY_EDITOR_HDRP_SUPPORT
	using PlatformSpecificShaderGUI = UnityEditor.Rendering.HighDefinition.LightingShaderGraphGUI;
#else
	using PlatformSpecificShaderGUI = UnityEditor.ShaderGUI;
#endif

	[HelpURL("https://frenzybyte.github.io/fluidfrenzy/docs/fluid_rendering_components/#water-shader")]
	public class FluidWaterShaderGUI : PlatformSpecificShaderGUI
	{
		private static class Styles
		{
			public static readonly GUIContent shaderDescriptionLabel = EditorGUIUtility.TrTextContent(
				"Water Shader",
				@"The FluidFrenzy/Water shader is applied to the material used by the Water Surface component. It provides a comprehensive set of material properties for creating visually appealing water.

Compatibility:
This shader is compatible with both the Universal Render Pipeline (URP) and the Built-in Render Pipeline (BiRP).

Note: The High Definition Render Pipeline (HDRP) requires a separate, dedicated shader: FluidFrenzy/HDRP/Water."
			);

			// Lighting
			public static GUIContent lightingHeader = EditorGUIUtility.TrTextContent(
				"Lighting",
				"Properties controlling the illumination and shading effects."
			);
			public static GUIContent specularIntensityText = EditorGUIUtility.TrTextContent(
				"Specular Intensity",
				"Scales the brightness of specular highlights from the main directional light."
			);
			public static GUIContent shadowMapText = EditorGUIUtility.TrTextContent(
				"Shadows",
				"Enables or disables whether the water surface receives shadows."
			);

			// Reflection
			public static GUIContent reflectionHeader = EditorGUIUtility.TrTextContent(
				"Reflection",
				"Properties controlling the water surface's reflection of the environment."
			);
			public static GUIContent planarReflectionText = EditorGUIUtility.TrTextContent(
				"Planar Reflection",
				"Enables or disables the use of planar reflections instead of only reflection probes."
			);
			public static GUIContent reflectivityMinText = EditorGUIUtility.TrTextContent(
				"Reflectivity Offset",
				@"Offsets the base reflectiveness of the water surface.
Use this to ensure the water is reflective even at sharp viewing angles."
			);

			public static GUIContent reflectionDistortionText = EditorGUIUtility.TrTextContent(
				"Distortion",
				"Scales the distortion applied to planar reflections."
			);

			// Absorption
			public static GUIContent absorptionHeader = EditorGUIUtility.TrTextContent(
				"Absorption",
				"Properties controlling depth-based color, transparency, and refraction effects."
			);
			public static GUIContent absorptionColorText = EditorGUIUtility.TrTextContent(
				"Color",
				@"RGB sets the color of the water at maximum depth. Alpha (A) is the base transparency of the water.
If 'Refraction Mode' is 'Screenspace Absorb', RGB is a color multiplier where White (1.0) is fully transparent.
For 'Alpha' or 'Screenspace Tint', RGB is the final color tint the water reaches at maximum depth/opacity."
			);
			public static GUIContent absorptionGradientScaleText = EditorGUIUtility.TrTextContent(
				"Depth Transparency",
				"Scales the rate at which the water's color changes and transparency fades based on depth. Lower values make the water more transparent at a faster rate."
			);
			public static GUIContent screenspaceRefractionText = EditorGUIUtility.TrTextContent(
				"Refraction Mode",
				@"Selects the method for rendering water transparency and refraction:

•  Alpha: Simple alpha blending transparency.
•  Opaque: Water is rendered as a solid, non-transparent surface.
•  Screenspace Tint: Uses screen-space refraction (GrabPass). Color interpolates from clear to the set color based on depth. Use for a single water color tint.
•  Screenspace Absorb: Uses screen-space refraction (GrabPass). Scene color is multiplied by water color, allowing for a color gradient (e.g., clear to turquoise to blue)."
			);
			public static GUIContent refractionDistortionText = EditorGUIUtility.TrTextContent(
				"Distortion",
				"Scales the amount of distortion applied to the screenspace refraction effect ('Screenspace Tint' or 'Screenspace Absorb' modes)."
			);

			// Subsurface Scattering
			public static GUIContent scatteringHeader = EditorGUIUtility.TrTextContent(
				"Subsurface Scattering",
				"Properties controlling the diffusion of light and subsurface scattering effect beneath the water surface."
			);
			public static GUIContent scatterColorText = EditorGUIUtility.TrTextContent(
				"Color",
				"The color the water will transition to when subsurface scattering occurs."
			);
			public static GUIContent scatterIntensityText = EditorGUIUtility.TrTextContent(
				"Intensity",
				"Scales the base intensity of the subsurface scattering effect."
			);
			public static GUIContent scatterAmbient = EditorGUIUtility.TrTextContent(
				"Ambient",
				"Scales the base contribution, ensuring some subsurface scattering is visible regardless of other parameters."
			);
			public static GUIContent scatterLightAngleIntensityText = EditorGUIUtility.TrTextContent(
				"Light Contribution",
				"Scales the contribution of subsurface scattering when the water surface faces away from the main light."
			);
			public static GUIContent scatterViewAngleIntensityText = EditorGUIUtility.TrTextContent(
				"View Contribution",
				"Scales the contribution of subsurface scattering when the water surface faces toward the observer/camera."
			);
			public static GUIContent scatterFoamContribution = EditorGUIUtility.TrTextContent(
				"Foam Contribution",
				"Scales the subsurface scattering contribution in areas covered by foam."
			);

			// Waves
			public static GUIContent wavesHeader = EditorGUIUtility.TrTextContent(
				"Waves",
				"Properties for adding detail to the water surface using normal mapping and procedural vertex displacement."
			);
			public static GUIContent normalMapText = EditorGUIUtility.TrTextContent(
				"Normal Map",
				"Texture used to add fine detail to the water's normals for lighting and PBR shading."
			);
			public static GUIContent displacementWavesText = EditorGUIUtility.TrTextContent(
				"Vertex Displacement",
				"Enables small-scale, procedural vertex displacement for finer wave details, which is based on the fluid simulation's velocity field."
			);
			public static GUIContent displacementWavesScaleText = EditorGUIUtility.TrTextContent(
				"Tiling",
				"Scales the overall density/tiling of the procedural displacement waves."
			);
			public static GUIContent displacementWaveAmplitudeText = EditorGUIUtility.TrTextContent(
				"Wave Amplitude",
				"Scales the maximum height (amplitude) of the displacement waves."
			);
			public static GUIContent displacementPhaseText = EditorGUIUtility.TrTextContent(
				"Phase Speed",
				"Scales the phase speed, which controls how fast the waves move up and down."
			);
			public static GUIContent displacementWaveSpeedText = EditorGUIUtility.TrTextContent(
				"Wave Speed",
				"Scales the horizontal movement speed of the displacement waves."
			);
			public static GUIContent displacementWaveLengthText = EditorGUIUtility.TrTextContent(
				"Wave Length",
				"Scales the distance between the crests (wavelength) of the displacement waves."
			);
			public static GUIContent displacementSteepnessText = EditorGUIUtility.TrTextContent(
				"Wave Steepness",
				"Scales the sharpness or smoothness (steepness) of the displacement waves."
			);

			// Foam
			public static GUIContent foamHeader = EditorGUIUtility.TrTextContent(
					"Foam",
					"Properties controlling the appearance and masking of the foam effect."
				);
			public static GUIContent foamColorText = EditorGUIUtility.TrTextContent(
				"Foam Color",
				"Sets the Foam Color (RGB) and acts as a multiplier/mask (A) for the Foam Map's transparency."
			);
			public static GUIContent foamMapText = EditorGUIUtility.TrTextContent(
				"Foam Map",
				@"Texture used for the foam's diffuse color (RGB) and its base mask/transparency (A)."
			);
			public static GUIContent foamNormalMapText = EditorGUIUtility.TrTextContent(
				"Foam Normal Map",
				"Normal map texture used to add PBR lighting detail to the foam."
			);
			public static GUIContent foamVisibilityText = EditorGUIUtility.TrTextContent(
				"Foam Visibility Range",
				@"Sets the minimum and maximum threshold values for when the foam becomes visible and reaches its maximum strength. Foam visibility is interpolated between these values using a smoothstep function."
			);
			public static GUIContent foamScreenSpaceParticlesText = EditorGUIUtility.TrTextContent(
				"Screenspace Particles",
				"Enables the use of the screenspace particles (from the FluidParticles component) as an additional mask to generate foam."
			);
			public static GUIContent foamModeText = EditorGUIUtility.TrTextContent(
				"Foam Mode",
				@"Selects the blending method for the foam:

•  Albedo: Soft foam using the Foam Map for color and mask.
•  Clip: Hard-edged foam using the Foam Map's red channel as a clip value for sharp borders.
•  Mask: Uses the Foam Layer Mask's value to select one of the Foam Map's RGB channels as an extra mask for blending the foam color, allowing for varied intensity: 0-0.334 uses Blue, 0.334-0.667 uses Green, and 0.667-1 uses Red."
			);


			// Rendering / General
			public static GUIContent renderingHeader = EditorGUIUtility.TrTextContent(
				"Rendering",
				"General rendering, depth-handling, and simulation sampling properties."
			);
			public static GUIContent layerText = EditorGUIUtility.TrTextContent(
				"Layer",
				"Selects which layer (e.g., Water or Lava, etc.) from the Fluid Simulation field to sample for effects."
			);
			public static GUIContent fadeHeightText = EditorGUIUtility.TrTextContent(
				"Fade Height",
				@"The world height at which the water will be fully faded out.
Used to soften edges or blend with geometry above a certain height."
			);
			public static GUIContent linearClipOffsetText = EditorGUIUtility.TrTextContent(
				"Linear Clip Offset",
				@"A linear offset applied to the clip-space Z depth
to help prevent visual clipping (Z-fighting) with close terrain or surfaces."
			);
			public static GUIContent exponentialClipOffsetText = EditorGUIUtility.TrTextContent(
				"Exponential Clip Offset",
				@"An exponential/depth-based offset applied to the clip-space Z depth
to help prevent visual clipping (Z-fighting) with distant terrain or surfaces."
			);
		}

		enum RefractionMode
		{
			Alpha,
			ScreenspaceTint,
			ScreenspaceAbsorb,
			Opaque
		}

		MaterialProperty specularIntensity = null;
		MaterialProperty shadowMap = null;
		MaterialProperty planarReflection = null;
		MaterialProperty reflectionDistortion = null;
		MaterialProperty reflectivityMin = null;
		MaterialProperty screenspaceRefraction = null;
		MaterialProperty absorptionScale = null;
		MaterialProperty refractionDistortion = null;
		MaterialProperty waterColor = null;

		MaterialProperty scatterColor = null;
		MaterialProperty scatterIntensity = null;
		MaterialProperty scatterLightIntensity = null;
		MaterialProperty scatterViewIntensity = null;
		MaterialProperty scatterFoamIntensity = null;
		MaterialProperty scatterAmbient = null;

		MaterialProperty displacementWaves = null;
		MaterialProperty displacementWaveAmplitude = null;
		MaterialProperty displacementPhase = null;
		MaterialProperty displacementWaveSpeed = null;
		MaterialProperty displacementWaveLength = null;
		MaterialProperty displacementSteepness = null;
		MaterialProperty displacementScale = null;

		MaterialProperty waveNormals = null;
		MaterialProperty waveNormalStrength = null;

		MaterialProperty foamColor = null;
		MaterialProperty foamVisibility = null;
		MaterialProperty foamTexture = null;
		MaterialProperty foamNormalMap = null;
		MaterialProperty foamNormalStrength = null;
		MaterialProperty foamScreenSpaceParticles = null;
		MaterialProperty foamMode = null;

		MaterialProperty fadeHeight = null;
		MaterialProperty linearClipOffset = null;
		MaterialProperty exponentialClipOffset = null;
		MaterialProperty layer = null;


#if FLUIDFRENZY_EXTERNALCOMPATIBILITY_CURVEDWORLD
		// Curved World
		MaterialProperty curvedWorldBendSettingsProperty;
#endif

		MaterialEditor m_MaterialEditor;

		private static bool foldoutLighting = true;
		private static bool foldoutReflection = true;
		private static bool foldoutAbsorption = true;
		private static bool foldoutSSS = true;
		private static bool foldoutWaves = true;
		private static bool foldoutFoam = true;
		private static bool foldoutRendering = true;

		public virtual void FindProperties(MaterialProperty[] props)
		{
			specularIntensity = FindProperty("_SpecularIntensity", props);
			shadowMap = FindProperty("_ShadowMap", props);
			planarReflection = FindProperty("_PlanarReflections", props);
			screenspaceRefraction = FindProperty("_ScreenSpaceRefraction", props);
			absorptionScale = FindProperty("_AbsorptionDepthScale", props);
			reflectivityMin = FindProperty("_ReflectivityMin", props);
			reflectionDistortion = FindProperty("_ReflectionDistortion", props);
			refractionDistortion = FindProperty("_RefractionDistortion", props);
			waterColor = FindProperty("_WaterColor", props);

			scatterColor = FindProperty("_ScatterColor", props);
			scatterIntensity = FindProperty("_ScatterIntensity", props);
			scatterLightIntensity = FindProperty("_ScatterLightIntensity", props);
			scatterViewIntensity = FindProperty("_ScatterViewIntensity", props);
			scatterFoamIntensity = FindProperty("_ScatterFoamIntensity", props);
			scatterAmbient = FindProperty("_ScatterAmbient", props);

			displacementWaves = FindProperty("_DisplacementWaves", props);
			displacementWaveAmplitude = FindProperty("_DisplacementWaveAmplitude", props);
			displacementPhase = FindProperty("_DisplacementPhase", props);
			displacementWaveSpeed = FindProperty("_DisplacementWaveSpeed", props);
			displacementWaveLength = FindProperty("_DisplacementWaveLength", props);
			displacementSteepness = FindProperty("_DisplacementSteepness", props);
			displacementScale = FindProperty("_DisplacementScale", props);

			waveNormals = FindProperty("_WaveNormals", props);
			waveNormalStrength = FindProperty("_WaveNormalStrength", props);

			foamColor = FindProperty("_FoamColor", props);
			foamVisibility = FindProperty("_FoamVisibility", props);
			foamTexture = FindProperty("_FoamTexture", props);
			foamNormalMap = FindProperty("_FoamNormalMap", props);
			foamNormalStrength = FindProperty("_FoamNormalStrength", props);
			foamScreenSpaceParticles = FindProperty("_FoamScreenSpace", props);
			foamMode = FindProperty("_FoamMode", props);

			fadeHeight = FindProperty("_FadeHeight", props);
			linearClipOffset = FindProperty("_LinearClipOffset", props);
			exponentialClipOffset = FindProperty("_ExponentialClipOffset", props);
			layer = FindProperty("_Layer", props);

#if FLUIDFRENZY_EXTERNALCOMPATIBILITY_CURVEDWORLD
			curvedWorldBendSettingsProperty = FindProperty("_CurvedWorldBendSettings", props, false);
#endif
		}

#if FLUIDFRENZY_EDITOR_HDRP_SUPPORT
		protected override void OnMaterialGUI(MaterialEditor materialEditor, MaterialProperty[] props)
#else
		public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] props)
#endif
		{
			//base.OnGUI(materialEditor, props);
			FindProperties(props); // MaterialProperties can be animated so we do not cache them but fetch them every event to ensure animated values are updated correctly
			m_MaterialEditor = materialEditor;
			Material material = materialEditor.target as Material;

			ShaderPropertiesGUI(material);
#if FLUIDFRENZY_EDITOR_HDRP_SUPPORT
			base.OnMaterialGUI(materialEditor, props);
#endif

			SetMaterialKeywords(material);
			SetMaterialRenderMode(material);
		}

		public virtual void ShaderPropertiesGUI(Material material)
		{
			// Use default labelWidth

			DrawCurvedWorldProperties();

			EditorGUIUtility.labelWidth = 0f;
			DrawLightingProperties();
			DrawReflectionProperties();
			DrawWaterColorProperties();
			DrawScatterProperties();
			DrawWaveProperties();
			DrawFoamProperties();
			DrawRenderingProperties();


		}

		public void DrawCurvedWorldProperties()
		{
#if FLUIDFRENZY_EXTERNALCOMPATIBILITY_CURVEDWORLD
			if (curvedWorldBendSettingsProperty != null)
			{
				EditorExtensions.DrawHeader(new GUIContent("Curved World"));
				m_MaterialEditor.ShaderProperty(curvedWorldBendSettingsProperty, "Curved World");
			}
#endif
		}

		public void DrawLightingProperties()
		{
			//GUILayout.Label(Styles.lightingText, EditorStyles.boldLabel);
			if (EditorExtensions.DrawFoldoutHeader(ref foldoutLighting, Styles.lightingHeader))
			{
				m_MaterialEditor.ShaderProperty(specularIntensity, Styles.specularIntensityText);
				m_MaterialEditor.ShaderProperty(shadowMap, Styles.shadowMapText);
				EditorGUILayout.Space();
			}
		}

		private void DrawWaterColorProperties()
		{
			//GUILayout.Label(Styles.absorptionText, EditorStyles.boldLabel);
			if (EditorExtensions.DrawFoldoutHeader(ref foldoutAbsorption, Styles.absorptionHeader))
			{
				m_MaterialEditor.ShaderProperty(waterColor, Styles.absorptionColorText);
				m_MaterialEditor.ShaderProperty(absorptionScale, Styles.absorptionGradientScaleText);
				//m_MaterialEditor.ShaderProperty(screenspaceRefraction, Styles.screenspaceRefractionText);
#if UNITY_2021_1_OR_NEWER
				RefractionMode refractionMode = (RefractionMode)screenspaceRefraction.intValue;
#else
				RefractionMode refractionMode = (RefractionMode)screenspaceRefraction.floatValue;
#endif
				RefractionMode newRefractionMode = (RefractionMode)EditorGUILayout.EnumPopup(Styles.screenspaceRefractionText, refractionMode);
				if(newRefractionMode != refractionMode)
				{
#if UNITY_2021_1_OR_NEWER
					screenspaceRefraction.intValue = (int)newRefractionMode;
#else
					screenspaceRefraction.floatValue = (float)newRefractionMode;
#endif
					refractionMode = newRefractionMode;
					m_MaterialEditor.PropertiesChanged();
				}

				if (refractionMode == RefractionMode.ScreenspaceTint || refractionMode == RefractionMode.ScreenspaceAbsorb)
				{
					EditorGUI.indentLevel = 1;
					m_MaterialEditor.ShaderProperty(refractionDistortion, Styles.refractionDistortionText);
					EditorGUI.indentLevel = 0;
				}
				EditorGUILayout.Space();
			}
		}

		private void DrawReflectionProperties()
		{
			//GUILayout.Label(Styles.reflectionText, EditorStyles.boldLabel);
			if (EditorExtensions.DrawFoldoutHeader(ref foldoutReflection, Styles.renderingHeader))
			{
				m_MaterialEditor.ShaderProperty(reflectivityMin, Styles.reflectivityMinText);
				m_MaterialEditor.ShaderProperty(planarReflection, Styles.planarReflectionText);
#if UNITY_2021_1_OR_NEWER
				if (planarReflection.intValue == 1)
#else
				if (planarReflection.floatValue == 1)
#endif
					m_MaterialEditor.ShaderProperty(reflectionDistortion, Styles.reflectionDistortionText, 1);
				EditorGUILayout.Space();
			}
		}

		public void DrawScatterProperties()
		{
			//GUILayout.Label(Styles.scatteringText, EditorStyles.boldLabel);
			if (EditorExtensions.DrawFoldoutHeader(ref foldoutSSS, Styles.scatteringHeader))
			{
				m_MaterialEditor.ColorProperty(scatterColor, Styles.scatterColorText.text);
				m_MaterialEditor.ShaderProperty(scatterIntensity, Styles.scatterIntensityText);
				m_MaterialEditor.ShaderProperty(scatterLightIntensity, Styles.scatterLightAngleIntensityText);
				m_MaterialEditor.ShaderProperty(scatterViewIntensity, Styles.scatterViewAngleIntensityText);
				m_MaterialEditor.ShaderProperty(scatterFoamIntensity, Styles.scatterFoamContribution);
				m_MaterialEditor.ShaderProperty(scatterAmbient, Styles.scatterAmbient);
				EditorGUILayout.Space();
			}
		}

		public void DrawWaveProperties()
		{
			//GUILayout.Label(Styles.wavesText, EditorStyles.boldLabel);
			if (EditorExtensions.DrawFoldoutHeader(ref foldoutWaves, Styles.wavesHeader))
			{
				m_MaterialEditor.TexturePropertySingleLine(Styles.normalMapText, waveNormals, waveNormals.textureValue != null ? waveNormalStrength : null);
				if (waveNormals.textureValue != null)
					m_MaterialEditor.TextureScaleOffsetProperty(waveNormals);

				m_MaterialEditor.ShaderProperty(displacementWaves, Styles.displacementWavesText);
#if UNITY_2021_1_OR_NEWER
				if (displacementWaves.intValue == 1)
#else
				if (displacementWaves.floatValue == 1)
#endif
				{
					EditorGUI.indentLevel = 1;
					m_MaterialEditor.ShaderProperty(displacementScale, Styles.displacementWavesScaleText);
					m_MaterialEditor.ShaderProperty(displacementWaveAmplitude, Styles.displacementWaveAmplitudeText);
					m_MaterialEditor.ShaderProperty(displacementWaveSpeed, Styles.displacementWaveSpeedText);
					m_MaterialEditor.ShaderProperty(displacementWaveLength, Styles.displacementWaveLengthText);
					m_MaterialEditor.ShaderProperty(displacementSteepness, Styles.displacementSteepnessText);
					m_MaterialEditor.ShaderProperty(displacementPhase, Styles.displacementPhaseText);
					EditorGUI.indentLevel = 0;
				}
				EditorGUILayout.Space();
			}
		}

		public void DrawFoamProperties()
		{
			//GUILayout.Label(Styles.foamText, EditorStyles.boldLabel); 
			if (EditorExtensions.DrawFoldoutHeader(ref foldoutFoam, Styles.foamHeader))
			{
				m_MaterialEditor.ShaderProperty(foamScreenSpaceParticles, Styles.foamScreenSpaceParticlesText);
				m_MaterialEditor.ShaderProperty(foamMode, Styles.foamModeText);
				m_MaterialEditor.TexturePropertyWithHDRColor(Styles.foamMapText, foamTexture, foamColor, true);
				if (foamTexture.textureValue != null)
				{
					m_MaterialEditor.TexturePropertySingleLine(Styles.foamNormalMapText, foamNormalMap, foamNormalMap.textureValue != null ? foamNormalStrength : null);
					m_MaterialEditor.TextureScaleOffsetProperty(foamTexture);
					EditorExtensions.MinMaxShaderProperty(m_MaterialEditor, foamVisibility, 0, 1, Styles.foamVisibilityText);
				}
				EditorGUILayout.Space();
			}
		}

		public void DrawRenderingProperties()
		{
			//GUILayout.Label(Styles.renderingText, EditorStyles.boldLabel);
			if (EditorExtensions.DrawFoldoutHeader(ref foldoutRendering, Styles.renderingHeader))
			{
				m_MaterialEditor.ShaderProperty(fadeHeight, Styles.fadeHeightText);
				m_MaterialEditor.ShaderProperty(linearClipOffset, Styles.linearClipOffsetText);
				m_MaterialEditor.ShaderProperty(exponentialClipOffset, Styles.exponentialClipOffsetText);
				m_MaterialEditor.ShaderProperty(layer, Styles.layerText);
				m_MaterialEditor.RenderQueueField();
				m_MaterialEditor.EnableInstancingField();
				//m_MaterialEditor.DoubleSidedGIField();
				EditorGUILayout.Space();
			}
		}

		public void SetMaterialKeywords(Material material)
		{
			SetKeyword(material, "_NORMALMAP", waveNormals.textureValue);
			SetKeyword(material, "_FOAMMAP", foamTexture.textureValue != null && foamNormalMap.textureValue == null);
			SetKeyword(material, "_FOAM_NORMALMAP", foamTexture.textureValue != null && foamNormalMap.textureValue != null);

#if UNITY_2021_1_OR_NEWER
			SetKeyword(material, "_DISPLACEMENTMAP", displacementWaves.intValue == 1);

			SetKeyword(material, "_SCREENSPACE_REFRACTION_OPAQUE", screenspaceRefraction.intValue == (int)RefractionMode.Opaque);
			SetKeyword(material, "_SCREENSPACE_REFRACTION_ON", screenspaceRefraction.intValue == (int)RefractionMode.ScreenspaceTint);
			SetKeyword(material, "_SCREENSPACE_REFRACTION_ABSORB", screenspaceRefraction.intValue == (int)RefractionMode.ScreenspaceAbsorb);
			SetKeyword(material, "_SCREENSPACE_REFRACTION_ALPHA", screenspaceRefraction.intValue == (int)RefractionMode.Alpha);
#else

			SetKeyword(material, "_SCREENSPACE_REFRACTION_OPAQUE", screenspaceRefraction.floatValue == (float)(RefractionMode.Opaque));
			SetKeyword(material, "_SCREENSPACE_REFRACTION_ON", screenspaceRefraction.floatValue == (float)RefractionMode.Screenspace);
			SetKeyword(material, "_SCREENSPACE_REFRACTION_ABSORB", screenspaceRefraction.floatValue == (float)RefractionMode.ScreenspaceAbsorb);
			SetKeyword(material, "_SCREENSPACE_REFRACTION_ALPHA", screenspaceRefraction.floatValue == (float)RefractionMode.Alpha);

			SetKeyword(material, "_DISPLACEMENTWAVES_ON", displacementWaves.floatValue == 1);
			SetKeyword(material, "_PLANAR_REFLECTION_ON", planarReflection.floatValue == 1);
			SetKeyword(material, "_SHADOWMAP_OFF", shadowMap.floatValue == 0);
			SetKeyword(material, "_SHADOWMAP_HARD", shadowMap.floatValue == 1);
			SetKeyword(material, "_SHADOWMAP_SOFT", shadowMap.floatValue == 2);
			SetKeyword(material, "_DISPLACEMENTMAP", displacementWaves.floatValue == 1);
#endif
		}

		public void SetMaterialRenderMode(Material material)
		{
#if UNITY_2021_1_OR_NEWER
			if (screenspaceRefraction.intValue == (int)RefractionMode.ScreenspaceTint || screenspaceRefraction.intValue == (int)RefractionMode.ScreenspaceAbsorb)
			{
				material.SetShaderPassEnabled("Always", true);
				material.SetInteger("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
				material.SetInteger("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
			}
			else if (screenspaceRefraction.intValue == (int)RefractionMode.Alpha)
			{
				material.SetShaderPassEnabled("Always", false);
				material.SetInteger("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
				material.SetInteger("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
			}
			else if(screenspaceRefraction.intValue == (int)RefractionMode.Opaque)
			{
				material.SetShaderPassEnabled("Always", false);
				material.SetInteger("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
				material.SetInteger("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
			}
#else
			if (screenspaceRefraction.floatValue == (float)RefractionMode.Screenspace || screenspaceRefraction.intValue == (int)RefractionMode.ScreenspaceAbsorb)
			{
				material.SetShaderPassEnabled("Always", true);
				material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
				material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
			}
			else if (screenspaceRefraction.floatValue == (float)RefractionMode.Alpha)
			{
				material.SetShaderPassEnabled("Always", false);
				material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
				material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
			}
			else if(screenspaceRefraction.floatValue == (float)RefractionMode.Opaque)
			{
				material.SetShaderPassEnabled("Always", false);
				material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
				material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
			}
#endif
		}

		static void SetKeyword(Material m, string keyword, bool state)
		{
			if (state)
				m.EnableKeyword(keyword);
			else
				m.DisableKeyword(keyword);
		}
	}
}