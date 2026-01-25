using UnityEditor;
using UnityEngine;

namespace FluidFrenzy.Editor
{
	[HelpURL("https://frenzybyte.github.io/fluidfrenzy/docs/terrain/#terraform-terrain-shader")]
	public class TerraformTerrainSingleLayerShaderGUI : ShaderGUI
	{
		private static class Styles
		{
			public static GUIContent layer0ColorText = EditorGUIUtility.TrTextContent("Color", "Color (RGB) multiplier of Layer 1.");
			public static GUIContent layer0AlbedoText = EditorGUIUtility.TrTextContent("Albedo", "Color (RGB) of Layer 1.");
			public static GUIContent layer0NormalText = EditorGUIUtility.TrTextContent("Normal Map", "Normal map of Layer 1.");
			public static GUIContent layer0MaskMapText = EditorGUIUtility.TrTextContent("Mask Map", "Metallic(R), Occlusion(G), Smoothness(A) of Layer 1.");

			public static GUIContent layer1ColorText = EditorGUIUtility.TrTextContent("Color", "Color (RGB) multiplier of Layer 2.");
			public static GUIContent layer1AlbedoText = EditorGUIUtility.TrTextContent("Albedo", "Color (RGB) of Layer 2.");
			public static GUIContent layer1NormalText = EditorGUIUtility.TrTextContent("Normal Map", "Normal map of Layer 2.");
			public static GUIContent layer1MaskMapText = EditorGUIUtility.TrTextContent("Mask Map", "Metallic(R), Occlusion(G), Smoothness(A) of Layer 2.");

			public static GUIContent layer2ColorText = EditorGUIUtility.TrTextContent("Color", "Color (RGB) multiplier of Layer 3.");
			public static GUIContent layer2AlbedoText = EditorGUIUtility.TrTextContent("Albedo", "Color (RGB) of Layer 3.");
			public static GUIContent layer2NormalText = EditorGUIUtility.TrTextContent("Normal Map", "Normal map of Layer 3.");
			public static GUIContent layer2MaskMapText = EditorGUIUtility.TrTextContent("Mask Map", "Metallic(R), Occlusion(G), Smoothness(A) of Layer 3.");

			public static GUIContent layer3ColorText = EditorGUIUtility.TrTextContent("Color", "Color (RGB) multiplier of Layer 4.");
			public static GUIContent layer3AlbedoText = EditorGUIUtility.TrTextContent("Albedo", "Color (RGB) of Layer 4.");
			public static GUIContent layer3NormalText = EditorGUIUtility.TrTextContent("Normal Map", "Normal map of Layer 4.");
			public static GUIContent layer3MaskMapText = EditorGUIUtility.TrTextContent("Mask Map", "Metallic(R), Occlusion(G), Smoothness(A) of Layer 4.");

			public static GUIContent topLayerAlbedoText = EditorGUIUtility.TrTextContent("Albedo", "Color (RGB) of the Top Layer (Erodable).");
			public static GUIContent topLayerNormalText = EditorGUIUtility.TrTextContent("Normal Map", "Normal map of the Top Layer (Erodable).");
			public static GUIContent topLayerMaskMapText = EditorGUIUtility.TrTextContent("Mask Map", "Metallic(R), Occlusion(G), Smoothness(A) of the Top Layer (Erodable).");

			public static GUIContent layer0Text = new GUIContent("Layer 1");
			public static GUIContent layer1Text = new GUIContent("Layer 2");
			public static GUIContent layer2Text = new GUIContent("Layer 3");
			public static GUIContent layer3Text = new GUIContent("Layer 4");
			public static GUIContent topLayerText = new GUIContent("Top/Erosion Layer");
			public static GUIContent renderingText = new GUIContent("Rendering");
		}

		MaterialProperty layer0Color;
		MaterialProperty layer0Albedo;
		MaterialProperty layer0MaskMap;
		MaterialProperty layer0BumpMap;
		MaterialProperty layer0BumpScale;

		MaterialProperty layer1Color;
		MaterialProperty layer1Albedo;
		MaterialProperty layer1MaskMap;
		MaterialProperty layer1BumpMap;
		MaterialProperty layer1BumpScale;

