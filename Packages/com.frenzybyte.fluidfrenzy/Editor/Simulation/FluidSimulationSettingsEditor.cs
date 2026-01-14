using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FluidFrenzy.Editor
{
#if UNITY_EDITOR
	public class FluidSimulationSettingsEditor : UnityEditor.Editor
	{
		/// <summary>
		/// Shared styles for all Fluid Simulation Editors.
		/// </summary>
		public static class Styles
		{
			// Headers
			public static GUIContent waveSimulationSettingsLabel = new GUIContent(
				"Wave Simulation Settings",
				"Settings that control the quality and behaviour of the waves in the fluid simulation."
			);

			public static GUIContent updateModeLabel = new GUIContent(
				"Update Mode",
				"Settings that control how the simulation should be updated,"
			);

			public static GUIContent renderingSettingsLabel = new GUIContent(
				"Rendering Settings",
				"Settings that control anything related to rendering,"
			);

			public static GUIContent evaporationSettingsLabel = new GUIContent(
				"Evaporation Settings",
				"Settings that control the evaporation of the fluid."
			);

			public static GUIContent secondLayerLabel = new GUIContent(
				"Second Layer",
				@"Enables an optional secondary layer for simulating a different type of fluid.

The secondary layer runs concurrently with the main fluid layer, increasing VRAM usage and slightly decreasing performance. However, this is generally more efficient than running a separate FluidSimulation component. The second layer is used for features like **Lava** in the Terraform simulation option. The following properties provide independent physics overrides for this layer."
			);

			// General Settings
			public static GUIContent runInFixedUpdateLabel = new GUIContent(
				"Run in FixedUpdate",
				@"Choose between simulating in *Update* or *Fixed Update*. 
It is recommended to run in *Fixed Update* for better stability, as the delta time will not fluctuate and the simulation will be in sync with Unity's physics. 
If your game is using a very high or low 'Time.fixedDeltaTime' value, it is advised to simulate in Update. 
The simulation will always run at a fixed timestep of 60hz to maintain stability, even if your framerate is higher or lower. 
This means that if you have a higher framerate, eventually there will be a frame where the simulation does not have to run. 
Conversely, with a lower framerate, the simulation will need to run multiple times per frame to catch up. This is because 2.5D Fluid simulations tend to become unstable with high timesteps."
			);

			public static GUIContent numberOfCellsLabel = new GUIContent(
				"Number of Cells",
				@"Controls the resolution (width and height) of the simulation's 2D grid.

It is highly recommended to use power-of-two dimensions (e.g., 512x512 or 1024x1024) for optimal GPU performance. A higher resolution increases the spatial accuracy of the fluid simulation but linearly increases both GPU memory usage and processing cost (frame time)."
			);

			public static GUIContent cellSizeLabel = new GUIContent(
				"Cell Size Scale",
				@"Adjusts the internal scale factor of the fluid volume within each cell to control the effective flow speed.

A smaller `cellSize` implies less fluid volume per cell, which results in faster-flowing fluid and more energetic wave behavior for a given acceleration."
			);

			public static GUIContent waveDampingLabel = new GUIContent(
				"Wave Damping",
				@"Adjusts the rate at which wave energy is dissipated (dampened) over time.

A higher value causes waves and ripples to fade away quickly, leading to a calmer surface, while a lower value allows waves to persist longer."
			);

			public static GUIContent accelerationLabel = new GUIContent(
				"Acceleration",
				@"The force of gravity or acceleration applied to the fluid, which directly controls the speed of wave propagation.

This value simulates the effect of gravity (9.8 m/s² is typical) on the fluid and is the primary factor determining how quickly waves travel across the simulation domain."
			);

			public static GUIContent openBordersLabel = new GUIContent(
				"Open Borders",
				@"Determines whether fluid is allowed to leave the simulation domain at the boundaries.

When disabled, the boundaries act as solid walls, causing fluid to reflect and accumulate over time. When enabled, fluid passing over the border is removed, which maintains fluid consistency but causes a net loss of volume."
			);

			public static GUIContent velocityMaxLabel = new GUIContent(
				"Max Velocity",
				"Clamps the velocity of the fluid to a maximum value. Limiting the velocity helps create different fluid behavior and improves simulation stability by slowing down the fluid."
			);

			// Rendering
			public static GUIContent clipHeightLabel = new GUIContent(
				"Clip Height",
				@"A minimum fluid height threshold below which a cell is considered to have no fluid.

This value is primarily used to prevent minor visual artifacts or ""clipping"" issues that can arise from floating-point imprecision when a cell's fluid height is extremely close to zero. Any cell below this height is treated as empty."
			);

			// Evaporation
			public static GUIContent linearEvaporationLabel = new GUIContent(
				"Linear Evaporation",
				@"The rate of constant (linear) fluid volume removal from every cell in the simulation.

This simulates a constant, external water loss like pumping. The fluid volume is reduced uniformly at this rate: `fluid -= linearEvaporation * dt`."
			);

			public static GUIContent proportionalEvaporationLabel = new GUIContent(
				"Proportional Evaporation",
				@"The rate of fluid volume removal proportional to the amount of fluid currently in the cell.

This simulates natural evaporation, where the rate is dependent on surface area/volume. More fluid results in a higher removal rate: `fluid -= fluid * proportionalEvaporation * dt`."
			);

			// Readback
			public static GUIContent readBackHeightLabel = new GUIContent(
				"Readback Height & Velocity",
				@"Enables asynchronous readback of the simulation's height and velocity data from the GPU to the CPU.

This is necessary for CPU-side interactions, such as buoyancy, floating objects, or gameplay logic that requires current fluid data. Since the readback is asynchronous to prevent performance stalls, the CPU data will lag behind the GPU simulation by a few frames."
			);

			public static GUIContent readBackTimeSliceFramesLabel = new GUIContent(
				"Timeslicing",
				@"The number of frames over which the CPU readback of the simulation data is sliced to spread the performance cost.

Time slicing divides the data transfer into smaller chunks over multiple frames. A higher value reduces the cost per frame but increases the total latency before the full simulation data is available on the CPU. The readback processes the simulation data vertically (from top to bottom)."
			);

			public static GUIContent distanceFieldReadbackLabel = new GUIContent(
				"Readback Distance Field",
				@"Enables the asynchronous generation and readback of a distance field representing the nearest fluid location.

The distance field is generated on the GPU using the Jump Flood algorithm and then transferred to the CPU. This data provides the distance to the nearest fluid cell, which is useful for advanced gameplay logic or visual effects. Due to the asynchronous nature of the readback, the CPU data will lag behind the GPU simulation."
			);

			public static GUIContent distanceFieldDownsampleLabel = new GUIContent(
				"Downsample",
				@"The downsampling factor applied to the distance field's resolution.

Increasing this value improves performance by reducing the GPU generation and CPU transfer time, but it decreases the spatial accuracy of the distance field. A value of 0 means no downsampling."
			);

			public static GUIContent distanceFieldIterationsLabel = new GUIContent(
				"Iterations",
				@"The number of internal steps the Jump Flood algorithm performs to generate the distance field.

Lowering this number increases performance but reduces the accuracy, particularly for larger distances within the field. Higher resolution distance fields generally require more iterations for full accuracy."
			);

			// Second Layer (Shared)
			public static GUIContent secondLayerCellSizeLabel = new GUIContent(
				"Cell size",
				@"Secondary Layer: Adjusts the internal scale factor of the fluid volume to control the effective flow speed.

A smaller `cellSize` implies less fluid volume per cell, which results in faster-flowing fluid and more energetic wave behavior for a given acceleration."
			);

			public static GUIContent secondLayerWaveDampingLabel = new GUIContent(
				"Wave Damping",
				@"Secondary Layer: Adjusts the rate at which wave energy is dissipated over time.

A higher value causes waves and ripples to fade away quickly, leading to a calmer surface, while a lower value allows waves to persist longer."
			);

			public static GUIContent secondLayerAccelerationLabel = new GUIContent(
				"Acceleration",
				@"Secondary Layer: The force of acceleration applied to the fluid, which directly controls the speed of wave propagation.

This value simulates the effect of gravity (9.8 m/s² is typical) on the fluid and is the primary factor determining how quickly waves travel across the simulation domain."
			);

			public static GUIContent secondLayerLinearEvaporationLabel = new GUIContent(
				"Linear Evaporation",
				@"Secondary Layer: The rate of constant (linear) fluid volume removal.

Fluid volume is reduced uniformly at this rate: `fluid -= secondLayerLinearEvaporation * dt`."
			);

			public static GUIContent secondLayerProportionalEvaporationLabel = new GUIContent(
				"Proportional Evaporation",
				@"Secondary Layer: The rate of fluid volume removal proportional to the amount of fluid currently in the cell.

More fluid results in a higher removal rate: `fluid -= fluid * secondLayerProportionalEvaporation * dt`."
			);
		}

		private readonly float kMinClipHeight = 0.000001f;
		static GUIContent[] readBackOptions = new GUIContent[] { new GUIContent("1"), new GUIContent("2"), new GUIContent("4"), new GUIContent("8") };
		static int[] readBackValues = { 1, 2, 4, 8 };
		public static readonly int[] simulationCellCounts = { 32, 64, 128, 256, 512, 1024, 2048, 4096 };
		public static GUIContent[] simulationCellCountsName => simulationCellCounts.Select(x => new GUIContent(x.ToString())).ToArray();

		protected SerializedProperty m_runInFixedUpdateProperty;

		protected SerializedProperty m_numberOfCellsProperty;
		protected SerializedProperty m_cellSizeProperty;
		protected SerializedProperty m_waveDampingProperty;
		protected SerializedProperty m_accelerationProperty;
		protected SerializedProperty m_openBordersProperty;

		protected SerializedProperty m_linearEvaporationProperty;
		protected SerializedProperty m_proportionalEvaporationProperty;

		protected SerializedProperty m_clipHeightProperty;

		protected SerializedProperty m_readBackHeightProperty;
		protected SerializedProperty m_readBackTimeSliceFramesProperty;

		protected SerializedProperty m_distanceFieldReadback;
		protected SerializedProperty m_distanceFieldDownsample;
		protected SerializedProperty m_distanceFieldIterations;
		protected SerializedProperty m_distanceFieldTimeSliceFrames;

		protected SerializedProperty m_secondLayerProperty;
		protected SerializedProperty m_secondLayerCellSizeProperty;
		protected SerializedProperty m_secondLayerWaveDampingProperty;
		protected SerializedProperty m_secondLayerAccelerationProperty;

		protected SerializedProperty m_secondLayerLinearEvaporationProperty;
		protected SerializedProperty m_secondLayerProportionalEvaporationProperty;

		protected virtual void OnEnable()
		{
			Undo.undoRedoPerformed += OnUndo;

			//m_runInFixedUpdateProperty						= serializedObject.FindProperty("runFixedUpdate");
			m_numberOfCellsProperty = serializedObject.FindProperty("numberOfCells");
			m_cellSizeProperty = serializedObject.FindProperty("cellSize");
			m_waveDampingProperty = serializedObject.FindProperty("waveDamping");
			m_accelerationProperty = serializedObject.FindProperty("acceleration");
			m_openBordersProperty = serializedObject.FindProperty("openBorders");
			m_clipHeightProperty = serializedObject.FindProperty("clipHeight");
			m_linearEvaporationProperty = serializedObject.FindProperty("linearEvaporation");
			m_proportionalEvaporationProperty = serializedObject.FindProperty("proportionalEvaporation");

			m_readBackHeightProperty = serializedObject.FindProperty("readBackHeight");
			m_readBackTimeSliceFramesProperty = serializedObject.FindProperty("readBackTimeSliceFrames");

			m_distanceFieldReadback = serializedObject.FindProperty("distanceFieldReadback");
			m_distanceFieldDownsample = serializedObject.FindProperty("distanceFieldDownsample");
			m_distanceFieldIterations = serializedObject.FindProperty("distanceFieldIterations");
			m_distanceFieldTimeSliceFrames = serializedObject.FindProperty("distanceFieldTimeSliceFrames");

			m_secondLayerProperty = serializedObject.FindProperty("secondLayer");
			m_secondLayerCellSizeProperty = serializedObject.FindProperty("secondLayerCellSize");
			m_secondLayerWaveDampingProperty = serializedObject.FindProperty("secondLayerWaveDamping");
			m_secondLayerAccelerationProperty = serializedObject.FindProperty("secondLayerAcceleration");
			m_secondLayerLinearEvaporationProperty = serializedObject.FindProperty("secondLayerLinearEvaporation");
			m_secondLayerProportionalEvaporationProperty = serializedObject.FindProperty("secondLayerProportionalEvaporation");
		}

		private void OnDisable()
		{
			Undo.undoRedoPerformed -= OnUndo;
		}

		void OnUndo()
		{
			FluidSimulationManager.MarkSettingsChanged(true);
		}

		protected virtual void DrawUpdateMode(FluidSimulationSettings settings)
		{
			/*if (EditorExtensions.DrawFoldoutHeader(m_runInFixedUpdateProperty, Styles.UpdateModeLabel))
			{
				using (new EditorGUI.IndentLevelScope())
				{
					EditorGUILayout.PropertyField(m_runInFixedUpdateProperty, Styles.RunInFixedUpdateLabel);
				}
			}*/
		}

		protected virtual void DrawWaveSimulation(FluidSimulationSettings settings)
		{
			if (EditorExtensions.DrawFoldoutHeader(m_numberOfCellsProperty, Styles.waveSimulationSettingsLabel))
			{
				using (new EditorGUI.IndentLevelScope())
				{
					EditorGUILayout.PropertyField(m_numberOfCellsProperty, Styles.numberOfCellsLabel);
					m_numberOfCellsProperty.vector2IntValue = Vector2Int.Max(FluidSimulation.kMinBufferSize, m_numberOfCellsProperty.vector2IntValue);
					m_numberOfCellsProperty.vector2IntValue = Vector2Int.Min(FluidSimulation.kMaxBufferSize, m_numberOfCellsProperty.vector2IntValue);
					m_cellSizeProperty.floatValue *= 10;
					EditorGUILayout.Slider(m_cellSizeProperty, 0.1f, 5, Styles.cellSizeLabel);
					m_cellSizeProperty.floatValue /= 10;
					EditorGUILayout.Slider(m_waveDampingProperty, 0, 5, Styles.waveDampingLabel);
					EditorGUILayout.Slider(m_accelerationProperty, 0, 30, Styles.accelerationLabel);
					EditorGUILayout.PropertyField(m_openBordersProperty, Styles.openBordersLabel);
				}
			}
		}

		protected virtual void DrawEvaporation(FluidSimulationSettings settings)
		{
			if (EditorExtensions.DrawFoldoutHeader(m_linearEvaporationProperty, Styles.evaporationSettingsLabel))
			{
				using (new EditorGUI.IndentLevelScope())
				{
					EditorGUILayout.Slider(m_linearEvaporationProperty, 0, 1, Styles.linearEvaporationLabel);
					EditorGUILayout.Slider(m_proportionalEvaporationProperty, 0, 1, Styles.proportionalEvaporationLabel);
				}
			}
		}

		protected virtual void DrawRendering(FluidSimulationSettings settings)
		{
			if (EditorExtensions.DrawFoldoutHeader(m_clipHeightProperty, Styles.renderingSettingsLabel))
			{
				using (new EditorGUI.IndentLevelScope())
				{
					EditorGUILayout.Slider(m_clipHeightProperty, kMinClipHeight, 0.01f, Styles.clipHeightLabel);
				}
			}
		}

		protected virtual void DrawReadback(FluidSimulationSettings settings)
		{
			if (EditorExtensions.DrawFoldoutHeaderToggle(m_readBackHeightProperty, Styles.readBackHeightLabel))
			{
				using (new EditorGUI.IndentLevelScope())
				{
					EditorGUILayout.IntPopup(m_readBackTimeSliceFramesProperty, readBackOptions, readBackValues, Styles.readBackTimeSliceFramesLabel);
				}
			}


			if (EditorExtensions.DrawFoldoutHeaderToggle(m_distanceFieldReadback, Styles.distanceFieldReadbackLabel))
			{
				using (new EditorGUI.IndentLevelScope())
				{
					int downsamples = m_distanceFieldDownsample.intValue + 1;
					int maxCells = Mathf.Max(settings.numberOfCells.x / downsamples, settings.numberOfCells.y / downsamples);
					int maxIterations = Mathf.CeilToInt(Mathf.Log(maxCells + 1, 2.0f));
					m_distanceFieldIterations.intValue = Mathf.Min(m_distanceFieldIterations.intValue, maxIterations);
					EditorGUILayout.IntSlider(m_distanceFieldDownsample, 0, 4, Styles.distanceFieldDownsampleLabel);
					EditorGUILayout.IntSlider(m_distanceFieldIterations, 1, maxIterations, Styles.distanceFieldIterationsLabel);
					EditorGUILayout.IntPopup(m_distanceFieldTimeSliceFrames, readBackOptions, readBackValues, Styles.readBackTimeSliceFramesLabel);
				}
			}
		}

		public delegate void initializeCallback<T>(T settings);

		public static T CreateFluidSimulationSettings<T>(Scene scene, string targetName, initializeCallback<T> init = null) where T : ScriptableObject
		{
			var path = FluidSimulationMenu.GetAssetPath(scene, "_FluidSimulationSettings");
			path += targetName + "_settings.asset";
			path = AssetDatabase.GenerateUniqueAssetPath(path);

			T settings = ScriptableObject.CreateInstance<T>();
			init?.Invoke(settings);
			AssetDatabase.CreateAsset(settings, path);
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
			return settings;
		}
	}
#endif
}