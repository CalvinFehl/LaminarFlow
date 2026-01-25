using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace FluidFrenzy
{
	/// <summary>
	/// <see cref="FoamLayer"/> is an <see cref="FluidLayer">extension layer</see> that can be attached to the <see cref="FluidSimulation"/>. 
	/// It generates a foam map based on the current state of the <see cref="FluidSimulation"/>. 
	/// There are several inputs from the <see cref="FluidSimulation"/> that are used to generate this map (Pressure, Y Velocity, and Slope). 
	/// The influence of each of these inputs can be controlled by the <see cref="FoamLayerSettings"/>. 
	/// Note: This component lives on a GameObject but also needs to be added to the Layers list of the Fluid Simulation.
	/// </summary>
	[HelpURL("https://frenzybyte.github.io/fluidfrenzy/docs/fluid_simulation_components/#foam-layer")]
	public class FoamLayer : FluidLayer
	{
		[SerializeField]
		internal int version = 1;
		public override bool copyNeighbours { get { return true; } }

		/// <summary>
		/// The <see cref="FoamLayerSettings">settings</see> that this <see cref="FoamLayer"/> will use to generate it's foam mask.
		/// </summary>
		public FoamLayerSettings settings;

		private MaterialPropertyBlock m_internalPropertyBlock = null;
		private MaterialPropertyBlock m_externalPropertyBlock = null;

		private Material m_updateFoamMaterial = null;
		private Material m_addFoamMaterial = null;

		private CommandBuffer m_commandBuffer;

		public override void Awake()
		{
			m_updateFoamMaterial = new Material(Shader.Find("Hidden/FluidFrenzy/UpdateFoam"));
			m_addFoamMaterial = new Material(Shader.Find("Hidden/FluidFrenzy/AddFoam"));

			m_commandBuffer = new CommandBuffer();
			m_commandBuffer.name = "FoamAdvectionLayer";

			m_internalPropertyBlock = new MaterialPropertyBlock();
			m_externalPropertyBlock = new MaterialPropertyBlock();
		}

		public override void CopyFrom(FluidLayer source)
		{
			FoamLayer sourceLayer = source as FoamLayer;
			settings = sourceLayer.settings;
		}

		public override void Init(FluidSimulation simulation)
		{
			base.Init(simulation);

			int velocityGhostCells2 = simulation.velocityGhostCells2;
			int velocityGhostCells = simulation.velocityGhostCells;

			if(simulation.simulationType == FluidSimulation.FluidSimulationType.Flux)
			{
				float scaleX = (float)settings.textureSize.x / simulation.velocityTextureSize.x;
				float scaleY = (float)settings.textureSize.x / simulation.velocityTextureSize.y;

				velocityGhostCells2 = (int)(float)(velocityGhostCells2 * scaleX);
				velocityGhostCells = (int)(float)(velocityGhostCells * scaleX);
			}

			int foamWidth = (settings.textureSize.x + 1) + velocityGhostCells2;
			int foamHeight = (settings.textureSize.y + 1) + velocityGhostCells2;
#if UNITY_2023_2_OR_NEWER
			GraphicsFormat foamFormat = SystemInfo.IsFormatSupported(GraphicsFormat.R16_UNorm, GraphicsFormatUsage.Render | GraphicsFormatUsage.Sample) ? GraphicsFormat.R16_UNorm : GraphicsFormat.R16_SFloat;
#else
			GraphicsFormat foamFormat = SystemInfo.IsFormatSupported(GraphicsFormat.R16_UNorm, FormatUsage.Render | FormatUsage.Sample) ? GraphicsFormat.R16_UNorm : GraphicsFormat.R16_SFloat;
#endif
			if (Application.platform == RuntimePlatform.WebGLPlayer)
            {
				foamFormat = GraphicsFormat.R16G16_SFloat;
			}

			activeLayer = GraphicsHelpers.CreateSimulationRT(foamWidth, foamHeight, foamFormat, true, name: "Foam_1");
			nextLayer = GraphicsHelpers.CreateSimulationRT(foamWidth, foamHeight, foamFormat, true, name: "Foam_2");

			//Vector2 foamTexelSize = new Vector2((float)(velocityGhostCells) / (settings.textureSize.x + velocityGhostCells2),
			//								(float)(velocityGhostCells) / (settings.textureSize.y + velocityGhostCells2));
			//textureST = new Vector4(1.0f - foamTexelSize.x * 2,
			//						1.0f - foamTexelSize.y * 2,
			//						foamTexelSize.x,
			//						foamTexelSize.y);

			Vector2 foamTexelSize = new Vector2(1.0f / foamWidth,
											1.0f / foamHeight);
			textureST = new Vector4(1.0f - foamTexelSize.x * velocityGhostCells2,
									1.0f - foamTexelSize.y * velocityGhostCells2,
									foamTexelSize.x * velocityGhostCells,
									foamTexelSize.y * velocityGhostCells);
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

			Destroy(m_updateFoamMaterial);
			Destroy(m_addFoamMaterial);
		}

		public override void Step(FluidSimulation simulation, float deltaTime, int numSteps)
		{
			UpdateFoam(simulation, deltaTime, numSteps);
		}

		private void UpdateFoam(FluidSimulation simulation, float dt, int numSteps)
		{
			UpdateFoamModifiers(dt);
			using (new ProfilingScope(m_commandBuffer, ProfilingSampler.Get(WaterSimProfileID.Foam)))
			{
				int velocityGhostCells2 = simulation.velocityGhostCells2;
				int velocityGhostCells = simulation.velocityGhostCells;

				if (simulation.simulationType == FluidSimulation.FluidSimulationType.Flux)
				{
					float scaleX = (float)settings.textureSize.x / simulation.velocityTextureSize.x;
					float scaleY = (float)settings.textureSize.x / simulation.velocityTextureSize.y;

					velocityGhostCells2 = (int)(float)(velocityGhostCells2 * scaleX);
					velocityGhostCells = (int)(float)(velocityGhostCells * scaleX);
				}

				bool isFluxSimulation = simulation is FluxFluidSimulation;
				if(settings.applyPressureFoam && simulation as FluxFluidSimulation)
				{
					Texture pressureTexture = (simulation as FluxFluidSimulation)?.pressureTexture;
					m_internalPropertyBlock.SetVector(FluidShaderProperties._FoamPressureSmoothStep, settings.pressureRange);
					m_internalPropertyBlock.SetTexture(FluidShaderProperties._PressureField, pressureTexture);
					m_updateFoamMaterial.EnableKeyword("_APPLY_PRESSURE_FOAM");
				}
				else
				{
					m_updateFoamMaterial.DisableKeyword("_APPLY_PRESSURE_FOAM");
				}

				if (settings.applyWaveFoam)
				{
					m_internalPropertyBlock.SetVector(FluidShaderProperties._FoamWaveSmoothStep, Vector2.one - settings.waveAngleRange);
					m_updateFoamMaterial.EnableKeyword("_APPLY_WAVE_FOAM");
				}
				else
				{
					m_updateFoamMaterial.DisableKeyword("_APPLY_WAVE_FOAM");
				}

				if (settings.applyShallowFoam)
				{
					m_updateFoamMaterial.EnableKeyword("_APPLY_SHALLOW_FOAM");
					m_internalPropertyBlock.SetVector(FluidShaderProperties._FoamShallowVelocitySmoothStep, settings.shallowVelocityRange);
					m_internalPropertyBlock.SetFloat(FluidShaderProperties._FoamShallowDepth, settings.shallowDepth);


				}
				else
				{
					m_updateFoamMaterial.DisableKeyword("_APPLY_SHALLOW_FOAM");
				}

				if (settings.applyTurbulenceFoam)
				{
					m_updateFoamMaterial.EnableKeyword("_APPLY_TURBULENCE_FOAM");
					m_internalPropertyBlock.SetFloat(FluidShaderProperties._FoamTurbulenceAmount, settings.turbulenceFoamAmount * dt);
					m_internalPropertyBlock.SetFloat(FluidShaderProperties._FoamDivergenceThreshold, settings.turbulenceFoamThreshold);
				}
				else
				{
					m_updateFoamMaterial.DisableKeyword("_APPLY_TURBULENCE_FOAM");
				}

				m_internalPropertyBlock.SetTexture(FluidShaderProperties._WorldNormal, simulation.normalTexture);
				m_internalPropertyBlock.SetTexture(FluidShaderProperties._VelocityField, simulation.velocityTexture);
				m_internalPropertyBlock.SetTexture(FluidShaderProperties._FluidHeightField, simulation.fluidRenderData);
				m_internalPropertyBlock.SetTexture(FluidShaderProperties._PreviousFluidHeightField, simulation.prevFluidRenderData);

				m_internalPropertyBlock.SetVector(FluidShaderProperties._FoamValues, new Vector4(settings.pressureIncreaseAmount,
																				settings.waveAngleIncreaseAmount,
																				settings.waveHeightIncreaseAmount / numSteps,
																				settings.shallowFoamAmount) * dt);

				m_internalPropertyBlock.SetVector(FluidShaderProperties._FoamFadeValues, new Vector4(1.0f - (settings.exponentialDecayRate * dt),
																									settings.linearDecayRate * dt, 0, 0));
				Vector2 advectScale;
				if (isFluxSimulation)
					advectScale = (Vector2.one * simulation.advectionScale / new Vector2(activeLayer.width, activeLayer.height) / simulation.GetCellSize()) * dt;
				else
					advectScale = ((Vector2.one * simulation.advectionScale / new Vector2(activeLayer.width, activeLayer.height)) / simulation.GetCellSize()) * dt;

				m_internalPropertyBlock.SetVector(FluidShaderProperties._AdvectScale, advectScale);
				m_internalPropertyBlock.SetVector(FluidShaderProperties._VelocityScale, new Vector2(1.0f / simulation.velocityScale, 1.0f / simulation.velocityScale));

				float foamWidth = activeLayer.width - velocityGhostCells2;
				float foamHeight = activeLayer.height - velocityGhostCells2;
				Vector2 normalTexelSize = new Vector2(1.0f / foamWidth, 1.0f / foamHeight);
				Vector4 normalBlitScale = new Vector4(1 + normalTexelSize.x * (velocityGhostCells2),
														1 + normalTexelSize.y * (velocityGhostCells2),
														-normalTexelSize.x * (velocityGhostCells),
														-normalTexelSize.y * (velocityGhostCells));

				m_internalPropertyBlock.SetVector(FluidShaderProperties._VelocityBlitScaleBias, Vector2.one);
				m_internalPropertyBlock.SetVector(FluidShaderProperties._NormalBlitScaleBias, normalBlitScale);

				FluidSimulation.BlitQuad(m_commandBuffer, activeLayer, nextLayer, m_updateFoamMaterial, m_internalPropertyBlock, 0);
				SwapActiveLayer();
			}

			Graphics.ExecuteCommandBuffer(m_commandBuffer);
			m_commandBuffer.Clear();
		}

		public void AddFoam(Vector3 worldPos, Vector2 size, float strength, float exponent, float dt)
		{
			using (new ProfilingScope(m_commandBuffer, ProfilingSampler.Get(WaterSimProfileID.ExternalFoam)))
			{
				Vector2 position = m_parentSimulation.WorldSpaceToUVSpace(worldPos);
				float offset = m_parentSimulation.velocityGhostCells / (float)activeLayer.width;
				float scale = m_parentSimulation.velocityGhostCells2 / (float)activeLayer.width;
				position = position * (Vector2.one - Vector2.one * scale) + Vector2.one * offset;

				Vector2 uvSize = m_parentSimulation.WorldSizeToUVSize(size);

				m_externalPropertyBlock.Clear();
				m_externalPropertyBlock.SetVector(FluidShaderProperties._BlitScaleBiasRt, new Vector4(uvSize.x, uvSize.y, position.x, position.y));
				m_externalPropertyBlock.SetFloat(FluidShaderProperties._IncreaseStrength, strength * dt);
				m_externalPropertyBlock.SetFloat(FluidShaderProperties._IncreaseExponent, exponent);
				FluidSimulation.BlitQuad(m_commandBuffer, null, activeLayer, m_addFoamMaterial, m_externalPropertyBlock, 0);
			}
		}

		private void UpdateFoamModifiers(float dt)
		{
			foreach (FoamModifier input in FoamModifier.foamModifiers)
			{
				if (input.isActiveAndEnabled)
				{
					input.Process(this, dt);
				}
			}
		}

		public override RenderTexture GetDebugBuffer(FluidSimulation.DebugBuffer buffer)
		{
			switch (buffer)
			{
				case FluidSimulation.DebugBuffer.Foam:
					return activeLayer;
			}
			return null;
		}

		public override IEnumerable<FluidSimulation.DebugBuffer> EnumerateBuffers()
		{
			yield return FluidSimulation.DebugBuffer.Foam;
		}
	}
}