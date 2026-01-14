using System;
using UnityEditor;
using UnityEngine;

namespace FluidFrenzy.Editor
{
#if UNITY_EDITOR
	[CustomEditor(typeof(FluidRenderer))]
	public class FluidRendererEditor : UnityEditor.Editor
	{
		class Styles
		{
			public static GUIContent simulationText = new GUIContent(
				"Simulation",
				@"The FluidSimulation component that this renderer will draw.

This is a mandatory dependency. The FluidRenderer will automatically adopt the world-space dimensions and position of the assigned Fluid Simulation, ensuring the rendered fluid surface matches the simulated area exactly."
			);
			public static GUIContent fluidMaterialText = new GUIContent(
				"Material",
				@"The material to be used to render the fluid surface.

This material is internally instantiated at runtime. The component copies the properties from the original material to the new instance, and then overrides or injects any necessary rendering requirements (e.g., shader keywords or properties) for the fluid simulation effects to function correctly."
			);
			public static GUIContent flowMappingText = new GUIContent(
				"Flow Mapping",
				@"The FluidFlowMapping component that this FluidRenderer uses to visualize fluid currents and wakes.

This component provides the necessary data to the fluid shader, which can be either a dedicated flow map texture (for dynamic UV-offsetting) or material parameters derived directly from the simulation's velocity texture. This allows the fluid surface to depict accurate movement and flow."
			);
		}

		SerializedProperty m_surfaceProperties;
		SerializedProperty m_simulationProperty;
		SerializedProperty m_fluidMaterialProperty;
		SerializedProperty m_flowMappingProperty;

		public virtual void OnEnable()
		{
			m_surfaceProperties = serializedObject.FindProperty("surfaceProperties");
			m_simulationProperty = serializedObject.FindProperty("simulation");
			m_fluidMaterialProperty = serializedObject.FindProperty("fluidMaterial");
			m_flowMappingProperty = serializedObject.FindProperty("flowMapping");
		}

		public override void OnInspectorGUI()
		{
			EditorGUI.BeginChangeCheck();
			EditorGUILayout.PropertyField(m_surfaceProperties);
			bool changedRenderMode = EditorGUI.EndChangeCheck();

			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Simulation Settings", EditorStyles.boldLabel);
			EditorGUILayout.PropertyField(m_simulationProperty, Styles.simulationText);
			EditorGUILayout.Space();

			ISurfaceRenderer.RenderMode renderMode = (ISurfaceRenderer.RenderMode)(m_surfaceProperties.FindPropertyRelative("renderMode").enumValueIndex);

			EditorGUILayout.LabelField("Rendering Settings", EditorStyles.boldLabel);
			EditorGUILayout.PropertyField(m_fluidMaterialProperty, Styles.fluidMaterialText);
			if (renderMode != ISurfaceRenderer.RenderMode.HDRPWaterSurface)
			{
				EditorGUILayout.PropertyField(m_flowMappingProperty, Styles.flowMappingText);
			}
			serializedObject.ApplyModifiedProperties();

			if (changedRenderMode)
			{
				(target as FluidRenderer).OnRenderModeChanged();
			}			
		}
	}
}
#endif