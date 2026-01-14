using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR
namespace FluidFrenzy.Editor
{
	using ObstacleMode = FluidSimulationObstacle.ObstacleMode;
	using ObstacleShape = FluidSimulationObstacle.ObstacleShape;

	[CustomEditor(typeof(FluidSimulationObstacle)), CanEditMultipleObjects]
	public class FluidSimulationObstacleEditor : UnityEditor.Editor
	{
		protected class Styles
		{
			public static GUIContent modeText = new GUIContent(
				"Mode",
				"The method used to define the obstacle's shape for the heightmap render. Defaults to ObstacleMode.Renderer."
			);
			public static GUIContent shapeText = new GUIContent(
				"Shape",
				@"The type of procedural primitive to use when mode is ObstacleMode.Shape. 
• Sphere 
• Box 
• Cylinder 
• Capsule 
• Ellipsoid 
• CappedCone 
• HexPrism 
• Wedge"
			);
			public static GUIContent centerText = new GUIContent(
				"Center",
				"Local offset from the Transform position."
			);

			public static GUIContent sizeText = new GUIContent(
				"Size",
				@"The XYZ dimensions for non-uniform procedural shapes. 
• Box:: The full width, height, and depth. 
• Ellipsoid:: The diameter of the X, Y, and Z axes. 
• Wedge:: The bounding dimensions of the wedge base and height."
			);
			public static GUIContent radiusText = new GUIContent(
				"Radius",
				@"The primary radius for rounded procedural shapes. 
• Sphere:: The radius of the sphere. 
• Cylinder:: The radius of the base. 
• HexPrism:: The radius of the base. 
• Capsule:: The radius of the cylinder body and the hemispherical end-caps. 
• Capped Cone:: The radius of the bottom base."
			);
			public static GUIContent secondaryRadiusText = new GUIContent(
				"Second Radius",
				@"An secondary radius used for complex shapes. 
• Capped Cone:: The radius of the top cap."
			);
			public static GUIContent heightText = new GUIContent(
				"Height",
				"The total length or height of the procedural shape along its alignment direction."
			);
			public static GUIContent directionText = new GUIContent(
				"Direction",
				@"The local axis that the procedural shape's height or length is aligned with. 
• 0:: X-Axis (Horizontal) 
• 1:: Y-Axis (Vertical) 
• 2:: Z-Axis (Forward)"
			);

			public static GUIContent conservativeRasterizationText = new GUIContent(
				"Conservative Rasterization",
				@"Ensures that even sub-pixel geometry is captured during the heightfield bake.

Standard rasterization only renders a pixel if its center is covered by a triangle. Conservative Rasterization renders a pixel if any part of it is touched by a triangle. Enabling this prevents thin obstacles like thin walls from being missed if they happen to fall between pixel centers, ensuring more reliable collision data. This may cause the obstacle to appear slightly larger than its actual mesh. 

 Warning: This feature requires hardware-level support. It is not supported on platforms like WebGL, OpenGL ES, or older mobile devices. The system will automatically fall back to standard rasterization on unsupported hardware."
			);
			public static GUIContent smoothRasterizationText = new GUIContent(
				"Smooth Rasterization",
				@"Enables multi-sampling to produce smoother edges for procedural shapes.

When disabled, procedural shapes are sampled at a single point per grid cell, which can result in jagged edges or stair stepping in the heightfield. When enabled, the shader performs a multi-sample average to create a soft, anti-aliased edge. 

 Warning: Because this averages height values within a local neighborhood, perfectly vertical drops (like the sides of a box) might be turned into slopes. This can lead to height leakage or cause fluid to climb the edges of an obstacle instead of colliding with a sharp wall."
			);

			public static string[] directionOptions = new string[] { "X-Axis", "Y-Axis", "Z-Axis" };
		}

