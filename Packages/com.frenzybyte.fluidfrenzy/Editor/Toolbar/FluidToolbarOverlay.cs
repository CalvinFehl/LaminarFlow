using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Overlays;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.UIElements;

namespace FluidFrenzy.Editor
{
	[EditorToolbarElement(ToolbarId, typeof(SceneView))]
	public class FluidToolbarElement : VisualElement, IAccessContainerWindow
	{
		public const string ToolbarId = "FluidFrenzyToolbar/Main";

		public static class Styles
		{
			public static class Dimensions
			{
				public const float ScrollViewPadding = 5f;

				// Elements
				public const float BaseButtonHeight = 20f;
				public const float ToggleIconSize = 18f;
				public const float ToggleWidthHorizontal = 32f;

				// Spacing
				public const float SeparatorLengthPercent = 80f;
			}

			public static class Text
			{
				public const string TooltipSculpt = "Sculpt Mode";
				public const string TooltipMod = "Modifier Mode";
				public const string TooltipLayer = "Terrain Layer";
				public const string TooltipSplat = "Splat Channel";
				public const string TooltipBlend = "Blend Mode";
				public const string TooltipBrushSelect = "Select Brush";

				public const string TooltipDragFluidSource = "Fluid Source";
				public const string TooltipDragFluidForce = "Fluid Force";
				public const string TooltipDragFluidCurrent = "Fluid Current";
				public const string TooltipDragFluidVortex = "Fluid Vortex";
				public const string TooltipDragTerrainMod = "Terrain Modifier";
				public const string TooltipDragTerraformMod = "Terraform Modifier";

				public const string TooltipDragObstacleCube = "Obstacle Cube";
				public const string TooltipDragObstacleSphere = "Obstacle Sphere";
				public const string TooltipDragObstacleCylinder = "Obstacle Cylinder";
				public const string TooltipDragObstacleElipse = "Obstacle Elipse";
				public const string TooltipDragObstacleWedge = "Obstacle Wedge";
				public const string TooltipDragObstacleHexPrism = "Obstacle HexPrism";
				public const string TooltipDragObstacleCone = "Obstacle Cone";
				public const string TooltipDragObstacleCapsule = "Obstacle Capsule";

				public const string LabelSize = "Size";
				public const string TooltipSize = "Brush Size";
				public const string LabelStr = "Str";
				public const string TooltipStr = "Brush Strength";
				public const string LabelRot = "Rot";
				public const string TooltipRot = "Brush Rotation";

				public static readonly string[] Layers = { "1", "2", "3", "4" };
				public static readonly string[] LayerTooltips = { "Layer 1", "Layer 2", "Layer 3", "Layer 4" };

				public static readonly string[] Splats = { "R", "G", "B", "A" };
				public static readonly string[] SplatTooltips = { "Red Channel", "Green Channel", "Blue Channel", "Alpha Channel" };

				public static readonly string[] Blends = { "+", "=", "<", ">" };
				public static readonly string[] BlendTooltips = { "Additive", "Set Height", "Minimum", "Maximum" };
			}

			public static class Icons
			{
				// Built-in Unity Icons
				public const string Sculpt = "TerrainInspector.TerrainToolSculpt On";
				public const string Modifier = "SceneViewTools";
				public const string TerrainLayers = "Collab.Build";
				public const string Splat = "PreTextureRGB";
				public const string BlendMode = "AudioMixerController On Icon";
				public const string BrushSplat = "TerrainInspector.TerrainToolSplat On";
				public const string ScaleTool = "ScaleTool";
				public const string StrengthSlider = "Exposure";
				public const string RotateTool = "RotateTool";
				public const string DragItem = "Prefab Icon";
			}

			public static class Colors
			{
				public static readonly Color ActiveButton = new Color(0.3f, 0.5f, 0.8f, 0.5f);
				public static readonly Color Separator = new Color(0.5f, 0.5f, 0.5f, 0.5f);
			}
		}

		public EditorWindow containerWindow { get; set; }

		private ScrollView m_scrollView;
		private VisualElement m_sharedContainer;
		private VisualElement m_sculptContainer;
		private VisualElement m_modContainer;
		private List<VisualElement> m_separator = new List<VisualElement>();

		private Button m_btnModeSculpt;
		private Button m_btnModeMod;

