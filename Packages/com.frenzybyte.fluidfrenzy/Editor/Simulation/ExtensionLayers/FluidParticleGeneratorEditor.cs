using UnityEditor;
using UnityEngine;

namespace FluidFrenzy.Editor
{
#if UNITY_EDITOR
	[CustomEditor(typeof(FluidParticleGenerator))]
	public class FluidParticleGeneratorEditor : UnityEditor.Editor
	{
		class Styles
		{
			public static GUIContent breakingWaveSplashesText = new GUIContent(
				"Breaking Wave Splashes",
				"Toggles the emission of splash particles from cresting or breaking waves."
			);
			public static GUIContent steepnessThresholdText = new GUIContent(
				"Steepness Threshold",
				@"The minimum surface angle (steepness) required to trigger a splash.

Higher values restrict splashes to only the sharpest peaks of the waves."
			);
			public static GUIContent riseRateThresholdText = new GUIContent(
				"Rise Rate Threshold",
				@"The minimum vertical (upward) velocity required to trigger a splash.

Used to identify waves that are rising rapidly before they break."
			);
			public static GUIContent waveLengthThresholdText = new GUIContent(
				"Wave Length Threshold",
				@"The minimum physical length a wave must have to emit particles.

Helps prevent small, high-frequency noise from generating excessive spray."
			);
			public static GUIContent breakingWaveGridStaggerText = new GUIContent(
				"Breaking Wave Grid Stagger",
				@"Optimization setting that spreads the sampling of grid cells for breaking waves across multiple frames.

A value of 2 means a specific cell is checked every 2nd frame. Increasing this value reduces the number of particles spawned and lowers performance cost, but may make emission look less responsive."
			);

			public static GUIContent turbulenceSplashesText = new GUIContent(
				"Turbulence Splashes",
				"Toggles the emission of spray particles from areas of high turbulence (diverging velocities)."
			);
			public static GUIContent turbulenceSplashGridStaggerText = new GUIContent(
				"Turbulence Splash Grid Stagger",
				"Optimization setting that spreads the sampling of grid cells for turbulence splashes across multiple frames."
			);
			public static GUIContent sprayTurbelenceThresholdText = new GUIContent(
				"Spray Turbulence Threshold",
				"The minimum turbulence value required to trigger a splash particle."
			);
			public static GUIContent splashParticleSystemText = new GUIContent(
				"Splash Particle System",
				"Configuration settings for the ballistic splash particles (movement, rendering, and limits)."
			);

			public static GUIContent turbulenceSurfaceText = new GUIContent(
				"Turbulence Surface",
				@"Toggles the emission of surface particles (foam) in turbulent areas.

Unlike splashes, these particles stick to the fluid surface and move with the flow."
			);
			public static GUIContent surfaceTurblenceThresholdText = new GUIContent(
				"Surface Turbulence Threshold",
				"The minimum turbulence value required to trigger a surface particle."
			);
			public static GUIContent surfaceGridStaggerText = new GUIContent(
				"Surface Grid Stagger",
				"Optimization setting that spreads the sampling of grid cells for surface particles across multiple frames."
			);
			public static GUIContent surfaceParticlesSystemText = new GUIContent(
				"Surface Particles System",
				"Configuration settings for the advected surface particles (movement, rendering, and limits)."
			);
			public static GUIContent renderOffscreenText = new GUIContent(
				"Render Offscreen",
				@"If enabled, surface particles are rendered to a dedicated offscreen texture buffer instead of the main camera.

This generated texture is globally available to shaders (e.g., as a foam mask) to create effects like white water trails without drawing individual particle geometry to the screen."
			);
		}

		private SerializedProperty m_breakingWaveSplashes;
		private SerializedProperty m_steepnessThreshold;
		private SerializedProperty m_riseRateThreshold;
		private SerializedProperty m_waveLengthThreshold;
		private SerializedProperty m_breakingWaveGridStagger;

		private SerializedProperty m_turbulenceSplashes;
		private SerializedProperty m_turbulenceSplashGridStagger;
		private SerializedProperty m_sprayTurbelenceThreshold;
		private SerializedProperty m_splashParticleSystem;

		private SerializedProperty m_turbulenceSurface;
		private SerializedProperty m_surfaceTurblenceThreshold;
		private SerializedProperty m_surfaceGridStagger;
		private SerializedProperty m_surfaceParticlesSystem;
		private SerializedProperty m_renderOffscreen;

