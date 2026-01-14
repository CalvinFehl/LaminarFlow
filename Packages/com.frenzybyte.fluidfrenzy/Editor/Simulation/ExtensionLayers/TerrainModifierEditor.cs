using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace FluidFrenzy.Editor
{
	using static FluidFrenzy.FluidSimulation;
	using static TerrainModifier;
#if UNITY_EDITOR
	[CustomEditor(typeof(TerrainModifier)), CanEditMultipleObjects]
	public class TerrainModifierEditor : UnityEditor.Editor
	{
		private static class Preview
		{
			public static Material previewTerrainMaterial = null;
			public static int addTerrainCirclePass = 0;
			public static int addSplatmapCirclePass = 0;
			public static int subSplatmapCirclePass = 0;
			public static int setSplatmapCirclePass = 0;

			public static int addTerrainSquarePass = 0;
			public static int addSplatmapSquarePass = 0;
			public static int subSplatmapSquarePass = 0;
			public static int setSplatmapSquarePass = 0;

			public static int addTerrainTexturePass = 0;
			public static int addSplatmapTexturePass = 0;
			public static int subSplatmapTexturePass = 0;
			public static int setSplatmapTexturePass = 0;

			public static int mixTerrainHeightCirclePass = 0;
			public static int mixTerrainHeightSquarePass = 0;
			public static int mixTerrainHeightTexturePass = 0;

			public static int mixTerrainDepthCirclePass = 0;
			public static int mixTerrainDepthSquarePass = 0;
			public static int mixTerrainDepthTexturePass = 0;

			public static void Init()
			{
				if (previewTerrainMaterial)
				{
					return;
				}
				previewTerrainMaterial = new Material(Shader.Find("Hidden/FluidFrenzy/AddTerrain"));
				addTerrainCirclePass = previewTerrainMaterial.FindPass("AddTerrainCircle");
				addSplatmapCirclePass = previewTerrainMaterial.FindPass("AddSplatmapCircle");
				subSplatmapCirclePass = previewTerrainMaterial.FindPass("SubSplatmapCircle");
				setSplatmapCirclePass = previewTerrainMaterial.FindPass("SetSplatmapCircle");

				addTerrainSquarePass = previewTerrainMaterial.FindPass("AddTerrainSquare");
				addSplatmapSquarePass = previewTerrainMaterial.FindPass("AddSplatmapSquare");
				subSplatmapSquarePass = previewTerrainMaterial.FindPass("SubSplatmapSquare");
				setSplatmapSquarePass = previewTerrainMaterial.FindPass("SetSplatmapSquare");

				addTerrainTexturePass = previewTerrainMaterial.FindPass("AddTerrainTexture");
				addSplatmapTexturePass = previewTerrainMaterial.FindPass("AddSplatmapTexture");
				subSplatmapTexturePass = previewTerrainMaterial.FindPass("SubSplatmapTexture");
				setSplatmapTexturePass = previewTerrainMaterial.FindPass("SetSplatmapTexture");

				mixTerrainHeightCirclePass = previewTerrainMaterial.FindPass("MixTerrainHeightCircle");
				mixTerrainHeightSquarePass = previewTerrainMaterial.FindPass("MixTerrainHeightSquare");
				mixTerrainHeightTexturePass = previewTerrainMaterial.FindPass("MixTerrainHeightTexture");

				mixTerrainDepthCirclePass = previewTerrainMaterial.FindPass("MixTerrainDepthCircle");
				mixTerrainDepthSquarePass = previewTerrainMaterial.FindPass("MixTerrainDepthSquare");
				mixTerrainDepthTexturePass = previewTerrainMaterial.FindPass("MixTerrainDepthTexture");
			}
		}

		public static class Styles
		{
			public static readonly GUIContent settingsLabel = new GUIContent(
				"Settings",
				"A collection of parameters that define the shape, strength, and blending of the terrain modification."
			);

			public static readonly GUIContent modeLabel = new GUIContent(
				"Mode",
				@"Defines the shape or source of the modification brush.

Input modes include: 
•  Circle: The brush applies the modification within a circular area. 
•  Box: The brush applies the modification within a rectangular area. 
•  Texture: The brush uses a source texture to define the shape and intensity."
			);
			
			public static readonly GUIContent blendModeLabel = new GUIContent(
				"Blend Mode",
				@"Defines the mathematical operation used to apply the modification to the terrain.

This determines how the modification is applied to the terrain. Options include: 
•  Additive: Raising or lowering the height over time. 
•  Set: Setting the height to a specific value. 
•  Minimum/Maximum: Clamping the height to the target value."
			);
			
			public static readonly GUIContent spaceLabel = new GUIContent(
				"Space",
				@"Specifies the coordinate space used for height modifications.


•  WorldHeight: The height is interpreted as a specific world Y-coordinate. 
•  LocalHeight: The height is interpreted relative to the base terrain height."
			);
			
			public static readonly GUIContent remapLabel = new GUIContent(
				"Remap",
				@"Adjust the range used to remap the normalized input value (e.g., from a texture) to the final output strength.

A normalized input of 0 is mapped to remap.x, and an input of 1 is mapped to remap.y."
			);		
			
			public static readonly GUIContent strengthLabel = new GUIContent(
				"Strength",
				@"Controls the magnitude or intensity of the terrain deformation.

- For Additive blending, this is the amount of height to add/subtract *per second*. - For Set, Minimum, or Maximum blending, this value contributes to the target height."
			);
			
			public static readonly GUIContent falloffLabel = new GUIContent(
				"Falloff",
				@"Adjust the softness of the brush edge (falloff) for the Circle and Box input modes.

Higher values create a wider and softer transition at the modification boundary."
			);
			
			public static readonly GUIContent sizeLabel = new GUIContent(
				"Size",
				"Adjust the size (width and height) of the modification area in world units."
			);
			
			public static readonly GUIContent layerLabel = new GUIContent(
				"Target Layer",
				@"Specifies the target terrain layer (e.g., a specific heightmap channel) to modify.

Typically used to select between the channels of a multi-channel heightmap, such as channel 0 for Red and 1 for Green."
			);
			
			public static readonly GUIContent splatLabel = new GUIContent(
				"Splat Channel",
				@"Specifies the target splatmap channel to use when the blend mode involves a terrain splatmap.

Used to select a specific texture layer for blending (e.g., channel 0 for Red, 1 for Green, etc.)."
			);
			
			public static readonly GUIContent textureLabel = new GUIContent(
				"Source Texture",
				"The source texture used to define the modification shape and intensity when mode is set to TerrainInputMode.Texture."
			);
		}

		private SerializedProperty m_settingsProperty;
		private SerializedProperty m_modeProperty;
		private SerializedProperty m_blendModeProperty;
		private SerializedProperty m_spaceProperty;
		private SerializedProperty m_remapProperty;
		private SerializedProperty m_strengthProperty;
		private SerializedProperty m_falloffProperty;
		private SerializedProperty m_sizeProperty;
		private SerializedProperty m_layerProperty;
		private SerializedProperty m_splatProperty;
		private SerializedProperty m_textureProperty;

		Material m_sceneViewGrid;
		FluidSimulation[] m_fluidSimulations;

		void OnEnable()
		{


			m_settingsProperty = serializedObject.FindProperty("settings");
			m_modeProperty = m_settingsProperty.FindPropertyRelative("mode");
			m_blendModeProperty = m_settingsProperty.FindPropertyRelative("blendMode");
			m_spaceProperty = m_settingsProperty.FindPropertyRelative("space");
			m_remapProperty = m_settingsProperty.FindPropertyRelative("remap");
			m_strengthProperty = m_settingsProperty.FindPropertyRelative("strength");
			m_falloffProperty = m_settingsProperty.FindPropertyRelative("falloff");
			m_sizeProperty = m_settingsProperty.FindPropertyRelative("size");
			m_layerProperty = m_settingsProperty.FindPropertyRelative("layer");
			m_splatProperty = m_settingsProperty.FindPropertyRelative("splat");
			m_textureProperty = m_settingsProperty.FindPropertyRelative("texture");

			m_sceneViewGrid = new Material(Shader.Find("Hidden/FluidFrenzy/TerrainModifyPreview"));

#if UNITY_2023_1_OR_NEWER
			m_fluidSimulations = FindObjectsByType<FluidSimulation>(FindObjectsSortMode.None);
#else
			m_fluidSimulations = FindObjectsOfType<FluidSimulation>();
#endif
		}

		private void OnDisable()
		{
			m_fluidSimulations = null;
		}


		public override void OnInspectorGUI()
		{
			EditorGUI.BeginChangeCheck();

			if (m_settingsProperty.isExpanded = EditorExtensions.DrawFoldoutHeader(m_settingsProperty, Styles.settingsLabel))
			{
				using (new EditorGUI.IndentLevelScope())
				{
					EditorGUILayout.PropertyField(m_modeProperty, Styles.modeLabel);
#if UNITY_2021_1_OR_NEWER
					TerrainInputMode mode = (TerrainInputMode)m_modeProperty.enumValueFlag;
#else
					TerrainInputMode mode = (TerrainInputMode)m_modeProperty.intValue;
#endif
					EditorGUILayout.PropertyField(m_blendModeProperty, Styles.blendModeLabel);
					if (m_blendModeProperty.enumValueIndex != (int)FluidSimulation.FluidModifierBlendMode.Additive)
					{
						EditorGUILayout.PropertyField(m_spaceProperty, Styles.spaceLabel);
					}
					EditorGUILayout.PropertyField(m_sizeProperty, Styles.sizeLabel);

					bool isModeAdditive = m_blendModeProperty.enumValueIndex == (int)TerrainModifierBlendMode.Additive;
					bool isModeLocalSpace = m_spaceProperty.enumValueIndex == (int)FluidSimulation.FluidModifierSpace.LocalHeight;
					if (isModeAdditive ||
						(!isModeAdditive && isModeLocalSpace))
					{
						EditorGUILayout.PropertyField(m_strengthProperty, Styles.strengthLabel);
					}

					if (mode == TerrainInputMode.Circle || mode == TerrainInputMode.Box)
					{
						EditorGUILayout.PropertyField(m_falloffProperty, Styles.falloffLabel);
					}
					else if (mode == TerrainInputMode.Texture)
					{
						EditorExtensions.MinMaxSlider(m_remapProperty, -1, 1, Styles.remapLabel);
						EditorGUILayout.PropertyField(m_textureProperty, Styles.textureLabel);
					}
					EditorGUILayout.PropertyField(m_layerProperty, Styles.layerLabel);
					EditorGUILayout.PropertyField(m_splatProperty, Styles.splatLabel);
				}
			}
			EditorGUILayout.EndFoldoutHeaderGroup();

			serializedObject.ApplyModifiedProperties();
		}

		void TerrainModifierSettingsToPass(TerrainModifier modifier, out int pass, out BlendOp blendOp, out int colorMask)
		{
			pass = 0;
			blendOp = BlendOp.Add;
			TerrainModifierSettings settings = modifier.settings;

			colorMask = FluidSimulation.LayerToColorMask((int)settings.layer);

			if (settings.blendMode == TerrainModifierBlendMode.Minimum)
			{
				blendOp = BlendOp.Min;
			}
			else if (settings.blendMode == TerrainModifierBlendMode.Maximum)
			{
				blendOp = BlendOp.Max;
			}

			if (settings.mode == TerrainInputMode.Circle)
			{
				if (settings.blendMode == TerrainModifierBlendMode.Additive)
				{
					pass = Preview.addTerrainCirclePass;
				}
				else 
				{
					pass = settings.space == FluidModifierSpace.WorldHeight ? Preview.mixTerrainHeightCirclePass : Preview.mixTerrainDepthCirclePass;
				}

			}
			else if (settings.mode == TerrainInputMode.Box)
			{
				if (settings.blendMode == TerrainModifierBlendMode.Additive)
				{
					pass = Preview.addTerrainSquarePass;
				}
				else
				{
					pass = settings.space == FluidModifierSpace.WorldHeight ? Preview.mixTerrainHeightSquarePass : Preview.mixTerrainDepthSquarePass;
				}
			}
			else if (settings.mode == TerrainInputMode.Texture)
			{
				if (settings.blendMode == TerrainModifierBlendMode.Additive)
				{
					pass = Preview.addTerrainTexturePass;
				}
				else
				{
					pass = settings.space == FluidModifierSpace.WorldHeight ? Preview.mixTerrainHeightTexturePass : Preview.mixTerrainDepthTexturePass;
				}
			}
		}

		private void OnSceneGUI()
		{
			if (Camera.current.cameraType != CameraType.SceneView)
				return;

			Preview.Init();

			TerrainModifier modifier = (target as TerrainModifier);
			Vector3 worldSize = modifier.GetSize();
			Vector2 boxSize = new Vector2(worldSize.x, worldSize.z);
			Bounds bounds = new Bounds(modifier.transform.position, new Vector3(worldSize.x, 0, worldSize.z));

			EditorGUI.BeginChangeCheck();
			Vector3 scaledSize = Handles.DoScaleHandle(worldSize, modifier.transform.position, modifier.transform.rotation, 5);
			if (EditorGUI.EndChangeCheck())
			{
				Undo.RecordObject(target, "Scaled FluidModifier");
				Vector2 scale = new Vector2(scaledSize.x, scaledSize.z) / boxSize;
				modifier.Scale(scale);
			}

			FluidSimulation containedSimulation = null;
			if (m_fluidSimulations != null)
			{
				foreach (FluidSimulation sim in m_fluidSimulations)
				{
					Bounds simBounds = sim.CalculateBounds();
					if (simBounds.Intersects(bounds))
						containedSimulation = sim;
				}
			}

			if (!containedSimulation || containedSimulation.terrainType != FluidSimulation.TerrainType.SimpleTerrain || !containedSimulation.simpleTerrain)
				return;

			SimpleTerrain terrain = containedSimulation.simpleTerrain;
			RenderTexture heightmap = terrain.renderHeightmap;
			Vector2 terrainDimension = terrain.surfaceProperties.dimension;
			Vector3 terrainScale = new Vector3(terrainDimension.x, 1, terrainDimension.y);
			Vector2 dimension = terrain.surfaceProperties.dimension;
			Vector2Int cells = terrain.surfaceProperties.meshResolution;

			CommandBuffer cmd = new CommandBuffer();
			RenderTexture previewHeight = RenderTexture.GetTemporary(heightmap.descriptor);

			cmd.Blit(heightmap, previewHeight);
			MaterialPropertyBlock mpb = new MaterialPropertyBlock();

			Bounds terrainBounds = new Bounds(terrain.transform.position, terrainScale);

			Vector2 position = FluidSimulation.WorldSpaceToUVSpace(modifier.transform.position, terrainBounds, terrainDimension);
			Vector2 uvSize = FluidSimulation.WorldSizeToUVSize(modifier.settings.size, terrainDimension);

			float strength = modifier.settings.strength;

			if(modifier.settings.blendMode != TerrainModifierBlendMode.Additive)
			{
				strength = modifier.settings.space == FluidModifierSpace.WorldHeight ? (modifier.transform.position.y - terrain.transform.position.y) : strength;
			}

			mpb.SetVector(FluidShaderProperties._Simulation_TexelSize, new Vector4(1.0f / heightmap.width, 1.0f / heightmap.height, heightmap.width, heightmap.height));
			mpb.SetVector(FluidShaderProperties._BlitScaleBiasRt, new Vector4(uvSize.x, uvSize.y, position.x, position.y));
			mpb.SetVector(FluidShaderProperties._BlitRotation, GraphicsHelpers.DegreesToVec2(modifier.transform.eulerAngles.y));
			mpb.SetVector(FluidShaderProperties._LayerMask, FluidSimulation.LayerToLayerMask((int)modifier.settings.layer));
			mpb.SetVector(FluidShaderProperties._BottomLayersMask, FluidSimulation.LayerToBottomLayersMask((int)modifier.settings.layer));
			mpb.SetVector(FluidShaderProperties._RemapRange, modifier.settings.remap);
			mpb.SetFloat(FluidShaderProperties._IncreaseStrength, strength);
			mpb.SetFloat(FluidShaderProperties._IncreaseExponent, modifier.settings.falloff);
			mpb.SetTexture(FluidShaderProperties._TerrainHeightField, heightmap);


			TerrainModifierSettingsToPass(modifier, out int pass, out BlendOp blendOp, out int colorMask);
			cmd.SetGlobalInt(FluidShaderProperties._ColorMaskTerrain, colorMask);
			cmd.SetGlobalInt(FluidShaderProperties._BlendOpTerrain, (int)blendOp);

			FluidSimulation.BlitQuad(cmd, modifier.settings.texture, previewHeight, Preview.previewTerrainMaterial, mpb, pass);
			Graphics.ExecuteCommandBuffer(cmd);
			RenderTexture.active = null; 

			m_sceneViewGrid.SetVector("_TerrainPosition", terrain.transform.position);
			m_sceneViewGrid.SetVector("_Position", modifier.transform.position);

			m_sceneViewGrid.SetVector("_TerrainScale", terrainScale);
			m_sceneViewGrid.SetTexture("_HeightmapPreview", previewHeight);
			m_sceneViewGrid.SetTexture("_Heightmap", heightmap);

			boxSize = modifier.settings.size;
			float mode = (float)modifier.settings.mode;
			int blendMode = (int)modifier.settings.blendMode;
			int space = (int)modifier.settings.space;

			if (modifier.settings.blendMode == TerrainModifier.TerrainModifierBlendMode.Additive)
				space = 0;

			RenderGrid(modifier, boxSize, dimension, cells, mode, blendMode, space);
			RenderTexture.ReleaseTemporary(previewHeight);
		}

		private void RenderGrid(TerrainModifier t, Vector2 boxSize, Vector2 dimension, Vector2Int cells, float mode, int blendMode, int space)
		{
			Vector2 percentSize = boxSize / dimension;
			Vector2 gridSize = cells * percentSize;
			gridSize.x = Mathf.Ceil(gridSize.x) + 2;
			gridSize.y = Mathf.Ceil(gridSize.y) + 2;

			int numCells = (int)(gridSize.x * gridSize.y);
			m_sceneViewGrid.SetFloat("_Shape", mode);
			m_sceneViewGrid.SetFloat("_BlendMode", blendMode);
			m_sceneViewGrid.SetFloat("_Space", space);
			m_sceneViewGrid.SetVector("_GridSize", gridSize);
			m_sceneViewGrid.SetVector("_GridDim", boxSize + (boxSize / gridSize) * 2);

			float amount = 0;
			if (t.settings.blendMode == TerrainModifierBlendMode.Additive || t.settings.space != FluidSimulation.FluidModifierSpace.WorldHeight)
				amount = t.settings.strength;
			else
				amount = t.transform.position.y;

			m_sceneViewGrid.SetFloat("_Amount", amount);
			m_sceneViewGrid.SetFloat("_GradientScale", Mathf.Log(amount, 3));
			m_sceneViewGrid.SetMatrix("_ObjectToWorld", t.transform.localToWorldMatrix);
			m_sceneViewGrid.SetPass(0);

			//For some reason in unity 2021 the active targetTexture needs to be set or DirectX12 crashes when closing a editor window (e.g Fluid Frenzy About) with the mouse above the scene view.
			RenderTexture.active = Camera.current.targetTexture;
			Graphics.DrawProceduralNow(MeshTopology.Quads, numCells * 4, 1);
		}
	}
}
#endif