		private ExclusiveButtonGrid<ErosionLayer.TerrainLayer> m_layerControl;
		private ExclusiveButtonGrid<ErosionLayer.SplatChannel> m_splatControl;
		private ExclusiveButtonGrid<FluidSimulation.FluidModifierBlendMode> m_blendControl;

		private List<ISizableElement> m_resizeables = new List<ISizableElement>();
		private bool m_isVerticalLayout = false;

		// Session State Keys
		private const string KEY_MODE = "LvlDes_Mode"; // true = Sculpt, false = Mod
		private const string KEY_LAYER = "LvlDes_Layer";
		private const string KEY_SPLAT = "LvlDes_Splat";
		private const string KEY_BLEND = "LvlDes_Blend";
		private const string KEY_SIZE = "LvlDes_Size";
		private const string KEY_STR = "LvlDes_Str";
		private const string KEY_ROT = "LvlDes_Rot";

		#region Public Accessors

		public static bool ModeIsSculpt => SessionState.GetBool(KEY_MODE, true);
		public static ErosionLayer.TerrainLayer CurrentLayer => (ErosionLayer.TerrainLayer)SessionState.GetInt(KEY_LAYER, (int)ErosionLayer.TerrainLayer.Layer1);
		public static ErosionLayer.SplatChannel CurrentSplat => (ErosionLayer.SplatChannel)SessionState.GetInt(KEY_SPLAT, (int)ErosionLayer.SplatChannel.R);
		public static FluidSimulation.FluidModifierBlendMode CurrentBlend => (FluidSimulation.FluidModifierBlendMode)SessionState.GetInt(KEY_BLEND, (int)FluidSimulation.FluidModifierBlendMode.Additive);
		public static float CurrentSize => SessionState.GetFloat(KEY_SIZE, 20f);
		public static float CurrentStrength => SessionState.GetFloat(KEY_STR, 0.5f);
		public static float CurrentRotation => SessionState.GetFloat(KEY_ROT, 0f);
		public static Quaternion CurrentRotationQuat => Quaternion.Euler(0, CurrentRotation, 0);

		#endregion

		public FluidToolbarElement()
		{
			// Initialize root element styling
			style.flexGrow = 1;
			style.flexShrink = 1;
			style.minWidth = 0;
			style.minHeight = 0;
			style.overflow = Overflow.Hidden;

			// Initialize the scroll view
			m_scrollView = new ScrollView();
			m_scrollView.style.flexGrow = 1;
			m_scrollView.style.minWidth = 0;
			m_scrollView.style.minHeight = 0;

			// Hide standard Unity scrollbars (we will handle scrolling logic)
			m_scrollView.verticalScrollerVisibility = ScrollerVisibility.Hidden;
			m_scrollView.horizontalScrollerVisibility = ScrollerVisibility.Hidden;

			// Register manual wheel event for custom scrolling behavior
			m_scrollView.RegisterCallback<WheelEvent>(OnScrollWheel, TrickleDown.TrickleDown);

			Add(m_scrollView);

			// Build the UI hierarchy
			CreateModeToggles();
			CreateSeparator();
			CreateSculptView();
			CreateModifiersView();
			CreateSeparator();
			CreateSharedView();

			// Load saved mode or default to Sculpt
			bool initialMode = SessionState.GetBool(KEY_MODE, true);
			SwitchMode(initialMode);

			// Register layout and attachment events
			RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
			RegisterCallback<AttachToPanelEvent>(OnAttach);
			RegisterCallback<DetachFromPanelEvent>(OnDetach);
		}

		#region Layout & Events

		private void OnAttach(AttachToPanelEvent evt)
		{
			if (evt.destinationPanel?.visualTree != null)
			{
				evt.destinationPanel.visualTree.RegisterCallback<GeometryChangedEvent>(OnParentResized);
				UpdateLayoutConstraints();
			}
		}

		private void OnDetach(DetachFromPanelEvent evt)
		{
			if (evt.originPanel?.visualTree != null)
			{
				evt.originPanel.visualTree.UnregisterCallback<GeometryChangedEvent>(OnParentResized);
			}
		}

		private void OnParentResized(GeometryChangedEvent evt)
		{
			UpdateLayoutConstraints();
		}

