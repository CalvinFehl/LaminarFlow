using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace FluidFrenzy.Editor
{
    using SurfaceRenderMode = ISurfaceRenderer.RenderMode;

	[CustomPropertyDrawer(typeof(ISurfaceRenderer.RenderProperties))]
	public class SurfaceRendererPropertyDrawer : PropertyDrawer
	{
		class Styles
		{
			public static GUIContent meshRenderingText = new GUIContent("Mesh Rendering", "Rendering settings to determined the quality and performance of the rendering of the surface mesh.");
			public static GUIContent renderModeText = new GUIContent(
				"Render Mode",
				@"The method used for generating and rendering the fluid surface geometry.


•  MeshRenderer: Uses standard GameObjects with MeshRenderer components. Best for simple setups where standard object culling is sufficient. 
•  DrawMesh: Uses RenderMesh to avoid GameObject overhead. Supports GPU Instancing. 
•  GPULOD: Draws the surface using a GPU-accelerated LOD system. Best for large-scale oceans or lakes. 
•  HDRPWaterSurface: Bridges the simulation data to a Unity HDRPWaterSurface component (Requires HDRP)."
			);
			public static GUIContent dimensionText = new GUIContent(
				"Dimension",
				"The total world-space size (X and Z) of the rendered surface."
			);
			public static GUIContent meshResolutionText = new GUIContent(
				"Mesh Resolution",
				@"The vertex resolution of the surface's base grid mesh.

For the most accurate visualization, it is recommended to match this value to the source heightmap resolution."
			);
			public static GUIContent meshBlocksText = new GUIContent(
				"Mesh Blocks",
				@"The number of subdivisions (blocks) to split the rendering mesh into along the X and Z axes.

Subdividing the mesh improves GPU performance by allowing the camera to cull blocks that are outside the view frustum."
			);
			public static GUIContent lodResolutionText = new GUIContent(
				"LOD Resolution",
				"The vertex resolution of individual LOD patches when using RenderMode.GPULOD."
			);
			public static GUIContent traverseIterationsText = new GUIContent(
				"Traverse Iterations",
				@"The number of iterations the Quadtree traversal algorithm performs per frame when using RenderMode.GPULOD.

Higher values resolve the surface quality faster during camera movement but may reduce performance."
			);
			public static GUIContent hdrpWaterSurfaceText = new GUIContent(
				"HDRP WaterSurface",
				"Configuration settings for bridging this simulation's data to an external HDRPWaterSurface."
			);
			public static GUIContent lodMinMaxText = new GUIContent(
				"LOD Levels",
				"The range of allowable LOD levels, where X is the minimum level and Y is the maximum level."
			);
		}

		private bool ShouldRenderDimension(SerializedProperty property)
		{
			return !(property.serializedObject.targetObject is FluidRenderer);
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			// Calculate the height based on how many properties are drawn
			SerializedProperty renderModeProperty = property.FindPropertyRelative("renderMode");
			SerializedProperty dimensionProperty = property.FindPropertyRelative("dimension");
			SerializedProperty traverseIterationsProperty = property.FindPropertyRelative("traverseIterations");

			float height = EditorGUIUtility.standardVerticalSpacing + EditorGUIUtility.singleLineHeight; //label

			if (!renderModeProperty.isExpanded)
				return height;
			
			height += (EditorGUI.GetPropertyHeight(renderModeProperty) + EditorGUIUtility.standardVerticalSpacing);
			if(ShouldRenderDimension(property))
				height += (EditorGUI.GetPropertyHeight(dimensionProperty) + EditorGUIUtility.standardVerticalSpacing);

			if (renderModeProperty.enumValueIndex == (int)SurfaceRenderMode.GPULOD)
			{
				height += 20; //(EditorGUI.GetPropertyHeight(lodResolutionProperty) + EditorGUIUtility.standardVerticalSpacing);
				height += (EditorGUI.GetPropertyHeight(traverseIterationsProperty) + EditorGUIUtility.standardVerticalSpacing);
				height += 20;// (EditorGUI.GetPropertyHeight(lodMinMaxProperty) + EditorGUIUtility.standardVerticalSpacing);
			}
			else if (renderModeProperty.enumValueIndex == (int)SurfaceRenderMode.HDRPWaterSurface)
			{
				SerializedProperty hdrpWaterSurfaceProperty = property.FindPropertyRelative("hdrpWaterSurface");
				height += (EditorGUI.GetPropertyHeight(hdrpWaterSurfaceProperty, true) + EditorGUIUtility.standardVerticalSpacing);
			}
			else
			{
				height += 20; //(EditorGUI.GetPropertyHeight(meshResolutionProperty) + EditorGUIUtility.standardVerticalSpacing);
				height += 20; //(EditorGUI.GetPropertyHeight(meshBlocksProperty) + EditorGUIUtility.standardVerticalSpacing);
			}

			if (!SurfaceRendererGUI.IsRenderModeSupported((SurfaceRenderMode)renderModeProperty.enumValueIndex, out SurfaceRenderMode supportedMode))
			{
				string message = SurfaceRendererGUI.RenderModeWarningMessage((SurfaceRenderMode)renderModeProperty.enumValueIndex, supportedMode);
				height += EditorExtensions.HelpBoxHeight(message) + EditorGUIUtility.standardVerticalSpacing;

			}

			return height;
		}

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.BeginProperty(position, label, property);

			// Getting the properties you want to draw
			SerializedProperty renderModeProperty = property.FindPropertyRelative("renderMode");
			SerializedProperty dimensionProperty = property.FindPropertyRelative("dimension");
			SerializedProperty meshResolutionProperty = property.FindPropertyRelative("meshResolution");
			SerializedProperty meshBlocksProperty = property.FindPropertyRelative("meshBlocks");
			SerializedProperty lodResolutionProperty = property.FindPropertyRelative("lodResolution");
			SerializedProperty traverseIterationsProperty = property.FindPropertyRelative("traverseIterations");
			SerializedProperty lodMinMaxProperty = property.FindPropertyRelative("lodMinMax");
			SerializedProperty hdrpWaterSurfaceProperty = property.FindPropertyRelative("hdrpWaterSurface");

			float lineHeight = EditorGUIUtility.singleLineHeight;
			float spacing = EditorGUIUtility.standardVerticalSpacing;

			// Draw each field manually
			Rect currentRect = position;
			currentRect.width = position.width;
			currentRect.height = lineHeight;

			if (EditorExtensions.DrawFoldoutHeader(renderModeProperty, Styles.meshRenderingText, ref currentRect))
			{
				SurfaceRenderMode renderMode = (SurfaceRenderMode)renderModeProperty.enumValueIndex;
				renderMode = (SurfaceRenderMode)EditorGUI.EnumPopup(currentRect, Styles.renderModeText, renderMode, (Enum e) =>
				{
					if ((SurfaceRenderMode)(e) == SurfaceRenderMode.HDRPWaterSurface)
						return property.serializedObject.targetObject is FluidRenderer;
					return true;
				});
				if (renderModeProperty.enumValueIndex != (int)renderMode)
				{
					renderModeProperty.enumValueIndex = (int)renderMode;
					renderModeProperty.serializedObject.ApplyModifiedProperties();
				}

				currentRect.y += lineHeight + spacing;

				SurfaceRendererGUI.DrawUnsupportedRenderMode(renderMode, ref currentRect);
				if (renderMode == SurfaceRenderMode.GPULOD)
				{
					lodMinMaxProperty.vector2IntValue = EditorExtensions.MinMaxSliderInt(currentRect, Styles.lodMinMaxText, lodMinMaxProperty.vector2IntValue, 0, 15, 7, 0);
					currentRect.y += lineHeight + spacing;
					SurfaceRendererGUI.DrawLODResolutionField(lodResolutionProperty, Styles.lodResolutionText, ref currentRect);
					EditorGUI.PropertyField(currentRect, traverseIterationsProperty, Styles.traverseIterationsText);
					currentRect.y += lineHeight + spacing;

				}
				else if (renderMode == SurfaceRenderMode.HDRPWaterSurface)
				{
					EditorGUI.PropertyField(currentRect, hdrpWaterSurfaceProperty, Styles.hdrpWaterSurfaceText, true);
				}
				else
				{
					SurfaceRendererGUI.DrawMeshResolutionField(meshResolutionProperty, Styles.meshResolutionText, ref currentRect);
					SurfaceRendererGUI.DrawMeshBlocksField(meshBlocksProperty, Styles.meshBlocksText, ref currentRect);
				}

				if (ShouldRenderDimension(property))
				{
					EditorGUI.PropertyField(currentRect, dimensionProperty, Styles.dimensionText);
				}
			}

			EditorGUI.EndProperty();
		}
	}

	public static class SurfaceRendererGUI
	{
		private static readonly int[] meshResolutions = { 64, 128, 256, 512, 1024, 2048, 4096 };
		private static GUIContent[] meshResolutionName => meshResolutions.Select(x => new GUIContent(x.ToString())).ToArray();

		private static readonly int[] lodResolutions = { 4, 8, 16, 32, 64, 128 };
		private static GUIContent[] lodResolutionsName => lodResolutions.Select(x => new GUIContent(x.ToString())).ToArray();

		private static readonly int[] meshBlocks = { 1, 2, 4, 8, 16 };
		private static GUIContent[] meshBlocksName => meshBlocks.Select(x => new GUIContent(x.ToString())).ToArray();

		public static bool IsRenderModeSupported(in SurfaceRenderMode mode, out SurfaceRenderMode supportedMode)
		{
			if (mode == SurfaceRenderMode.GPULOD)
			{
				if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.WebGL)
				{
					supportedMode = SurfaceRenderMode.MeshRenderer;
					return false;
				}
			}
			supportedMode = mode;
			return true;
		}

		internal static string RenderModeWarningMessage(SurfaceRenderMode renderMode, SurfaceRenderMode supportedMode)
		{
			return $"Render Mode {renderMode} Is not supported on {EditorUserBuildSettings.activeBuildTarget} build will fall back to {supportedMode}.";
		}


		public static void DrawUnsupportedRenderMode(SurfaceRenderMode renderMode, ref Rect currentRect)
		{
			if (!IsRenderModeSupported(renderMode, out SurfaceRenderMode supportedMode))
			{
				string message = RenderModeWarningMessage(renderMode, supportedMode);
				float height = EditorExtensions.HelpBoxHeight(message);
				EditorGUI.HelpBox(new Rect(currentRect.x, currentRect.y, currentRect.width, height), message, MessageType.Warning);
				currentRect.y += height + EditorGUIUtility.standardVerticalSpacing;
			}
		}

		public static void DrawMeshResolutionField(SerializedProperty property, GUIContent label, ref Rect currentRect)
		{
			currentRect.height = EditorGUIUtility.singleLineHeight;
			EditorExtensions.WidthHeightDropdown(currentRect, property, meshResolutionName, meshResolutions, label);
			currentRect.y += currentRect.height + EditorGUIUtility.standardVerticalSpacing;
		}

		public static void DrawLODResolutionField(SerializedProperty property, GUIContent label, ref Rect currentRect)
		{
			currentRect.height = EditorGUIUtility.singleLineHeight;
			int resolution = EditorGUI.IntPopup(currentRect, label, property.vector2IntValue.x, lodResolutionsName, lodResolutions);
			property.vector2IntValue = new Vector2Int(resolution, resolution);
			currentRect.y += currentRect.height + EditorGUIUtility.standardVerticalSpacing;
		}

		public static void DrawMeshBlocksField(SerializedProperty property, GUIContent label, ref Rect currentRect)
		{
			currentRect.height = EditorGUIUtility.singleLineHeight;
			int resolution = EditorGUI.IntPopup(currentRect, label, property.vector2IntValue.x, meshBlocksName, meshBlocks);
			property.vector2IntValue = new Vector2Int(resolution, resolution);
			currentRect.y += currentRect.height + EditorGUIUtility.standardVerticalSpacing;
		}
	}
}