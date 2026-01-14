using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace FluidFrenzy.Editor
{
	public class FilledSlider : VisualElement, ISizableElement
	{
		internal static class Styles
		{
			public const float SliderHeightVertical = 80f;
			public const float SliderWidthHorizontal = 120f;
			public const float SliderHeightHorizontal = 20f;
			public const float SliderThumbSize = 14f;
			public const float MarginHorizontal = 6f;

			public static readonly Color SliderBg = new Color(0.22f, 0.22f, 0.22f, 1f);
			public static readonly Color SliderFill = new Color(0.38f, 0.38f, 0.38f, 1f);
			public static readonly Color Border = new Color(0.1f, 0.1f, 0.1f, 1f);
			public static readonly Color Text = new Color(0.9f, 0.9f, 0.9f);
		}

		private VisualElement m_fill;
		private Label m_label;
		private Image m_iconImg;

		public float Min, Max;
		private float m_value;
		public string TextPrefix;

		private bool m_isVertical = false;
		private bool m_isDragging = false;

		public Action<float> OnValueChanged;

		public float Value
		{
			get => m_value;
			set
			{
				m_value = Mathf.Clamp(value, Min, Max);
				UpdateVisuals();
				OnValueChanged?.Invoke(m_value);
			}
		}

		public FilledSlider(string iconName, float min, float max, float initial, string prefix, string tooltip, Action<float> onValueChange = null)
		{
			Min = min;
			Max = max;
			m_value = initial;
			TextPrefix = prefix;
			this.tooltip = tooltip;
			OnValueChanged = onValueChange;

			// Base container styling
			style.flexShrink = 0;
			style.overflow = Overflow.Hidden;
			style.backgroundColor = new StyleColor(Styles.SliderBg);

			style.borderTopWidth = style.borderBottomWidth = style.borderLeftWidth = style.borderRightWidth = 1;
			style.borderTopColor = style.borderBottomColor = style.borderLeftColor = style.borderRightColor = Styles.Border;
			style.borderTopLeftRadius = style.borderTopRightRadius = style.borderBottomLeftRadius = style.borderBottomRightRadius = 3;

			// Fill element
			m_fill = new VisualElement();
			m_fill.style.backgroundColor = new StyleColor(Styles.SliderFill);
			m_fill.pickingMode = PickingMode.Ignore;
			Add(m_fill);

			// Icon
			m_iconImg = new Image { image = EditorGUIUtility.IconContent(iconName).image };
			m_iconImg.style.position = Position.Absolute;
			m_iconImg.style.width = Styles.SliderThumbSize;
			m_iconImg.style.height = Styles.SliderThumbSize;
			m_iconImg.style.opacity = 0.7f;
			m_iconImg.pickingMode = PickingMode.Ignore;
			Add(m_iconImg);

			// Text Label
			m_label = new Label();
			m_label.style.position = Position.Absolute;
			m_label.style.left = 0; m_label.style.right = 0; m_label.style.top = 0; m_label.style.bottom = 0;
			m_label.style.unityTextAlign = TextAnchor.MiddleCenter;
			m_label.style.color = Styles.Text;
			m_label.style.fontSize = 10;
			m_label.pickingMode = PickingMode.Ignore;
			Add(m_label);

			// Event Registration
			RegisterCallback<MouseDownEvent>(OnMouseDown);
			RegisterCallback<MouseMoveEvent>(OnMouseMove);
			RegisterCallback<MouseUpEvent>(OnMouseUp);
			RegisterCallback<MouseLeaveEvent>(evt => {
				if (m_isDragging)
				{
					m_isDragging = false;
					this.ReleaseMouse();
				}
			});

			UpdateVisuals();
		}

		public void SetVertical()
		{
			m_isVertical = true;

			style.flexDirection = FlexDirection.Column;
			style.marginBottom = 6;
			style.marginRight = 0;
			style.width = Length.Percent(100);
			style.height = Styles.SliderHeightVertical;

			UpdateVisuals();
		}

		public void SetHorizontal()
		{
			m_isVertical = false;

			style.flexDirection = FlexDirection.Row;
			style.marginBottom = 0;
			style.marginRight = Styles.MarginHorizontal;
			style.width = Styles.SliderWidthHorizontal;
			style.height = Styles.SliderHeightHorizontal;

			UpdateVisuals();
		}

		private void OnMouseDown(MouseDownEvent evt)
		{
			if (evt.button != 0) return;
			m_isDragging = true;
			this.CaptureMouse();
			CalculateValue(evt.localMousePosition);
			evt.StopPropagation();
		}

		private void OnMouseMove(MouseMoveEvent evt)
		{
			if (m_isDragging)
			{
				CalculateValue(evt.localMousePosition);
				evt.StopPropagation();
			}
		}

		private void OnMouseUp(MouseUpEvent evt)
		{
			if (m_isDragging)
			{
				m_isDragging = false;
				this.ReleaseMouse();
				evt.StopPropagation();
			}
		}

		private void CalculateValue(Vector2 mousePos)
		{
			float t = 0;
			if (m_isVertical) t = Mathf.Clamp01(mousePos.y / contentRect.height);
			else t = Mathf.Clamp01(mousePos.x / contentRect.width);

			Value = Mathf.Lerp(Min, Max, t);
		}

		private void UpdateVisuals()
		{
			float t = Mathf.InverseLerp(Min, Max, m_value);
			string format = (Max - Min) > 100 ? "F0" : "F1";

			if (m_isVertical)
			{
				m_fill.style.width = Length.Percent(100);
				m_fill.style.height = Length.Percent(t * 100f);
				m_fill.style.top = 0;
				m_fill.style.bottom = StyleKeyword.Auto;
				m_fill.style.left = 0;

				m_label.text = m_value.ToString(format);

				m_iconImg.style.left = Length.Percent(50);
				m_iconImg.style.marginLeft = -(Styles.SliderThumbSize / 2f);
				m_iconImg.style.top = 2;
			}
			else
			{
				m_fill.style.width = Length.Percent(t * 100f);
				m_fill.style.height = Length.Percent(100);
				m_fill.style.top = 0;
				m_fill.style.bottom = 0;
				m_fill.style.left = 0;

				m_label.text = $"{TextPrefix}: {m_value.ToString(format)}";

				m_iconImg.style.top = 3;
				m_iconImg.style.left = 4;
				m_iconImg.style.marginLeft = 0;
			}
		}
	}
}