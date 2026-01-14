using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using static FluidFrenzy.SurfaceCollider;

namespace FluidFrenzy
{
	/// <summary>
	/// <see cref="FluidSimulation"/> is the core component of Fluid Frenzy. It handles the full simulation and the components attached to it.
	/// </summary>
	/// <remarks>
	/// <para>
	/// This is the base class for all fluid simulation types (<see cref="FluxFluidSimulation"/> and <see cref="FlowFluidSimulation"/>). The system uses the <c>Shallow Water Equations</c> to create a physically-based <c>fluid heightfield simulation</c>. This core technology calculates a velocity field that dictates how the fluid moves and flows over a static, underlying ground heightfield, providing a realistic and performant simulation of large water bodies. It defines common properties and systems used by both solvers.
	/// </para>
	/// 
	/// <h4>Terrain and Obstacle Input</h4>
	/// <para>
	/// The simulation's bottom depth and its interaction with static terrain are managed through a single primary input source. You must select one of the following modes to define the simulation floor, as they are mutually exclusive:
	/// </para>
	/// <list type="bullet">
	/// 	<item><c>UnityTerrain</c> (Uses the standard Unity Terrain component)</item>
	/// 	<item><c>Simple/TerraformTerrain</c> (A custom, simplified terrain for faster results)</item>
	/// 	<item><c>Orthographic layer capture</c> (Captures scene geometry from a camera view)</item>
	/// 	<item><c>Custom heightmap</c> (Uses a texture as the terrain height input)</item>
	/// 	<item><c>MeshCollider</c> (Can be used as a simple, static bottom surface if no other base mode is selected)</item>
	/// </list>
	/// <para>Additionally, <c>FluidObstacle</c> components can be added to the scene to represent dynamic or static objects that interact with the fluid surface, compatible with any of the primary base modes.</para>
	/// 
	/// <h4>Boundary Conditions</h4>
	/// <para>
	/// The simulation handles the outer edges of the simulation grid realistically for various scene types:
	/// </para>
	/// <list type="bullet">
	/// 	<item>
	/// 		<term>Reflective Boundaries</term>
	/// 		<description>The grid boundary forces water to reflect back into the simulation, ideal for enclosed areas like pools or tanks.</description>
	/// 	</item>
	/// 	<item>
	/// 		<term>Open Borders</term>
	/// 		<description>Allows fluid to <c>disappear out of the simulation domain</c> without creating noticeable reflections. This is essential for simulating an open ocean or a river with continuous flow.</description>
	/// 	</item>
	/// </list>
	/// </remarks>
	[AddComponentMenu("")]
	[HelpURL("https://frenzybyte.github.io/fluidfrenzy/docs/fluid_simulation_components/#fluxflow-fluid-simulation")]
	public partial class FluidSimulation : MonoBehaviour
	{
		[SerializeField]
		internal int version = 1;
		/// <summary>
		/// The simulation will always run at a fixed timestep of 60hz to maintain stability, even if your framerate is higher or lower. 
		/// This means that if you have a higher framerate, eventually there will be a frame where the simulation does not have to run. 
		/// Conversely, with a lower framerate, the simulation will need to run multiple times per frame to catch up. 
		/// This is because 2.5D fluid simulations tend to become unstable with high timesteps.
		/// </summary>
		internal const float kMaxTimestep = 1.0f / 60.0f;

		public static Vector2Int kMinBufferSize = new Vector2Int(32,32);
		public static Vector2Int kMaxBufferSize = new Vector2Int(4096,4096);

		public void Reset()
		{
			groupID = GetInstanceID();
		}

		/// <summary>
		/// Defines the different base ground sources the fluid simulation can use to determine the terrain height.
		/// </summary>
		public enum TerrainType
		{
			/// <summary>
			/// Uses the standard Unity <see cref="Terrain"/> component assigned to the <see cref="unityTerrain"/> field.
			/// </summary>
			UnityTerrain,
			/// <summary>
			/// Uses a custom terrain system component, such as <c>SimpleTerrain</c> or <c>TerraformTerrain</c>, often used for performant, run-time editing.
			/// </summary>
			SimpleTerrain,
			/// <summary>
			/// Uses a pre-existing <see cref="Texture2D"/> as a heightmap input.
			/// </summary>
			Heightmap,
			/// <summary>
			/// Uses a <see cref="MeshCollider"/> to define the base ground shape.
			/// </summary>
			MeshCollider,
			/// <summary>
			/// Captures the base heightmap by performing a top-down orthographic render of the scene against specified <see cref="LayerMask">layers</see>.
			/// </summary>
			Layers
		}

		/// <summary>
		/// The <see cref="FluidSimulationSettings">settings</see> asset that controls the core physical parameters and resolution of the simulation.
		/// </summary>
		/// <remarks>
		/// Most settings within the asset can be modified at runtime and automatically update the simulation. 
		/// However, assigning an entirely new settings instance will require the simulation's compute resources to be completely recreated.
		/// </remarks>
		public FluidSimulationSettings settings;

		/// <summary>
		/// Specifies the grouping ID for this <see cref="FluidSimulation"/>. 
		/// This is used to automatically identify and connect with neighbouring simulations that share the same ID.
		/// </summary>
		public int groupID = 0;
		/// <summary>
		/// Specifies the grid position of this simulation within a tiled setup. 
		/// This is used, along with <see cref="groupID"/>, to gather and manage neighbouring <see cref="FluidSimulation">Fluid Simulations</see>
		/// </summary>
		public Vector2Int gridPos = Vector2Int.zero;

		/// <summary>
		/// Selects the method used to determine the world-space <see cref="dimension"/> and <see cref="cellWorldSize"/> of the simulation.
		/// </summary>
		/// <remarks>
		/// <list type="bullet">
		/// 	<item>
		/// 		<term><c>Bounds</c></term>
		/// 		<description>The user sets the total world-space <see cref="dimension"/>. The <see cref="cellWorldSize"/> is then automatically calculated based on the number of cells in the settings.</description>
		/// 	</item>
		/// 	<item>
		/// 		<term><c>CellSize</c></term>
		/// 		<description>The user sets the size of a single cell (<see cref="cellWorldSize"/>). The total world-space <see cref="dimension"/> is then automatically calculated based on the number of cells in the settings.</description>
		/// 	</item>
		/// </list>
		/// </remarks>
		public DimensionMode dimensionMode = DimensionMode.Bounds;

		/// <summary>
		/// The total world-space size (X and Z) of the fluid simulation domain. 
		/// </summary>
		/// <remarks>
		/// This dimension is critical for several components:
		/// <list type="bullet">
		///		<item>
		///			<term>Fluid Renderer</term>
		///			<description>Determines the size of the rendered surface mesh.</description>
		///		</item>
		///		<item>
		///			<term>Fluid Simulation</term>
		///			<description>Scales the behaviour of the fluid (e.g., speed and wave height) to ensure physical consistency regardless of the simulation's size.</description>
		///		</item>
		/// </list>
		/// </remarks>
		public Vector2 dimension = Vector2.one;

		/// <summary>
		/// The world-space size of a single cell (or pixel) in the fluid simulation grid.
		/// </summary>
		/// <remarks>
		/// This value is either user-defined (<see cref="DimensionMode.CellSize"/>) or automatically calculated (<see cref="DimensionMode.Bounds"/>). A smaller value increases resolution but reduces performance.
		/// </remarks>
		public float cellWorldSize = 1;
		/// <summary>
		/// Specifies the uniform fluid height at the start of the simulation. 
		/// </summary>
		/// <remarks>
		/// This value defines the initial water *depth* relative to the terrain height at that point. Specifically, it's the target initial Y-coordinate for the fluid surface. Any terrain geometry below this Y-coordinate will be submerged.
		/// </remarks>
		public float initialFluidHeight = 0;
		/// <summary>
		/// A texture mask that specifies a non-uniform initial fluid height across the domain.
		/// </summary>
		/// <remarks>
		/// This acts as a heightmap, where bright pixels correspond to a higher initial fluid level. The final initial fluid height for any pixel is the maximum of the value sampled from this texture and the uniform <see cref="initialFluidHeight"/>.
		/// </remarks>
		public Texture2D initialFluidHeightTexture;

