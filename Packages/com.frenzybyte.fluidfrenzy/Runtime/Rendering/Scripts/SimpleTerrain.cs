using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using System.IO;

namespace FluidFrenzy
{
	using static FluidFrenzy.SimulationIO;
	using static FluidFrenzy.SurfaceCollider;
	using SurfaceRenderMode = ISurfaceRenderer.RenderMode;

	/// <summary>
	/// A specialized terrain rendering component designed for the <see cref="FluidSimulation"/> system, capable of supporting real-time modifications from an <see cref="ErosionLayer"/>.
	/// </summary>
	/// <remarks>
	/// Unlike a Unity Terrain, <c>SimpleTerrain</c> allows for dynamic updates to its heightmap data directly on the GPU. 
	/// When an <see cref="ErosionLayer"/> is active, sediment transport and deposition changes are applied to this terrain instantly.
	/// </remarks>
	[ExecuteInEditMode]
	[HelpURL("https://frenzybyte.github.io/fluidfrenzy/docs/terrain/#simple-terrain")]
	public class SimpleTerrain : MonoBehaviour
	{
		/// <summary>
		/// The runtime RenderTexture containing the current height data of the terrain.
		/// </summary>
		/// <remarks>
		/// This texture is generated at runtime and includes any modifications applied by the <see cref="ErosionLayer"/>. 
		/// It should not be modified manually from the CPU.
		/// </remarks>
		public RenderTexture renderHeightmap { get; private set; } = null;

		/// <summary> 
		/// The material used to render the terrain surface.
		/// </summary>
		/// <remarks>
		/// The assigned shader must support vertex displacement based on the <see cref="renderHeightmap"/> to correctly visualize the terrain shape.
		/// </remarks>
		public Material terrainMaterial = null;

		/// <summary> 
		/// Configuration settings that determine the visual quality, resolution, and rendering method (e.g., MeshRenderer vs. GPULOD) of the terrain. 
		/// </summary>
		public ISurfaceRenderer.RenderProperties surfaceProperties = ISurfaceRenderer.RenderProperties.Default;

		[Obsolete("dimension property has been deprecated use surfaceProperties.dimension"), SerializeField]
		public Vector2 dimension = Vector2.one;
		[Obsolete("meshResolution property has been deprecated use surfaceProperties.meshResolution"), SerializeField]
		public Vector2Int meshResolution = new Vector2Int(512, 512);

		/// <summary>
		/// The input texture that defines the initial shape and composition of the terrain.
		/// </summary>
		/// <remarks>
		/// For best results, use a 16-bit (16bpp) texture to prevent stepping artifacts.
		/// <para>
		/// The texture channels represent distinct material layers stacked sequentially from bottom to top. All layers are erodible.
		/// <list type="bullet">
		///		<item>
		///			<term><c>Red Channel</c></term>
		///			<description>Layer 1 (Bottom). Defines the base height. In <c>TerraformTerrain</c>, a separate splatmap texture is used to apply visual variation to this specific layer.</description>
		///		</item>
		///		<item>
		///			<term><c>Green Channel</c></term>
		///			<description>Layer 2. Stacked on top of the Red channel.</description>
		///		</item>
		///		<item>
		///			<term><c>Blue Channel</c></term>
		///			<description>Layer 3. Stacked on top of the Green channel.</description>
		///		</item>
		///		<item>
		///			<term><c>Alpha Channel</c></term>
		///			<description>Layer 4 (Top). Stacked on top of the Blue channel.</description>
		///		</item>
		/// </list>
		/// </para>
		/// </remarks>
		public Texture2D sourceHeightmap = null;

		/// <summary> 
		/// A global multiplier applied to the height values sampled from the <see cref="sourceHeightmap"/>. 
		/// </summary>
		/// <remarks>
		/// This converts the normalized (0 to 1) texture data into world-space height units.
		/// </remarks>
		public float heightScale = 1.0f;

