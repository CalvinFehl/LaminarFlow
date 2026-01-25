using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace FluidFrenzy.Editor
{
	public class ExclusiveButtonGrid<T> : VisualElement, ISizableElement
	{
		static class Styles
		{
			public const float BaseIconSize = 16f;
			public const float MarginVertical = 5f;
			public static readonly Color ActiveButton = new Color(0.3f, 0.5f, 0.8f, 0.5f);
		}

		public T Value => m_values[m_selectedIndex];

		private VisualElement m_icon;
		private VisualElement m_container;
		private List<Button> m_buttons = new List<Button>();
		private T[] m_values;
		private Action<T> m_callback;
		private int m_selectedIndex = 0;

		public ExclusiveButtonGrid(string iconName, string[] labels, T[] values, T initialValue, Action<T> callback, string groupTooltip, string[] elementTooltips = null)
		{
			m_values = values;
			m_callback = callback;

			// Add Icon
			m_icon = new Image { image = EditorGUIUtility.IconContent(iconName).image };
			m_icon.style.width = Styles.BaseIconSize;
			m_icon.style.height = Styles.BaseIconSize;
			m_icon.tooltip = groupTooltip;
			Add(m_icon);

			// Add Container
			m_container = new VisualElement();
			Add(m_container);

			for (int i = 0; i < labels.Length; i++)
			{
				int index = i;
				Button b = new Button(() => SelectIndex(index));
				b.text = labels[index];
				b.style.minWidth = 20;
				b.style.height = 20;
				b.style.paddingLeft = 2; b.style.paddingRight = 2;
				b.style.fontSize = 10;
				b.style.marginRight = 0; b.style.marginLeft = 0; b.style.marginTop = 0; b.style.marginBottom = 0;
				b.style.flexGrow = 1;

				if (elementTooltips != null && elementTooltips.Length > index)
					b.tooltip = elementTooltips[index];
				else
					b.tooltip = $"{groupTooltip}: {labels[index]}";

				m_container.Add(b);
				m_buttons.Add(b);
				if (EqualityComparer<T>.Default.Equals(initialValue, values[i])) m_selectedIndex = i;
			}
			UpdateVisuals();
		}

		private void SelectIndex(int index) { m_selectedIndex = index; UpdateVisuals(); m_callback?.Invoke(m_values[index]); }

		private void UpdateVisuals()
		{
			for (int i = 0; i < m_buttons.Count; i++)
			{
				if (i == m_selectedIndex)
				{
					m_buttons[i].style.backgroundColor = new StyleColor(Styles.ActiveButton);
					m_buttons[i].style.color = Color.white;
				}
				else
				{
					m_buttons[i].style.backgroundColor = StyleKeyword.Null;
					m_buttons[i].style.color = StyleKeyword.Null;
				}
			}
		}

		public new void SetEnabled(bool enabled) { base.SetEnabled(enabled); style.opacity = enabled ? 1f : 0.5f; }

		public void SetVertical()
		{
			style.flexDirection = FlexDirection.Column;
			style.alignItems = Align.Stretch;
			style.marginBottom = Styles.MarginVertical;
			style.marginRight = 0;

			m_icon.style.marginBottom = 2;
			m_icon.style.marginRight = 0;
			m_icon.style.alignSelf = Align.Center;

			m_container.style.flexDirection = FlexDirection.Column;
			m_container.style.width = Length.Percent(100);
		}

		public void SetHorizontal()
		{
			style.flexDirection = FlexDirection.Row;
			style.alignItems = Align.Center;
			style.marginBottom = 0;
			style.marginRight = 8;

			m_icon.style.marginBottom = 0;
			m_icon.style.marginRight = 4;
			m_icon.style.alignSelf = Align.Auto;

			m_container.style.flexDirection = FlexDirection.Row;
			m_container.style.width = StyleKeyword.Auto;
		}
	}
}