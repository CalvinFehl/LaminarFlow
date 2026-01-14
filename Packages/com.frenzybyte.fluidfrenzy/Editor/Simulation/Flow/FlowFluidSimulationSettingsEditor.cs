using UnityEditor;
using UnityEngine;
using static FluidFrenzy.Editor.EditorExtensions;

namespace FluidFrenzy.Editor
{
#if UNITY_EDITOR
	[CustomEditor(typeof(FlowFluidSimulationSettings))]
	public class FlowFluidSimulationSettingsEditor : FluidSimulationSettingsEditor
	{
		protected class FlowStyles
		{
			public static GUIContent accelerationMaxLabel = new GUIContent(
				"Max Acceleration",
				@"Clamps the magnitude of the fluid's acceleration to a maximum value per frame.

Limiting acceleration is a stability measure, as it prevents sudden, large forces from being applied to the fluid. This helps control the rate at which fluid speed changes and improves the overall stability of the simulation."
			);

			public static GUIContent overshootingReductionLabel = new GUIContent(
				"Overshooting Reduction",
				@"Enables a technique to mitigate the amplification of wave heights that can occur when waves transition from deep to shallow water.

This feature prevents ""spiking"" artifacts from appearing at the edges of waves, particularly in areas of rapid depth change, by applying a necessary correction factor."
			);

			public static GUIContent overshootingEdgeLabel = new GUIContent(
				"Overshooting Edge",
				@"A threshold that determines the sensitivity for detecting a significant change in wave height (a ""wave edge"") as the fluid transitions into shallow areas.

A lower value makes the reduction system more sensitive, applying the correction to smaller wave changes."
			);

			public static GUIContent overshootingScaleLabel = new GUIContent(
				"Overshooting Scale",
				@"A scaling factor that adjusts the magnitude of the correction applied to reduce overshooting at detected wave edges.

This controls how aggressively the ""spiking"" artifacts are dampened. A higher value results in a stronger smoothing effect."
			);

			// Second Layer Flow Specifics
			public static GUIContent secondLayerAccelerationMaxLabel = new GUIContent(
				"Max Acceleration",
				@"Secondary Layer: Clamps the magnitude of the second fluid layer's acceleration to a maximum value per frame.

Limiting acceleration is a stability measure, as it prevents sudden, large forces from being applied to the fluid. This helps control the rate at which fluid speed changes and improves the overall stability of the simulation."
			);

			public static GUIContent secondLayerVelocityMaxLabel = new GUIContent(
				"Max Velocity",
				@"Secondary Layer: Clamps the magnitude of the second fluid layer's velocity vector to a maximum value.

This prevents the fluid from accelerating past a defined maximum speed, which helps maintain numerical stability and controls the intensity of the flow."
			);
		}

		SerializedProperty m_accelerationMaxProperty;
		SerializedProperty m_velocityMaxProperty;

		SerializedProperty m_overshootingReductionProperty;
		SerializedProperty m_overshootingEdgeProperty;
		SerializedProperty m_overshootingScaleProperty;

		SerializedProperty m_secondLayerAccelerationMax;
		SerializedProperty m_secondLayerVelocityMax;

		protected override void OnEnable()
		{
			base.OnEnable();

			m_accelerationMaxProperty = serializedObject.FindProperty("accelerationMax");
			m_velocityMaxProperty = serializedObject.FindProperty("velocityMax");

			m_overshootingReductionProperty = serializedObject.FindProperty("overshootingReduction");
			m_overshootingEdgeProperty = serializedObject.FindProperty("overshootingEdge");
			m_overshootingScaleProperty = serializedObject.FindProperty("overshootingScale");

			m_secondLayerAccelerationMax = serializedObject.FindProperty("secondLayerAccelerationMax");
			m_secondLayerVelocityMax = serializedObject.FindProperty("secondLayerVelocityMax");
		}