		MaterialProperty layer2Color;
		MaterialProperty layer2Albedo;
		MaterialProperty layer2MaskMap;
		MaterialProperty layer2BumpMap;
		MaterialProperty layer2BumpScale;

		MaterialProperty layer3Color;
		MaterialProperty layer3Albedo;
		MaterialProperty layer3MaskMap;
		MaterialProperty layer3BumpMap;
		MaterialProperty layer3BumpScale;
		
		MaterialProperty topLayerAlbedo;
		MaterialProperty topLayerMaskMap;
		MaterialProperty topLayerBumpMap;
		MaterialProperty topLayerBumpScale;

#if FLUIDFRENZY_EXTERNALCOMPATIBILITY_CURVEDWORLD
		// Curved world
		MaterialProperty curvedWorldBendSettingsProperty = null;
#endif

		MaterialEditor m_MaterialEditor;

		private static bool foldoutLayer0 = true;
		private static bool foldoutLayer1 = true;
		private static bool foldoutLayer2 = true;
		private static bool foldoutLayer3 = true;
		private static bool foldoutToplayer = true;
		private static bool foldoutRendering = true;

		public void FindProperties(MaterialProperty[] props)
		{
			layer0Color = FindProperty("_Layer0Color", props);
			layer0Albedo = FindProperty("_Layer0Albedo", props);
			layer0MaskMap = FindProperty("_Layer0MaskMap", props);
			layer0BumpMap = FindProperty("_Layer0BumpMap", props);
			layer0BumpScale = FindProperty("_Layer0BumpScale", props);

			layer1Color = FindProperty("_Layer1Color", props);
			layer1Albedo = FindProperty("_Layer1Albedo", props);
			layer1MaskMap = FindProperty("_Layer1MaskMap", props);
			layer1BumpMap = FindProperty("_Layer1BumpMap", props);
			layer1BumpScale = FindProperty("_Layer1BumpScale", props);

			layer2Color = FindProperty("_Layer2Color", props);
			layer2Albedo = FindProperty("_Layer2Albedo", props);
			layer2MaskMap = FindProperty("_Layer2MaskMap", props);
			layer2BumpMap = FindProperty("_Layer2BumpMap", props);
			layer2BumpScale = FindProperty("_Layer2BumpScale", props);

			layer3Color = FindProperty("_Layer3Color", props);
			layer3Albedo = FindProperty("_Layer3Albedo", props);
			layer3MaskMap = FindProperty("_Layer3MaskMap", props);
			layer3BumpMap = FindProperty("_Layer3BumpMap", props);
			layer3BumpScale = FindProperty("_Layer3BumpScale", props);

			topLayerAlbedo = FindProperty("_TopLayerAlbedo", props);
			topLayerMaskMap = FindProperty("_TopLayerMaskMap", props);
			topLayerBumpMap = FindProperty("_TopLayerBumpMap", props);
			topLayerBumpScale = FindProperty("_TopLayerBumpScale", props);

#if FLUIDFRENZY_EXTERNALCOMPATIBILITY_CURVEDWORLD
			curvedWorldBendSettingsProperty = FindProperty("_CurvedWorldBendSettings", props, false);
#endif
		}

