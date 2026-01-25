using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace FluidFrenzy
{
	/// <summary>
	/// <see cref="FluxFluidSimulation"/> is the core component of Fluid Frenzy. It handles the full simulation and the components attached to it.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The <c>FluxFluidSimulation</c> uses a Flux-based algorithm, often referred to as the **Pipe Model**, to simulate large water bodies. This approach directly calculates the volume exchange (flux) between grid cells, resulting in a highly stable simulation capable of generating very smooth, realistic waves.
	/// </para>
	/// 
	/// <h4>The Pipe (Flux) Model</h4>
	/// <para>
	/// In this model, the fluid domain is discretized into a grid of <c>vertical columns</c>. These columns interact with their four neighbors via <c>virtual pipes</c>. The simulation determines the flow (flux) through these pipes by calculating the difference in pressure (based on fluid level) between adjacent columns. This method of modeling volume exchange guarantees mass conservation and smooth waves.
	/// </para>
	/// 
	/// <h4>Simulation Characteristics</h4>
	/// <para>
	/// The system provides the following trade-offs compared to other simulation types:
	/// </para>
	/// <list type="table">
	/// 	<listheader>
	/// 		<term>Pros</term>
	/// 		<term>Cons</term>
	/// 	</listheader>
	/// 	<item><term>Stable at any height.</term><term>Higher performance cost.</term></item>
	/// 	<item><term>Smoother waves.</term><term>Higher VRAM usage.</term></item>
	/// 	<item><term>Allows for more complex velocity solving (e.g., vortices).</term><term>Decoupled Wave/Velocity: The velocity field is derived from the outflow, causing it to lag behind abruptly changing waves.</term></item>
	/// 	<item><term>Control waves and flow mapping separately.</term><term></term></item>
	/// </list>
	/// </remarks>
	public partial class FluxFluidSimulation : FluidSimulation
	{
		const float kMinCellSize = 0.011f;

		private FluxFluidSimulationSettings m_internalSettings;

		//Fluid simulation data
		private RenderTexture m_activeOutFlow = null;
		private RenderTexture m_nextOutFlow = null;

		private RenderTexture m_activeOutFlowLayer1 = null;
		private RenderTexture m_nextOutFlowLayer1 = null;

		private RenderTexture m_outFlowVelocity = null;

		private RenderTexture m_externalOutFlow = null;
		private Vector4 m_externalOutFlowClamp;

		private RenderTexture m_divergence = null;
		private RenderTexture m_activePressure = null;
		private RenderTexture m_nextPressure = null;

		//Last time the fluid simulation got ran
		private int m_LastStepFrame;

		protected override void Awake()
		{
			simulationType = FluidSimulationType.Flux;
			base.Awake();

			//Init markers
			ProfilingSampler.Get(WaterSimProfileID.WaterSimulation_Fluid);
		}

		protected override void Start()
        {
			m_internalSettings = settings as FluxFluidSimulationSettings;

			base.Start();

			multiLayeredFluid = settings.secondLayer;

			bounds = CalculateBounds();

			velocityGhostCells = (int)((float)(velocityTextureSize.x) * (m_internalSettings.paddingScale / 100));
			velocityGhostCells2 = velocityGhostCells * 2;

			ghostCells = new Vector2Int(3, 3);
			ghostCells2 = ghostCells * 2;
			numRenderCells = settings.numberOfCells + Vector2Int.one;
			numSimulationCells = numRenderCells + ghostCells2;

			Vector2 paddingTexel = new Vector2(1.0f / numSimulationCells.x, 1.0f / numSimulationCells.y);
			m_paddingST = new Vector4(1.0f - paddingTexel.x * ghostCells2.x,
										1.0f - paddingTexel.y * ghostCells2.y,
										paddingTexel.x * ghostCells.x,
										paddingTexel.y * ghostCells.y);

			foreach (FluidLayer layer in extensionLayers)
			{
				layer.Init(this);
			}

			InitShaders();
			InitRenderTextures();
			ResetSimulation();

			CreateCollision();
			UpdateColliderFull();

		}

		protected override void OnDestroy()
		{
			base.OnDestroy();
			GraphicsHelpers.ReleaseSimulationRT(m_nextOutFlow);
			GraphicsHelpers.ReleaseSimulationRT(m_activeOutFlow);
			GraphicsHelpers.ReleaseSimulationRT(m_activePressure);
			GraphicsHelpers.ReleaseSimulationRT(m_nextPressure);
			GraphicsHelpers.ReleaseSimulationRT(m_divergence);
			GraphicsHelpers.ReleaseSimulationRT(m_externalOutFlow);
			GraphicsHelpers.ReleaseSimulationRT(m_outFlowVelocity);
			GraphicsHelpers.ReleaseSimulationRT(m_activeOutFlowLayer1);
			GraphicsHelpers.ReleaseSimulationRT(m_nextOutFlowLayer1);

			DestroyMaterials();
		}

		protected override void InitRenderTextures()
		{
			base.InitRenderTextures();

			m_activeOutFlow = GraphicsHelpers.CreateSimulationRT(numSimulationCells.x, numSimulationCells.y, RenderTextureFormat.ARGBHalf, true, name: "Outflow_1");
			m_nextOutFlow = GraphicsHelpers.CreateSimulationRT(numSimulationCells.x, numSimulationCells.y, RenderTextureFormat.ARGBHalf, true, name: "Outflow_2");

			if (multiLayeredFluid)
			{
				m_activeOutFlowLayer1 = GraphicsHelpers.CreateSimulationRT(numSimulationCells.x, numSimulationCells.y, RenderTextureFormat.ARGBHalf, true, name: "OutflowLayer1_1");
				m_nextOutFlowLayer1 = GraphicsHelpers.CreateSimulationRT(numSimulationCells.x, numSimulationCells.y, RenderTextureFormat.ARGBHalf, true, name: "OutflowLayer1_2");
			}

			m_externalOutFlow = GraphicsHelpers.CreateSimulationRT(numSimulationCells.x, numSimulationCells.y, RenderTextureFormat.RHalf, true, name: "AddedOutflow");
			m_outFlowVelocity = GraphicsHelpers.CreateSimulationRT(numSimulationCells.x, numSimulationCells.y, GraphicsFormat.R16G16B16A16_SFloat, name: "OutflowVelocity");

			m_externalOutFlowClamp = new Vector4(3, m_activeWaterHeight.width - 6, 3, m_activeWaterHeight.height - 6);

			Vector2Int velocitySize = velocityTextureSize + Vector2Int.one + new Vector2Int(velocityGhostCells2, velocityGhostCells2);
			m_divergence = GraphicsHelpers.CreateSimulationRT(velocitySize.x, velocitySize.y, RenderTextureFormat.RHalf, name: "DivergeTex");
			m_activePressure = GraphicsHelpers.CreateSimulationRT(velocitySize.x, velocitySize.y, RenderTextureFormat.RHalf, true, name: "Pressure_0");
			m_nextPressure = GraphicsHelpers.CreateSimulationRT(velocitySize.x, velocitySize.y, RenderTextureFormat.RHalf, true, name: "Pressure_1");

			m_activeVelocity = GraphicsHelpers.CreateSimulationRT(velocitySize.x, velocitySize.y, velocityFormat, true, name: "Velocity_1");
			m_nextVelocity = GraphicsHelpers.CreateSimulationRT(velocitySize.x, velocitySize.y, velocityFormat, true, name: "Velocity_2");

			Vector2 velocityTexelSize = new Vector2((float)(velocityGhostCells) / (velocityTextureSize.x + velocityGhostCells2),
													(float)(velocityGhostCells) / (velocityTextureSize.y + velocityGhostCells2));
			m_velocityTextureST = new Vector4(1.0f - velocityTexelSize.x * 2,
									1.0f - velocityTexelSize.y * 2,
									velocityTexelSize.x,
									velocityTexelSize.y);
			m_velocityBoundaryCells = new RenderTexture[(int)BoundarySides.Max];

			m_simulationTexelSize = new Vector4(1.0f / numSimulationCells.x, 1.0f / numSimulationCells.y, numSimulationCells.x, numSimulationCells.y);

			if (velocityGhostCells > 0)
			{
				for (int i = 0; i < m_velocityBoundaryCells.Length; i++)
				{
					m_velocityBoundaryCells[i] = GraphicsHelpers.CreateSimulationRT(velocityTextureSize.x, velocityGhostCells, RenderTextureFormat.ARGBHalf, true, filterMode: FilterMode.Point, name: "VelocityBoundaryCopy");
				}
			}

			ResetRenderTextures();
		}

		protected override void ResetRenderTextures()
		{
			base.ResetRenderTextures();
			RenderTexture previousRT = RenderTexture.active;

			RenderTexture.active = m_nextOutFlow;
			GL.Clear(false, true, Color.clear);

			RenderTexture.active = m_activeOutFlow;
			GL.Clear(false, true, Color.clear);

			RenderTexture.active = m_activePressure;
			GL.Clear(false, true, Color.clear);

			RenderTexture.active = m_nextPressure;
			GL.Clear(false, true, Color.clear);

			RenderTexture.active = m_divergence;
			GL.Clear(false, true, Color.clear);

			RenderTexture.active = m_externalOutFlow;
			GL.Clear(false, true, Color.clear);

			RenderTexture.active = m_outFlowVelocity;
			GL.Clear(false, true, Color.blue);

			RenderTexture.active = m_activeOutFlowLayer1;
			GL.Clear(false, true, Color.clear);

			RenderTexture.active = m_nextOutFlowLayer1;
			GL.Clear(false, true, Color.clear);			

			RenderTexture.active = previousRT;
		}

		/// <summary>
		/// Resets the <see cref="FluxFluidSimulation"/> to its original state.
		/// </summary>
		public override void ResetSimulation()
		{
			UpdateSimulationConstants(1.0f);
			base.ResetSimulation();

			if (m_internalSettings.secondLayerCustomViscosity)
			{
				m_fluidSimulationMaterial.EnableKeyword("FLUID_CUSTOMVISCOSITY");
			}
			else
			{
				m_fluidSimulationMaterial.DisableKeyword("FLUID_CUSTOMVISCOSITY");
			}
		}

		public override void PreUpdate(float deltaTime, int numSteps = 2)
		{
			int numIterations = numSteps ;
			float fluidTimestep = kMaxTimestep;
			base.PreUpdate(deltaTime, numSteps);
			UpdateSimulationConstants(fluidTimestep);
			UpdateFluidInput(numIterations, fluidTimestep);

			m_solidToFluidCS.EnableKeyword("USE_FLUX_SIMULATION");

			UpdateFluidRigidBody();
		}

		public override void Step(float deltaTime, int numSteps = 2)
		{
			base.Step(deltaTime, numSteps);

			float fluidTimestep = kMaxTimestep;
			m_timestep -= kMaxTimestep * numSteps;

			UpdateCollider();
			UpdateSimulationConstants(fluidTimestep);
			UpdateTerrain(updateGroundEveryFrame);
			UpdateSimulation(numSteps);
			UpdateAdvectionLayers(fluidTimestep * numSteps, numSteps);

			PostProcessFluidModifiers(fluidTimestep * numSteps);

			UpdateRenderInfo();
			ReadbackSimulationData();
		}

		internal override void CopyNeighboursData()
		{
			if (!isActiveAndEnabled)
			{
				return;
			}

			Vector2 texelOffset = new Vector2(1.0f / m_activeWaterHeight.width, 1.0f / m_activeWaterHeight.height);

			Span<Vector4> sourceblitSidesNeighbor = stackalloc Vector4[4];
			sourceblitSidesNeighbor[0] = new Vector4(texelOffset.x * ghostCells.x, 1, 1 - texelOffset.x * ghostCells2.x, 0);    // Right side of the neighbour
			sourceblitSidesNeighbor[1] = new Vector4(texelOffset.x * ghostCells.x, 1, texelOffset.x * ghostCells.x, 0);         // left side of the neighbour
			sourceblitSidesNeighbor[2] = new Vector4(1, texelOffset.y * ghostCells.y, 0, 1 - texelOffset.y * ghostCells2.y);    // Bottom side of the neighbour
			sourceblitSidesNeighbor[3] = new Vector4(1, texelOffset.y * ghostCells.y, 0, texelOffset.y * ghostCells.y);          // Top side of the neighbour  

			Span<Vector4> destblitSides = stackalloc Vector4[4];
			destblitSides[0] = new Vector4(texelOffset.x * ghostCells.x, 1, 0, 0);
			destblitSides[1] = new Vector4(texelOffset.x * ghostCells.x, 1, 1 - texelOffset.x * ghostCells.x, 0);
			destblitSides[2] = new Vector4(1, texelOffset.y * ghostCells.y, 0, 0);
			destblitSides[3] = new Vector4(1, texelOffset.y * ghostCells.y, 0, 1 - texelOffset.y * ghostCells.y);

			{
				for (int i = 0; i < sourceblitSidesNeighbor.Length; i++)
				{
					if (!IsExternalBoundary((BoundarySides)i))
					{
						continue;
					}

					RenderTexture source = GetNeighbour((BoundarySides)i).terrainHeight;
					m_internalPropertyBlock.Clear();
					m_internalPropertyBlock.SetVector(FluidShaderProperties._BlitScaleBiasRt, Vector2.one);
					m_internalPropertyBlock.SetVector(FluidShaderProperties._BlitScaleBias, sourceblitSidesNeighbor[i]);
					m_internalPropertyBlock.SetInteger(FluidShaderProperties._RotateBlit, (i < 2) ? 1 : 0);

					BlitQuad(m_neighbourCopyCommandBuffer, source, m_terrainBoundaryCells[i], m_boundaryMaterial, m_internalPropertyBlock, m_boundaryStorePass);
				}

				for (int i = 0; i < sourceblitSidesNeighbor.Length; i++)
				{
					if (!IsExternalBoundary((BoundarySides)i))
					{
						continue;
					}

					m_internalPropertyBlock.Clear();
					m_internalPropertyBlock.SetVector(FluidShaderProperties._BlitScaleBiasRt, destblitSides[i]);
					m_internalPropertyBlock.SetVector(FluidShaderProperties._BlitScaleBias, Vector2.one);
					m_internalPropertyBlock.SetInteger(FluidShaderProperties._RotateSample, (i < 2) ? 1 : 0);

					BlitQuad(m_neighbourCopyCommandBuffer, m_terrainBoundaryCells[i], terrainHeight, m_boundaryMaterial, m_internalPropertyBlock, m_boundaryCopyPass);
				}
			}

			// Outflow
			{
				for (int i = 0; i < sourceblitSidesNeighbor.Length; i++)
				{
					if (!IsExternalBoundary((BoundarySides)i))
					{
						continue;
					}

					RenderTexture source = (GetNeighbour((BoundarySides)i) as FluxFluidSimulation).outFlowTexture;
					m_internalPropertyBlock.Clear();
					m_internalPropertyBlock.SetVector(FluidShaderProperties._BlitScaleBiasRt, Vector2.one);
					m_internalPropertyBlock.SetVector(FluidShaderProperties._BlitScaleBias, sourceblitSidesNeighbor[i]);
					m_internalPropertyBlock.SetInteger(FluidShaderProperties._RotateBlit, (i < 2) ? 1 : 0);

					BlitQuad(m_neighbourCopyCommandBuffer, source, m_waterBoundaryCells[i], m_boundaryMaterial, m_internalPropertyBlock, m_boundaryStorePass);
				}

				for (int i = 0; i < sourceblitSidesNeighbor.Length; i++)
				{
					if (!IsExternalBoundary((BoundarySides)i))
					{
						continue;
					}

					m_internalPropertyBlock.Clear();
					m_internalPropertyBlock.SetVector(FluidShaderProperties._BlitScaleBiasRt, destblitSides[i]);
					m_internalPropertyBlock.SetVector(FluidShaderProperties._BlitScaleBias, Vector2.one);
					m_internalPropertyBlock.SetInteger(FluidShaderProperties._RotateSample, (i < 2) ? 1 : 0);

					BlitQuad(m_neighbourCopyCommandBuffer, m_waterBoundaryCells[i], m_activeOutFlow, m_boundaryMaterial, m_internalPropertyBlock, m_boundaryCopyPass);
					BlitQuad(m_neighbourCopyCommandBuffer, m_waterBoundaryCells[i], m_nextOutFlow, m_boundaryMaterial, m_internalPropertyBlock, m_boundaryCopyPass);
				}
			}

			// Fluid
			{
				for (int i = 0; i < sourceblitSidesNeighbor.Length; i++)
				{
					if (!IsExternalBoundary((BoundarySides)i))
					{
						continue;
					}

					RenderTexture source = (GetNeighbour((BoundarySides)i) as FluxFluidSimulation).fluidHeight;
					m_internalPropertyBlock.Clear();
					m_internalPropertyBlock.SetVector(FluidShaderProperties._BlitScaleBiasRt, Vector2.one);
					m_internalPropertyBlock.SetVector(FluidShaderProperties._BlitScaleBias, sourceblitSidesNeighbor[i]);
					m_internalPropertyBlock.SetInteger(FluidShaderProperties._RotateBlit, (i < 2) ? 1 : 0);

					BlitQuad(m_neighbourCopyCommandBuffer, source, m_waterBoundaryCells[i], m_boundaryMaterial, m_internalPropertyBlock, m_boundaryStorePass);
				}

				for (int i = 0; i < sourceblitSidesNeighbor.Length; i++)
				{
					if (!IsExternalBoundary((BoundarySides)i))
					{
						continue;
					}

					m_internalPropertyBlock.Clear();
					m_internalPropertyBlock.SetVector(FluidShaderProperties._BlitScaleBiasRt, destblitSides[i]);
					m_internalPropertyBlock.SetVector(FluidShaderProperties._BlitScaleBias, Vector2.one);
					m_internalPropertyBlock.SetInteger(FluidShaderProperties._RotateSample, (i < 2) ? 1 : 0);

					BlitQuad(m_neighbourCopyCommandBuffer, m_waterBoundaryCells[i], m_activeWaterHeight, m_boundaryMaterial, m_internalPropertyBlock, m_boundaryCopyPass);
					BlitQuad(m_neighbourCopyCommandBuffer, m_waterBoundaryCells[i], m_nextWaterHeight, m_boundaryMaterial, m_internalPropertyBlock, m_boundaryCopyPass);
				}
			}

			if (velocityGhostCells == 0)
			{
				Graphics.ExecuteCommandBuffer(m_neighbourCopyCommandBuffer);
				m_neighbourCopyCommandBuffer.Clear();
				return;
			}

			texelOffset = new Vector2(1.0f / m_activeVelocity.width, 1.0f / m_activeVelocity.height);
			sourceblitSidesNeighbor[0] = new Vector4(texelOffset.x * velocityGhostCells, 1, 1 - texelOffset.x * velocityGhostCells2, 0);    // Right side of the neighbour
			sourceblitSidesNeighbor[1] = new Vector4(texelOffset.x * velocityGhostCells, 1, texelOffset.x * velocityGhostCells, 0);         // left side of the neighbour
			sourceblitSidesNeighbor[2] = new Vector4(1, texelOffset.y * velocityGhostCells, 0, 1 - texelOffset.y * velocityGhostCells2);    // Bottom side of the neighbour
			sourceblitSidesNeighbor[3] = new Vector4(1, texelOffset.y * velocityGhostCells, 0, texelOffset.y * velocityGhostCells);          // Top side of the neighbour  

			destblitSides[0] = new Vector4(texelOffset.x * velocityGhostCells, 1, 0, 0);
			destblitSides[1] = new Vector4(texelOffset.x * velocityGhostCells, 1, 1 - texelOffset.x * velocityGhostCells, 0);
			destblitSides[2] = new Vector4(1, texelOffset.y * velocityGhostCells, 0, 0);
			destblitSides[3] = new Vector4(1, texelOffset.y * velocityGhostCells, 0, 1 - texelOffset.y * velocityGhostCells);

			// Velocity
			{
				for (int i = 0; i < sourceblitSidesNeighbor.Length; i++)
				{
					if (!IsExternalBoundary((BoundarySides)i))
					{
						continue;
					}

					RenderTexture source = (GetNeighbour((BoundarySides)i) as FluxFluidSimulation).velocityTexture;
					m_internalPropertyBlock.Clear();
					m_internalPropertyBlock.SetVector(FluidShaderProperties._BlitScaleBiasRt, Vector2.one);
					m_internalPropertyBlock.SetVector(FluidShaderProperties._BlitScaleBias, sourceblitSidesNeighbor[i]);
					m_internalPropertyBlock.SetInteger(FluidShaderProperties._RotateBlit, (i < 2) ? 1 : 0);

					BlitQuad(m_neighbourCopyCommandBuffer, source, m_velocityBoundaryCells[i], m_boundaryMaterial, m_internalPropertyBlock, m_boundaryStorePass);
				}

				for (int i = 0; i < sourceblitSidesNeighbor.Length; i++)
				{
					if (!IsExternalBoundary((BoundarySides)i))
					{
						continue;
					}

					m_internalPropertyBlock.Clear();
					m_internalPropertyBlock.SetVector(FluidShaderProperties._BlitScaleBiasRt, destblitSides[i]);
					m_internalPropertyBlock.SetVector(FluidShaderProperties._BlitScaleBias, Vector2.one);
					m_internalPropertyBlock.SetInteger(FluidShaderProperties._RotateSample, (i < 2) ? 1 : 0);

					BlitQuad(m_neighbourCopyCommandBuffer, m_velocityBoundaryCells[i], velocityTexture, m_boundaryMaterial, m_internalPropertyBlock, m_boundaryCopyPass);
				}
			}

			// Pressure
			{
				for (int i = 0; i < sourceblitSidesNeighbor.Length; i++)
				{
					if (!IsExternalBoundary((BoundarySides)i))
					{
						continue;
					}

					RenderTexture source = (GetNeighbour((BoundarySides)i) as FluxFluidSimulation).pressureTexture;
					m_internalPropertyBlock.Clear();
					m_internalPropertyBlock.SetVector(FluidShaderProperties._BlitScaleBiasRt, Vector2.one);
					m_internalPropertyBlock.SetVector(FluidShaderProperties._BlitScaleBias, sourceblitSidesNeighbor[i]);
					m_internalPropertyBlock.SetInteger(FluidShaderProperties._RotateBlit, (i < 2) ? 1 : 0);

					BlitQuad(m_neighbourCopyCommandBuffer, source, m_velocityBoundaryCells[i], m_boundaryMaterial, m_internalPropertyBlock, m_boundaryStorePass);
				}

				for (int i = 0; i < sourceblitSidesNeighbor.Length; i++)
				{
					if (!IsExternalBoundary((BoundarySides)i))
					{
						continue;
					}

					m_internalPropertyBlock.Clear();
					m_internalPropertyBlock.SetVector(FluidShaderProperties._BlitScaleBiasRt, destblitSides[i]);
					m_internalPropertyBlock.SetVector(FluidShaderProperties._BlitScaleBias, Vector2.one);
					m_internalPropertyBlock.SetInteger(FluidShaderProperties._RotateSample, (i < 2) ? 1 : 0);

					BlitQuad(m_neighbourCopyCommandBuffer, m_velocityBoundaryCells[i], m_activePressure, m_boundaryMaterial, m_internalPropertyBlock, m_boundaryCopyPass);
				}
			}

			foreach (FluidLayer layer in extensionLayers)
			{
				if (!layer.copyNeighbours)
					continue;

				for (int i = 0; i < sourceblitSidesNeighbor.Length; i++)
				{
					if (!IsExternalBoundary((BoundarySides)i))
					{
						continue;
					}

					RenderTexture source = GetNeighbour((BoundarySides)i).GetFluidLayer(layer.GetType()).activeLayer;
					m_internalPropertyBlock.Clear();
					m_internalPropertyBlock.SetVector(FluidShaderProperties._BlitScaleBiasRt, Vector2.one);
					m_internalPropertyBlock.SetVector(FluidShaderProperties._BlitScaleBias, sourceblitSidesNeighbor[i]);
					m_internalPropertyBlock.SetInteger(FluidShaderProperties._RotateBlit, (i < 2) ? 1 : 0);

					BlitQuad(m_neighbourCopyCommandBuffer, source, m_velocityBoundaryCells[i], m_boundaryMaterial, m_internalPropertyBlock, m_boundaryStorePass);
				}

				for (int i = 0; i < sourceblitSidesNeighbor.Length; i++)
				{
					if (!IsExternalBoundary((BoundarySides)i))
					{
						continue;
					}

					RenderTexture target = layer.activeLayer;

					m_internalPropertyBlock.Clear();
					m_internalPropertyBlock.SetVector(FluidShaderProperties._BlitScaleBiasRt, destblitSides[i]);
					m_internalPropertyBlock.SetVector(FluidShaderProperties._BlitScaleBias, Vector2.one);
					m_internalPropertyBlock.SetInteger(FluidShaderProperties._RotateSample, (i < 2) ? 1 : 0);

					BlitQuad(m_neighbourCopyCommandBuffer, m_velocityBoundaryCells[i], target, m_boundaryMaterial, m_internalPropertyBlock, m_boundaryCopyPass);
				}
			}

			Graphics.ExecuteCommandBuffer(m_neighbourCopyCommandBuffer);
			m_neighbourCopyCommandBuffer.Clear();
		}

		private void UpdateSimulationConstants(float fluidTimestep)
		{
			Vector2 cellSizeScale = new Vector2(settings.cellSize, settings.secondLayerCellSize);
			Vector4 worldCellSize = new Vector4(cellWorldSize, cellWorldSize, cellWorldSize, cellWorldSize);
			Vector4 worldCellSizeRcp = new Vector4(1.0f / cellWorldSize, 1.0f / cellWorldSize, 1.0f / cellWorldSize, 1.0f / cellWorldSize);
			Vector4 cellSize = GetSimulationCellSize();
			Vector4 cellSizeRcp = new Vector4(1.0f / cellSize.x, 1.0f / cellSize.y, 1.0f / cellSize.z, 1.0f / cellSize.w);
			//this seems to be the absolute limit at a timestep of 1.0f / 60.0f
			float cellSizeX = cellSize.x;
			float cellSizeY = cellSize.y;
			float cellSizeSq = cellSizeX * cellSizeY;

			float cellSizeLayer2X = cellSize.z;
			float cellSizeLayer2Y = cellSize.w;
			float cellSizeSqLayer2 = cellSizeLayer2X * cellSizeLayer2Y;

			//At the smallest cellSize we can at most accelerate 9.8f at this time step. But if we scale the cellSize up we can in turn scale acceleration higher
			float maxAcceleration = 9.8f * (cellSizeX / kMinCellSize);
			float acceleration = Mathf.Min(maxAcceleration, settings.acceleration);

			//At the smallest cellSize we can at most accelerate 9.8f at this time step. But if we scale the cellSize up we can in turn scale acceleration higher
			float maxAccelerationLayer2 = 9.8f * (cellSizeLayer2X / kMinCellSize);
			float accelerationLayer2 = Mathf.Min(maxAccelerationLayer2, settings.secondLayerAcceleration);

			Vector2 packedFluidScalar = new Vector2(acceleration * cellSizeX * fluidTimestep,
													acceleration * cellSizeY * fluidTimestep);
			Vector2 packedFluidScalarLayer2 = new Vector2(accelerationLayer2 * cellSizeLayer2X * fluidTimestep,
															accelerationLayer2 * cellSizeLayer2Y * fluidTimestep);

			float packedRcpCellSizeSqDT = (1.0f / cellSizeSq) * fluidTimestep;
			float packedRcpCellSizeSqDTLayer2 = (1.0f / cellSizeSqLayer2) * fluidTimestep;

			Vector2 fluidAcceleration = new Vector2(acceleration, accelerationLayer2);
			Vector2 damping = new Vector2(1.0f - (settings.waveDamping * fluidTimestep), 1.0f - (settings.secondLayerWaveDamping * fluidTimestep));
			Vector4 cellSizePacked = new Vector4(cellSizeX, cellSizeY, cellSizeLayer2X, cellSizeLayer2Y);
			Vector4 cellSizeSqPacked = new Vector4(cellSizeSq, cellSizeSq, cellSizeSqLayer2, cellSizeSqLayer2);
			Vector4 packedFluidData = new Vector4(packedFluidScalar.x, packedFluidScalar.y, packedFluidScalarLayer2.x, packedFluidScalarLayer2.y);
			Vector4 packedRcpFluidData = new Vector4(packedRcpCellSizeSqDT, packedRcpCellSizeSqDT, packedRcpCellSizeSqDTLayer2, packedRcpCellSizeSqDTLayer2);
			Vector2 packedVelocityScale = new Vector2(velocityScale * settings.numberOfCells.x / 1024.0f, m_internalSettings.secondLayerVelocityScale * settings.numberOfCells.x / 1024.0f);
			Vector4 packedEvaporation = new Vector4(settings.linearEvaporation * fluidTimestep,
													settings.proportionalEvaporation * fluidTimestep,
													settings.secondLayerLinearEvaporation * fluidTimestep,
													settings.secondLayerProportionalEvaporation * fluidTimestep);

			// For finding center of grid.
			Vector2 heightFieldRcp = new Vector2((1.0f / (m_activeWaterHeight.width - 1)) * m_activeWaterHeight.width, (1.0f / (m_activeWaterHeight.height - 1)) * m_activeWaterHeight.height);

			m_dynamicCommandBuffer.SetGlobalVector(FluidShaderProperties._Damping, damping);

			m_dynamicCommandBuffer.SetGlobalVector(FluidShaderProperties._WorldCellSize, worldCellSize);
			m_dynamicCommandBuffer.SetGlobalVector(FluidShaderProperties._WorldCellSizeRcp, worldCellSizeRcp);
			m_dynamicCommandBuffer.SetGlobalVector(FluidShaderProperties._CellSize, cellSizePacked);
			m_dynamicCommandBuffer.SetGlobalVector(FluidShaderProperties._CellSizeSq, cellSizeSqPacked);
			m_dynamicCommandBuffer.SetGlobalVector(FluidShaderProperties._CellSizeRcp, cellSizeRcp);
			m_dynamicCommandBuffer.SetGlobalVector(FluidShaderProperties._CellSizeScale, cellSizeScale);

			m_dynamicCommandBuffer.SetGlobalVector(FluidShaderProperties._FluidAcceleration, fluidAcceleration);
			m_dynamicCommandBuffer.SetGlobalVector(FluidShaderProperties._AccelCellSizeDeltaTime, packedFluidData);
			m_dynamicCommandBuffer.SetGlobalVector(FluidShaderProperties._RcpCellSizeSqDeltaTime, packedRcpFluidData);

			m_dynamicCommandBuffer.SetGlobalVector(FluidShaderProperties._VelocityScale, packedVelocityScale);
			m_dynamicCommandBuffer.SetGlobalFloat(FluidShaderProperties._FluidClipHeight, settings.clipHeight);
			m_dynamicCommandBuffer.SetGlobalFloat(FluidShaderProperties._TerrainHeightScale, terrainScale);
			m_dynamicCommandBuffer.SetGlobalFloat(FluidShaderProperties._FluidBaseHeightOffset, fluidBaseHeight);

			m_dynamicCommandBuffer.SetGlobalFloat(FluidShaderProperties._FluidViscosity, 1 - m_internalSettings.secondLayerViscosity);
			m_dynamicCommandBuffer.SetGlobalFloat(FluidShaderProperties._FluidFlowHeight, m_internalSettings.secondLayerFlowHeight);

			m_dynamicCommandBuffer.SetGlobalVector(FluidShaderProperties._Evaporation, packedEvaporation);

			m_dynamicCommandBuffer.SetGlobalVector(FluidShaderProperties._HeightFieldRcp, heightFieldRcp);
			m_dynamicCommandBuffer.SetGlobalVector(FluidShaderProperties._WorldSize, dimension);
			m_dynamicCommandBuffer.SetGlobalVector(FluidShaderProperties._BoundaryCells, new Vector2(ghostCells.x, ghostCells.y));

			m_dynamicCommandBuffer.SetGlobalVector(FluidShaderProperties._Simulation_TexelSize, m_simulationTexelSize);
			m_dynamicCommandBuffer.SetGlobalVector(FluidShaderProperties._VelocityPadding_ST, m_velocityTextureST);
			m_dynamicCommandBuffer.SetGlobalVector(FluidShaderProperties._Padding_ST, m_paddingST);

			m_dynamicCommandBuffer.SetGlobalFloat(FluidShaderProperties._FluidSimDeltaTime, fluidTimestep);
			m_dynamicCommandBuffer.SetGlobalFloat(FluidShaderProperties._FluidSimStepDeltaTime, fluidTimestep);
			m_dynamicCommandBuffer.SetGlobalFloat(FluidShaderProperties._VelocityDeltaTime, fluidTimestep);
			m_dynamicCommandBuffer.SetGlobalFloat(FluidShaderProperties._VelocityDeltaTimeRcp, 1.0f / fluidTimestep);
			m_dynamicCommandBuffer.SetGlobalFloat(FluidShaderProperties._VelocityDamping, 1.0f - (m_internalSettings.velocityDamping * fluidTimestep));
			Graphics.ExecuteCommandBuffer(m_dynamicCommandBuffer);
			m_dynamicCommandBuffer.Clear();
		}

		internal override void UpdateFluidInput(int numSteps, float fluidTimestep)
		{
#if FLUIDFRENZY_RPCORE_15_OR_NEWER
			using (new ProfilingScope(ProfilingSampler.Get(WaterSimProfileID.WaterSimulationDynamic)))
#else
			using (new ProfilingScope(null, ProfilingSampler.Get(WaterSimProfileID.WaterSimulationDynamic)))
#endif
			{
				UpdateDynamicWaterInput(fluidTimestep * numSteps);
			}

			Graphics.ExecuteCommandBuffer(m_dynamicCommandBuffer);
			m_dynamicCommandBuffer.Clear();
		}

		private void UpdateSimulation(int numSteps)
		{
			for (int i = 0; i < numSteps; i++)
			{
				CommandBufferKey key = new CommandBufferKey(m_activeWaterHeight, m_activeOutFlow, null);
				if (!m_staticCommandBuffers.GetCommandBuffer(key, out m_commandBuffer))
				{
					m_commandBuffer.name = name + "WaterSimulation_Fluid" + key.GetHashCode();
					using (new ProfilingScope(m_commandBuffer, ProfilingSampler.Get(WaterSimProfileID.WaterSimulation_Fluid)))
					{
						if (m_hasStaticWaterInput)
						{
							AddFluidStatic(m_commandBuffer, m_staticInput, m_activeWaterHeight, m_nextWaterHeight);
							SwapFluidRT();
						}

						if (!platformSupportsFloat32Blend)
						{
							AddFluidDynamic(m_commandBuffer, m_dynamicInput, m_activeWaterHeight, m_nextWaterHeight);
							SwapFluidRT();
						}

						UpdateSlipFree();
						UpdateFluidHeight();
					}
				}
				else
				{
					int swapCount = 1;
					swapCount += m_hasStaticWaterInput ? 1 : 0;
					swapCount += !platformSupportsFloat32Blend ? 1 : 0;
					if (swapCount % 2 != 0) // this may look confusing, but because the static one up there can swap 2 or 3 times depending if the sim has static water input it might have to swap.
						SwapFluidRT();
					SwapOutFlowRT();
				}
				Graphics.ExecuteCommandBuffer(m_commandBuffer);

				CommandBufferKey velocityKey = new CommandBufferKey(m_activeVelocity, null, null);
				if (!m_velocityCommandBuffers.TryGetCommandBuffer(velocityKey, out m_commandBuffer))
				{
					m_commandBuffer.name = name + "_Velocity_" + velocityKey.GetHashCode();
					using (new ProfilingScope(m_commandBuffer, ProfilingSampler.Get(WaterSimProfileID.WaterSimulation_Velocity)))
					{
						m_velocityCommandBuffers.AddCommandBuffer(velocityKey, m_commandBuffer);
						UpdateVelocity();
					}
				}
				else
				{
					SwapVelocity();
				}
				Graphics.ExecuteCommandBuffer(m_commandBuffer);
			}
		}

		private void UpdateSlipFree()
        {
			if (!settings.openBorders)
			{
				Vector2 texelOffset = new Vector2(1.0f / m_activeWaterHeight.width, 1.0f / m_activeWaterHeight.height);
				Vector4[] sourceblitSides = {   new Vector4(texelOffset.x * 1, 1, texelOffset.x * ghostCells.x, 0),
											new Vector4(texelOffset.x * 1, 1, 1 - texelOffset.x * (ghostCells.x + 1), 0) ,
											new Vector4(1, texelOffset.y * 1, 0, texelOffset.y * ghostCells.y),
											new Vector4(1, texelOffset.y * 1, 0, 1 - texelOffset.y * (ghostCells.y + 1))};

				for (int i = 0; i < sourceblitSides.Length; i++)
				{
					if (IsExternalBoundary((BoundarySides)i))
					{
						continue;
					}

					m_internalPropertyBlock.Clear();
					m_internalPropertyBlock.SetVector(FluidShaderProperties._BlitScaleBiasRt, Vector2.one);
					m_internalPropertyBlock.SetVector(FluidShaderProperties._BlitScaleBias, sourceblitSides[i]);
#if UNITY_2021_1_OR_NEWER
					m_internalPropertyBlock.SetInteger(FluidShaderProperties._RotateBlit, (i < 2) ? 1 : 0);
#else
				m_internalPropertyBlock.SetInt(FluidShaderProperties._RotateBlit, (i < 2) ? 1 : 0);
#endif
					BlitQuad(m_commandBuffer, m_activeWaterHeight, m_waterBoundaryCells[i], m_boundaryMaterial, m_internalPropertyBlock, m_boundaryStorePass);
				}

				Vector4[] destblitSides = {     new Vector4(texelOffset.x * ghostCells.x, 1, 0, 0),
											new Vector4(texelOffset.x * ghostCells.x, 1, 1 - texelOffset.x * ghostCells.x, 0) ,
											new Vector4(1, texelOffset.y * ghostCells.y, 0, 0),
											new Vector4(1, texelOffset.y * ghostCells.y, 0, 1 - texelOffset.y * ghostCells.y)};

				for (int i = 0; i < sourceblitSides.Length; i++)
				{
					if (IsExternalBoundary((BoundarySides)i))
					{
						continue;
					}

					m_internalPropertyBlock.Clear();
					m_internalPropertyBlock.SetVector(FluidShaderProperties._BlitScaleBiasRt, destblitSides[i]);
					m_internalPropertyBlock.SetVector(FluidShaderProperties._BlitScaleBias, Vector2.one);
#if UNITY_2021_1_OR_NEWER
					m_internalPropertyBlock.SetInteger(FluidShaderProperties._RotateSample, (i < 2) ? 1 : 0);
#else
				m_internalPropertyBlock.SetInt(FluidShaderProperties._RotateSample, (i < 2) ? 1 : 0);
#endif
					BlitQuad(m_commandBuffer, m_waterBoundaryCells[i], m_activeWaterHeight, m_boundaryMaterial, m_internalPropertyBlock, m_boundaryCopyPass);
					BlitQuad(m_commandBuffer, m_waterBoundaryCells[i], m_nextWaterHeight, m_boundaryMaterial, m_internalPropertyBlock, m_boundaryCopyPass);
				}
			}
		}

		private void UpdateFluidHeight()
		{
			using (new ProfilingScope(m_commandBuffer, ProfilingSampler.Get(WaterSimProfileID.Outflow)))
			{
				Vector4 clampMinMax = new Vector4(ghostCells.x, m_nextWaterHeight.width - ghostCells.x - 1, ghostCells.y, m_nextWaterHeight.height - ghostCells.y - 1);
				Vector2 offset = new Vector2(1.0f / m_nextWaterHeight.width, 1.0f / m_nextWaterHeight.height);
				Vector4 blitScale = Vector2.one;
				Rect rect = new Rect(0, 0, 1, 1);
				if (!settings.openBorders)
				{
					float xOffset = offset.x * ghostCells.x, yOffset = offset.y * ghostCells.y;

					rect.min = new Vector2(xOffset, yOffset);
					rect.max = new Vector2(1-xOffset, 1-yOffset);
					if (IsExternalBoundary(BoundarySides.Left))
					{
						rect.xMin = 0;
						clampMinMax.x = 0;
					}

					if (IsExternalBoundary(BoundarySides.Right))
					{
						rect.xMax = 1;
						clampMinMax.y = m_activeWaterHeight.width - 1;
					}

					if (IsExternalBoundary(BoundarySides.Bottom))
					{
						rect.yMin = 0;
						clampMinMax.z = 0;
					}

					if (IsExternalBoundary(BoundarySides.Top))
					{
						rect.yMax = 1;
						clampMinMax.w = m_activeWaterHeight.height - 1;
					}

					blitScale = new Vector4(rect.width, rect.height, rect.x, rect.y);
				}
				else
				{
					clampMinMax = new Vector4(-1, 10000, -1, 10000);

					if (IsExternalBoundary(BoundarySides.Left))
					{
						rect.xMin = 0;
						clampMinMax.x = 0;
					}

					if (IsExternalBoundary(BoundarySides.Right))
					{
						rect.xMax = 1;
						clampMinMax.y = m_activeWaterHeight.width - 1;
					}

					if (IsExternalBoundary(BoundarySides.Bottom))
					{
						rect.yMin = 0;
						clampMinMax.z = 0;
					}

					if (IsExternalBoundary(BoundarySides.Top))
					{
						rect.yMax = 1;
						clampMinMax.w = m_activeWaterHeight.height - 1;
					}
				}

				m_internalPropertyBlock.Clear();
				m_internalPropertyBlock.SetVector(FluidShaderProperties._FluxClampMinMax, clampMinMax);
				m_internalPropertyBlock.SetVector(FluidShaderProperties._ExternalOutflowFieldClamp, m_externalOutFlowClamp);

				m_internalPropertyBlock.SetVector(FluidShaderProperties._BlitScaleBias, blitScale);
				m_internalPropertyBlock.SetVector(FluidShaderProperties._BlitScaleBiasRt, blitScale);

				m_internalPropertyBlock.SetTexture(FluidShaderProperties._OutflowField, m_activeOutFlow);
				m_internalPropertyBlock.SetTexture(FluidShaderProperties._TerrainHeightField, m_terrainHeight);
				m_internalPropertyBlock.SetTexture(FluidShaderProperties._FluidHeightField, m_activeWaterHeight);
				m_internalPropertyBlock.SetTexture(FluidShaderProperties._ExternalOutflowField, m_externalOutFlow);

				if (multiLayeredFluid)
				{
					m_internalPropertyBlock.SetTexture(FluidShaderProperties._OutflowFieldLayer2, m_activeOutFlowLayer1);
					RenderTargetIdentifier[] mrt = new RenderTargetIdentifier[] { m_nextOutFlow, m_nextOutFlowLayer1 };
					BlitQuad(m_commandBuffer, null, mrt, m_nextOutFlow.depthBuffer, m_fluidSimulationMaterial, m_internalPropertyBlock, m_fluidSimulationFluxPass);
				}
				else
				{
					BlitQuad(m_commandBuffer, null, m_nextOutFlow, m_fluidSimulationMaterial, m_internalPropertyBlock, m_fluidSimulationFluxPass);
				}
				SwapOutFlowRT();

				m_commandBuffer.SetRenderTarget(m_externalOutFlow);
				m_commandBuffer.ClearRenderTarget(false, true, Color.clear);
			}

			using (new ProfilingScope(m_commandBuffer, ProfilingSampler.Get(WaterSimProfileID.UpdateWaterHeight)))
			{
				Vector2 offset = new Vector2(1.0f / m_nextWaterHeight.width, 1.0f / m_nextWaterHeight.height);
				Vector4 blitScale = Vector2.one;
				Rect rect = new Rect(0, 0, 1, 1);
				if (!settings.openBorders)
				{
					float xScale = offset.x * ghostCells2.x, yScale = offset.y * ghostCells2.y;
					float xOffset = offset.x * ghostCells.x, yOffset = offset.y * ghostCells.y;

					rect.min = new Vector2(xOffset, yOffset);
					rect.max = new Vector2(1 - xOffset, 1 - yOffset);
					if (IsExternalBoundary(BoundarySides.Left))
					{
						rect.xMin = 0;
					}

					if (IsExternalBoundary(BoundarySides.Right))
					{
						rect.xMax = 1;
					}

					if (IsExternalBoundary(BoundarySides.Bottom))
					{
						rect.yMin = 0;
					}

					if (IsExternalBoundary(BoundarySides.Top))
					{
						rect.yMax = 1;
					}

					blitScale = new Vector4(rect.width, rect.height, rect.x, rect.y);

				}

				m_internalPropertyBlock.Clear();
				m_internalPropertyBlock.SetTexture(FluidShaderProperties._OutflowField, m_activeOutFlow);
				m_internalPropertyBlock.SetTexture(FluidShaderProperties._FluidHeightField, m_activeWaterHeight);
				m_internalPropertyBlock.SetVector(FluidShaderProperties._BlitScaleBias, blitScale);
				m_internalPropertyBlock.SetVector(FluidShaderProperties._BlitScaleBiasRt, blitScale);

				if (multiLayeredFluid)
				{
					m_internalPropertyBlock.SetTexture(FluidShaderProperties._OutflowFieldLayer2, m_activeOutFlowLayer1);
				}

				BlitQuad(m_commandBuffer, null, new RenderTargetIdentifier[] { m_nextWaterHeight, m_outFlowVelocity }, m_outFlowVelocity.depthBuffer, m_fluidSimulationMaterial, m_internalPropertyBlock, m_fluidSimulationApplyFluxPass);
				SwapFluidRT();
			}
		}

		private void UpdateVelocity()
		{
			using (new ProfilingScope(m_commandBuffer, ProfilingSampler.Get(WaterSimProfileID.Velocity)))
			{
				if (!settings.openBorders)
					m_solveVelocityMaterial.DisableKeyword("OPEN_BORDER");
				else
					m_solveVelocityMaterial.EnableKeyword("OPEN_BORDER");

				float aspectRatio = dimension.y / dimension.x;
				float aspectRatioX = aspectRatio < 1 ? aspectRatio : 1;
				float aspectRatioY = aspectRatio > 1 ? (1.0f / aspectRatio) : 1;
				Vector2 aspectScale = new Vector4(1.0f / aspectRatioX, 1.0f / aspectRatioY);

				float sqrtTextureSize = Mathf.Sqrt(velocityTextureSize.x);
				if (m_internalSettings.additiveVelocity || m_internalSettings.secondLayerAdditiveVelocity)
				{
					using (new ProfilingScope(m_commandBuffer, ProfilingSampler.Get(WaterSimProfileID.Advect)))
					{
						Vector2 advectScale = (Vector2.one * advectionScale ) / new Vector2(m_nextVelocity.width, m_nextVelocity.height) / GetCellSize();
						m_internalPropertyBlock.Clear();
						m_internalPropertyBlock.SetTexture(FluidShaderProperties._VelocityField, m_activeVelocity);
						m_internalPropertyBlock.SetVector(FluidShaderProperties._AdvectScale, advectScale);
						BlitQuad(m_commandBuffer, null, m_nextVelocity, m_solveVelocityMaterial, m_internalPropertyBlock, m_advectVelocityPass);
						SwapVelocity();
					}

					using (new ProfilingScope(m_commandBuffer, ProfilingSampler.Get(WaterSimProfileID.Divergence)))
					{
						Vector2 divergeRho = Vector2.one;
						Vector2 divergeEpsilon = (Vector2.one / new Vector2(m_activePressure.width, m_activePressure.height) ) ;
						Vector2 divergeEpsilonDT = (-0.5f * divergeEpsilon * divergeRho * aspectScale);

						m_internalPropertyBlock.Clear();
						m_internalPropertyBlock.SetVector(FluidShaderProperties._Epsilon, new Vector4(divergeEpsilon.x, divergeEpsilon.y, divergeEpsilonDT.x, divergeEpsilonDT.y));
						m_internalPropertyBlock.SetTexture(FluidShaderProperties._VelocityField, m_activeVelocity);
						BlitQuad(m_commandBuffer, null, m_divergence, m_solveVelocityMaterial, m_internalPropertyBlock, m_divergencePass);
					}

					using (new ProfilingScope(m_commandBuffer, ProfilingSampler.Get(WaterSimProfileID.Jacobi)))
					{
						m_internalPropertyBlock.Clear();
						m_internalPropertyBlock.SetTexture(FluidShaderProperties._DivergenceField, m_divergence);
						m_internalPropertyBlock.SetFloat(FluidShaderProperties._Pressure, 1.0f - ((1.0f - m_internalSettings.pressure)));

						Vector2 pressureEpsilon = (Vector2.one / new Vector2(m_activePressure.width, m_activePressure.height)) ;
						m_internalPropertyBlock.SetVector(FluidShaderProperties._Epsilon, new Vector4(pressureEpsilon.x, pressureEpsilon.y, pressureEpsilon.x * aspectScale.x, pressureEpsilon.y * aspectScale.y));
						for (int i = 0; i < m_internalSettings.advectionIterations; i++)
						{
							m_internalPropertyBlock.SetTexture(FluidShaderProperties._PressureField, m_activePressure);
							BlitQuad(m_commandBuffer, null, m_nextPressure, m_solveVelocityMaterial, m_internalPropertyBlock, i == 0 ? m_jacobianReducePressurePass : m_jacobianPass);
							m_internalPropertyBlock.SetTexture(FluidShaderProperties._PressureField, m_nextPressure);
							BlitQuad(m_commandBuffer, null, m_activePressure, m_solveVelocityMaterial, m_internalPropertyBlock, m_jacobianPass);
						}
					}

					using (new ProfilingScope(m_commandBuffer, ProfilingSampler.Get(WaterSimProfileID.Pressure)))
					{
						Vector2 pressureRho = Vector2.one;
						Vector2 pressureEpsilon = (Vector2.one / new Vector2(m_activePressure.width, m_activePressure.height) ) ;
						Vector2 pressureEpsilonDT = (Vector2.one / (2.0f * pressureRho * pressureEpsilon * aspectScale));
						m_internalPropertyBlock.Clear();
						m_internalPropertyBlock.SetVector(FluidShaderProperties._Epsilon, new Vector4(pressureEpsilon.x, pressureEpsilon.y, pressureEpsilonDT.x, pressureEpsilonDT.y));
						m_internalPropertyBlock.SetTexture(FluidShaderProperties._VelocityField, m_activeVelocity);
						m_internalPropertyBlock.SetTexture(FluidShaderProperties._PressureField, m_activePressure);
						BlitQuad(m_commandBuffer, null, m_nextVelocity, m_solveVelocityMaterial, m_internalPropertyBlock, m_applyPressurePass);
						SwapVelocity();
					}
				}

				if (m_internalSettings.additiveVelocity)
					m_integrateVelocityMaterial.EnableKeyword("ADDITIVE_VELOCITY");
				else
					m_integrateVelocityMaterial.DisableKeyword("ADDITIVE_VELOCITY");

				if (m_internalSettings.secondLayerAdditiveVelocity)
					m_integrateVelocityMaterial.EnableKeyword("SECOND_ADDITIVE_VELOCITY");
				else
					m_integrateVelocityMaterial.DisableKeyword("SECOND_ADDITIVE_VELOCITY");

				using (new ProfilingScope(m_commandBuffer, ProfilingSampler.Get(WaterSimProfileID.FlowToVelocity)))
				{
					m_internalPropertyBlock.Clear();
					m_internalPropertyBlock.SetTexture(FluidShaderProperties._OutflowField, m_nextOutFlow);
					m_internalPropertyBlock.SetTexture(FluidShaderProperties._PreviousVelocityField, m_activeVelocity);
					m_internalPropertyBlock.SetTexture(FluidShaderProperties._VelocityField, m_outFlowVelocity);
					m_internalPropertyBlock.SetVector(FluidShaderProperties._VelocityMax, new Vector2(m_internalSettings.velocityMax, m_internalSettings.velocityMax));

					Vector2 velocitySize = velocityTextureSize;
					Vector2 ghostCellRatio = new Vector2(velocitySize.x / m_activeVelocity.width, velocitySize.y / m_activeVelocity.height);
					Vector4 ghostCellBlitScale = new Vector4(1.0f / ghostCellRatio.x,
															 1.0f / ghostCellRatio.y,
															ghostCellRatio.x,
															ghostCellRatio.y);
					m_internalPropertyBlock.SetVector(FluidShaderProperties._VelocityBlitScaleBias, ghostCellBlitScale);

					if (!settings.openBorders)
					{
						float offsetX = (1.0f / m_activeVelocity.width) * (velocityGhostCells + 1);
						float offsetY = (1.0f / m_activeVelocity.height) * (velocityGhostCells + 1);
						Vector4 velocityBoundary = new Vector4(offsetX, offsetY, 1 - offsetX, 1 - offsetY);
						if (IsExternalBoundary(BoundarySides.Left))
							velocityBoundary.x = 0;
						if (IsExternalBoundary(BoundarySides.Right))
							velocityBoundary.z = 1;
						if (IsExternalBoundary(BoundarySides.Bottom))
							velocityBoundary.y = 0;
						if (IsExternalBoundary(BoundarySides.Top))
							velocityBoundary.w = 1;

						m_internalPropertyBlock.SetVector(FluidShaderProperties._VelocityFieldBoundary, velocityBoundary);
						m_integrateVelocityMaterial.DisableKeyword("OPEN_BORDER");
					}
					else
						m_integrateVelocityMaterial.EnableKeyword("OPEN_BORDER");

					BlitQuad(m_commandBuffer, null, m_nextVelocity, m_integrateVelocityMaterial, m_internalPropertyBlock, 0);
					SwapVelocity();
				}
			}
		}

		protected override void OnSettingsChanged()
		{
			if (!settings.openBorders)
			{
				m_addFluidMaterial.DisableKeyword("OPEN_BORDER");

				Vector2 texelOffset = new Vector2(1.0f / m_activeOutFlow.width, 1.0f / m_activeOutFlow.height);
				Vector4[] blitSides = { new Vector4(texelOffset.x * ghostCells.x, 1, 0, 0),
											new Vector4(texelOffset.x * ghostCells.x, 1, 1 - texelOffset.x * ghostCells.x, 0) ,
											new Vector4(1, texelOffset.y * ghostCells.y, 0, 0),
											new Vector4(1, texelOffset.y * ghostCells.y, 0, 1 - texelOffset.y * ghostCells.y)};

				for (int i = 0; i < blitSides.Length; i++)
				{
					m_internalPropertyBlock.SetVector(FluidShaderProperties._BlitScaleBiasRt, blitSides[i]);
					m_internalPropertyBlock.SetVector(FluidShaderProperties._BlitScaleBias, Vector2.one);
					BlitQuad(m_dynamicCommandBuffer, null, m_activeOutFlow, m_boundaryMaterial, m_internalPropertyBlock, m_boundaryResetPass);
					BlitQuad(m_dynamicCommandBuffer, null, m_nextOutFlow, m_boundaryMaterial, m_internalPropertyBlock, m_boundaryResetPass);
					if (m_activeOutFlowLayer1)
						BlitQuad(m_dynamicCommandBuffer, null, m_activeOutFlowLayer1, m_boundaryMaterial, m_internalPropertyBlock, m_boundaryResetPass);

					if (m_nextOutFlowLayer1)
						BlitQuad(m_dynamicCommandBuffer, null, m_nextOutFlowLayer1, m_boundaryMaterial, m_internalPropertyBlock, m_boundaryResetPass);
				}
			}
			else
			{
				m_addFluidMaterial.EnableKeyword("OPEN_BORDER");
			}

			if (m_internalSettings.secondLayerCustomViscosity)
			{
				m_fluidSimulationMaterial.EnableKeyword("FLUID_CUSTOMVISCOSITY");
			}
			else
			{
				m_fluidSimulationMaterial.DisableKeyword("FLUID_CUSTOMVISCOSITY");
			}

			m_staticCommandBuffers.Clear();
			m_velocityCommandBuffers.Clear();
			m_renderInfoCommandBuffers.Clear();
			m_timestep = 0;
		}

		internal Vector4 GetSimulationCellSize()
		{
			//Make this work with dimension, but also make sure its clamped and that cellsize can be 1 or higher.
			float dimAspectRatio = dimension.y / dimension.x;
			float bufferAspectRatio = (float)(settings.numberOfCells.y) / settings.numberOfCells.x;
			float aspectRatio = dimAspectRatio / bufferAspectRatio;
			float aspectRatioY = aspectRatio < 1 ? aspectRatio : 1;
			float aspectRatioX = aspectRatio > 1 ? (1.0f / aspectRatio) : 1;
			Vector4 aspectScale = new Vector4(1.0f / aspectRatioX, 1.0f / aspectRatioY, 1.0f / aspectRatioX, 1.0f / aspectRatioY);

			Vector4 cellSize;
			if (dimensionMode == DimensionMode.Bounds)
			{
				Vector2 cellSizeFromDimension = dimension / settings.numberOfCells;
				cellWorldSize = Mathf.Max(cellSizeFromDimension.x, cellSizeFromDimension.y);
			}

			cellSize = Vector4.one * cellWorldSize;
			cellSize.Scale(aspectScale);
			cellSize.Scale(cellSize);
			cellSize.Scale(new Vector4(settings.cellSize, settings.cellSize, settings.secondLayerCellSize, settings.secondLayerCellSize));

			cellSize = Vector4.Max(Vector4.one * kMinCellSize, cellSize);

			return cellSize;
		}

		public override Vector4 GetCellSize()
		{
			//Make this work with dimension, but also make sure its clamped and that cellsize can be 1 or higher.
			float dimAspectRatio = dimension.y / dimension.x;
			float bufferAspectRatio = (float)(settings.numberOfCells.y) / settings.numberOfCells.x;
			float aspectRatio = dimAspectRatio / bufferAspectRatio;
			float aspectRatioY = aspectRatio < 1 ? aspectRatio : 1;
			float aspectRatioX = aspectRatio > 1 ? (1.0f / aspectRatio) : 1;
			Vector4 aspectScale = new Vector4(1.0f / aspectRatioX, 1.0f / aspectRatioY, 1.0f / aspectRatioX, 1.0f / aspectRatioY);

			Vector4 cellSize;
			if (dimensionMode == DimensionMode.Bounds)
			{
				Vector2 cellSizeFromDimension = dimension / settings.numberOfCells;
				cellWorldSize = Mathf.Max(cellSizeFromDimension.x, cellSizeFromDimension.y);
			}

			cellSize = Vector4.one * cellWorldSize;
			cellSize.Scale(aspectScale);
			cellSize.Scale(new Vector4(settings.cellSize, settings.cellSize, settings.secondLayerCellSize, settings.secondLayerCellSize));

			cellSize = Vector4.Max(Vector4.one * Mathf.Sqrt(kMinCellSize), cellSize);

			return cellSize;
		}

		private void SwapOutFlowRT()
		{
			Swap(ref m_activeOutFlow, ref m_nextOutFlow);
			Swap(ref m_activeOutFlowLayer1, ref m_nextOutFlowLayer1);
		}
	}
}