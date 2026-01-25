using UnityEngine;
using System.IO;
using System;
using System.IO.Compression;
using UnityEngine.Experimental.Rendering;

namespace FluidFrenzy
{

	/// <summary>
	/// A utility class for saving and loading simulation textures to and from disk.
	/// Supports a custom, high-performance raw data format (.data) and standard PNG.
	/// </summary>
	public static class SimulationIO
	{
		/// <summary>
		/// Defines the file format for saving or loading textures.
		/// </summary>
		public enum FileFormat
		{
			/// <summary>
			/// A custom, fast, and lossless raw data format saved as a .data file.
			/// Recommended for saving/loading high-precision simulation state.
			/// </summary>
			RAW,
			/// <summary>
			/// Standard PNG format. Useful for visual debugging but will lose precision
			/// when saving floating-point textures.
			/// </summary>
			PNG
		}

		[Serializable]
		private struct TextureMetadata
		{
			public int width;
			public int height;
			public TextureFormat format;
			public bool isLinear;
			public bool isCompressed;
		}

		/// <summary>
		/// Saves a RenderTexture to a file using the specified format.
		/// </summary>
		/// <param name="source">The RenderTexture to save.</param>
		/// <param name="path">The full file path (e.g., "C:/MySim/Height.data").</param>
		/// <param name="format">The file format to use (RAW or PNG).</param>
		/// <param name="compress">For RAW format, determines whether to use GZip compression.</param>
		/// <param name="embedMetadata">For RAW format, determines whether to embed metadata into a single .data file.</param>
		public static void SaveTexture(RenderTexture source, string path, FileFormat format, bool compress = true, bool embedMetadata = true)
		{
			Texture2D tempTexture = null;
			try
			{
				byte[] data;

				// Prepare the data based on the chosen format
				if (format == FileFormat.PNG)
				{
					path = Path.HasExtension(path) ? path : path + ".png";
					// PNG conversion requires a specific format and is lossy for float textures.
					tempTexture = new Texture2D(source.width, source.height, TextureFormat.ARGB32, false, true);
					RenderTexture.active = source;
					tempTexture.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
					tempTexture.Apply();
					data = tempTexture.EncodeToPNG();
				}
				else // RAW format
				{
					path = Path.HasExtension(path) ? path : path + ".data";
					tempTexture = new Texture2D(source.width, source.height, source.graphicsFormat, TextureCreationFlags.None);
					RenderTexture.active = source;
					tempTexture.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
					tempTexture.Apply(false, false);

					using (var rawDataArray = tempTexture.GetRawTextureData<byte>())
					{
						data = rawDataArray.ToArray();
					}

					if (compress)
					{
						using (var memoryStream = new MemoryStream())
						{
							using (var gzipStream = new GZipStream(memoryStream, CompressionMode.Compress))
							{
								gzipStream.Write(data, 0, data.Length);
							}
							data = memoryStream.ToArray();
						}
					}

					// Handle Metadata for RAW format
					TextureMetadata metadata = new TextureMetadata
					{
						width = source.width,
						height = source.height,
						format = tempTexture.format,
						isLinear = true,
						isCompressed = compress
					};
					string json = JsonUtility.ToJson(metadata);

					if (embedMetadata)
					{
						// Embed the metadata as a header in the single .data file
						byte[] headerBytes = System.Text.Encoding.UTF8.GetBytes(json);
						int headerLength = headerBytes.Length;

						using (var memoryStream = new MemoryStream())
						{
							// Write header: [4-byte header length][json header][texture data]
							memoryStream.Write(BitConverter.GetBytes(headerLength), 0, 4);
							memoryStream.Write(headerBytes, 0, headerLength);
							memoryStream.Write(data, 0, data.Length);
							data = memoryStream.ToArray(); // 'data' is now the final, combined byte array
						}
					}
					else
					{
						// Save metadata to a separate .json file (legacy option)
						File.WriteAllText(path + ".json", json);
					}
				}

				// Step 3: Write the final byte array to disk
				string directory = Path.GetDirectoryName(path);
				if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
				File.WriteAllBytes(path, data);
			}
			catch (Exception e)
			{
				Debug.LogException(e);
			}
			finally
			{
				RenderTexture.active = null;
				if (tempTexture != null)
				{
					if (Application.isPlaying) UnityEngine.Object.Destroy(tempTexture);
					else UnityEngine.Object.DestroyImmediate(tempTexture);
				}
			}
		}

		/// <summary>
		/// Loads a Texture2D from a file, automatically detecting format and metadata location.
		/// </summary>
		/// <param name="path">The full file path (e.g., "C:/MySim/Height.data" or "C:/MySim/Height.png").</param>
		/// <param name="metadataIsEmbedded">For .data files, specifies if metadata is embedded in the file itself.</param>
		public static Texture2D LoadTexture(string path, bool metadataIsEmbedded = true)
		{
			try
			{
				string extension = Path.GetExtension(path).ToLower();
				byte[] data = File.ReadAllBytes(path);

				if (extension == ".png")
				{
					Texture2D pngTexture = new Texture2D(2, 2, TextureFormat.ARGB32, false, true);
					pngTexture.LoadImage(data, false);
					pngTexture.Apply(false, false);
					return pngTexture;
				}
				else // if (extension == ".data")
				{
					TextureMetadata metadata;
					int dataOffset = 0;

					if (metadataIsEmbedded)
					{
						// Read header from the single .data file
						int headerLength = BitConverter.ToInt32(data, 0);
						string json = System.Text.Encoding.UTF8.GetString(data, 4, headerLength);
						metadata = JsonUtility.FromJson<TextureMetadata>(json);
						dataOffset = 4 + headerLength;
					}
					else
					{
						// Read metadata from a separate .json file
						string metaPath = Path.ChangeExtension(path, ".json");
						string json = File.ReadAllText(metaPath);
						metadata = JsonUtility.FromJson<TextureMetadata>(json);
					}

					// Get the raw texture data segment
					byte[] textureData = new byte[data.Length - dataOffset];
					Array.Copy(data, dataOffset, textureData, 0, textureData.Length);

					if (metadata.isCompressed)
					{
						using (var memoryStream = new MemoryStream(textureData))
						using (var gzipStream = new GZipStream(memoryStream, CompressionMode.Decompress))
						using (var resultStream = new MemoryStream())
						{
							gzipStream.CopyTo(resultStream);
							textureData = resultStream.ToArray();
						}
					}

					Texture2D rawTexture = new Texture2D(metadata.width, metadata.height, metadata.format, false, metadata.isLinear);
					rawTexture.LoadRawTextureData(textureData);
					rawTexture.Apply(false, false);
					return rawTexture;
				}
			}
			catch (Exception e)
			{
				Debug.LogException(e);
			}

			Debug.LogError($"Could not load or identify texture format for path: {path}");
			return null;
		}
	}
}