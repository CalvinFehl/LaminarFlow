using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
#if FLUIDFRENZY_RUNTIME_URP_SUPPORT
using UnityEngine.Rendering.Universal;
#endif

namespace FluidFrenzy
{
	/// <summary>
	/// <see cref="GPULODSurface"/> implements a Level of Detail (LOD) technique that harness the power of the GPU called <b>Quadtrees on the GPU</b>. 
	/// This implementation dynamically adjusts the detail of the rendered surface based on the
	/// viewer’s distance to optimize performance and rendering efficiency, making it particularly suitable for real-time
	/// applications where the rendering workload must be managed effectively.
	/// </summary>
	public class GPULODSurface : ISurfaceRenderer
	{
		public bool customRender { get { return true; } }

		public float terrainSize;
		public float heightmapScale;

		private Mesh m_surfaceMesh = null;
		private QuadTreeGPU m_quadTree;
		private Vector3 m_gridDim;
		private Vector4[] m_frustumPlanes = new Vector4[6];
		private Vector3 m_boundsSize;
		private bool m_fullTraverse = true;

		private Matrix4x4 m_aspectScale;

#if FLUIDFRENZY_RUNTIME_URP_SUPPORT
		private GPULODTraversePassURP m_traversePass;
		private GPULODCullCameraPassURP m_cullCameraPass;
		private GPULODCullShadowPassURP m_cullShadowPass;
#endif

		public GPULODSurface(ISurfaceRenderer.SurfaceDescriptor desc)
		{
			m_gridDim = new Vector3(desc.meshResolution.x, desc.meshResolution.x, desc.meshResolution.x);

			float aspectRatio = desc.dimension.y / desc.dimension.x;
			float aspectRatioX = aspectRatio > 1 ? (1.0f / aspectRatio) : 1;
			float aspectRatioY = aspectRatio < 1 ? aspectRatio : 1; 
			m_aspectScale = Matrix4x4.Scale(new Vector3(aspectRatioX, 1, aspectRatioY));
			terrainSize = Mathf.Max(desc.dimension.x, desc.dimension.y);
			heightmapScale = desc.heightScale;
			m_boundsSize = new Vector3(desc.dimension.x, desc.maxHeight, desc.dimension.y);
			m_surfaceMesh ??= PrimitiveGenerator.GenerateTerrainLODMesh(desc.meshResolution.x, desc.meshResolution.y);
			if (m_surfaceMesh.isReadable)
				m_surfaceMesh.RecalculateBounds();

			m_quadTree ??= new QuadTreeGPU(m_surfaceMesh.GetIndexCount(0), desc.heightmapMask, desc.lodMinMax);

#if FLUIDFRENZY_RUNTIME_URP_SUPPORT
			m_traversePass = new GPULODTraversePassURP(this);
			m_cullCameraPass = new GPULODCullCameraPassURP(this);
			m_cullShadowPass = new GPULODCullShadowPassURP(this);
#endif
		}

		private void DisposeQuadtree()
		{
			m_quadTree.Dispose();
		}

		public void Dispose()
		{
#if UNITY_EDITOR
			AssemblyReloadEvents.beforeAssemblyReload -= DisposeQuadtree;
#endif
			m_quadTree.Dispose();
			GraphicsHelpers.SafeDestroy(m_surfaceMesh);
		}

		public void SetupMaterial(Material material)
		{
			material.EnableKeyword("_FLUIDFRENZY_INSTANCING");
		}

		public void UpdateSurfaceDescriptor(ISurfaceRenderer.SurfaceDescriptor desc)
		{
			m_boundsSize = new Vector3(desc.dimension.x, desc.maxHeight, desc.dimension.y);
			heightmapScale = desc.heightScale;
		}

		public void OnEnable()
		{
#if UNITY_EDITOR
			AssemblyReloadEvents.beforeAssemblyReload += DisposeQuadtree;
#endif
		}

		public void OnDisable()
		{
#if UNITY_EDITOR
			AssemblyReloadEvents.beforeAssemblyReload -= DisposeQuadtree;
#endif
		}
		public void AddCommandBuffers(Camera camera)
		{
			if (camera.actualRenderingPath == RenderingPath.Forward)
			{
				CameraEventCommandBuffer.GetOrCreateAndAttach(camera, CameraEvent.BeforeDepthTexture, "GPULODSurface");
				CameraEventCommandBuffer.GetOrCreateAndAttach(camera, CameraEvent.BeforeForwardOpaque, "GPULODSurface");
			}
			else
			{
				CameraEventCommandBuffer.GetOrCreateAndAttach(camera, CameraEvent.BeforeGBuffer);
			}
		}

