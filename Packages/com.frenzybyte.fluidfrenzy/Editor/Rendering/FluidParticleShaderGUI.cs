using UnityEditor;
#if FLUIDFRENZY_EDITOR_HDRP_SUPPORT
using UnityEditor.Rendering.HighDefinition;
#endif
using UnityEngine;

namespace FluidFrenzy.Editor
{
#if FLUIDFRENZY_EDITOR_HDRP_SUPPORT
	using PlatformSpecificShaderGUI = UnityEditor.Rendering.HighDefinition.LightingShaderGraphGUI;
#else
	using PlatformSpecificShaderGUI = UnityEditor.ShaderGUI;
#endif

	[HelpURL("https://frenzybyte.github.io/fluidfrenzy/docs/fluid_rendering_components/#particles")]
	public class FluidParticleShaderGUI : PlatformSpecificShaderGUI
	{
#if UNITY_2021_1_OR_NEWER
		public enum BillboardMode
		{
			Camera,
			CameraNormalUp,
			Up,
			Normal
		}


#if FLUIDFRENZY_EDITOR_HDRP_SUPPORT
		// For surface option shader graph we only want all unlit features but alpha clip and back then front rendering
		const SurfaceOptionUIBlock.Features surfaceOptionFeatures = SurfaceOptionUIBlock.Features.Lit
			| SurfaceOptionUIBlock.Features.ShowDepthOffsetOnly;

		MaterialUIBlockList m_UIBlocks = new MaterialUIBlockList
		{
			new SurfaceOptionUIBlock(MaterialUIBlock.ExpandableBit.Base, features: surfaceOptionFeatures),
            // We don't want distortion in Lit
            new TransparencyUIBlock(MaterialUIBlock.ExpandableBit.Transparency, features: TransparencyUIBlock.Features.All & ~TransparencyUIBlock.Features.Distortion),
		};

		new MaterialUIBlockList uiBlocks => m_UIBlocks;
#endif

		/// <summary>
		/// The blend mode for your material.
		/// </summary>
		public enum BlendMode
		{
			/// <summary>
			/// Use this for alpha blend mode.
			/// </summary>
			Alpha,   // Old school alpha-blending mode, fresnel does not affect amount of transparency

			/// <summary>
			/// Use this for premultiply blend mode.
			/// </summary>
			Premultiply, // Physically plausible transparency mode, implemented as alpha pre-multiply

			/// <summary>
			/// Use this for additive blend mode.
			/// </summary>
			Additive,

			/// <summary>
			/// Use this for multiply blend mode.
			/// </summary>
			Multiply,

			/// <summary>
			/// Use this for alpha test mode.
			/// </summary>
			AlphaTest,

			/// <summary>
			/// Use this for hardware alpha test mode.
			/// </summary>
			AlphaToMask,

			/// <summary>
			/// Choose a custom blendmode.
			/// </summary>
			Custom
		}

		private static class Styles
		{
			public static readonly GUIContent shaderDescriptionLabel = EditorGUIUtility.TrTextContent(
				"Fluid Particle Shaders", 
				@"Fluid Frenzy uses custom shaders to render its completely GPU-accelerated particle system. Two shaders are available: *ProceduralParticle* (Lit) and *ProceduralParticleUnlit*. Both render particles as billboards.

• ProceduralParticle (Lit): Includes PBR lighting with support for Normal Maps, Metallic, and Smoothness.
• ProceduralParticleUnlit (Unlit): Does not perform lighting, offering a lower rendering cost.

Both shaders share settings for **Blend Mode** and **Billboard Mode**. **Billboard Mode** controls particle orientation, including options for camera-facing or world-up normals to manage lighting.

Compatibility:
For URP and BiRP, the shaders are FluidFrenzy/Particle and FluidFrenzy/ParticleUnlit.
The High Definition Render Pipeline (HDRP) requires its own dedicated shaders: FluidFrenzy/HDRP/Particle and FluidFrenzy/HDRP/ParticleUnlit."
			);

