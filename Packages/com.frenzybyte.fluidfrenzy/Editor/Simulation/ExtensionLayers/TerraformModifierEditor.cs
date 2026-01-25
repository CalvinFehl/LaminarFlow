using UnityEditor;
using UnityEngine;

namespace FluidFrenzy.Editor
{
	using static FluidFrenzy.TerraformModifier;

#if UNITY_EDITOR
	[CustomEditor(typeof(TerraformModifier)), CanEditMultipleObjects]
	public class TerraformModifierEditor : UnityEditor.Editor
	{
		private static class Styles
		{
			public static readonly GUIContent settingsLabel = new GUIContent(
				"Settings",
				"A collection of parameters that define the shape, size, and liquification/solidification behavior."
			);

			public static readonly GUIContent modeLabel = new GUIContent(
				"Mode",
				"Set the shape of the modification brush (Circle, Box, Sphere, Cube, Cylinder, Capsule)."
			);

			public static readonly GUIContent sizeLabel = new GUIContent(
				"Size",
				"Adjust the dimensions of the modification area in world units. Interpretation varies by mode (e.g., Circle uses X for radius, Box uses X/Z for width/depth)."
			);

			public static readonly GUIContent falloffLabel = new GUIContent(
				"Falloff",
				"Adjust the sharpness of the brush edge. Higher values create a softer edge."
			);

			// Liquify Settings
			public static readonly GUIContent liquifyHeaderLabel = new GUIContent(
				"Liquify: Terrain to Fluid",
				"Enable and configure the process of dissolving terrain into fluid."
				);
			public static readonly GUIContent liquifyTerrainLayerLabel = new GUIContent(
				"Source Terrain Layer",
				"Set the terrain layer (e.g. Layer 1, Layer 2) that will be dissolved."
			);
			public static readonly GUIContent liquifyFluidLayerLabel = new GUIContent(
				"Target Fluid Layer",
				"Set the fluid layer (e.g., Layer 1, Layer 2) that this terrain will dissolve into."
			);
			public static readonly GUIContent liquifyRateLabel = new GUIContent(
				"Liquify Rate",
				"Set the speed at which the terrain dissolves into fluid, in units of height per second. Higher values mean faster melting or dissolving.."
			);
			public static readonly GUIContent liquifyAmountLabel = new GUIContent(
				"Terrain to Fluid Ratio",
				"Set the conversion ratio of terrain height to fluid depth. A value of 1 means 1 unit of terrain height becomes 1 unit of fluid depth. A value of 2 means 1 unit of terrain becomes 2 units of fluid."
			);

			// Solidify Settings
			public static readonly GUIContent solidifyHeaderLabel = new GUIContent(
				"Solidify: Fluid to Terrain",
				"Enable and configure the process of solidifying fluid into terrain."
				);
			public static readonly GUIContent solidifyTerrainLayerLabel = new GUIContent(
				"Target Terrain Layer",
				"Set the terrain layer (e.g. Layer 1, Layer 2) that will be built up."
			);
			public static readonly GUIContent solidifySplatChannelLabel = new GUIContent(
				"Target Splat Channel",
				"Set the splat channel (e.g., R, G, B, A) that will be used to paint the built-up terrain."
			);
			public static readonly GUIContent solidifyFluidLayerLabel = new GUIContent(
				"Source Fluid Layer",
				"Set the fluid layer (e.g., Layer 1, Layer 2) that will be consumed to create terrain."
			);
			public static readonly GUIContent solidifyRateLabel = new GUIContent(
				"Solidify Rate",
				"Set the speed at which the fluid solidifies into terrain, in units of height per second. Higher values mean faster build-up of terrain."
			);
			public static readonly GUIContent solidifyAmountLabel = new GUIContent(
				"Fluid to Terrain Ratio",
				"Set the conversion ratio of fluid depth to terrain height. A value of 1 means 1 unit of fluid depth becomes 1 unit of terrain height. A value of 2 means 2 units of fluid become 1 unit of terrain."
			);
		}

		private SerializedProperty m_settingsProperty;
		private SerializedProperty m_modeProperty;
		private SerializedProperty m_sizeProperty;
		private SerializedProperty m_falloffProperty;

		// Liquify Properties
		private SerializedProperty m_liquifyProperty;
		private SerializedProperty m_liquifyTerrainLayerProperty;
		private SerializedProperty m_liquifyFluidLayerProperty;
		private SerializedProperty m_liquifyRateProperty;
		private SerializedProperty m_liquifyAmountProperty;

