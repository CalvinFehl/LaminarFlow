using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;
using static FluidFrenzy.FluidSimulation;

namespace FluidFrenzy
{
	/// <summary>
	/// The <see cref="TerraformLayer"/> is an advanced extension of the <see cref="ErosionLayer"/> that enables dynamic **terrain synthesis and multi-fluid interaction** (e.g., "God game" mechanics) by simulating complex layer transformations.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The <c>Terraform Layer</c> extends the base erosion system by adding highly customizable rules for material transformation between fluid and terrain layers. This facilitates complex real-time behaviors like fluid mixing, liquefaction, and contact-based reactions that modify both the terrain heightmap and splatmap.
	/// </para>
	/// 
	/// <h4>Liquefaction (Solid Terrain to Fluid)</h4>
	/// <para>
	/// This feature allows a terrain layer (e.g., snow or ice) to dissolve into a selected fluid layer (e.g., water) over time. You can configure the <c>Liquify Rate</c> and the <c>Liquify Amount</c> (conversion ratio of terrain height to fluid depth) independently for each terrain layer.
	/// </para>
	/// 
	/// <h4>Fluid Contact Reactions (Fluid and Terrain)</h4>
	/// <para>
	/// This system allows each terrain layer to react specifically when it comes into contact with any of the fluid layers. Reactions can be configured to:
	/// <list type="bullet">
	/// 	<item><term>Dissolve</term><description>Consume the terrain and/or the fluid over time.</description></item>
	/// 	<item><term>Convert</term><description>Change the terrain into a new terrain layer (with a new splat channel) or convert the terrain into a different fluid layer (e.g., snow reacting to <c>Lava</c> to create <c>Water</c>).</description></item>
	/// 	<item><term>Volume Control</term><description>Adjust the <c>Terrain Volume</c> and <c>Fluid Volume</c> multipliers to simulate material expansion or compression during the transformation.</description></item>
	/// </list>
	/// </para>
	/// 
	/// <h4>Fluid Mixing (Fluid + Fluid to Solid Terrain)</h4>
	/// <para>
	/// When two different fluid layers occupy the same cell, this system can be triggered to simulate a reaction (e.g., water and lava mixing to cool and solidify into rock).
	/// <list type="bullet">
	/// 	<item><term>Solidification</term><description>The mixing fluids are consumed and deposited as a new, solid terrain layer onto the heightmap and splatmap.</description></item>
	/// 	<item><term>Particle Emission</term><description>The system can emit visual particles (e.g., steam) upon mixing, with customizable settings for <c>Emission Rate</c>, color, and lifetime.</description></item>
	/// </list>
	/// </para>
	/// 
	/// <para>
	/// <c>Setup Note:</c> While this component must be attached to a GameObject, it also requires registration in the <see cref="FluidSimulation.extensionLayers"/> list to function correctly.
	/// </para>
	/// </remarks>
	[HelpURL("https://frenzybyte.github.io/fluidfrenzy/docs/fluid_simulation_components/#terraform-layer")]
	public partial class TerraformLayer : ErosionLayer
	{
		/// <summary>
		/// Toggles the interaction logic between overlapping fluid types.
		/// </summary>
		/// <remarks>
		/// When enabled, if two distinct fluids (e.g., Water and Lava) occupy the same grid cell, they will trigger a mixing event. 
		/// In standard configurations, this results in the fluid volume being consumed and converted into solid terrain geometry.
		/// </remarks>
		public bool fluidMixing = true;

		/// <summary>
		/// Controls the speed of the reaction between interacting fluids.
		/// </summary>
		/// <remarks>
		/// Higher values cause the fluids to consume each other and generate terrain more rapidly.
		/// </remarks>
		public float fluidMixRate = 1;

		/// <summary>
		/// The volumetric conversion ratio between consumed fluid and generated terrain.
		/// </summary>
		/// <remarks>
		/// This value determines how much solid ground is created for every unit of fluid lost during mixing.
		/// <list type="bullet">
		/// 	<item>
		/// 		<term>1.0</term>
		/// 		<description>One unit of fluid volume converts exactly to one unit of terrain volume.</description>
		/// 	</item>
		/// 	<item>
		/// 		<term>Greater than 1.0</term>
		/// 		<description>The reaction expands, creating more terrain than the fluid consumed.</description>
		/// 	</item>
		/// 	<item>
		/// 		<term>Less than 1.0</term>
		/// 		<description>The reaction contracts, creating less terrain than the fluid consumed.</description>
		/// 	</item>
		/// </list>
		/// </remarks>
		public float fluidMixScale = 1;

