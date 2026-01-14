using System;
using UnityEditor;
using UnityEngine;

namespace FluidFrenzy.Editor
{
#if UNITY_EDITOR

	[CustomEditor(typeof(LavaSurface))]
	public class LavaSurfaceEditor : FluidRendererEditor
	{
		class Styles
		{
			public static GUIContent generateHeatLutText = new GUIContent(
				"Generate Heat LUT",
				"If enabled, the heat gradient will be used to procedurally generate a **Heat LUT** that overrides the existing LUT on the FluidRenderer.fluidMaterial."
			);
			public static GUIContent heatText = new GUIContent(
				"Heat Gradient",
				"The Gradient used to define the heat/color transition for the lava. The color samples are mapped from Cold Lava (Left side of the gradient) to Hot Lava (Right side of the gradient)."
			);

		}

		SerializedProperty m_generateHeatLutProperty;
		SerializedProperty m_heatProperty;

		public override void OnEnable()
		{
			base.OnEnable();
			m_generateHeatLutProperty = serializedObject.FindProperty("generateHeatLut");
			m_heatProperty = serializedObject.FindProperty("heat");
		}

		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();
			EditorGUILayout.PropertyField(m_generateHeatLutProperty, Styles.generateHeatLutText);
			if(m_generateHeatLutProperty.boolValue)
				EditorGUILayout.PropertyField(m_heatProperty, Styles.heatText);
			serializedObject.ApplyModifiedProperties();
	
		}
	}
}
#endif