		private void UpdateLayoutConstraints()
		{
			if (panel == null || panel.visualTree == null) return;

			Rect windowRect = panel.visualTree.layout;
			Rect myRect = this.worldBound;

			if (m_isVerticalLayout)
			{
				// Constrain height in vertical mode to prevent overflowing the window
				float availableHeight = windowRect.height - myRect.y - 20f;
				style.maxHeight = Mathf.Max(availableHeight, 50f);
				style.maxWidth = StyleKeyword.None;
			}
			else
			{
				// Constrain width in horizontal mode
				float availableWidth = windowRect.width - myRect.x - 20f;
				style.maxWidth = Mathf.Max(availableWidth, 50f);
				style.maxHeight = StyleKeyword.None;
			}
		}

		private void OnScrollWheel(WheelEvent evt)
		{
			float scrollSpeed = 50f;

			if (m_scrollView.mode == ScrollViewMode.Vertical)
			{
				if (evt.delta.y != 0)
				{
					Vector2 newPos = m_scrollView.scrollOffset;
					newPos.y += evt.delta.y * scrollSpeed;
					m_scrollView.scrollOffset = newPos;
					evt.StopPropagation();
				}
			}
			else
			{
				// In horizontal mode, map vertical wheel delta to horizontal scroll
				if (evt.delta.y != 0)
				{
					Vector2 newPos = m_scrollView.scrollOffset;
					newPos.x += evt.delta.y * scrollSpeed;
					m_scrollView.scrollOffset = newPos;
					evt.StopPropagation();
				}
			}
		}

		private void OnGeometryChanged(GeometryChangedEvent evt)
		{
			// Determine orientation based on aspect ratio and width threshold
			bool isVertical = evt.newRect.width < evt.newRect.height && evt.newRect.width < 150;

			if (m_isVerticalLayout != isVertical || evt.oldRect.width == 0)
			{
				m_isVerticalLayout = isVertical;
				if (m_isVerticalLayout) ApplyVerticalLayout();
				else ApplyHorizontalLayout();
			}

			UpdateLayoutConstraints();
		}

		private void ApplyVerticalLayout()
		{
			style.flexDirection = FlexDirection.Column;
			m_scrollView.mode = ScrollViewMode.Vertical;

			// Reset horizontal offset to prevent content being hidden laterally
			m_scrollView.scrollOffset = new Vector2(0, m_scrollView.scrollOffset.y);

			var content = m_scrollView.contentContainer;
			content.style.flexDirection = FlexDirection.Column;
			content.style.flexWrap = Wrap.NoWrap;
			content.style.alignItems = Align.Stretch;

			// Vertical Settings: Fill width, do not shrink
			content.style.width = StyleKeyword.Auto;
			content.style.flexGrow = 1;
			content.style.flexShrink = 0;

			// Layout Children
			m_sharedContainer.style.flexDirection = FlexDirection.Column;
			m_sculptContainer.style.flexDirection = FlexDirection.Column;

			// Mod Container Vertical Layout
			m_modContainer.style.flexDirection = FlexDirection.Column;
			m_modContainer.style.flexWrap = Wrap.NoWrap;
			// Fix: Center items so drag icons don't stick to the left
			m_modContainer.style.alignItems = Align.Center;

			// Adjust Separators for vertical view
			foreach (var sep in m_separator)
			{
				sep.style.width = Length.Percent(Styles.Dimensions.SeparatorLengthPercent);
				sep.style.height = 1;
				sep.style.marginTop = 4;
				sep.style.marginBottom = 4;
				sep.style.marginLeft = 0;
				sep.style.marginRight = 0;
				// Ensure separator is centered within the vertical column
				sep.style.alignSelf = Align.Center;
			}

			// Adjust Mode Buttons
			// Fix: Use fixed width and center alignment instead of 100% stretch
			m_btnModeSculpt.style.width = Styles.Dimensions.ToggleWidthHorizontal;
			m_btnModeMod.style.width = Styles.Dimensions.ToggleWidthHorizontal;
			m_btnModeSculpt.style.alignSelf = Align.Center;
			m_btnModeMod.style.alignSelf = Align.Center;

			m_btnModeSculpt.style.flexShrink = 0;
			m_btnModeMod.style.flexShrink = 0;

			foreach (var el in m_resizeables) el.SetVertical();
		}

