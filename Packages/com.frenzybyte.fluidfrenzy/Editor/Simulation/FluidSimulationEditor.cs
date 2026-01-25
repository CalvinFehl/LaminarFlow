using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using static FluidFrenzy.Editor.EditorExtensions;

namespace FluidFrenzy.Editor
{
#if UNITY_EDITOR
	[CustomEditor(typeof(FluidSimulation), editorForChildClasses:true)]
	public class FluidSimulationEditor : UnityEditor.Editor
	{
		class Styles
		{
			public static GUIContent syncToTerrain = new GUIContent("Sync to Terrain", "Synchronize the Fluid Simulation's transform, bounds and dimension's with the assigned Terrain.");
			public static GUIContent newSettings = new GUIContent("New", "Create a new Fluid Simulation Settings asset and assign it to this Fluid Simulation.");
			public static GUIContent fluxSettingsText = new GUIContent("Settings", "A Flux Fluid Simulation Settings asset that holds the settings to be used for this Fluid Simulation.");
			public static GUIContent flowSettingsText = new GUIContent("Settings", "A Flow Fluid Simulation Settings asset that holds the settings to be used for this Fluid Simulation.");
			public static GUIContent dimensionModeText = new GUIContent(
				"Dimension Mode",
				@"Selects the method used to determine the world-space dimension and cellWorldSize of the simulation.


•  Bounds: The user sets the total world-space dimension. The cellWorldSize is then automatically calculated based on the number of cells in the settings. 
•  CellSize: The user sets the size of a single cell (cellWorldSize). The total world-space dimension is then automatically calculated based on the number of cells in the settings."
			);
			public static GUIContent dimensionText = new GUIContent(
				"Dimension",
				@"The total world-space size (X and Z) of the fluid simulation domain.

This dimension is critical for several components: 
•  Fluid Renderer: Determines the size of the rendered surface mesh. 
•  Fluid Simulation: Scales the behaviour of the fluid (e.g., speed and wave height) to ensure physical consistency regardless of the simulation's size."
			);
			public static GUIContent cellSizeWorldText = new GUIContent("World Space Cell Size", "The width and height that each pixel in the simulation buffer is in world space.");
			public static GUIContent fluidBaseHeightText = new GUIContent(
				"Fluid Base Height",
				@"A normalized offset (from 0 to 1) applied to the fluid's base height.

This subtle adjustment can be used to prevent visual clipping artifacts that may occur between the fluid surface and underlying tessellated or displaced terrain geometry."
			);
			public static GUIContent initialFluidHeightText = new GUIContent(
				"Initial Fluid Height",
				@"Specifies the uniform fluid height at the start of the simulation.

This value defines the initial water *depth* relative to the terrain height at that point. Specifically, it's the target initial Y-coordinate for the fluid surface. Any terrain geometry below this Y-coordinate will be submerged."
			);
			public static GUIContent initialFluidHeightTextureText = new GUIContent(
				"Initial Fluid Height Texture",
				@"A texture mask that specifies a non-uniform initial fluid height across the domain.

This acts as a heightmap, where bright pixels correspond to a higher initial fluid level. The final initial fluid height for any pixel is the maximum of the value sampled from this texture and the uniform initialFluidHeight."
			);
			public static GUIContent terrainTypeText = new GUIContent(
				"Terrain Type",
				@"Specifies which type of scene geometry or data source to use as the base ground for fluid flow calculations.


•  UnityTerrain: Use a standard Unity Terrain assigned to the unityTerrain field. 
•  SimpleTerrain: Use a custom terrain component (e.g., SimpleTerrain or TerraformTerrain) assigned to the simpleTerrain field. 
•  Heightmap: Use a Texture2D as a heightmap assigned to the textureHeightmap field. This is useful for custom terrain systems. 
•  MeshCollider: Use a static MeshCollider assigned to the meshCollider field as the base ground. 
•  Layers: Generate the base heightmap by capturing layers via a top-down orthographic render."
			);
			public static GUIContent unityTerrainText = new GUIContent(
				"Unity Terrain",
				"Assign a Unity Terrain to be used as the simulation's base ground when terrainType is TerrainType.UnityTerrain."
			);
			public static GUIContent simpleTerrainText = new GUIContent(
				"Simple Terrain",
				"Assign a custom terrain component, such as SimpleTerrain or TerraformTerrain, to be used as the base ground when terrainType is TerrainType.SimpleTerrain."
			);
			public static GUIContent textureHeightmapText = new GUIContent(
				"Texture Heightmap",
				"Assign a heightmap Texture2D to be used as the simulation's base ground when terrainType is TerrainType.Heightmap."
			);
			public static GUIContent meshColliderText = new GUIContent(
				"Mesh Collider",
				"Assign a MeshCollider component to be used as the simulation's base ground when terrainType is TerrainType.MeshCollider."
			);
			public static GUIContent captureLayersText = new GUIContent(
				"Capture Layers",
				"A layer mask used to filter which scene objects are captured when terrainType is TerrainType.Layers."
			);
			public static GUIContent captureHeightText = new GUIContent(
				"Capture Height",
				@"The vertical extent, measured in world units, for the top-down orthographic capture when terrainType is TerrainType.Layers.

The orthographic render captures geometry from the simulation object's Y position up to transform.position.y + captureHeight."
			);			
			public static GUIContent updateGroundEveryFrameText = new GUIContent(
				"Update Ground Every Frame",
				"If true, the simulation re-samples the underlying geometry (Unity Terrain, Meshes, Layers, or Colliders) every frame. Enable this if your ground surface is deforming or moving during the simulation."
			);	  