		/// <summary>
		/// A normalized offset (from 0 to 1) applied to the fluid's base height.
		/// </summary>
		/// <remarks>
		/// This subtle adjustment can be used to prevent visual clipping artifacts that may occur between the fluid surface and underlying tessellated or displaced terrain geometry.
		/// </remarks>
		[Range(0, 1)]
		public float fluidBaseHeight = 0;

		/// <summary>
		/// Specifies which type of scene geometry or data source to use as the base ground for fluid flow calculations.
		/// </summary>
		/// <remarks>
		/// <list type="bullet">
		///		<item>
		///			<term><see cref="TerrainType.UnityTerrain"/></term>
		///			<description> Use a standard Unity <see cref="Terrain"/> assigned to the <see cref="unityTerrain"/> field. </description>
		///		</item>
		///		<item>
		///			<term><see cref="TerrainType.SimpleTerrain"/></term>
		///			<description> Use a custom terrain component (e.g., <c>SimpleTerrain</c> or <c>TerraformTerrain</c>) assigned to the <see cref="simpleTerrain"/> field. </description>
		///		</item>
		///		<item>
		///			<term><see cref="TerrainType.Heightmap"/></term>
		///			<description> Use a <see cref="Texture2D"/> as a heightmap assigned to the <see cref="textureHeightmap"/> field. This is useful for custom terrain systems.</description>
		///		</item>
		///		<item>
		///			<term><see cref="TerrainType.MeshCollider"/></term>
		///			<description> Use a static <see cref="MeshCollider"/> assigned to the <see cref="meshCollider"/> field as the base ground.</description>
		///		</item>
		///		<item>
		///			<term><see cref="TerrainType.Layers"/></term>
		///			<description> Generate the base heightmap by capturing layers via a top-down orthographic render.</description>
		///		</item>
		/// </list>
		/// </remarks>
		public TerrainType terrainType;
		/// <summary>
		/// Assign a Unity <see cref="Terrain"/> to be used as the simulation's base ground when <see cref="terrainType"/> is <see cref="TerrainType.UnityTerrain"/>.
		/// </summary>
		public Terrain unityTerrain = null;
		/// <summary>
		/// Assign a custom terrain component, such as <c>SimpleTerrain</c> or <c>TerraformTerrain</c>, to be used as the base ground when <see cref="terrainType"/> is <see cref="TerrainType.SimpleTerrain"/>.
		/// </summary>
		public SimpleTerrain simpleTerrain = null;
		/// <summary>
		/// Assign a heightmap <see cref="Texture2D"/> to be used as the simulation's base ground when <see cref="terrainType"/> is <see cref="TerrainType.Heightmap"/>.
		/// </summary>
		public Texture2D textureHeightmap = null;
		/// <summary>
		/// A global multiplier applied to the sampled height values from the <see cref="textureHeightmap"/>.
		/// </summary>
		public float heightmapScale = 1.0f;
		/// <summary>
		/// Assign a <see cref="MeshCollider"/> component to be used as the simulation's base ground when <see cref="terrainType"/> is <see cref="TerrainType.MeshCollider"/>.
		/// </summary>
		public MeshCollider meshCollider = null;
		/// <summary>
		/// A layer mask used to filter which scene objects are captured when <see cref="terrainType"/> is <see cref="TerrainType.Layers"/>.
		/// </summary>
		public LayerMask captureLayers = -1;
		/// <summary>
		/// The vertical extent, measured in world units, for the top-down orthographic capture when <see cref="terrainType"/> is <see cref="TerrainType.Layers"/>.
		/// </summary>
		/// <remarks>
		/// The orthographic render captures geometry from the simulation object's Y position up to <c>transform.position.y + captureHeight</c>.
		/// </remarks>
		public float captureHeight = 100;

		/// <summary>
		/// The vertical scale factor that was applied to initialize the internal terrain heightmap used by the simulation.
		/// </summary>
		public float terrainScale { get; protected set; } = 0;

		/// <summary>
		/// If true, the simulation re-samples the underlying geometry (Unity Terrain, Meshes, Layers, or Colliders) every frame. 
		/// Enable this if your ground surface is deforming or moving during the simulation.
		/// </summary>
		public bool updateGroundEveryFrame = false;

		/// <summary>
		/// A list of optional <see cref="FluidLayer"/> extensions (e.g., <c>FoamLayer</c>, <c>FluidFlowMapping</c>) that should be executed and managed by this fluid simulation.
		/// </summary>
		public List<FluidLayer> extensionLayers = new List<FluidLayer>();

		/// <summary>
		/// Properties used to control the generation and configuration of the <see cref="MeshCollider"/> representing the fluid surface.
		/// </summary>
		/// <remarks>
		/// These settings determine the physical shape and properties of the fluid's surface when interacting with objects via Unity's physics system.
		/// </remarks>
		public SurfaceCollider.ColliderProperties colliderProperties = SurfaceCollider.ColliderProperties.DefaultFluid;

		//IOS 16 and higher do not seem to support blending on 32bit float rendertextures, we will add blend to 16bit first and do a custom add shader with that.
		internal bool platformSupportsFloat32Blend { get; private set; } = true;
		internal bool platformSupportsLinearFilterSimulation { get; private set; } = true;

		//Shared Fluid Simulation Buffers
		protected RenderTexture m_terrainHeight = null;
		protected RenderTexture m_obstacleHeight = null;
		protected RenderTexture m_capturedSceneHeight = null;
		protected Vector4 m_terrainTextureST = Vector2.one;
		protected Vector4 m_paddingST = Vector2.one;

		protected RenderTexture m_activeWaterHeight = null;
		protected RenderTexture m_nextWaterHeight = null;

		protected RenderTexture m_staticInput = null;
		protected RenderTexture m_dynamicInput = null;

		protected GraphicsBuffer m_solidToFluidHeightDelta;
		protected GraphicsBuffer m_solidToFluidVelocityDelta;

		//Boundary Copy storage textures
		protected RenderTexture[] m_velocityBoundaryCells = null;
		protected RenderTexture[] m_waterBoundaryCells = null;
		protected RenderTexture[] m_terrainBoundaryCells = null;

		//Flow and advection data
		protected RenderTexture m_activeVelocity = null;
		protected RenderTexture m_nextVelocity = null;
		protected Vector4 m_velocityTextureST = Vector2.one;

		protected Vector4 m_simulationTexelSize = Vector2.one;

		protected virtual RenderTextureFormat velocityFormat { get { return RenderTextureFormat.RGHalf; } }

		//Rendering data
		protected RenderTexture m_activeHeightVelocityTexture = null;
		protected RenderTexture m_nextHeightVelocityTexture = null;
		protected RenderTexture m_normalMap = null;

		protected RenderTexture m_fluidSDF = null;

		//Command buffers
		protected CommandBuffer m_commandBuffer = null;
		protected CommandBuffer m_dynamicCommandBuffer = null;
		protected CommandBuffer m_neighbourCopyCommandBuffer = null;

		//We build commandbuffers once if things dont change, this reduces CPU overhead.
		protected StaticCommandBuffer m_staticCommandBuffers = new StaticCommandBuffer();
		protected StaticCommandBuffer m_velocityCommandBuffers = new StaticCommandBuffer();
		protected StaticCommandBuffer m_renderInfoCommandBuffers = new StaticCommandBuffer();

		//Check to see if settings changed so we can rebuild the commandbuffer with new settings
		protected bool m_settingsChanged = false;

		//Check to see if settings changed so we can rebuild the commandbuffer with new settings
		protected bool m_updateObstacles = false;

		protected MaterialPropertyBlock m_externalPropertyBlock = null;
		protected MaterialPropertyBlock m_internalPropertyBlock = null;

		private SurfaceCollider m_collider;

		private Transform m_cachedTransform;
		private Vector3 m_cachedPosition;

		private Camera m_captureCamera = null;

		private bool m_hasSecondVelocityLayer = false;

		//If we dont have any static input then we dont need to use this pass so its nice to save this overhead if its not used
		protected bool m_hasStaticWaterInput = false;

		//Internal accumulated simulation time.
		protected float m_timestep = 0;