		/// <summary>
		/// The rate at which the newly solidified material is integrated into the terrain's heightmap.
		/// </summary>
		/// <remarks>
		/// While <see cref="fluidMixRate"/> controls the fluid consumption, this controls the visual rise of the ground. 
		/// Lower values can smooth out the generation process, preventing abrupt spikes in the terrain mesh.
		/// </remarks>
		public float depositRate = 3.0f;

		/// <summary>
		/// Specifies which vertical terrain layer (e.g., Bedrock or Sediment) receives the newly generated geometry.
		/// </summary>
		public TerrainLayerMask depositTerrainLayers = TerrainLayerMask.Layer1;

		/// <summary>
		/// Specifies the material channel (R, G, B, or A) in the splatmap to apply to the newly generated terrain.
		/// </summary>
		/// <remarks>
		/// This ensures that the new ground visually matches the expected material (e.g., setting the channel to display "Obsidian" or "Rock" texture where lava hardened).
		/// </remarks>
		public SplatChannel depositTerrainSplat = SplatChannel.R;

		/// <summary>
		/// Configuration for the particle system emitted during fluid mixing events.
		/// </summary>
		/// <remarks>
		/// This is commonly used to create steam or smoke effects when hot fluids interact with cool fluids (e.g., Lava meeting Water).
		/// </remarks>
		public FluidParticleSystem fluidParticles;

		/// <summary>
		/// The time interval, in seconds, between consecutive particle spawn events at a mixing location.
		/// </summary>
		/// <remarks>
		/// A lower value results in a higher frequency of particle emission (more particles), while a higher value results in sparse emission.
		/// </remarks>
		public float emissionRate = 1;

		private Material m_terraformMaterial = null;
		private int m_terraformFluidMixPass = 0;

		private Material m_terraformModifierMaterial = null;
		private int m_terraformModifierCirclePass = 0;
		private int m_terraformModifierSquarePass = 0;
		private int m_terraformModifierSpherePass = 0;
		private int m_terraformModifierCubePass = 0;
		private int m_terraformModifierCylinderPass = 0;
		private int m_terraformModifierCapsulePass = 0;

		private RenderTexture m_modifyLayer0 = null;
		private RenderTexture m_modifyLayer1 = null;

		private RenderTexture m_splatmap0 = null;
		private RenderTexture m_splatmap1 = null;

		protected RenderTargetIdentifier[] m_mrt3 = null;
		protected RenderTargetIdentifier[] m_mrt4 = null;


		Vector4[] m_liquifyMask = new Vector4[4];
		Vector4[] m_reactionFactors_F1 = new Vector4[4];
		Vector4[] m_reactionFactors_F2 = new Vector4[4];

		Vector4[] m_addTerrainMask_F1 = new Vector4[4];
		Vector4[] m_addTerrainMask_F2 = new Vector4[4];

		Vector4[] m_addFluidmask_F1 = new Vector4[4];
		Vector4[] m_addFluidmask_F2 = new Vector4[4];

		Vector4[] m_splatMask_Fluid1 = new Vector4[4];
		Vector4[] m_splatMask_Fluid2 = new Vector4[4];

		private bool m_supportsParticles = true;
		public override void Awake()
		{
			base.Awake();
			m_terraformMaterial = new Material(Shader.Find("Hidden/FluidFrenzy/TerraForm"));
			m_terraformFluidMixPass = m_terraformMaterial.FindPass("FluidMix");

			m_terraformModifierMaterial = new Material(Shader.Find("Hidden/FluidFrenzy/TerraformModifier"));
			m_terraformModifierCirclePass = m_terraformModifierMaterial.FindPass("TerraformCircle");
			m_terraformModifierSquarePass = m_terraformModifierMaterial.FindPass("TerraformBox");
			m_terraformModifierSpherePass = m_terraformModifierMaterial.FindPass("TerraformSphere");
			m_terraformModifierCubePass = m_terraformModifierMaterial.FindPass("TerraformCube");
			m_terraformModifierCylinderPass = m_terraformModifierMaterial.FindPass("TerraformCylinder");
			m_terraformModifierCapsulePass = m_terraformModifierMaterial.FindPass("TerraformCapsule");

			m_mrt3 = new RenderTargetIdentifier[3];
			m_mrt4 = new RenderTargetIdentifier[4];
			m_supportsParticles = Application.platform != RuntimePlatform.WebGLPlayer && SystemInfo.supportedRandomWriteTargetCount > 2;
		}