		private void ApplyHorizontalLayout()
		{
			style.flexDirection = FlexDirection.Row;
			m_scrollView.mode = ScrollViewMode.Horizontal;

			var content = m_scrollView.contentContainer;
			content.style.flexDirection = FlexDirection.Row;
			content.style.alignItems = Align.Center;
			content.style.flexWrap = Wrap.NoWrap;

			// Horizontal Settings: Fill width, do not shrink
			content.style.width = StyleKeyword.Auto;
			content.style.flexGrow = 1;
			content.style.flexShrink = 0;

			// Prevent children shrinking
			m_sharedContainer.style.flexShrink = 0;
			m_sculptContainer.style.flexShrink = 0;
			m_modContainer.style.flexShrink = 0;

			// Layout Children
			m_sharedContainer.style.flexDirection = FlexDirection.Row;
			m_sculptContainer.style.flexDirection = FlexDirection.Row;

			// Mod Container Horizontal Layout
			m_modContainer.style.flexDirection = FlexDirection.Row;
			m_modContainer.style.flexWrap = Wrap.NoWrap;
			// Reset alignment for horizontal flow
			m_modContainer.style.alignItems = Align.Stretch;

			// Adjust Separators for horizontal view
			foreach (var sep in m_separator)
			{
				sep.style.width = 1;
				sep.style.height = Styles.Dimensions.ToggleIconSize;
				sep.style.marginTop = 0;
				sep.style.marginBottom = 0;
				sep.style.marginLeft = 4;
				sep.style.marginRight = 4;
				sep.style.alignSelf = Align.Auto;
			}

			// Adjust Mode Buttons
			m_btnModeSculpt.style.width = Styles.Dimensions.ToggleWidthHorizontal;
			m_btnModeMod.style.width = Styles.Dimensions.ToggleWidthHorizontal;
			m_btnModeSculpt.style.flexShrink = 0;
			m_btnModeMod.style.flexShrink = 0;
			// Reset alignment to parent default (Center in this case)
			m_btnModeSculpt.style.alignSelf = Align.Auto;
			m_btnModeMod.style.alignSelf = Align.Auto;

			foreach (var el in m_resizeables) el.SetHorizontal();
		}

		#endregion

		#region UI Creation

		private void CreateModeToggles()
		{
			// Note: The click actions here also update SessionState inside SwitchMode
			m_btnModeSculpt = CreateToggleButton(Styles.Icons.Sculpt, Styles.Text.TooltipSculpt, () => SwitchMode(true));
			m_btnModeMod = CreateToggleButton(Styles.Icons.Modifier, Styles.Text.TooltipMod, () => SwitchMode(false));
			m_scrollView.Add(m_btnModeSculpt);
			m_scrollView.Add(m_btnModeMod);
		}

		private Button CreateToggleButton(string iconName, string tooltipText, Action onClick)
		{
			Button b = new Button(onClick);
			b.tooltip = tooltipText;
			b.style.height = Styles.Dimensions.BaseButtonHeight;
			b.style.flexShrink = 0;

			Image icon = new Image();
			icon.image = EditorGUIUtility.IconContent(iconName).image;
			icon.style.width = Styles.Dimensions.ToggleIconSize;
			icon.style.height = Styles.Dimensions.ToggleIconSize;
			icon.style.alignSelf = Align.Center;
			b.Add(icon);
			return b;
		}

		private void SwitchMode(bool isSculpt)
		{
			// Save State
			SessionState.SetBool(KEY_MODE, isSculpt);

			m_sculptContainer.style.display = isSculpt ? DisplayStyle.Flex : DisplayStyle.None;
			m_modContainer.style.display = isSculpt ? DisplayStyle.None : DisplayStyle.Flex;

			m_btnModeSculpt.style.backgroundColor = isSculpt ? new StyleColor(Styles.Colors.ActiveButton) : StyleKeyword.Null;
			m_btnModeMod.style.backgroundColor = !isSculpt ? new StyleColor(Styles.Colors.ActiveButton) : StyleKeyword.Null;
		}

		private void CreateSeparator()
		{
			VisualElement separator = new VisualElement();
			separator.style.backgroundColor = Styles.Colors.Separator;
			separator.style.flexShrink = 0;
			m_separator.Add(separator);
			m_scrollView.Add(separator);
		}

