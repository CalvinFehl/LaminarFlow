using System;
using UnityEngine;
using UnityEngine.Rendering;
using SurfaceRenderMode = FluidFrenzy.ISurfaceRenderer.RenderMode;
#if FLUIDFRENZY_RUNTIME_HDRP_SUPPORT
using HDRPWaterSurface = UnityEngine.Rendering.HighDefinition.WaterSurface;
#endif

namespace FluidFrenzy
{
	/// <summary>
	/// The <see cref="FluidRenderer"/> component is responsible for rendering the <see cref="FluidSimulation"/>. 
	/// This component is in charge of creating and rendering the necessary meshes and materials needed for displaying the assigned <see cref="FluidSimulation"/>. 
	/// Users can customize the <see cref="FluidRenderer"/> component to create their own rendering effects, similar to <see cref="WaterSurface"/> and <see cref="LavaSurface"/> renderers.
	/// </summary>
	public class FluidRenderer : MonoBehaviour
	{
		[SerializeField]
		internal int version = 1;
		/// <summary> 
		/// Different fluid debugging modes that can be used in the editor. 
		/// </summary>
		public enum DebugMode
		{
			None,
			Normals,
			Height,
			Flow,
			Velocity,
			Divergence,
			Pressure,
			Foam,
			UV,
			LOD,
		}

		/// <summary> 
		/// Properties that determine the mesh quality and the specific drawing mode of the fluid surface. 
		/// </summary>
		/// <remarks>
		/// This structure holds settings that control the visual fidelity and performance of the fluid surface mesh. 
		/// This includes the specific method used to render the mesh, such as standard MeshRenderer, procedural drawing, GPULOD, or a specialized HDRP mode.
		/// </remarks>
		public ISurfaceRenderer.RenderProperties surfaceProperties = ISurfaceRenderer.RenderProperties.Default;
		/// <summary> 
		/// The material to be used to render the fluid surface. 
		/// </summary>
		/// <remarks> 
		/// This material is internally instantiated at runtime. The component copies the properties from the original material to the new instance, 
		/// and then overrides or injects any necessary rendering requirements (e.g., shader keywords or properties) for the fluid simulation effects 
		/// to function correctly.
		/// </remarks>
		public Material fluidMaterial = null;
		/// <summary> 
		/// The <see cref="FluidSimulation"/> component that this renderer will draw. 
		/// </summary>
		/// <remarks>
		/// This is a mandatory dependency. The FluidRenderer will automatically adopt the world-space dimensions and position of the assigned Fluid Simulation,
		/// ensuring the rendered fluid surface matches the simulated area exactly.
		/// </remarks>
		public FluidSimulation simulation;
		/// <summary> 
		/// The <see cref="FluidFlowMapping"/> component that this <see cref="FluidRenderer"/> uses to visualize fluid currents and wakes.
		/// </summary>
		/// <remarks>
		/// This component provides the necessary data to the fluid shader, which can be either a dedicated flow map texture (for dynamic UV-offsetting) 
		/// or material parameters derived directly from the simulation's velocity texture. This allows the fluid surface to depict accurate movement and flow.
		/// </remarks>
		public FluidFlowMapping flowMapping;

		[Obsolete("meshResolution property has been deprecated use surfaceProperties.meshResolution"), SerializeField, HideInInspector]
		public Vector2Int meshResolution = new Vector2Int(1024, 1024);
		[Obsolete("meshBlocks property has been deprecated use surfaceProperties.meshBlocks"), SerializeField, HideInInspector]
		public Vector2Int meshBlocks = Vector2Int.one;

		public ISurfaceRenderer surfaceRenderer { get { return m_surfaceRenderer; } }

		protected ISurfaceRenderer m_surfaceRenderer = null;


		protected Material m_renderMaterial;
		private MaterialPropertyBlock m_propertyBlock;

#if UNITY_2021_1_OR_NEWER
		private LocalKeyword m_dynamicFlowMappingKey;
		private LocalKeyword m_staticFlowMappingKey;
#endif

#if UNITY_EDITOR
		protected DebugMode m_debugMode = DebugMode.None;
		protected Material m_debugMaterial;
#endif

		/// <summary>
		/// Copies the required data from a <see cref="FluidRenderer"/> into this <see cref="FluidRenderer"/>.
		/// </summary>
		/// <param name="source">The <see cref="FluidRenderer"/> to copy from.</param>
		public virtual void CopyFrom(FluidRenderer source)
		{
			fluidMaterial = source.fluidMaterial;
			surfaceProperties = source.surfaceProperties;
		}

