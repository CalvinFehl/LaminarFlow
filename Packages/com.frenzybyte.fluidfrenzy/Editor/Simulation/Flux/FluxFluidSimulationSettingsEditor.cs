using UnityEditor;
using UnityEngine;

namespace FluidFrenzy.Editor
{
#if UNITY_EDITOR
	[CustomEditor(typeof(FluxFluidSimulationSettings))]
	public class FluxFluidSimulationSettingsEditor : FluidSimulationSettingsEditor
	{
		protected class FluxStyles
		{
			public static GUIContent velocityMapSettingsLabel = new GUIContent(
				"Velocity Map Settings",
				"Settings that control the generation of a velocity field."
			);

			public static GUIContent velocityTextureSizeLabel = new GUIContent(
				"Velocity Texture Size",
				@"Controls the resolution (width and height) of the internal Velocity Field texture.

This texture stores the flow direction and magnitude used for advection. The resolution is often lower than the main fluid grid to save memory and processing time."
			);

			public static GUIContent paddingScaleLabel = new GUIContent(
				"Padding Percentage",
				@"A percentage of padding added to the borders of the velocity flow map.

This padding is specifically designed for use in tiled fluid simulations (currently in **beta**) to ensure smooth flow continuity between adjacent tiles. It should typically be set to 0 if tiling is not used."
			);

			public static GUIContent velocityScaleLabel = new GUIContent(
				"Velocity Scale",
				@"The factor by which the newly generated fluid velocity (outflow) is applied to the final velocity map texture.

This value controls the responsiveness and maximum speed of the flow. A higher scale means the fluid accelerates faster, resulting in more pronounced and quickly-appearing flow patterns."
			);

			public static GUIContent advectionScaleLabel = new GUIContent(
				"Advection Scale",
				@"Scales the distance the velocity field advects (carries) itself and other data maps like the FoamLayer.

A larger value causes the flow patterns, foam, and dynamic flow mapping data to be carried further by the fluid movement each frame, effectively increasing the visual influence of the velocity field."
			);

			public static GUIContent additiveVelocityLabel = new GUIContent(
				"Additive Velocity",
				@"Controls whether the newly calculated velocity for the current frame is added to or overwrites the previous frame's velocity.


•  Enabled (Additive): The current frame's velocity is accumulated onto the existing velocity map. This is essential for simulating persistent effects like continuous flow, pressure buildup, and rotational momentum (swirls/eddies). 
•  Disabled (Overwrite): The velocity map is reset each frame to only contain the velocity calculated from the fluid's movement during that single frame. This typically results in a less continuous, more reactive flow."
			);

			public static GUIContent velocityDampingLabel = new GUIContent(
				"Velocity Damping",
				@"Scales down the accumulated velocity of the fluid each frame to slow down movement when no new acceleration is applied.

This damping acts as a friction or viscosity factor. Higher values dampen the velocity faster, causing the fluid to come to rest more quickly."
			);

			public static GUIContent pressureLabel = new GUIContent(
				"Pressure",
				@"Scales the perceived incompressibility of the fluid's velocity field when solving for pressure.

A higher value forces the fluid to ""push out"" more aggressively to neighboring cells. Tweaking this value significantly influences the size and intensity of swirls/eddies and the pressure buildup around obstacles."
			);

			// Second Layer Flux Specifics
			public static GUIContent secondLayerVelocityScaleLabel = new GUIContent(
				"Velocity Scale",
				@"Secondary Layer: The factor by which the newly generated fluid velocity is applied to the final velocity map texture.

This value controls the responsiveness and maximum speed of the flow. A higher scale means the fluid accelerates faster, resulting in more pronounced and quickly-appearing flow patterns."
			);