			public static GUIContent neighboursText = new GUIContent("Neighbours", "The neighbouring Fluid Simulation's of this Fluid Simulation.");
			public static GUIContent extentionLayersText = new GUIContent("Extention Layers", "Fluid Simulation extension layers like foam, flow mapping, and terraforming so they are executed within this fluid simulation. Assign any extension fluid layer components that should run with this fluid simulation. ");
			public static GUIContent visualizeInitialFluidHeightOn = EditorGUIUtility.IconContent("SceneviewLighting On", "Toggle scene view visualization of the initial fluid height.");
			public static GUIContent visualizeInitialFluidHeightOff = EditorGUIUtility.IconContent("SceneviewLighting", "Toggle scene view visualization of the initial fluid height.");

			public static GUIContent neighbourHandlesToggleIcon = EditorGUIUtility.IconContent("d_ToggleUVOverlay", "Toggle neighbour Scene View handles.");
			public static GUIContent addNeighbourIcon = EditorGUIUtility.IconContent("Toolbar Plus@2x", "Add a neighbour to this simulation.");
			public static GUIContent disconnectNeighbourIcon = EditorGUIUtility.IconContent("Toolbar Minus@2x", "Disconnect the neighbour link to this simulation.");

			public static Color selectionColor = new Color32(0xAC, 0xCE, 0xF7, 0xFF);
		}

		SerializedProperty m_neighoursProperty;
		SerializedProperty m_settingsProperty;
		SerializedProperty m_terrainTypeProperty;
		SerializedProperty m_unityTerrainProperty;
		SerializedProperty m_simpleTerrainProperty;
		SerializedProperty m_textureHeightmapProperty;
		SerializedProperty m_dimensionModeProperty;
		SerializedProperty m_dimensionProperty;
		SerializedProperty m_cellWorldSizeProperty;
		SerializedProperty m_heightmapScaleProperty;
		SerializedProperty m_meshColliderProperty;
		SerializedProperty m_captureLayersProperty;
		SerializedProperty m_captureHeightProperty;
		SerializedProperty m_updateGroundEveryFrameProperty;
		SerializedProperty m_extensionLayerProperty;
		SerializedProperty m_initialFluidHeightProperty;
		SerializedProperty m_initialFluidHeightTextureProperty;
		SerializedProperty m_fluidBaseHeightProperty;
		SerializedProperty m_colliderProperties;