		protected virtual void Awake()
		{
			m_cachedTransform = transform;
			m_cachedPosition = transform.position;
			FluidSimulationManager.Register(this);

			if (Application.platform == RuntimePlatform.WebGLPlayer)
			{
				string os = SystemInfo.operatingSystem.ToLower();
				platformSupportsFloat32Blend = !os.Contains("iphone") && !os.Contains("ipad") && !os.Contains("macos");
				platformSupportsLinearFilterSimulation = !os.Contains("iphone") && !os.Contains("ipad") && !os.Contains("android") && !os.Contains("macos");
			}

#if UNITY_6000_0_OR_NEWER
			if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.WebGPU)
			{
				platformSupportsFloat32Blend = SystemInfo.SupportsBlendingOnRenderTextureFormat(RenderTextureFormat.ARGBFloat);
			}
#endif

			m_commandBuffer = new CommandBuffer();
			m_commandBuffer.name = name + "CommandBuffer";

			m_dynamicCommandBuffer = new CommandBuffer();
			m_dynamicCommandBuffer.name = name + "DynamicCommandBuffer";

			m_neighbourCopyCommandBuffer = new CommandBuffer();
			m_neighbourCopyCommandBuffer.name = name + "NeighbourCopyCommandBuffer";

			m_externalPropertyBlock = new MaterialPropertyBlock();
			m_internalPropertyBlock = new MaterialPropertyBlock();

			if (m_readbackHeightVelocityAction == null)
			{
				m_readbackHeightVelocityAction = new Action<AsyncGPUReadbackRequest>(ReadbackHeightVelocityCallback);
			}

			if (m_readbackSDFAction == null)
			{
				m_readbackSDFAction = new Action<AsyncGPUReadbackRequest>(ReadbackSDFCallback);
			}

			m_hasSecondVelocityLayer = this is FlowFluidSimulation;

		}

		protected virtual void Start()
		{
			CalculateDimensions();
		}


		protected virtual void OnDestroy()
		{
			FluidSimulationManager.Deregister(this);

			GraphicsHelpers.ReleaseSimulationRT(m_activeVelocity);
			GraphicsHelpers.ReleaseSimulationRT(m_nextVelocity);
			GraphicsHelpers.ReleaseSimulationRT(m_activeHeightVelocityTexture);
			GraphicsHelpers.ReleaseSimulationRT(m_terrainHeight);
			GraphicsHelpers.ReleaseSimulationRT(m_activeWaterHeight);
			GraphicsHelpers.ReleaseSimulationRT(m_nextWaterHeight);
			GraphicsHelpers.ReleaseSimulationRT(m_dynamicInput);

			// Release water boundary cells
			if (m_waterBoundaryCells != null)
			{
				foreach (var texture in m_waterBoundaryCells)
				{
					GraphicsHelpers.ReleaseSimulationRT(texture);
				}
			}

			// Release terrain boundary cells
			if (m_terrainBoundaryCells != null)
			{
				foreach (var texture in m_terrainBoundaryCells)
				{
					GraphicsHelpers.ReleaseSimulationRT(texture);
				}
			}

			// Release velocity boundary cells if they exist
			if (m_velocityBoundaryCells != null)
			{
				foreach (var texture in m_velocityBoundaryCells)
				{
					GraphicsHelpers.ReleaseSimulationRT(texture);
				}
			}

			if (m_solidToFluidHeightDelta != null)
			{
				m_solidToFluidHeightDelta.Release();
				m_solidToFluidHeightDelta = null;
			}
			
			if (m_solidToFluidVelocityDelta != null)
			{
				m_solidToFluidVelocityDelta.Release();
				m_solidToFluidVelocityDelta = null;
			}

			if (m_heightVelocityData.IsCreated)
				m_heightVelocityData.Dispose();

			if(m_sdfData.IsCreated)
				m_sdfData.Dispose();

			DestroyMaterials();
		}

		protected virtual void InitRenderTextures()
		{
			InitHeightReadbackData();
			InitFluidData();

			if (!platformSupportsFloat32Blend)
			{
				RenderTextureFormat dynamicInputFormat = multiLayeredFluid ? RenderTextureFormat.RGHalf : RenderTextureFormat.RHalf;
				m_dynamicInput = GraphicsHelpers.CreateSimulationRT(numSimulationCells.x, numSimulationCells.y, dynamicInputFormat, name: "DynamicFluidInput");
			}
			m_staticInput = GraphicsHelpers.CreateSimulationRT(numSimulationCells.x, numSimulationCells.y, RenderTextureFormat.RHalf, name: "StaticFluidInput");

			InitRenderData();
			InitSDFData();
		}

		private void InitHeightReadbackData()
		{
			if (settings.readBackHeight && !m_heightVelocityData.IsCreated)
			{
				m_heightVelocityData = new NativeArray<long>(numRenderCells.x * numRenderCells.y, Allocator.Persistent);
				m_renderDataWidth = numRenderCells.x;
				m_renderDataHeight = numRenderCells.y;
			}
		}

		private void InitFluidData()
		{
			GraphicsFormat terrainFormat = (terrainType == TerrainType.UnityTerrain && unityTerrain) ? unityTerrain.terrainData.heightmapTexture.graphicsFormat : GraphicsFormat.R16_SFloat;
			RenderTextureFormat fluidFormat = multiLayeredFluid ? RenderTextureFormat.RGFloat : RenderTextureFormat.RFloat;
			FilterMode fluidFilterMode = platformSupportsLinearFilterSimulation ? FilterMode.Bilinear : FilterMode.Point;
			m_terrainHeight = GraphicsHelpers.CreateSimulationRT(numSimulationCells.x, numSimulationCells.y, terrainFormat, true, fluidFilterMode, name: "Terrain");

			Vector2 terrainTexel = new Vector2(1.0f / m_terrainHeight.width, 1.0f / m_terrainHeight.height);
			// terrain and velocity are the same in this case
			m_terrainTextureST = new Vector4(1.0f - terrainTexel.x * ghostCells2.x,
									1.0f - terrainTexel.y * ghostCells2.y,
									terrainTexel.x * ghostCells.x,
									terrainTexel.y * ghostCells.y);


			m_activeWaterHeight = GraphicsHelpers.CreateSimulationRT(numSimulationCells.x, numSimulationCells.y, fluidFormat, true, fluidFilterMode, name: "FluidHeight_1");
			m_nextWaterHeight = GraphicsHelpers.CreateSimulationRT(numSimulationCells.x, numSimulationCells.y, fluidFormat, true, fluidFilterMode, name: "FluidHeight_2");


			if (SystemInfo.supportsComputeShaders)
			{
				int totalCellCount = numSimulationCells.x * numSimulationCells.y;
				int heightElementSize = sizeof(int);
				m_solidToFluidHeightDelta = new GraphicsBuffer(GraphicsBuffer.Target.Structured, totalCellCount, heightElementSize);
				int velocityElementSize = sizeof(uint);
				m_solidToFluidVelocityDelta = new GraphicsBuffer(GraphicsBuffer.Target.Structured, totalCellCount, velocityElementSize);
			}

			Vector2Int obstacleSize = GetSourceHeightmapSize();
			m_obstacleHeight = GraphicsHelpers.CreateSimulationRT(obstacleSize.x, obstacleSize.y, RenderTextureFormat.RHalf, true, fluidFilterMode, name: "Obstacles");

			m_terrainBoundaryCells = new RenderTexture[(int)BoundarySides.Max];
			for (int i = 0; i < m_terrainBoundaryCells.Length; i++)
			{
				m_terrainBoundaryCells[i] = GraphicsHelpers.CreateSimulationRT(m_activeWaterHeight.width, ghostCells.x, terrainFormat, true, filterMode: FilterMode.Point, name: "BoundaryCopy");
			}

			m_waterBoundaryCells = new RenderTexture[(int)BoundarySides.Max];
			for (int i = 0; i < m_waterBoundaryCells.Length; i++)
			{
				m_waterBoundaryCells[i] = GraphicsHelpers.CreateSimulationRT(m_activeWaterHeight.width, ghostCells.x, RenderTextureFormat.ARGBFloat, true, filterMode: FilterMode.Point, name: "BoundaryCopy");
			}
		}

