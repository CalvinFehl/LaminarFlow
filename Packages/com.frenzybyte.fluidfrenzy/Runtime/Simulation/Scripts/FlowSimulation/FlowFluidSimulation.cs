using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using System.Collections.Generic;
using UnityEngine.Serialization;
using System.Collections;

namespace FluidFrenzy
{
	/// <summary>
	/// <see cref="FlowFluidSimulation"/> is the core component of Fluid Frenzy. It handles the full fluid simulation based on the 2D Shallow Water Equations (SWE) approach for large bodies of water.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The <c>FlowFluidSimulation</c> leverages the **Shallow Water Equations (SWE)** to simulate large water bodies such as rivers, lakes, and oceans by treating the fluid as a 2D height field. This velocity-based model is computationally efficient, allowing for real-time performance and multiple simulation iterations per frame.
	/// </para>
	/// 
	/// <h4>The 2D Height Field Model</h4>
	/// <para>
	/// The simulation models large bodies of water (like rivers, lakes, and oceans) using a highly efficient <c>2D Height Field</c>. This model is a streamlined version of full 3D fluid dynamics, designed for fast, real-time performance. It tracks two main components across a grid: the <c>Height Field</c> (<c>h</c>), which manages the overall water level, and the <c>Horizontal Velocity Field</c> (<c>v</c>), which controls the direction and speed of the flow. This approach is key to capturing realistic horizontal phenomena like strong river currents, swirling whirlpools, and how objects are pushed by the flow features that simpler wave simulations miss.
	/// </para>
	/// 
	/// <h4>Performance and Stability</h4>
	/// <para>
	/// The solver is built for maximum speed and runs on a <c>Fixed Timestep</c>. To maintain accuracy and stability regardless of the rendering frame rate, the simulation employs several techniques:
	/// </para>
	/// <list type="bullet">
	/// 	<item>
	/// 		<term>Adaptive Stepping</term>
	/// 		<description>The simulation will run multiple times per frame to catch up if the frame rate drops, or skip a frame if the frame rate is higher than the simulation's fixed timestep.</description>
	/// 	</item>
	/// 	<item>
	/// 		<term>Clamping</term>
	/// 		<description>Limits the water's maximum flow speed and ensures the water level never dips below the terrain (<c>h >= 0</c>). This is vital for stability, especially in dynamic, high-energy water scenes.</description>
	/// 	</item>
	/// 	<item>
	/// 		<term>Overshooting Reduction</term>
	/// 		<description>Automatically detects and smooths out unnatural wave artifacts that appear when large waves transition too quickly into shallow areas, making breaking waves look more convincing.</description>
	/// 	</item>
	/// </list>
	/// 
	/// <h4>Extensibility: FluidLayer System</h4>
	/// <para>
	/// The simulation core can be extended using the <c>FluidLayer</c> system, which allows you to attach custom or prebuilt components (like <see cref="FoamLayer"/> or <see cref="FluidFlowMapping"/>) to add visual effects or enhance flow dynamics without modifying the core solver.
	/// </para>
	/// 
	/// <list type="table">
	/// 	<listheader>
	/// 		<term>Pros</term>
	/// 		<term>Cons</term>
	/// 	</listheader>
	/// 	<item><term>Lower performance cost.</term><term>Reduces velocity on height fluids to remain stable.</term></item>
	/// 	<item><term>Lower VRAM usages.</term><term>Lower quality velocity field.</term></item>
	/// 	<item><term>More control over fluid velocity.</term><term>Sharper wave edges.</term></item>
	/// 	<item><term>Slower moving fluids can have even lower cost.</term><term>Less stability on fast moving fluids.</term></item>
	/// 	<item><term>Waves and velocity field are coupled making Flow Mapping and waves in sync.</term><term></term></item>
	/// </list>
	/// </remarks>
	public partial class FlowFluidSimulation : FluidSimulation
	{
		private static Vector4 kMinCellSize = new Vector4(0.078125f, 0.078125f, 0.078125f, 0.078125f);
		/// <summary>
		/// The number of internal sub-steps (iterations) the simulation performs per frame to increase numerical stability and accuracy.
		/// </summary>
		/// <remarks>
		/// This value represents a trade-off between stability and performance.
		/// <list type="bullet">
		/// 	<item>
		/// 		<term>Stability</term>
		/// 		<description>A smaller <see cref="cellWorldSize"/> (higher spatial detail) or faster effective fluid movement requires more iterations to prevent the simulation from becoming unstable.</description>
		/// 	</item>
		/// 	<item>
		/// 		<term>Performance</term>
		/// 		<description>Each iteration executes the core simulation logic, meaning increasing this value directly and linearly increases the GPU computation cost.</description>
		/// 	</item>
		/// </list>
		/// </remarks>
		public int iterations = 4;