		// Solidify Properties
		private SerializedProperty m_solidifyProperty;
		private SerializedProperty m_solidifyTerrainLayerProperty;
		private SerializedProperty m_solidifySplatChannelProperty; // Note the typo solidfySplatChannel for consistency
		private SerializedProperty m_solidifyFluidLayerProperty;
		private SerializedProperty m_solidifyRateProperty;
		private SerializedProperty m_solidifyAmountProperty;

		void OnEnable()
		{
			m_settingsProperty = serializedObject.FindProperty("settings");
			m_modeProperty = m_settingsProperty.FindPropertyRelative("mode");
			m_sizeProperty = m_settingsProperty.FindPropertyRelative("size");
			m_falloffProperty = m_settingsProperty.FindPropertyRelative("falloff");

			// Liquify
			m_liquifyProperty = m_settingsProperty.FindPropertyRelative("liquify");
			m_liquifyTerrainLayerProperty = m_settingsProperty.FindPropertyRelative("liquifyTerrainLayer");
			m_liquifyFluidLayerProperty = m_settingsProperty.FindPropertyRelative("liquifyFluidLayer");
			m_liquifyRateProperty = m_settingsProperty.FindPropertyRelative("liquifyRate");
			m_liquifyAmountProperty = m_settingsProperty.FindPropertyRelative("liquifyAmount");

			// Solidify
			m_solidifyProperty = m_settingsProperty.FindPropertyRelative("solidify");
			m_solidifyTerrainLayerProperty = m_settingsProperty.FindPropertyRelative("solidifyTerrainLayer");
			m_solidifySplatChannelProperty = m_settingsProperty.FindPropertyRelative("solidifySplatChannel");
			m_solidifyFluidLayerProperty = m_settingsProperty.FindPropertyRelative("solidifyFluidLayer");
			m_solidifyRateProperty = m_settingsProperty.FindPropertyRelative("solidifyRate");
			m_solidifyAmountProperty = m_settingsProperty.FindPropertyRelative("solidifyAmount");
		}


		public override void OnInspectorGUI()
		{
			EditorGUI.BeginChangeCheck();

			// Main Settings Foldout
			if (m_settingsProperty.isExpanded = EditorExtensions.DrawFoldoutHeader(m_settingsProperty, Styles.settingsLabel))
			{

				EditorGUILayout.PropertyField(m_modeProperty, Styles.modeLabel);
				EditorGUILayout.PropertyField(m_sizeProperty, Styles.sizeLabel);

#if UNITY_2021_1_OR_NEWER
				TerraformInputMode mode = (TerraformInputMode)m_modeProperty.enumValueFlag;
#else
				TerraformInputMode mode = (TerraformInputMode)m_modeProperty.intValue;
#endif

				EditorGUILayout.PropertyField(m_falloffProperty, Styles.falloffLabel);

				EditorGUILayout.Space(2);

				if (EditorExtensions.DrawFoldoutHeaderToggle(m_liquifyProperty, Styles.liquifyHeaderLabel))
				{
					using (new EditorGUI.IndentLevelScope())
					using (new EditorGUI.DisabledGroupScope(!m_liquifyProperty.boolValue))
					{
						EditorGUILayout.PropertyField(m_liquifyTerrainLayerProperty, Styles.liquifyTerrainLayerLabel);
						EditorGUILayout.PropertyField(m_liquifyFluidLayerProperty, Styles.liquifyFluidLayerLabel);
						EditorGUILayout.PropertyField(m_liquifyRateProperty, Styles.liquifyRateLabel);
						EditorGUILayout.PropertyField(m_liquifyAmountProperty, Styles.liquifyAmountLabel);
					}
				}

				EditorGUILayout.Space(2);

				if (EditorExtensions.DrawFoldoutHeaderToggle(m_solidifyProperty, Styles.solidifyHeaderLabel))
				{
					using (new EditorGUI.IndentLevelScope())
					using (new EditorGUI.DisabledGroupScope(!m_solidifyProperty.boolValue))
					{
						EditorGUILayout.PropertyField(m_solidifyTerrainLayerProperty, Styles.solidifyTerrainLayerLabel);
						EditorGUILayout.PropertyField(m_solidifySplatChannelProperty, Styles.solidifySplatChannelLabel);
						EditorGUILayout.PropertyField(m_solidifyFluidLayerProperty, Styles.solidifyFluidLayerLabel);
						EditorGUILayout.PropertyField(m_solidifyRateProperty, Styles.solidifyRateLabel);
						EditorGUILayout.PropertyField(m_solidifyAmountProperty, Styles.solidifyAmountLabel);
					}
				}
			}
			EditorGUILayout.EndFoldoutHeaderGroup(); // Closes the custom foldout header

			if (EditorGUI.EndChangeCheck())
			{
				serializedObject.ApplyModifiedProperties();
			}
		}
	}
}
#endif