		private void InitRenderData()
		{
#if UNITY_2023_2_OR_NEWER
			GraphicsFormat normalMapFormat = SystemInfo.IsFormatSupported(GraphicsFormat.R16G16B16A16_SNorm, GraphicsFormatUsage.Render) ? GraphicsFormat.R16G16B16A16_SNorm : GraphicsFormat.R16G16B16A16_SFloat;
#else
			GraphicsFormat normalMapFormat = SystemInfo.IsFormatSupported(GraphicsFormat.R16G16B16A16_SNorm, FormatUsage.Render) ? GraphicsFormat.R16G16B16A16_SNorm : GraphicsFormat.R16G16B16A16_SFloat;
#endif

			m_normalMap = GraphicsHelpers.CreateSimulationRT(numRenderCells.x, numRenderCells.y, normalMapFormat, name: "NormalMap");
			m_activeHeightVelocityTexture = GraphicsHelpers.CreateSimulationRT(numRenderCells.x, numRenderCells.y, RenderTextureFormat.ARGBHalf, name: "HeightVelocityTexture_1");
			m_nextHeightVelocityTexture = GraphicsHelpers.CreateSimulationRT(numRenderCells.x, numRenderCells.y, RenderTextureFormat.ARGBHalf, name: "HeightVelocityTexture_2");
		}

		private void InitSDFData()
		{
			int numSdfDownsamples = settings.distanceFieldDownsample + 1;

			if (settings.distanceFieldReadback)
			{
				if (m_fluidSDF == null)
				{
					m_fluidSDF = GraphicsHelpers.CreateSimulationRT(numRenderCells.x / numSdfDownsamples, numRenderCells.y / numSdfDownsamples, RenderTextureFormat.RGHalf, name: "FluidSDF");
				}

				if (!m_sdfData.IsCreated)
				{
					m_sdfData = new NativeArray<int>(m_fluidSDF.width * m_fluidSDF.height, Allocator.Persistent);
					m_sdfDataWidth = m_fluidSDF.width;
					m_sdfDataHeight = m_fluidSDF.height;
				}
			}
		}

		protected virtual void ResetRenderTextures()
		{
			RenderTexture previousRT = RenderTexture.active;

			RenderTexture.active = m_activeVelocity;
			GL.Clear(false, true, Color.clear);

			RenderTexture.active = m_nextVelocity;
			GL.Clear(false, true, Color.clear);

			RenderTexture.active = m_activeHeightVelocityTexture;
			GL.Clear(false, true, Color.clear);

			RenderTexture.active = m_terrainHeight;
			GL.Clear(false, true, Color.clear);

			RenderTexture.active = m_activeWaterHeight;
			GL.Clear(false, true, Color.clear);

			RenderTexture.active = m_nextWaterHeight;
			GL.Clear(false, true, Color.clear);

			RenderTexture.active = m_staticInput;
			GL.Clear(false, true, Color.clear);

			if (!platformSupportsFloat32Blend)
			{
				RenderTexture.active = m_dynamicInput;
				GL.Clear(false, true, Color.clear);
			}

			for (int i = 0; i < m_waterBoundaryCells.Length; i++)
			{
				RenderTexture.active = m_waterBoundaryCells[i];
				GL.Clear(false, true, Color.clear);
			}

			for (int i = 0; i < m_terrainBoundaryCells.Length; i++)
			{
				RenderTexture.active = m_terrainBoundaryCells[i];
				GL.Clear(false, true, Color.clear);
			}

			if (velocityGhostCells > 0)
			{
				for (int i = 0; i < m_velocityBoundaryCells.Length; i++)
				{
					RenderTexture.active = m_velocityBoundaryCells[i];
					GL.Clear(false, true, Color.clear);
				}
			}

			RenderTexture.active = previousRT;
		}

		void ResetSolidToFluidBuffers()
		{
			if(!SystemInfo.supportsComputeShaders)
			{
				return;
			}
			int totalCellCount = numSimulationCells.x * numSimulationCells.y;
			if (m_solidToFluidCS != null)
			{
				m_solidToFluidCS.SetInt(FluidShaderProperties._BufferWidth, totalCellCount);
				m_solidToFluidCS.SetBuffer(m_solidToFluidClearBuffersKernel, FluidShaderProperties._HeightAccumulator, m_solidToFluidHeightDelta);
				m_solidToFluidCS.SetBuffer(m_solidToFluidClearBuffersKernel, FluidShaderProperties._VelocityAccumulator, m_solidToFluidVelocityDelta);

				m_solidToFluidCS.GetKernelThreadGroupSizes(m_solidToFluidClearBuffersKernel, out uint threadGroupCountX, out _, out _);
				m_solidToFluidCS.Dispatch(m_solidToFluidClearBuffersKernel, GraphicsHelpers.DCS(totalCellCount, threadGroupCountX), 1, 1);
			}
		}

		public virtual void ResetSimulation()
		{
			m_timestep = 0;
			ResetRenderTextures();
			ResetSolidToFluidBuffers();
			InitTerrain();
			UpdateStaticWaterInput();

			foreach (FluidLayer layer in extensionLayers)
				layer.ResetLayer(this);

			m_internalPropertyBlock.Clear();
			m_internalPropertyBlock.SetTexture(FluidShaderProperties._TerrainHeightField, m_terrainHeight);
			m_internalPropertyBlock.SetFloat(FluidShaderProperties._TerrainHeightScale, terrainScale);
			m_internalPropertyBlock.SetFloat(FluidShaderProperties._HeightScale, heightmapScale);
			m_internalPropertyBlock.SetFloat(FluidShaderProperties._WaterHeight, initialFluidHeight);
			m_internalPropertyBlock.SetVector(FluidShaderProperties._BlitScaleBias, Vector2.one);
			m_internalPropertyBlock.SetVector(FluidShaderProperties._BlitScaleBiasRt, Vector2.one);
			int initFluidpass = (terrainType == TerrainType.UnityTerrain) ? m_initSimulationFluidHeightUnityPass : m_initSimulationFluidHeightPass;
			BlitQuad(m_dynamicCommandBuffer, initialFluidHeightTexture ? initialFluidHeightTexture : Texture2D.blackTexture, m_activeWaterHeight, m_initSimulationMaterial, m_internalPropertyBlock, initFluidpass);
			m_dynamicCommandBuffer.Blit(m_activeWaterHeight, m_nextWaterHeight);
			Graphics.ExecuteCommandBuffer(m_dynamicCommandBuffer);
			m_dynamicCommandBuffer.Clear();

			m_staticCommandBuffers.Clear();
			m_velocityCommandBuffers.Clear();
			m_renderInfoCommandBuffers.Clear();

			if (!settings.openBorders)
			{
				m_addFluidMaterial.DisableKeyword("OPEN_BORDER");
			}
			else
			{
				m_addFluidMaterial.EnableKeyword("OPEN_BORDER");
			}
		}

		private void RefreshTerrain()
		{
			Texture heightmap = null;
			Vector4 uvScaleOffset = Vector2.one;
			GetSourceHeightmapData(ref heightmap, ref heightmapScale, ref uvScaleOffset);
			if (!heightmap)
			{
				return;
			}

			CopyTerrainToSimulation(heightmap, heightmapScale, uvScaleOffset, true);

			UpdateTerrainBorders();

			if (terrainType == TerrainType.MeshCollider && heightmap is RenderTexture releaseRT)
				RenderTexture.ReleaseTemporary(releaseRT);
		}

		public Vector4 CalculateTerrainUVScaleOffset()
		{
			Vector2 sourceDimenions = new Vector2(unityTerrain.terrainData.size.x, unityTerrain.terrainData.size.z);
			Vector3 terrainBottomLeft = unityTerrain.transform.position;
			Vector3 terrainTopRight = unityTerrain.transform.position + unityTerrain.terrainData.size;

			Bounds currentBounds = CalculateBounds();
			float xPos = Mathf.InverseLerp(terrainBottomLeft.x, terrainTopRight.x, currentBounds.min.x);
			float yPos = Mathf.InverseLerp(terrainBottomLeft.z, terrainTopRight.z, currentBounds.min.z);

			float xScale = dimension.x / sourceDimenions.x;
			float yScale = dimension.y / sourceDimenions.y;

			return new Vector4(xScale, yScale, xPos, yPos);
		}