		public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] props)
		{
			FindProperties(props);
			m_MaterialEditor = materialEditor;
			Material material = materialEditor.target as Material;

			ShaderPropertiesGUI(material);
		}

		public void ShaderPropertiesGUI(Material material)
		{
			// Use default labelWidth
			EditorGUIUtility.labelWidth = 0f;
			DrawCurvedWorldProperties();
			DrawLayer0Properties();
			DrawLayer1Properties();
			DrawLayer2Properties();
			DrawLayer3Properties();
			DrawTopLayerProperties();
			DrawRenderingProperties();
			EditorGUILayout.Space();
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


		public void DrawLayer0Properties()
		{
			if (EditorExtensions.DrawFoldoutHeader(ref foldoutLayer0, Styles.layer0Text))
			{
				m_MaterialEditor.TexturePropertySingleLine(Styles.layer0AlbedoText, layer0Albedo, layer0Color);
				m_MaterialEditor.TexturePropertySingleLine(Styles.layer0NormalText, layer0BumpMap, layer0BumpMap.textureValue != null ? layer0BumpScale : null);
				m_MaterialEditor.TexturePropertySingleLine(Styles.layer0MaskMapText, layer0MaskMap);
				m_MaterialEditor.TextureScaleOffsetProperty(layer0Albedo);
				EditorGUILayout.Space();
			}
		}

		public void DrawLayer1Properties()
		{
			if (EditorExtensions.DrawFoldoutHeader(ref foldoutLayer1, Styles.layer1Text))
			{
				m_MaterialEditor.TexturePropertySingleLine(Styles.layer1AlbedoText, layer1Albedo, layer1Color);
				m_MaterialEditor.TexturePropertySingleLine(Styles.layer1NormalText, layer1BumpMap, layer1BumpMap.textureValue != null ? layer1BumpScale : null);
				m_MaterialEditor.TexturePropertySingleLine(Styles.layer1MaskMapText, layer1MaskMap);
				m_MaterialEditor.TextureScaleOffsetProperty(layer1Albedo);
				EditorGUILayout.Space();
			}
		}

		public void DrawLayer2Properties()
		{
			if (EditorExtensions.DrawFoldoutHeader(ref foldoutLayer2, Styles.layer2Text))
			{
				m_MaterialEditor.TexturePropertySingleLine(Styles.layer2AlbedoText, layer2Albedo, layer2Color);
				m_MaterialEditor.TexturePropertySingleLine(Styles.layer2NormalText, layer2BumpMap, layer2BumpMap.textureValue != null ? layer2BumpScale : null);
				m_MaterialEditor.TexturePropertySingleLine(Styles.layer2MaskMapText, layer2MaskMap);
				m_MaterialEditor.TextureScaleOffsetProperty(layer2Albedo);
				EditorGUILayout.Space();
			}
		}

		public void DrawLayer3Properties()
		{
			if (EditorExtensions.DrawFoldoutHeader(ref foldoutLayer3, Styles.layer3Text))
			{
				m_MaterialEditor.TexturePropertySingleLine(Styles.layer3AlbedoText, layer3Albedo, layer3Color);
				m_MaterialEditor.TexturePropertySingleLine(Styles.layer3NormalText, layer3BumpMap, layer3BumpMap.textureValue != null ? layer3BumpScale : null);
				m_MaterialEditor.TexturePropertySingleLine(Styles.layer3MaskMapText, layer3MaskMap);
				m_MaterialEditor.TextureScaleOffsetProperty(layer3Albedo);
				EditorGUILayout.Space();
			}
		}

		public void DrawTopLayerProperties()
		{
			if (EditorExtensions.DrawFoldoutHeader(ref foldoutToplayer, Styles.topLayerText))
			{
				m_MaterialEditor.TexturePropertySingleLine(Styles.topLayerAlbedoText, topLayerAlbedo);
				m_MaterialEditor.TexturePropertySingleLine(Styles.topLayerNormalText, topLayerBumpMap, topLayerBumpMap.textureValue != null ? topLayerBumpScale : null);
				m_MaterialEditor.TexturePropertySingleLine(Styles.topLayerMaskMapText, topLayerMaskMap);
				m_MaterialEditor.TextureScaleOffsetProperty(topLayerAlbedo);
				EditorGUILayout.Space();
			}
		}

		public void DrawRenderingProperties()
		{
			if (EditorExtensions.DrawFoldoutHeader(ref foldoutRendering, Styles.renderingText))
			{
				m_MaterialEditor.RenderQueueField();
				m_MaterialEditor.EnableInstancingField();
				//m_MaterialEditor.DoubleSidedGIField();
				EditorGUILayout.Space();
			}
		}

		public override void OnMaterialPreviewGUI(MaterialEditor materialEditor, Rect r, GUIStyle background)
		{
			Material material = materialEditor.target as Material;
		}
		
		public override void OnMaterialInteractivePreviewGUI(MaterialEditor materialEditor, Rect r, GUIStyle background)
		{
			Material material = materialEditor.target as Material;
		}
	}
}