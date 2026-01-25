using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace FluidFrenzy
{
	/// <summary>
	/// <see cref="FluidFlowMapping"/> is an <see cref="FluidLayer">extension layer</see> that enables and controls flow mapping functionality in the simulation and rendering side of Fluid Frenzy. 
	/// The layer generates the flow map procedurally using the flow of the fluid simulation. The rendering data is automatically passed to the material assigned to the <see cref="FluidRenderer"/>. 
	/// There are several settings to control the visuals of the flow mapping in the layer which can be set in the <see cref="FluidFlowMappingSettings"/> asset assigned to this layer.
	/// Note: This component lives on a GameObject but also needs to be added to the Layers list of the Fluid Simulation.
	/// </summary>
	[HelpURL("https://frenzybyte.github.io/fluidfrenzy/docs/fluid_simulation_components/#fluid-flow-mapping")]
	public class FluidFlowMapping : FluidLayer
	{
		[SerializeField]
		internal int version = 1;
		/// <summary>
		/// The method of flow mapping to use
		/// </summary>
		public enum FlowMappingMode
		{
			/// <summary> No flow mapping applied. </summary>
			Off,
			/// <summary> Flow mapping is performed directly in the shader by offsetting UV coordinates based on the velocity field. </summary>
			Static,
			/// <summary>
			/// Flow mapping utilizes a separate buffer to calculate UV offsets. The UVs are advected in a manner similar to the velocity field and foam mask. 
			/// In this mode, the offset is used to determine the velocity at the advected UV position, allowing for more intricate swirling effects. 
			/// However, this can result in increased distortion over time.
			/// </summary>
			Dynamic
		}

		/// <summary>
		/// The <see cref="FluidFlowMappingSettings">settings</see> that this <see cref="FluidFlowMapping"/> will use to generate the flow mapping data.
		/// </summary>
		public FluidFlowMappingSettings settings;

		/// <summary>
		/// The blending state of the flow mapping, each component represents a weight how visible each of the layers is.
		/// </summary>
		public Vector3 flowBlend { get { return m_flowBlend; } private set { } }
		/// <summary>
		/// The progress of the flow mapping, each component represents a time how visible far the flow mapping is progressed per layer.
		/// </summary>
		public Vector3 flowTime { get { return m_flowTimer; } private set { } }
		/// <summary>
		/// The offset UV applied to each flow mapping layer. Each component represents a layer.
		/// </summary>
		public Vector3 flowUVOffset { get { return m_flowUVOffset; } private set { } }
		/// <summary>
		/// The speed/distance the flow mapping should travel per second.
		/// </summary>
		//public float flowSpeed { get { return settings.flowSpeed / m_simulationDimensions.x; } private set { } }

		public Vector2 flowSpeed { get { return (Vector2.one * settings.flowSpeed) / new Vector2(m_parentSimulation.settings.numberOfCells.x, m_parentSimulation.settings.numberOfCells.y) / m_parentSimulation.GetCellSize().x; } private set { } }

		private Material m_uvAdvectionMaterial = null;
		private MaterialPropertyBlock m_internalPropertyBlock;
		private CommandBuffer m_commandBuffer;

		//Flow mapping data
		private int m_activeUVBufferIdx = 0;
		private Vector3 m_flowBlend = Vector3.zero;
		private Vector3 m_flowTimer = Vector3.zero;
		private Vector3 m_flowUVOffset = Vector3.zero;

		private Vector2 m_simulationDimensions;

		public override void Awake()
		{
			m_uvAdvectionMaterial = new Material(Shader.Find("Hidden/FluidFrenzy/AdvectUV"));

			m_commandBuffer = new CommandBuffer();
			m_commandBuffer.name = "FluidFlowMapping";

			m_internalPropertyBlock = new MaterialPropertyBlock();
		}

		public override void CopyFrom(FluidLayer source)
		{
			FluidFlowMapping sourceLayer = source as FluidFlowMapping;
			settings = sourceLayer.settings;
		}

		public override void Init(FluidSimulation simulation)
		{
			base.Init(simulation);

			Vector2Int numRenderCells = simulation.numRenderCells;
			activeLayer = GraphicsHelpers.CreateSimulationRT(numRenderCells.x, numRenderCells.y, GraphicsFormat.R16G16B16A16_SFloat, name: "UVBuffer_0");
			nextLayer = GraphicsHelpers.CreateSimulationRT(numRenderCells.x, numRenderCells.y, GraphicsFormat.R16G16B16A16_SFloat, name: "UVBuffer_1");
			textureST = Vector2.one;

			m_simulationDimensions = simulation.dimension;
		}

		public override void ResetLayer(FluidSimulation simulation)
		{
			RenderTexture.active = activeLayer;
			GL.Clear(false, true, Color.clear);
			RenderTexture.active = nextLayer;
			GL.Clear(false, true, Color.clear);
		}

		public override void OnDestroy()
		{
			GraphicsHelpers.ReleaseSimulationRT(activeLayer);
			GraphicsHelpers.ReleaseSimulationRT(nextLayer);
			Destroy(m_uvAdvectionMaterial);
		}

		public override void Step(FluidSimulation simulation, float deltaTime, int numSteps)
		{
			UpdateUVBuffer(simulation, deltaTime);
		}

		private void ResetUVBuffer(FluidSimulation simulation)
		{
			m_internalPropertyBlock.Clear();
			FluidSimulation.BlitQuad(m_commandBuffer, null, activeLayer, m_uvAdvectionMaterial, m_internalPropertyBlock, m_activeUVBufferIdx);
		}

		private void AdvectUV(FluidSimulation simulation, float dt)
		{
			m_internalPropertyBlock.Clear();
			m_internalPropertyBlock.SetVector(FluidShaderProperties._FlowSpeed, ((Vector2.one * settings.flowSpeed) / new Vector2(activeLayer.width, activeLayer.height) / m_parentSimulation.GetCellSize() * dt));
			m_internalPropertyBlock.SetTexture(FluidShaderProperties._VelocityField, simulation.velocityTexture);
			m_internalPropertyBlock.SetTexture(FluidShaderProperties._MainTex, activeLayer);
			m_internalPropertyBlock.SetVector(FluidShaderProperties._UVTextureSize, new Vector2(1.0f / (activeLayer.width - 1), 1.0f / (activeLayer.height - 1)));

			Vector2 texelOffset = new Vector2(1.0f / simulation.velocityTexture.width, 1.0f / simulation.velocityTexture.height);
			Vector4 blitScale = new Vector4(1 - texelOffset.x * simulation.velocityGhostCells2,
											1 - texelOffset.y * simulation.velocityGhostCells2,
											texelOffset.x * simulation.velocityGhostCells,
											texelOffset.y * simulation.velocityGhostCells);
			m_internalPropertyBlock.SetVector(FluidShaderProperties._VelocityBlitScaleBias, blitScale);
			FluidSimulation.BlitQuad(m_commandBuffer, activeLayer, nextLayer, m_uvAdvectionMaterial, m_internalPropertyBlock, 2);
			SwapActiveLayer();
		}

		private void UpdateUVBuffer(FluidSimulation simulation, float dt)
		{
			if (settings.flowMappingMode != FlowMappingMode.Dynamic)
			{
				CalculateStaticFlowValues(Time.fixedTime);
				return;
			}

			bool resetUV = CalculateDynamicFlowValues(Time.fixedTime);
			using (new ProfilingScope(m_commandBuffer, ProfilingSampler.Get(WaterSimProfileID.AdvectUV)))
			{
				if (resetUV)
				{
					using (new ProfilingScope(m_commandBuffer, ProfilingSampler.Get(WaterSimProfileID.ResetUV)))
					{
						ResetUVBuffer(simulation);
					}
				}
				AdvectUV(simulation, dt);
			}
			Graphics.ExecuteCommandBuffer(m_commandBuffer);
			m_commandBuffer.Clear();
		}

		bool CalculateDynamicFlowValues(float currentTime)
		{
			float currentTimeBlend = currentTime * settings.flowPhaseSpeed;
			float t = Mathf.Repeat(currentTimeBlend, 1.0f);

			float nk = 2.0f;
			float k = 1.0f / nk;
			float tt = t / k;
			int previousIndex = m_activeUVBufferIdx;
			m_activeUVBufferIdx = Mathf.FloorToInt(tt);

			float t0 = Mathf.Repeat(currentTimeBlend, 1.0f);
			float t1 = Mathf.Repeat(currentTimeBlend + 0.5f, 1.0f);

			m_flowTimer.x = t0 / settings.flowPhaseSpeed;
			m_flowTimer.y = t1 / settings.flowPhaseSpeed;

			float jump = 0.25f;
			m_flowUVOffset.x = (currentTimeBlend - t0) * jump;
			m_flowUVOffset.y = (currentTimeBlend - t1) * jump;

			m_flowBlend.x = 1 - Mathf.Abs(1 - 2 * t0);
			m_flowBlend.y = 1 - Mathf.Abs(1 - 2 * t1);
			return m_activeUVBufferIdx != previousIndex;
		}

		void CalculateStaticFlowValues(float currentTime)
		{
			currentTime = currentTime * settings.flowPhaseSpeed;

			float nk = 3.0f;
			float nkk = 0.5f / nk;

			float t1 = Mathf.Repeat(currentTime, 1.0f);
			float a1 = 1 - Mathf.Clamp01((nkk - t1) / nkk) - Mathf.Clamp01((t1 - (1 - nkk)) / nkk);
			float t2 = Mathf.Repeat((currentTime) + 1.0f / nk, 1.0f);
			float a2 = 1 - Mathf.Clamp01((nkk - t2) / nkk) - Mathf.Clamp01((t2 - (1 - nkk)) / nkk);
			float t3 = Mathf.Repeat((currentTime) + 2.0f / nk, 1.0f);
			float a3 = 1 - Mathf.Clamp01((nkk - t3) / nkk) - Mathf.Clamp01((t3 - (1 - nkk)) / nkk);

			float jump = 0.25f;
			m_flowUVOffset.x = (currentTime - t1) * jump;
			m_flowUVOffset.y = (currentTime - t2) * jump;
			m_flowUVOffset.z = (currentTime - t3) * jump;

			m_flowTimer = (new Vector3(t1, t2, t3) - new Vector3(0.5f, 0.5f, 0.5f)) / settings.flowPhaseSpeed;
			m_flowBlend = new Vector3(a1, a2, a3) / settings.flowPhaseSpeed;
			float alphaTot = 1.0f / Vector3.Dot(m_flowBlend, Vector3.one);
			m_flowBlend *= alphaTot;
		}

		public override RenderTexture GetDebugBuffer(FluidSimulation.DebugBuffer buffer)
		{
			switch (buffer)
			{
				case FluidSimulation.DebugBuffer.FlowUV:
					return activeLayer;
			}
			return null;
		}

		public override IEnumerable<FluidSimulation.DebugBuffer> EnumerateBuffers()
		{
			if(settings.flowMappingMode == FlowMappingMode.Dynamic)
				yield return FluidSimulation.DebugBuffer.FlowUV;
		}
	}
}