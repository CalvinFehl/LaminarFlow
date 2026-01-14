using UnityEditor;
using UnityEngine;

#if FLUIDFRENZY_EDITOR_HDRP_SUPPORT
using PlatformSpecificShaderGUI = UnityEditor.Rendering.HighDefinition.UnlitShaderGraphGUI;
#else
using PlatformSpecificShaderGUI = UnityEditor.ShaderGUI;
#endif

namespace FluidFrenzy.Editor
{
	[HelpURL("https://frenzybyte.github.io/fluidfrenzy/docs/fluid_rendering_components/#lava-shader")]
	public class FluidLavaShaderGUI : PlatformSpecificShaderGUI
	{
		private static class Styles
		{
			public static readonly GUIContent shaderDescriptionLabel = EditorGUIUtility.TrTextContent(
				"Lava Shader", // Short Label
				@"The FluidFrenzy/Lava shader is applied to the material used by the Lava Surface component. It creates realistic, flowing lava visuals where the 'heat' and resulting glow are dynamically driven by the **length of the fluid's velocity vector** in the simulation.

The shader uses textures for the base 'cold' lava surface (Albedo, Smoothness, Normal Map) and employs a specialized **Heat Look-Up Table (LUT)** alongside an **Emission Map** to control the vibrant colors and intensity of the glowing, 'hot' lava. A separate **Noise** texture is used to break up tiling patterns.

Compatibility:
The FluidFrenzy/Lava shader is for URP and BiRP. The High Definition Render Pipeline (HDRP) requires a separate, dedicated shader: FluidFrenzy/HDRP/Lava."
			);

			// Lighting
			public static GUIContent lightingHeader = EditorGUIUtility.TrTextContent(
				"Lighting",
				"Properties controlling the illumination and shading effects."
			);
			public static GUIContent lightIntensityText = EditorGUIUtility.TrTextContent(
				"Light Intensity",
				"Scales the influence of the main directional light on the lava surface (e.g., specular highlights)."
			);
			public static GUIContent shadowMapText = EditorGUIUtility.TrTextContent(
				"Shadows",
				"Enables or disables if the lava surface receives shadows from other scene objects."
			);

			// Heat & Emission (Lava Color)
			public static GUIContent lavaColorHeader = EditorGUIUtility.TrTextContent(
				"Heat & Emission",
				"Properties controlling the lava's color and emission, driven by the fluid's 'heat' (usually fluid velocity/movement)."
			);
			public static GUIContent heatLutText = EditorGUIUtility.TrTextContent(
				"Heat LUT",
				"Gradient Lookup Texture (LUT) used to determine the lava's color and emission based on the fluid's 'heat'."
			);
			public static GUIContent heatLutScaleText = EditorGUIUtility.TrTextContent(
				"Heat Scale",
				"Scales the fluid 'heat' value when sampling the Heat LUT gradient. Lower values increase the effective range of the lookup."
			);
			public static GUIContent emissionMapText = EditorGUIUtility.TrTextContent(
				"Emission Map",
				"Texture used for the emission color of the lava. A sample of this texture is multiplied by the fluid's 'heat'."
			);
			public static GUIContent emissionText = EditorGUIUtility.TrTextContent(
				"Emission",
				"Scales the overall intensity of the emission determined by the Heat LUT and the Emission Map."
			);

			// Material Properties
			public static GUIContent materialPropHeader = EditorGUIUtility.TrTextContent(
				"Material Properties",
				"Properties controlling the cold lava surface's visual and PBR shading characteristics."
			);
			public static GUIContent albedoText = EditorGUIUtility.TrTextContent(
				"Albedo",
				"Sets the base Albedo color and texture of the lava. This represents the appearance of cold (non-emissive) lava."
			);
			public static GUIContent glossmapScaleText = EditorGUIUtility.TrTextContent(
				"Smoothness Scale",
				"Scales the PBR smoothness of the cold lava surface, affecting its specular reflections."
			);
			public static GUIContent normalMapText = EditorGUIUtility.TrTextContent(
				"Normal Map",
				"Normal map texture used to add detailed lighting to the cold lava surface."
			);
			public static GUIContent noiseText = EditorGUIUtility.TrTextContent(
				"Noise",
				"Noise texture used to eliminate noticeable tiling and repetition from the lava textures."
			);

			// Rendering
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
				@"The world height at which the lava will be fully faded out.
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

		MaterialProperty heatLut = null;
		MaterialProperty heatlutScale = null;
		MaterialProperty albedoMap = null;
		MaterialProperty diffuseColor = null;
		MaterialProperty glossmapScale = null;
		MaterialProperty normalScale = null;
		MaterialProperty normalMap = null;
		MaterialProperty emissionMap = null;
		MaterialProperty emission = null;
		MaterialProperty noise = null;

		MaterialProperty lightIntensity = null;
		MaterialProperty shadowMap = null;
		MaterialProperty fadeHeight = null;
		MaterialProperty linearClipOffset = null;
		MaterialProperty exponentialClipOffset = null;
		MaterialProperty layer = null;

#if FLUIDFRENZY_EXTERNALCOMPATIBILITY_CURVEDWORLD
		// Curved Worlds
		MaterialProperty curvedWorldBendSettingsProperty = null;
#endif

		MaterialEditor m_MaterialEditor;