		private FlowFluidSimulationSettings m_internalSettings;

		protected override RenderTextureFormat velocityFormat { get { return  settings.secondLayer ? RenderTextureFormat.ARGBFloat : RenderTextureFormat.RGFloat; } }

		protected override void Awake()
		{
			m_internalSettings = settings as FlowFluidSimulationSettings;
			simulationType = FluidSimulationType.Flow;
			base.Awake();

			//Init markers
			ProfilingSampler.Get(WaterSimProfileID.WaterSimulation_Fluid);
		}

		protected override void Start()
        {
			m_internalSettings = settings as FlowFluidSimulationSettings;
			multiLayeredFluid = settings.secondLayer;

			base.Start();

			bounds = CalculateBounds();

			ghostCells = new Vector2Int(8, 8);
			ghostCells2 = ghostCells * 2;

			velocityGhostCells = ghostCells.x;
			velocityGhostCells2 = ghostCells2.x;

			numRenderCells = settings.numberOfCells + Vector2Int.one;
			numSimulationCells = numRenderCells + ghostCells2;

			Vector2 paddingTexel = new Vector2(1.0f / numSimulationCells.x, 1.0f / numSimulationCells.y);
			m_paddingST = new Vector4(	1.0f - paddingTexel.x * ghostCells2.x,
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
			DestroyMaterials();
		}

		protected override void InitRenderTextures()
		{
			base.InitRenderTextures();

			Vector2Int velocitySize = numSimulationCells;
			m_activeVelocity = GraphicsHelpers.CreateSimulationRT(velocitySize.x, velocitySize.y, velocityFormat, true, name: "Velocity_1");
			m_nextVelocity = GraphicsHelpers.CreateSimulationRT(velocitySize.x, velocitySize.y, velocityFormat, true, name: "Velocity_2");

			Vector2 velocityTexelSize = new Vector2((float)(velocityGhostCells) / (velocityTextureSize.x + velocityGhostCells2),
													(float)(velocityGhostCells) / (velocityTextureSize.y + velocityGhostCells2));
			m_velocityTextureST = new Vector4(1.0f - velocityTexelSize.x * 2,
									1.0f - velocityTexelSize.y * 2,
									velocityTexelSize.x,
									velocityTexelSize.y);

			m_simulationTexelSize = new Vector4(1.0f / numSimulationCells.x, 1.0f / numSimulationCells.y, numSimulationCells.x, numSimulationCells.y);

			m_velocityBoundaryCells = new RenderTexture[(int)BoundarySides.Max];

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


			RenderTexture.active = previousRT;
		}

		/// <summary>
		/// Resets the <see cref="FluxFluidSimulation"/> to its original state.
		/// </summary>
		public override void ResetSimulation()
		{
			UpdateSimulationConstants(1, 1.0f);
			base.ResetSimulation();

			if (!settings.openBorders)
			{
				m_addFluidMaterial.DisableKeyword("OPEN_BORDER");
				m_fluidSolverMaterial.DisableKeyword("OPEN_BORDER");
			}
			else
			{
				m_addFluidMaterial.EnableKeyword("OPEN_BORDER");
				m_fluidSolverMaterial.EnableKeyword("OPEN_BORDER");
			}

			FluidSimulationManager.s_simulationTime = 0;
		}

		public override void PreUpdate(float deltaTime, int numSteps = 2)
		{
			int numIterations = numSteps * iterations;
			float fluidTimestep = kMaxTimestep / iterations;
			m_commandBuffer = m_dynamicCommandBuffer;
			base.PreUpdate(deltaTime, numSteps);
			UpdateSimulationConstants(numIterations, fluidTimestep);
			UpdateFluidInput(numIterations, fluidTimestep);

			UpdateFluidRigidBody();
		}

		public override void Step(float deltaTime, int numSteps = 2)
		{
			base.Step(deltaTime, numSteps);

			int numIterations = numSteps * iterations;
			float fluidTimestep = kMaxTimestep / iterations;
			m_commandBuffer = m_dynamicCommandBuffer;

			UpdateCollider();
			UpdateSimulationConstants(numIterations, fluidTimestep);

			UpdateTerrain(updateGroundEveryFrame);

			UpdateSimulation(numIterations, fluidTimestep);
			UpdateAdvectionLayers(fluidTimestep * numIterations, numIterations);
			
			PostProcessFluidModifiers(fluidTimestep * numIterations);

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

			for (int i = 0; i < sourceblitSidesNeighbor.Length; i++)
			{
				if (!IsExternalBoundary((BoundarySides)i))
				{
					continue;
				}

				RenderTexture source = GetNeighbour((BoundarySides)i).fluidHeight;
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
			}

			for (int i = 0; i < sourceblitSidesNeighbor.Length; i++)
			{
				if (!IsExternalBoundary((BoundarySides)i))
				{
					continue;
				}

				RenderTexture source = GetNeighbour((BoundarySides)i).velocityTexture;
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

				BlitQuad(m_neighbourCopyCommandBuffer, m_waterBoundaryCells[i], velocityTexture, m_boundaryMaterial, m_internalPropertyBlock, m_boundaryCopyPass);
			}

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


			foreach (FluidLayer layer in extensionLayers)
			{
				if (!layer.copyNeighbours)
					continue;

				RenderTexture target = layer.activeLayer;

				texelOffset = new Vector2(1.0f / target.width, 1.0f / target.height);

				sourceblitSidesNeighbor[0] = new Vector4(texelOffset.x * ghostCells.x, 1, 1 - texelOffset.x * ghostCells2.x, 0);    // Right side of the neighbour
				sourceblitSidesNeighbor[1] = new Vector4(texelOffset.x * ghostCells.x, 1, texelOffset.x * ghostCells.x, 0);         // left side of the neighbour
				sourceblitSidesNeighbor[2] = new Vector4(1, texelOffset.y * ghostCells.y, 0, 1 - texelOffset.y * ghostCells2.y);    // Bottom side of the neighbour
				sourceblitSidesNeighbor[3] = new Vector4(1, texelOffset.y * ghostCells.y, 0, texelOffset.y * ghostCells.y);          // Top side of the neighbour  

				destblitSides[0] = new Vector4(texelOffset.x * ghostCells.x, 1, 0, 0);
				destblitSides[1] = new Vector4(texelOffset.x * ghostCells.x, 1, 1 - texelOffset.x * ghostCells.x, 0);
				destblitSides[2] = new Vector4(1, texelOffset.y * ghostCells.y, 0, 0);
				destblitSides[3] = new Vector4(1, texelOffset.y * ghostCells.y, 0, 1 - texelOffset.y * ghostCells.y);

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

					BlitQuad(m_neighbourCopyCommandBuffer, m_waterBoundaryCells[i], target, m_boundaryMaterial, m_internalPropertyBlock, m_boundaryCopyPass);
				}
			}

			Graphics.ExecuteCommandBuffer(m_neighbourCopyCommandBuffer);
			m_neighbourCopyCommandBuffer.Clear();
		}
		