		SerializedProperty m_iterationsProperty;


		FluidSimulationSettingsEditor m_settingsEditor;

		Material m_fluidSimulationSceneView;

		bool m_visualizeInitialFluidHeight = false;
		bool m_drawNeighbourHandles = false;

		void OnEnable()
		{
			m_neighoursProperty = serializedObject.FindProperty("m_neighbours");
			m_settingsProperty = serializedObject.FindProperty("settings");
			m_terrainTypeProperty = serializedObject.FindProperty("terrainType");
			m_unityTerrainProperty = serializedObject.FindProperty("unityTerrain");
			m_simpleTerrainProperty = serializedObject.FindProperty("simpleTerrain");
			m_textureHeightmapProperty = serializedObject.FindProperty("textureHeightmap");
			m_meshColliderProperty = serializedObject.FindProperty("meshCollider");
			m_captureLayersProperty = serializedObject.FindProperty("captureLayers");
			m_captureHeightProperty = serializedObject.FindProperty("captureHeight");
			m_updateGroundEveryFrameProperty = serializedObject.FindProperty("updateGroundEveryFrame");
			m_dimensionModeProperty = serializedObject.FindProperty("dimensionMode");
			m_dimensionProperty = serializedObject.FindProperty("dimension");
			m_cellWorldSizeProperty = serializedObject.FindProperty("cellWorldSize");
			m_heightmapScaleProperty = serializedObject.FindProperty("heightmapScale");
			m_extensionLayerProperty = serializedObject.FindProperty("extensionLayers");
			m_initialFluidHeightProperty = serializedObject.FindProperty("initialFluidHeight");
			m_initialFluidHeightTextureProperty = serializedObject.FindProperty("initialFluidHeightTexture");
			m_fluidBaseHeightProperty = serializedObject.FindProperty("fluidBaseHeight");
			m_colliderProperties = serializedObject.FindProperty("colliderProperties");

			m_iterationsProperty = serializedObject.FindProperty("iterations");

			m_settingsEditor = CreateEditor(m_settingsProperty.objectReferenceValue) as FluidSimulationSettingsEditor;

			m_fluidSimulationSceneView = new Material(Shader.Find("Hidden/FluidFrenzy/FluidSimulationSceneView"));
		}

		private void OnDisable()
		{
		}

