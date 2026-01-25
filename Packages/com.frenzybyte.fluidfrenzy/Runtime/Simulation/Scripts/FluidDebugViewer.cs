using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace FluidFrenzy
{
	public class FluidDebugViewer : MonoBehaviour
	{
		// Static variable to control UI element height for easy tuning.
		// Default is 30f, which is 25% smaller than the previous 40f.
		private static float s_elementHeight = 30f;

		public Material m_debugMaterial;
		public bool m_guiVisible = true;

		// UI State
		private FluidSimulation m_targetSimulation;
		private Vector2 m_selectionScrollPos;
		private Vector2 m_panPosition = Vector2.zero;
		private float m_zoom = 1.0f;
		private Vector3 m_levels = new Vector3(0, 1, 1); // x=min, y=max, z=gamma
		private int m_selectedChannelIndex = 0;
		private readonly string[] m_channelOptions = { "RGB", "Red", "Green", "Blue", "Alpha" };
		private bool m_autoLevels = true;

		// Buffer State
		private FluidSimulation.DebugBuffer m_selectedBuffer;
		private List<FluidSimulation.DebugBuffer> m_availableBuffers;
		private bool m_isInitialized = false;

		// UI Styles for larger controls
		private GUIStyle m_largeButton;
		private GUIStyle m_largeLabel;
		private GUIStyle m_largeToggle;
		private GUIStyle m_largeToolbar;
		private bool m_stylesInitialized = false;

		void Start()
		{
			Initialize();
		}

		private void Initialize()
		{
			if (FluidSimulationManager.simulations.Count > 0)
			{
				m_targetSimulation = FluidSimulationManager.simulations[0];
			}

			if (m_targetSimulation == null)
			{
				Debug.LogWarning("FluidDebugViewer: Target Simulator not found via FluidSimulationManager.", this);
				return;
			}

			m_availableBuffers = new List<FluidSimulation.DebugBuffer>(m_targetSimulation.EnumerateBuffers());
			if (m_availableBuffers.Count > 0)
			{
				m_selectedBuffer = m_availableBuffers[0];
			}
			m_isInitialized = true;
		}

		private void InitializeStyles()
		{
			// Re-initialize styles if the static height variable has changed at runtime
			if (m_stylesInitialized && m_largeButton.fixedHeight == s_elementHeight) return;

			float elementHeight = s_elementHeight;

			m_largeButton = new GUIStyle(GUI.skin.button);
			m_largeButton.fixedHeight = elementHeight;
			m_largeButton.fontSize = (int)(elementHeight * 0.45f);

			m_largeLabel = new GUIStyle(GUI.skin.label);
			m_largeLabel.fixedHeight = elementHeight;
			m_largeLabel.alignment = TextAnchor.MiddleLeft;
			m_largeLabel.fontSize = (int)(elementHeight * 0.45f);

			m_largeToggle = new GUIStyle(GUI.skin.toggle);
			m_largeToggle.fixedHeight = elementHeight;
			m_largeToggle.fontSize = (int)(elementHeight * 0.45f);

			m_largeToolbar = new GUIStyle(GUI.skin.button);
			m_largeToolbar.fixedHeight = elementHeight;
			m_largeToolbar.fontSize = (int)(elementHeight * 0.4f);

			m_stylesInitialized = true;
		}

		void OnGUI()
		{
			InitializeStyles(); // Create/update styles on each OnGUI call
			DrawToggleButton();

			if (!m_isInitialized || !m_guiVisible)
			{
				if (m_targetSimulation == null && FluidSimulationManager.simulations.Count > 0) Initialize();
				return;
			}

			float leftPanelWidth = Screen.width * 0.2f;
			Rect leftPanelRect = new Rect(0, 0, leftPanelWidth, Screen.height);
			Rect rightPanelRect = new Rect(leftPanelWidth, 0, Screen.width - leftPanelWidth, Screen.height);

			DrawLeftPanel(leftPanelRect);
			DrawRightPanel(rightPanelRect);
		}

		private void DrawToggleButton()
		{
			string buttonText = m_guiVisible ? "Hide" : "Debug";
			Rect buttonRect = new Rect(Screen.width - (s_elementHeight * 2.2f), 10, s_elementHeight * 2f, s_elementHeight);
			if (GUI.Button(buttonRect, buttonText, m_largeButton))
			{
				m_guiVisible = !m_guiVisible;
			}
		}

		private void DrawLeftPanel(Rect panelRect)
		{
			GUILayout.BeginArea(panelRect, GUI.skin.box);
			GUILayout.Label("Debug Buffers", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, fontSize = (int)(s_elementHeight * 0.5f) });

			m_selectionScrollPos = GUILayout.BeginScrollView(m_selectionScrollPos);
			foreach (var buffer in m_availableBuffers)
			{
				if (GUILayout.Button(buffer.ToString(), m_largeButton))
				{
					m_selectedBuffer = buffer;
				}
			}
			GUILayout.EndScrollView();
			GUILayout.EndArea();
		}

		private void DrawRightPanel(Rect panelRect)
		{
			GUILayout.BeginArea(panelRect);

			float controlsHeight = s_elementHeight * 3;
			Rect controlsRect = new Rect(10, 10, panelRect.width - 80, controlsHeight);
			DrawControls(controlsRect);

			float availableHeight = panelRect.height - controlsRect.yMax - (panelRect.height * 0.075f) - 5;
			float viewSize = Mathf.Min(panelRect.width - 20, availableHeight);
			Rect viewRect = new Rect(10, controlsRect.yMax + 5, viewSize, viewSize);

			RenderTexture textureToDraw = m_targetSimulation.GetDebugBuffer(m_selectedBuffer);
			if (textureToDraw != null)
			{
				HandleInput(viewRect);
				DrawTextureWithControls(viewRect, textureToDraw);
			}

			GUILayout.EndArea();
		}

		private void HandleInput(Rect viewRect)
		{
			if (Input.touchCount > 0)
			{
				if (Input.touchCount == 1)
				{
					Touch touch = Input.GetTouch(0);
					if (viewRect.Contains(touch.position) && touch.phase == TouchPhase.Moved)
					{
						Vector2 panDelta = touch.deltaPosition;
						panDelta.y *= -1;
						panDelta /= viewRect.size * Mathf.Sqrt(m_zoom);
						m_panPosition -= panDelta;
					}
				}
				else if (Input.touchCount == 2)
				{
					Touch touchZero = Input.GetTouch(0);
					Touch touchOne = Input.GetTouch(1);

					if (viewRect.Contains(touchZero.position) || viewRect.Contains(touchOne.position))
					{
						Vector2 touchZeroPrevPos = touchZero.position - touchZero.deltaPosition;
						Vector2 touchOnePrevPos = touchOne.position - touchOne.deltaPosition;
						float prevMagnitude = (touchZeroPrevPos - touchOnePrevPos).magnitude;
						float currentMagnitude = (touchZero.position - touchOne.position).magnitude;
						float deltaMagnitudeDiff = currentMagnitude - prevMagnitude;
						m_zoom = Mathf.Clamp(m_zoom + deltaMagnitudeDiff * 0.01f * m_zoom, 1.0f, 32.0f);
					}
				}
			}
			else
			{
				Event e = Event.current;
				if (viewRect.Contains(e.mousePosition))
				{
					if (e.type == EventType.ScrollWheel)
					{
						float zoomDelta = -e.delta.y * 0.05f * m_zoom;
						m_zoom = Mathf.Clamp(m_zoom + zoomDelta, 1.0f, 32.0f);
						e.Use();
					}
					else if (e.type == EventType.MouseDrag && e.button == 0)
					{
						Vector2 panDelta = e.delta;
						panDelta.y *= -1;
						panDelta /= viewRect.size * Mathf.Sqrt(m_zoom);
						m_panPosition -= panDelta;
						e.Use();
					}
				}
			}
		}

		private void DrawTextureWithControls(Rect viewRect, Texture texture)
		{
			if (Event.current.type != EventType.Repaint)
			{
				return;
			}
			Vector4 channelMask = Vector4.zero;
			if (m_selectedChannelIndex == 0) channelMask = new Vector4(1, 1, 1, 0); // RGB
			else channelMask[m_selectedChannelIndex - 1] = 1.0f;

			Vector3 levels = m_autoLevels ? m_targetSimulation.GetDebugLevels(m_selectedBuffer) : m_levels;
			m_debugMaterial.SetVector("_ColorSelection", channelMask);
			m_debugMaterial.SetVector("_Levels", levels);

			float panRange = Mathf.Max(0, (1.0f - 1.0f / m_zoom) * 0.5f);
			m_panPosition.x = Mathf.Clamp(m_panPosition.x, -panRange, panRange);
			m_panPosition.y = Mathf.Clamp(m_panPosition.y, -panRange, panRange);

			Rect uvRect = new Rect(0, 0, 1, 1);
			uvRect.width /= m_zoom;
			uvRect.height /= m_zoom;
			uvRect.x = m_panPosition.x + (1.0f - uvRect.width) * 0.5f;
			uvRect.y = m_panPosition.y + (1.0f - uvRect.height) * 0.5f;

			GUI.BeginGroup(viewRect);
			Rect localDrawRect = new Rect(0, 0, viewRect.width, viewRect.height);
			Graphics.DrawTexture(localDrawRect, texture, uvRect, 0, 0, 0, 0, m_debugMaterial);
			GUI.EndGroup();
		}

		private void DrawControls(Rect controlsRect)
		{
			GUILayout.BeginArea(controlsRect, GUI.skin.box);

			// First Row: Levels and Gamma
			GUILayout.BeginHorizontal();
			GUI.enabled = !m_autoLevels; // Sliders are disabled if autolevels is checked
			GUILayout.Label("Levels:", GUILayout.Width(50));
			m_levels.x = GUILayout.HorizontalSlider(m_levels.x, 0.0f, 10.0f);
			m_levels.y = GUILayout.HorizontalSlider(m_levels.y, 0.0f, 10.0f);
			GUI.enabled = true; // Re-enable for other controls
			GUILayout.Label("Gamma:", GUILayout.Width(50));
			m_levels.z = GUILayout.HorizontalSlider(m_levels.z, 0.1f, 5.0f);
			GUILayout.EndHorizontal();

			// Second Row: Channels
			GUILayout.BeginHorizontal();
			GUILayout.Label("Channels:", GUILayout.Width(60));
			m_selectedChannelIndex = GUILayout.Toolbar(m_selectedChannelIndex, m_channelOptions);
			GUILayout.EndHorizontal();

			// Third Row: Reset and Autolevels
			GUILayout.BeginHorizontal();
			if (GUILayout.Button("Reset View"))
			{
				m_panPosition = Vector2.zero;
				m_zoom = 1.0f;
				m_levels = new Vector3(0, 1, 1);
				m_autoLevels = false;
			}
			m_autoLevels = GUILayout.Toggle(m_autoLevels, "Autolevels");
			GUILayout.EndHorizontal();

			GUILayout.EndArea();
		}
	}
}