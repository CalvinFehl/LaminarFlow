using UnityEditor;
using UnityEngine;

namespace FluidFrenzy.Editor
{
	[HelpURL("https://frenzybyte.github.io/fluidfrenzy/docs/terrain/#terraform-terrain-shader")]
	public class TerraformTerrainShaderGUI : ShaderGUI
	{
		/// <summary>
		/// A static class to hold all GUIContent for the editor UI.
		/// </summary>
		private static class Styles
		{
			public static readonly GUIContent shaderDescriptionLabel = EditorGUIUtility.TrTextContent(
							"Terraform Terrain Shader",
							@"A multi-layered, texture-array-driven surface shader designed to render a terrain that is fully compatible with Fluid Frenzy's terraforming capabilities.

This shader requires **Texture2DArray** assets for its texture slots. These assets can be created using the **Window > Fluid Frenzy Texture Array Creator** tool.

The shader organizes its texture inputs into two primary, fully terraformable groups: Splat Layers and Dynamic Layers.

Splat Layers (R):
Defines the four base layers, blended by a Splatmap (RGBA).

Dynamic Layers:
Three additional layers for dynamic, transformable materials (mud, sand, snow).

Layer Overrides:
Allows per-layer adjustments across all seven layers, including Layer Tint, Tiling / Offset, and Normal Scale, without modifying source textures.

Compatibility:
The FluidFrenzy/TerraformTerrain shader is for URP and BiRP. The High Definition Render Pipeline (HDRP) requires a separate, dedicated shader: FluidFrenzy/HDRP/TerraformTerrain."
						);

			// Main Headers
			public static readonly GUIContent baseLayerHeader = EditorGUIUtility.TrTextContent(
				"Splat Layers (R)",
				"The base terrain layer that has it's visuals controlled by 4 texture layers that are blended using the splatmap (RGBA).");
			public static readonly GUIContent dynamicLayerHeader = EditorGUIUtility.TrTextContent(
				"Dynamic Layers",
				"The 3 terrain layers used for dynamic effects like erosion, deposition, or liquification.");
			public static readonly GUIContent renderingHeader = EditorGUIUtility.TrTextContent(
				"Rendering Settings",
				"Advanced controls for render queue and instancing.");
			public static readonly GUIContent layerOverridesHeader = EditorGUIUtility.TrTextContent(
				"Layer Settings",
				"Per-layer property adjustments to color, tile or scale the normals.");

			// Texture Array Labels (the "row" headers)
			public static readonly GUIContent albedoArrayText = EditorGUIUtility.TrTextContent(
				"Albedo",
				"Texture2DArray for Albedo (RGB)."
			);
			public static readonly GUIContent maskMapArrayText = EditorGUIUtility.TrTextContent(
				"Mask Map",
				"Texture2DArray for Metallic (R), Occlusion (G), and Smoothness (A)."
			);
			public static readonly GUIContent normalMapArrayText = EditorGUIUtility.TrTextContent(
				"Normal Map",
				"Texture2DArray for Normal Maps."
			);

			// Row Headers
			public static readonly GUIContent tintText = EditorGUIUtility.TrTextContent(
				"Layer Tint",
				"A color multiplier for each layer's albedo."
			);
			public static readonly GUIContent tilingText = EditorGUIUtility.TrTextContent(
				"Tiling",
				"The UV tiling (XY) for each layer."
			);
			public static readonly GUIContent offsetText = EditorGUIUtility.TrTextContent(
				"Offset",
				"The UV offset (ZW) for each layer."
			);
			public static readonly GUIContent baseNormalScaleText = EditorGUIUtility.TrTextContent(
				"Normal Scale",
				"Per-layer scale (R,G,B,A) for each of the 4 Base Layer normal maps."
			);
			public static readonly GUIContent dynamicNormalScaleText = EditorGUIUtility.TrTextContent(
				"Normal Scale",
				"Per-layer scale (R,G,B) for each of the 3 Dynamic Layer normal maps."
			);

			// New header for the first column
			public static readonly GUIContent propertyHeaderText = EditorGUIUtility.TrTextContent(
				"Property",
				"The property to override per layer."
			);