		protected virtual void OnValidate()
		{
			if (surfaceProperties.version == 0)
			{
#pragma warning disable CS0618 // Rethrow to preserve stack details
#if UNITY_EDITOR
				UnityEditor.Undo.RecordObject(this, "Update FluidRenderer");
#endif
				surfaceProperties = new ISurfaceRenderer.RenderProperties(SurfaceRenderMode.MeshRenderer, simulation ? simulation.dimension : Vector2.one, meshResolution, Vector2Int.one * 8, Vector2Int.one * 8, 1, new Vector2Int(0, 15), ISurfaceRenderer.RenderProperties.Version);
#pragma warning restore CS0618 // Rethrow to preserve stack details
			}
		}

		protected virtual void OnEnable()
		{
		}

		protected virtual void OnDisable()
		{
		}

		protected virtual void Awake()
		{
#if UNITY_EDITOR
			m_debugMaterial = new Material(Shader.Find("FluidFrenzy/Debug/SimulationData"));
#endif
			m_propertyBlock = new MaterialPropertyBlock();
		}

		protected virtual void OnDestroy()
		{
			Destroy(m_renderMaterial);
#if UNITY_EDITOR
			Destroy(m_debugMaterial);
#endif
			RemoveCameraCallbacks();
			m_surfaceRenderer.Dispose();
		}


		// Start is called before the first frame update
		protected virtual void Start()
		{
			m_renderMaterial = Instantiate(fluidMaterial);
			CreateRenderer();
			AddCameraCallbacks();
		}

		private void CreateRenderer()
		{
			surfaceProperties.dimension = simulation ? simulation.dimension : surfaceProperties.dimension;
			ISurfaceRenderer.Extensions.IsRenderModeSupported(surfaceProperties.renderMode, out surfaceProperties.renderMode);
			ISurfaceRenderer.SurfaceDescriptor desc = new ISurfaceRenderer.SurfaceDescriptor()
			{
				dimension = simulation ? simulation.dimension : surfaceProperties.dimension,
				heightScale = 1,
				maxHeight = 2500,
				meshResolution = surfaceProperties.meshResolution,
				heightmapMask = new Vector4(1,0,0,0),
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
#if FLUIDFRENZY_RUNTIME_HDRP_SUPPORT
			else if (surfaceProperties.renderMode == SurfaceRenderMode.HDRPWaterSurface)
			{
				m_surfaceRenderer?.Dispose();
				m_surfaceRenderer = new WaterSurfaceHDRP(surfaceProperties.hdrpWaterSurface, simulation);
				surfaceProperties.hdrpWaterSurface.targetWaterSurface.customMaterial = m_renderMaterial;
			}
#endif

			m_surfaceRenderer.SetupMaterial(m_renderMaterial);
		}

		public void OnRenderModeChanged()
		{
#if UNITY_EDITOR
			if (!Application.isPlaying)
				return;
#endif
			CreateRenderer();
			m_surfaceRenderer.OnEnable();
		}

		private void AddCameraCallbacks()
		{
			RemoveCameraCallbacks();
			Camera.onPreCull += PreCull;
			Camera.onPostRender += PostRender;
			RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
		}

		private void RemoveCameraCallbacks()
		{
			Camera.onPreCull -= PreCull;
			Camera.onPostRender -= PostRender;
			RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
		}

		protected virtual void PreCull(Camera camera)
		{
			if (camera.cameraType == CameraType.Preview)
				return;

			if (m_surfaceRenderer.customRender)
			{
				m_surfaceRenderer.PreRenderCamera(transform, camera, simulation.fluidRenderData, surfaceProperties.traverseIterations);
				m_surfaceRenderer.AddCommandBuffers(camera);
			}
		}

		protected virtual void PostRender(Camera camera)
		{
			if (camera.cameraType == CameraType.Preview)
				return;

			if (m_surfaceRenderer.customRender)
			{
				m_surfaceRenderer.RemoveCommandBuffers(camera);
			}
		}

		protected virtual void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera)
		{
			if (camera.cameraType == CameraType.Preview)
				return;
 			if (m_surfaceRenderer.customRender)
			{
				m_surfaceRenderer.PreRender(context, transform, camera, simulation.fluidRenderData, surfaceProperties.traverseIterations, false);
			}
		}

