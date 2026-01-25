using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR
namespace FluidFrenzy.Editor
{
	[CustomEditor(typeof(FluidModifierWaves)), CanEditMultipleObjects]
	public class FluidModifierWavesEditor : UnityEditor.Editor
	{
		class Styles
		{
			public static GUIContent strengthText = new GUIContent(
				"Strength",
				"A global multiplier applied to the total force calculated from all wave octaves."
			);
			public static GUIContent wavesText = new GUIContent("Waves", "Set the amount and behaviour of the waves to be applied to the FluidSimulation");
			public static GUIContent noiseWavesText = new GUIContent("Noise Waves", "Generate waves based on a noise texture to break up repeating patterns.");

			public static GUIContent octaveCountText = new GUIContent(
				"Wave Count",
				@"The number of individual wave layers (octaves) to generate and stack.

Each octave is randomly generated based on the ranges defined below. Increasing this count adds more detail and complexity to the surface but increases the computational cost."
			);
			public static GUIContent waveLengthRangeText = new GUIContent(
				"Wavelength",
				@"Defines the minimum and maximum wavelength (physical size) for the generated octaves.


•  X (Min): The smallest allowed wavelength (tight ripples). 
•  Y (Max): The largest allowed wavelength (broad swells)."
			);
			public static GUIContent directionRangeText = new GUIContent(
				"Direction",
				@"Defines the angular range (in degrees) for the propagation direction of the waves.


•  X (Min): The minimum angle in degrees. 
•  Y (Max): The maximum angle in degrees. Use this to restrict waves to a specific wind direction or allow them to move chaotically in all directions."
			);
			public static GUIContent amplitudeRangeText = new GUIContent(
				"Amplitude",
				@"Defines the minimum and maximum height intensity for the generated octaves.


•  X (Min): The lowest possible amplitude for an octave. 
•  Y (Max): The highest possible amplitude for an octave."
			);
			public static GUIContent speedRangeText = new GUIContent(
				"Speed",
				@"Defines the minimum and maximum phase speed (travel speed) for the generated octaves.


•  X (Min): The slowest speed a wave can travel. 
•  Y (Max): The fastest speed a wave can travel."
			);
			public static GUIContent noiseAmplitudeText = new GUIContent(
				"Noise Amplitude",
				@"Controls the intensity of the secondary Perlin noise layer.

A noise layer is applied on top of the wave octaves to break up mathematical patterns and add organic irregularity to the surface. Higher values result in a more chaotic surface."
			);
		}


		protected SerializedProperty m_strengthProperty;
		protected SerializedProperty m_waveCountProperty;
		protected SerializedProperty m_waveLengthProperty;
		protected SerializedProperty m_directionProperty;
		protected SerializedProperty m_amplitudeProperty;
		protected SerializedProperty m_speedProperty;

		protected SerializedProperty m_noiseAmplitudeProperty;

		private void OnEnable()
		{
			m_strengthProperty = serializedObject.FindProperty("strength");
			m_waveCountProperty = serializedObject.FindProperty("octaveCount");
			m_waveLengthProperty = serializedObject.FindProperty("waveLengthRange");
			m_directionProperty = serializedObject.FindProperty("directionRange");
			m_amplitudeProperty = serializedObject.FindProperty("amplitudeRange");
			m_speedProperty = serializedObject.FindProperty("speedRange");

			m_noiseAmplitudeProperty = serializedObject.FindProperty("noiseAmplitude");
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			EditorGUI.BeginChangeCheck();

			EditorGUILayout.Slider(m_strengthProperty, 0, 5000, Styles.strengthText);

			if (EditorExtensions.DrawFoldoutHeader(m_waveCountProperty, Styles.wavesText))
			{ 
				EditorGUILayout.IntSlider(m_waveCountProperty, 1, 4, Styles.octaveCountText);
				EditorExtensions.MinMaxSlider(m_waveLengthProperty, 0, 20, Styles.waveLengthRangeText);
				EditorExtensions.MinMaxSlider(m_directionProperty, 0, 360, Styles.directionRangeText);
				EditorExtensions.MinMaxSlider(m_amplitudeProperty, 0, 1, Styles.amplitudeRangeText);
				EditorExtensions.MinMaxSlider(m_speedProperty, 0, 1, Styles.speedRangeText);
			}

			EditorGUILayout.Space(2);
			EditorExtensions.DrawHeader(Styles.noiseWavesText);
			EditorGUILayout.Slider(m_noiseAmplitudeProperty, 0, 10, Styles.noiseAmplitudeText);

			if (EditorGUI.EndChangeCheck())
			{
				serializedObject.ApplyModifiedProperties();
			}
		}
	}
}
#endif