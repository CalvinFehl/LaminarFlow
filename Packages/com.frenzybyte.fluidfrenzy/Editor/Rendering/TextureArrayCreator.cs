using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering; // Required for AsyncGPUReadback

namespace FluidFrenzy.Editor
{
	public class TextureArrayCreator : EditorWindow
	{
		[MenuItem("Window/Fluid Frenzy/Texture Array Creator")]
		public static void ShowWindow()
		{
			var window = GetWindow<TextureArrayCreator>("Texture Array Creator");
			window.minSize = new Vector2(360, 250);
			window.maxSize = new Vector2(360, 250);
		}

		private Texture2D[] m_sourceTextures = new Texture2D[4];

		private int m_targetWidthIndex = 2; // Default to 1024
		private int m_targetHeightIndex = 2; // Default to 1024
		private readonly string[] m_resolutionOptions = { "256", "512", "1024", "2048", "4096" };
		private readonly int[] m_resolutionValues = { 256, 512, 1024, 2048, 4096 };

		private Material m_blitTextureToArrayMaterial;

		private bool m_isCompiling = false; // Flag to prevent multiple builds

		private void OnEnable()
		{
			// Find and cache the material on enable.
			if (m_blitTextureToArrayMaterial == null)
			{
				m_blitTextureToArrayMaterial = new Material(Shader.Find("Hidden/FluidFrenzy/TextureArrayCreator"));
			}
		}

		private void OnDisable()
		{
			// Clean up the material when the window is closed
			if (m_blitTextureToArrayMaterial != null)
			{
				DestroyImmediate(m_blitTextureToArrayMaterial);
			}
		}

		private void OnGUI()
		{
			// Top label and space removed
			if (m_blitTextureToArrayMaterial == null || m_blitTextureToArrayMaterial.shader == null)
			{
				EditorGUILayout.HelpBox("Error: Could not find the shader 'Hidden/FluidFrenzy/TextureArrayCreator'. Please ensure the shader file is in the project.", MessageType.Error);
				GUI.enabled = false;
			}

			EditorGUILayout.LabelField("Tile Size", EditorStyles.boldLabel);
			EditorGUILayout.BeginHorizontal();
			{
				float originalLabelWidth = EditorGUIUtility.labelWidth;
				EditorGUIUtility.labelWidth = 50f;

				m_targetWidthIndex = EditorGUILayout.Popup("Width", m_targetWidthIndex, m_resolutionOptions);
				m_targetHeightIndex = EditorGUILayout.Popup("Height", m_targetHeightIndex, m_resolutionOptions);

				EditorGUIUtility.labelWidth = originalLabelWidth;
			}
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Source Textures (1 to 4)", EditorStyles.boldLabel);
			EditorGUILayout.BeginHorizontal();
			{
				GUILayout.FlexibleSpace();
				DrawTextureSlot(0, "Layer 1");
				GUILayout.FlexibleSpace();
				DrawTextureSlot(1, "Layer 2");
				GUILayout.FlexibleSpace();
				DrawTextureSlot(2, "Layer 3");
				GUILayout.FlexibleSpace();
				DrawTextureSlot(3, "Layer 4");
				GUILayout.FlexibleSpace();
			}
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.Space();

			List<Texture2D> validTextures = m_sourceTextures.Where(t => t != null).ToList();
			string error = ValidateTextures(validTextures);

			// Disable the button if an error exists
			if (!string.IsNullOrEmpty(error))
			{
				EditorGUILayout.HelpBox(error, MessageType.Error);
				GUI.enabled = false;
			}

			// Show progress bar or build button
			if (m_isCompiling)
			{
				EditorGUILayout.HelpBox("Building texture atlas...", MessageType.Info);
				GUI.enabled = false;
			}
			else
			{
				// Build and Save button ---
				if (GUILayout.Button("Build and Save Atlas..."))
				{
					string path = EditorUtility.SaveFilePanelInProject(
						"Save Texture Atlas", "MyTextureAtlas", "png",
						"Please enter a file name for the PNG atlas.", "Assets/");

					if (!string.IsNullOrEmpty(path))
					{
						m_isCompiling = true; // Set compiling flag
						int targetWidth = m_resolutionValues[m_targetWidthIndex];
						int targetHeight = m_resolutionValues[m_targetHeightIndex];

						// Start the compile process
						CompileAtlas(validTextures, path, targetWidth, targetHeight);
					}
				}
			}

			// Restore GUI state
			GUI.enabled = true;
		}

		/// <summary>
		/// Helper to draw one of the 80x80 texture slots.
		/// </summary>
		private void DrawTextureSlot(int index, string label)
		{
			using (new EditorGUILayout.VerticalScope(GUILayout.Width(80)))
			{
				EditorGUILayout.LabelField(label, GUILayout.Width(80));
				m_sourceTextures[index] = (Texture2D)EditorGUILayout.ObjectField(
					m_sourceTextures[index],
					typeof(Texture2D),
					false,
					GUILayout.Width(80),
					GUILayout.Height(80)
				);
			}
		}

		private string ValidateTextures(List<Texture2D> textures)
		{
			if (textures.Count == 0) return "No source textures provided.";
			bool isFirstNormal = TextureIsNormalMap(textures[0]);
			for (int i = 1; i < textures.Count; i++)
			{
				if (TextureIsNormalMap(textures[i]) != isFirstNormal)
				{
					return "Mixing normal maps and non-normal maps is not supported. All source textures must be of the same type.";
				}
			}
			return null;
		}