		SerializedProperty m_modeProperty;
		SerializedProperty m_shapeProperty;
		SerializedProperty m_conservativeRasterizationProperty;
		SerializedProperty m_smoothRasterizationProperty;

		SerializedProperty m_centerProperty;
		SerializedProperty m_sizeProperty;
		SerializedProperty m_radiusProperty;
		SerializedProperty m_secondaryRadiusProperty;
		SerializedProperty m_heightProperty;
		SerializedProperty m_directionProperty;

		void OnEnable()
		{
			m_modeProperty = serializedObject.FindProperty("mode");
			m_conservativeRasterizationProperty = serializedObject.FindProperty("conservativeRasterization");
			m_smoothRasterizationProperty = serializedObject.FindProperty("smoothRasterization");
			m_shapeProperty = serializedObject.FindProperty("shape");

			m_centerProperty = serializedObject.FindProperty("center");
			m_sizeProperty = serializedObject.FindProperty("size");
			m_radiusProperty = serializedObject.FindProperty("radius");
			m_secondaryRadiusProperty = serializedObject.FindProperty("secondaryRadius");
			m_heightProperty = serializedObject.FindProperty("height");
			m_directionProperty = serializedObject.FindProperty("direction");
		}

		public override void OnInspectorGUI()
		{
			EditorGUI.BeginChangeCheck();

			EditorGUILayout.PropertyField(m_modeProperty, Styles.modeText);

#if UNITY_2021_1_OR_NEWER
			ObstacleMode mode = (ObstacleMode)m_modeProperty.enumValueFlag;
#else
			ObstacleMode mode = (ObstacleMode)m_modeProperty.intValue;
#endif

			if (mode != ObstacleMode.Renderer)
			{
				EditorGUILayout.PropertyField(m_shapeProperty, Styles.shapeText);
				EditorGUILayout.PropertyField(m_centerProperty, Styles.centerText);

#if UNITY_2021_1_OR_NEWER
				ObstacleShape shape = (ObstacleShape)m_shapeProperty.enumValueFlag;
#else
				ObstacleShape shape = (ObstacleShape)m_shapeProperty.intValue;
#endif
				switch (shape)
				{
					case ObstacleShape.Sphere:
						EditorGUILayout.PropertyField(m_radiusProperty, Styles.radiusText);
						break;
					case ObstacleShape.Box:
					case ObstacleShape.Wedge:
					case ObstacleShape.Ellipsoid:
						EditorGUILayout.PropertyField(m_sizeProperty, Styles.sizeText);
						break;
					case ObstacleShape.Cylinder:
					case ObstacleShape.Capsule:
					case ObstacleShape.HexPrism:
						EditorGUILayout.PropertyField(m_radiusProperty, Styles.radiusText);
						EditorGUILayout.PropertyField(m_heightProperty, Styles.heightText);
						m_directionProperty.intValue = EditorGUILayout.Popup(Styles.directionText, m_directionProperty.intValue, Styles.directionOptions);
						break;
					case ObstacleShape.CappedCone:
						EditorGUILayout.PropertyField(m_radiusProperty, Styles.radiusText);
						EditorGUILayout.PropertyField(m_secondaryRadiusProperty, Styles.secondaryRadiusText);
						if (shape == ObstacleShape.CappedCone) EditorGUILayout.PropertyField(m_heightProperty, Styles.heightText);
						m_directionProperty.intValue = EditorGUILayout.Popup(Styles.directionText, m_directionProperty.intValue, Styles.directionOptions);
						break;
				}
				EditorGUILayout.PropertyField(m_smoothRasterizationProperty, Styles.smoothRasterizationText);
			}
			else
			{
				EditorGUILayout.PropertyField(m_conservativeRasterizationProperty, Styles.conservativeRasterizationText);
			}

			if (EditorGUI.EndChangeCheck())
			{
				(target as FluidSimulationObstacle).OnChanged();
				serializedObject.ApplyModifiedProperties();
			}
		}
	}
}
#endif