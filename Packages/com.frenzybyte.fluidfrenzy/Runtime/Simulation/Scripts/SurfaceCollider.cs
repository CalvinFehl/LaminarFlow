using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Rendering;

namespace FluidFrenzy
{

	/// <summary>
	/// Manages the generation and synchronization of a Unity physics collider to allow physical interactions, such as collisions and raycasting, with the dynamic fluid surface and underlying terrain.
	/// </summary>
	/// <remarks>
	/// <para>
	/// This optional feature enables physical interaction between standard Unity physics components and the elements simulated by Fluid Frenzy, specifically the <c>FluidSimulation</c> and the underlying <c>SimpleTerrain/TerraformTerrain</c>.
	/// </para>
	/// 
	/// <h4>GPU-CPU Synchronization and Performance</h4>
	/// <para>
	/// Since the fluid is simulated entirely on the GPU, the height data required for a physics collider must be transferred back to the CPU to generate the surface. This **GPU-readback** and the subsequent collider generation are inherently expensive operations.
	/// </para>
	/// <para>
	/// To prevent the application from experiencing large stalls, the update process is managed by the <c>SurfaceCollider</c> class. It operates **asynchronously** and uses **timeslicing** to spread the computational load across multiple frames. The system provides a critical trade-off:
	/// </para>
	/// <list type="bullet">
	/// 	<item>Increasing the number of timesliced frames reduces the per-frame cost.</item>
	/// 	<item>However, this also increases the update delay, meaning the physics surface lags further behind the current fluid state.</item>
	/// </list>
	/// <para>
	/// The system includes several settings to modify both the quality of the generated collider and the performance characteristics of this synchronization process.
	/// </para>
	/// </remarks>
	public class SurfaceCollider : IDisposable
	{
		/// <summary>
		/// Encapsulates settings used for configuring and initializing <see cref="SurfaceCollider"/>.
		/// </summary>
		[Serializable]
		public struct ColliderProperties
		{
			public static ColliderProperties DefaultTerrain = new ColliderProperties(true, 512, false, 1, 16);
			public static ColliderProperties DefaultFluid = new ColliderProperties(false, 512, false, 1, 16);

			/// <summary>
			/// Toggles the generation of a <see cref="TerrainCollider"/> to handle physical interactions with the fluid surface.
			/// </summary>
			public bool createCollider;

			/// <summary>
			/// Specifies the grid resolution of the generated <see cref="TerrainCollider"/>.
			/// </summary>
			/// <remarks>
			/// This value determines the density of the physics mesh. Higher resolutions result in more accurate physical interactions but increase generation time and physics processing overhead.
			/// <para>
			/// Internally, the actual grid size is set to <c>resolution + 1</c> to satisfy heightmap requirements.
			/// </para>
			/// </remarks>
			public int resolution;

			/// <summary>
			/// Controls whether the collider's heightmap is updated at runtime to match the visual fluid simulation.
			/// </summary>
			/// <remarks>
			/// When enabled, the simulation data is continuously synchronized with the physics collider. 
			/// Note that this process requires reading GPU terrain data back to the CPU and applying it to the <see cref="TerrainData"/>, which can be resource-intensive and cause garbage collection spikes.
			/// </remarks>
			public bool realtime;

			/// <summary>
			/// The interval, in frames, between consecutive collider updates when <see cref="realtime"/> is enabled.
			/// </summary>
			/// <remarks>
			/// Increasing this value reduces the performance cost of the readback but causes the physics representation to lag behind the visual rendering.
			/// </remarks>
			public int updateFrequency;

			/// <summary>
			/// The number of frames over which a single full collider update is distributed.
			/// </summary>
			/// <remarks>
			/// This feature splits the heightmap update into smaller segments, processing only a fraction of the data per frame. 
			/// This helps to smooth out performance spikes and maintain a stable framerate, though it increases the time required for the collider to fully reflect a change in the fluid surface.
			/// </remarks>
			public int timeslicing;

			public int version;


			/// <summary>
			/// Parameterized constructor which allows initialization with specific values.
			/// The constructor ensures that traverseIterations is clamped within a manageable range.
			/// </summary>
			/// <param name="createCollider"><inheritdoc cref="createCollider" path="/summary"/></param>
			/// <param name="resolution"><inheritdoc cref="resolution" path="/summary"/></param>
			/// <param name="realtime"><inheritdoc cref="realtime" path="/summary"/></param>
			/// <param name="updateFrequency"><inheritdoc cref="updateFrequency" path="/summary"/></param>
			/// <param name="timeslicing"><inheritdoc cref="timeslicing" path="/summary"/></param>
			public ColliderProperties(bool createCollider, int resolution, bool realtime,
									int updateFrequency, int timeslicing, int version = 1)
			{
				this.createCollider = createCollider;
				this.resolution = resolution;
				this.realtime = realtime;
				this.updateFrequency = updateFrequency;
				this.timeslicing = timeslicing;
				this.version = version;
			}
		}