		/// <summary>
		/// Toggles bilinear interpolation for the heightmap sampling.
		/// </summary>
		/// <remarks>
		/// Enabling this increases the number of samples taken to smooth out the terrain. 
		/// This is particularly useful for reducing "stair-stepping" artifacts when using lower bit-depth source textures.
		/// </remarks>
		public bool upsample = false;

		/// <summary> 
		/// The primary directional light used to calculate shadows on the terrain.
		/// </summary>
		/// <remarks>
		/// This is primarily used when rendering in the Built-in Render Pipeline (BiRP) to manually handle shadow projection on the custom terrain mesh.
		/// </remarks>
		public Light shadowLight = null;

		/// <summary>
		/// Configuration settings for generating the physical <see cref="TerrainCollider"/> associated with this terrain.
		/// </summary>
		public ColliderProperties colliderProperties = ColliderProperties.DefaultTerrain;

		internal bool reloadedTerrain = false;

		private MaterialPropertyBlock m_propertyBlock;
		private Material m_initSimulationMaterial = null;
		private int m_initSimulationCopyHeightmapPass = 0;
		private int m_initSimulationUpsampleHeightmapPass = 0;
		private int m_initSimulationSaveHeightmapPass = 0;
		private ISurfaceRenderer m_surfaceRenderer = null;
		private CommandBuffer m_shadowRenderCommands;
		private bool m_addedCommandBuffers = false;

		SurfaceCollider m_collider;


		void OnValidate()
		{
			if(surfaceProperties.version == 0)
			{
#pragma warning disable CS0618 // Rethrow to preserve stack details
#if UNITY_EDITOR
				UnityEditor.Undo.RecordObject(this, "Update SimpleTerrain");
#endif
				surfaceProperties = new ISurfaceRenderer.RenderProperties(SurfaceRenderMode.MeshRenderer, dimension, meshResolution, Vector2Int.one * 8, Vector2Int.one * 8, 1, new Vector2Int(0,15), ISurfaceRenderer.RenderProperties.Version);
#pragma warning restore CS0618 // Rethrow to preserve stack details
			}
		}

		// Start is called before the first frame update
		public virtual void Start()
		{
			m_shadowRenderCommands ??= new CommandBuffer();
			m_shadowRenderCommands.name = "DirectionalShadowCulling";

			if (surfaceProperties.renderMode == SurfaceRenderMode.GPULOD)
				renderHeightmap = new RenderTexture(sourceHeightmap.width + 1, sourceHeightmap.height + 1, 0, RenderTextureFormat.ARGBHalf);
			else
				renderHeightmap = new RenderTexture(surfaceProperties.meshResolution.x + 1, surfaceProperties.meshResolution.y + 1, 0, RenderTextureFormat.ARGBHalf);
			renderHeightmap.filterMode = FilterMode.Bilinear;
			renderHeightmap.wrapMode = TextureWrapMode.Clamp;
			renderHeightmap.name = GraphicsHelpers.AutoRenderTextureName("TerrainTexture");
			renderHeightmap.Create();

			m_initSimulationMaterial = new Material(Shader.Find("Hidden/FluidFrenzy/InitSimulation"));
			m_initSimulationCopyHeightmapPass = m_initSimulationMaterial.FindPass("CopyHeightmap");
			m_initSimulationUpsampleHeightmapPass = m_initSimulationMaterial.FindPass("UpsampleHeightmap");
			m_initSimulationSaveHeightmapPass = m_initSimulationMaterial.FindPass("SaveHeightmap");
			InitializeTerrain(sourceHeightmap);
			UpdateColliderFull();
		}