			// Column Headers
			public static readonly GUIContent splat1Text = EditorGUIUtility.TrTextContent("Splat 1 (R)");
			public static readonly GUIContent splat2Text = EditorGUIUtility.TrTextContent("Splat 2 (G)");
			public static readonly GUIContent splat3Text = EditorGUIUtility.TrTextContent("Splat 3 (B)");
			public static readonly GUIContent splat4Text = EditorGUIUtility.TrTextContent("Splat 4 (A)");
			public static readonly GUIContent layer1Text = EditorGUIUtility.TrTextContent("Layer 1 (G)");
			public static readonly GUIContent layer2Text = EditorGUIUtility.TrTextContent("Layer 2 (B)");
			public static readonly GUIContent layer3Text = EditorGUIUtility.TrTextContent("Layer 3 (A)");

			// Background colors for table columns
			public static readonly Color columnColorEven = new Color(0.5f, 0.5f, 0.5f, 0.1f);
			public static readonly Color columnColorOdd = new Color(0.5f, 0.5f, 0.5f, 0.0f);
		}

		// Serialized Properties
		MaterialProperty baseLayerColor0Property;
		MaterialProperty baseLayerColor1Property;
		MaterialProperty baseLayerColor2Property;
		MaterialProperty baseLayerColor3Property;

		MaterialProperty baseLayerAlbedoProperty;
		MaterialProperty baseLayerMaskMapProperty;
		MaterialProperty baseLayerBumpMapProperty;
		MaterialProperty baseLayerBumpScaleProperty;
		MaterialProperty baseLayer0_STProperty;
		MaterialProperty baseLayer1_STProperty;
		MaterialProperty baseLayer2_STProperty;
		MaterialProperty baseLayer3_STProperty;

		MaterialProperty dynamicLayerAlbedoProperty;
		MaterialProperty dynamicLayerMaskMapProperty;
		MaterialProperty dynamicLayerBumpMapProperty;
		MaterialProperty dynamicLayerBumpScaleProperty;
		MaterialProperty dynamicLayer0_STProperty;
		MaterialProperty dynamicLayer1_STProperty;
		MaterialProperty dynamicLayer2_STProperty;


#if FLUIDFRENZY_EXTERNALCOMPATIBILITY_CURVEDWORLD
		MaterialProperty curvedWorldBendSettingsProperty = null;
#endif

		MaterialEditor m_materialEditor;

		// Foldout States
		private bool m_foldoutBaseLayers = true;
		private bool m_foldoutDynamicLayers = true;
		private bool m_foldoutRendering = true;

		public void FindProperties(MaterialProperty[] props)
		{
			baseLayerColor0Property = FindProperty("_BaseLayerColor0", props);
			baseLayerColor1Property = FindProperty("_BaseLayerColor1", props);
			baseLayerColor2Property = FindProperty("_BaseLayerColor2", props);
			baseLayerColor3Property = FindProperty("_BaseLayerColor3", props);

			baseLayerAlbedoProperty = FindProperty("_BaseLayerAlbedo", props);
			baseLayerMaskMapProperty = FindProperty("_BaseLayerMaskMap", props);
			baseLayerBumpMapProperty = FindProperty("_BaseLayerBumpMap", props);
			baseLayerBumpScaleProperty = FindProperty("_BaseLayerBumpScale", props);
			baseLayer0_STProperty = FindProperty("_BaseLayer0_ST", props);
			baseLayer1_STProperty = FindProperty("_BaseLayer1_ST", props);
			baseLayer2_STProperty = FindProperty("_BaseLayer2_ST", props);
			baseLayer3_STProperty = FindProperty("_BaseLayer3_ST", props);

			dynamicLayerAlbedoProperty = FindProperty("_DynamicLayerAlbedo", props);
			dynamicLayerMaskMapProperty = FindProperty("_DynamicLayerMaskMap", props);
			dynamicLayerBumpMapProperty = FindProperty("_DynamicLayerBumpMap", props);
			dynamicLayerBumpScaleProperty = FindProperty("_DynamicLayerBumpScale", props);
			dynamicLayer0_STProperty = FindProperty("_DynamicLayer0_ST", props);
			dynamicLayer1_STProperty = FindProperty("_DynamicLayer1_ST", props);
			dynamicLayer2_STProperty = FindProperty("_DynamicLayer2_ST", props);

#if FLUIDFRENZY_EXTERNALCOMPATIBILITY_CURVEDWORLD
			curvedWorldBendSettingsProperty = FindProperty("_CurvedWorldBendSettings", props, false);
#endif
		}

