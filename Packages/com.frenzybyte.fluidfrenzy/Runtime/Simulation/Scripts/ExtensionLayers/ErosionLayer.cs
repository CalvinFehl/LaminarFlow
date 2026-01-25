using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace FluidFrenzy
{
	/// <summary>
	/// The <see cref="ErosionLayer"/> is an extension layer for the <see cref="FluidSimulation"/> that simulates physically-based <c>hydraulic erosion, slope-based slippage, and terrain modification</c> based on the fluid's state and terrain slope.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The <c>Erosion Layer</c> is a texture-based system that analyzes the fluid simulation's state (fluid height and velocity) and the terrain's geometry to dynamically modify the ground. Modifications are applied directly to the associated terrain input (e.g., <c>SimpleTerrain</c> or <c>TerraformTerrain</c>) and fed back into the simulation to affect fluid flow.
	/// </para>
	/// 
	/// <h4>Multi-Layer Terrain System</h4>
	/// <para>
	/// The erosion system allows you to define up to <c>four distinct terrain layers</c>, each with its own properties (e.g., hardness, color). The layers are structured from the bottom up:
	/// <list type="bullet">
	/// 	<item><term>Layer Structure</term><description>The first element (Layer 0) represents the bottom-most layer (e.g., bedrock), and each subsequent element represents the material stacked on top.</description></item>
	/// 	<item><term>User Clarity</term><description>For clarity in the editor, you can give each layer a custom name (e.g., "Rock," "Soil," or "Snow") to help keep track of what it represents.</description></item>
	/// </list>
	/// </para>
	/// <para>
	/// <h4>Terrain Modification Processes</h4>
	/// </para>
	/// <h5>1. Hydraulic Erosion (Water-Based)</h5>
	/// <para>
	/// This process converts terrain material into **sediment** using two-dimensional textures, driven by the fluid's velocity field.
	/// <list type="bullet">
	/// 	<item>
	/// 		<term>Erosion</term><description>Higher fluid velocity removes terrain material from the heightmap and transfers it into a Sediment Map.</description>
	/// 	</item>
	/// 	<item>
	/// 		<term>Transport</term><description>The Sediment Map is continuously updated and advected (moved) across the grid according to the fluid's velocity field.</description>
	/// 	</item>
	/// 	<item>
	/// 		<term>Deposition</term><description>Sediment is redeposited back onto the terrain heightmap in areas where the fluid flow is slower.</description>
	/// 	</item>
	/// </list>
	/// </para>
	/// 
	/// <h5>2. Slope-Based Slippage</h5>
	/// <para>
	/// This process simulates gravity-driven effects (like landslides and thermal erosion). Material is removed and shifted to lower areas wherever the terrain's slope angle exceeds a configurable threshold (the material's angle of repose), helping to smooth steep cliffs over time.
	/// </para>
	/// </remarks>
	[HelpURL("https://frenzybyte.github.io/fluidfrenzy/docs/fluid_simulation_components/#erosion-layer")]
	public partial class ErosionLayer : FluidLayer
	{
		protected static int Version = 2;
		[SerializeField]
		internal int version = Version;

		/// <summary>
		/// Identifies specific vertical layers within the terrain structure.
		/// </summary>
		public enum TerrainLayer { Layer1 = 0, Layer2 = 1, Layer3 = 2, Layer4 = 3 }

		/// <summary>
		/// A bitmask used to select multiple <see cref="TerrainLayer"/>s simultaneously.
		/// </summary>
		[Flags]
		public enum TerrainLayerMask { None, Layer1 = 1 << 0, Layer2 = 1 << 1, Layer3 = 1 << 2, Layer4 = 1 << 3 }

		/// <summary>
		/// Identifies a specific color channel in a texture or splatmap.
		/// </summary>
		public enum SplatChannel { R = 0, G = 1, B = 2, A = 3, None = 99 }

		/// <summary>
		/// Converts a <see cref="TerrainLayerMask"/> into a vector representation for shader operations.
		/// </summary>
		internal static Vector4 TerrainLayerMaskToMask(TerrainLayerMask mask)
		{
			return new Vector4((mask & TerrainLayerMask.Layer1) != 0 ? 1.0f : 0.0f,
								(mask & TerrainLayerMask.Layer2) != 0 ? 1.0f : 0.0f,
								(mask & TerrainLayerMask.Layer3) != 0 ? 1.0f : 0.0f,
								(mask & TerrainLayerMask.Layer4) != 0 ? 1.0f : 0.0f);
		}

		/// <summary>
		/// Converts a <see cref="SplatChannel"/> enum into a vector mask for shader operations.
		/// </summary>
		internal static Vector4 SplatChannelToMask(SplatChannel splat)
		{
			return new Vector4(splat == SplatChannel.R ? 1f : 0f, splat == SplatChannel.G ? 1f : 0f, splat == SplatChannel.B ? 1f : 0f, splat == SplatChannel.A ? 1f : 0f);
		}

		/// <summary>
		/// Contains configuration parameters for erosion behavior specific to a single terrain layer.
		/// </summary>
		[Serializable]
		public class ErosionSettings
		{
			/// <summary>
			/// The display name for this settings entry.
			/// </summary>
			public string name;

			/// <summary>
			/// Toggles material slippage.
			/// </summary>
			/// <remarks>
			/// When enabled, material on steep slopes will naturally slide down to lower areas, smoothing the terrain over time.
			/// </remarks>
			public bool slippage = true;

			/// <summary>
			/// The angle limit for slopes, in degrees.
			/// </summary>
			/// <remarks>
			/// Terrain slopes steeper than this angle will trigger slippage, causing material to slide down.
			/// </remarks>
			[Range(0, 90)]
			public float slippageAngle = 15;

			/// <summary>
			/// Controls the intensity of the slippage effect.
			/// </summary>
			/// <remarks>
			/// Higher values result in more aggressive smoothing of slopes that exceed the <see cref="slippageAngle"/>.
			/// </remarks>
			[Range(0, 10)]
			public float slopeSmoothness = 5;

			/// <summary>
			/// Toggles hydraulic erosion.
			/// </summary>
			/// <remarks>
			/// When enabled, moving fluid will wear down the terrain and transport sediment based on velocity and turbulence.
			/// </remarks>
			public bool hydraulicErosion = true;

			/// <summary>
			/// The maximum amount of sediment a fluid cell can carry.
			/// </summary>
			/// <remarks>
			/// Once the sediment carried by the fluid reaches this limit, no further erosion will occur in that cell until material is deposited elsewhere.
			/// </remarks>
			[Range(0, 5)]
			public float maxSediment = 1;

			/// <summary>
			/// The rate at which solid terrain is picked up by the fluid.
			/// </summary>
			/// <remarks>
			/// Higher values cause the terrain to erode faster, provided the fluid has not reached its <see cref="maxSediment"/> capacity.
			/// </remarks>
			[Range(0, 5)]
			public float sedimentDissolveRate = 1;

			/// <summary>
			/// The rate at which carried sediment settles back onto the terrain.
			/// </summary>
			/// <remarks>
			/// Deposition occurs when the fluid slows down or when the carried material exceeds the capacity defined by <see cref="maxSediment"/>.
			/// </remarks>
			[Range(0, 5)]
			public float sedimentDepositRate = 1;
		}

		// Obsolete fields - Comments matched to ErosionSettings for documentation consistency

		/// <summary>
		/// Toggles material slippage.
		/// </summary>
		/// <remarks>
		/// When enabled, material on steep slopes will naturally slide down to lower areas, smoothing the terrain over time.
		/// </remarks>
		[Obsolete] 
		public bool slippage = true;

		/// <summary>
		/// The angle limit for slopes, in degrees.
		/// </summary>
		/// <remarks>
		/// Terrain slopes steeper than this angle will trigger slippage, causing material to slide down.
		/// </remarks>
		[Obsolete] 
		public float slippageAngle = 15;

		/// <summary>
		/// Controls the intensity of the slippage effect.
		/// </summary>
		/// <remarks>
		/// Higher values result in more aggressive smoothing of slopes that exceed the <see cref="slippageAngle"/>.
		/// </remarks>
		[Obsolete] 
		public float slopeSmoothness = 5;

		/// <summary>
		/// Toggles hydraulic erosion.
		/// </summary>
		/// <remarks>
		/// When enabled, moving fluid will wear down the terrain and transport sediment based on velocity and turbulence.
		/// </remarks>
		[Obsolete] 
		public bool hydraulicErosion = true;

		/// <summary>
		/// The maximum amount of sediment a fluid cell can carry.
		/// </summary>
		/// <remarks>
		/// Once the sediment carried by the fluid reaches this limit, no further erosion will occur in that cell until material is deposited elsewhere.
		/// </remarks>
		[Obsolete] 
		public float maxSediment = 1;

		/// <summary>
		/// The rate at which solid terrain is picked up by the fluid.
		/// </summary>
		/// <remarks>
		/// Higher values cause the terrain to erode faster, provided the fluid has not reached its <see cref="maxSediment"/> capacity.
		/// </remarks>
		[Obsolete] 
		public float sedimentDissolveRate = 1;

		/// <summary>
		/// The rate at which carried sediment settles back onto the terrain.
		/// </summary>
		/// <remarks>
		/// Deposition occurs when the fluid slows down or when the carried material exceeds the capacity defined by <see cref="maxSediment"/>.
		/// </remarks>
		[Obsolete] 
		public float sedimentDepositRate = 1;

		/// <summary>
		/// Defines the minimum slope angle required for full hydraulic erosion efficiency.
		/// </summary>
		/// <remarks>
		/// This value modulates erosion based on the terrain's tilt.
		/// <list type="bullet">
		/// 	<item>
		/// 		<term>0 Degrees</term>
		/// 		<description>No restriction. Flat surfaces erode at the same rate as slopes.</description>
		/// 	</item>
		/// 	<item>
		/// 		<term>High Degrees</term>
		/// 		<description>Limits erosion primarily to steeper slopes, preserving flat areas.</description>
		/// 	</item>
		/// </list>
		/// </remarks>
		[Range(0, 90)]
		public float minTiltAngle = 0;

		/// <summary>
		/// The speed at which sediment moves with the fluid flow.
		/// </summary>
		/// <remarks>
		/// Higher values transport material further across the world before it deposits.
		/// <para>
		/// <b>Note:</b> Because the erosion simulation is not strictly mass-conserving, very high speeds may cause sediment to "vanish" if it moves into cells with no fluid or off the edge of the simulation grid.
		/// </para>
		/// </remarks>
		[Range(0, 10)]
		public float sedimentAdvectionSpeed = 0.01f;

		/// <summary>
		/// Toggles a higher-fidelity movement calculation for sediment.
		/// </summary>
		/// <remarks>
		/// When enabled, the simulation uses a more accurate method to move sediment. 
		/// This prevents sediment from artificially fading away due to calculation errors but increases the GPU performance cost.
		/// </remarks>
		public bool highPrecisionAdvection = true;

		/// <summary>
		/// A list of erosion configurations, allowing different physical properties to be applied to different terrain layers.
		/// </summary>
		[SerializeReference]
		public List<ErosionSettings> layerSettings = new List<ErosionSettings>();

		protected MaterialPropertyBlock m_internalPropertyBlock = null;
		protected MaterialPropertyBlock m_externalPropertyBlock = null;

		private Material m_erosionMaterial = null;
		private int m_erosionMaxSlippagePass;
		private int m_erosionErosionOutflowPass;
		private int m_erosionErosionApplyPass;
		private int m_erosionSedimentPass;
		private int m_erosionAdvectSedimentFastPass;
		private int m_erosionAdvectSedimentPass;
		private int m_erosionMacCormackPass;
		private int m_erosionCopyTerrainPass;
		private int m_erosionCombineTerrainPass;
		private int m_addTerrainDynamicPass;

		protected Material m_addTerrainMaterial = null;
		protected int m_addTerrainCirclePass = 0;
		protected int m_addSplatmapCirclePass = 0;
		protected int m_subSplatmapCirclePass = 0;
		protected int m_setSplatmapCirclePass = 0;

		protected int m_addTerrainSquarePass = 0;
		protected int m_addSplatmapSquarePass = 0;
		protected int m_subSplatmapSquarePass = 0;
		protected int m_setSplatmapSquarePass = 0;

		protected int m_addTerrainTexturePass = 0;
		protected int m_addSplatmapTexturePass = 0;
		protected int m_subSplatmapTexturePass = 0;
		protected int m_setSplatmapTexturePass = 0;

		protected int m_mixTerrainHeightCirclePass = 0;
		protected int m_mixTerrainHeightSquarePass = 0;
		protected int m_mixTerrainHeightTexturePass = 0;

		protected int m_mixTerrainDepthCirclePass = 0;
		protected int m_mixTerrainDepthSquarePass = 0;
		protected int m_mixTerrainDepthTexturePass = 0;

		protected CommandBuffer m_commandBuffer;

		protected RenderTexture m_activeTerrainheight = null;
		protected RenderTexture m_nextTerrainheight = null;
		protected RenderTexture m_dynamicInput = null;
		private RenderTexture m_slippage = null;
		private RenderTexture m_outFlow = null;
		private RenderTexture m_sediment0 = null;
		private RenderTexture m_sediment1 = null;		
		
		protected RenderTargetIdentifier[] m_mrt = null;
		private bool m_platformSupportsFloat32Blend = true;
		protected bool m_terrainSupportsTerraform = true;

		private readonly int m_temp0Id = Shader.PropertyToID("_MacCormackTemp0");
		private readonly int m_temp1Id = Shader.PropertyToID("_MacCormackTemp1");

		public override void Awake()
		{
			m_erosionMaterial = new Material(Shader.Find("Hidden/FluidFrenzy/Erosion"));
			m_erosionMaxSlippagePass = m_erosionMaterial.FindPass("MaxSlippage");
			m_erosionErosionOutflowPass = m_erosionMaterial.FindPass("ErosionOutflow");
			m_erosionErosionApplyPass = m_erosionMaterial.FindPass("ErosionApply");
			m_erosionSedimentPass = m_erosionMaterial.FindPass("Sediment");
			m_erosionAdvectSedimentFastPass = m_erosionMaterial.FindPass("AdvectSedimentFast");
			m_erosionAdvectSedimentPass = m_erosionMaterial.FindPass("AdvectSediment");
			m_erosionMacCormackPass = m_erosionMaterial.FindPass("ProcessMacCormack");
			m_erosionCopyTerrainPass = m_erosionMaterial.FindPass("CopyTerrain");
			m_erosionCombineTerrainPass = m_erosionMaterial.FindPass("CombineTerrain");

			m_addTerrainMaterial = new Material(Shader.Find("Hidden/FluidFrenzy/AddTerrain"));
			m_addTerrainCirclePass = m_addTerrainMaterial.FindPass("AddTerrainCircle");
			m_addSplatmapCirclePass = m_addTerrainMaterial.FindPass("AddSplatmapCircle");
			m_subSplatmapCirclePass = m_addTerrainMaterial.FindPass("SubSplatmapCircle");
			m_setSplatmapCirclePass = m_addTerrainMaterial.FindPass("SetSplatmapCircle");

			m_addTerrainSquarePass = m_addTerrainMaterial.FindPass("AddTerrainSquare");
			m_addSplatmapSquarePass = m_addTerrainMaterial.FindPass("AddSplatmapSquare");
			m_subSplatmapSquarePass = m_addTerrainMaterial.FindPass("SubSplatmapSquare");
			m_setSplatmapSquarePass = m_addTerrainMaterial.FindPass("SetSplatmapSquare");

			m_addTerrainTexturePass = m_addTerrainMaterial.FindPass("AddTerrainTexture");
			m_addSplatmapTexturePass = m_addTerrainMaterial.FindPass("AddSplatmapTexture");
			m_subSplatmapTexturePass = m_addTerrainMaterial.FindPass("SubSplatmapTexture");
			m_setSplatmapTexturePass = m_addTerrainMaterial.FindPass("SetSplatmapTexture");

			m_mixTerrainHeightCirclePass = m_addTerrainMaterial.FindPass("MixTerrainHeightCircle");
			m_mixTerrainHeightSquarePass = m_addTerrainMaterial.FindPass("MixTerrainHeightSquare");
			m_mixTerrainHeightTexturePass = m_addTerrainMaterial.FindPass("MixTerrainHeightTexture");

			m_mixTerrainDepthCirclePass = m_addTerrainMaterial.FindPass("MixTerrainDepthCircle");
			m_mixTerrainDepthSquarePass = m_addTerrainMaterial.FindPass("MixTerrainDepthSquare");
			m_mixTerrainDepthTexturePass = m_addTerrainMaterial.FindPass("MixTerrainDepthTexture");

			m_addTerrainDynamicPass = m_addTerrainMaterial.FindPass("AddTerrainDynamic");

			m_mrt = new RenderTargetIdentifier[2];

			m_commandBuffer = new CommandBuffer();
			m_commandBuffer.name = "Erosionlayer";
			m_internalPropertyBlock = new MaterialPropertyBlock();
			m_externalPropertyBlock = new MaterialPropertyBlock();
		}

		public override void CopyFrom(FluidLayer source)
		{
			ErosionLayer sourceLayer = source as ErosionLayer;

			layerSettings.Clear();

			for (int i = 0; i < sourceLayer.layerSettings.Count; i++)
			{
				ErosionSettings sourceSettings = sourceLayer.layerSettings[i];
				ErosionSettings newSettings = new ErosionSettings()
				{
					slippage = sourceSettings.slippage,
					slippageAngle = sourceSettings.slippageAngle,
					slopeSmoothness = sourceSettings.slopeSmoothness,
					hydraulicErosion = sourceSettings.hydraulicErosion,
					maxSediment = sourceSettings.maxSediment,
					sedimentDissolveRate = sourceSettings.sedimentDissolveRate,
					sedimentDepositRate = sourceSettings.sedimentDepositRate,
				};
				layerSettings.Add(newSettings);
			}
		}


		void OnValidate()
		{
			if (version < 2)
			{
#pragma warning disable CS0612 // Rethrow to preserve stack details
#if UNITY_EDITOR
				UnityEditor.Undo.RecordObject(this, "Update ErosionLayer");
				layerSettings.Clear();

				ErosionSettings layer0Settings = new ErosionSettings()
				{
					slippage = false,
					hydraulicErosion = false
				};
				layerSettings.Add(layer0Settings);

				ErosionSettings newSettings = new ErosionSettings()
				{
					slippage = this.slippage,
					slippageAngle = this.slippageAngle,
					slopeSmoothness = this.slopeSmoothness,
					hydraulicErosion = this.hydraulicErosion,
					maxSediment = this.maxSediment,
					sedimentDissolveRate = this.sedimentDissolveRate,
					sedimentDepositRate = this.sedimentDepositRate,
				};
				layerSettings.Add(newSettings);
				minTiltAngle = minTiltAngle * 90;
				version = Version;
#endif
#pragma warning restore CS0618 // Rethrow to preserve stack details
			}
		}

		public override void Init(FluidSimulation simulation)
		{
			base.Init(simulation);
			m_platformSupportsFloat32Blend = simulation.platformSupportsFloat32Blend;
			m_terrainSupportsTerraform = simulation.terrainType == FluidSimulation.TerrainType.SimpleTerrain;
			Vector2Int numSimulationCells = simulation.numSimulationCells;

			if (m_terrainSupportsTerraform)
			{
				RenderTextureFormat heightFormat = layerSettings.Count > 1 ? RenderTextureFormat.ARGBFloat : RenderTextureFormat.RGFloat;
				RenderTextureFormat sedimentFormat = RenderTextureFormat.RHalf;
				if (layerSettings.Count >= 3)
				{
					sedimentFormat = RenderTextureFormat.ARGBHalf;
				}
				else if (layerSettings.Count == 2)
				{
					sedimentFormat = RenderTextureFormat.RGHalf;
				}
				FilterMode fluidFilterMode = simulation.platformSupportsLinearFilterSimulation ? FilterMode.Bilinear : FilterMode.Point;
				m_activeTerrainheight = GraphicsHelpers.CreateSimulationRT(numSimulationCells.x, numSimulationCells.y, heightFormat, true, fluidFilterMode, name: "Terrain_1");
				m_nextTerrainheight = GraphicsHelpers.CreateSimulationRT(numSimulationCells.x, numSimulationCells.y, heightFormat, true, fluidFilterMode, name: "Terrain_2");
				m_outFlow = GraphicsHelpers.CreateSimulationRT(numSimulationCells.x, numSimulationCells.y, RenderTextureFormat.ARGBHalf, name: "ErosionOutflow");
				m_slippage = GraphicsHelpers.CreateSimulationRT(numSimulationCells.x, numSimulationCells.y, RenderTextureFormat.RHalf, name: "ErosionSlippage");
				m_sediment0 = GraphicsHelpers.CreateSimulationRT(numSimulationCells.x, numSimulationCells.y, sedimentFormat, name: "Sediment0");
				m_sediment1 = GraphicsHelpers.CreateSimulationRT(numSimulationCells.x, numSimulationCells.y, sedimentFormat, name: "Sediment1");	
				
				if (!m_platformSupportsFloat32Blend)
				{
					RenderTextureFormat	dynamicInputFormat = layerSettings.Count > 1 ? RenderTextureFormat.ARGBHalf : RenderTextureFormat.RGHalf;
					m_dynamicInput = GraphicsHelpers.CreateSimulationRT(numSimulationCells.x, numSimulationCells.y, dynamicInputFormat, true, name: "DynamicTerrainInput");
					m_addTerrainMaterial.EnableKeyword("_BLEND_NOT_SUPPORTED");
				}
			}
		}

		public override void ResetLayer(FluidSimulation simulation)
		{
			if (!m_terrainSupportsTerraform)
			{
				return;
			}
			Texture heightmap = null;
			float heightmapScale = 0;

			Vector4 uvScaleOffset = Vector2.one;
			simulation.GetSourceHeightmapData(ref heightmap, ref heightmapScale, ref uvScaleOffset);
			simulation.CopyTerrainToSimulation(heightmap, heightmapScale, uvScaleOffset, false, m_activeTerrainheight);
			simulation.CopyTerrainToSimulation(heightmap, heightmapScale, uvScaleOffset, false, m_nextTerrainheight);

			Vector2 texelOffset = new Vector2(1.0f / m_activeTerrainheight.width, 1.0f / m_activeTerrainheight.height);
			Vector4 blitScale = new Vector4(1 - texelOffset.x * simulation.ghostCells2.x,
											1 - texelOffset.y * simulation.ghostCells2.y,
											texelOffset.x * simulation.ghostCells.x,
											texelOffset.y * simulation.ghostCells.y);

			m_internalPropertyBlock.Clear();
			m_internalPropertyBlock.SetTexture(FluidShaderProperties._TerrainHeightField, m_activeTerrainheight);
			m_internalPropertyBlock.SetTexture(FluidShaderProperties._Obstacles, simulation.obstaclesHeight);
			m_internalPropertyBlock.SetVector(FluidShaderProperties._BlitScaleBiasRt, blitScale);
			m_internalPropertyBlock.SetVector(FluidShaderProperties._BlitScaleBias, blitScale);
			FluidSimulation.BlitQuad(m_commandBuffer, null, simulation.terrainHeight, m_erosionMaterial, m_internalPropertyBlock, m_erosionCombineTerrainPass);

			Graphics.ExecuteCommandBuffer(m_commandBuffer);
			m_commandBuffer.Clear();

			RenderTexture.active = m_sediment0;
			GL.Clear(false, true, Color.clear);

			RenderTexture.active = m_sediment1;
			GL.Clear(false, true, Color.clear);

			RenderTexture.active = m_slippage;
			GL.Clear(false, true, Color.clear);

			RenderTexture.active = m_outFlow;
			GL.Clear(false, true, Color.clear);			
			
			if (!m_platformSupportsFloat32Blend)
			{
				RenderTexture.active = m_dynamicInput;
				GL.Clear(false, true, Color.clear);
			}
		}

		public override void OnDestroy()
		{
			GraphicsHelpers.ReleaseSimulationRT(m_activeTerrainheight);
			GraphicsHelpers.ReleaseSimulationRT(m_nextTerrainheight);
			GraphicsHelpers.ReleaseSimulationRT(m_slippage);
			GraphicsHelpers.ReleaseSimulationRT(m_sediment0);
			GraphicsHelpers.ReleaseSimulationRT(m_sediment1);
			GraphicsHelpers.ReleaseSimulationRT(m_outFlow);
			GraphicsHelpers.ReleaseSimulationRT(m_dynamicInput);
			Destroy(m_erosionMaterial);
			Destroy(m_addTerrainMaterial);
		}

		public override void Step(FluidSimulation simulation, float deltaTime, int numSteps)
		{
			Process(simulation, deltaTime);

			if (m_terrainSupportsTerraform)
			{
				using (new ProfilingScope(m_commandBuffer, ProfilingSampler.Get(WaterSimProfileID.CopyTerrain)))
				{
					RenderTexture terrainTarget = simulation.simpleTerrain.renderHeightmap;
					Vector2 texelOffset = new Vector2(1.0f / m_activeTerrainheight.width, 1.0f / m_activeTerrainheight.height);
					Vector4 blitScale = new Vector4(1 - texelOffset.x * simulation.ghostCells2.x,
													1 - texelOffset.y * simulation.ghostCells2.y,
													texelOffset.x * simulation.ghostCells.x,
													texelOffset.y * simulation.ghostCells.y);

					m_internalPropertyBlock.Clear();
					m_internalPropertyBlock.SetTexture(FluidShaderProperties._TerrainHeightField, m_activeTerrainheight);
					m_internalPropertyBlock.SetVector(FluidShaderProperties._BlitScaleBiasRt, Vector2.one);
					m_internalPropertyBlock.SetVector(FluidShaderProperties._BlitScaleBias, blitScale);
					FluidSimulation.BlitQuad(m_commandBuffer, null, terrainTarget, m_erosionMaterial, m_internalPropertyBlock, m_erosionCopyTerrainPass);


					Vector2 obstaclePaddingTexel = new Vector2(1.0f / simulation.obstaclesHeight.width, 1.0f / simulation.obstaclesHeight.width);
					Vector4 obstaclePadding = new Vector4(1.0f - obstaclePaddingTexel.x * simulation.ghostCells2.x,
								1.0f - obstaclePaddingTexel.y * simulation.ghostCells2.y,
								obstaclePaddingTexel.x * simulation.ghostCells.x,
								obstaclePaddingTexel.y * simulation.ghostCells.y);

					m_internalPropertyBlock.Clear();
					m_internalPropertyBlock.SetTexture(FluidShaderProperties._TerrainHeightField, m_activeTerrainheight);
					m_internalPropertyBlock.SetTexture(FluidShaderProperties._Obstacles, simulation.obstaclesHeight);
					m_internalPropertyBlock.SetVector(FluidShaderProperties._ObstaclePadding_ST, obstaclePadding);
					m_internalPropertyBlock.SetVector(FluidShaderProperties._BlitScaleBiasRt, blitScale);
					m_internalPropertyBlock.SetVector(FluidShaderProperties._BlitScaleBias, blitScale);
					FluidSimulation.BlitQuad(m_commandBuffer, null, simulation.terrainHeight, m_erosionMaterial, m_internalPropertyBlock, m_erosionCombineTerrainPass);

					simulation.UpdateTerrainBorders(m_commandBuffer);
				}

				// Make sure this is synced up for the next add/min/max/set pass
				if (!m_platformSupportsFloat32Blend)
				{
					m_commandBuffer.Blit(m_activeTerrainheight, m_nextTerrainheight);
				}
			}


			Graphics.ExecuteCommandBuffer(m_commandBuffer);
			m_commandBuffer.Clear();
		}

		protected virtual void Process(FluidSimulation simulation, float deltaTime)
		{
			UpdateErosion(simulation, deltaTime);
		}

		private void RefreshTerrain(FluidSimulation simulation)
		{
			float heightmapScale = 1;
			Vector4 uvScaleOffset = Vector2.one;
			Texture heightmap = simulation.simpleTerrain.renderHeightmap;
			simulation.CopyTerrainToSimulation(heightmap, heightmapScale, uvScaleOffset, false, m_activeTerrainheight);
		}

		private void UpdateErosion(FluidSimulation simulation, float deltaTime)
		{

			if (!m_terrainSupportsTerraform)
			{
				return;
			}

			if (simulation.simpleTerrain.reloadedTerrain)
			{
				RefreshTerrain(simulation);
				simulation.simpleTerrain.reloadedTerrain = false;
			}

			UpdateTerrainModifiers(simulation, deltaTime);
			using (new ProfilingScope(m_commandBuffer, ProfilingSampler.Get(WaterSimProfileID.Erosion)))
			{
				if (!m_platformSupportsFloat32Blend)
				{
					m_internalPropertyBlock.Clear();
					m_internalPropertyBlock.SetVector(FluidShaderProperties._BlitScaleBias, Vector2.one);
					m_internalPropertyBlock.SetVector(FluidShaderProperties._BlitScaleBiasRt, Vector2.one);
					m_internalPropertyBlock.SetTexture(FluidShaderProperties._TerrainHeightField, m_activeTerrainheight);
					FluidSimulation.BlitQuad(m_commandBuffer, m_dynamicInput, m_nextTerrainheight, m_addTerrainMaterial, m_internalPropertyBlock, m_addTerrainDynamicPass);
					SwapTerrainRT();
				}

				float cellSizeWS = simulation.dimension.x / simulation.settings.numberOfCells.x;
				float texelWS = simulation.dimension.x / m_activeTerrainheight.width;
				// Erosion is not applied to base layer(0)
				for (int layer = 0; layer < layerSettings.Count; layer++)
				{
					var settings = layerSettings[layer];
					float angle = Mathf.Deg2Rad * Mathf.Min(settings.slippageAngle, 89.99f);
					float maxHeightDif = Mathf.Tan(angle) * cellSizeWS;
					if (settings.slippage)
					{
						m_internalPropertyBlock.Clear();
						m_internalPropertyBlock.SetVector(FluidShaderProperties._BlitScaleBias, Vector2.one);
						m_internalPropertyBlock.SetVector(FluidShaderProperties._BlitScaleBiasRt, Vector2.one);
						m_internalPropertyBlock.SetVector(FluidShaderProperties._TotalHeightLayerMask, FluidSimulation.LayerToTotalHeightLayerMask(layer));
						m_internalPropertyBlock.SetFloat(FluidShaderProperties._MaxHeightDif, maxHeightDif);
						m_internalPropertyBlock.SetFloat(FluidShaderProperties._SlopeSmoothness, settings.slopeSmoothness);
						m_internalPropertyBlock.SetTexture(FluidShaderProperties._TerrainHeightField, m_activeTerrainheight);
						FluidSimulation.BlitQuad(m_commandBuffer, null, m_slippage, m_erosionMaterial, m_internalPropertyBlock, m_erosionMaxSlippagePass);

						m_internalPropertyBlock.Clear();
						m_internalPropertyBlock.SetVector(FluidShaderProperties._BlitScaleBias, Vector2.one);
						m_internalPropertyBlock.SetVector(FluidShaderProperties._BlitScaleBiasRt, Vector2.one);
						m_internalPropertyBlock.SetVector(FluidShaderProperties._LayerMask, FluidSimulation.LayerToLayerMask(layer));
						m_internalPropertyBlock.SetVector(FluidShaderProperties._TotalHeightLayerMask, FluidSimulation.LayerToTotalHeightLayerMask(layer));
						m_internalPropertyBlock.SetTexture(FluidShaderProperties._TerrainHeightField, m_activeTerrainheight);
						m_internalPropertyBlock.SetTexture(FluidShaderProperties._MaxHeightField, m_slippage);
						m_internalPropertyBlock.SetFloat(FluidShaderProperties._ErosionOutflowRate, simulation.settings.acceleration);
						FluidSimulation.BlitQuad(m_commandBuffer, null, m_outFlow, m_erosionMaterial, m_internalPropertyBlock, m_erosionErosionOutflowPass);

						m_internalPropertyBlock.Clear();
						m_internalPropertyBlock.SetVector(FluidShaderProperties._BlitScaleBias, Vector2.one);
						m_internalPropertyBlock.SetVector(FluidShaderProperties._BlitScaleBiasRt, Vector2.one);
						m_internalPropertyBlock.SetVector(FluidShaderProperties._LayerMask, FluidSimulation.LayerToLayerMask(layer));
						m_internalPropertyBlock.SetVector(FluidShaderProperties._TotalHeightLayerMask, FluidSimulation.LayerToTotalHeightLayerMask(layer));
						m_internalPropertyBlock.SetTexture(FluidShaderProperties._TerrainHeightField, m_activeTerrainheight);
						m_internalPropertyBlock.SetTexture(FluidShaderProperties._OutflowField, m_outFlow);
						FluidSimulation.BlitQuad(m_commandBuffer, null, m_nextTerrainheight, m_erosionMaterial, m_internalPropertyBlock, m_erosionErosionApplyPass);
						SwapTerrainRT();
					}
				}


				Vector4 layersMaxSediment = Vector4.zero;
				Vector4 layersDissolveRate = Vector4.zero;
				Vector4 layersDepositRate = Vector4.zero;

				bool hasErosion = false;
				for (int layer = 0; layer < layerSettings.Count; layer++)
				{
					var settings = layerSettings[layer];
					if (!settings.hydraulicErosion)
					{
						continue;
					}
					layersMaxSediment[layer] = settings.maxSediment;
					layersDissolveRate[layer] = settings.sedimentDissolveRate * deltaTime;
					layersDepositRate[layer] = settings.sedimentDepositRate * deltaTime;
					hasErosion = true;
				}

				if (!hasErosion)
				{
					return;
				}

				Vector4 velocityTextureST = Vector2.one;
				if (simulation is FluxFluidSimulation)
				{
					velocityTextureST = simulation.velocityTextureST;
				}

				m_internalPropertyBlock.Clear();
				m_internalPropertyBlock.SetVector(FluidShaderProperties._BlitScaleBias, Vector2.one);
				m_internalPropertyBlock.SetVector(FluidShaderProperties._BlitScaleBiasRt, Vector2.one);

				m_internalPropertyBlock.SetVector(FluidShaderProperties._VelocityBlitScaleBias, velocityTextureST);
				m_internalPropertyBlock.SetTexture(FluidShaderProperties._TerrainHeightField, m_activeTerrainheight);
				m_internalPropertyBlock.SetTexture(FluidShaderProperties._FluidHeightField, simulation.fluidHeight);
				m_internalPropertyBlock.SetTexture(FluidShaderProperties._SedimentField, m_sediment0);
				m_internalPropertyBlock.SetTexture(FluidShaderProperties._VelocityField, simulation.velocityTexture);

				m_internalPropertyBlock.SetInt(FluidShaderProperties._NumLayers, layerSettings.Count);
				m_internalPropertyBlock.SetVector(FluidShaderProperties._TopLayerMask, FluidSimulation.LayersToTopLayerMask(layerSettings.Count));
				m_internalPropertyBlock.SetVector(FluidShaderProperties._SedimentMax, layersMaxSediment);
				m_internalPropertyBlock.SetVector(FluidShaderProperties._DissolveRate, layersDissolveRate);
				m_internalPropertyBlock.SetVector(FluidShaderProperties._DepositRate, layersDepositRate);
				m_internalPropertyBlock.SetFloat(FluidShaderProperties._MinTiltAngle, Mathf.Sin(Mathf.Deg2Rad * minTiltAngle));
				m_internalPropertyBlock.SetFloat(FluidShaderProperties._TexelWorldSize, texelWS);

				m_mrt[0] = m_nextTerrainheight;
				m_mrt[1] = m_sediment1;
				FluidSimulation.BlitQuad(m_commandBuffer, null, m_mrt, m_sediment1.depthBuffer, m_erosionMaterial, m_internalPropertyBlock, m_erosionSedimentPass);
				SwapTerrainRT();

				Vector2 advectScale = ((Vector2.one * simulation.advectionScale / new Vector2(m_sediment1.width, m_sediment1.height)) / simulation.GetCellSize()) * deltaTime * sedimentAdvectionSpeed;
				if (highPrecisionAdvection)
				{
					RenderTextureDescriptor tempRTD = m_sediment1.descriptor;
					m_commandBuffer.GetTemporaryRT(m_temp0Id, tempRTD);
					m_commandBuffer.GetTemporaryRT(m_temp1Id, tempRTD);

					// Execute the Advection/MacCormack sequence
					{
						// Forward Advection: m_sediment1 -> m_temp0
						AdvectSediment(simulation, m_sediment1, m_temp0Id, advectScale, velocityTextureST);
						// Backward Advection: m_temp0 -> m_temp1
						AdvectSediment(simulation, m_temp0Id, m_temp1Id, -advectScale, velocityTextureST);
						// MacCormack Correction: uses m_sediment1 (source), m_temp0 (intermediate 1), m_temp1 (intermediate 2) -> m_sediment0 (destination)
						MacCormack(simulation, advectScale, velocityTextureST);
					}

					m_commandBuffer.ReleaseTemporaryRT(m_temp0Id);
					m_commandBuffer.ReleaseTemporaryRT(m_temp1Id);

				}
				else
				{
					m_internalPropertyBlock.Clear();
					m_internalPropertyBlock.SetTexture(FluidShaderProperties._VelocityField, simulation.velocityTexture);
					m_internalPropertyBlock.SetTexture(FluidShaderProperties._SedimentField, m_sediment1);
					m_internalPropertyBlock.SetVector(FluidShaderProperties._AdvectScale, advectScale);
					m_internalPropertyBlock.SetTexture(FluidShaderProperties._FluidHeightField, simulation.fluidHeight);
					m_internalPropertyBlock.SetVector(FluidShaderProperties._VelocityBlitScaleBias, velocityTextureST);
					FluidSimulation.BlitQuad(m_commandBuffer, null, m_sediment0, m_erosionMaterial, m_internalPropertyBlock, m_erosionAdvectSedimentFastPass);
				}
			}
		}

		private void AdvectSediment(FluidSimulation simulation, RenderTargetIdentifier source, RenderTargetIdentifier destId, Vector2 advectScale, Vector4 velocityTextureST)
		{
			m_internalPropertyBlock.Clear();
			m_internalPropertyBlock.SetTexture(FluidShaderProperties._VelocityField, simulation.velocityTexture);
			m_internalPropertyBlock.SetVector(FluidShaderProperties._AdvectScale, advectScale );
			m_internalPropertyBlock.SetTexture(FluidShaderProperties._FluidHeightField, simulation.fluidHeight);
			m_internalPropertyBlock.SetVector(FluidShaderProperties._VelocityBlitScaleBias, velocityTextureST);

			m_commandBuffer.SetGlobalTexture(FluidShaderProperties._SedimentField, source);
			FluidSimulation.BlitQuad(m_commandBuffer, destId, m_erosionMaterial, m_internalPropertyBlock, m_erosionAdvectSedimentPass);
		}

		private void MacCormack(FluidSimulation simulation, Vector2 advectScale, Vector4 velocityTextureST)
		{
			m_internalPropertyBlock.Clear();
			m_internalPropertyBlock.SetTexture(FluidShaderProperties._VelocityField, simulation.velocityTexture);

			m_internalPropertyBlock.SetTexture(FluidShaderProperties._SedimentField, m_sediment1);
			m_internalPropertyBlock.SetVector(FluidShaderProperties._AdvectScale, advectScale);
			m_internalPropertyBlock.SetTexture(FluidShaderProperties._FluidHeightField, simulation.fluidHeight);
			m_internalPropertyBlock.SetVector(FluidShaderProperties._VelocityBlitScaleBias, velocityTextureST);

			m_commandBuffer.SetGlobalTexture(FluidShaderProperties._InterField1, m_temp0Id);
			m_commandBuffer.SetGlobalTexture(FluidShaderProperties._InterField2, m_temp1Id);
			// Blit to the final destination (m_sediment0)
			FluidSimulation.BlitQuad(m_commandBuffer, null, m_sediment0, m_erosionMaterial, m_internalPropertyBlock, m_erosionMacCormackPass);
		}

		public void SwapTerrainRT()
		{
			FluidSimulation.Swap(ref m_activeTerrainheight, ref m_nextTerrainheight);
		}

		public override RenderTexture GetDebugBuffer(FluidSimulation.DebugBuffer buffer)
		{
			switch (buffer)
			{
				case FluidSimulation.DebugBuffer.Sediment:
					return m_sediment0;				
				case FluidSimulation.DebugBuffer.Slippage:
					return m_slippage;				
				case FluidSimulation.DebugBuffer.ErosionTerrain:
					return m_activeTerrainheight;
			}
			return null;
		}

		public override IEnumerable<FluidSimulation.DebugBuffer> EnumerateBuffers()
		{
			yield return FluidSimulation.DebugBuffer.Sediment;
			yield return FluidSimulation.DebugBuffer.Slippage;
			yield return FluidSimulation.DebugBuffer.ErosionTerrain;
		}
	}
}