		// Update is called once per frame
		protected virtual void Update()
		{
			if (m_renderMaterial)
			{
#if UNITY_2022_2_OR_NEWER || (UNITY_2021_3_OR_NEWER && !UNITY_2022_1_OR_NEWER)
				m_renderMaterial.CopyMatchingPropertiesFromMaterial(fluidMaterial);
#else
				m_renderMaterial.CopyPropertiesFromMaterial(fluidMaterial);
#endif

#if COZY_WEATHER
				if (ExternalHelpers.IsCozyWeatherFogEnabled() && m_renderMaterial.renderQueue > 2900)
				{
					m_renderMaterial.EnableKeyword("_FLUID_COZY_FOG_FORWARD");
				}
				else
				{
					m_renderMaterial.DisableKeyword("_FLUID_COZY_FOG_FORWARD");
				}
#endif

				m_surfaceRenderer.SetupMaterial(m_renderMaterial);

				Vector2 dimension = surfaceProperties.dimension / surfaceProperties.meshBlocks.x;
				Vector2 dimensionRcp = Vector2.one / dimension;
				Vector2 meshResolution = surfaceProperties.meshResolution / surfaceProperties.meshBlocks.x;
				m_renderMaterial.SetVector(FluidShaderProperties._FluidGridMeshDimensions, new Vector4(dimension.x, dimension.y, dimensionRcp.x, dimensionRcp.y));
				m_renderMaterial.SetVector(FluidShaderProperties._FluidGridMeshResolution, Vector2.one * meshResolution);
				m_renderMaterial.SetVector(FluidShaderProperties._FluidGridMeshRcp, Vector2.one / (meshResolution + Vector2.one));

				m_renderMaterial.SetMatrix("_FluidGridWorldToObject", transform.worldToLocalMatrix);

				//if(simulation.terrainType == FluidSimulation.TerrainType.SimpleTerrain)
				//	m_renderMaterial.SetTexture(FluidShaderProperties._TerrainHeightField, simulation.simpleTerrain.renderHeightmap);

				m_renderMaterial.SetFloat(FluidShaderProperties._TerrainHeightScale, simulation.terrainScale);
				m_renderMaterial.SetVector(FluidShaderProperties._TerrainHeightField_ST, simulation.terrainTextureST);
				m_renderMaterial.SetTexture(FluidShaderProperties._TerrainHeightField, simulation.terrainHeight);
				m_renderMaterial.SetTexture(FluidShaderProperties._FluidHeightVelocityField, simulation.fluidRenderData);
				m_renderMaterial.SetTexture(FluidShaderProperties._FluidNormalField, simulation.normalTexture);
				m_renderMaterial.SetFloat(FluidShaderProperties._FluidClipHeight, simulation.clipHeight);

#if UNITY_2021_1_OR_NEWER
				m_dynamicFlowMappingKey = new LocalKeyword(m_renderMaterial.shader, "_FLUID_FLOWMAPPING_DYNAMIC");
				m_staticFlowMappingKey = new LocalKeyword(m_renderMaterial.shader, "_FLUID_FLOWMAPPING_STATIC");
				if (simulation.terrainType == FluidSimulation.TerrainType.UnityTerrain)
					m_renderMaterial.EnableKeyword(new LocalKeyword(m_renderMaterial.shader, "_FLUID_UNITY_TERRAIN"));
#else
				if (simulation.terrainType == FluidSimulation.TerrainType.UnityTerrain)
					m_renderMaterial.EnableKeyword("_FLUID_UNITY_TERRAIN");
#endif
				if (flowMapping)
				{
					m_renderMaterial.SetTexture(FluidShaderProperties._FluidUVOffsetField, flowMapping.activeLayer);
#if UNITY_2021_1_OR_NEWER
					if (flowMapping.settings.flowMappingMode == FluidFlowMapping.FlowMappingMode.Dynamic)
					{
						m_renderMaterial.EnableKeyword(m_dynamicFlowMappingKey);
						m_renderMaterial.DisableKeyword(m_staticFlowMappingKey);
					}
					else if (flowMapping.settings.flowMappingMode == FluidFlowMapping.FlowMappingMode.Static)
					{
						m_renderMaterial.EnableKeyword(m_staticFlowMappingKey);
						m_renderMaterial.DisableKeyword(m_dynamicFlowMappingKey);
					}
					else if (flowMapping.settings.flowMappingMode == FluidFlowMapping.FlowMappingMode.Off)
					{
						m_renderMaterial.DisableKeyword(m_staticFlowMappingKey);
						m_renderMaterial.DisableKeyword(m_dynamicFlowMappingKey);
					}
#else
					if (flowMapping.settings.flowMappingMode == FluidFlowMapping.FlowMappingMode.Dynamic)
					{
						m_renderMaterial.EnableKeyword("_FLUID_FLOWMAPPING_DYNAMIC");
						m_renderMaterial.DisableKeyword("_FLUID_FLOWMAPPING_STATIC");
					}
					else if (flowMapping.settings.flowMappingMode == FluidFlowMapping.FlowMappingMode.Static)
					{
						m_renderMaterial.EnableKeyword("_FLUID_FLOWMAPPING_STATIC");
						m_renderMaterial.DisableKeyword("_FLUID_FLOWMAPPING_DYNAMIC");
					}
					else if (flowMapping.settings.flowMappingMode == FluidFlowMapping.FlowMappingMode.Off)
					{
						m_renderMaterial.DisableKeyword("_FLUID_FLOWMAPPING_DYNAMIC");
						m_renderMaterial.DisableKeyword("_FLUID_FLOWMAPPING_STATIC");
					}
#endif

					m_renderMaterial.SetVector(FluidShaderProperties._FlowBlend, flowMapping.flowBlend);
					m_renderMaterial.SetVector(FluidShaderProperties._FlowTimer, flowMapping.flowTime);
					m_renderMaterial.SetVector(FluidShaderProperties._FlowUVOffset, flowMapping.flowUVOffset);
					m_renderMaterial.SetVector(FluidShaderProperties._FlowSpeed, flowMapping.flowSpeed);
				}
				else
				{
					m_renderMaterial.DisableKeyword("_FLUID_FLOWMAPPING_DYNAMIC");
					m_renderMaterial.DisableKeyword("_FLUID_FLOWMAPPING_STATIC");
				}

			}
#if UNITY_EDITOR
			UpdateDebugMaterial();
#endif
		}

