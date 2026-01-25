using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR
namespace FluidFrenzy.Editor
{
	[CustomEditor(typeof(FluidRigidBodyLite)), CanEditMultipleObjects]
	public class FluidRigidBodyLiteEditor : UnityEditor.Editor
	{
		class Styles
		{
			public static GUIContent createWavesText = new GUIContent(
				"Create Waves",
				"If enabled, the object will interact with the fluid simulation by creating waves in its direction of movement (e.g., wakes)."
			);
			public static GUIContent waveRadiusText = new GUIContent(
				"Radius",
				"Adjusts the radius (size) of the generated wave/wake on the fluid surface."
			);
			public static GUIContent waveStrengthText = new GUIContent(
				"Force",
				"Adjusts the height (amplitude) or intensity of the wave."
			);
			public static GUIContent waveExponentText = new GUIContent(
				"Falloff",
				"Adjusts the falloff curve of the wave's strength. Higher values mean a faster falloff, which can be used to create sharper or flatter wave/vortex shapes."
			);
			
			public static GUIContent createSplashesText = new GUIContent(
				"Create Splashes",
				"If enabled, the object will interact with the fluid simulation by generating splashes when falling into the fluid."
			);
			public static GUIContent splashForceText = new GUIContent(
				"Radius",
				"Adjusts the force applied to the fluid simulation when the object lands in the fluid. Faster falling objects create bigger splashes."
			);
			public static GUIContent splashRadiusText = new GUIContent(
				"Force",
				"Adjusts the size of the splash area on the fluid surface."
			);
			public static GUIContent splashTimeText = new GUIContent(
				"Time",
				"Adjusts the time duration over which the splash force is applied while surface contact is made."
			);
			public static GUIContent splashParticlesText = new GUIContent(
				"Particles",
				"A list of SplashParticleSystem settings that will be spawned when the rigid body makes contact with the fluid."
			);
			
			public static GUIContent advectionSpeedText = new GUIContent(
				"Advection speed",
				"Adjusts the influence the FluidSimulation velocity field (current) has on the object. Higher values will move the object through the fluid at faster speeds."
			);
			public static GUIContent dragText = new GUIContent(
				"Drag",
				"Adjusts the amount of linear drag applied to the object when it is in contact with the fluid."
			);
			public static GUIContent angularDragText = new GUIContent(
				"Angular drag",
				"Adjusts the amount of angular (rotational) drag applied to the object when it is in contact with the fluid."
			);
			public static GUIContent buoyancyText = new GUIContent(
				"Buoyancy",
				"Adjusts the buoyancy of the object. Higher values increase the upward force, causing the object to float higher. Lower values make the object float lower or sink."
			);
		}

		SerializedProperty m_createWavesProperty;
		SerializedProperty m_waveRadiusProperty;
		SerializedProperty m_waveStrengthProperty;
		SerializedProperty m_waveExponentProperty;
		
		SerializedProperty m_createSplashesProperty;
		SerializedProperty m_splashForceProperty;
		SerializedProperty m_splashRadiusProperty;
		SerializedProperty m_splashTimeProperty;
		SerializedProperty m_splashParticlesProperty;

		SerializedProperty m_advectionSpeedProperty;
		SerializedProperty m_dragProperty;
		SerializedProperty m_angularDragProperty;
		SerializedProperty m_buoyancyProperty;

		void OnEnable()
		{
			m_createWavesProperty = serializedObject.FindProperty("createWaves");
			m_waveRadiusProperty = serializedObject.FindProperty("waveRadius");
			m_waveStrengthProperty = serializedObject.FindProperty("waveStrength");
			m_waveExponentProperty = serializedObject.FindProperty("waveExponent");

			m_createSplashesProperty = serializedObject.FindProperty("createSplashes");
			m_splashForceProperty = serializedObject.FindProperty("splashForce");
			m_splashRadiusProperty = serializedObject.FindProperty("splashRadius");
			m_splashTimeProperty = serializedObject.FindProperty("splashTime");
			m_splashParticlesProperty = serializedObject.FindProperty("splashParticles");

			m_advectionSpeedProperty = serializedObject.FindProperty("advectionSpeed");
			m_dragProperty = serializedObject.FindProperty("drag");
			m_angularDragProperty = serializedObject.FindProperty("angularDrag");
			m_buoyancyProperty = serializedObject.FindProperty("buoyancy");
		}

		private void OnDisable()
		{
		}

		public override void OnInspectorGUI()
		{
			if (EditorExtensions.DrawFoldoutHeaderToggle(m_createWavesProperty, Styles.createWavesText))
			{
				EditorGUILayout.PropertyField(m_waveRadiusProperty, Styles.waveRadiusText);
				EditorGUILayout.PropertyField(m_waveStrengthProperty, Styles.waveStrengthText);
				EditorGUILayout.PropertyField(m_waveExponentProperty, Styles.waveExponentText);
			}
			EditorGUILayout.Space();

			if (EditorExtensions.DrawFoldoutHeaderToggle(m_createSplashesProperty, Styles.createSplashesText))
			{
				EditorGUILayout.PropertyField(m_splashForceProperty, Styles.splashForceText);
				EditorGUILayout.PropertyField(m_splashRadiusProperty, Styles.splashRadiusText);
				EditorGUILayout.PropertyField(m_splashTimeProperty, Styles.splashTimeText);
				EditorGUILayout.PropertyField(m_splashParticlesProperty, Styles.splashParticlesText);
			}
			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Physics", EditorStyles.boldLabel);
			EditorGUILayout.PropertyField(m_advectionSpeedProperty, Styles.advectionSpeedText);
			EditorGUILayout.PropertyField(m_dragProperty, Styles.dragText);
			EditorGUILayout.PropertyField(m_angularDragProperty, Styles.angularDragText);
			EditorGUILayout.PropertyField(m_buoyancyProperty, Styles.buoyancyText);

			serializedObject.ApplyModifiedProperties();
		}
	}
}
#endif