		private void CreateSharedView()
		{
			m_sharedContainer = new VisualElement();

			// Load saved layer (default to Layer 1)
			var savedLayer = (ErosionLayer.TerrainLayer)SessionState.GetInt(KEY_LAYER, (int)ErosionLayer.TerrainLayer.Layer1);

			m_layerControl = new ExclusiveButtonGrid<ErosionLayer.TerrainLayer>(
				Styles.Icons.TerrainLayers, Styles.Text.Layers,
				new[] { ErosionLayer.TerrainLayer.Layer1, ErosionLayer.TerrainLayer.Layer2, ErosionLayer.TerrainLayer.Layer3, ErosionLayer.TerrainLayer.Layer4 },
				savedLayer,
				(val) => {
					SessionState.SetInt(KEY_LAYER, (int)val);
					OnLayerChanged(val);
				},
				Styles.Text.TooltipLayer, Styles.Text.LayerTooltips
			);
			m_resizeables.Add(m_layerControl);
			m_sharedContainer.Add(m_layerControl);

			// Load saved splat channel (default to R)
			var savedSplat = (ErosionLayer.SplatChannel)SessionState.GetInt(KEY_SPLAT, (int)ErosionLayer.SplatChannel.R);

			m_splatControl = new ExclusiveButtonGrid<ErosionLayer.SplatChannel>(
				Styles.Icons.Splat, Styles.Text.Splats,
				new[] { ErosionLayer.SplatChannel.R, ErosionLayer.SplatChannel.G, ErosionLayer.SplatChannel.B, ErosionLayer.SplatChannel.A },
				savedSplat,
				(val) => SessionState.SetInt(KEY_SPLAT, (int)val),
				Styles.Text.TooltipSplat, Styles.Text.SplatTooltips
			);
			m_resizeables.Add(m_splatControl);
			m_sharedContainer.Add(m_splatControl);

			// Load saved blend mode (default to Additive)
			var savedBlend = (FluidSimulation.FluidModifierBlendMode)SessionState.GetInt(KEY_BLEND, (int)FluidSimulation.FluidModifierBlendMode.Additive);

			m_blendControl = new ExclusiveButtonGrid<FluidSimulation.FluidModifierBlendMode>(
				Styles.Icons.BlendMode, Styles.Text.Blends,
				new[] { FluidSimulation.FluidModifierBlendMode.Additive, FluidSimulation.FluidModifierBlendMode.Set, FluidSimulation.FluidModifierBlendMode.Minimum, FluidSimulation.FluidModifierBlendMode.Maximum },
				savedBlend,
				(val) => SessionState.SetInt(KEY_BLEND, (int)val),
				Styles.Text.TooltipBlend, Styles.Text.BlendTooltips
			);
			m_resizeables.Add(m_blendControl);
			m_sharedContainer.Add(m_blendControl);

			// Brush Size
			float initialSize = SessionState.GetFloat(KEY_SIZE, 20f);
			var sizeSl = new FilledSlider(Styles.Icons.ScaleTool, 1, 100, initialSize,
				Styles.Text.LabelSize, Styles.Text.TooltipSize, (v) => SessionState.SetFloat(KEY_SIZE, v));
			m_resizeables.Add(sizeSl);
			m_sharedContainer.Add(sizeSl);

			// Brush Strength
			float initialStr = SessionState.GetFloat(KEY_STR, 0.5f);
			var strSl = new FilledSlider(Styles.Icons.StrengthSlider, 0, 100, initialStr,
				Styles.Text.LabelStr, Styles.Text.TooltipStr, (v) => SessionState.SetFloat(KEY_STR, v));
			m_resizeables.Add(strSl);
			m_sharedContainer.Add(strSl);

			// Brush Rotation
			float initialRot = SessionState.GetFloat(KEY_ROT, 0f);
			var rotSl = new FilledSlider(Styles.Icons.RotateTool, 0, 360, initialRot,
				Styles.Text.LabelRot, Styles.Text.TooltipRot, (v) => SessionState.SetFloat(KEY_ROT, v));
			m_resizeables.Add(rotSl);
			m_sharedContainer.Add(rotSl);

			// Trigger logic to enable/disable Splat control based on the loaded layer
			OnLayerChanged(savedLayer);

			m_scrollView.Add(m_sharedContainer);
		}

