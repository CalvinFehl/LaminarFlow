using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace FluidFrenzy.Editor
{
	public class EditorExtensions
	{
		public static readonly int[] pot8 = { 1, 2, 4, 8 };
		public static readonly int[] pot16 = { 1, 2, 4, 8, 16 };
		public static readonly int[] pot32 = { 1, 2, 4, 8, 16, 32 };
		public static readonly int[] pot64 = { 1, 2, 4, 8, 16, 32, 64 };
		public static readonly int[] pot128 = { 1, 2, 4, 8, 16, 32, 64, 128 };
		public static readonly int[] pot256 = { 1, 2, 4, 8, 16, 32, 64, 128, 256 };
		public static readonly int[] pot512 = { 1, 2, 4, 8, 16, 32, 64, 128, 256, 512 };
		public static readonly int[] pot1024 = { 1, 2, 4, 8, 16, 32, 64, 128, 256, 512, 1024 };
		public static GUIContent[] pot8Name => pot8.Select(x => new GUIContent(x.ToString())).ToArray();
		public static GUIContent[] pot16Name => pot16.Select(x => new GUIContent(x.ToString())).ToArray();
		public static GUIContent[] pot32Name => pot32.Select(x => new GUIContent(x.ToString())).ToArray();
		public static GUIContent[] pot64Name => pot64.Select(x => new GUIContent(x.ToString())).ToArray();
		public static GUIContent[] pot128Name => pot128.Select(x => new GUIContent(x.ToString())).ToArray();
		public static GUIContent[] pot256Name => pot256.Select(x => new GUIContent(x.ToString())).ToArray();
		public static GUIContent[] pot512Name => pot512.Select(x => new GUIContent(x.ToString())).ToArray();
		public static GUIContent[] pot1024Name => pot1024.Select(x => new GUIContent(x.ToString())).ToArray();

		public static class Styling
		{
			/// <summary>
			/// Style for the override checkbox.
			/// </summary>
			public static readonly GUIStyle smallTickbox;


			static readonly Color splitterDark;
			static readonly Color splitterLight;

			/// <summary>
			/// Color of UI splitters.
			/// </summary>
			public static Color splitter { get { return EditorGUIUtility.isProSkin ? splitterDark : splitterLight; } }

			static readonly Texture2D paneOptionsIconDark;
			static readonly Texture2D paneOptionsIconLight;

			/// <summary>
			/// Option icon used in effect headers.
			/// </summary>
			public static Texture2D paneOptionsIcon { get { return EditorGUIUtility.isProSkin ? paneOptionsIconDark : paneOptionsIconLight; } }

			/// <summary>
			/// Style for effect header labels.
			/// </summary>
			public static readonly GUIStyle headerLabel;

			static readonly Color headerBackgroundDark;
			static readonly Color headerBackgroundLight;

			/// <summary>
			/// Color of effect header backgrounds.
			/// </summary>
			public static Color headerBackground { get { return EditorGUIUtility.isProSkin ? headerBackgroundDark : headerBackgroundLight; } }

			/// <summary>
			/// Style for the trackball labels.
			/// </summary>
			public static readonly GUIStyle wheelLabel;

			/// <summary>
			/// Style for the trackball cursors.
			/// </summary>
			public static readonly GUIStyle wheelThumb;

			/// <summary>
			/// Size of the trackball cursors.
			/// </summary>
			public static readonly Vector2 wheelThumbSize;

			/// <summary>
			/// Style for the curve editor position info.
			/// </summary>
			public static readonly GUIStyle preLabel;

			static Styling()
			{
				smallTickbox = new GUIStyle("ShurikenToggle");

				splitterDark = new Color(0.12f, 0.12f, 0.12f, 1.333f);
				splitterLight = new Color(0.6f, 0.6f, 0.6f, 1.333f);

				headerBackgroundDark = new Color(0.1f, 0.1f, 0.1f, 0.2f);
				headerBackgroundLight = new Color(1f, 1f, 1f, 0.2f);

				paneOptionsIconDark = (Texture2D)EditorGUIUtility.Load("Builtin Skins/DarkSkin/Images/pane options.png");
				paneOptionsIconLight = (Texture2D)EditorGUIUtility.Load("Builtin Skins/LightSkin/Images/pane options.png");

				headerLabel = new GUIStyle(EditorStyles.miniLabel);

				wheelThumb = new GUIStyle("ColorPicker2DThumb");

				wheelThumbSize = new Vector2(
					!Mathf.Approximately(wheelThumb.fixedWidth, 0f) ? wheelThumb.fixedWidth : wheelThumb.padding.horizontal,
					!Mathf.Approximately(wheelThumb.fixedHeight, 0f) ? wheelThumb.fixedHeight : wheelThumb.padding.vertical
				);

				wheelLabel = new GUIStyle(EditorStyles.miniLabel);

				preLabel = new GUIStyle("ShurikenLabel");
			}
		}

		public class GUIStyleScope : GUI.Scope
		{
			GUIStyle targetStyle;
			FontStyle origFontStyle;
			public GUIStyleScope(GUIStyle style, FontStyle fontStyle)
			{
				targetStyle = style;
				origFontStyle = targetStyle.fontStyle;
				targetStyle.fontStyle = FontStyle.Bold;
			}
			public bool enabled { get; protected set; }
			protected override void CloseScope()
			{
				targetStyle.fontStyle = origFontStyle;
			}
		}

		//
		// Summary:
		//     Change the background color of the GuI.
		public struct BackgroundColorScope : IDisposable
		{
			//
			// Summary:
			//     Create a new BackgroundColorScope and begin the corresponding group.
			//
			// Parameters:
			//   disabled:
			//     Color specifying if the background color inside the group.
			public BackgroundColorScope(Color backgroundColor)
			{
				m_previousColor = GUI.backgroundColor;
				GUI.backgroundColor = backgroundColor;
			}

			public void Dispose()
			{
				GUI.backgroundColor = m_previousColor;
			}

			private Color m_previousColor;
		}

		//
		// Summary:
		//     Change the labelwidth of the GuI.
		public struct LabelWidthScope : IDisposable
		{
			//
			// Summary:
			//     Create a new BackgroundColorScope and begin the corresponding group.
			//
			// Parameters:
			//   disabled:
			//     Color specifying if the background color inside the group.
			public LabelWidthScope(float width)
			{
				m_previousWidth = EditorGUIUtility.labelWidth;
				EditorGUIUtility.labelWidth = width;
			}

			public void Dispose()
			{
				EditorGUIUtility.labelWidth = m_previousWidth;
			}

			private float m_previousWidth;
		}

		public static void DrawVector2AsDegrees(SerializedProperty property, GUIContent label)
		{
			Vector2 direction = property.vector2Value;
			float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
			float newAngle = EditorGUILayout.FloatField(label, angle);
			if (newAngle != angle)
			{
				float rad = newAngle * Mathf.Deg2Rad;
				Vector2 newDirection = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
				property.vector2Value = newDirection;
			}
		}

		public static void WidthHeightDropdown(SerializedProperty property, GUIContent[] displayOptions, int[] valueOptions, GUIContent label)
		{
			SerializedProperty x = property.FindPropertyRelative("x");
			SerializedProperty y = property.FindPropertyRelative("y");

			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField(label, GUILayout.Width(EditorGUIUtility.labelWidth - EditorGUI.indentLevel * 15));
			float labelWidth = EditorGUIUtility.labelWidth;
			EditorGUIUtility.labelWidth = 12 + EditorGUI.indentLevel * 15;
			EditorGUILayout.IntPopup(x, displayOptions, valueOptions);
			GUILayout.Space(-EditorGUI.indentLevel * 15);
			EditorGUILayout.IntPopup(y, displayOptions, valueOptions);
			EditorGUIUtility.labelWidth = labelWidth;
			EditorGUILayout.EndHorizontal();
		}

		public static void WidthHeightDropdown(Rect currentRect, SerializedProperty property, GUIContent[] displayOptions, int[] valueOptions, GUIContent label)
		{
			SerializedProperty x = property.FindPropertyRelative("x");
			SerializedProperty y = property.FindPropertyRelative("y");

			float labelWidth = EditorGUIUtility.labelWidth;

			EditorGUI.PrefixLabel(currentRect, label);

			float w = currentRect.width - labelWidth;
			EditorGUIUtility.labelWidth = 8;
			currentRect.x += labelWidth;
			currentRect.width = w / 2.0f;
			EditorGUI.IntPopup(currentRect, x, displayOptions, valueOptions);
			currentRect.x += w / 2.0f;
			//GUILayout.Space(-EditorGUI.indentLevel * 15);
			EditorGUI.IntPopup(currentRect, y, displayOptions, valueOptions);

			EditorGUIUtility.labelWidth = labelWidth;
		}

		public static Vector2Int MinMaxSliderInt(Rect currentRect, GUIContent label, Vector2Int value, int minValue, int maxValue, int max_minValue, int min_maxValue)
		{
			float fieldWidth = EditorGUIUtility.fieldWidth;
			float indentOffset = EditorGUI.indentLevel * 15f;
			float totalWidth = EditorGUIUtility.currentViewWidth - EditorGUIUtility.labelWidth - indentOffset - 20; // 20 for padding
			Rect labelRect = new Rect(currentRect.x, currentRect.y, EditorGUIUtility.labelWidth - indentOffset, currentRect.height);
			
			// Ensure minField and maxField sizes are appropriate and calculate sliderRect width
			float minMaxFieldWidth = fieldWidth / 2;
			float availableSliderWidth = totalWidth - (minMaxFieldWidth * 2) - 16; // 8 for padding between min, slider, max
			
			Rect minRect = new Rect(labelRect.xMax + 4, labelRect.y, minMaxFieldWidth, currentRect.height);
			Rect sliderRect = new Rect(minRect.xMax + 4, labelRect.y, availableSliderWidth, currentRect.height);
			Rect maxRect = new Rect(sliderRect.xMax + 4, labelRect.y, minMaxFieldWidth, currentRect.height);
			
			EditorGUI.PrefixLabel(labelRect, label);
			
			float x = value.x;
			float y = value.y;
			
			// Draw the MinMaxSlider in the middle
			EditorGUI.MinMaxSlider(sliderRect, ref x, ref y, minValue, maxValue);
			x = Mathf.Min(x, max_minValue);
			y = Mathf.Max(y, min_maxValue);
			// Update the min and max fields after the slider
			x = Mathf.Round(EditorGUI.IntField(minRect, (int)x));
			y = Mathf.Round(EditorGUI.IntField(maxRect, (int)y));

			return new Vector2Int((int)x, (int)y);
		}

		public static void MinMaxSlider(ref Vector2 minmax, float minLimit, float maxLimit, GUIContent label)
		{
			EditorGUILayout.BeginHorizontal();
			minmax.x = EditorGUILayout.FloatField(label, minmax.x);
			EditorGUILayout.MinMaxSlider(ref minmax.x, ref minmax.y, minLimit, maxLimit);
			minmax.y = EditorGUILayout.FloatField(minmax.y, GUILayout.Width(50));
			EditorGUILayout.EndHorizontal();
		}

		public static void MinMaxShaderProperty(MaterialEditor editor, MaterialProperty minmax, float minLimit, float maxLimit, GUIContent label)
		{
			Vector2 minmaxValue = minmax.vectorValue;
			EditorGUI.BeginChangeCheck();

			EditorGUILayout.BeginHorizontal();
			minmaxValue.x = EditorGUILayout.FloatField(label, minmaxValue.x);
			EditorGUILayout.MinMaxSlider(ref minmaxValue.x, ref minmaxValue.y, minLimit, maxLimit);
			minmaxValue.y = EditorGUILayout.FloatField(minmaxValue.y, GUILayout.Width(50));
			EditorGUILayout.EndHorizontal();

			if (EditorGUI.EndChangeCheck())
			{
				minmax.vectorValue = minmaxValue;
			}
		}

		public static void MinMaxSlider(SerializedProperty minmax, float minLimit, float maxLimit, GUIContent label)
		{
			Vector2 minmaxValue = minmax.vector2Value;
			EditorGUI.BeginChangeCheck();

			EditorGUILayout.BeginHorizontal();
			minmaxValue.x = EditorGUILayout.FloatField(label, minmaxValue.x);
			EditorGUILayout.MinMaxSlider(ref minmaxValue.x, ref minmaxValue.y, minLimit, maxLimit);
			minmaxValue.y = EditorGUILayout.FloatField(minmaxValue.y, GUILayout.Width(50));
			EditorGUILayout.EndHorizontal();

			if (EditorGUI.EndChangeCheck())
			{
				minmax.vector2Value = minmaxValue;
			}
		}

		public static bool DrawFoldoutHeaderToggle(SerializedProperty activeProperty, GUIContent content)
		{
			var backgroundRect = GUILayoutUtility.GetRect(1f, 17f);
			return DrawFoldoutHeaderToggle(activeProperty, content, ref backgroundRect);
		}

		public static bool DrawFoldoutHeaderToggle(SerializedProperty activeProperty, GUIContent content, out bool changed)
		{
			using (var check = new EditorGUI.ChangeCheckScope())
			{
				bool expanded = DrawFoldoutHeaderToggle(activeProperty, content);
				changed = check.changed;
				return expanded;
			}
		}

		public static bool DrawFoldoutHeaderToggle(SerializedProperty activeProperty, GUIContent content, ref Rect rect, bool fullWidth = true)
		{
			var backgroundRect = rect;
			var labelRect = backgroundRect;
			labelRect.xMin += 32f;
			labelRect.xMax -= 20f;

			var foldoutRect = backgroundRect;
			foldoutRect.y += 1f;
			foldoutRect.width = 13f;
			foldoutRect.height = 13f;

			var toggleRect = backgroundRect;
			toggleRect.x += 16f;
			toggleRect.y += 2f;
			toggleRect.width = 13f;
			toggleRect.height = 13f;

			var menuIcon = Styling.paneOptionsIcon;
#if UNITY_2019_3_OR_NEWER
			var menuRect = new Rect(labelRect.xMax + 4f, labelRect.y, menuIcon.width, menuIcon.height);
#else
            var menuRect = new Rect(labelRect.xMax + 4f, labelRect.y + 4f, menuIcon.width, menuIcon.height);
#endif

			// Background rect should be full-width
			if (fullWidth)
			{
				backgroundRect.xMin = 0f;
				backgroundRect.width += 4f;
			}

			// foldout
			activeProperty.isExpanded = GUI.Toggle(foldoutRect, activeProperty.isExpanded, GUIContent.none, EditorStyles.foldout);

			// Active checkbox
			activeProperty.boolValue = GUI.Toggle(toggleRect, activeProperty.boolValue, GUIContent.none, Styling.smallTickbox);

			// Background
			using (var bg = new BackgroundColorScope(Styling.headerBackground.gamma))
			{
				if (GUI.Button(backgroundRect, GUIContent.none))
					activeProperty.isExpanded = !activeProperty.isExpanded;
			}

			// Title
			using (new EditorGUI.DisabledScope(!activeProperty.boolValue))
				EditorGUI.LabelField(labelRect, content, EditorStyles.boldLabel);

			rect.y += 20;

			return activeProperty.isExpanded;
		}

		public static bool DrawFoldoutHeader(SerializedProperty activeProperty, GUIContent content)
		{
			var backgroundRect = GUILayoutUtility.GetRect(1f, 17f);
			return DrawFoldoutHeader(activeProperty, content, ref backgroundRect);
		}

		public static bool DrawFoldoutHeader(ref bool isExpanded, GUIContent content)
		{
			var backgroundRect = GUILayoutUtility.GetRect(1f, 17f);
			return DrawFoldoutHeader(ref isExpanded, content, ref backgroundRect);
		}

		public static bool DrawFoldoutHeader(SerializedProperty activeProperty, GUIContent content, ref Rect rect, bool fullWidth = true)
		{
			bool isExpanded = activeProperty.isExpanded;
			activeProperty.isExpanded = DrawFoldoutHeader(ref isExpanded, content, ref rect, fullWidth);
			return isExpanded;
		}
		public static bool DrawFoldoutHeader(ref bool isExpanded, GUIContent content, ref Rect rect, bool fullWidth = true)
		{
			var backgroundRect = rect;
			var labelRect = backgroundRect;
			labelRect.xMin += 16f;
			labelRect.xMax -= 20f;

			var foldoutRect = backgroundRect;
			foldoutRect.y += 1f;
			foldoutRect.width = 13f;
			foldoutRect.height = 13f;

			var menuIcon = Styling.paneOptionsIcon;
#if UNITY_2019_3_OR_NEWER
			var menuRect = new Rect(labelRect.xMax + 4f, labelRect.y, menuIcon.width, menuIcon.height);
#else
            var menuRect = new Rect(labelRect.xMax + 4f, labelRect.y + 4f, menuIcon.width, menuIcon.height);
#endif

			// Background rect should be full-width
			if (fullWidth)
			{
				backgroundRect.xMin = 0f;
				backgroundRect.width += 4f;
			}

			// foldout
			isExpanded = EditorGUI.Toggle(foldoutRect, GUIContent.none, isExpanded, EditorStyles.foldout);

			// Background
			using (var bg = new BackgroundColorScope(Styling.headerBackground.gamma))
			{
				if (GUI.Button(backgroundRect, GUIContent.none))
					isExpanded = !isExpanded;
			}

			// Title
			EditorGUI.LabelField(labelRect, content, EditorStyles.boldLabel);

			rect.y += 20;

			return isExpanded;
		}


		public static bool DrawExpandToggle(SerializedProperty activeProperty, GUIContent label)
		{
			activeProperty.isExpanded = GUILayout.Toggle(activeProperty.isExpanded, label, EditorStyles.foldoutHeader, GUILayout.Width(EditorGUIUtility.labelWidth));
			return activeProperty.isExpanded;
		}

		public static void DrawHeader(GUIContent content)
		{
			var backgroundRect = GUILayoutUtility.GetRect(1f, 17f);
			DrawHeader(content, ref backgroundRect);
		}

		public static void DrawHeader(GUIContent content, ref Rect rect)
		{
			var backgroundRect = rect;
			var labelRect = backgroundRect;

			var menuIcon = Styling.paneOptionsIcon;
#if UNITY_2019_3_OR_NEWER
			var menuRect = new Rect(labelRect.xMax + 4f, labelRect.y, menuIcon.width, menuIcon.height);
#else
            var menuRect = new Rect(labelRect.xMax + 4f, labelRect.y + 4f, menuIcon.width, menuIcon.height);
#endif

			// Background rect should be full-width
			backgroundRect.xMin = 0f;
			backgroundRect.width += 4f;

			// Background
			EditorGUI.DrawRect(backgroundRect, Styling.headerBackground);

			// Title
			EditorGUI.LabelField(labelRect, content, EditorStyles.boldLabel);

			rect.y += 20;
		}

		internal static float HelpBoxHeight(string message)
		{
			Vector2 textDimensions = GUI.skin.label.CalcSize(new GUIContent(message));
			return Mathf.Ceil(textDimensions.x / EditorGUIUtility.currentViewWidth) * EditorGUIUtility.singleLineHeight;
		}


		public static void DrawComputeUnsupportedWarning()
		{
			BuildTarget target = EditorUserBuildSettings.activeBuildTarget;
			BuildTargetGroup targetGroup = BuildPipeline.GetBuildTargetGroup(target);
			if (targetGroup == BuildTargetGroup.WebGL)
			{
				EditorGUILayout.HelpBox("This component uses Compute Shaders which is not supported on the selected platform", MessageType.Error);
			}
		}
	}
}