		/// <summary>
		/// Encapsulates settings used for configuring and initializing <see cref="SurfaceCollider"/>.
		/// </summary>
		public struct ColliderDescriptor
		{
			/// <summary><inheritdoc cref="ColliderProperties.resolution" path="/summary"/></summary>
			public int resolution;
			/// <summary><inheritdoc cref="ColliderProperties.realtime" path="/summary"/></summary>
			public bool realtime;
			/// <summary><inheritdoc cref="ColliderProperties.updateFrequency" path="/summary"/></summary>
			public int updateFrequency;
			/// <summary><inheritdoc cref="ColliderProperties.timeslicing" path="/summary"/></summary>
			public int timeslicing;


			/// <summary><inheritdoc cref="ISurfaceRenderer.RenderProperties.dimension" path="/summary"/></summary>
			public Vector2 dimension;
			/// <summary>The maximum height the surface will be. This is used for the packing the heightmap into a normalized format.</summary>
			internal float maxHeight;
			/// <summary>
			/// Specifies which channels of the heightmap to read 1 is read, 0 is ignore. 
			/// The result is accumulated with the following formula: dot(heightTexel, heightmapMask)
			/// </summary>
			public Vector4 heightmapMask;

		}

		private ColliderDescriptor m_descriptor;
		private Material m_packheightmapMaterial;

		private GameObject m_collision;
		private TerrainData m_terrainData;

		private float[,] m_collisionHeights;
		private RenderTexture m_collisionRT;
		private MaterialPropertyBlock m_colliderUpdateProperties;

		private int m_colliderResolution;
		private int m_colliderUpdateTimeslice;
		private Vector2Int m_colliderCopySize;
		private Vector2Int m_colliderSlices;

		private CommandBuffer m_collisionCmd;
		private event Action<AsyncGPUReadbackRequest> m_terrainReadbackAction;
		private int m_currentReadRequestSlice = 0;
		private int m_currentReadbackSlice = 0;

		public SurfaceCollider(GameObject parent, ColliderDescriptor desc)
		{
			m_packheightmapMaterial = new Material(Shader.Find("Hidden/FluidFrenzy/PackHeightmap"));
			SetColliderDesc(desc);
			CreateCollision(parent);
		}

		public void Dispose()
		{
			AsyncGPUReadback.WaitAllRequests();
			m_collisionRT?.Release();
			m_collisionRT = null;
			GraphicsHelpers.SafeDestroy(m_terrainData);
			m_collisionHeights = null;

			GraphicsHelpers.SafeDestroy(m_collision);
			m_collisionCmd.Dispose();
			m_collisionCmd = null;
		}

		public void SetColliderDesc( ColliderDescriptor desc)
		{
			m_descriptor = desc;
		}

		private void CreateCollision(GameObject parent)
		{
			AsyncGPUReadback.WaitAllRequests();
			m_colliderUpdateTimeslice = m_descriptor.timeslicing;
			m_colliderResolution = m_descriptor.resolution + 1;
			m_colliderSlices = CalculateSplits(m_descriptor.timeslicing);
			m_terrainReadbackAction = AsyncCallbackTimeslice;

			m_terrainData = new TerrainData();
			m_terrainData.name = "CollisionData";
			m_terrainData.heightmapResolution = m_colliderResolution;
			m_terrainData.size = new Vector3(m_descriptor.dimension.x, m_descriptor.maxHeight, m_descriptor.dimension.y);

			m_colliderUpdateProperties = new MaterialPropertyBlock();
			m_colliderCopySize = new Vector2Int((m_descriptor.resolution / m_colliderSlices.x) + 1, (m_descriptor.resolution / m_colliderSlices.y) + 1);
			m_collisionHeights = new float[m_colliderCopySize.y, m_colliderCopySize.x];

			if (m_collisionRT == null || (m_collisionRT.width != m_colliderCopySize.x || m_collisionRT.height != m_colliderCopySize.y))
			{
				m_collisionRT = new RenderTexture(m_colliderCopySize.x, m_colliderCopySize.y, 0, RenderTextureFormat.RFloat);
				m_collisionRT.name = "CollisionHeightmap";
				m_collisionRT.Create();
			}

			if (m_collision == null)
			{
				m_collision = new GameObject("Collider", typeof(TerrainCollider));
				m_collision.transform.SetParent(parent.transform);
				m_collision.transform.localPosition = new Vector3(-m_descriptor.dimension.x * 0.5f, 0, -m_descriptor.dimension.y * 0.5f);
				m_collision.layer = parent.layer;
			}
			TerrainCollider collider = m_collision.GetComponent<TerrainCollider>();
			collider.terrainData = m_terrainData;

			m_collisionCmd ??= new CommandBuffer()
			{
				name = "UpdateCollision"
			};
		}


		private void AsyncCallbackTimeslice(AsyncGPUReadbackRequest request)
		{
			if (!request.done || request.hasError || !m_terrainData)
				return;

			NativeArray<float> data = request.GetData<float>();
			ToArray(data, m_collisionHeights);
			Vector2Int copyPos = CurrentTimeslicePos(m_currentReadbackSlice % m_colliderUpdateTimeslice);
			m_terrainData.SetHeightsDelayLOD(copyPos.x, copyPos.y, m_collisionHeights);
			m_currentReadbackSlice = (m_currentReadbackSlice + 1) % m_colliderUpdateTimeslice;
		}