		internal Vector2Int GetSourceHeightmapSize()
		{
			Texture sourceTexture = null;
			Vector2Int sourceSize = numSimulationCells;
			if (terrainType == TerrainType.SimpleTerrain)
			{
				sourceTexture = simpleTerrain.sourceHeightmap;
			}
			else if (terrainType == TerrainType.UnityTerrain && unityTerrain)
			{
				sourceTexture = unityTerrain.terrainData.heightmapTexture;
			}

			if(sourceTexture)
			{
				sourceSize = new Vector2Int(sourceTexture.width, sourceTexture.height);
			}
			return sourceSize;
		}
		internal void GetSourceHeightmapData(ref Texture heightmap, ref float sourceScale, ref Vector4 uvScaleOffset)
		{
			uvScaleOffset = Vector2.one;
			if (terrainType == TerrainType.SimpleTerrain)
			{
				heightmap = simpleTerrain.sourceHeightmap;
				sourceScale = simpleTerrain.heightScale;
				terrainScale = 1;
			}
			else if (terrainType == TerrainType.UnityTerrain && unityTerrain)
			{
				heightmap = unityTerrain.terrainData.heightmapTexture;
				sourceScale = 1;// unityTerrain.terrainData.heightmapScale.y * (65535.0f / 32766);
				terrainScale = unityTerrain.terrainData.heightmapScale.y * (65535.0f / 32766);

				uvScaleOffset = CalculateTerrainUVScaleOffset();

			}
			else if (terrainType == TerrainType.MeshCollider && meshCollider)
			{
				RenderTexture rt = RenderTexture.GetTemporary(settings.numberOfCells.x + 1, settings.numberOfCells.y + 1, 0, RenderTextureFormat.RFloat);
				rt.Create();
				float size = dimension.x * 0.5f;

				float colliderBase = meshCollider.bounds.center.y - meshCollider.bounds.extents.y;
				float offset = (colliderBase - m_cachedTransform.position.y) + (meshCollider.bounds.size.y + 0.1f);
				Matrix4x4 viewMatrix = Matrix4x4.TRS(m_cachedTransform.position + Vector3.up * offset, Quaternion.Euler(90, 0, 0), new Vector3(1, 1, -1)).inverse;
				Matrix4x4 projMatrix = Matrix4x4.Ortho(-size, size, -size, size, 0.1f, 10000);
				m_dynamicCommandBuffer.SetViewProjectionMatrices(viewMatrix, projMatrix);
				m_dynamicCommandBuffer.SetRenderTarget(rt);
				m_dynamicCommandBuffer.ClearRenderTarget(false, true, Color.clear);
				m_dynamicCommandBuffer.DrawMesh(meshCollider.sharedMesh, meshCollider.transform.localToWorldMatrix, m_obstacleMaterial, 0);
				Graphics.ExecuteCommandBuffer(m_dynamicCommandBuffer);
				m_dynamicCommandBuffer.Clear();
				sourceScale = 1;
				terrainScale = 1;
				heightmap = rt;
			}
			else if (terrainType == TerrainType.Layers)
			{
				terrainScale = 1;
				heightmap = m_capturedSceneHeight;
			}
			else
			{
				heightmap = textureHeightmap;
				terrainScale = 1;
			}
		}

		private void CaptureHeightmap()
		{
			if (terrainType != TerrainType.Layers)
				return;

			m_capturedSceneHeight ??= GraphicsHelpers.CreateSimulationRT(numRenderCells.x, numRenderCells.y, RenderTextureFormat.Depth, false, name: "CapturedScene", depth: 24);
			Dictionary<Terrain, bool> terrainState = new Dictionary<Terrain, bool>(Terrain.activeTerrains.Length);
			foreach (Terrain terrain in Terrain.activeTerrains)
			{
				terrainState.Add(terrain, terrain.drawTreesAndFoliage);
				terrain.drawTreesAndFoliage = false;
			}

			if (m_captureCamera == null)
			{
				m_captureCamera = new GameObject("CameraObject").gameObject.AddComponent<Camera>();
				m_captureCamera.gameObject.hideFlags = HideFlags.HideInHierarchy;
				m_captureCamera.clearFlags = CameraClearFlags.Depth;
				m_captureCamera.backgroundColor = Color.clear;
			}

			m_captureCamera.transform.SetParent(transform);
			m_captureCamera.transform.localPosition = new Vector3(0, captureHeight, 0);
			m_captureCamera.transform.LookAt(transform);

			m_captureCamera.orthographic = true;
			m_captureCamera.orthographicSize = Mathf.Min(dimension.x, dimension.y) * 0.5f;
			m_captureCamera.targetTexture = m_capturedSceneHeight;
			m_captureCamera.nearClipPlane = 0.01f;
			m_captureCamera.farClipPlane = captureHeight;

			m_captureCamera.cullingMask = captureLayers;
			m_captureCamera.Render();
			m_captureCamera.enabled = false;

			foreach (Terrain terrain in Terrain.activeTerrains)
			{
				terrain.drawTreesAndFoliage = terrainState[terrain];
			}

			if (!updateGroundEveryFrame)
			{
				m_captureCamera.targetTexture = null;
				Destroy(m_captureCamera.gameObject, 0.1f);
			}
		}

		protected void InitTerrain()
		{
			UpdateTerrain(true);
		}

		public void UpdateTerrain(bool force = false, RenderTexture target = null)
		{
			if (m_updateObstacles || force)
			{
				if(force)
					CaptureHeightmap();

#if FLUIDFRENZY_RPCORE_15_OR_NEWER
				using (new ProfilingScope(ProfilingSampler.Get(WaterSimProfileID.DrawObstacles)))
#else
				using (new ProfilingScope(m_dynamicCommandBuffer, ProfilingSampler.Get(WaterSimProfileID.DrawObstacles)))
#endif
				{
					Vector2 size = dimension * 0.5f * (Vector2.one + (ghostCells2 + Vector2Int.one) / new Vector2(m_obstacleHeight.width, m_obstacleHeight.height));
					Matrix4x4 viewMatrix = Matrix4x4.TRS(m_cachedTransform.position + Vector3.up * 1000, Quaternion.Euler(90, 0, 0), new Vector3(1, 1, -1)).inverse;
					Matrix4x4 projMatrix = Matrix4x4.Ortho(-size.x, size.x, -size.y, size.y, 0.1f, 10000);
					m_dynamicCommandBuffer.SetViewProjectionMatrices(viewMatrix, projMatrix);
					m_dynamicCommandBuffer.SetRenderTarget(m_obstacleHeight);
					m_dynamicCommandBuffer.SetGlobalVector(FluidShaderProperties._SimulationPositionWS, m_cachedTransform.position);
					m_dynamicCommandBuffer.ClearRenderTarget(false, true, Color.clear);
					foreach (FluidSimulationObstacle obstacle in FluidSimulationObstacle.waterObstacles)
					{
						if (obstacle.isActiveAndEnabled)
						{
							obstacle.Process(this, m_dynamicCommandBuffer, m_obstacleMaterial);
						}
					}
				}
				Graphics.ExecuteCommandBuffer(m_dynamicCommandBuffer);
				m_dynamicCommandBuffer.Clear();

				m_updateObstacles = false;

				bool refreshTerrain = true;
				if (TryGetFluidLayer(out TerraformLayer layer))
				{
					refreshTerrain = !layer.isActiveAndEnabled;
				}
				if (TryGetFluidLayer(out layer))
				{
					refreshTerrain = !layer.isActiveAndEnabled;
				}

				refreshTerrain |= force;

				if (refreshTerrain)
				{
					RefreshTerrain();
				}

			}
		}