		public override void OnInspectorGUI()
		{
			FluidSimulation sim = target as FluidSimulation;
			serializedObject.Update();
			EditorGUILayout.BeginHorizontal();

			bool expandSettings = EditorExtensions.DrawExpandToggle(m_settingsProperty, sim is FluxFluidSimulation ? Styles.fluxSettingsText : Styles.flowSettingsText);

			using (var settingsChanged = new EditorGUI.ChangeCheckScope())
			using (new EditorExtensions.LabelWidthScope(1))
			{
				EditorGUILayout.PropertyField(m_settingsProperty);
				if (settingsChanged.changed)
				{
					serializedObject.ApplyModifiedProperties();
					m_settingsEditor = CreateEditor(m_settingsProperty.objectReferenceValue) as FluidSimulationSettingsEditor;
				}
			}

			if (GUILayout.Button(Styles.newSettings, GUILayout.Width(50)))
			{
				if (sim is FluxFluidSimulation)
					m_settingsProperty.objectReferenceValue = FluidSimulationSettingsEditor.CreateFluidSimulationSettings<FluxFluidSimulationSettings>(sim.gameObject.scene, sim.gameObject.name);
				else if (sim is FlowFluidSimulation)
					m_settingsProperty.objectReferenceValue = FluidSimulationSettingsEditor.CreateFluidSimulationSettings<FlowFluidSimulationSettings>(sim.gameObject.scene, sim.gameObject.name);
			}
			EditorGUILayout.EndHorizontal();

			FluidSimulationSettings fluidSimSettings = m_settingsProperty.objectReferenceValue as FluidSimulationSettings;

			if (sim is FluxFluidSimulation)
			{
				if (fluidSimSettings != null && !(fluidSimSettings is FluxFluidSimulationSettings))
				{
					EditorGUILayout.HelpBox($"Assigned setting {fluidSimSettings.name} is not a FluxFluidSimulationSettings. This will result in unexpected behaviour. Assign a FluxFluidSimulationSettings asset.", MessageType.Error);
				}
			}
			else if (sim is FlowFluidSimulation)
			{
				if (fluidSimSettings != null && !(fluidSimSettings is FlowFluidSimulationSettings))
				{
					EditorGUILayout.HelpBox($"Assigned setting {fluidSimSettings.name} is not a FlowFluidSimulationSettings. This will result in unexpected behaviour. Assign a FlowFluidSimulationSettings asset.", MessageType.Error);
				}
			}

			if (expandSettings)
			{
				m_settingsEditor?.OnInspectorGUI();
			}
			EditorGUILayout.Space();

			using (new EditorGUILayout.HorizontalScope())
			using(new EditorGUI.DisabledGroupScope(!Application.isPlaying))
			{
				if (GUILayout.Button("Save"))
				{
					sim.Save(Application.dataPath, target.name + ".data");
				}
				if (GUILayout.Button("Load"))
				{
					sim.Load(Application.dataPath, target.name + ".data");
				}
			}


			EditorGUILayout.BeginHorizontal();
			if (GUILayout.Button(Styles.syncToTerrain, GUILayout.Height(20)))
			{
				sim.SyncDimensions();
				sim.SyncTransform();
				sim.CalculateWorldCellSizeFromDimension();
			}

			using(new EditorGUI.DisabledGroupScope(!Application.isPlaying))
			{
				if (GUILayout.Button("Reset", GUILayout.Width(50)))
				{
					sim.ResetSimulation();
				}
			}

			Color color_default = GUI.backgroundColor;
			GUI.backgroundColor = (m_drawNeighbourHandles) ? Styles.selectionColor : color_default;
			if (GUILayout.Button(Styles.neighbourHandlesToggleIcon, GUILayout.Width(50)))
			{
				m_drawNeighbourHandles = !m_drawNeighbourHandles;
				SceneView.RepaintAll();
			}
			GUI.backgroundColor = color_default;

			if (GUILayout.Button(EditorGUIUtility.IconContent("DebuggerAttached"), GUILayout.Width(50)))
			{
				FluidSimulationDebugEditor window = EditorWindow.GetWindow<FluidSimulationDebugEditor>("Fluid Simulation Debug");
				window.SetSimulation(sim);
				window.Show();
			}
			EditorGUILayout.EndHorizontal();

			using (var changedDimension = new EditorGUI.ChangeCheckScope())
			{
				EditorGUILayout.PropertyField(m_dimensionModeProperty, Styles.dimensionModeText);
				using (new EditorGUI.DisabledGroupScope(m_dimensionModeProperty.enumValueIndex != (int)FluidSimulation.DimensionMode.Bounds))
				{
					EditorGUILayout.PropertyField(m_dimensionProperty, Styles.dimensionText);
					Vector2 dimension = m_dimensionProperty.vector2Value;
					Vector2 resolution = fluidSimSettings ? fluidSimSettings.numberOfCells : Vector2.one;

					float aspectDiff = Mathf.Abs(dimension.x / dimension.y - resolution.x / resolution.y);
					if (aspectDiff > 0.25f)
					{
						EditorGUILayout.HelpBox(@"Aspect ration difference between the number of cells and dimension size of the simulation. 
The fluid simulation will correct for this as much as possible but fluids might flow at different rates in the x and y directions. 
It is recommended to match the aspect ratio as close as possible.", MessageType.Warning);
					}

				}
				using (new EditorGUI.DisabledGroupScope(m_dimensionModeProperty.enumValueIndex != (int)FluidSimulation.DimensionMode.CellSize))
				{
					EditorGUILayout.PropertyField(m_cellWorldSizeProperty, Styles.cellSizeWorldText);
				}
				if (changedDimension.changed)
				{
					serializedObject.ApplyModifiedProperties();
				}
			}


			if (m_iterationsProperty != null)
			{ 
				Vector4 cellSize = (target as FluidSimulation).GetCellSize();
				m_iterationsProperty = serializedObject.FindProperty("iterations");
				Color backgroundColor = (m_iterationsProperty.intValue >= FlowFluidSimulation.CalculateRequiredIterationCount(cellSize.x)) ? GUI.backgroundColor : Color.red;
				using (var backgroundScope = new BackgroundColorScope(backgroundColor))
				{
					EditorGUILayout.IntSlider(m_iterationsProperty, 1, 4, FlowFluidSimulationEditor.Styles.iterationsLabel);
				}
			}

			sim.CalculateDimensions();

			EditorGUILayout.Space();
			EditorGUILayout.PropertyField(m_fluidBaseHeightProperty, Styles.fluidBaseHeightText);
			using (new EditorGUILayout.HorizontalScope())
			{
				EditorGUILayout.PropertyField(m_initialFluidHeightProperty, Styles.initialFluidHeightText);
				GUIContent visualizeButtonIcon = m_visualizeInitialFluidHeight ? Styles.visualizeInitialFluidHeightOn : Styles.visualizeInitialFluidHeightOff;
				if (GUILayout.Button(visualizeButtonIcon, GUILayout.Width(50)))
				{
					m_visualizeInitialFluidHeight = !m_visualizeInitialFluidHeight;
					SceneView.RepaintAll();
				}
			}
			EditorGUILayout.PropertyField(m_initialFluidHeightTextureProperty, Styles.initialFluidHeightTextureText);
			EditorGUILayout.PropertyField(m_terrainTypeProperty, Styles.terrainTypeText);

			using (var terrainSourceChanged = new EditorGUI.ChangeCheckScope())
			{
				if (m_terrainTypeProperty.enumValueIndex == (int)FluidSimulation.TerrainType.UnityTerrain)
					EditorGUILayout.PropertyField(m_unityTerrainProperty, Styles.unityTerrainText);
				else if (m_terrainTypeProperty.enumValueIndex == (int)FluidSimulation.TerrainType.SimpleTerrain)
					EditorGUILayout.PropertyField(m_simpleTerrainProperty, Styles.simpleTerrainText);
				else if (m_terrainTypeProperty.enumValueIndex == (int)FluidSimulation.TerrainType.Heightmap)
					EditorGUILayout.PropertyField(m_textureHeightmapProperty, Styles.textureHeightmapText);
				else if (m_terrainTypeProperty.enumValueIndex == (int)FluidSimulation.TerrainType.MeshCollider)
					EditorGUILayout.PropertyField(m_meshColliderProperty, Styles.meshColliderText);
				else if (m_terrainTypeProperty.enumValueIndex == (int)FluidSimulation.TerrainType.Layers)
				{
					EditorGUILayout.PropertyField(m_captureLayersProperty, Styles.captureLayersText);
					EditorGUILayout.PropertyField(m_captureHeightProperty, Styles.captureHeightText);
				}

				if (m_terrainTypeProperty.enumValueIndex != (int)FluidSimulation.TerrainType.SimpleTerrain)
					EditorGUILayout.PropertyField(m_updateGroundEveryFrameProperty, Styles.updateGroundEveryFrameText);
				

				if (terrainSourceChanged.changed)
				{
					serializedObject.ApplyModifiedProperties();
					sim.SyncDimensions();
					sim.SyncTransform();
				}
			}
			if (m_terrainTypeProperty.enumValueIndex == (int)FluidSimulation.TerrainType.Heightmap)
				EditorGUILayout.PropertyField(m_heightmapScaleProperty);

			EditorGUILayout.PropertyField(m_extensionLayerProperty, Styles.extentionLayersText);

			if (m_neighoursProperty.isExpanded = EditorGUILayout.BeginFoldoutHeaderGroup(m_neighoursProperty.isExpanded, Styles.neighboursText))
			{
				DrawNeighbour(sim, "Left", FluidSimulation.BoundarySides.Left);
				DrawNeighbour(sim, "Right", FluidSimulation.BoundarySides.Right);
				DrawNeighbour(sim, "Top", FluidSimulation.BoundarySides.Top);
				DrawNeighbour(sim, "Bottom", FluidSimulation.BoundarySides.Bottom);
			}
			EditorGUILayout.EndFoldoutHeaderGroup();

			EditorGUI.BeginChangeCheck();
			EditorGUILayout.PropertyField(m_colliderProperties);
			bool colliderChanged = EditorGUI.EndChangeCheck();

			serializedObject.ApplyModifiedProperties();

			if (colliderChanged)
			{
				(target as FluidSimulation).OnColliderChanged();
			}
		}

		private void VisualizeInitialFluidHeight(FluidSimulation targetSim)
		{
			if (!m_visualizeInitialFluidHeight)
				return;
			Vector3 terrainScale = Vector3.one;
			Texture heightmap = null;

			Vector4 uvScaleOffset = Vector2.one;
			if (targetSim)
			{
				if (targetSim.unityTerrain && targetSim.terrainType == FluidSimulation.TerrainType.UnityTerrain)
				{
					terrainScale = targetSim.unityTerrain.terrainData.size;
					terrainScale.y *= (65535.0f / 32766);
					heightmap = targetSim.unityTerrain.terrainData.heightmapTexture;

					uvScaleOffset = targetSim.CalculateTerrainUVScaleOffset();
					m_fluidSimulationSceneView.EnableKeyword("_FLUID_UNITY_TERRAIN");
				}
				else if (targetSim.simpleTerrain && targetSim.terrainType == FluidSimulation.TerrainType.SimpleTerrain)
				{
					Vector2 terrainDimension = targetSim.simpleTerrain.surfaceProperties.dimension;
					if (Application.isPlaying)
					{
						terrainScale = new Vector3(terrainDimension.x, 1, terrainDimension.y);
						heightmap = targetSim.simpleTerrain.renderHeightmap;

					}
					else
					{
						terrainScale = new Vector3(terrainDimension.x, targetSim.simpleTerrain.heightScale, terrainDimension.y);
						heightmap = targetSim.simpleTerrain.sourceHeightmap;
					}
				}
				else if (targetSim.terrainType == FluxFluidSimulation.TerrainType.Heightmap)
				{
					terrainScale = new Vector3(targetSim.dimension.x, targetSim.heightmapScale, targetSim.dimension.y);
					heightmap = targetSim.textureHeightmap;
				}
			}

			Matrix4x4 transform = Matrix4x4.TRS(targetSim.transform.position, targetSim.transform.rotation, new Vector3(targetSim.dimension.x * 0.5f, 1, targetSim.dimension.y * 0.5f));
			m_fluidSimulationSceneView.SetFloat("_FluidHeight", targetSim.initialFluidHeight);
			m_fluidSimulationSceneView.SetVector("_Position", targetSim.transform.position);
			m_fluidSimulationSceneView.SetVector("_TerrainScale", terrainScale);
			m_fluidSimulationSceneView.SetTexture("_Heightmap", heightmap);
			m_fluidSimulationSceneView.SetMatrix("_WorldMatrix", transform);
			m_fluidSimulationSceneView.SetVector("_TransformScale", new Vector4(uvScaleOffset.x, uvScaleOffset.y, uvScaleOffset.z, uvScaleOffset.w));

			m_fluidSimulationSceneView.SetPass(0);

			//For some reason in unity 2021 the active targetTexture needs to be set or DirectX12 crashes when closing a editor window (e.g Fluid Frenzy About) with the mouse above the scene view.
			RenderTexture.active = Camera.current.targetTexture;
			Graphics.DrawProceduralNow(MeshTopology.Quads, 4, 1);
		}

		private void DrawNeighbour(FluidSimulation parentSim, string name, FluidSimulation.BoundarySides side)
		{
			GUIContent labelName = new GUIContent(name);
			using (new EditorGUILayout.HorizontalScope())
			{
				SerializedProperty neighbourProperty = m_neighoursProperty.GetArrayElementAtIndex((int)side);
				EditorGUILayout.PropertyField(neighbourProperty, labelName);

				if (!neighbourProperty.objectReferenceValue)
				{
					if (GUILayout.Button(Styles.addNeighbourIcon, GUILayout.Height(20), GUILayout.Width(40)))
					{
						parentSim.AddNeighbour(side);
						serializedObject.ApplyModifiedProperties();
					}
				}
				else
				{
					if (GUILayout.Button(Styles.disconnectNeighbourIcon, GUILayout.Height(20), GUILayout.Width(40)))
					{
						parentSim.DisconnectNeighbour(side);
						serializedObject.ApplyModifiedProperties();
					}
				}
			}
		}

		public void OnSceneGUI()
		{
			if (Camera.current.cameraType != CameraType.SceneView)
				return;

			FluidSimulation targetSim = target as FluidSimulation;
			Handles.color = new Color(0.3490195f, 0.6313726f, 0.5843138f, 1).linear;
			Bounds bounds = targetSim.CalculateBounds();
			Handles.DrawWireCube(bounds.center, bounds.size);
			Vector3 size = bounds.size;

			if (Application.isPlaying || !m_drawNeighbourHandles) return;

			List<FluidSimulation> simulationsInGroup = targetSim.GetSimulationGroup();
			Dictionary<Vector2Int, FluidSimulation> simulationMap = targetSim.GetSimulationGroupGrid();

			Handles.color = new Color(0.9190195f, 0.6313726f, 0.0f, 1).linear;
			foreach (FluidSimulation currentSim in simulationsInGroup)
			{
				Vector3 pos = currentSim.transform.position;
				DrawNeighbourSceneHandle(currentSim, simulationMap, pos - new Vector3(0, 0, size.z), size, FluidSimulation.BoundarySides.Bottom);
				DrawNeighbourSceneHandle(currentSim, simulationMap, pos + new Vector3(0, 0, size.z), size, FluidSimulation.BoundarySides.Top);
				DrawNeighbourSceneHandle(currentSim, simulationMap, pos - new Vector3(size.x, 0, 0), size, FluidSimulation.BoundarySides.Left);
				DrawNeighbourSceneHandle(currentSim, simulationMap, pos + new Vector3(size.x, 0, 0), size, FluidSimulation.BoundarySides.Right);
			}

			VisualizeInitialFluidHeight(targetSim);
		}

		private void DrawNeighbourSceneHandle(FluidSimulation parentSim, Dictionary<Vector2Int, FluidSimulation> fluidSimMap, Vector3 pos, Vector3 size, FluidSimulation.BoundarySides side)
		{
			Quaternion rot = Quaternion.Euler(90, 00, 0);
			rot.eulerAngles = new Vector3(90, 00, 0);

			FluidSimulation neighbourSim = parentSim.GetNeighbour(side);
			if ((neighbourSim == null) && Handles.Button(pos, rot, 0.5f * size.x, 0.5f * size.x, Handles.RectangleHandleCap))
			{
				GameObject newNeighbour = parentSim.AddNeighbour(fluidSimMap, side, pos).gameObject;
				Undo.RegisterCreatedObjectUndo(newNeighbour, "FluidSimNeighboursNewObject");
			}
		}
	}
}
#endif