		public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] props)
		{
			FindProperties(props);
			m_materialEditor = materialEditor;
			Material material = materialEditor.target as Material;

			ShaderPropertiesGUI(material);
		}

		public void ShaderPropertiesGUI(Material material)
		{
			EditorGUIUtility.labelWidth = 0f;
			DrawCurvedWorldProperties();
			DrawBaseLayerProperties();
			EditorGUILayout.Space(); // Added space between sections
			DrawDynamicLayerProperties();
			DrawRenderingProperties();
			EditorGUILayout.Space();
		}

		public void DrawCurvedWorldProperties()
		{
#if FLUIDFRENZY_EXTERNALCOMPATIBILITY_CURVEDWORLD
			if (curvedWorldBendSettingsProperty != null)
			{
				// EditorExtensions.DrawHeader(new GUIContent("Curved World"));
				m_materialEditor.ShaderProperty(curvedWorldBendSettingsProperty, "Curved World");
			}
#endif
		}

		public void DrawBaseLayerProperties()
		{
			if (EditorExtensions.DrawFoldoutHeader(ref m_foldoutBaseLayers, Styles.baseLayerHeader))
			{
				using (new EditorGUI.IndentLevelScope())
				{
					// Main Texture Array Properties 
					DrawTextureSlots(
						new MaterialProperty[] { baseLayerAlbedoProperty, baseLayerMaskMapProperty, baseLayerBumpMapProperty },
						new GUIContent[] { Styles.albedoArrayText, Styles.maskMapArrayText, Styles.normalMapArrayText }
					);

					EditorGUILayout.Space();
					EditorExtensions.DrawHeader(Styles.layerOverridesHeader);

					// Column Headers 
					Rect headerRect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
					DrawTableHeaders(headerRect, Styles.splat1Text, Styles.splat2Text, Styles.splat3Text, Styles.splat4Text);

					// Row 1: Layer Tint 
					DrawColorRow(Styles.tintText, new MaterialProperty[] {
						baseLayerColor0Property, baseLayerColor1Property, baseLayerColor2Property, baseLayerColor3Property
					});

					// Row 2: Tiling (XY) 
					DrawTilingRow(Styles.tilingText, new MaterialProperty[] {
						baseLayer0_STProperty, baseLayer1_STProperty, baseLayer2_STProperty, baseLayer3_STProperty
					});

					// Row 3: Offset (ZW) 
					DrawOffsetRow(Styles.offsetText, new MaterialProperty[] {
						baseLayer0_STProperty, baseLayer1_STProperty, baseLayer2_STProperty, baseLayer3_STProperty
					});

					// Row 4: Normal Scale 
					DrawNormalScaleRow(Styles.baseNormalScaleText, baseLayerBumpScaleProperty, 4);
				}
			}
		}

		public void DrawDynamicLayerProperties()
		{
			if (EditorExtensions.DrawFoldoutHeader(ref m_foldoutDynamicLayers, Styles.dynamicLayerHeader))
			{
				using (new EditorGUI.IndentLevelScope())
				{
					// Main Texture Array Properties 
					DrawTextureSlots(
						new MaterialProperty[] { dynamicLayerAlbedoProperty, dynamicLayerMaskMapProperty, dynamicLayerBumpMapProperty },
						new GUIContent[] { Styles.albedoArrayText, Styles.maskMapArrayText, Styles.normalMapArrayText }
					);

					EditorGUILayout.Space();
					EditorExtensions.DrawHeader(Styles.layerOverridesHeader);

					// Column Headers 
					Rect headerRect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
					DrawTableHeaders(headerRect, Styles.layer1Text, Styles.layer2Text, Styles.layer3Text);

					// Row 1: Tiling (XY) 
					DrawTilingRow(Styles.tilingText, new MaterialProperty[] {
						dynamicLayer0_STProperty, dynamicLayer1_STProperty, dynamicLayer2_STProperty
					});

					// Row 2: Offset (ZW) 
					DrawOffsetRow(Styles.offsetText, new MaterialProperty[] {
						dynamicLayer0_STProperty, dynamicLayer1_STProperty, dynamicLayer2_STProperty
					});

					// Row 3: Normal Scale 
					DrawNormalScaleRow(Styles.dynamicNormalScaleText, dynamicLayerBumpScaleProperty, 3);
				}
			}
		}

		public void DrawRenderingProperties()
		{
			if (EditorExtensions.DrawFoldoutHeader(ref m_foldoutRendering, Styles.renderingHeader))
			{
				m_materialEditor.RenderQueueField();
				m_materialEditor.EnableInstancingField();
				EditorGUILayout.Space();
			}
		}

		const float kLabelWidth = 90f;

		/// <summary>
		/// Draws a row of large, fixed-size texture slots using a manual Rect layout.
		/// </summary>
		private void DrawTextureSlots(MaterialProperty[] props, GUIContent[] labels)
		{
			const float slotSize = 90f;
			const float slotPadding = 10f; // Space between slots
			const float labelHeight = 18f; // Height for the label
			float totalHeight = slotSize + labelHeight;

			// Get a rect for the whole row
			Rect rowRect = EditorGUILayout.GetControlRect(false, totalHeight);

			float currentX = rowRect.x;

			for (int i = 0; i < props.Length; i++)
			{
				if (props[i] == null) continue;

				// Calculate width for the label. Use CalcMinMaxWidth to find the true width.
				float labelWidth = EditorStyles.label.CalcSize(labels[i]).x;
				// The column width is the larger of the label or the 80px slot
				float columnWidth = Mathf.Max(slotSize, labelWidth);

				// Draw Label at the top of the rect
				Rect labelRect = new Rect(currentX, rowRect.y, columnWidth, labelHeight);
				EditorGUI.LabelField(labelRect, labels[i]);

				// Draw Object Field below the label, centered horizontally in its column
				Rect textureRect = new Rect(currentX + (columnWidth - slotSize) * 0.5f, rowRect.y + labelHeight, slotSize, slotSize);
				props[i].textureValue = (Texture)EditorGUI.ObjectField(textureRect, props[i].textureValue, typeof(Texture2DArray), false);

				// Move to the next column
				currentX += columnWidth + slotPadding;
			}
		}

		private void DrawTableHeaders(Rect rect, params GUIContent[] labels)
		{
			// Add the "Property" header for the first column
			Rect propertyLabelRect = new Rect(rect.x, rect.y, kLabelWidth, rect.height);
			EditorGUI.LabelField(propertyLabelRect, Styles.propertyHeaderText, EditorStyles.boldLabel);

			rect.x += kLabelWidth;
			rect.width -= kLabelWidth;
			float colWidth = rect.width / labels.Length;

			for (int i = 0; i < labels.Length; i++)
			{
				Rect labelRect = new Rect(rect.x + i * colWidth, rect.y, colWidth, rect.height);
				// Draw alternating column background color
				EditorGUI.DrawRect(labelRect, (i % 2 == 0) ? Styles.columnColorOdd : Styles.columnColorEven);
				// Draw the column headers with bold style
				EditorGUI.LabelField(labelRect, labels[i], EditorStyles.boldLabel);
			}
		}

		/// <summary>
		/// Draws a table row for full MaterialProperties (like Color).
		/// </summary>
		private void DrawColorRow(GUIContent label, MaterialProperty[] props)
		{
			Rect rect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
			DrawRowLabel(rect, label);

			float colWidth = GetColumnWidth(rect, props.Length);
			for (int i = 0; i < props.Length; i++)
			{
				Rect propRect = GetColumnRect(rect, i, colWidth);
				DrawColumnBackground(propRect, i);
				if (props[i] != null) m_materialEditor.ShaderProperty(propRect, props[i], GUIContent.none);
			}
		}

		/// <summary>
		/// Draws a table row for the Tiling (XY) part of _ST Vector4 properties.
		/// </summary>
		private void DrawTilingRow(GUIContent label, MaterialProperty[] props)
		{
			Rect rect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
			DrawRowLabel(rect, label);

			float colWidth = GetColumnWidth(rect, props.Length);
			for (int i = 0; i < props.Length; i++)
			{
				Rect propRect = GetColumnRect(rect, i, colWidth);
				DrawColumnBackground(propRect, i);
				DrawVector2Property(propRect, props[i], 0); // 0 = Tiling (XY)
			}
		}

		/// <summary>
		/// Draws a table row for the Offset (ZW) part of _ST Vector4 properties.
		/// </summary>
		private void DrawOffsetRow(GUIContent label, MaterialProperty[] props)
		{
			Rect rect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
			DrawRowLabel(rect, label);

			float colWidth = GetColumnWidth(rect, props.Length);
			for (int i = 0; i < props.Length; i++)
			{
				Rect propRect = GetColumnRect(rect, i, colWidth);
				DrawColumnBackground(propRect, i);
				DrawVector2Property(propRect, props[i], 2); // 2 = Offset (ZW)
			}
		}

		/// <summary>
		/// Draws a table row for the individual Float components of a Vector4 property.
		/// </summary>
		private void DrawNormalScaleRow(GUIContent label, MaterialProperty prop, int componentCount)
		{
			Rect rect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
			DrawRowLabel(rect, label);

			float colWidth = GetColumnWidth(rect, componentCount);
			for (int i = 0; i < componentCount; i++)
			{
				Rect propRect = GetColumnRect(rect, i, colWidth);
				DrawColumnBackground(propRect, i);
				DrawFloatProperty(propRect, prop, i); // i = 0(R), 1(G), 2(B), 3(A)
			}
		}

		private void DrawRowLabel(Rect rect, GUIContent label)
		{
			Rect labelRect = new Rect(rect.x, rect.y, kLabelWidth, rect.height);
			EditorGUI.LabelField(labelRect, label);
		}

		private float GetColumnWidth(Rect rect, int columns)
		{
			return (rect.width - kLabelWidth) / columns;
		}

		private Rect GetColumnRect(Rect rect, int index, float colWidth)
		{
			return new Rect(rect.x + kLabelWidth + index * colWidth, rect.y, colWidth, rect.height);
		}

		private void DrawColumnBackground(Rect rect, int index)
		{
			EditorGUI.DrawRect(rect, (index % 2 == 0) ? Styles.columnColorOdd : Styles.columnColorEven);
		}

		private void DrawVector2Property(Rect rect, MaterialProperty prop, int firstComponent)
		{
			if (prop == null) return;
			Vector4 val = prop.vectorValue;
			Vector2 vec = (firstComponent == 0) ? new Vector2(val.x, val.y) : new Vector2(val.z, val.w);

			EditorGUI.BeginChangeCheck();
			rect.xMin += 2; rect.xMax -= 2;
			vec = EditorGUI.Vector2Field(rect, GUIContent.none, vec);
			if (EditorGUI.EndChangeCheck())
			{
				if (firstComponent == 0) { val.x = vec.x; val.y = vec.y; }
				else { val.z = vec.x; val.w = vec.y; }
				prop.vectorValue = val;
			}
		}

		private void DrawFloatProperty(Rect rect, MaterialProperty prop, int componentIndex)
		{
			if (prop == null) return;
			Vector4 val = prop.vectorValue;
			float f = val[componentIndex];

			EditorGUI.BeginChangeCheck();
			rect.xMin += 2; rect.xMax -= 2;
			f = EditorGUI.FloatField(rect, GUIContent.none, f);
			if (EditorGUI.EndChangeCheck())
			{
				val[componentIndex] = f;
				prop.vectorValue = val;
			}
		}
	}
}