		internal void CopyTerrainToSimulation(Texture heightmap, float scale, Vector4 uvScaleOffset, bool combine, RenderTexture target = null)
		{
			if (!heightmap)
			{
				heightmap = Texture2D.blackTexture;
			}

			using (new ProfilingScope(m_dynamicCommandBuffer, ProfilingSampler.Get(WaterSimProfileID.CopyTerrain)))
			{
				int pass = combine ? m_initSimulationCopyHeightmapCombinePass : m_initSimulationCopyHeightmapPass;
				float offsetUVScale = 1;
				if (terrainType == TerrainType.UnityTerrain)
				{
					pass = m_initSimulationCopyTerrainPass;
				}
				else if (terrainType == TerrainType.MeshCollider)
				{
					offsetUVScale = 0;
				}
				else if (terrainType == TerrainType.Layers)
				{
					pass = m_initSimulationCopyFromDepthPass;
					offsetUVScale = 0;
				}
				else if (terrainType == TerrainType.SimpleTerrain)
				{
					offsetUVScale = Mathf.IsPowerOfTwo(heightmap.width) ? 1 : 0;
				}

				Texture obstacles = Texture2D.blackTexture;
				if (target == null)
				{
					target = m_terrainHeight;
					obstacles = m_obstacleHeight;
				}

				Vector2 offset = new Vector2(1.0f / m_terrainHeight.width, 1.0f / m_terrainHeight.height);

				Vector4 blitScale = new Vector4(1 - offset.x * ghostCells2.x,
												1 - offset.y * ghostCells2.y,
												offset.x * ghostCells.x,
												offset.y * ghostCells.y);

				int samplesX = Mathf.RoundToInt(((float)(heightmap.width * uvScaleOffset.x) / m_terrainHeight.width) / 2);
				int samplesY = Mathf.RoundToInt(((float)(heightmap.height * uvScaleOffset.y) / m_terrainHeight.height) / 2);


				m_initSimulationMaterial.DisableKeyword("_DOWN_SAMPLE_NXN");
				m_initSimulationMaterial.DisableKeyword("_DOWN_SAMPLE_2X2");
				m_initSimulationMaterial.DisableKeyword("_DOWN_SAMPLE_2X1");
				m_initSimulationMaterial.DisableKeyword("_DOWN_SAMPLE_1X2");
				if (Application.platform == RuntimePlatform.WebGLPlayer)
				{
					if (samplesX > 1 && samplesY > 0)
					{
						m_initSimulationMaterial.EnableKeyword("_DOWN_SAMPLE_2X1");
					}
					else if (samplesX > 0 && samplesY > 1)
					{
						m_initSimulationMaterial.EnableKeyword("_DOWN_SAMPLE_1X2");
					}
					else if(samplesX > 0 && samplesY > 0)
					{
						m_initSimulationMaterial.EnableKeyword("_DOWN_SAMPLE_2X2");
					}
				}
				else
				{
					if (samplesX > 0 && samplesY > 0)
					{
						m_initSimulationMaterial.EnableKeyword("_DOWN_SAMPLE_NXN");
					}
				}

				Vector2 obstaclePaddingTexel = new Vector2(1.0f / m_obstacleHeight.width, 1.0f / m_obstacleHeight.width);
				Vector4 obstaclePadding = new Vector4(1.0f - obstaclePaddingTexel.x * ghostCells2.x,
							1.0f - obstaclePaddingTexel.y * ghostCells2.y,
							obstaclePaddingTexel.x * ghostCells.x,
							obstaclePaddingTexel.y * ghostCells.y);

				m_internalPropertyBlock.Clear();
				m_internalPropertyBlock.SetTexture(FluidShaderProperties._TerrainHeightField, heightmap);
				m_internalPropertyBlock.SetFloat(FluidShaderProperties._TerrainHeightScale, terrainScale);
				m_internalPropertyBlock.SetTexture(FluidShaderProperties._Obstacles, obstacles);
				m_internalPropertyBlock.SetFloat(FluidShaderProperties._HeightScale, scale);
				m_internalPropertyBlock.SetFloat(FluidShaderProperties._OffsetUVScale, offsetUVScale);
				m_internalPropertyBlock.SetVector(FluidShaderProperties._BlitScaleBias, Vector2.one);
				m_internalPropertyBlock.SetVector(FluidShaderProperties._BlitScaleBiasRt, blitScale);
				m_internalPropertyBlock.SetVector(FluidShaderProperties._TransformScale, new Vector4(uvScaleOffset.x, uvScaleOffset.y, uvScaleOffset.z, uvScaleOffset.w));
				m_internalPropertyBlock.SetInt(FluidShaderProperties._SampleCountX, samplesX);
				m_internalPropertyBlock.SetInt(FluidShaderProperties._SampleCountY, samplesY);
				m_internalPropertyBlock.SetFloat(FluidShaderProperties._CaptureHeight, captureHeight);
				m_internalPropertyBlock.SetVector(FluidShaderProperties._Padding_ST, m_paddingST);
				m_internalPropertyBlock.SetVector(FluidShaderProperties._ObstaclePadding_ST, obstaclePadding);

				BlitQuad(m_dynamicCommandBuffer, null, target, m_initSimulationMaterial, m_internalPropertyBlock, pass);
			}
			Graphics.ExecuteCommandBuffer(m_dynamicCommandBuffer);
			m_dynamicCommandBuffer.Clear();
		}

		public virtual void PreUpdate(float deltaTime, int numSteps = 2)
		{
			m_timestep += deltaTime;

			CheckSettingsChanged();
			CopyNeighboursData();
		}

		protected void UpdateFluidRigidBody()
		{
			if (FluidRigidBody.fluidRigidBodies.Length > 0 && SystemInfo.supportsComputeShaders)
			{
				foreach (FluidRigidBody solid in FluidRigidBody.fluidRigidBodies)
				{
					if (!solid.isActiveAndEnabled || !solid.applyFluidDisplacement)
					{
						continue;
					}
					solid.Step(this);
				}

				if (SystemInfo.supportsComputeShaders)
				{
					UpdateSolidToFluid();
				}

				Graphics.ExecuteCommandBuffer(m_dynamicCommandBuffer);
				m_dynamicCommandBuffer.Clear();
			}
		}

		/// <summary>
		/// Advances the internal simulation time of the <see cref="FluidSimulation"/> by the specified amount of <paramref name="deltaTime"/>.
		/// 
		/// If the internal simulation time exceeds the maximum allowed timestep (<see cref="kMaxTimestep"/>), the simulation will perform
		/// necessary updates to account for the elapsed time. The simulation may need to run multiple steps if the time exceeds the maximum 
		/// threshold of <see cref="kMaxTimestep"/> * 2 up to <paramref name="maxSteps"/> times. The remaining time that has not been used will be stored for the next update cycle.
		/// </summary>
		/// <param name="deltaTime">The amount of time to add to the simulation's internal clock.</param>
		/// <param name="maxSteps">The maximum number of update steps to perform in one call (defaults to 2).</param>
		public virtual void Step(float deltaTime, int numSteps = 2)
		{
			m_cachedPosition = m_cachedTransform.position;
		}

		protected void CheckSettingsChanged()
		{
			if (m_settingsChanged)
			{
				OnSettingsChanged();
				m_settingsChanged = false;
			}
		}

		internal virtual void UpdateFluidInput(int numSteps, float fluidTimestep) { }

		internal virtual void CopyNeighboursData() { }