			public static GUIContent secondLayerAdditiveVelocityLabel = new GUIContent(
				"Additive Velocity",
				@"Secondary Layer: Controls whether the newly calculated velocity is added to or overwrites the previous frame's velocity.


•  Enabled (Additive): The current frame's velocity is accumulated onto the existing velocity map. This is essential for simulating persistent effects like continuous flow, pressure buildup, and rotational momentum (swirls/eddies). 
•  Disabled (Overwrite): The velocity map is reset each frame to only contain the velocity calculated from the fluid's movement during that single frame. This typically results in a less continuous, more reactive flow."
			);

			public static GUIContent secondLayerCustomViscosityLabel = new GUIContent(
				"Use Custom Viscosity",
				@"Enables a custom viscosity control model for the second fluid layer.

When enabled, this feature allows the second fluid to flow more slowly on shallow slopes and stack up to a certain height before flowing, which is useful for simulating highly viscous fluids like lava."
			);

			public static GUIContent secondLayerViscosityLabel = new GUIContent(
				"Viscosity",
				@"Scales the flow speed of the second layer when secondLayerCustomViscosity is enabled.

This factor determines the fluid's viscosity. The fluid volume leaves the cell at a slower rate than its calculated velocity, simulating thicker fluid. A higher value results in more viscous, slower flow."
			);

			public static GUIContent secondLayerFlowHeightLabel = new GUIContent(
				"Flow Height",
				@"Indicates the minimum height (thickness) the second layer fluid must achieve before it begins to flow significantly on flat or near-flat surfaces.

This simulates the non-Newtonian behavior of highly viscous fluids like lava, where an initial minimum head height is required to overcome internal friction before flow commences."
			);
		}

		SerializedProperty m_velocityTextureSizeProperty;
		SerializedProperty m_paddingScaleProperty;
		SerializedProperty m_velocityMaxProperty;
		SerializedProperty m_velocityScaleProperty;
		SerializedProperty m_advectionScaleProperty;
		SerializedProperty m_additiveVelocityProperty;
		SerializedProperty m_velocityDampingProperty;
		SerializedProperty m_pressureProperty;

		SerializedProperty m_secondLayerVelocityScaleProperty;
		SerializedProperty m_secondLayerAdditiveVelocityProperty;
		SerializedProperty m_secondLayerCustomViscosityProperty;
		SerializedProperty m_secondLayerViscosityProperty;
		SerializedProperty m_secondLayerFlowHeightProperty;


		protected override void OnEnable()
		{
			base.OnEnable();

			m_velocityTextureSizeProperty = serializedObject.FindProperty("velocityTextureSize");
			m_paddingScaleProperty = serializedObject.FindProperty("paddingScale");
			m_velocityMaxProperty = serializedObject.FindProperty("velocityMax");
			m_velocityScaleProperty = serializedObject.FindProperty("velocityScale");
			m_advectionScaleProperty = serializedObject.FindProperty("advectionScale");
			m_additiveVelocityProperty = serializedObject.FindProperty("additiveVelocity");
			m_velocityDampingProperty = serializedObject.FindProperty("velocityDamping");
			m_pressureProperty = serializedObject.FindProperty("pressure");

			m_secondLayerVelocityScaleProperty = serializedObject.FindProperty("secondLayerVelocityScale");
			m_secondLayerAdditiveVelocityProperty = serializedObject.FindProperty("secondLayerAdditiveVelocity");
			m_secondLayerCustomViscosityProperty = serializedObject.FindProperty("secondLayerCustomViscosity");
			m_secondLayerViscosityProperty = serializedObject.FindProperty("secondLayerViscosity");
			m_secondLayerFlowHeightProperty = serializedObject.FindProperty("secondLayerFlowHeight");
		}