			public static GUIContent mainTextureText = EditorGUIUtility.TrTextContent("Albedo", "albedo color and transparency of the particle.");
			public static GUIContent mainColorText = EditorGUIUtility.TrTextContent("Color", "albedo color and transparency of the particle.");
			public static GUIContent normalMapText = EditorGUIUtility.TrTextContent("Normal Map", "can be used to add extra lighting details.");
			public static GUIContent alphaCutoffText = EditorGUIUtility.TrTextContent("Alpha Threshold", "Alpha below this value will be clipped.");
			public static GUIContent blendModeText = EditorGUIUtility.TrTextContent("Blend Mode", "select which to use for the particles.");
			public static GUIContent srcBlendText = EditorGUIUtility.TrTextContent("Source Blend", "Source Blend.");
			public static GUIContent dstBlendText = EditorGUIUtility.TrTextContent("Dest Blend", "Dest Blend.");
			public static GUIContent zWriteText = EditorGUIUtility.TrTextContent("ZWrite", "Write particle to the depth buffer.");
			public static GUIContent billboardModeText = EditorGUIUtility.TrTextContent("Billboard Mode", @"Select which method to use for rendering the particle billboard.

• Camera: the billboard and world normal will face in the direction of the camera.

• Camera Normal Up: the billboard will face the camera and the normal will face in in the world space up direction.This can be useful to have more uniform lighting from every direction.

• Up: the billboard and normal will both face in the world space up direction.

• Normal: not yet implemented.");

			public static GUIContent metallicText = EditorGUIUtility.TrTextContent("Metallic", "The metalness of this material.");
			public static GUIContent smoothnessText = EditorGUIUtility.TrTextContent("Smoothness", "The smoothness of this material.");


			public static GUIContent lightingText = new GUIContent("Lighting");
			public static GUIContent renderingText = new GUIContent("Rendering");

			public static GUIContent[] billboardFaceNames = { new GUIContent("Camera"), new GUIContent("Camera Normal Up"), new GUIContent("Up"), new GUIContent("Normal") };

		}

		MaterialProperty mainTextureProperty = null;
		MaterialProperty mainColorProperty = null;
		MaterialProperty metallicProperty = null;
		MaterialProperty smoothnessProperty = null;
		MaterialProperty normalMapProperty = null;

		MaterialProperty billboardModeProperty = null;


		MaterialProperty blendModeProperty = null;
		MaterialProperty alphaCuttoffProperty = null;
		MaterialProperty srcBlendProperty = null;
		MaterialProperty dstBlendProperty = null;
		MaterialProperty zwriteProperty = null;
		MaterialProperty alphaToMaskProperty = null;
		MaterialProperty alphaCuttoffEnableProperty = null;

#if FLUIDFRENZY_EXTERNALCOMPATIBILITY_CURVEDWORLD
		// Curved World 
		MaterialProperty curvedWorldBendSettingsProperty = null;
#endif

		MaterialEditor m_MaterialEditor;

		private static bool foldoutLighting = true;
		private static bool foldoutRendering = true;


		public void FindProperties(MaterialProperty[] props)
		{
			mainTextureProperty = FindProperty("_MainTex", props);
			mainColorProperty = FindProperty("_Color", props);
			metallicProperty = FindProperty("_Metallic", props, false);
			smoothnessProperty = FindProperty("_Smoothness", props, false);
			normalMapProperty = FindProperty("_NormalMap", props, false);
			billboardModeProperty = FindProperty("_BillboardMode", props);


			blendModeProperty = FindProperty("_Blend", props, false);
			alphaCuttoffProperty = FindProperty("_Cutoff", props, false);
			srcBlendProperty = FindProperty("_SrcBlend", props, false);
			dstBlendProperty = FindProperty("_DstBlend", props, false);
			zwriteProperty = FindProperty("_ZWrite", props, false);
			alphaToMaskProperty = FindProperty("_AlphaToMask", props, false);
			alphaCuttoffEnableProperty = FindProperty("_AlphaCutoffEnable", props, false);

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
			uiBlocks.OnGUI(materialEditor, props);
#endif
		}

