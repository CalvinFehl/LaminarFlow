using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR
namespace FluidFrenzy.Editor
{
	[CustomEditor(typeof(FluidFlowMappingSettings))]
	public class FluidFlowMappingLayerSettingsEditor : UnityEditor.Editor
	{
		protected class Styles
		{
			public static GUIContent flowMappingSettingsText = new GUIContent("Flow Mapping Settings", "Fluid Flow Mapping is an extension layer that enables and controls flow mapping functionality in the simulation and rendering side of Fluid Frenzy. The layer generates the *flow map* procedurally using the flow of the fluid simulation. The rendering data is automatically passed to the material assigned to the [Fluid Renderer](#fluid-rendering-components). There are several settings to control the visuals of the flow mapping in the layer which can be set in the Fluid Flow Mapping Settings asset assigned to this layer.");
			public static GUIContent flowMappingModeText = new GUIContent(
				"Mode",
				@"Defines the technique used to render the fluid's surface flow and texture advection.


•  Off: No flow mapping is applied. 
•  Static: Flow mapping is performed directly in the shader by offsetting UV coordinates based on the instantaneous velocity field. 
•  Dynamic: Utilizes a separate simulation buffer to calculate UV offsets. The UVs are advected over time, similar to the velocity field and foam mask. This allows for complex swirling effects but may accumulate distortion over longer periods."
			);
			public static GUIContent flowSpeedText = new GUIContent(
				"Flow Speed",
				@"A multiplier applied to the velocity vectors when calculating UV offsets.

Higher values create the appearance of faster-moving fluid but increase the visual distortion (stretching) of the surface texture."
			);
			public static GUIContent flowPhaseSpeedText = new GUIContent(
				"Phase Speed",
				@"Controls the frequency at which the flow map cycle resets to its original UV coordinates.

Continuous advection eventually distorts textures beyond recognition. To prevent this, the system resets the UVs periodically. 

 Increasing this value makes the reset occur more frequently, reducing maximum distortion. To hide the visual ""pop"" during a reset, the texture is sampled multiple times with offset phases and blended based on this cycle speed."
			);
		}

		SerializedProperty m_flowMappingModeProperty;
		SerializedProperty m_flowSpeedProperty;
		SerializedProperty m_flowPhaseSpeedProperty;

		void OnEnable()
		{

			m_flowMappingModeProperty = serializedObject.FindProperty("flowMappingMode");
			m_flowSpeedProperty = serializedObject.FindProperty("flowSpeed");
			m_flowPhaseSpeedProperty = serializedObject.FindProperty("flowPhaseSpeed");
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();
			EditorExtensions.DrawHeader(Styles.flowMappingSettingsText);
			EditorGUILayout.PropertyField(m_flowMappingModeProperty, Styles.flowMappingModeText);
			EditorGUILayout.Slider(m_flowSpeedProperty, 0, 15, Styles.flowSpeedText);
			EditorGUILayout.Slider(m_flowPhaseSpeedProperty, 0, 2, Styles.flowPhaseSpeedText);

			serializedObject.ApplyModifiedProperties();
		}
	}
}
#endif