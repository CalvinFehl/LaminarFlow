using UnityEngine;
using UnityEngine.Rendering;

namespace FluidFrenzy
{
	/// <summary>
	/// Represents a surface renderer that can be instantiated to render heightfields using Unity's <see cref="MeshRenderer"/> components.
	/// The surfaces can be split into multiple blocks to optimize rendering performance as the blocks can be more easily culled by the camera.
	/// When splitting the surface into chunks there is support for rendering using Unity's GPU Instancing functionality available on materials.
	/// </summary>
	public class MeshRendererSurface : ISurfaceRenderer
	{
		public bool customRender { get { return false; } }

		private Mesh m_surfaceMesh = null;
		private Renderer[] m_rendererBlocks = null;

		public MeshRendererSurface(GameObject parent, ISurfaceRenderer.SurfaceDescriptor desc, Vector2Int meshBlocks)
		{
			int numVertsX = desc.meshResolution.x / meshBlocks.x;
			int numVertsY = desc.meshResolution.y / meshBlocks.y;

			m_surfaceMesh ??= PrimitiveGenerator.GenerateTerrainMesh(numVertsX, numVertsY, desc.dimension / meshBlocks, Vector2.zero, Vector2.one);

			if (m_surfaceMesh.isReadable)
				m_surfaceMesh.RecalculateBounds();

			Bounds bounds = m_surfaceMesh.bounds;
			bounds.Encapsulate(Vector3.up * desc.maxHeight);
			m_surfaceMesh.bounds = bounds;

			m_rendererBlocks = new Renderer[meshBlocks.x * meshBlocks.y];

			MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
			Vector2 gridSize = desc.meshResolution + Vector2.one;
			Vector2 chunkScale = Vector2.one - (Vector2.one / meshBlocks);
			Vector2 uvChunkScale = Vector2.one / gridSize * chunkScale;
			Vector2 uvChunkOffset = Vector2.one - (Vector2.one / gridSize);
			for (int y = 0; y < meshBlocks.y; y++)
			{
				for (int x = 0; x < meshBlocks.x; x++)
				{
					Vector3 pos = new Vector3()
					{
						x = (-desc.dimension.x / 2) + desc.dimension.x / 2 / meshBlocks.x,
						z = (-desc.dimension.y / 2) + desc.dimension.y / 2 / meshBlocks.y
					};
					pos.x += (desc.dimension.x / meshBlocks.x) * x;
					pos.z += (desc.dimension.y / meshBlocks.y) * y;
					int index = x + y * meshBlocks.x;

					GameObject chunk = new GameObject(string.Format("Chunk-{0}-{1}", x, y));
					chunk.layer = parent.layer;
					chunk.transform.position = pos;
					chunk.transform.SetParent(parent.transform, false);
					chunk.hideFlags = HideFlags.DontSave;

					m_rendererBlocks[index] = chunk.AddComponent<MeshRenderer>();
					MeshFilter meshfilter = chunk.AddComponent<MeshFilter>();


					Vector2 textureUVStart = new Vector2((float)x / meshBlocks.x, (float)y / meshBlocks.y);
					Vector2 textureUVScale = new Vector2(1.0f / meshBlocks.x, 1.0f / meshBlocks.y);
					Vector2 meshUVStart = textureUVStart * uvChunkOffset;
					Vector2 meshUVScale = textureUVScale + Vector2.one * uvChunkScale;

					propertyBlock.SetVector(FluidShaderProperties._MeshUVOffsetScale, new Vector4(meshUVStart.x, meshUVStart.y, meshUVScale.x, meshUVScale.y));
					propertyBlock.SetVector(FluidShaderProperties._TextureUVOffsetScale, new Vector4(textureUVStart.x, textureUVStart.y, textureUVScale.x, textureUVScale.y));

					meshfilter.sharedMesh = m_surfaceMesh;

					m_rendererBlocks[index].SetPropertyBlock(propertyBlock);
				}
			}
		}


		public void Dispose()
		{
#if UNITY_EDITOR
			// Unity will destroy this if we are entering playmode. But we aren't in playmode they can be destroyed by changing rendering mode.
			// if ((!UnityEditor.EditorApplication.isPlaying && !UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode) || UnityEditor.EditorApplication.isPlaying)
#endif
			{
				if (m_rendererBlocks != null)
				{
					foreach (MeshRenderer renderer in m_rendererBlocks)
					{
						if (renderer != null)
						{
							GraphicsHelpers.SafeDestroy(renderer.gameObject);
						}
					}
					m_rendererBlocks = null;
				}
			}

			GraphicsHelpers.SafeDestroy(m_surfaceMesh);
		}

		public void SetupMaterial(Material material)
		{
			material?.DisableKeyword("_FLUIDFRENZY_INSTANCING");
			material?.SetConstantBuffer(FluidShaderProperties._UnityInstancing_InstanceProperties, (null as GraphicsBuffer), 0, 0);

			foreach (Renderer renderer in m_rendererBlocks)
			{
				renderer.sharedMaterial = material;
			}
		}

		public void UpdateSurfaceDescriptor(ISurfaceRenderer.SurfaceDescriptor desc)
		{
			Bounds bounds = m_surfaceMesh.bounds;
			Vector3 boundSize = bounds.size;
			boundSize.y = desc.maxHeight;
			bounds.size = boundSize;
			m_surfaceMesh.bounds = bounds;
		}

		public void OnEnable() { }
		public void OnDisable()
		{
			if (Application.isPlaying)
				return;
			Dispose();
		}
		public void AddCommandBuffers(Camera camera) { }

		public void RemoveCommandBuffers(Camera camera) { }

		public void PreRender(ScriptableRenderContext context, Transform transform, Camera camera, Texture heightmap, int traverseIterations = 1, bool renderShadows = false)
		{
		}

		public void PreRenderCamera(Transform transform, Camera camera, Texture heightmap, int traverseIterations = 1)
		{
		}

		public void CullShadowLight(CommandBuffer commandBuffer, Transform transform, Camera camera, Light light)
		{
		}

		public void Render(Transform transform, Material material, MaterialPropertyBlock properties, Camera camera = null)
		{

		}

		public void Render(CommandBuffer cmd, Matrix4x4 localToWorld, Material material, MaterialPropertyBlock properties, int pass = 0, Camera camera = null)
		{
			material?.DisableKeyword("_FLUIDFRENZY_INSTANCING");
			material?.SetConstantBuffer(FluidShaderProperties._UnityInstancing_InstanceProperties, (null as GraphicsBuffer), 0, 0);

			foreach (Renderer renderer in m_rendererBlocks)
			{
				cmd.DrawRenderer(renderer, material, 0, pass);
			}
		}
	}
}
