using UnityEditor;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.UIElements;

namespace FluidFrenzy.Editor
{
	public class DropdownPopupPanel : ISizableElement
	{
		static class Styles
		{
			public const float DropdownWidthHorizontal = 40f;
			public const float MarginVertical = 5f;

			internal static class Colors
			{
				public static readonly Color Text = new Color(0.9f, 0.9f, 0.9f);
			}
		}

		public VisualElement Root;
		private EditorToolbarDropdown m_dropdown;
		private GUIContent m_panelLabel;

		public DropdownPopupPanel(GUIContent icon, string tooltip, GUIContent panelLabel)
		{
			Root = new VisualElement();
			Root.style.justifyContent = Justify.Center;
			Root.style.alignItems = Align.Stretch;

			m_dropdown = new EditorToolbarDropdown();
			m_dropdown.tooltip = tooltip;
			m_dropdown.text = "";
			m_dropdown.icon = icon.image as Texture2D;
			m_dropdown.clicked += OpenPopup;

			m_panelLabel = panelLabel;

			Root.Add(m_dropdown);
		}

		private void OpenPopup()
		{
			UnityEditor.PopupWindow.Show(m_dropdown.worldBound, new DropdownPopupContent(m_panelLabel));
		}

		public void SetVertical()
		{
			Root.style.marginBottom = Styles.MarginVertical;
			Root.style.marginRight = 0;
			m_dropdown.style.width = StyleKeyword.Auto;
			m_dropdown.style.flexGrow = 1;
		}

		public void SetHorizontal()
		{
			Root.style.marginBottom = 0;
			Root.style.marginRight = Styles.MarginVertical;
			m_dropdown.style.width = Styles.DropdownWidthHorizontal;
			m_dropdown.style.flexGrow = 0;
		}
	}

	public class DropdownPopupContent : PopupWindowContent
	{
		private Vector2 m_scroll;
		private GUIContent m_label;
		private const int BoxSize = 40;

		public DropdownPopupContent(GUIContent label)
		{
			m_label = label;
		}

		public override Vector2 GetWindowSize()
		{
			return new Vector2(210, 240);
		}

		public override void OnGUI(Rect rect)
		{
			GUILayout.BeginVertical(EditorStyles.helpBox);
			GUILayout.Label(m_label, EditorStyles.boldLabel);
			GUILayout.Space(4);

			m_scroll = GUILayout.BeginScrollView(m_scroll);

			int columns = 4;
			int count = 0;

			GUILayout.BeginVertical();
			GUILayout.BeginHorizontal();

			for (int i = 0; i < 16; i++)
			{
				if (count >= columns)
				{
					count = 0;
					GUILayout.EndHorizontal();
					GUILayout.BeginHorizontal();
				}

				string iconName = (i % 2 == 0) ? "KnobCShape" : $"sv_icon_dot{i}_sml";
				var content = EditorGUIUtility.IconContent(iconName);

				if (GUILayout.Button(content, GUILayout.Width(BoxSize), GUILayout.Height(BoxSize)))
				{
					Debug.Log($"Selected Brush {i}");
					editorWindow.Close();
				}

				count++;
			}

			GUILayout.EndHorizontal();
			GUILayout.EndVertical();
			GUILayout.EndScrollView();
			GUILayout.EndVertical();

			if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
				editorWindow.Close();
		}
	}
}