		internal void UpdateTerrainBorders(CommandBuffer cmd = null)
		{
			cmd ??= m_neighbourCopyCommandBuffer;
			Vector2 texelOffset = new Vector2(1.0f / m_activeWaterHeight.width, 1.0f / m_activeWaterHeight.height);
			// Repeat 1 row of pixels
			Vector4[] sourceblitSides = {	new Vector4(texelOffset.x, 1, texelOffset.x * ghostCells.x, 0),
											new Vector4(texelOffset.x, 1, 1 - texelOffset.x * (ghostCells.x + 1), 0),
											new Vector4(1, texelOffset.y, 0, texelOffset.y * ghostCells.y),
											new Vector4(1, texelOffset.y, 0, 1 - texelOffset.y * (ghostCells.y + 1))
										};

			Vector4[] sourceblitSidesNeighbor = {	new Vector4(texelOffset.x * ghostCells.x, 1, 1 - texelOffset.x * ghostCells2.x, 0),	// Right side of the neighbour
													new Vector4(texelOffset.x * ghostCells.x, 1, texelOffset.x * ghostCells.x, 0),			// left side of the neighbour
													new Vector4(1, texelOffset.y * ghostCells.y, 0, 1 - texelOffset.y * ghostCells2.y),    // Bottom side of the neighbour
													new Vector4(1, texelOffset.y * ghostCells.y, 0, texelOffset.y * ghostCells.y)          // Top side of the neighbour  
												};
			Vector4[] destblitSides =	{	new Vector4(texelOffset.x * ghostCells.x, 1, 0, 0),
											new Vector4(texelOffset.x * ghostCells.x, 1, 1 - texelOffset.x * ghostCells.x, 0),
											new Vector4(1, texelOffset.y * ghostCells.y, 0, 0),
											new Vector4(1, texelOffset.y * ghostCells.y, 0, 1 - texelOffset.y * ghostCells.y) 
										};


			for (int i = 0; i < sourceblitSides.Length; i++)
			{

				RenderTexture source = terrainHeight;
				Vector4 blitBias = sourceblitSides[i];
				if (IsExternalBoundary((BoundarySides)i))
				{
					blitBias = sourceblitSidesNeighbor[i];
					source = GetNeighbour((BoundarySides)i).terrainHeight;
				}

				m_internalPropertyBlock.Clear();
				m_internalPropertyBlock.SetVector(FluidShaderProperties._BlitScaleBiasRt, Vector2.one);
				m_internalPropertyBlock.SetVector(FluidShaderProperties._BlitScaleBias, blitBias);
				m_internalPropertyBlock.SetInteger(FluidShaderProperties._RotateBlit, (i < 2) ? 1 : 0);

				BlitQuad(cmd, source, m_terrainBoundaryCells[i], m_boundaryMaterial, m_internalPropertyBlock, m_boundaryStorePass);
			}

			for (int i = 0; i < sourceblitSides.Length; i++)
			{
				m_internalPropertyBlock.Clear();
				m_internalPropertyBlock.SetVector(FluidShaderProperties._BlitScaleBiasRt, destblitSides[i]);
				m_internalPropertyBlock.SetVector(FluidShaderProperties._BlitScaleBias, Vector2.one);
				m_internalPropertyBlock.SetInteger(FluidShaderProperties._RotateSample, (i < 2) ? 1 : 0);

				BlitQuad(cmd, m_terrainBoundaryCells[i], terrainHeight, m_boundaryMaterial, m_internalPropertyBlock, m_boundaryCopyPass);
			}

			if (cmd == m_neighbourCopyCommandBuffer)
			{
				Graphics.ExecuteCommandBuffer(m_neighbourCopyCommandBuffer);
				m_neighbourCopyCommandBuffer.Clear();
			}
		}

		protected void UpdateStaticWaterInput()
		{
			m_commandBuffer.SetGlobalFloat(FluidShaderProperties._FluidSimDeltaTime, 1);
			foreach (FluidModifier input in FluidModifier.waterModifiers)
			{
				FluidModifierVolume volumeInput = input as FluidModifierVolume;
				if (volumeInput && volumeInput.isActiveAndEnabled && volumeInput.type == FluidModifierVolume.FluidModifierType.Source && !volumeInput.sourceSettings.dynamic)
				{
					Vector2 size = volumeInput.sourceSettings.size;
					float strength = volumeInput.sourceSettings.strength;
					float exponent = volumeInput.sourceSettings.falloff;
					int pass = m_addFluidCirclePass;
					if (volumeInput.sourceSettings.mode == FluidModifierVolume.FluidSourceSettings.FluidSourceMode.Box)
						pass = m_addFluidSquarePass;
					AddFluid(m_commandBuffer, m_staticInput, input.transform.position, size, strength, exponent, 0, 1, pass);
					m_hasStaticWaterInput = true;
				}
			}
			Graphics.ExecuteCommandBuffer(m_commandBuffer);
			m_commandBuffer.Clear();
		}

		protected void UpdateDynamicWaterInput(float dt)
		{
			if (!platformSupportsFloat32Blend)
			{
				m_dynamicCommandBuffer.SetRenderTarget(m_dynamicInput);
				m_dynamicCommandBuffer.ClearRenderTarget(false, true, Color.clear);
			}
			foreach (FluidModifier input in FluidModifier.waterModifiers)
			{
				if (input.isActiveAndEnabled)
				{
					input.Process(this, dt);
				}
			}
		}

		protected void PostProcessFluidModifiers(float dt)
		{
			foreach (FluidModifier input in FluidModifier.waterModifiers)
			{
				if (input.isActiveAndEnabled)
				{
					input.PostProcess(this, dt);
				}
			}
			Graphics.ExecuteCommandBuffer(m_dynamicCommandBuffer);
			m_dynamicCommandBuffer.Clear();
		}

		protected void UpdateRenderInfo()
		{
			CommandBufferKey renderInfoKey = new CommandBufferKey(m_activeVelocity, m_activeWaterHeight, m_activeHeightVelocityTexture);
			if (!m_renderInfoCommandBuffers.TryGetCommandBuffer(renderInfoKey, out m_commandBuffer))
			{
				m_commandBuffer.name = name + "_RenderInfo_" + renderInfoKey.GetHashCode();
				m_renderInfoCommandBuffers.AddCommandBuffer(renderInfoKey, m_commandBuffer);
				using (new ProfilingScope(m_commandBuffer, ProfilingSampler.Get(WaterSimProfileID.CombineRenderInfo)))
				{
					using (new ProfilingScope(m_commandBuffer, ProfilingSampler.Get(WaterSimProfileID.CombineHeightVelocity)))
					{
						// * 2 because it needs to weigh X and Y.
						float wsTexelSizeX = (dimension.x / m_activeWaterHeight.width) * 2;
						float wsTexelSizeY = (dimension.y / m_activeWaterHeight.height) * 2;

						m_internalPropertyBlock.Clear();
						m_internalPropertyBlock.SetVector(FluidShaderProperties._TexelWorldSize, new Vector4(wsTexelSizeX, wsTexelSizeY, 1.0f / wsTexelSizeX, 1.0f / wsTexelSizeY));
						m_internalPropertyBlock.SetTexture(FluidShaderProperties._FluidHeightField, m_activeWaterHeight);
						m_internalPropertyBlock.SetTexture(FluidShaderProperties._PreviousFluidHeightField, m_activeHeightVelocityTexture);
						m_internalPropertyBlock.SetTexture(FluidShaderProperties._TerrainHeightField, m_terrainHeight);
						m_internalPropertyBlock.SetTexture(FluidShaderProperties._VelocityField, m_activeVelocity);
						m_internalPropertyBlock.SetVector(FluidShaderProperties._BlitScaleBiasRt, Vector2.one);

						Vector2 texelOffset = new Vector2(1.0f / m_activeVelocity.width, 1.0f / m_activeVelocity.height);
						Vector4 blitScale = new Vector4(1 - texelOffset.x * (velocityGhostCells2 - 1),
														1 - texelOffset.y * (velocityGhostCells2 - 1),
														texelOffset.x * (velocityGhostCells),
														texelOffset.y * (velocityGhostCells));
						m_internalPropertyBlock.SetVector(FluidShaderProperties._BlitScaleBias, blitScale);

						BlitQuad(m_commandBuffer, null, new RenderTargetIdentifier[] { m_nextHeightVelocityTexture, m_normalMap }, m_normalMap.depthBuffer, m_createRenderDataMaterial, m_internalPropertyBlock, 0);
						SwapHeightVelocityRT();
					}

					InitSDFData();
					if (settings.distanceFieldReadback)
					{
						using (new ProfilingScope(m_commandBuffer, ProfilingSampler.Get(WaterSimProfileID.CreateSDF)))
						{
							float maxLength = Mathf.Max(m_fluidSDF.width, m_fluidSDF.height);
							int iterations = (int)Mathf.Ceil(Mathf.Log(maxLength, 2.0f));
							iterations = Mathf.Min(settings.distanceFieldIterations, iterations);

							int pongID = Shader.PropertyToID("Pong");
							RenderTargetIdentifier ping = m_fluidSDF;
							RenderTargetIdentifier pong = pongID;

							m_commandBuffer.GetTemporaryRT(pongID, m_fluidSDF.descriptor);

							m_internalPropertyBlock.Clear();
							m_internalPropertyBlock.SetTexture(FluidShaderProperties._FluidHeightField, m_activeHeightVelocityTexture);
							m_internalPropertyBlock.SetVector(FluidShaderProperties._BlitScaleBiasRt, Vector2.one);
							m_internalPropertyBlock.SetVector(FluidShaderProperties._BlitScaleBias, Vector2.one);
							BlitQuad(m_commandBuffer, ping, m_jumpFloodMaterial, m_internalPropertyBlock, m_jumpFloodInit);

							m_internalPropertyBlock.Clear();
							m_internalPropertyBlock.SetVector(FluidShaderProperties._BlitScaleBiasRt, Vector2.one);
							m_internalPropertyBlock.SetVector(FluidShaderProperties._BlitScaleBias, Vector2.one);
							m_commandBuffer.SetGlobalFloat(FluidShaderProperties._AspectRatio, dimension.x / dimension.y);

							for (int i = 1; i <= iterations; i++)
							{
								m_commandBuffer.SetGlobalFloat(FluidShaderProperties._StepSize, 1.0f / Mathf.Pow(2, i));
								m_commandBuffer.SetGlobalTexture(FluidShaderProperties._FluidHeightField, ping);
								BlitQuad(m_commandBuffer, pong, m_jumpFloodMaterial, m_internalPropertyBlock, m_jumpFloodStep);
								Swap(ref ping, ref pong);
							}

							if (ping != m_fluidSDF)
							{
								m_commandBuffer.Blit(ping, m_fluidSDF);
							}
							m_commandBuffer.ReleaseTemporaryRT(pongID);
						}
					}
				}
			}
			else
			{
				SwapHeightVelocityRT();
			}
			Graphics.ExecuteCommandBuffer(m_commandBuffer);
		}

