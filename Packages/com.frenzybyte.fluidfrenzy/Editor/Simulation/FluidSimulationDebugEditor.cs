using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System;
using System.Text.RegularExpressions;
using System.Linq;

namespace FluidFrenzy.Editor
{
    public class FluidSimulationDebugEditor : EditorWindow
    {
		[Flags]
		enum ColorChannels
		{
			Red = 1,
			Green = 2,
			Blue = 4,
			Alpha = 8
		}

		class Styles
		{
			public static int kSimulationLabelHeight = 20;
			public static int kBufferLabelHeight = 20;
			public static int kToolbarHeight = 24;

			public static GUIContent sortIcon = EditorGUIUtility.IconContent("AlphabeticalSorting");
			public static GUIContent fluidSimulationText = new GUIContent("Fluid Simulation", "Select which Fluid Simulation to debug.");
			public static GUIContent repaintText = new GUIContent("Live", "Update the window every frame.");
			public static GUIContent bufferText = new GUIContent("Buffer", "Select which Fluid Simulation Buffer to debug.");
			public static GUIContent infoText = new GUIContent("Info", "Show information about the selected buffer.");
			public static GUIContent simulationInfoText = new GUIContent("Simulation Info", "Show information about the selected simulation.");
			public static GUIContent bufferInfoText = new GUIContent("Buffer Info", "Show information about the selected buffer.");
			public static GUIContent scaleText = new GUIContent("Scale", "Scale the texture view.");
			public static GUIContent channelsText = new GUIContent("Channels", "Select which channel of the displayed fluid simulation buffer.");
			public static GUIContent levelsText = new GUIContent("Auto Levels", "Toggle Auto Levels to change the visible range of the displayed fluid simulation buffer.");
			public static GUIContent gammaText = new GUIContent("Gamma", "Change the gamma curve of the displayed fluid simulation buffer.");

			public static GUIContent channelAllText = new GUIContent("All", "Show all channels.");
			public static GUIContent channelRText = new GUIContent("R", "Show the red channel.");
			public static GUIContent channelGText = new GUIContent("G", "Show the green channel.");
			public static GUIContent channelBText = new GUIContent("B", "Show the blue channel.");
			public static GUIContent channelAText = new GUIContent("A", "Show the alpha channel.");


			public static Color selectionColor = new Color32(0xAC, 0xCE, 0xF7, 0xFF);


			public static Color selectionAreaColor = new Color32(0x38, 0x38, 0x38, 0xFF);
			public static Vector2 selectionAreaSize = new Vector2(0.25f, 1.0f);
			public static Vector2 simulationListSize = new Vector2(1.0f, 1.0f / 4.0f);
			public static Vector2 bufferListSize = new Vector2(1.0f, 1.0f / 3.0f);
			public static Vector2 bufferViewerAreaSize = new Vector2(0.75f, 1.0f);

			public static GUIStyle levelsSlider = new GUIStyle(EditorStyles.toolbarButton)
			{
				margin = new RectOffset(4, 4, 0, 0),
				padding = new RectOffset(4, 4, 0, 0),
				fixedHeight = kToolbarHeight - 1
			};

			public static GUIStyle toolbarLeft = new GUIStyle(GUI.skin.FindStyle("toolbarbuttonRight"))
			{
				fixedHeight = kToolbarHeight - 1
			};

			public static GUIStyle toolbar = new GUIStyle(EditorStyles.toolbar)
			{
				fixedHeight = kToolbarHeight
			};

			public static GUIStyle channelsLabel = GUI.skin.FindStyle("ToolbarLabel");
			public static GUIStyle itemStyle = new GUIStyle(GUI.skin.button)
			{

				alignment = TextAnchor.MiddleLeft, //align text to the left
				margin = new RectOffset(0, 0, 0, 0) //removes the space between items (previously there was a small gap between GUI which made it harder to select a desired item)
			};

			static Styles()
			{
				itemStyle.active.background = itemStyle.normal.background; //gets rid of button click background style.
			}
		}


		string m_searchQuery = string.Empty;
		bool m_sortAlphabetical = false;

		bool m_repaint = true;
		Vector2 m_fluidSimulationScrollbar;
		FluidSimulation m_selectedSimulation = null;