		private static bool foldoutLighting = true;
		private static bool foldoutLavaColor = true;
		private static bool foldoutMaterialProps = true;
		private static bool foldoutRendering = true;

		public void FindProperties(MaterialProperty[] props)
		{
			heatLut = FindProperty("_HeatLUT", props);
			heatlutScale = FindProperty("_LUTScale", props);
			albedoMap = FindProperty("_MainTex", props);
			diffuseColor = FindProperty("_Color", props);
			glossmapScale = FindProperty("_GlossMapScale", props);
			normalScale = FindProperty("_BumpScale", props);
			normalMap = FindProperty("_BumpMap", props);
			emissionMap = FindProperty("_EmissionMap", props);
			emission = FindProperty("_Emission", props);
			noise = FindProperty("_Noise", props, false);

			lightIntensity = FindProperty("_LightIntensity", props, false);
			shadowMap = FindProperty("_ShadowMap", props, false);
			layer = FindProperty("_Layer", props);
			linearClipOffset = FindProperty("_LinearClipOffset", props, false);
			exponentialClipOffset = FindProperty("_ExponentialClipOffset", props, false);
			fadeHeight = FindProperty("_FadeHeight", props);
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
			FindProperties(props);
			m_MaterialEditor = materialEditor;
			Material material = materialEditor.target as Material;

			ShaderPropertiesGUI(material);
#if FLUIDFRENZY_EDITOR_HDRP_SUPPORT
			base.OnMaterialGUI(materialEditor, props);
#endif
		}

		public void ShaderPropertiesGUI(Material material)
		{
			// Use default labelWidth
			EditorGUIUtility.labelWidth = 0f;

			DrawCurvedWorldProperties();

			DrawLightingProperties();
			DrawLavaColorProperties();
			DrawMaterialProperties();
			DrawRenderingProperties();

			material.SetTextureScale(normalMap.name, material.GetTextureScale(albedoMap.name));
			material.SetTextureOffset(normalMap.name, material.GetTextureOffset(albedoMap.name));

			material.SetTextureScale(emissionMap.name, material.GetTextureScale(albedoMap.name));
			material.SetTextureOffset(emissionMap.name, material.GetTextureOffset(albedoMap.name));
			SetMaterialKeywords(material);
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
			if (lightIntensity != null)
			{
				if (EditorExtensions.DrawFoldoutHeader(ref foldoutLighting, Styles.lightingHeader))
				{

					m_MaterialEditor.ShaderProperty(lightIntensity, Styles.lightIntensityText);
					m_MaterialEditor.ShaderProperty(shadowMap, Styles.shadowMapText);
				}
				EditorGUILayout.Space();
				EditorGUILayout.EndFoldoutHeaderGroup();
			}
		}

		public void DrawLavaColorProperties()
		{
			if (EditorExtensions.DrawFoldoutHeader(ref foldoutLavaColor, Styles.lavaColorHeader))
			{
				m_MaterialEditor.TexturePropertySingleLine(Styles.heatLutText, heatLut);
				m_MaterialEditor.ShaderProperty(heatlutScale, Styles.heatLutScaleText);
				EditorGUILayout.Space();
			}
			EditorGUILayout.EndFoldoutHeaderGroup();
		}

		public void DrawMaterialProperties()
		{
			if (EditorExtensions.DrawFoldoutHeader(ref foldoutMaterialProps, Styles.materialPropHeader))
			{
				m_MaterialEditor.TexturePropertySingleLine(Styles.albedoText, albedoMap, diffuseColor);
				m_MaterialEditor.ShaderProperty(glossmapScale, Styles.glossmapScaleText);
				m_MaterialEditor.TexturePropertySingleLine(Styles.normalMapText, normalMap, normalMap.textureValue != null ? normalScale : null);
				m_MaterialEditor.TexturePropertySingleLine(Styles.emissionText, emissionMap, emissionMap.textureValue != null ? emission : null);
				m_MaterialEditor.TextureScaleOffsetProperty(albedoMap);
				if(noise != null)
					m_MaterialEditor.TexturePropertySingleLine(Styles.noiseText, noise);
				EditorGUILayout.Space();
			}
			EditorGUILayout.EndFoldoutHeaderGroup();
		}

		public void DrawRenderingProperties()
		{
			if (EditorExtensions.DrawFoldoutHeader(ref foldoutRendering, Styles.renderingHeader))
			{
				m_MaterialEditor.ShaderProperty(fadeHeight, Styles.fadeHeightText);
				if (linearClipOffset != null)
				{
					m_MaterialEditor.ShaderProperty(linearClipOffset, Styles.linearClipOffsetText);
				}
				if (exponentialClipOffset != null)
				{
					m_MaterialEditor.ShaderProperty(exponentialClipOffset, Styles.exponentialClipOffsetText);
				}
				m_MaterialEditor.ShaderProperty(layer, Styles.layerText);
				m_MaterialEditor.RenderQueueField();
				m_MaterialEditor.EnableInstancingField();
				//m_MaterialEditor.DoubleSidedGIField();
				EditorGUILayout.Space();
			}
			EditorGUILayout.EndFoldoutHeaderGroup();
		}

		public void SetMaterialKeywords(Material material)
		{
			SetKeyword(material, "_NORMALMAP", normalMap.textureValue);
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