		protected void UpdateAdvectionLayers(float deltaTime, int numSteps)
		{
			foreach (FluidLayer layer in extensionLayers)
			{
				if (layer.isActiveAndEnabled)
				{
					layer.Step(this, deltaTime, numSteps);
				}
			}
		}

		//Copies a texture to the a padded simulation texture
		internal void BlitRenderDataToSimulation(CommandBuffer commandBuffer, Texture source, RenderTexture dest)
		{
			Vector2 srcTexelSize = new Vector2(1.0f / source.width, 1.0f / source.height);
			Vector2 destTexelSize = new Vector2(1.0f / dest.width, 1.0f / dest.height);

			Vector4 blitScale = new Vector4(1 + destTexelSize.x * 2, 1 + destTexelSize.y * 2, -destTexelSize.x, -destTexelSize.y);

			m_externalPropertyBlock.Clear();
			m_externalPropertyBlock.SetVector(FluidShaderProperties._BlitScaleBias, blitScale);
			m_externalPropertyBlock.SetVector(FluidShaderProperties._BlitScaleBiasRt, Vector2.one);
			if (Mathf.IsPowerOfTwo(source.width) && Mathf.IsPowerOfTwo(source.height))
				m_externalPropertyBlock.SetVector(FluidShaderProperties._Offset, -srcTexelSize * 0.5f);
			else
				m_externalPropertyBlock.SetVector(FluidShaderProperties._Offset, Vector2.zero);

			BlitQuad(commandBuffer, source, dest, m_copyTextureMaterial, m_externalPropertyBlock, 0);
		}

		//Copies a padded simulation texture to the a texture
		internal void BlitSimulationToRenderData(CommandBuffer commandBuffer, Texture source, RenderTexture dest)
		{
			Vector2 srcTexelSize = new Vector2(1.0f / source.width, 1.0f / source.height);
			Vector4 blitScale = new Vector4(1 - srcTexelSize.x * ghostCells2.x,
											1 - srcTexelSize.y * ghostCells2.y,
											srcTexelSize.x * ghostCells.x,
											srcTexelSize.y * ghostCells.y);

			m_externalPropertyBlock.Clear();
			m_externalPropertyBlock.SetVector(FluidShaderProperties._BlitScaleBias, blitScale);
			m_externalPropertyBlock.SetVector(FluidShaderProperties._BlitScaleBiasRt, Vector2.one);
			m_externalPropertyBlock.SetVector(FluidShaderProperties._Offset, Vector2.zero);
			BlitQuad(commandBuffer, source, dest, m_copyTextureMaterial, m_externalPropertyBlock, 0);
		}

		protected void CreateCollision()
		{
			if (!Application.isPlaying || !colliderProperties.createCollider)
				return;

			ColliderDescriptor desc = new ColliderDescriptor()
			{
				resolution = colliderProperties.resolution,
				updateFrequency = colliderProperties.updateFrequency,
				timeslicing = colliderProperties.timeslicing,
				realtime = colliderProperties.realtime,
				dimension = dimension,
				heightmapMask = new Vector4(1, 0, 0, 0),
				maxHeight = 500
			};

			m_collider?.Dispose();
			m_collider = new SurfaceCollider(gameObject, desc);
		}

		protected void UpdateColliderFull()
		{
			if (!Application.isPlaying || m_collider == null)
				return;

			m_collider.UpdateFull(m_activeHeightVelocityTexture, dimension, 500);
		}

		protected void UpdateCollider()
		{
			if (!Application.isPlaying || m_collider == null)
				return;

			m_collider.Update(m_activeHeightVelocityTexture, 500);
		}

		public void OnColliderChanged()
		{
			CreateCollision();
			UpdateColliderFull();
		}
		protected virtual void OnSettingsChanged() { }

		protected void GetBoundsHeight(out float boundsHeight)
		{
			boundsHeight = 100;
			if (terrainType == TerrainType.UnityTerrain && unityTerrain)
			{
				boundsHeight = unityTerrain.terrainData.heightmapScale.y * 2;
			}
			else if (terrainType == TerrainType.SimpleTerrain && simpleTerrain)
			{
				boundsHeight = simpleTerrain.heightScale * 2;
			}
			else if (terrainType == TerrainType.Heightmap)
			{
				boundsHeight = heightmapScale * 2;
			}
			else if (terrainType == TerrainType.MeshCollider && meshCollider)
			{
				boundsHeight = meshCollider.bounds.size.y * 2;
			}
		}

		/// <summary>
		/// Returns the cellsize for this simulation adjusted for aspect ratio of dimensions and buffer sizes.
		/// </summary>
		/// <returns></returns>
		public virtual Vector4 GetCellSize()
		{
			return new Vector4(settings.cellSize, settings.cellSize, settings.secondLayerCellSize, settings.secondLayerCellSize);
		}

		public virtual Vector2 GetWorldVelocityScale()
		{
			Vector2 scale = (Vector2.one / settings.numberOfCells * dimension) / GetCellSize();
			return scale;
		}

		public void CalculateDimensions()
		{
			if (dimensionMode == DimensionMode.Bounds)
				CalculateWorldCellSizeFromDimension();
			else
				CalculateDimensionFromWorldCellSize();
		}

		public void CalculateWorldCellSizeFromDimension()
		{
			if (settings)
			{
				Vector2 dim = dimension / settings.numberOfCells;
				cellWorldSize = Mathf.Min(dim.x, dim.y);
			}
		}		
		
		public void CalculateDimensionFromWorldCellSize()
		{
			if (settings)
			{
				dimension = new Vector2(cellWorldSize, cellWorldSize) * settings.numberOfCells;
			}
		}


		/// <summary>
		/// Helper function to swap two objects.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="a"></param>
		/// <param name="b"></param>
		public static void Swap<T>(ref T a, ref T b)
		{
			T t = a;
			a = b;
			b = t;
		}

		public void SwapFluidRT()
		{
			Swap(ref m_activeWaterHeight, ref m_nextWaterHeight);
		}

		protected void SwapVelocity()
		{
			Swap(ref m_activeVelocity, ref m_nextVelocity);
		}

		protected void SwapHeightVelocityRT()
		{
			Swap(ref m_activeHeightVelocityTexture, ref m_nextHeightVelocityTexture);
		}
	}
}
