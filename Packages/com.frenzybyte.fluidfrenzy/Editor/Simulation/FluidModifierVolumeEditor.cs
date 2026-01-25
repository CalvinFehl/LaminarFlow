using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace FluidFrenzy.Editor
{
	using static FluidModifierVolume.FluidSourceSettings;
	using static FluidModifierVolume.FluidForceSettings;
	using static FluidModifierVolume.FluidFlowSettings;

#if UNITY_EDITOR
	[CustomEditor(typeof(FluidModifierVolume)), CanEditMultipleObjects]
	public class FluidModifierVolumeEditor : UnityEditor.Editor
	{

		public static class Styles
		{
			public static readonly GUIContent typeLabel = new GUIContent(
				"Type",
				"Specifies the type of fluid modification enabled on this volume, which can be a combination of Source, Flow, and Force."
			);

			public static readonly GUIContent sourceSettingsLabel = new GUIContent(
				"Source Settings",
				"Contains all configuration settings for the fluid source behavior when type includes FluidModifierType.Source."
			);

			public static readonly GUIContent flowSettingsLabel = new GUIContent(
				"Flow Settings",
				"Contains all configuration settings for the fluid flow behavior when type includes FluidModifierType.Flow."
			);

			public static readonly GUIContent forceSettingsLabel = new GUIContent(
				"Force Settings",
				"Contains all configuration settings for the fluid force/wave behavior when type includes FluidModifierType.Force."
			);

			public static class SourceSettings
			{
				public static readonly GUIContent modeLabel = new GUIContent(
					"Source Mode",
					@"Sets the input mode of the modifier, defining the shape or source of the fluid input.

Fluid input modes include: 
•  Circle: Fluid input in a circular shape. 
•  Box: Fluid input in a rectangular shape. 
•  Texture: Fluid input defined by a source texture."
				);
				public static readonly GUIContent dynamicLabel = new GUIContent(
					"Dynamic",
					@"Enables or disables movement for this modifier.

When disabled, the modifier is treated as static and its contribution is calculated once at the start of the simulation. This improves performance for multiple stationary fluid sources."
				);
				public static readonly GUIContent additiveLabel = new GUIContent(
					"Additive",
					"Add or set the amount of fluid to the simulation."
				);

				public static readonly GUIContent blendModeLabel = new GUIContent(
					"Blend Mode",
					@"Defines the blending operation used to apply the fluid source to the simulation's height field.

This determines how the fluid is applied to the simulation's current height. Options include: - FluidModifierBlendMode.Additive: Adds or subtracts the fluid amount. - FluidModifierBlendMode.Set: Sets the height to a specific value. - FluidModifierBlendMode.Minimum/FluidModifierBlendMode.Maximum: Clamps the height to the target value."
				);

				public static readonly GUIContent spaceLabel = new GUIContent(
					"Space",
					@"Specifies the coordinate space to which the fluid height source should be set relative to.


•  WorldHeight: The height is interpreted as a specific world Y-coordinate. 
•  LocalHeight: The height is interpreted relative to the fluid surface's base height."
				);
				public static readonly GUIContent strengthLabel = new GUIContent(
					"Strength",
					@"Adjusts the amount of fluid added or set by the volume.


•  For Additive blending: This is the rate per second of fluid to add/subtract. 
•  For Set blending: This value contributes to the target height."
				);
				public static readonly GUIContent falloffLabel = new GUIContent(
					"Falloff",
					@"Adjusts the curve of the distance-based strength, controlling how quickly the influence falls off from the center.

Higher values create a faster falloff, resulting in a more focused fluid source."
				);
				public static readonly GUIContent layerLabel = new GUIContent(
					"Layer",
					"Specifies the target fluid layer to which the fluid will be added."
				);
				public static readonly GUIContent sizeLabel = new GUIContent(
					"Size",
					"Adjust the size (width and height) of the modification area in world units."
				);
				public static readonly GUIContent textureLabel = new GUIContent(
					"Texture",
					"The source texture used to determine the shape and intensity of the fluid input when mode is FluidSourceMode.Texture. Only the red channel of the texture is used."
				);
			}

			public static class FlowSettings
			{
				public static readonly GUIContent modeLabel = new GUIContent(
					"Flow Mode",
					@"Sets the input mode of the modifier, defining how flow is applied.

Flow application modes include: 
•  Circle: A constant directional flow within a circular shape. 
•  Vortex: A circular flow with radial and tangential control. 
•  Texture: Flow direction supplied from a dedicated flow map texture."
				);
				public static readonly GUIContent directionLabel = new GUIContent(
					"Direction",
					@"Sets the 2D direction in which the flow force will be applied for FluidFlowMode.Circle.

The x component maps to world X, and the y component maps to world Z (assuming a flat surface)."
				);
				public static readonly GUIContent blendModeLabel = new GUIContent(
					"Blend Mode",
					@"The blending operation used to apply the generated velocity to the simulation's velocity field.

This determines how the velocity is applied to the simulation's current flow. Options include: 
•  Additive: Adds or subtracts the flow/velocity amount. 
•  Set: Sets the flow/velocity to a specific vector. 
•  Minimum/Maximum: Clamps the velocity vector components to the target values."
				);
				public static readonly GUIContent strengthLabel = new GUIContent(
					"Strength",
					@"Adjusts the magnitude of the flow applied to the velocity field.

For FluidFlowMode.Vortex mode, this specifically controls the *inward* flow to the center."
				);
				public static readonly GUIContent radialFlowStrengthLabel = new GUIContent(
					"Radial Flow Strength",
					"Adjusts the amount of *tangential* flow applied for FluidFlowMode.Vortex mode. Higher values create a faster spinning vortex."
				);
				public static readonly GUIContent falloffLabel = new GUIContent(
					"Falloff",
					@"Adjusts the curve of the distance-based strength, controlling how quickly the influence falls off from the center.

Higher values create a sharper shape with faster falloff."
				);
				public static readonly GUIContent sizeLabel = new GUIContent(
					"Size",
					"Adjust the size (width and height) of the modification area in world units."
				);
				public static readonly GUIContent textureLabel = new GUIContent(
					"Texture",
					@"The flow map texture used as input when mode is FluidFlowMode.Texture.

The texture's Red and Green channels map to the X and Y velocity components. The texture is unpacked from the [0, 1] range to the [-1, 1] velocity range."
				);
			}

			public static class ForceSettings
			{
				public static readonly GUIContent modeLabel = new GUIContent(
					"Force Mode",
					@"Sets the input mode of the modifier, defining the type of force applied.

Force application modes include: 
•  Circle: A directional force within a circular shape (for waves/pushes). 
•  Vortex: A downward, distance-based force (for whirlpools). 
•  Splash: An immediate outward force (for splash effects). 
•  Texture: Forces created from a texture input."
				);
				public static readonly GUIContent directionLabel = new GUIContent(
					"Direction",
					@"Sets the 2D direction of the applied force/wave propagation.

The x component maps to world X, and the y component maps to world Z (assuming a flat surface)."
				);
				public static readonly GUIContent strengthLabel = new GUIContent(
					"Strength",
					@"Controls the magnitude of the force applied.

This represents the height of the wave/splash, the depth of the vortex, or the strength to apply the supplied texture."
				);
				public static readonly GUIContent falloffLabel = new GUIContent(
					"Falloff",
					@"Adjusts the curve of the distance-based strength, controlling how quickly the influence falls off from the center.

Higher values create a sharper shape with faster falloff."
				);
				public static readonly GUIContent sizeLabel = new GUIContent(
					"Size",
					"Adjust the size (width and height) of the modification area in world units."
				);
				public static readonly GUIContent textureLabel = new GUIContent(
					"Texture",
					"The source texture used as an input when mode is FluidForceMode.Texture. Only the red channel is used for height/force displacement."
				);

				public static readonly GUIContent blendModeLabel = new GUIContent(
					"Blend Mode",
					@"The blending operation used to apply or dampen the force in the simulation.

This determines how the force is applied to the simulation. Options include: 
•  Additive: Adds or subtracts the force amount. 
•  Set: Sets the force to a specific vector. 
•  Minimum/Maximum: Clamps the force vector components to the target values."
				);
			}
		}

		SerializedProperty m_typeProperty;

		SerializedProperty m_sourceSettingsProperty;
		SerializedProperty m_sourceModeProperty;
		SerializedProperty m_sourceBlendModeProperty;
		SerializedProperty m_sourceSpaceProperty;
		SerializedProperty m_sourceDynamicProperty;
		SerializedProperty m_sourceStrengthProperty;
		SerializedProperty m_sourceFalloffProperty;
		SerializedProperty m_sourceSizeProperty;
		SerializedProperty m_sourceTextureProperty;
		SerializedProperty m_sourceLayerProperty;

		SerializedProperty m_flowSettingsProperty;
		SerializedProperty m_flowModeProperty;
		SerializedProperty m_flowDirectionProperty;
		SerializedProperty m_flowBlendModeProperty;
		SerializedProperty m_flowStrengthProperty;
		SerializedProperty m_flowStrengthOuterProperty;
		SerializedProperty m_flowFalloffProperty;
		SerializedProperty m_flowSizeProperty;
		SerializedProperty m_flowTextureProperty;

		SerializedProperty m_forceSettingsProperty;
		SerializedProperty m_forceModeProperty;
		SerializedProperty m_forceDirectionProperty;
		SerializedProperty m_forceBlendModeProperty;
		SerializedProperty m_forceStrengthProperty;
		SerializedProperty m_forceFalloffProperty;
		SerializedProperty m_forceSizeProperty;
		SerializedProperty m_forceTextureProperty;

		Material m_sceneViewGrid;
		FluidSimulation[] m_fluidSimulations;

		void OnEnable()
		{
			m_typeProperty = serializedObject.FindProperty("type");
			m_sourceSettingsProperty = serializedObject.FindProperty("sourceSettings");
			m_sourceModeProperty = m_sourceSettingsProperty.FindPropertyRelative("mode");
			m_sourceSpaceProperty = m_sourceSettingsProperty.FindPropertyRelative("space");
			m_sourceBlendModeProperty = m_sourceSettingsProperty.FindPropertyRelative("blendMode");
			m_sourceDynamicProperty = m_sourceSettingsProperty.FindPropertyRelative("dynamic");
			m_sourceStrengthProperty = m_sourceSettingsProperty.FindPropertyRelative("strength");
			m_sourceFalloffProperty = m_sourceSettingsProperty.FindPropertyRelative("falloff");
			m_sourceSizeProperty = m_sourceSettingsProperty.FindPropertyRelative("size");
			m_sourceTextureProperty = m_sourceSettingsProperty.FindPropertyRelative("texture");
			m_sourceLayerProperty = m_sourceSettingsProperty.FindPropertyRelative("layer");

			m_flowSettingsProperty = serializedObject.FindProperty("flowSettings");
			m_flowModeProperty = m_flowSettingsProperty.FindPropertyRelative("mode");
			m_flowDirectionProperty = m_flowSettingsProperty.FindPropertyRelative("direction");
			m_flowBlendModeProperty = m_flowSettingsProperty.FindPropertyRelative("blendMode");
			m_flowStrengthProperty = m_flowSettingsProperty.FindPropertyRelative("strength");
			m_flowStrengthOuterProperty = m_flowSettingsProperty.FindPropertyRelative("radialFlowStrength");
			m_flowFalloffProperty = m_flowSettingsProperty.FindPropertyRelative("falloff");
			m_flowSizeProperty = m_flowSettingsProperty.FindPropertyRelative("size");
			m_flowTextureProperty = m_flowSettingsProperty.FindPropertyRelative("texture");

			m_forceSettingsProperty = serializedObject.FindProperty("forceSettings");
			m_forceModeProperty = m_forceSettingsProperty.FindPropertyRelative("mode");
			m_forceDirectionProperty = m_forceSettingsProperty.FindPropertyRelative("direction");
			m_forceBlendModeProperty = m_forceSettingsProperty.FindPropertyRelative("blendMode");
			m_forceStrengthProperty = m_forceSettingsProperty.FindPropertyRelative("strength");
			m_forceFalloffProperty = m_forceSettingsProperty.FindPropertyRelative("falloff");
			m_forceSizeProperty = m_forceSettingsProperty.FindPropertyRelative("size");
			m_forceTextureProperty = m_forceSettingsProperty.FindPropertyRelative("texture");

			m_sceneViewGrid = new Material(Shader.Find("Hidden/FluidFrenzy/FluidVolumeGrid"));

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
#if UNITY_2021_1_OR_NEWER
			FluidModifierVolume.FluidModifierType editorType = (FluidModifierVolume.FluidModifierType)m_typeProperty.enumValueFlag;
#else
			FluidModifierVolume.FluidModifierType editorType = (FluidModifierVolume.FluidModifierType)m_typeProperty.intValue;
#endif

			EditorGUILayout.PropertyField(m_typeProperty, Styles.typeLabel);

			if ((editorType & FluidModifierVolume.FluidModifierType.Source) != 0)
			{
				if (m_sourceSettingsProperty.isExpanded = EditorExtensions.DrawFoldoutHeader(m_sourceSettingsProperty, Styles.sourceSettingsLabel))
				{
					using (new EditorGUI.IndentLevelScope())
					{
						EditorGUILayout.PropertyField(m_sourceModeProperty, Styles.SourceSettings.modeLabel);
#if UNITY_2021_1_OR_NEWER
						FluidSourceMode sourceMode = (FluidSourceMode)m_sourceModeProperty.enumValueFlag;
#else
						FluidSourceMode sourceMode = (FluidSourceMode)m_sourceModeProperty.intValue;
#endif
						EditorGUILayout.PropertyField(m_sourceDynamicProperty, Styles.SourceSettings.dynamicLabel);
						if (m_sourceDynamicProperty.boolValue)
						{
							EditorGUILayout.PropertyField(m_sourceBlendModeProperty, Styles.SourceSettings.blendModeLabel);
							if (m_sourceBlendModeProperty.enumValueIndex != (int)FluidSimulation.FluidModifierBlendMode.Additive)
							{
								EditorGUILayout.PropertyField(m_sourceSpaceProperty, Styles.SourceSettings.spaceLabel);
							}
						}
						EditorGUILayout.PropertyField(m_sourceSizeProperty, Styles.SourceSettings.sizeLabel);

						bool isModeAdditive = m_sourceBlendModeProperty.enumValueIndex == (int)FluidSimulation.FluidModifierBlendMode.Additive;
						bool isModeLocalSpace = m_sourceSpaceProperty.enumValueIndex == (int)FluidSimulation.FluidModifierSpace.LocalHeight;
						if (isModeAdditive ||
							(!isModeAdditive && isModeLocalSpace))
						{
							EditorGUILayout.PropertyField(m_sourceStrengthProperty, Styles.SourceSettings.strengthLabel);
						}

						if (sourceMode == FluidSourceMode.Circle || sourceMode == FluidSourceMode.Box)
						{
							EditorGUILayout.PropertyField(m_sourceFalloffProperty, Styles.SourceSettings.falloffLabel);
						}
						else if (sourceMode == FluidSourceMode.Texture)
						{
							EditorGUILayout.PropertyField(m_sourceTextureProperty, Styles.SourceSettings.textureLabel);
						}
						EditorGUILayout.PropertyField(m_sourceLayerProperty, Styles.SourceSettings.layerLabel);
					}
				}
				EditorGUILayout.EndFoldoutHeaderGroup();
			}

			if ((editorType & FluidModifierVolume.FluidModifierType.Flow) != 0)
			{
				if (m_flowSettingsProperty.isExpanded = EditorExtensions.DrawFoldoutHeader(m_flowSettingsProperty, Styles.flowSettingsLabel))
				{
					using (new EditorGUI.IndentLevelScope())
					{
						EditorGUILayout.PropertyField(m_flowModeProperty, Styles.FlowSettings.modeLabel);
#if UNITY_2021_1_OR_NEWER
						FluidFlowMode flowMode = (FluidFlowMode)m_flowModeProperty.enumValueFlag;
#else
						FluidFlowMode flowMode = (FluidFlowMode)m_flowModeProperty.intValue;
#endif
						EditorGUILayout.PropertyField(m_flowBlendModeProperty, Styles.FlowSettings.blendModeLabel);

						if (flowMode == FluidFlowMode.Circle)
						{
							EditorExtensions.DrawVector2AsDegrees(m_flowDirectionProperty, Styles.FlowSettings.directionLabel);
						}

						EditorGUILayout.PropertyField(m_flowStrengthProperty, Styles.FlowSettings.strengthLabel);
						EditorGUILayout.PropertyField(m_flowSizeProperty, Styles.FlowSettings.sizeLabel);
						if (flowMode == FluidFlowMode.Circle || flowMode == FluidFlowMode.Vortex)
						{
							if (flowMode == FluidFlowMode.Vortex)
							{
								EditorGUILayout.PropertyField(m_flowStrengthOuterProperty, Styles.FlowSettings.radialFlowStrengthLabel);
							}
							else
							{
								EditorGUILayout.PropertyField(m_flowFalloffProperty, Styles.FlowSettings.falloffLabel);
							}
						}
						else if (flowMode == FluidFlowMode.Texture)
						{
							EditorGUILayout.PropertyField(m_flowTextureProperty, Styles.FlowSettings.textureLabel);
						}
					}
				}
				EditorGUILayout.EndFoldoutHeaderGroup();
			}
			if ((editorType & FluidModifierVolume.FluidModifierType.Force) != 0)
			{
				if (m_forceSettingsProperty.isExpanded = EditorExtensions.DrawFoldoutHeader(m_forceSettingsProperty, Styles.forceSettingsLabel))
				{
					using (new EditorGUI.IndentLevelScope())
					{
						EditorGUILayout.PropertyField(m_forceModeProperty, Styles.ForceSettings.modeLabel);

						EditorGUILayout.PropertyField(m_forceBlendModeProperty, Styles.ForceSettings.blendModeLabel);

#if UNITY_2021_1_OR_NEWER
						FluidForceMode forceMode = (FluidForceMode)m_forceModeProperty.enumValueFlag;
#else
						FluidForceMode forceMode = (FluidForceMode)m_forceModeProperty.intValue;
#endif

						if (forceMode == FluidForceMode.Circle)
						{
							EditorExtensions.DrawVector2AsDegrees(m_forceDirectionProperty, Styles.ForceSettings.directionLabel);
						}

						EditorGUILayout.PropertyField(m_forceSizeProperty, Styles.ForceSettings.sizeLabel);
						EditorGUILayout.PropertyField(m_forceStrengthProperty, Styles.ForceSettings.strengthLabel);
						if (forceMode == FluidForceMode.Circle || forceMode == FluidForceMode.Splash || forceMode == FluidForceMode.Vortex)
						{
							EditorGUILayout.PropertyField(m_forceFalloffProperty, Styles.ForceSettings.falloffLabel);
						}
						else if (forceMode == FluidForceMode.Texture)
						{
							EditorGUILayout.PropertyField(m_forceTextureProperty, Styles.ForceSettings.textureLabel);
						}
					}
				}
				EditorGUILayout.EndFoldoutHeaderGroup();
			}

			serializedObject.ApplyModifiedProperties();
		}

		private void OnSceneGUI()
		{
			if (Camera.current == null || Camera.current.cameraType != CameraType.SceneView)
				return;

			if (Event.current.type != EventType.Repaint)
				return;

			FluidModifierVolume t = (target as FluidModifierVolume);
			Vector3 worldSize = t.GetSize();
			Vector2 boxSize = new Vector2(worldSize.x, worldSize.z);
			Bounds bounds = new Bounds(t.transform.position, new Vector3(worldSize.x, 0, worldSize.z));

			EditorGUI.BeginChangeCheck();
			Vector3 scaledSize = Handles.DoScaleHandle(worldSize, t.transform.position, t.transform.rotation, 5);
			if (EditorGUI.EndChangeCheck())
			{
				Undo.RecordObject(target, "Scaled FluidModifier");
				Vector2 scale = new Vector2(scaledSize.x, scaledSize.z) / boxSize;
				t.Scale(scale);
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

			Vector2 dimension = Vector2.one;
			Vector2Int cells = Vector2Int.one;
			Vector3 terrainScale = Vector3.one;
			Vector3 simulationPosition = Vector3.zero;
			Texture heightmap = Texture2D.blackTexture;
			if (containedSimulation && containedSimulation.settings)
			{
				cells = containedSimulation.settings.numberOfCells;
				dimension = containedSimulation.dimension;


				if (containedSimulation.unityTerrain && containedSimulation.terrainType == FluidSimulation.TerrainType.UnityTerrain)
				{
					simulationPosition = containedSimulation.unityTerrain.transform.position;
					terrainScale = containedSimulation.unityTerrain.terrainData.size;
					terrainScale.y *= (65535.0f / 32766);
					heightmap = containedSimulation.unityTerrain.terrainData.heightmapTexture;
					m_sceneViewGrid.EnableKeyword("_FLUID_UNITY_TERRAIN");
				}
				else if (containedSimulation.simpleTerrain && containedSimulation.terrainType == FluidSimulation.TerrainType.SimpleTerrain)
				{
					simulationPosition = containedSimulation.simpleTerrain.transform.position;

					Vector2 terrainDimension = containedSimulation.simpleTerrain.surfaceProperties.dimension;
					if (Application.isPlaying)
					{
						terrainScale = new Vector3(terrainDimension.x, 1, terrainDimension.y);
						heightmap = containedSimulation.simpleTerrain.renderHeightmap;

					}
					else
					{
						terrainScale = new Vector3(terrainDimension.x, containedSimulation.simpleTerrain.heightScale, terrainDimension.y);
						heightmap = containedSimulation.simpleTerrain.sourceHeightmap;
					}
				}
				else if (containedSimulation.terrainType == FluxFluidSimulation.TerrainType.Heightmap)
				{
					simulationPosition = containedSimulation.transform.position;
					terrainScale = new Vector3(containedSimulation.dimension.x, containedSimulation.heightmapScale, containedSimulation.dimension.y);
					heightmap = containedSimulation.textureHeightmap;
				}
				else if (containedSimulation.terrainType == FluxFluidSimulation.TerrainType.Layers)
				{
					simulationPosition = containedSimulation.transform.position;
				}
			}

			m_sceneViewGrid.SetVector("_TerrainPosition", simulationPosition);
			m_sceneViewGrid.SetVector("_Position", t.transform.position);

			m_sceneViewGrid.SetVector("_TerrainScale", terrainScale);
			m_sceneViewGrid.SetTexture("_Heightmap", heightmap);

			if ((t.type & FluidModifierVolume.FluidModifierType.Source) != 0)
			{
				boxSize = t.sourceSettings.size;
				float mode = (float)t.sourceSettings.mode;
				int blendMode = (int)t.sourceSettings.blendMode;
				int space = (int)t.sourceSettings.space;

				if (t.sourceSettings.blendMode == FluidSimulation.FluidModifierBlendMode.Additive)
					space = 0;

				RenderGrid(t, boxSize, dimension, cells, mode, blendMode, space);
			}

			Vector2 direction = Vector2.one;
			if ((t.type & FluidModifierVolume.FluidModifierType.Force) != 0)
			{
				boxSize = Vector2.one * t.forceSettings.size;
				direction = t.forceSettings.direction;
				float amount = t.forceSettings.strength;
				Texture2D texture = t.forceSettings.texture;
				float mode = (float)t.forceSettings.mode;
				RenderDirectionArrows(boxSize, dimension, cells, amount, mode, texture, direction);
			}

			if ((t.type & FluidModifierVolume.FluidModifierType.Flow) != 0)
			{
				boxSize = t.flowSettings.size;
				direction = t.flowSettings.direction;
				float amount = t.flowSettings.strength;
				Texture2D texture = t.flowSettings.texture;
				float mode = (float)t.flowSettings.mode;

				RenderDirectionArrows(boxSize, dimension, cells, amount, mode, texture, direction);
			}

		}

		// Shared internal function to handle the grid logic and drawing
		private void RenderProceduralMesh(int passIndex, Vector2 boxSize, Vector2 dimension, Vector2Int cells, float shapeMode, float amount,
											Vector2? direction = null, int blendMode = -1, int space = -1)
		{
			dimension.x = Mathf.Max(dimension.x, 0.0001f);
			dimension.y = Mathf.Max(dimension.y, 0.0001f);

			Vector2 percentSize = boxSize / dimension;
			Vector2 gridSize = cells * percentSize;

			// Prevent the GPU from crashing if the grid becomes too dense (e.g., > 256x256)
			float maxResolution = 256f;
			float maxAxis = Mathf.Max(gridSize.x, gridSize.y);

			if (maxAxis > maxResolution)
			{
				float scale = maxResolution / maxAxis;
				gridSize *= scale;
			}

			// Ensure at least 1x1
			gridSize.x = Mathf.Max(1, Mathf.Ceil(gridSize.x));
			gridSize.y = Mathf.Max(1, Mathf.Ceil(gridSize.y));
			int numCells = (int)(gridSize.x * gridSize.y);

			// 3. Set Common Properties
			m_sceneViewGrid.SetVector("_GridSize", gridSize);
			m_sceneViewGrid.SetVector("_GridDim", boxSize);
			m_sceneViewGrid.SetFloat("_Shape", shapeMode);
			m_sceneViewGrid.SetFloat("_Amount", amount);
			// Use a safe log calculation (prevent log of <= 0)
			m_sceneViewGrid.SetFloat("_GradientScale", Mathf.Log(Mathf.Max(amount, 0.0001f), 3));

			if (direction.HasValue)
				m_sceneViewGrid.SetVector("_Direction", direction.Value);

			if (blendMode != -1)
				m_sceneViewGrid.SetFloat("_BlendMode", blendMode);

			if (space != -1)
				m_sceneViewGrid.SetFloat("_Space", space);

			m_sceneViewGrid.SetPass(passIndex);

			if (Camera.current != null)
			{
				using (new RenderTextureScope(Camera.current.targetTexture))
				{
					Graphics.DrawProceduralNow(MeshTopology.Triangles, numCells * 6, 1);
				}
			}
		}

		private void RenderGrid(FluidModifierVolume t, Vector2 boxSize, Vector2 dimension, Vector2Int cells, float mode, int blendMode, int space)
		{
			// Handle the specific Amount logic for the Grid
			float amount = t.sourceSettings.strength;
			if (t.sourceSettings.space == FluidSimulation.FluidModifierSpace.WorldHeight)
				amount = t.transform.position.y;

			// Call shared function with Pass 0
			RenderProceduralMesh(
				passIndex: 0,
				boxSize: boxSize,
				dimension: dimension,
				cells: cells,
				shapeMode: mode,
				amount: amount,
				direction: null,
				blendMode: blendMode,
				space: space
			);
		}

		private void RenderDirectionArrows(Vector2 boxSize, Vector2 dimension, Vector2Int cells, float amount, float mode, Texture2D texture, Vector2 direction)
		{
			// Call shared function with Pass 1
			RenderProceduralMesh(
				passIndex: 1,
				boxSize: boxSize,
				dimension: dimension,
				cells: cells,
				shapeMode: mode,
				amount: amount,
				direction: direction,
				blendMode: -1, 
				space: -1      
			);
		}

	}
}
#endif