		void OnEnable()
		{
			m_breakingWaveSplashes = serializedObject.FindProperty("breakingWaveSplashes");
			m_steepnessThreshold = serializedObject.FindProperty("steepnessThreshold");
			m_riseRateThreshold = serializedObject.FindProperty("riseRateThreshold");
			m_waveLengthThreshold = serializedObject.FindProperty("waveLengthThreshold");
			m_breakingWaveGridStagger = serializedObject.FindProperty("breakingWaveGridStagger");

			m_turbulenceSplashes = serializedObject.FindProperty("turbulenceSplashes");
			m_turbulenceSplashGridStagger = serializedObject.FindProperty("turbulenceSplashGridStagger");
			m_sprayTurbelenceThreshold = serializedObject.FindProperty("sprayTurbelenceThreshold");
			m_splashParticleSystem = serializedObject.FindProperty("splashParticleSystem");

			m_turbulenceSurface = serializedObject.FindProperty("turbulenceSurface");
			m_surfaceTurblenceThreshold = serializedObject.FindProperty("surfaceTurblenceThreshold");
			m_surfaceGridStagger = serializedObject.FindProperty("surfaceGridStagger");
			m_surfaceParticlesSystem = serializedObject.FindProperty("surfaceParticlesSystem");
			m_renderOffscreen = serializedObject.FindProperty("renderOffscreen");
		}


		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			DrawUnsupportedRenderMode();

			bool drawSplashparticleSystem = false;
			if (EditorExtensions.DrawFoldoutHeaderToggle(m_breakingWaveSplashes, Styles.breakingWaveSplashesText))
			{
				drawSplashparticleSystem |= true;
				EditorGUILayout.Slider(m_steepnessThreshold, 0, 1, Styles.steepnessThresholdText);
				EditorGUILayout.Slider(m_riseRateThreshold, 0, 8, Styles.riseRateThresholdText);
				EditorGUILayout.Slider(m_waveLengthThreshold, -4, 0, Styles.waveLengthThresholdText);
				EditorGUILayout.IntPopup(m_breakingWaveGridStagger, EditorExtensions.pot32Name, EditorExtensions.pot32, Styles.breakingWaveGridStaggerText);
			}
			GUILayout.Space(5);

			if (EditorExtensions.DrawFoldoutHeaderToggle(m_turbulenceSplashes, Styles.turbulenceSplashesText))
			{
				drawSplashparticleSystem |= true;
				EditorGUILayout.Slider(m_sprayTurbelenceThreshold, 0, 1, Styles.sprayTurbelenceThresholdText);
				EditorGUILayout.IntPopup(m_turbulenceSplashGridStagger, EditorExtensions.pot32Name, EditorExtensions.pot32, Styles.turbulenceSplashGridStaggerText);
			}

			if (drawSplashparticleSystem)
			{
				EditorGUILayout.PropertyField(m_splashParticleSystem, Styles.splashParticleSystemText);
				GUILayout.Space(5);
			}

			GUILayout.Space(5);

			if (EditorExtensions.DrawFoldoutHeaderToggle(m_turbulenceSurface, Styles.turbulenceSurfaceText))
			{
				EditorGUILayout.Slider(m_surfaceTurblenceThreshold, 0 , 1, Styles.surfaceTurblenceThresholdText);
				EditorGUILayout.IntPopup(m_surfaceGridStagger, EditorExtensions.pot32Name, EditorExtensions.pot32, Styles.surfaceGridStaggerText);
				EditorGUILayout.PropertyField(m_renderOffscreen, Styles.renderOffscreenText);
				EditorGUILayout.PropertyField(m_surfaceParticlesSystem, Styles.surfaceParticlesSystemText);
			}


			serializedObject.ApplyModifiedProperties();

		}

		public static bool IsGPUParticlesSupported()
		{
			return EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL;
		}

		internal static string SupportWarningMessage()
		{
			return $"GPU Particles are not supported due to {EditorUserBuildSettings.activeBuildTarget} not supporting compute shaders. ";
		}


		public static void DrawUnsupportedRenderMode()
		{
			if (!IsGPUParticlesSupported())
			{
				string message = SupportWarningMessage();
				float height = EditorExtensions.HelpBoxHeight(message);
				EditorGUILayout.HelpBox(message, MessageType.Warning);
			}
		}
	}
}
#endif