		void OnValidate()
		{
			if (version < 2)
			{
#pragma warning disable CS0612 // Rethrow to preserve stack details
#if UNITY_EDITOR
				UnityEditor.Undo.RecordObject(this, "Update TerraformLayer");
				layerSettings.Clear();

				TerraformSettings layer0Settings = new TerraformSettings()
				{
					slippage = false,
					hydraulicErosion = false,
				};

				layer0Settings.fluidLayer2Contact = new FluidContactReaction()
				{
					enabled = true,
					terrainDissolveAmount = 0,
					fluidConsumptionAmount = 0,
					convertToSplatChannel = SplatChannel.R
				};
				layerSettings.Add(layer0Settings);

				TerraformSettings newSettings = new TerraformSettings()
				{
					slippage = this.slippage,
					slippageAngle = this.slippageAngle,
					slopeSmoothness = this.slopeSmoothness,
					hydraulicErosion = this.hydraulicErosion,
					maxSediment = this.maxSediment,
					sedimentDissolveRate = this.sedimentDissolveRate,
					sedimentDepositRate = this.sedimentDepositRate,
				};
				newSettings.fluidLayer2Contact = new FluidContactReaction()
				{
					enabled = true,
					terrainDissolveAmount = 1,
					fluidConsumptionAmount = 0,
					convertToTerrainLayer = TerrainLayerMask.Layer1,
					convertToTerrainVolume = 1.0f,
					convertToSplatChannel = SplatChannel.R
				};

				layerSettings.Add(newSettings);
				minTiltAngle = minTiltAngle * 90;
				version = Version;
#endif
#pragma warning restore CS0618 // Rethrow to preserve stack details
			}
		}

		public override void ResetLayer(FluidSimulation simulation)
		{
			if (!m_terrainSupportsTerraform)
			{
				return;
			}
			simulation.BlitRenderDataToSimulation(m_commandBuffer, (m_parentSimulation.simpleTerrain as TerraformTerrain).splatmap, m_splatmap0);
			simulation.BlitRenderDataToSimulation(m_commandBuffer, (m_parentSimulation.simpleTerrain as TerraformTerrain).splatmap, m_splatmap1);
			base.ResetLayer(simulation);
		}

		public override void OnDestroy()
		{
			GraphicsHelpers.ReleaseSimulationRT(m_modifyLayer0);
			GraphicsHelpers.ReleaseSimulationRT(m_modifyLayer1);
			GraphicsHelpers.ReleaseSimulationRT(m_splatmap0);
			GraphicsHelpers.ReleaseSimulationRT(m_splatmap1);

			Destroy(m_terraformMaterial);

			if (m_supportsParticles)
			{ 
				fluidParticles.Destroy();
			}
			base.OnDestroy();
		}

		public override void CopyFrom(FluidLayer source)
		{
			base.CopyFrom(source);

			TerraformLayer sourceLayer = source as TerraformLayer;
			fluidMixing = sourceLayer.fluidMixing;
			fluidMixRate = sourceLayer.fluidMixRate;
			fluidMixScale = sourceLayer.fluidMixRate;
			depositRate = sourceLayer.depositRate;
			fluidParticles = sourceLayer.fluidParticles;
			emissionRate = sourceLayer.emissionRate;
		}
		public override void Init(FluidSimulation simulation)
		{
			base.Init(simulation);

			Vector2Int numSimulationCells = simulation.numSimulationCells;

			m_modifyLayer0 = GraphicsHelpers.CreateSimulationRT(numSimulationCells.x, numSimulationCells.y, RenderTextureFormat.RGHalf, true, name: "LayerModify_0");
			m_modifyLayer1 = GraphicsHelpers.CreateSimulationRT(numSimulationCells.x, numSimulationCells.y, RenderTextureFormat.RGHalf, true, name: "LayerModify_1");

			if (m_terrainSupportsTerraform)
			{
				m_splatmap0 = GraphicsHelpers.CreateSimulationRT(numSimulationCells.x, numSimulationCells.y, RenderTextureFormat.ARGB32, true, name: "Splatmap_0");
				m_splatmap1 = GraphicsHelpers.CreateSimulationRT(numSimulationCells.x, numSimulationCells.y, RenderTextureFormat.ARGB32, true, name: "Splatmap_1");
			}
			else
			{
				m_terraformMaterial.EnableKeyword("_FLUID_UNITY_TERRAIN");
			}

			if (m_supportsParticles)
			{
				fluidParticles.Init(FluidParticleSystem.UpdateMode.Generic, simulation);
			}
		}