		Vector2 m_simulationBufferScrollbar;
		FluidSimulation.DebugBuffer m_selectedDebugBuffer = FluidSimulation.DebugBuffer.Fluid;
		RenderTexture m_simulationBuffer;
		ColorChannels m_selectedChannel = ColorChannels.Red | ColorChannels.Green | ColorChannels.Blue | ColorChannels.Alpha;

		bool m_autoLevels = true;
		Vector2 m_levelsRange = new Vector2(0,1);
		Vector3 m_levels = new Vector3(0,1,1);
		Vector2 m_textureScrollbar;
		float m_textureScale = 1;

		Material m_debuggerMaterial;

		[MenuItem("Window/Fluid Frenzy/Debugger", priority = -1)]
        public static void ShowWindow()
        {
			// Create and show the editor window.
			FluidSimulationDebugEditor window = GetWindow<FluidSimulationDebugEditor>("Fluid Simulation Debug");
            window.Show();
        }

		public void SetSimulation(FluidSimulation simulation)
		{
			m_selectedSimulation = simulation;
		}

		private void OnEnable()
		{
			m_debuggerMaterial = new Material(Shader.Find("Hidden/FluidFrenzy/Debug/FluidSimulationDebugger"));
		}


		private void OnGUI()
        {
			float windowWidth = position.width;
			float windowHeight = position.height;
			// Draw top welcome label.
			Color color_default = GUI.backgroundColor;

			Rect selectionListArea = new Rect(0, 0, windowWidth * Styles.selectionAreaSize.x, windowHeight * Styles.selectionAreaSize.y);
			Rect bufferViewArea = new Rect(selectionListArea.width-1, 0, windowWidth * Styles.bufferViewerAreaSize.x, windowHeight * Styles.bufferViewerAreaSize.y);

			using (var area = new GUILayout.AreaScope(selectionListArea, GUIContent.none))
			{
				using (var scope = new GUILayout.VerticalScope())
				{
					using (var scope1 = new GUILayout.HorizontalScope(Styles.toolbar))
					{
						using (var scope2 = new GUILayout.HorizontalScope(Styles.toolbarLeft))
						{
							m_searchQuery = GUILayout.TextField(m_searchQuery, EditorStyles.toolbarSearchField, GUILayout.Width(200));
							GUI.backgroundColor = m_sortAlphabetical ? Styles.selectionColor : color_default;
							if (GUILayout.Button(Styles.sortIcon, EditorStyles.toolbarButton, GUILayout.Width(32))) 
							{
								m_sortAlphabetical = !m_sortAlphabetical;
							}
							GUI.backgroundColor = color_default;
						}
					}

					Rect listBorder = selectionListArea;
					listBorder.y = Styles.kToolbarHeight; listBorder.height -= Styles.kToolbarHeight;
					EditorGUI.DrawRect(listBorder, Color.black);

					Rect listBackground = listBorder;
					listBackground.width -= 1;
					EditorGUI.DrawRect(listBackground, Styles.selectionAreaColor);

					var myRegex = new Regex(m_searchQuery, RegexOptions.IgnoreCase);
					List<FluidSimulation> searchedSimulations = new List<FluidSimulation>(FluidSimulationManager.simulations);

					if (m_searchQuery.Length != 0)
						searchedSimulations = FluidSimulationManager.simulations.FindAll(delegate (FluidSimulation s) { return myRegex.IsMatch(s.name); });

					if (m_sortAlphabetical)
						searchedSimulations = searchedSimulations.OrderBy(r => r.gameObject.name).ToList();

					GUILayout.Space(2);
					GUILayout.Label(Styles.fluidSimulationText, EditorStyles.boldLabel);
					using (var scroll = new GUILayout.ScrollViewScope(m_fluidSimulationScrollbar, EditorStyles.helpBox, GUILayout.Height(windowHeight * Styles.simulationListSize.y)))
					{
						m_fluidSimulationScrollbar = scroll.scrollPosition;
						for (int i = 0; i < searchedSimulations.Count; i++)
						{
							GUI.backgroundColor = (m_selectedSimulation == searchedSimulations[i]) ? Styles.selectionColor : Color.clear;

							if (GUILayout.Button(searchedSimulations[i].name, Styles.itemStyle))
							{
								m_selectedSimulation = searchedSimulations[i];
							}
						}
						GUI.backgroundColor = color_default;
					}


					GUILayout.Space(1);

					GUILayout.Label(Styles.bufferText, EditorStyles.boldLabel);
					using (var scroll = new GUILayout.ScrollViewScope(m_simulationBufferScrollbar, EditorStyles.helpBox, GUILayout.Height(windowHeight * Styles.bufferListSize.y)))
					{
						m_simulationBufferScrollbar = scroll.scrollPosition;
						if (m_selectedSimulation)
						{
							foreach (FluidSimulation.DebugBuffer debugBuffer in m_selectedSimulation?.EnumerateBuffers())
							{
								GUI.backgroundColor = (m_selectedDebugBuffer == debugBuffer) ? Styles.selectionColor : Color.clear;

								if (GUILayout.Button(debugBuffer.ToString(), Styles.itemStyle))
								{
									m_selectedDebugBuffer = debugBuffer;
								}
							}
						}
						GUI.backgroundColor = color_default;
					}
					m_simulationBuffer = m_selectedSimulation?.GetDebugBuffer(m_selectedDebugBuffer);
					GUILayout.Space(1);

					GUILayout.Label(Styles.infoText, EditorStyles.boldLabel);
					using (var scroll = new GUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.ExpandHeight(true)))
					{
						if (m_selectedSimulation)
						{
							GUILayout.Label(Styles.simulationInfoText, EditorStyles.boldLabel);
							GUILayout.Label($"Name: \t\t{m_selectedSimulation.name}");
							GUILayout.Label($"Type: \t\t{m_selectedSimulation.simulationType}");
							GUILayout.Label($"Terrain Type: \t{m_selectedSimulation.terrainType}");
							GUILayout.Label($"Dimensions : \t{m_selectedSimulation.dimension.x}x{m_selectedSimulation.dimension.y}");
							GUILayout.Space(20);
						}
						if (m_simulationBuffer)
						{
							GUILayout.Label(Styles.bufferInfoText, EditorStyles.boldLabel);
							GUILayout.Label($"Name: \t\t{m_simulationBuffer.name}");
							GUILayout.Label($"Resolution: \t{m_simulationBuffer.width}x{m_simulationBuffer.height}");
							GUILayout.Label($"Format: \t\t{m_simulationBuffer.graphicsFormat}");
						}
					}
				}
			}