		public override void OnInspectorGUI()
		{
			FlowFluidSimulationSettings settings = target as FlowFluidSimulationSettings;
			EditorGUI.BeginChangeCheck();
			serializedObject.UpdateIfRequiredOrScript();
			DrawUpdateMode(settings);

			GUILayout.Space(5);
			DrawWaveSimulation(settings);

			GUILayout.Space(5);
			DrawOvershootingReduction(settings);

			GUILayout.Space(5);
			DrawRendering(settings);

			GUILayout.Space(5);
			DrawReadback(settings);

			GUILayout.Space(5);
			DrawEvaporation(settings);

			GUILayout.Space(5);
			DrawSecondLayer(settings);

			if (EditorGUI.EndChangeCheck())
			{
				serializedObject.ApplyModifiedProperties();
				FluidSimulationManager.MarkSettingsChanged(true);
			}
		}

		protected virtual void DrawWaveSimulation(FlowFluidSimulationSettings settings)
		{
			if (DrawFoldoutHeader(m_numberOfCellsProperty, Styles.waveSimulationSettingsLabel))
			{
				using (new EditorGUI.IndentLevelScope())
				{
					EditorGUILayout.PropertyField(m_numberOfCellsProperty, Styles.numberOfCellsLabel);
					m_numberOfCellsProperty.vector2IntValue = Vector2Int.Max(FluidSimulation.kMinBufferSize, m_numberOfCellsProperty.vector2IntValue);
					m_numberOfCellsProperty.vector2IntValue = Vector2Int.Min(FluidSimulation.kMaxBufferSize, m_numberOfCellsProperty.vector2IntValue);
					EditorGUILayout.Slider(m_cellSizeProperty, 0.1f, 5, Styles.cellSizeLabel);
					EditorGUILayout.Slider(m_waveDampingProperty, 0, 5, Styles.waveDampingLabel);
					EditorGUILayout.Slider(m_accelerationProperty, 0, 30, Styles.accelerationLabel);
					EditorGUILayout.Slider(m_accelerationMaxProperty, 0, 10, FlowStyles.accelerationMaxLabel);
					EditorGUILayout.Slider(m_velocityMaxProperty, 0, 10, Styles.velocityMaxLabel); // Shared
					EditorGUILayout.PropertyField(m_openBordersProperty, Styles.openBordersLabel);
				}
			}
		}

		protected virtual void DrawOvershootingReduction(FlowFluidSimulationSettings settings)
		{
			if (DrawFoldoutHeaderToggle(m_overshootingReductionProperty, FlowStyles.overshootingReductionLabel))
			{
				using (new EditorGUI.IndentLevelScope())
				{
					EditorGUILayout.Slider(m_overshootingEdgeProperty, 1.0f, 5, FlowStyles.overshootingEdgeLabel);
					EditorGUILayout.Slider(m_overshootingScaleProperty, 0.1f, 0.5f, FlowStyles.overshootingScaleLabel);
				}
			}
		}

		protected void DrawSecondLayer(FlowFluidSimulationSettings settings)
		{
			if (DrawFoldoutHeaderToggle(m_secondLayerProperty, Styles.secondLayerLabel))
			{
				using (new EditorGUI.IndentLevelScope())
				{
					EditorGUILayout.Slider(m_secondLayerCellSizeProperty, 0.01f, 1, Styles.secondLayerCellSizeLabel);
					EditorGUILayout.Slider(m_secondLayerWaveDampingProperty, 0, 5, Styles.secondLayerWaveDampingLabel);
					EditorGUILayout.Slider(m_secondLayerAccelerationProperty, 0, 30, Styles.secondLayerAccelerationLabel);
					EditorGUILayout.Slider(m_secondLayerAccelerationMax, 0, 10, FlowStyles.secondLayerAccelerationMaxLabel);
					EditorGUILayout.Slider(m_secondLayerVelocityMax, 0, 10, FlowStyles.secondLayerVelocityMaxLabel);

					EditorGUILayout.Slider(m_secondLayerLinearEvaporationProperty, 0, 1, Styles.secondLayerLinearEvaporationLabel);
					EditorGUILayout.Slider(m_secondLayerProportionalEvaporationProperty, 0, 1, Styles.secondLayerProportionalEvaporationLabel);
				}
			}
		}
	}
#endif
}