		public void RemoveCommandBuffers(Camera camera)
		{
			if (camera.actualRenderingPath == RenderingPath.Forward)
			{
				CameraEventCommandBuffer.Detach(camera, CameraEvent.BeforeDepthTexture);
				CameraEventCommandBuffer.Detach(camera, CameraEvent.BeforeForwardOpaque);
			}
			else
			{
				CameraEventCommandBuffer.Detach(camera, CameraEvent.BeforeGBuffer);
			}
		}

		public void PreRender(ScriptableRenderContext context, Transform transform, Camera camera, Texture heightmap, int traverseIterations = 1, bool renderShadows = false)
		{
#if FLUIDFRENZY_RUNTIME_URP_SUPPORT
			m_cullCameraPass.SetTransform(transform);

			if (Camera.main == camera || camera.cameraType == CameraType.SceneView)
			{
				traverseIterations = m_fullTraverse ? 15 : traverseIterations;
				m_fullTraverse = false;
				m_traversePass.SetRenderData(transform, heightmap, traverseIterations);
				camera.GetUniversalAdditionalCameraData().scriptableRenderer.EnqueuePass(m_traversePass);
			}
			camera.GetUniversalAdditionalCameraData().scriptableRenderer.EnqueuePass(m_cullCameraPass);

			if(renderShadows)
				camera.GetUniversalAdditionalCameraData().scriptableRenderer.EnqueuePass(m_cullShadowPass);
#endif
		}

		public void Traverse(CommandBuffer commandBuffer, Transform transform, Camera camera, Texture heightmap, int traverseIterations = 1)
		{
			// Apply a aspect scale as it is the easiest way to allow none square terrains.
			Matrix4x4 localToWorld = transform.localToWorldMatrix * m_aspectScale;
			//Traverse the quadtree
			for (int i = 0; i < traverseIterations; i++)
				m_quadTree.Traverse(camera, commandBuffer, heightmap, localToWorld, terrainSize, heightmapScale, m_boundsSize.y, out _, out _);
		}

		public void Cull(CommandBuffer commandBuffer, Transform transform, Camera camera)
		{
			// Apply a aspect scale as it is the easiest way to allow none square terrains.
			Matrix4x4 localToWorld = transform.localToWorldMatrix * m_aspectScale;
			//Cull the quadtree
			m_quadTree.CullQuadtree(camera, commandBuffer, localToWorld, terrainSize, m_boundsSize.y, out _, out _);
		}

		public void PreRenderCamera(Transform transform, Camera camera, Texture heightmap, int traverseIterations = 1)
		{
			// Apply a aspect scale as it is the easiest way to allow none square terrains.
			Matrix4x4 localToWorld = transform.localToWorldMatrix * m_aspectScale;
			traverseIterations = m_fullTraverse ? 15 : traverseIterations;
			m_fullTraverse = false;
			if (camera.actualRenderingPath == RenderingPath.Forward)
			{
				// If there are no shadows, or if depthTextureMode is None we need to update the quadtree in the color pass.
				bool hasDepthPass = camera.depthTextureMode != DepthTextureMode.None;
				CommandBuffer beforeDepthCmd = CameraEventCommandBuffer.GetOrCreate(camera, CameraEvent.BeforeDepthTexture, "GPULODSurface");
				CommandBuffer beforeColorCmd = CameraEventCommandBuffer.GetOrCreate(camera, CameraEvent.BeforeForwardOpaque, "GPULODSurface");
				CommandBuffer traverseCmd = hasDepthPass ? beforeDepthCmd : beforeColorCmd;
#if UNITY_EDITOR
				// Scene view does have depth pass, so does gameview when not in playmode, this prevents shadows from lagging behind.
				if (!Application.isPlaying && !hasDepthPass)
				{
					for (int i = 0; i < traverseIterations; i++)
						m_quadTree.Traverse(camera, beforeDepthCmd, heightmap, localToWorld, terrainSize, heightmapScale, m_boundsSize.y, out _, out _);
				}
#endif

				//Traverse the quadtree
				for (int i = 0; i < traverseIterations; i++)
					m_quadTree.Traverse(camera, traverseCmd, heightmap, localToWorld, terrainSize, heightmapScale, m_boundsSize.y, out _, out _);

				// If there is a depth pass there is a good chance another pass (shadows) has overwritten the culling, so we recull.
				if (hasDepthPass)
					m_quadTree.CullQuadtree(camera, beforeColorCmd, localToWorld, terrainSize, m_boundsSize.y, out _, out _);

			}
			else
			{
				CommandBuffer beforeGBufferCmd = CameraEventCommandBuffer.GetOrCreate(camera, CameraEvent.BeforeGBuffer);
				for (int i = 0; i < traverseIterations; i++)
					m_quadTree.Traverse(camera, beforeGBufferCmd, heightmap, localToWorld, terrainSize, heightmapScale, m_boundsSize.y, out _, out _);
			}
		}