		public virtual void LateUpdate()
		{
			Texture heightmap = simulation.fluidRenderData;
			//if (simulation.terrainType == FluidSimulation.TerrainType.SimpleTerrain)
			//	heightmap = simulation.simpleTerrain.renderHeightmap;

			Vector2 heightmapRcp = (Vector2.one / new Vector2(heightmap.width, heightmap.height)) * new Vector2(heightmap.width - 1, heightmap.height - 1);
			float wsTexelSizeX = surfaceProperties.dimension.x / heightmap.width;
			float wsTexelSizeY = surfaceProperties.dimension.y / heightmap.height;

			//terrainMaterial.SetFloat(FluidShaderProperties._HeightScale, 1);
			m_propertyBlock.SetVector(FluidShaderProperties._HeightmapRcpScale, heightmapRcp);
			m_propertyBlock.SetMatrix(FluidShaderProperties._ObjectToWorld, transform.localToWorldMatrix);

#if UNITY_EDITOR
			m_surfaceRenderer.Render(transform, m_debugMode != DebugMode.None ? m_debugMaterial : m_renderMaterial, m_propertyBlock);
#else
			m_surfaceRenderer.Render(transform, m_renderMaterial, m_propertyBlock);
#endif
		}

#if UNITY_EDITOR
		public void SetDebugMode(DebugMode debugMode)
		{
			m_debugMode = debugMode;
			m_debugMaterial.DisableKeyword("WATER_NORMALS");
			m_debugMaterial.DisableKeyword("WATER_HEIGHT");
			m_debugMaterial.DisableKeyword("WATER_VELOCITY");
			m_debugMaterial.DisableKeyword("WATER_FLOW");
			m_debugMaterial.DisableKeyword("WATER_DIVERGENCE");
			m_debugMaterial.DisableKeyword("WATER_FOAM");
			m_debugMaterial.DisableKeyword("WATER_PRESSURE");
			m_debugMaterial.DisableKeyword("WATER_UV");

			if (m_debugMode == DebugMode.Normals)
				m_debugMaterial.EnableKeyword("WATER_NORMALS");
			else if (m_debugMode == DebugMode.Height)
				m_debugMaterial.EnableKeyword("WATER_HEIGHT");
			else if (m_debugMode == DebugMode.Flow)
				m_debugMaterial.EnableKeyword("WATER_FLOW");
			else if (m_debugMode == DebugMode.Velocity)
				m_debugMaterial.EnableKeyword("WATER_VELOCITY");
			else if (m_debugMode == DebugMode.Divergence)
				m_debugMaterial.EnableKeyword("WATER_DIVERGENCE");
			else if (m_debugMode == DebugMode.Pressure)
				m_debugMaterial.EnableKeyword("WATER_PRESSURE");
			else if (m_debugMode == DebugMode.UV)
				m_debugMaterial.EnableKeyword("WATER_UV");			
			else if (m_debugMode == DebugMode.LOD)
				m_debugMaterial.EnableKeyword("FLUID_LOD");
		}

