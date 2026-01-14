using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace FluidFrenzy.Editor
{
	public class DragAndDropButton : VisualElement
	{
		private const string DragKey = "FluidFrenzy_ComponentType";
		private readonly Type m_ComponentType;
		private readonly Action<Vector3, Transform> m_CustomAction;

		// Constructor for Type-based dragging
		public DragAndDropButton(Type componentType, Texture2D icon, string tooltipText, int width = 20, int height = 20)
			: this(icon, tooltipText, width, height)
		{
			m_ComponentType = componentType;
		}

		// Constructor for Custom Action dragging
		public DragAndDropButton(Action<Vector3, Transform> onDrop, Texture2D icon, string tooltipText, int width = 20, int height = 20)
			: this(icon, tooltipText, width, height)
		{
			m_CustomAction = onDrop;
		}

		private DragAndDropButton(Texture2D icon, string tooltipText, int width, int height)
		{
			style.width = width;
			style.height = height;
			style.marginRight = 2;
			style.marginBottom = 2;
			style.backgroundImage = icon;
			tooltip = tooltipText;
			AddToClassList("toolbar-button");

			RegisterCallback<PointerDownEvent>(OnPointerDown);
			RegisterCallback<PointerUpEvent>(OnPointerUp);
			RegisterCallback<PointerMoveEvent>(OnPointerMove);

			// Register the Scene Drop Handler
#if UNITY_6000_3_OR_NEWER
			RegisterCallback<AttachToPanelEvent>(evt => DragAndDrop.AddDropHandlerV2(SceneDropCallback));
			RegisterCallback<DetachFromPanelEvent>(evt => DragAndDrop.RemoveDropHandlerV2(SceneDropCallback));
#else
			RegisterCallback<AttachToPanelEvent>(evt => DragAndDrop.AddDropHandler(SceneDropCallback));
			RegisterCallback<DetachFromPanelEvent>(evt => DragAndDrop.RemoveDropHandler(SceneDropCallback));
#endif
		}

		// Capture Pointer on Down
		private void OnPointerDown(PointerDownEvent e)
		{
			if (e.button == 0)
			{
				SceneViewDepthPicker.IsActive = true;
				this.CapturePointer(e.pointerId);
				e.StopImmediatePropagation();
			}
		}

		// Release Pointer on Up
		private void OnPointerUp(PointerUpEvent e)
		{
			if (this.HasPointerCapture(e.pointerId))
			{
				SceneViewDepthPicker.IsActive = false;
				this.ReleasePointer(e.pointerId);
			}
		}

		// Handle Drag on Move
		private void OnPointerMove(PointerMoveEvent e)
		{
			// Check if we have captured this specific pointer
			if (this.HasPointerCapture(e.pointerId))
			{
				DragAndDrop.PrepareStartDrag();
				DragAndDrop.SetGenericData(DragKey, this);
				DragAndDrop.StartDrag(tooltip);

				this.ReleasePointer(e.pointerId);
			}
		}

		private DragAndDropVisualMode SceneDropCallback(Object dropUpon, Vector3 worldPosition, Vector2 viewportPosition, Transform parentForDraggedObjects, bool perform)
		{
			var sourceButton = DragAndDrop.GetGenericData(DragKey) as DragAndDropButton;
			if (sourceButton != this) return DragAndDropVisualMode.None;

			if (!perform) return DragAndDropVisualMode.Copy;

			Vector3 finalPos = SceneViewDepthPicker.WorldPosition;

			if (m_ComponentType != null)
			{
				CreateObjectWithType(finalPos, parentForDraggedObjects);
			}
			else
			{
				m_CustomAction?.Invoke(finalPos, parentForDraggedObjects);
			}

			SceneViewDepthPicker.IsActive = false;

			return DragAndDropVisualMode.Copy;
		}

		private void CreateObjectWithType(Vector3 position, Transform parent)
		{
			string name = m_ComponentType.Name;
			GameObject go = new GameObject($"New {name}");
			if (parent != null) go.transform.SetParent(parent);
			go.transform.position = position;
			go.AddComponent(m_ComponentType);

			Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
			Selection.activeObject = go;
		}

		public static DragAndDropButton Create<T>(Texture2D icon, string name) where T : Component
		{
			return new DragAndDropButton(typeof(T), icon, name);
		}

		// Add this inside DragAndDropButton class next to the existing Create<T> method
		public static DragAndDropButton Create(Action<Vector3, Transform> onDrop, Texture2D icon, string name)
		{
			return new DragAndDropButton(onDrop, icon, name);
		}
	}
}