		public void LateUpdate()
		{
			if (m_supportsParticles)
			{
				fluidParticles.Render();
			}
		}

		protected override void Process(FluidSimulation simulation, float deltaTime)
		{
			base.Process(simulation, deltaTime);
			UpdateTerraforming(simulation, deltaTime);
		}

		private void UpdateTerraforming(FluidSimulation simulation, float deltaTime)
		{
			using (new ProfilingScope(m_commandBuffer, ProfilingSampler.Get(WaterSimProfileID.Terraforming)))
			{
				UpdateTerraformModifier(simulation, deltaTime);

				bool runTerraform = simulation.multiLayeredFluid && fluidMixing;
				bool runMix = runTerraform;
				bool runLiquify = false;
				bool runContact = false;
				for (int i = 0; i < layerSettings.Count; i++)
				{
					m_splatMask_Fluid1[i] = m_addFluidmask_F1[i] = m_addTerrainMask_F1[i] = m_reactionFactors_F1[i] = Vector4.zero;
					m_splatMask_Fluid2[i] = m_addFluidmask_F2[i] = m_addTerrainMask_F2[i] = m_reactionFactors_F2[i] = Vector4.zero;

					var setting = layerSettings[i] as TerraformSettings;
					if (setting.liquify)
					{
						runTerraform = true;
						runLiquify = true;
						m_liquifyMask[i] = new Vector4(setting.liquifyLayer == FluidLayerIndex.Layer1 ? 1.0f : 0.0f,
														setting.liquifyLayer == FluidLayerIndex.Layer2 ? 1.0f : 0.0f,
														setting.liquifyRate * deltaTime,
														setting.liquifyAmount);
					}

					FluidContactReaction fluid1Reaction = setting.fluidLayer1Contact;
					if (fluid1Reaction.enabled)
					{
						runTerraform = true;
						runContact = true;
						m_addTerrainMask_F1[i] = new Vector4(fluid1Reaction.ConvertsToTerrain(TerrainLayerMask.Layer1),
																	fluid1Reaction.ConvertsToTerrain(TerrainLayerMask.Layer2),
																	fluid1Reaction.ConvertsToTerrain(TerrainLayerMask.Layer3),
																	fluid1Reaction.ConvertsToTerrain(TerrainLayerMask.Layer4));

						m_addTerrainMask_F1[i] /= Mathf.Max(1, Vector4.Dot(m_addTerrainMask_F1[i], Vector4.one));
						m_addTerrainMask_F1[i] *= fluid1Reaction.convertToTerrainVolume;

						m_reactionFactors_F1[i].x = fluid1Reaction.conversionRate * deltaTime;
						m_reactionFactors_F1[i].y = fluid1Reaction.terrainDissolveAmount;
						m_reactionFactors_F1[i].z = fluid1Reaction.fluidConsumptionAmount;

						m_addFluidmask_F1[i] = new Vector4((fluid1Reaction.convertToFluidLayer == FluidLayerIndex.Layer1) ? 1f : 0f,
																		(fluid1Reaction.convertToFluidLayer == FluidLayerIndex.Layer2) ? 1f : 0f,
																		0,
																		0) * fluid1Reaction.convertToFluidVolume;

						m_splatMask_Fluid1[i] = fluid1Reaction.ConvertsToSplatMask();
					}

					FluidContactReaction fluid2Reaction = setting.fluidLayer2Contact;
					if (fluid2Reaction.enabled)
					{
						runTerraform = true;
						runContact = true;
						m_addTerrainMask_F2[i] = new Vector4(fluid2Reaction.ConvertsToTerrain(TerrainLayerMask.Layer1),
																	fluid2Reaction.ConvertsToTerrain(TerrainLayerMask.Layer2),
																	fluid2Reaction.ConvertsToTerrain(TerrainLayerMask.Layer3),
																	fluid2Reaction.ConvertsToTerrain(TerrainLayerMask.Layer4));
						m_addTerrainMask_F2[i] /= Mathf.Max(1, Vector4.Dot(m_addTerrainMask_F2[i], Vector4.one));
						m_addTerrainMask_F2[i] *= fluid2Reaction.convertToTerrainVolume;

						m_reactionFactors_F2[i].x = fluid2Reaction.conversionRate * deltaTime;
						m_reactionFactors_F2[i].y = fluid2Reaction.terrainDissolveAmount;
						m_reactionFactors_F2[i].z = fluid2Reaction.fluidConsumptionAmount;

						m_addFluidmask_F2[i] = new Vector4((fluid2Reaction.convertToFluidLayer == FluidLayerIndex.Layer1) ? 1f : 0f,
																		(fluid2Reaction.convertToFluidLayer == FluidLayerIndex.Layer2) ? 1f : 0f,
																		0,
																		0) * fluid2Reaction.convertToFluidVolume;

						m_splatMask_Fluid2[i] = fluid2Reaction.ConvertsToSplatMask();
					}
				}

				if (runTerraform)
				{
					Shader shader = m_terraformMaterial.shader;
					LocalKeyword mixKeyword = new LocalKeyword(shader, "_FLUID_MIX");
					LocalKeyword liquifyKeyword = new LocalKeyword(shader, "_FLUID_LIQUIFY");
					LocalKeyword contactKeyword = new LocalKeyword(shader, "_FLUID_CONTACT");
					m_terraformMaterial.SetKeyword(mixKeyword, runMix);
					m_terraformMaterial.SetKeyword(liquifyKeyword, runLiquify);
					m_terraformMaterial.SetKeyword(contactKeyword, runContact);

					if (m_supportsParticles)
					{
						using (new ProfilingScope(m_commandBuffer, ProfilingSampler.Get(WaterSimProfileID.Terraforming_Particles)))
						{
							fluidParticles.Process(m_commandBuffer, deltaTime);
						}
					}

					m_internalPropertyBlock.Clear();
					m_internalPropertyBlock.SetVector(FluidShaderProperties._BlitScaleBias, Vector2.one);
					m_internalPropertyBlock.SetVector(FluidShaderProperties._BlitScaleBiasRt, Vector2.one);
					m_internalPropertyBlock.SetTexture(FluidShaderProperties._FluidHeightField, simulation.fluidHeight);

					m_internalPropertyBlock.SetTexture(FluidShaderProperties._LayerModify, m_modifyLayer0);
					if (m_terrainSupportsTerraform)
					{
						m_internalPropertyBlock.SetTexture(FluidShaderProperties._TerrainHeightField, m_activeTerrainheight);
						m_internalPropertyBlock.SetTexture(FluidShaderProperties._Splatmap, m_splatmap0);
					}
					else
					{
						m_internalPropertyBlock.SetTexture(FluidShaderProperties._TerrainHeightField, simulation.terrainHeight);
					}

					Vector4 depositSplatMask = SplatChannelToMask(depositTerrainSplat);
					m_internalPropertyBlock.SetVector(FluidShaderProperties._TerrainDepositSplatMask, depositSplatMask);
					m_internalPropertyBlock.SetFloat(FluidShaderProperties._MixRate, fluidMixRate * deltaTime);
					m_internalPropertyBlock.SetFloat(FluidShaderProperties._MixScale, fluidMixScale);
					m_internalPropertyBlock.SetFloat(FluidShaderProperties._MixDepositRate, depositRate * deltaTime);

					m_internalPropertyBlock.SetVectorArray(FluidShaderProperties._LiquifyLayerMask, m_liquifyMask);
					m_internalPropertyBlock.SetVectorArray(FluidShaderProperties._ReactionFactors_F1, m_reactionFactors_F1);
					m_internalPropertyBlock.SetVectorArray(FluidShaderProperties._ReactionFactors_F2, m_reactionFactors_F2);		
					
					m_internalPropertyBlock.SetVectorArray(FluidShaderProperties._AddTerrainMask_F1, m_addTerrainMask_F1);
					m_internalPropertyBlock.SetVectorArray(FluidShaderProperties._AddTerrainMask_F2, m_addTerrainMask_F2);	
					
					m_internalPropertyBlock.SetVectorArray(FluidShaderProperties._AddFluidMask_F1, m_addFluidmask_F1);
					m_internalPropertyBlock.SetVectorArray(FluidShaderProperties._AddFluidMask_F2, m_addFluidmask_F2);		
					
					m_internalPropertyBlock.SetVectorArray(FluidShaderProperties._SetSplatMask_F1, m_splatMask_Fluid1);
					m_internalPropertyBlock.SetVectorArray(FluidShaderProperties._SetSplatMask_F2, m_splatMask_Fluid2);

					if (m_supportsParticles)
					{
						m_internalPropertyBlock.SetConstantBuffer("ParticleEmitter0", fluidParticles.emitterDescBuffer, 0, fluidParticles.emitterDescBuffer.stride);
						m_internalPropertyBlock.SetFloat(FluidShaderProperties._ParticleEmissionRate, emissionRate);
						m_commandBuffer.SetRandomWriteTarget(4, fluidParticles.freeParticleIndices);
						m_commandBuffer.SetRandomWriteTarget(5, fluidParticles.particleBuffer);
					}
					if (m_terrainSupportsTerraform)
					{
						m_mrt4[0] = simulation.nextFluidHeight;
						m_mrt4[1] = m_modifyLayer1;
						m_mrt4[2] = m_nextTerrainheight;
						m_mrt4[3] = m_splatmap1;
						FluidSimulation.BlitQuad(m_commandBuffer, null, m_mrt4, m_modifyLayer1.depthBuffer, m_terraformMaterial, m_internalPropertyBlock, m_terraformFluidMixPass);
						SwapTerrainRT();
					}
					else
					{
						// Mixing and particles only
						m_mrt[0] = simulation.nextFluidHeight;
						m_mrt[1] = m_modifyLayer1;
						FluidSimulation.BlitQuad(m_commandBuffer, null, m_mrt, m_modifyLayer1.depthBuffer, m_terraformMaterial, m_internalPropertyBlock, m_terraformFluidMixPass);
					}
					m_commandBuffer.ClearRandomWriteTargets();
					simulation.SwapFluidRT();

					FluidSimulation.Swap(ref m_modifyLayer0, ref m_modifyLayer1);

					Vector4 depositLayerMask = TerrainLayerMaskToMask(depositTerrainLayers);
					m_internalPropertyBlock.Clear();
					m_internalPropertyBlock.SetVector(FluidShaderProperties._BlitScaleBias, Vector2.one);
					m_internalPropertyBlock.SetVector(FluidShaderProperties._BlitScaleBiasRt, Vector2.one);
					m_internalPropertyBlock.SetVector(FluidShaderProperties._TerrainDepositMask, depositLayerMask / Vector4.Dot(depositLayerMask, Vector4.one));
					m_internalPropertyBlock.SetTexture(FluidShaderProperties._LayerModify, m_modifyLayer0);
					m_internalPropertyBlock.SetFloat(FluidShaderProperties._MixDepositRate, depositRate * deltaTime);
					if (m_terrainSupportsTerraform && runMix)
					{
						m_internalPropertyBlock.SetTexture(FluidShaderProperties._TerrainHeightField, m_activeTerrainheight);

						m_mrt[0] = m_modifyLayer1;
						m_mrt[1] = m_nextTerrainheight;
						FluidSimulation.BlitQuad(m_commandBuffer, null, m_mrt, m_modifyLayer1.depthBuffer, m_terraformMaterial, m_internalPropertyBlock, 1);


						SwapTerrainRT();
						FluidSimulation.Swap(ref m_splatmap0, ref m_splatmap1);
						simulation.BlitSimulationToRenderData(m_commandBuffer, m_splatmap0, (m_parentSimulation.simpleTerrain as TerraformTerrain).renderSplatmap);
					}
					else
					{
						// Mixing and particles only
						m_internalPropertyBlock.SetTexture(FluidShaderProperties._TerrainHeightField, simulation.terrainHeight);
						FluidSimulation.BlitQuad(m_commandBuffer, null, m_modifyLayer1, m_terraformMaterial, m_internalPropertyBlock, 1);
					}

					FluidSimulation.Swap(ref m_modifyLayer0, ref m_modifyLayer1);
				}
			}
		}
	}
}