		private void UpdateSimulationConstants(int numSteps, float fluidTimestep)
		{
			Vector2 cellSizeScale = new Vector2(settings.cellSize, settings.secondLayerCellSize);
			Vector4 cellSize = GetCellSize();
			Vector4 cellSizeRcp = new Vector4(1.0f / cellSize.x, 1.0f / cellSize.y, 1.0f / cellSize.z, 1.0f / cellSize.w);

			Vector4 worldCellSize = new Vector4(cellWorldSize, cellWorldSize, cellWorldSize, cellWorldSize);
			Vector4 worldCellSizeRcp = new Vector4(1.0f / cellWorldSize, 1.0f / cellWorldSize, 1.0f / cellWorldSize, 1.0f / cellWorldSize);

			// For finding center of grid.
			Vector2 heightFieldRcp = new Vector2((1.0f / (m_activeWaterHeight.width - 1)) * m_activeWaterHeight.width, (1.0f / (m_activeWaterHeight.height - 1)) * m_activeWaterHeight.height);


			// Setup settings
			Vector4 packedEvaporation = new Vector4(settings.linearEvaporation * fluidTimestep,
					settings.proportionalEvaporation * fluidTimestep,
					settings.secondLayerLinearEvaporation * fluidTimestep,
					settings.secondLayerProportionalEvaporation * fluidTimestep);

			Vector2 damping = new Vector2(settings.waveDamping * fluidTimestep, settings.secondLayerWaveDamping * fluidTimestep);
			m_commandBuffer.SetGlobalVector(FluidShaderProperties._Damping, damping);
			m_commandBuffer.SetGlobalVector(FluidShaderProperties._WorldCellSize, worldCellSize);
			m_commandBuffer.SetGlobalVector(FluidShaderProperties._WorldCellSizeRcp, worldCellSizeRcp);
			m_commandBuffer.SetGlobalVector(FluidShaderProperties._CellSize, cellSize);
			m_commandBuffer.SetGlobalVector(FluidShaderProperties._CellSizeScale, cellSizeScale);
			m_commandBuffer.SetGlobalVector(FluidShaderProperties._CellSizeRcp, cellSizeRcp);
			m_commandBuffer.SetGlobalFloat(FluidShaderProperties._TerrainHeightScale, terrainScale);
			m_commandBuffer.SetGlobalFloat(FluidShaderProperties._FluidClipHeight, settings.clipHeight);
			m_commandBuffer.SetGlobalFloat(FluidShaderProperties._FluidBaseHeightOffset, fluidBaseHeight);
			m_commandBuffer.SetGlobalFloat(FluidShaderProperties._FluidSimDeltaTime, fluidTimestep);
			m_commandBuffer.SetGlobalFloat(FluidShaderProperties._FluidSimStepDeltaTime, fluidTimestep);
			m_commandBuffer.SetGlobalFloat(FluidShaderProperties._VelocityDeltaTime, fluidTimestep * numSteps);
			m_commandBuffer.SetGlobalVector(FluidShaderProperties._Evaporation, packedEvaporation);
			m_commandBuffer.SetGlobalVector(FluidShaderProperties._HeightFieldRcp, heightFieldRcp);
			m_commandBuffer.SetGlobalVector(FluidShaderProperties._WorldSize, dimension);
			m_commandBuffer.SetGlobalVector(FluidShaderProperties._BoundaryCells, new Vector2(ghostCells.x, ghostCells.y));
			m_commandBuffer.SetGlobalVector(FluidShaderProperties._Padding_ST, m_paddingST);
			m_commandBuffer.SetGlobalVector(FluidShaderProperties._FluidAcceleration, new Vector2(settings.acceleration, settings.secondLayerAcceleration));

			m_commandBuffer.SetGlobalVector(FluidShaderProperties._Simulation_TexelSize, m_simulationTexelSize);
			m_commandBuffer.SetGlobalVector(FluidShaderProperties._VelocityPadding_ST, m_velocityTextureST);

			m_commandBuffer.SetGlobalVector(FluidShaderProperties._VelocityMax, new Vector2(m_internalSettings.velocityMax, m_internalSettings.secondLayerVelocityMax));
			m_commandBuffer.SetGlobalVector(FluidShaderProperties._AccelerationMax, new Vector2(m_internalSettings.accelerationMax, m_internalSettings.secondLayerAccelerationMax));
			m_commandBuffer.SetGlobalFloat(FluidShaderProperties._OvershootingEdge, 2.0f * cellSize.x * m_internalSettings.overshootingEdge);
			m_commandBuffer.SetGlobalFloat(FluidShaderProperties._OvershootingScale, m_internalSettings.overshootingScale);

			Graphics.ExecuteCommandBuffer(m_commandBuffer);
			m_commandBuffer.Clear();
		}
		