			using (var area = new GUILayout.AreaScope(bufferViewArea, GUIContent.none))
			{
				using (var scope1 = new GUILayout.HorizontalScope(Styles.toolbar))
				{
					using (var scope2 = new GUILayout.HorizontalScope(Styles.toolbarLeft))
					{
						GUI.backgroundColor = m_repaint ? Styles.selectionColor : color_default;
						if (GUILayout.Button(Styles.repaintText, EditorStyles.miniButtonLeft, GUILayout.Width(40))) { m_repaint = !m_repaint; }
						GUI.backgroundColor = color_default;
					}
					using (var scope2 = new GUILayout.HorizontalScope(Styles.levelsSlider))
					{
						GUILayout.Label(Styles.channelsText, Styles.channelsLabel);
						if (GUILayout.Button(Styles.channelAllText, EditorStyles.miniButtonLeft, GUILayout.Width(32))) { m_selectedChannel = ColorChannels.Red | ColorChannels.Green | ColorChannels.Blue | ColorChannels.Alpha; }

						GUI.backgroundColor = ((m_selectedChannel & ColorChannels.Red) != 0) ? Styles.selectionColor : color_default;
						if (GUILayout.Button(Styles.channelRText, EditorStyles.miniButtonMid, GUILayout.Width(32))) { m_selectedChannel = ColorChannels.Red; }

						GUI.backgroundColor = ((m_selectedChannel & ColorChannels.Green) != 0) ? Styles.selectionColor : color_default;
						if (GUILayout.Button(Styles.channelGText, EditorStyles.miniButtonMid, GUILayout.Width(32))) { m_selectedChannel = ColorChannels.Green; }

						GUI.backgroundColor = ((m_selectedChannel & ColorChannels.Blue) != 0) ? Styles.selectionColor : color_default;
						if (GUILayout.Button(Styles.channelBText, EditorStyles.miniButtonMid, GUILayout.Width(32))) { m_selectedChannel = ColorChannels.Blue; }

						GUI.backgroundColor = ((m_selectedChannel & ColorChannels.Alpha) != 0) ? Styles.selectionColor : color_default;
						if (GUILayout.Button(Styles.channelAText, EditorStyles.miniButtonRight, GUILayout.Width(32))) { m_selectedChannel = ColorChannels.Alpha; }

						GUI.backgroundColor = color_default;
					}

					using (var scope2 = new GUILayout.HorizontalScope(Styles.levelsSlider))
					{
						GUILayout.Label(Styles.scaleText, Styles.channelsLabel);
						m_textureScale = EditorGUILayout.Slider(GUIContent.none, m_textureScale, 0.25f, 10, GUILayout.Width(150));
					}

					using (var scope2 = new GUILayout.HorizontalScope(Styles.levelsSlider))
					{
						GUILayout.Label(Styles.levelsText, Styles.channelsLabel);
						m_autoLevels = EditorGUILayout.Toggle(m_autoLevels, GUILayout.Width(20));

						GUI.enabled = !m_autoLevels;
						m_levelsRange.x = EditorGUILayout.DelayedFloatField(m_levelsRange.x, GUILayout.Width(32));
						EditorGUILayout.MinMaxSlider(GUIContent.none, ref m_levels.x, ref m_levels.y, m_levelsRange.x, m_levelsRange.y, GUILayout.Width(200));
						m_levelsRange.y = EditorGUILayout.DelayedFloatField(m_levelsRange.y, GUILayout.Width(32));
						GUILayout.Label(Styles.gammaText, Styles.channelsLabel);
						m_levels.z = EditorGUILayout.FloatField(m_levels.z, GUILayout.Width(32));

						GUI.enabled = true;

					}
					GUILayout.FlexibleSpace();
				}

				m_levelsRange.x = Mathf.Clamp(m_levelsRange.x, 0, m_levelsRange.y);
				m_levelsRange.y = Mathf.Clamp(m_levelsRange.y, m_levelsRange.x, 1000);

				m_levels.x = Mathf.Clamp(m_levels.x, m_levelsRange.x, m_levels.y);
				m_levels.y = Mathf.Clamp(m_levels.y, m_levels.x, m_levelsRange.y);

				if (!m_simulationBuffer)
					return;

				if(m_repaint)
					Repaint();

				RenderTextureDescriptor desc = m_simulationBuffer.descriptor;
				desc.graphicsFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R32G32B32A32_SFloat;
				RenderTexture tempRT = RenderTexture.GetTemporary(desc);
				tempRT.filterMode = FilterMode.Point;
				RenderTexture currentRT = RenderTexture.active;

				Color selectedColors = new Color((m_selectedChannel & ColorChannels.Red) != 0 ? 1 : 0,
					(m_selectedChannel & ColorChannels.Green) != 0 ? 1 : 0,
					(m_selectedChannel & ColorChannels.Blue) != 0 ? 1 : 0,
					(m_selectedChannel & ColorChannels.Alpha) != 0 ? 1 : 0);

				m_debuggerMaterial.SetColor("_ColorSelection", selectedColors);

				if(m_autoLevels)
					m_debuggerMaterial.SetVector("_Levels", m_selectedSimulation.GetDebugLevels(m_selectedDebugBuffer));
				else
					m_debuggerMaterial.SetVector("_Levels", m_levels);
				Graphics.Blit(m_simulationBuffer, tempRT, m_debuggerMaterial);
				RenderTexture.active = currentRT;

				float offset = Styles.kToolbarHeight + 1;
				// GUI scroll viewer
				m_textureScrollbar = GUI.BeginScrollView(new Rect(0, offset, bufferViewArea.width, bufferViewArea.height - offset), m_textureScrollbar, 
														new Rect(0, 0, m_simulationBuffer.width * m_textureScale, m_simulationBuffer.height * m_textureScale));
				//EditorGUI.DrawPreviewTexture(new Rect(1, 0, tempRT.width * m_textureScale, tempRT.height * m_textureScale), tempRT, null, scaleMode: ScaleMode.StretchToFill);
				GUI.DrawTexture(new Rect(1, 0, tempRT.width * m_textureScale, tempRT.height * m_textureScale), tempRT);
				GUI.EndScrollView();
				RenderTexture.ReleaseTemporary(tempRT);

			}

		}
	}
}