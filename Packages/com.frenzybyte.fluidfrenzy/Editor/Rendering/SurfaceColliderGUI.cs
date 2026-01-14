using System.Linq;
using UnityEditor;
using UnityEngine;

namespace FluidFrenzy.Editor
{

	[CustomPropertyDrawer(typeof(SurfaceCollider.ColliderProperties))]
	public class SurfaceColliderPropertyDrawer : PropertyDrawer
	{
		class Styles
		{
			public static GUIContent createColliderText = new GUIContent(
				"Create Collider",
				"Toggles the generation of a TerrainCollider to handle physical interactions with the fluid surface."
			);
			public static GUIContent resolutionText = new GUIContent(
				"Resolution",
				@"Specifies the grid resolution of the generated TerrainCollider.

This value determines the density of the physics mesh. Higher resolutions result in more accurate physical interactions but increase generation time and physics processing overhead. 

 Internally, the actual grid size is set to resolution + 1 to satisfy heightmap requirements."
			);
			public static GUIContent realtimeText = new GUIContent(
				"Update Realtime",
				@"Controls whether the collider's heightmap is updated at runtime to match the visual fluid simulation.

When enabled, the simulation data is continuously synchronized with the physics collider. Note that this process requires reading GPU terrain data back to the CPU and applying it to the TerrainData, which can be resource-intensive and cause garbage collection spikes."
			);
			public static GUIContent updateFrequencyText = new GUIContent(
				"Frequency",
				@"The interval, in frames, between consecutive collider updates when realtime is enabled.

Increasing this value reduces the performance cost of the readback but causes the physics representation to lag behind the visual rendering."
			);
			public static GUIContent timeslicingText = new GUIContent(
				"Timeslice",
				@"The number of frames over which a single full collider update is distributed.

This feature splits the heightmap update into smaller segments, processing only a fraction of the data per frame. This helps to smooth out performance spikes and maintain a stable framerate, though it increases the time required for the collider to fully reflect a change in the fluid surface."
			);
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			// Calculate the height based on how many properties are drawn
			SerializedProperty createColliderProperty = property.FindPropertyRelative("createCollider");
			SerializedProperty resolutionProperty = property.FindPropertyRelative("resolution");
			SerializedProperty realtimeProperty = property.FindPropertyRelative("realtime");
			SerializedProperty updateFrequencyProperty = property.FindPropertyRelative("updateFrequency");
			SerializedProperty timeslicingProperty = property.FindPropertyRelative("timeslicing");

			float height = 0; //label
			
			height += (EditorGUI.GetPropertyHeight(createColliderProperty) + EditorGUIUtility.standardVerticalSpacing);
			if (createColliderProperty.isExpanded)
			{
				height += (EditorGUI.GetPropertyHeight(resolutionProperty) + EditorGUIUtility.standardVerticalSpacing);
				height += (EditorGUI.GetPropertyHeight(realtimeProperty) + EditorGUIUtility.standardVerticalSpacing);
				height += (EditorGUI.GetPropertyHeight(updateFrequencyProperty) + EditorGUIUtility.standardVerticalSpacing);
				height += (EditorGUI.GetPropertyHeight(timeslicingProperty) + EditorGUIUtility.standardVerticalSpacing);
			}
			return height;
		}

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.BeginProperty(position, label, property);

			// Getting the properties you want to draw
			SerializedProperty createColliderProperty = property.FindPropertyRelative("createCollider");
			SerializedProperty resolutionProperty = property.FindPropertyRelative("resolution");
			SerializedProperty realtimeProperty = property.FindPropertyRelative("realtime");
			SerializedProperty updateFrequencyProperty = property.FindPropertyRelative("updateFrequency");
			SerializedProperty timesilcingProperty = property.FindPropertyRelative("timeslicing");

			float lineHeight = EditorGUIUtility.singleLineHeight;
			float spacing = EditorGUIUtility.standardVerticalSpacing;

			// Draw each field manually
			Rect currentRect = position;
			currentRect.width = position.width;
			currentRect.height = lineHeight;

			if (EditorExtensions.DrawFoldoutHeaderToggle(createColliderProperty, Styles.createColliderText, ref currentRect))
			{
				using (new EditorGUI.DisabledGroupScope(!createColliderProperty.boolValue))
				{
					SurfaceColliderGUI.DrawResolutionField(resolutionProperty, Styles.resolutionText,  ref currentRect);
					EditorGUI.PropertyField(currentRect, realtimeProperty, Styles.realtimeText);
					currentRect.y += lineHeight + spacing;
					using (new EditorGUI.DisabledGroupScope(!realtimeProperty.boolValue))
					{
						EditorGUI.PropertyField(currentRect, updateFrequencyProperty, Styles.updateFrequencyText);
						currentRect.y += lineHeight + spacing;
						SurfaceColliderGUI.DrawReadbackOptionsField(timesilcingProperty, Styles.timeslicingText, ref currentRect);
					}
				}
			}


			EditorGUI.EndProperty();
		}
	}

	public static class SurfaceColliderGUI
	{
		private static readonly int[] meshResolutions = { 64, 128, 256, 512, 1024, 2048, 4096 };
		private static GUIContent[] meshResolutionName => meshResolutions.Select(x => new GUIContent(x.ToString())).ToArray();

		public static GUIContent[] readBackOptions => readBackValues.Select(x => new GUIContent(x.ToString())).ToArray();
		static int[] readBackValues = { 1, 2, 4, 8, 16, 32, 64 };

		public static void DrawResolutionField(SerializedProperty property, GUIContent label, ref Rect currentRect)
		{
			currentRect.height = EditorGUIUtility.singleLineHeight;
			property.intValue = EditorGUI.IntPopup(currentRect, label, property.intValue, meshResolutionName, meshResolutions);
			currentRect.y += currentRect.height + EditorGUIUtility.standardVerticalSpacing;
		}

		public static void DrawReadbackOptionsField(SerializedProperty property, GUIContent label, ref Rect currentRect)
		{
			currentRect.height = EditorGUIUtility.singleLineHeight;
			property.intValue = EditorGUI.IntPopup(currentRect, label, property.intValue, readBackOptions, readBackValues);
			currentRect.y += currentRect.height + EditorGUIUtility.standardVerticalSpacing;
		}
	}
}