		protected virtual void InitializeTerrain(Texture source, bool applyOffset = true)
		{
			CommandBuffer commandBuffer = new CommandBuffer();
			Vector2 offset = Vector2.zero;
			if (source)
			{
				offset = applyOffset ? new Vector2(1.0f / source.width, 1.0f / source.height) : Vector2.one;
			}

			Vector4 blitScale = new Vector4(1 + offset.x * 2,
											1 + offset.y * 2,
											-offset.x,
											-offset.y);

			MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
			propertyBlock.Clear();
			propertyBlock.SetTexture(FluidShaderProperties._TerrainHeightField, source ? source : Texture2D.blackTexture);
			propertyBlock.SetTexture(FluidShaderProperties._Obstacles, Texture2D.blackTexture);
			propertyBlock.SetFloat(FluidShaderProperties._HeightScale, heightScale);
			propertyBlock.SetVector(FluidShaderProperties._BlitScaleBias, applyOffset ? blitScale : new Vector4(1,1,0,0));
			propertyBlock.SetVector(FluidShaderProperties._BlitScaleBiasRt, Vector2.one);
			propertyBlock.SetFloat(FluidShaderProperties._OffsetUVScale, 1);

			int copyPass = upsample ? m_initSimulationUpsampleHeightmapPass : m_initSimulationCopyHeightmapPass;
			FluidSimulation.BlitQuad(commandBuffer, null, renderHeightmap, m_initSimulationMaterial, propertyBlock, applyOffset ? copyPass : m_initSimulationSaveHeightmapPass);
			Graphics.ExecuteCommandBuffer(commandBuffer);
		}


		private void CreateResources()
		{
			m_propertyBlock ??= new MaterialPropertyBlock();
			m_shadowRenderCommands ??= new CommandBuffer();
			m_shadowRenderCommands.name = "SimpleTerrainShadows";
			CreateTerrain();
			CreateCollision();
		}

		private void OnEnable()
        {
#if UNITY_EDITOR
			AssemblyReloadEvents.beforeAssemblyReload += RemoveShadowCommands;
			AssemblyReloadEvents.beforeAssemblyReload += RemoveCameraCallbacks;
#endif
			Setup();
			m_surfaceRenderer.OnEnable();
		}

		private void OnDisable()
        {
#if UNITY_EDITOR
			AssemblyReloadEvents.beforeAssemblyReload -= RemoveShadowCommands;
			AssemblyReloadEvents.beforeAssemblyReload -= RemoveCameraCallbacks;
#endif
			RemoveShadowCommands();
			RemoveCameraCallbacks();
			m_surfaceRenderer.OnDisable();
		}

        public virtual void OnDestroy()
        {
#if UNITY_EDITOR
			AssemblyReloadEvents.beforeAssemblyReload -= RemoveShadowCommands;
			AssemblyReloadEvents.beforeAssemblyReload -= RemoveCameraCallbacks;
#endif
			RemoveCameraCallbacks();
			RemoveShadowCommands();
			GraphicsHelpers.ReleaseSimulationRT(renderHeightmap);
			GraphicsHelpers.SafeDestroy(m_initSimulationMaterial);
			m_surfaceRenderer.Dispose();
		}

		private void AddCameraCallbacks()
        {
			if (!m_addedCommandBuffers)
			{
				Camera.onPreCull += PreCull;
				Camera.onPostRender += PostRender;
				RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
				m_addedCommandBuffers = true;
			}
		}

        private void RemoveCameraCallbacks()
        {
			if (m_addedCommandBuffers)
			{
				Camera.onPreCull -= PreCull;
				Camera.onPostRender -= PostRender;
				RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
				m_addedCommandBuffers = false;
			}
		}

		private void AddShadowCommands()
        {
#if !FLUIDFRENZY_RUNTIME_URP_SUPPORT
			if (shadowLight)
			{
				shadowLight.AddCommandBuffer(LightEvent.BeforeShadowMap, m_shadowRenderCommands, ShadowMapPass.Directional);
			}
#endif
		}

		private void RemoveShadowCommands()
		{
#if !FLUIDFRENZY_RUNTIME_URP_SUPPORT
			if (shadowLight)
			{
				shadowLight.RemoveCommandBuffer(LightEvent.BeforeShadowMap, m_shadowRenderCommands);
			}
#endif
		}