		private void CreateSculptView()
		{
			m_sculptContainer = new VisualElement();
			var brushWrapper = new DropdownPopupPanel(EditorGUIUtility.IconContent(Styles.Icons.BrushSplat), Styles.Text.TooltipBrushSelect, new GUIContent(Styles.Text.TooltipBrushSelect));
			m_resizeables.Add(brushWrapper);
			m_sculptContainer.Add(brushWrapper.Root);
			m_scrollView.Add(m_sculptContainer);
		}

		private void OnLayerChanged(ErosionLayer.TerrainLayer layer)
		{
			bool isLayer1 = layer == ErosionLayer.TerrainLayer.Layer1;
			m_splatControl.SetEnabled(isLayer1);
		}

		private void CreateModifiersView()
		{
			m_modContainer = new VisualElement();
			m_modContainer.style.display = DisplayStyle.None;

			// Add drag-and-drop buttons with specific tooltips
			m_modContainer.Add(DragAndDropButton.Create(CreateFluidSource, FluidEditorIcons.toolbar_mod_fluid_source, Styles.Text.TooltipDragFluidSource));
			m_modContainer.Add(DragAndDropButton.Create(CreateFluidForce, FluidEditorIcons.toolbar_mod_fluid_force, Styles.Text.TooltipDragFluidForce));
			m_modContainer.Add(DragAndDropButton.Create(CreateFluidCurrent, FluidEditorIcons.toolbar_mod_fluid_current, Styles.Text.TooltipDragFluidCurrent));
			m_modContainer.Add(DragAndDropButton.Create(CreateFluidVortex, FluidEditorIcons.toolbar_mod_fluid_vortex, Styles.Text.TooltipDragFluidVortex));

			m_modContainer.Add(DragAndDropButton.Create(CreateTerrainModifier, FluidEditorIcons.toolbar_mod_terrain_source, Styles.Text.TooltipDragTerrainMod));
			m_modContainer.Add(DragAndDropButton.Create(CreateTerraformModifier, FluidEditorIcons.toolbar_mod_terraform_transform, Styles.Text.TooltipDragTerraformMod));

			m_modContainer.Add(DragAndDropButton.Create(CreateObstacleCube, FluidEditorIcons.toolbar_obstacle_cube, Styles.Text.TooltipDragObstacleCube));
			m_modContainer.Add(DragAndDropButton.Create(CreateObstacleSphere, FluidEditorIcons.toolbar_obstacle_sphere, Styles.Text.TooltipDragObstacleSphere));
			m_modContainer.Add(DragAndDropButton.Create(CreateObstacleCylinder, FluidEditorIcons.toolbar_obstacle_cylider, Styles.Text.TooltipDragObstacleCylinder));

			m_modContainer.Add(DragAndDropButton.Create(CreateObstacleElipse, FluidEditorIcons.toolbar_obstacle_elipse, Styles.Text.TooltipDragObstacleElipse));
			m_modContainer.Add(DragAndDropButton.Create(CreateObstacleWedge, FluidEditorIcons.toolbar_obstacle_wedge, Styles.Text.TooltipDragObstacleWedge));
			m_modContainer.Add(DragAndDropButton.Create(CreateObstacleHexPrism, FluidEditorIcons.toolbar_obstacle_hexprism, Styles.Text.TooltipDragObstacleHexPrism));
			m_modContainer.Add(DragAndDropButton.Create(CreateObstacleCone, FluidEditorIcons.toolbar_obstacle_cone, Styles.Text.TooltipDragObstacleCone));
			m_modContainer.Add(DragAndDropButton.Create(CreateObstacleCapsule, FluidEditorIcons.toolbar_obstacle_capsule, Styles.Text.TooltipDragObstacleCapsule));
			m_scrollView.Add(m_modContainer);
		}

		#endregion

		#region Modifier Creation Handlers

		private void CreateFluidSource(Vector3 position, Transform parent)
		{
			GameObject go = FluidEditorUtils.CreateFluidModifierSource(position, CurrentRotationQuat, Vector3.one * CurrentSize, CurrentStrength, (int)CurrentLayer);
			if (parent != null) go.transform.SetParent(parent);
			Selection.activeObject = go;
		}

		private void CreateFluidForce(Vector3 position, Transform parent)
		{

			GameObject go = FluidEditorUtils.CreateFluidModifierForce(position, CurrentRotationQuat, Vector3.one * CurrentSize, CurrentStrength);
			if (parent != null) go.transform.SetParent(parent);
			Selection.activeObject = go;
		}