		public override void OnInspectorGUI()
		{
			FluxFluidSimulationSettings settings = target as FluxFluidSimulationSettings;
			EditorGUI.BeginChangeCheck();
			serializedObject.UpdateIfRequiredOrScript();

			DrawUpdateMode(settings);

			GUILayout.Space(5);
			DrawWaveSimulation(settings);

			GUILayout.Space(5);
			DrawRendering(settings);

			GUILayout.Space(5);
			DrawReadback(settings);

			GUILayout.Space(5);
			DrawVelocity(settings);

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

		private void DrawVelocity(FluxFluidSimulationSettings settings)
		{
			if (EditorExtensions.DrawFoldoutHeader(m_velocityTextureSizeProperty, FluxStyles.velocityMapSettingsLabel))
			{
				using (new EditorGUI.IndentLevelScope())
				{
					EditorGUILayout.PropertyField(m_velocityTextureSizeProperty, FluxStyles.velocityTextureSizeLabel);
					m_velocityTextureSizeProperty.vector2IntValue = Vector2Int.Max(FluidSimulation.kMinBufferSize, m_velocityTextureSizeProperty.vector2IntValue);
					m_velocityTextureSizeProperty.vector2IntValue = Vector2Int.Min(FluidSimulation.kMaxBufferSize, m_velocityTextureSizeProperty.vector2IntValue);
					EditorGUILayout.Slider(m_paddingScaleProperty, 0, 50, FluxStyles.paddingScaleLabel);
					m_paddingScaleProperty.floatValue = (Mathf.ClosestPowerOfTwo(Mathf.RoundToInt((m_paddingScaleProperty.floatValue / 100.0f * 32.0f))) / 32.0f) * 100;

					EditorGUILayout.Slider(m_velocityMaxProperty, 0, 10, Styles.velocityMaxLabel); // Shared label
					EditorGUILayout.Slider(m_velocityScaleProperty, 0, 5, FluxStyles.velocityScaleLabel);
					EditorGUILayout.Slider(m_advectionScaleProperty, 0, 15.0f, FluxStyles.advectionScaleLabel);
					EditorGUILayout.PropertyField(m_additiveVelocityProperty, FluxStyles.additiveVelocityLabel);

					GUI.enabled = m_additiveVelocityProperty.boolValue;
					EditorGUILayout.Slider(m_velocityDampingProperty, 0, 1.0f, FluxStyles.velocityDampingLabel);
					EditorGUILayout.Slider(m_pressureProperty, 0, 1.0f, FluxStyles.pressureLabel);
					GUI.enabled = true;
				}
			}
		}

		protected void DrawSecondLayer(FluxFluidSimulationSettings settings)
		{
			if (EditorExtensions.DrawFoldoutHeaderToggle(m_secondLayerProperty, Styles.secondLayerLabel))
			{
				using (new EditorGUI.IndentLevelScope())
				{
					EditorGUILayout.Slider(m_secondLayerCellSizeProperty, 0.01f, 1, Styles.secondLayerCellSizeLabel);
					EditorGUILayout.Slider(m_secondLayerWaveDampingProperty, 0, 5, Styles.secondLayerWaveDampingLabel);
					EditorGUILayout.Slider(m_secondLayerAccelerationProperty, 0, 30, Styles.secondLayerAccelerationLabel);

					EditorGUILayout.Slider(m_secondLayerLinearEvaporationProperty, 0, 1, Styles.secondLayerLinearEvaporationLabel);
					EditorGUILayout.Slider(m_secondLayerProportionalEvaporationProperty, 0, 1, Styles.secondLayerProportionalEvaporationLabel);

					EditorGUILayout.Slider(m_secondLayerVelocityScaleProperty, 0, 5, FluxStyles.secondLayerVelocityScaleLabel);

					EditorGUILayout.PropertyField(m_secondLayerAdditiveVelocityProperty, FluxStyles.secondLayerAdditiveVelocityLabel);
					EditorGUILayout.PropertyField(m_secondLayerCustomViscosityProperty, FluxStyles.secondLayerCustomViscosityLabel);

					GUI.enabled = m_secondLayerCustomViscosityProperty.boolValue;
					EditorGUILayout.Slider(m_secondLayerViscosityProperty, 0, 1, FluxStyles.secondLayerViscosityLabel);
					EditorGUILayout.Slider(m_secondLayerFlowHeightProperty, 0, 1, FluxStyles.secondLayerFlowHeightLabel);
					GUI.enabled = true;
				}
			}
		}
	}
#endif
}