		private void PreCull(Camera camera)
		{
			if (camera.cameraType == CameraType.Preview)
				return;
			Texture heightmap = (renderHeightmap != null) ? renderHeightmap as Texture : sourceHeightmap;
			m_surfaceRenderer.PreRenderCamera(transform, camera, heightmap, surfaceProperties.traverseIterations);
			m_surfaceRenderer.AddCommandBuffers(camera);

			m_shadowRenderCommands.Clear();
			m_surfaceRenderer.CullShadowLight(m_shadowRenderCommands, transform, camera, shadowLight);
		}

		private void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera)
		{
			if (camera.cameraType == CameraType.Preview)
				return;

			Texture heightmap = renderHeightmap ? renderHeightmap : sourceHeightmap as Texture;
			m_surfaceRenderer.PreRender(context, transform, camera, heightmap, surfaceProperties.traverseIterations, true);
		}

		private void PostRender(Camera camera)
		{
			if (camera.cameraType == CameraType.Preview)
				return;
			m_surfaceRenderer.RemoveCommandBuffers(camera);
		}

		private void Setup()
        {
			CameraEventCommandBuffer.CleanupUnused();
			CreateResources();
		}

		public void OnRenderModeChanged()
		{
			Setup();
			m_surfaceRenderer.OnEnable();
		}

		public void OnColliderChanged()
		{
			CreateCollision();
			UpdateColliderFull();
		}

		public void OnTerrainChanged()
		{
			InitializeTerrain(sourceHeightmap);
			UpdateColliderFull();
			ISurfaceRenderer.SurfaceDescriptor desc = new ISurfaceRenderer.SurfaceDescriptor()
			{
				dimension = surfaceProperties.dimension,
				heightScale = 1,
				maxHeight = heightScale * 2,
				meshResolution = surfaceProperties.meshResolution,
				heightmapMask = new Vector4(1, 1, 0, 0),
				lodMinMax = surfaceProperties.lodMinMax
			};

			m_surfaceRenderer.UpdateSurfaceDescriptor(desc);
		}

		public virtual void SaveTerrain(string directory, string filename, FileFormat format)
		{
			RenderTextureDescriptor desc = renderHeightmap.descriptor;
			desc.colorFormat = format == FileFormat.PNG ? RenderTextureFormat.ARGB32 : RenderTextureFormat.ARGBHalf;
			desc.sRGB = false;
			RenderTexture tempRT = RenderTexture.GetTemporary(desc);
			CommandBuffer commandBuffer = new CommandBuffer();
			MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
			propertyBlock.Clear();
			propertyBlock.SetTexture(FluidShaderProperties._TerrainHeightField, renderHeightmap);
			propertyBlock.SetFloat(FluidShaderProperties._HeightScale, 1.0f / heightScale);
			propertyBlock.SetVector(FluidShaderProperties._BlitScaleBias, Vector2.one);
			propertyBlock.SetVector(FluidShaderProperties._BlitScaleBiasRt, Vector2.one);
			propertyBlock.SetFloat(FluidShaderProperties._OffsetUVScale, 1);
			FluidSimulation.BlitQuad(commandBuffer, null, tempRT, m_initSimulationMaterial, propertyBlock, m_initSimulationSaveHeightmapPass);
			Graphics.ExecuteCommandBuffer(commandBuffer);

#if UNITY_2021_1_OR_NEWER
			SimulationIO.SaveTexture(tempRT, Path.Join(directory, filename), format);
#else
			SimulationIO.SaveTexture(tempRT, (directory + "/" + filename), format);
#endif

			RenderTexture.ReleaseTemporary(tempRT);
		}

		public virtual void LoadTerrain(string directory, string filename, FileFormat format)
		{
#if UNITY_2021_1_OR_NEWER
			Texture2D texture = SimulationIO.LoadTexture(Path.Join(directory, filename));
#else
			Texture2D texture = SimulationIO.LoadTexture((directory + "/" + filename), format);
#endif
			InitializeTerrain(texture, false);
			reloadedTerrain = true;
		}