		private void CreateFluidCurrent(Vector3 position, Transform parent)
		{
			GameObject go = FluidEditorUtils.CreateFluidModifierFlow(position, CurrentRotationQuat, Vector3.one * CurrentSize, CurrentStrength);
			if (parent != null) go.transform.SetParent(parent);
			Selection.activeObject = go;
		}

		private void CreateFluidVortex(Vector3 position, Transform parent)
		{
			GameObject go = FluidEditorUtils.CreateFluidModifierVortex(position, CurrentRotationQuat, Vector3.one * CurrentSize);
			if (parent != null) go.transform.SetParent(parent);
			Selection.activeObject = go;
		}

		private void CreateTerrainModifier(Vector3 position, Transform parent)
		{
			GameObject go = FluidEditorUtils.CreateTerrainModifier(position, CurrentRotationQuat, Vector3.one * CurrentSize, CurrentStrength, CurrentLayer, CurrentSplat);
			if (parent != null) go.transform.SetParent(parent);
			go.transform.position = position;
			Selection.activeObject = go;
		}

		private void CreateTerraformModifier(Vector3 position, Transform parent)
		{
			GameObject go = FluidEditorUtils.CreateTerraformModifier(position, CurrentRotationQuat, Vector3.one * CurrentSize);
			if (parent != null) go.transform.SetParent(parent);
			Selection.activeObject = go;
		}

		private void CreateObstacleCube(Vector3 position, Transform parent)
		{
			GameObject go = FluidEditorUtils.CreateObstacleBox(position, CurrentRotationQuat, Vector3.one * CurrentSize);
			if (parent != null) go.transform.SetParent(parent);
			Selection.activeObject = go;
		}

		private void CreateObstacleSphere(Vector3 position, Transform parent)
		{
			GameObject go = FluidEditorUtils.CreateObstacleSphere(position, CurrentRotationQuat, CurrentSize * 0.5f);
			if (parent != null) go.transform.SetParent(parent);
			Selection.activeObject = go;
		}

		private void CreateObstacleCylinder(Vector3 position, Transform parent)
		{
			GameObject go = FluidEditorUtils.CreateObstacleCylinder(position, CurrentRotationQuat, CurrentSize * 0.5f, CurrentSize);
			if (parent != null) go.transform.SetParent(parent);
			Selection.activeObject = go;
		}

		private void CreateObstacleElipse(Vector3 position, Transform parent)
		{
			GameObject go = FluidEditorUtils.CreateObstacleElipse(position, CurrentRotationQuat, Vector3.one * CurrentSize * 0.5f);
			if (parent != null) go.transform.SetParent(parent);
			Selection.activeObject = go;
		}

		private void CreateObstacleWedge(Vector3 position, Transform parent)
		{
			GameObject go = FluidEditorUtils.CreateObstacleWedge(position, CurrentRotationQuat, Vector3.one * CurrentSize);
			if (parent != null) go.transform.SetParent(parent);
			Selection.activeObject = go;
		}

		private void CreateObstacleHexPrism(Vector3 position, Transform parent)
		{
			GameObject go = FluidEditorUtils.CreateObstacleHexPrism(position, CurrentRotationQuat, CurrentSize * 0.5f, CurrentSize);
			if (parent != null) go.transform.SetParent(parent);
			Selection.activeObject = go;
		}

		private void CreateObstacleCone(Vector3 position, Transform parent)
		{
			GameObject go = FluidEditorUtils.CreateObstacleCone(position, CurrentRotationQuat, CurrentSize * 0.5f, CurrentSize);
			if (parent != null) go.transform.SetParent(parent);
			Selection.activeObject = go;
		}

		private void CreateObstacleCapsule(Vector3 position, Transform parent)
		{
			GameObject go = FluidEditorUtils.CreateObstacleCapsule(position, CurrentRotationQuat, CurrentSize * 0.5f, CurrentSize);
			if (parent != null) go.transform.SetParent(parent);
			Selection.activeObject = go;
		}

		#endregion
	}

	[Overlay(typeof(SceneView), "Fluid Frenzy/Toolbar")]
	public class LevelDesignOverlay : ToolbarOverlay
	{
		public LevelDesignOverlay() : base(FluidToolbarElement.ToolbarId) { }
	}
}