		public void CullShadowLight(CommandBuffer commandBuffer, Transform transform, Camera camera, Light light)
		{
			if (!light)
				return;

			// Apply a aspect scale as it is the easiest way to allow none square terrains.
			Matrix4x4 localToWorld = transform.localToWorldMatrix * m_aspectScale;

			// Get Frustum for the directional light.
			GraphicsHelpers.CalculateShadowFrustumPlanes(camera, light, m_frustumPlanes);

			// Cull quadtree for directional light
			m_quadTree.CullQuadtree(camera, commandBuffer, localToWorld, terrainSize, m_boundsSize.y, m_frustumPlanes, out _, out _);
		}

		public void FillRenderBufferNoCull(CommandBuffer commandBuffer)
		{
			// Cull quadtree for directional light
			m_quadTree.FillRenderBufferNoCull(commandBuffer, terrainSize, m_boundsSize.y, out _, out _);
		}

		public void Render(Transform transform, Material material, MaterialPropertyBlock properties, Camera camera = null)
		{

#if UNITY_2021_1_OR_NEWER
			// Get the current nodes and draw args
			m_quadTree.GetRenderData(out GraphicsBuffer nodes, out GraphicsBuffer drawArgs);
			properties.SetBuffer(FluidShaderProperties._QuadTreeNodes, nodes);
			properties.SetVector(FluidShaderProperties._LODMeshDim, m_gridDim);
			properties.SetVector(FluidShaderProperties._SurfaceLocalBounds, new Vector3(terrainSize, 1, terrainSize));

			// Apply a aspect scale as it is the easiest way to allow none square terrains.
			Matrix4x4 rotation_scale = Matrix4x4.TRS(Vector3.zero, transform.rotation, transform.lossyScale);
			properties.SetMatrix(FluidShaderProperties._ObjectToWorld, transform.localToWorldMatrix * m_aspectScale);
			properties.SetMatrix("_ObjectToWorldRotationScale", rotation_scale * m_aspectScale);

			// Render the quadtree/surface
			RenderParams rp = new RenderParams()
			{
				receiveShadows = true,
				shadowCastingMode = ShadowCastingMode.On,
				worldBounds = new Bounds(transform.position, m_boundsSize),
				lightProbeUsage = LightProbeUsage.BlendProbes,
				material = material,
				reflectionProbeUsage = ReflectionProbeUsage.BlendProbes,
				layer = transform.gameObject.layer,
				matProps = properties,
				camera = camera,
				renderingLayerMask = uint.MaxValue,
			};

			Graphics.RenderMeshIndirect(rp, m_surfaceMesh, drawArgs);
#else
			m_quadTree.GetRenderData(out ComputeBuffer nodes, out ComputeBuffer drawArgs);
			properties.SetBuffer(FluidShaderProperties._QuadTreeNodes, nodes);
			properties.SetVector(FluidShaderProperties._LODMeshDim, m_gridDim);
			properties.SetVector(FluidShaderProperties._SurfaceLocalBounds, new Vector3(m_boundsSize.x, 1, m_boundsSize.z)); 
			Graphics.DrawMeshInstancedIndirect( m_surfaceMesh, 0, material,
												new Bounds(transform.position, m_boundsSize), drawArgs, 0, 
												properties, ShadowCastingMode.On, true, transform.gameObject.layer, 
												null, LightProbeUsage.BlendProbes);

#endif
		}

		public void Render(CommandBuffer cmd, Matrix4x4 localToWorld, Material material, MaterialPropertyBlock properties, int pass = 0, Camera camera = null)
		{
			SetupMaterial(material);

			// Get the current nodes and draw args
			m_quadTree.GetRenderData(out GraphicsBuffer nodes, out GraphicsBuffer drawArgs);
			properties.SetBuffer(FluidShaderProperties._QuadTreeNodes, nodes);
			properties.SetVector(FluidShaderProperties._LODMeshDim, m_gridDim);
			properties.SetVector(FluidShaderProperties._SurfaceLocalBounds, new Vector3(terrainSize, 1, terrainSize));

			// Apply a aspect scale as it is the easiest way to allow none square terrains.
			Matrix4x4 rotation_scale = Matrix4x4.TRS(Vector3.zero, localToWorld.rotation, localToWorld.lossyScale);
			properties.SetMatrix(FluidShaderProperties._ObjectToWorld, localToWorld * m_aspectScale);
			properties.SetMatrix("_ObjectToWorldRotationScale", rotation_scale * m_aspectScale);

			cmd.DrawMeshInstancedIndirect(m_surfaceMesh, 0, material, pass, drawArgs, 0, properties);

		}
	}
}