		internal override void UpdateFluidInput(int numSteps, float fluidTimestep)
		{
#if FLUIDFRENZY_RPCORE_15_OR_NEWER
			using (new ProfilingScope(ProfilingSampler.Get(WaterSimProfileID.WaterSimulationDynamic)))
#else
			using (new ProfilingScope(m_dynamicCommandBuffer, ProfilingSampler.Get(WaterSimProfileID.WaterSimulationDynamic)))
#endif
			{
				UpdateDynamicWaterInput(fluidTimestep * numSteps);
				if (!platformSupportsFloat32Blend)
				{
					AddFluidDynamic(m_commandBuffer, m_dynamicInput, m_activeWaterHeight, m_nextWaterHeight);
					SwapFluidRT();
				}
			}
			Graphics.ExecuteCommandBuffer(m_dynamicCommandBuffer);
			m_dynamicCommandBuffer.Clear();
		}

		private void UpdateSimulation(int numSteps, float fluidTimestep)
		{
			Vector2 offset = new Vector2(1.0f / m_nextWaterHeight.width, 1.0f / m_nextWaterHeight.height);
			float xScale = offset.x * ghostCells2.x, yScale = offset.y * ghostCells2.y;
			float xOffset = offset.x * ghostCells.x, yOffset = offset.y * ghostCells.y;
			if (IsExternalBoundary(BoundarySides.Left))
			{
				xScale -= offset.x * ghostCells.x;
				xOffset = 0;
			}

			if (IsExternalBoundary(BoundarySides.Right))
			{
				xScale -= offset.x * ghostCells.x;
			}

			if (IsExternalBoundary(BoundarySides.Bottom))
			{
				yScale -= offset.y * ghostCells.y;
				yOffset = 0;
			}

			if (IsExternalBoundary(BoundarySides.Top))
			{
				yScale -= offset.y * ghostCells.y;
			}

			Vector4 blitScale = new Vector4(1 - xScale, 1 - yScale, xOffset, yOffset);

			using (new ProfilingScope(m_commandBuffer, ProfilingSampler.Get(WaterSimProfileID.WaterSimulation_Fluid)))
			{
				UpdateSlipFree();
				for (int i = 0; i < numSteps; i++)
				{
					//Integrate Velocity
					m_internalPropertyBlock.Clear();
					m_internalPropertyBlock.SetFloat(FluidShaderProperties._TerrainHeightScale, terrainScale);
					m_internalPropertyBlock.SetTexture(FluidShaderProperties._VelocityField, m_activeVelocity);
					m_internalPropertyBlock.SetTexture(FluidShaderProperties._FluidHeightField, m_activeWaterHeight);
					m_internalPropertyBlock.SetTexture(FluidShaderProperties._TerrainHeightField, m_terrainHeight);
					m_internalPropertyBlock.SetVector(FluidShaderProperties._BlitScaleBias, blitScale);
					m_internalPropertyBlock.SetVector(FluidShaderProperties._BlitScaleBiasRt, blitScale);
					BlitQuad(m_commandBuffer, null, m_nextVelocity, m_fluidSolverMaterial, m_internalPropertyBlock, m_fluidSolverIntegrateVelocityPass);

					SwapVelocity();

					//Integrate height
					m_internalPropertyBlock.Clear();
					m_internalPropertyBlock.SetTexture(FluidShaderProperties._FluidHeightField, m_activeWaterHeight);
					m_internalPropertyBlock.SetTexture(FluidShaderProperties._VelocityField, m_activeVelocity);
					m_internalPropertyBlock.SetVector(FluidShaderProperties._BlitScaleBias, Vector2.one);
					m_internalPropertyBlock.SetVector(FluidShaderProperties._BlitScaleBiasRt, Vector2.one);
					BlitQuad(m_commandBuffer, null, m_nextWaterHeight, m_fluidSolverMaterial, m_internalPropertyBlock, m_fluidSolverIntegrateHeightPass);
					SwapFluidRT();
				}
			}

			m_commandBuffer.SetGlobalFloat(FluidShaderProperties._FluidSimDeltaTime, fluidTimestep * numSteps);

			// Overshooting reduction
			if (m_internalSettings.overshootingReduction && m_internalSettings.overshootingScale > 0)
			{
				m_internalPropertyBlock.Clear();
				m_internalPropertyBlock.SetFloat(FluidShaderProperties._TerrainHeightScale, terrainScale);
				m_internalPropertyBlock.SetTexture(FluidShaderProperties._FluidHeightField, m_activeWaterHeight);
				m_internalPropertyBlock.SetTexture(FluidShaderProperties._TerrainHeightField, m_terrainHeight);
				BlitQuad(m_commandBuffer, null, m_nextWaterHeight, m_fluidSolverMaterial, m_internalPropertyBlock, m_fluidSolverOvershootReductionPass);
				SwapFluidRT();
			}

			Graphics.ExecuteCommandBuffer(m_dynamicCommandBuffer);
			m_dynamicCommandBuffer.Clear();
		}
		