		/// <summary>
		/// Gets a compatible 8-bit-per-channel format for PNG output.
		/// </summary>
		private GraphicsFormat GetCompatiblePNGFormat(bool isLinear, bool isNormal)
		{
			if (isNormal) return GraphicsFormat.R8G8B8A8_UNorm;
			// sRGB format for gamma-correct color, UNorm for linear data
			return isLinear ? GraphicsFormat.R8G8B8A8_UNorm : GraphicsFormat.R8G8B8A8_SRGB;
		}

		/// <summary>
		/// Begins the atlas compilation and requests an async readback.
		/// </summary>
		private void CompileAtlas(List<Texture2D> textures, string fullPath, int tileWidth, int tileHeight)
		{
			Texture2D sample = textures[0];
			bool isLinear = TextureIsLinear(sample);
			bool isNormalMap = TextureIsNormalMap(sample);

			// Since we are saving to PNG, we must use a standard 8-bit format.
			GraphicsFormat finalFormat = GetCompatiblePNGFormat(isLinear, isNormalMap);

			int atlasCols, atlasRows;
			if (textures.Count == 4) { atlasCols = 2; atlasRows = 2; }
			else { atlasCols = textures.Count; atlasRows = 1; }

			int atlasWidth = tileWidth * atlasCols;
			int atlasHeight = tileHeight * atlasRows;

			RenderTexture atlasRT = RenderTexture.GetTemporary(atlasWidth, atlasHeight, 0, finalFormat);
			RenderTexture previousActive = RenderTexture.active;
			Graphics.SetRenderTarget(atlasRT);
			GL.Clear(true, true, Color.clear);

			try
			{
				for (int i = 0; i < textures.Count; i++)
				{
					int x, y;
					if (textures.Count == 4) { x = i % 2; y = 1 - (i / 2); }
					else { x = i; y = 0; }
					Rect viewportRect = new Rect(x * tileWidth, y * tileHeight, tileWidth, tileHeight);

					Graphics.SetRenderTarget(atlasRT);
					GL.Viewport(viewportRect);
					Graphics.Blit(textures[i], m_blitTextureToArrayMaterial, isNormalMap ? 1 : 0);
				}
			}
			finally
			{
				GL.Viewport(new Rect(0, 0, Screen.width, Screen.height));
				Graphics.SetRenderTarget(previousActive);
			}

			AsyncGPUReadback.Request(atlasRT, 0, finalFormat, (request) =>
			{
				OnReadbackComplete(request, fullPath, isNormalMap, textures.Count, atlasWidth, atlasHeight);
				RenderTexture.ReleaseTemporary(atlasRT);
			});
		}

		/// <summary>
		/// Callback function that runs when the GPU data is ready.
		/// </summary>
		private void OnReadbackComplete(AsyncGPUReadbackRequest request, string fullPath, bool isNormalMap, int textureCount, int atlasWidth, int atlasHeight)
		{
			if (request.hasError)
			{
				Debug.LogError("GPU Readback failed. Atlas not saved.");
				m_isCompiling = false;
				return;
			}

			// Create a CPU-side texture to receive the data
			Texture2D atlasCPU = new Texture2D(atlasWidth, atlasHeight, GetCompatiblePNGFormat(TextureIsLinear(m_sourceTextures.First(t => t != null)), isNormalMap), TextureCreationFlags.None);

			// Load the raw data from the request
			atlasCPU.LoadRawTextureData(request.GetData<byte>());
			atlasCPU.Apply(false, false);

			// Now we can encode the CPU texture to PNG
			byte[] pngData = atlasCPU.EncodeToPNG();
			System.IO.File.WriteAllBytes(fullPath, pngData);
			DestroyImmediate(atlasCPU);

			AssetDatabase.Refresh();

			ConfigureImporter(fullPath, isNormalMap, textureCount);

			m_isCompiling = false;
		}

		/// <summary>
		/// Configures the newly created PNG asset as a Texture2DArray.
		/// </summary>
		private void ConfigureImporter(string fullPath, bool isNormalMap, int textureCount)
		{
			var importer = AssetImporter.GetAtPath(fullPath) as TextureImporter;
			if (importer != null)
			{
				var settings = new TextureImporterSettings();
				importer.ReadTextureSettings(settings);
				settings.textureShape = TextureImporterShape.Texture2DArray;
				settings.npotScale = TextureImporterNPOTScale.None;

				if (textureCount == 4)
				{
					settings.flipbookColumns = 2;
					settings.flipbookRows = 2;
				}
				else
				{
					settings.flipbookColumns = textureCount;
					settings.flipbookRows = 1;
				}

				if (isNormalMap) settings.textureType = TextureImporterType.NormalMap;
				importer.SetTextureSettings(settings);
				importer.SaveAndReimport();
				Debug.Log("Saved and configured Texture Atlas asset at " + fullPath, AssetDatabase.LoadAssetAtPath<Texture2D>(fullPath));
			}
		}

		private bool TextureIsNormalMap(Texture2D tex)
		{
			if (tex == null) return false;
			string path = AssetDatabase.GetAssetPath(tex);
			if (string.IsNullOrEmpty(path)) return false;
			var importer = AssetImporter.GetAtPath(path) as TextureImporter;
			if (importer == null) return false;
			return importer.textureType == TextureImporterType.NormalMap;
		}

		private bool TextureIsLinear(Texture2D tex)
		{
			if (tex == null) return false;
			string path = AssetDatabase.GetAssetPath(tex);
			if (string.IsNullOrEmpty(path)) return true;
			var importer = AssetImporter.GetAtPath(path) as TextureImporter;
			return importer != null && !importer.sRGBTexture;
		}
	}
}