		private void CreateTerrain()
		{
			ISurfaceRenderer.Extensions.IsRenderModeSupported(surfaceProperties.renderMode, out surfaceProperties.renderMode);
			ISurfaceRenderer.SurfaceDescriptor desc = new ISurfaceRenderer.SurfaceDescriptor()
			{
				dimension = surfaceProperties.dimension,
				heightScale = 1,
				maxHeight = heightScale * 2,
				meshResolution = surfaceProperties.meshResolution,
				heightmapMask = new Vector4(1, 1, 0, 0),
				lodMinMax = surfaceProperties.lodMinMax
			};
			if (surfaceProperties.renderMode == SurfaceRenderMode.MeshRenderer)
			{
				m_surfaceRenderer?.Dispose();
				m_surfaceRenderer = new MeshRendererSurface(gameObject, desc, surfaceProperties.meshBlocks);
			}
			else if (surfaceProperties.renderMode == SurfaceRenderMode.DrawMesh)
			{
				m_surfaceRenderer?.Dispose();
				m_surfaceRenderer = new MeshSurface(desc, surfaceProperties.meshBlocks);
			}
			else if (surfaceProperties.renderMode == SurfaceRenderMode.GPULOD)
			{
				desc.meshResolution = surfaceProperties.lodResolution;
				m_surfaceRenderer?.Dispose();
				m_surfaceRenderer = new GPULODSurface(desc);
			}
			m_surfaceRenderer.SetupMaterial(terrainMaterial);

			if (m_surfaceRenderer.customRender)
			{
				AddCameraCallbacks();
				RemoveShadowCommands();
				AddShadowCommands();
			}
			else
			{
				RemoveCameraCallbacks();
				RemoveShadowCommands();
			}
		}

		private void CreateCollision()
		{
			if (!Application.isPlaying || !colliderProperties.createCollider)
				return;

			ColliderDescriptor desc = new ColliderDescriptor()
			{
				resolution = colliderProperties.resolution,
				updateFrequency = colliderProperties.updateFrequency,
				timeslicing = colliderProperties.timeslicing,
				realtime = colliderProperties.realtime,
				dimension = surfaceProperties.dimension,
				heightmapMask = new Vector4(1, 1, 1, 1),
				maxHeight = heightScale * 2
			};

			m_collider?.Dispose();
			m_collider = new SurfaceCollider(gameObject, desc);
		}

		private void UpdateColliderFull()
		{
			if (!Application.isPlaying || m_collider == null)
				return;

			m_collider.UpdateFull(renderHeightmap, surfaceProperties.dimension, heightScale * 2);
		}

		private void UpdateCollider()
		{
			if (!Application.isPlaying || m_collider == null)
				return;

			m_collider.Update(renderHeightmap, heightScale * 2);
		}

		protected virtual void Update()
		{
			UpdateCollider();
		}

		protected virtual void LateUpdate()
		{
			CameraEventCommandBuffer.CleanupUnused();

			if (terrainMaterial)
			{
				Texture heightmap = renderHeightmap ? renderHeightmap : sourceHeightmap as Texture;
				Vector2 heightmapRcp = (Vector2.one / new Vector2(heightmap.width + 1, heightmap.height + 1)) * new Vector2(heightmap.width, heightmap.height);
				float wsTexelSizeX = surfaceProperties.dimension.x / heightmap.width;
				float wsTexelSizeY = surfaceProperties.dimension.y / heightmap.height;
				terrainMaterial.SetVector(FluidShaderProperties._TexelWorldSize, new Vector4(wsTexelSizeX, wsTexelSizeY, 1.0f / wsTexelSizeX, 1.0f / wsTexelSizeY));
				terrainMaterial.SetTexture(FluidShaderProperties._HeightField, heightmap);				

				terrainMaterial.SetFloat(FluidShaderProperties._HeightScale, 1);
				m_propertyBlock.SetVector(FluidShaderProperties._HeightmapRcpScale, heightmapRcp);
				m_propertyBlock.SetMatrix(FluidShaderProperties._ObjectToWorld, transform.localToWorldMatrix);
			}

			m_surfaceRenderer.Render(transform, terrainMaterial, m_propertyBlock);
		}
	}
}