		private void AsyncCallbackFull(AsyncGPUReadbackRequest request)
		{
			if (!request.done || request.hasError || !m_terrainData)
				return;

			NativeArray<float> data = request.GetData<float>();
			float[,] heights = new float[request.width, request.height];
			ToArray(data, heights);
			m_terrainData.SetHeightsDelayLOD(0, 0, heights);
		}

		internal void Update(Texture heightMap, float heightScale)
		{
			if (!m_descriptor.realtime)
				return;

			if ((Time.frameCount % m_descriptor.updateFrequency) != 0)
				return;

			if (m_collisionCmd != null)
			{
				Vector2Int copyPos = CurrentTimeslicePos(m_currentReadRequestSlice % m_colliderUpdateTimeslice);
				Vector4 blitScaleBias = new Vector4((float)(m_colliderCopySize.x) / m_colliderResolution,
					(float)(m_colliderCopySize.y) / m_colliderResolution,
					(float)(copyPos.x) / m_colliderResolution,
					(float)(copyPos.y) / m_colliderResolution);
				m_colliderUpdateProperties.Clear();
				m_colliderUpdateProperties.SetVector(FluidShaderProperties._HeightMapMask, m_descriptor.heightmapMask);
				m_colliderUpdateProperties.SetFloat(FluidShaderProperties._HeightScale, heightScale);
				m_colliderUpdateProperties.SetVector(FluidShaderProperties._BlitScaleBias, blitScaleBias);
				m_colliderUpdateProperties.SetVector(FluidShaderProperties._BlitScaleBiasRt, Vector2.one);

				m_collisionCmd.Clear();
				FluidSimulation.BlitQuad(m_collisionCmd, heightMap, m_collisionRT, m_packheightmapMaterial, m_colliderUpdateProperties, 0);
				m_collisionCmd.RequestAsyncReadback(m_collisionRT, 0, 0, m_colliderCopySize.x, 0, m_colliderCopySize.y, 0, 1, m_terrainReadbackAction);
				Graphics.ExecuteCommandBuffer(m_collisionCmd);
				m_currentReadRequestSlice = (m_currentReadRequestSlice + 1) % m_colliderUpdateTimeslice;
			}
		}

		internal void UpdateFull(Texture heightMap, Vector2 dimension, float heightScale)
		{
			if (!Application.isPlaying)
				return;

			m_terrainData.heightmapResolution = m_colliderResolution;
			m_terrainData.size = new Vector3(dimension.x, heightScale, dimension.y);
			m_colliderUpdateProperties.Clear();
			m_colliderUpdateProperties.SetVector(FluidShaderProperties._HeightMapMask, m_descriptor.heightmapMask);
			m_colliderUpdateProperties.SetFloat(FluidShaderProperties._HeightScale, heightScale);
			m_colliderUpdateProperties.SetVector(FluidShaderProperties._BlitScaleBias, Vector2.one);
			m_colliderUpdateProperties.SetVector(FluidShaderProperties._BlitScaleBiasRt, Vector2.one);
			RenderTexture collisionRT = new RenderTexture(m_colliderResolution, m_colliderResolution, 0, RenderTextureFormat.RFloat);
			collisionRT.Create();
			m_collisionCmd.Clear();
			FluidSimulation.BlitQuad(m_collisionCmd, heightMap, collisionRT, m_packheightmapMaterial, m_colliderUpdateProperties, 0);
			m_collisionCmd.RequestAsyncReadback(collisionRT, 0, 0, collisionRT.width, 0, collisionRT.height, 0, 1, AsyncCallbackFull);
			m_collisionCmd.WaitAllAsyncReadbackRequests();
			Graphics.ExecuteCommandBuffer(m_collisionCmd);
			collisionRT.Release();
		}


		private static Vector2Int CalculateSplits(int timeslice)
		{
			int n = 0;
			while ((1 << n) < timeslice)
			{
				n++;
			}

			int widthSplit = 1 << (n / 2);
			int heightSplit = 1 << (n - n / 2);
			return new Vector2Int(widthSplit, heightSplit);
		}

		private Vector2Int CurrentTimeslicePos(int slice)
		{
			Vector2Int copyBlock = new Vector2Int(slice % m_colliderSlices.x, slice / m_colliderSlices.x);
			return copyBlock * m_colliderCopySize - copyBlock;
		}

		private unsafe void ToArray(NativeArray<float> data, float[,] arrOut)
		{
			void* unsafeVals = NativeArrayUnsafeUtility.GetUnsafeReadOnlyPtr(data);

			var arrOutPtr = UnsafeUtility.PinGCArrayAndGetDataAddress(arrOut, out ulong handle);

			UnsafeUtility.MemCpy(arrOutPtr, unsafeVals, data.Length * UnsafeUtility.SizeOf<float>());

			UnsafeUtility.ReleaseGCObject(handle);
		}
	}
}