		public void ShaderPropertiesGUI(Material material)
		{
			// Use default labelWidth
			EditorGUIUtility.labelWidth = 0f;

			DrawCurvedWorldProperties();

			DrawLightingProperties();
			DrawRenderingProperties();

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
			if (EditorExtensions.DrawFoldoutHeader(ref foldoutLighting, Styles.lightingText))
			{
				m_MaterialEditor.TexturePropertyWithHDRColor(Styles.mainColorText, mainTextureProperty, mainColorProperty, true);
				if(normalMapProperty != null)
					m_MaterialEditor.TexturePropertySingleLine(Styles.normalMapText, normalMapProperty);

				if(metallicProperty != null)
					m_MaterialEditor.ShaderProperty(metallicProperty, Styles.metallicText);

				if(smoothnessProperty != null)	
					m_MaterialEditor.ShaderProperty(smoothnessProperty, Styles.smoothnessText);

				EditorGUILayout.Space();
			}
		}

		private bool BlendModePopup()
		{
			if(blendModeProperty == null)
			{
				return false;
			}
			var mode = (BlendMode)blendModeProperty.intValue;

			EditorGUI.BeginChangeCheck();
			mode = (BlendMode)EditorGUILayout.EnumPopup(Styles.blendModeText, mode);
			bool result = EditorGUI.EndChangeCheck();
			if (result)
			{
				m_MaterialEditor.PropertiesChanged();
				m_MaterialEditor.RegisterPropertyChangeUndo("Blend Mode");
				blendModeProperty.intValue = (int)mode;
			}

			EditorGUI.showMixedValue = false;
			return result;
		}

		public void DrawRenderingProperties()
		{
			if (EditorExtensions.DrawFoldoutHeader(ref foldoutRendering, Styles.renderingText))
			{
				BlendModePopup();

				if (blendModeProperty != null)
				{
					BlendMode blendMode = (BlendMode)blendModeProperty.intValue;
					if (blendMode == BlendMode.Custom)
					{
						m_MaterialEditor.ShaderProperty(srcBlendProperty, Styles.srcBlendText);
						m_MaterialEditor.ShaderProperty(dstBlendProperty, Styles.dstBlendText);
						m_MaterialEditor.ShaderProperty(zwriteProperty, Styles.zWriteText);
					}
					else if (blendMode == BlendMode.AlphaTest)
					{
						m_MaterialEditor.ShaderProperty(alphaCuttoffProperty, Styles.alphaCutoffText);
					}
				}
				else if(alphaCuttoffEnableProperty != null && alphaCuttoffEnableProperty.floatValue == 1.0f)
				{
					m_MaterialEditor.ShaderProperty(alphaCuttoffProperty, Styles.alphaCutoffText);
				}

				BillboardMode billboardMode = (BillboardMode)billboardModeProperty.floatValue;
				billboardMode = (BillboardMode)EditorGUILayout.Popup(Styles.billboardModeText, (int)billboardMode, Styles.billboardFaceNames);

				if (billboardMode != (BillboardMode)billboardModeProperty.floatValue)
				{
					m_MaterialEditor.PropertiesChanged();
					m_MaterialEditor.RegisterPropertyChangeUndo("Rendering Mode");
					billboardModeProperty.floatValue = (float)billboardMode;
				}

				m_MaterialEditor.RenderQueueField();
				//m_MaterialEditor.EnableInstancingField();
				//m_MaterialEditor.DoubleSidedGIField();
				EditorGUILayout.Space();
			}
		}