		protected virtual bool UpdateDebugMaterial()
		{
			if (m_debugMode == DebugMode.None)
				return false;

			if (flowMapping)
			{
				m_debugMaterial.SetTexture(FluidShaderProperties._FluidUVOffsetField, flowMapping.activeLayer);
				if (flowMapping.settings.flowMappingMode == FluidFlowMapping.FlowMappingMode.Dynamic)
				{
					m_debugMaterial.EnableKeyword("_FLUID_FLOWMAPPING_DYNAMIC");
					m_debugMaterial.DisableKeyword("_FLUID_FLOWMAPPING_STATIC");
				}
				else if (flowMapping.settings.flowMappingMode == FluidFlowMapping.FlowMappingMode.Static)
				{
					m_debugMaterial.EnableKeyword("_FLUID_FLOWMAPPING_STATIC");
					m_debugMaterial.DisableKeyword("_FLUID_FLOWMAPPING_DYNAMIC");
				}
				else if (flowMapping.settings.flowMappingMode == FluidFlowMapping.FlowMappingMode.Off)
				{
					m_debugMaterial.DisableKeyword("_FLUID_FLOWMAPPING_STATIC");
					m_debugMaterial.DisableKeyword("_FLUID_FLOWMAPPING_DYNAMIC");
				}

				m_debugMaterial.SetVector(FluidShaderProperties._FlowBlend, flowMapping.flowBlend);
				m_debugMaterial.SetVector(FluidShaderProperties._FlowTimer, flowMapping.flowTime);
				m_debugMaterial.SetVector(FluidShaderProperties._FlowUVOffset, flowMapping.flowUVOffset);
				m_debugMaterial.SetVector(FluidShaderProperties._FlowSpeed, flowMapping.flowSpeed);
			}
			else
			{
				m_debugMaterial.DisableKeyword("_FLUID_FLOWMAPPING_STATIC");
				m_debugMaterial.DisableKeyword("_FLUID_FLOWMAPPING_DYNAMIC");
			}

			m_surfaceRenderer.SetupMaterial(m_debugMaterial);

#if UNITY_2021_1_OR_NEWER
			if (simulation.terrainType == FluidSimulation.TerrainType.UnityTerrain)
				m_debugMaterial.EnableKeyword(new LocalKeyword(m_debugMaterial.shader, "_FLUID_UNITY_TERRAIN"));
#endif

			m_debugMaterial.SetTexture(FluidShaderProperties._FluidHeightVelocityField, simulation.fluidRenderData);
			m_debugMaterial.SetTexture(FluidShaderProperties._FluidNormalField, simulation.normalTexture);

			if (simulation is FluxFluidSimulation fluxSimulation)
			{
				m_debugMaterial.SetTexture(FluidShaderProperties._OutflowField, fluxSimulation.outFlowTexture);
				m_debugMaterial.SetTexture(FluidShaderProperties._DivergenceField, fluxSimulation.divergenceTexture);
				m_debugMaterial.SetTexture(FluidShaderProperties._PressureField, fluxSimulation.pressureTexture);
			}
			m_debugMaterial.SetTexture(FluidShaderProperties._VelocityField, simulation.velocityTexture);
			m_debugMaterial.SetVector(FluidShaderProperties._VelocityField_ST, simulation.velocityTextureST);
			m_debugMaterial.SetFloat(FluidShaderProperties._VelocityScale, 1.0f / simulation.velocityScale);
			m_debugMaterial.SetFloat(FluidShaderProperties._Layer, m_renderMaterial.GetFloat(FluidShaderProperties._Layer));

			m_debugMaterial.SetTexture(FluidShaderProperties._TerrainHeightField, simulation.terrainHeight);
			m_debugMaterial.SetVector("_TerrainHeightField_ST", simulation.terrainTextureST);
			m_debugMaterial.SetFloat(FluidShaderProperties._TerrainHeightScale, simulation.terrainScale);


			return true;
		}
#endif
	}
}