		private void UpdateSlipFree()
		{
			if (!settings.openBorders)
			{
				Vector2 texelOffset = new Vector2(1.0f / m_activeWaterHeight.width, 1.0f / m_activeWaterHeight.height);
				Span<Vector4> sourceblitSides = stackalloc Vector4[4];
				sourceblitSides[0] = new Vector4(texelOffset.x, 1, texelOffset.x * ghostCells.x, 0);
				sourceblitSides[1] = new Vector4(texelOffset.x, 1, 1 - texelOffset.x * ghostCells.x, 0);
				sourceblitSides[2] = new Vector4(1, texelOffset.y, 0, texelOffset.y * ghostCells.y);
				sourceblitSides[3] = new Vector4(1, texelOffset.y, 0, 1 - texelOffset.y * ghostCells.y);

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

				Span<Vector4> destblitSides = stackalloc Vector4[4];
				destblitSides[0] = new Vector4(texelOffset.x * ghostCells.x, 1, 0, 0);
				destblitSides[1] = new Vector4(texelOffset.x * ghostCells.x, 1, 1 - texelOffset.x * ghostCells.x, 0);
				destblitSides[2] = new Vector4(1, texelOffset.y * ghostCells.y, 0, 0);
				destblitSides[3] = new Vector4(1, texelOffset.y * ghostCells.y, 0, 1 - texelOffset.y * ghostCells.y);

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
				}

				destblitSides[0] = new Vector4(texelOffset.x * ghostCells.x, 1, 0, 0);
				destblitSides[1] = new Vector4(texelOffset.x * ghostCells.x, 1, 1 - texelOffset.x * ghostCells.x, 0);
				destblitSides[2] = new Vector4(1, texelOffset.y * ghostCells.y, 0, 0);
				destblitSides[3] = new Vector4(1, texelOffset.y * ghostCells.y, 0, 1 - texelOffset.y * ghostCells.y);

				for (int i = 0; i < destblitSides.Length; i++)
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
					BlitQuad(m_commandBuffer, null, m_activeVelocity, m_boundaryMaterial, m_internalPropertyBlock, m_boundaryResetPass);
					BlitQuad(m_commandBuffer, null, m_nextVelocity, m_boundaryMaterial, m_internalPropertyBlock, m_boundaryResetPass);
				}
			}
			else
			{
				Vector2 texelOffset = new Vector2(1.0f / m_activeWaterHeight.width, 1.0f / m_activeWaterHeight.height);
				Span<Vector4> destblitSides = stackalloc Vector4[4];
				destblitSides[0] = new Vector4(texelOffset.x * ghostCells.x, 1, 0, 0);
				destblitSides[1] = new Vector4(texelOffset.x * ghostCells.x, 1, 1 - texelOffset.x * ghostCells.x, 0);
				destblitSides[2] = new Vector4(1, texelOffset.y * ghostCells.y, 0, 0);
				destblitSides[3] = new Vector4(1, texelOffset.y * ghostCells.y, 0, 1 - texelOffset.y * ghostCells.y);

				float maxVelocity = m_internalSettings.velocityMax;

				Span<Vector4> velocitySides = stackalloc Vector4[4];
				velocitySides[0] = new Vector4(-maxVelocity, 0, -maxVelocity, 0);
				velocitySides[1] = new Vector4(maxVelocity, 0, maxVelocity, 0);
				velocitySides[2] = new Vector4(0, -maxVelocity, 0, -maxVelocity);
				velocitySides[3] = new Vector4(0, maxVelocity, 0, maxVelocity);

				for (int i = 0; i < destblitSides.Length; i++)
				{
					if (IsExternalBoundary((BoundarySides)i))
					{
						continue;
					}

					m_internalPropertyBlock.Clear();
					m_internalPropertyBlock.SetVector(FluidShaderProperties._BlitScaleBiasRt, destblitSides[i]);
					m_internalPropertyBlock.SetVector(FluidShaderProperties._BlitScaleBias, Vector2.one);
					m_internalPropertyBlock.SetVector("_BoundaryValue", velocitySides[i]);
#if UNITY_2021_1_OR_NEWER
					m_internalPropertyBlock.SetInteger(FluidShaderProperties._RotateSample, (i < 2) ? 1 : 0);
#else
					m_internalPropertyBlock.SetInt(FluidShaderProperties._RotateSample, (i < 2) ? 1 : 0);
#endif
					BlitQuad(m_commandBuffer, null, m_activeVelocity, m_boundaryMaterial, m_internalPropertyBlock, m_boundarySetPass);
					BlitQuad(m_commandBuffer, null, m_nextVelocity, m_boundaryMaterial, m_internalPropertyBlock, m_boundarySetPass);
				}

			}
		}

		protected override void OnSettingsChanged()
		{
			if (!settings.openBorders)
			{
				m_addFluidMaterial.DisableKeyword("OPEN_BORDER");
				m_fluidSolverMaterial.DisableKeyword("OPEN_BORDER");
			}
			else
			{
				m_addFluidMaterial.EnableKeyword("OPEN_BORDER");
				m_fluidSolverMaterial.EnableKeyword("OPEN_BORDER");
			}

			m_staticCommandBuffers.Clear();
			m_velocityCommandBuffers.Clear();
			m_renderInfoCommandBuffers.Clear();
			m_timestep = 0;
		}

		public override Vector4 GetCellSize()
		{
			if (!settings)
			{
				return Vector4.one;
			}
			Vector4 cellSize;
			float dimAspectRatio = dimension.y / dimension.x;
			float bufferAspectRatio = (float)(settings.numberOfCells.y) / settings.numberOfCells.x;
			float aspectRatio = dimAspectRatio / bufferAspectRatio;
			float aspectRatioX = aspectRatio < 1 ? aspectRatio : 1;
			float aspectRatioY = aspectRatio > 1 ? (1.0f / aspectRatio) : 1;
			Vector4 aspectScale = new Vector4(1.0f / aspectRatioX, 1.0f / aspectRatioY, 1.0f / aspectRatioX, 1.0f / aspectRatioY);

			if (dimensionMode == DimensionMode.Bounds)
			{
				Vector2 cellSizeFromDimension = dimension / settings.numberOfCells;
				cellWorldSize = Mathf.Max(cellSizeFromDimension.x, cellSizeFromDimension.y);
				cellSize = new Vector4(settings.cellSize, settings.cellSize, settings.secondLayerCellSize, settings.secondLayerCellSize) * cellWorldSize;
			}
			else
			{
				cellSize = new Vector4(settings.cellSize, settings.cellSize, settings.secondLayerCellSize, settings.secondLayerCellSize) * cellWorldSize;
			}
			cellSize.Scale(aspectScale);
			cellSize = Vector4.Max(kMinCellSize, cellSize);
			return cellSize;
		}

		public static int CalculateRequiredIterationCount(float x)
		{
			if (x >= 0.078125 && x < 0.1171875)
				return 4;
			else if (x >= 0.1171875 && x < 0.1567188)
				return 3;
			else if (x >= 0.1567188 && x < 0.2734375)
				return 2;
			else if (x >= 0.2734375)
				return 1;
			else
				return 0; // If x is less than 0.078125
		}
	}
}