		public void SetMaterialKeywords(Material material)
		{
			if(normalMapProperty != null)
				SetKeyword(material, "_NORMALMAP", normalMapProperty.textureValue);

			if (blendModeProperty != null)
			{
				BlendMode blendMode = (BlendMode)blendModeProperty.intValue;

				material.DisableKeyword("_ALPHABLEND_ON");
				material.DisableKeyword("_ALPHATEST_ON");
				material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
				material.DisableKeyword("_BLENDADDITIVE_ON");

				alphaToMaskProperty.intValue = 0;
				switch (blendMode)
				{
					case BlendMode.Alpha:
						srcBlendProperty.intValue = (int)UnityEngine.Rendering.BlendMode.SrcAlpha;
						dstBlendProperty.intValue = (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha;
						zwriteProperty.floatValue = 0;
						material.EnableKeyword("_ALPHABLEND_ON");
						break;
					case BlendMode.AlphaTest:
						srcBlendProperty.intValue = (int)UnityEngine.Rendering.BlendMode.One;
						dstBlendProperty.intValue = (int)UnityEngine.Rendering.BlendMode.Zero;
						zwriteProperty.floatValue = 1;
						material.EnableKeyword("_ALPHATEST_ON");
						break;
					case BlendMode.AlphaToMask:
						alphaToMaskProperty.intValue = 1;
						zwriteProperty.floatValue = 1;
						material.EnableKeyword("_ALPHABLEND_ON");
						break;
					case BlendMode.Premultiply:
						srcBlendProperty.intValue = (int)UnityEngine.Rendering.BlendMode.One;
						dstBlendProperty.intValue = (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha;
						zwriteProperty.floatValue = 0;
						material.EnableKeyword("_ALPHAPREMULTIPLY_ON");
						break;
					case BlendMode.Additive:
						srcBlendProperty.intValue = (int)UnityEngine.Rendering.BlendMode.One;
						dstBlendProperty.intValue = (int)UnityEngine.Rendering.BlendMode.One;
						zwriteProperty.floatValue = 0;
						material.EnableKeyword("_BLENDADDITIVE_ON");
						break;
					case BlendMode.Multiply:
						srcBlendProperty.intValue = (int)UnityEngine.Rendering.BlendMode.DstColor;
						dstBlendProperty.intValue = (int)UnityEngine.Rendering.BlendMode.Zero;
						zwriteProperty.floatValue = 0;
						break;
					case BlendMode.Custom:
						material.EnableKeyword("_ALPHABLEND_ON");
						break;
				}
			}

			if ((BillboardMode)billboardModeProperty.floatValue == BillboardMode.Camera)
			{
				material.EnableKeyword("_BILLBOARDMODE_CAMERA");
				material.DisableKeyword("_BILLBOARDMODE_CAMERA_NORMAL_UP");
				material.DisableKeyword("_BILLBOARDMODE_UP");
				material.DisableKeyword("_BILLBOARDMODE_NORMAL");
			}
			else if((BillboardMode)billboardModeProperty.floatValue == BillboardMode.CameraNormalUp)
			{
				material.DisableKeyword("_BILLBOARDMODE_CAMERA");
				material.EnableKeyword("_BILLBOARDMODE_CAMERA_NORMAL_UP");
				material.DisableKeyword("_BILLBOARDMODE_UP");
				material.DisableKeyword("_BILLBOARDMODE_NORMAL");
			}
			else if((BillboardMode)billboardModeProperty.floatValue == BillboardMode.Up)
			{
				material.DisableKeyword("_BILLBOARDMODE_CAMERA");
				material.DisableKeyword("_BILLBOARDMODE_CAMERA_NORMAL_UP");
				material.EnableKeyword("_BILLBOARDMODE_UP");
				material.DisableKeyword("_BILLBOARDMODE_NORMAL");
			}
			else if((BillboardMode)billboardModeProperty.floatValue == BillboardMode.Normal)
			{
				material.DisableKeyword("_BILLBOARDMODE_CAMERA");
				material.DisableKeyword("_BILLBOARDMODE_CAMERA_NORMAL_UP");
				material.DisableKeyword("_BILLBOARDMODE_UP");
				material.EnableKeyword("_BILLBOARDMODE_NORMAL");
			}
		}

		static void SetKeyword(Material m, string keyword, bool state)
		{
			if (state)
				m.EnableKeyword(keyword);
			else
				m.DisableKeyword(keyword);
		}
#endif
	}
}