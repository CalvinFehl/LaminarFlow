using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
#if FLUIDFRENZY_RUNTIME_HDRP_SUPPORT
using UnityEngine.Rendering.HighDefinition;
#endif
#if FLUIDFRENZY_RUNTIME_URP_SUPPORT
using UnityEngine.Rendering.Universal;
#endif
using static FluidFrenzy.FluidSimulation;

namespace FluidFrenzy
{
	/// <summary>
	/// <see cref="FluidParticleGenerator"/> is an <see cref="FluidLayer"/> extension that analyzes the fluid simulation's dynamics to spawn and manage visual particle effects for foam and spray.
	/// </summary>
	/// <remarks>
	/// <para>
	/// This component generates two distinct types of particles by detecting areas of high turbulence and breaking waves within the fluid simulation:
	/// </para>
	/// <list type="bullet">
	/// 	<item>
	/// 		<term>Splash Particles (Spray/Droplets)</term>
	/// 		<description>Ballistic particles spawned at high-energy events (e.g., breaking waves, collisions). These particles inherit the fluid's velocity at the moment of spawn and follow a physical trajectory (like spray or droplets) until their lifetime expires.</description>
	/// 	</item>
	/// 	<item>
	/// 		<term>Surface Particles (Foam/Bubbles)</term>
	/// 		<description>Particles spawned on top of the fluid surface, primarily in areas of high turbulence. They are continuously advected (moved) by the simulation's velocity field, acting as a visual representation of sea foam or churn. These particles can often be rendered to an off-screen buffer for use as a <c>foam mask</c> in the water shader.</description>
	/// 	</item>
	/// </list>
	/// </remarks>
	[HelpURL("https://frenzybyte.github.io/fluidfrenzy/docs/fluid_simulation_components/#particle-generator")]
	public class FluidParticleGenerator : FluidLayer
	{
		[SerializeField]
		internal int version = 1;
#if UNITY_2021_1_OR_NEWER
		int _FluidFrenzySurfaceParticlesID = Shader.PropertyToID("_FluidFrenzySurfaceParticles");

		private CommandBuffer m_commandBuffer;

		/// <summary>
		/// Toggles the emission of splash particles from cresting or breaking waves.
		/// </summary>
		public bool breakingWaveSplashes = true;

		/// <summary>
		/// The minimum surface angle (steepness) required to trigger a splash.
		/// </summary>
		/// <remarks>
		/// Higher values restrict splashes to only the sharpest peaks of the waves.
		/// </remarks>
		public float steepnessThreshold = 0.45f;

		/// <summary>
		/// The minimum vertical (upward) velocity required to trigger a splash.
		/// </summary>
		/// <remarks>
		/// Used to identify waves that are rising rapidly before they break.
		/// </remarks>
		public float riseRateThreshold = 4;

		/// <summary>
		/// The minimum physical length a wave must have to emit particles.
		/// </summary>
		/// <remarks>
		/// Helps prevent small, high-frequency noise from generating excessive spray.
		/// </remarks>
		public float waveLengthThreshold = -4;

		/// <summary>
		/// Optimization setting that spreads the sampling of grid cells for breaking waves across multiple frames.
		/// </summary>
		/// <remarks>
		/// A value of 2 means a specific cell is checked every 2nd frame. Increasing this value reduces the number of particles spawned and lowers performance cost, but may make emission look less responsive.
		/// </remarks>
		public int breakingWaveGridStagger = 2;

		/// <summary>
		/// Toggles the emission of spray particles from areas of high turbulence (diverging velocities).
		/// </summary>
		public bool turbulenceSplashes = true;

		/// <summary>
		/// Optimization setting that spreads the sampling of grid cells for turbulence splashes across multiple frames.
		/// </summary>
		public int turbulenceSplashGridStagger = 8;

		/// <summary>
		/// The minimum turbulence value required to trigger a splash particle.
		/// </summary>
		public float sprayTurbelenceThreshold = 0.3f;

		/// <summary>
		/// Configuration settings for the ballistic splash particles (movement, rendering, and limits).
		/// </summary>
		public FluidParticleSystem splashParticleSystem;

		/// <summary>
		/// Toggles the emission of surface particles (foam) in turbulent areas.
		/// </summary>
		/// <remarks>
		/// Unlike splashes, these particles stick to the fluid surface and move with the flow.
		/// </remarks>
		public bool turbulenceSurface = true;

		/// <summary>
		/// The minimum turbulence value required to trigger a surface particle.
		/// </summary>
		public float surfaceTurblenceThreshold = 0.3f;

		/// <summary>
		/// Optimization setting that spreads the sampling of grid cells for surface particles across multiple frames.
		/// </summary>
		public int surfaceGridStagger = 4;

		/// <summary>
		/// Configuration settings for the advected surface particles (movement, rendering, and limits).
		/// </summary>
		public FluidParticleSystem surfaceParticlesSystem;

		/// <summary>
		/// If enabled, surface particles are rendered to a dedicated offscreen texture buffer instead of the main camera.
		/// </summary>
		/// <remarks>
		/// This generated texture is globally available to shaders (e.g., as a foam mask) to create effects like white water trails without drawing individual particle geometry to the screen.
		/// </remarks>
		public bool renderOffscreen = true;

		RenderTexture m_visualizeSplash;
		protected MaterialPropertyBlock m_internalPropertyBlock = null;
		protected int m_fluidSolverSplashAndSprayPass = 0;
		private Material m_fluidSolverMaterial;
		private LocalKeyword m_fluidSplashBreakingwave;
		private LocalKeyword m_fluidSplashTurbulence;
		private LocalKeyword m_fluidSurfaceTurbulence;

#if FLUIDFRENZY_RUNTIME_URP_SUPPORT
		private RenderScreenspaceParticlesURP m_renderScreenspaceParticlesURP;
#endif

#if FLUIDFRENZY_RUNTIME_HDRP_SUPPORT
		private static RenderScreenspaceParticlesHDRP s_renderScreenspaceParticlesHDRP;
#endif

		public override void Awake()
		{
			if (!SystemInfo.supportsComputeShaders)
			{
				enabled = false;
				return;
			}
			m_commandBuffer = new CommandBuffer();
			m_commandBuffer.name = "FluidParticles";
			m_internalPropertyBlock = new MaterialPropertyBlock();

			m_fluidSolverMaterial = new Material(Shader.Find("Hidden/FluidFrenzy/Simulation/FluidParticles"));
			m_fluidSolverSplashAndSprayPass = m_fluidSolverMaterial.FindPass("SplashAndSpray"); 
																								
			m_fluidSplashBreakingwave = new LocalKeyword(m_fluidSolverMaterial.shader, "FLUID_SPLASH_BREAKINGWAVE");
			m_fluidSplashTurbulence = new LocalKeyword(m_fluidSolverMaterial.shader, "FLUID_SPLASH_TURBULENCE");
			m_fluidSurfaceTurbulence = new LocalKeyword(m_fluidSolverMaterial.shader, "FLUID_SURFACE_TURBULENCE");

#if FLUIDFRENZY_RUNTIME_URP_SUPPORT
			m_renderScreenspaceParticlesURP = new RenderScreenspaceParticlesURP(this);
#endif

#if FLUIDFRENZY_RUNTIME_HDRP_SUPPORT
			if (s_renderScreenspaceParticlesHDRP == null)
			{
				s_renderScreenspaceParticlesHDRP = new RenderScreenspaceParticlesHDRP();
				CustomPassVolume.RegisterGlobalCustomPass(CustomPassInjectionPoint.BeforeTransparent, s_renderScreenspaceParticlesHDRP);
			}
#endif

		}
		public override void CopyFrom(FluidLayer source)
		{
		}

		public override void Init(FluidSimulation simulation)
		{
			if (!SystemInfo.supportsComputeShaders)
				return;

			base.Init(simulation);

			splashParticleSystem.Init(FluidParticleSystem.UpdateMode.Generic, simulation);
			surfaceParticlesSystem.Init(FluidParticleSystem.UpdateMode.ReadSimulation, simulation);

			m_visualizeSplash = new RenderTexture(simulation.numRenderCells.x, simulation.numRenderCells.y, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
			m_visualizeSplash.Create();

			if (simulation.terrainType == TerrainType.UnityTerrain)
				m_fluidSolverMaterial.EnableKeyword("_FLUID_UNITY_TERRAIN");

		}

		public override void ResetLayer(FluidSimulation simulation)
		{
			if (!SystemInfo.supportsComputeShaders)
				return;
			splashParticleSystem.Reset();
			surfaceParticlesSystem.Reset();
		}

		public void OnEnable()
		{
			if (!SystemInfo.supportsComputeShaders)
				return;

			if (renderOffscreen)
			{
				AddCameraCallbacks();
			}
		}

		public void OnDisable()
		{
			if (!SystemInfo.supportsComputeShaders)
				return;

			if (renderOffscreen)
				RemoveCameraCallbacks();

			Shader.SetGlobalTexture(FluidShaderProperties._FluidScreenSpaceParticles, Texture2D.blackTexture);
		}

		public override void OnDestroy()
		{
			splashParticleSystem?.Destroy();
			surfaceParticlesSystem?.Destroy();
		}

		private void LateUpdate()
		{
			splashParticleSystem?.Render();
			if (!renderOffscreen)
			{
				Shader.SetGlobalTexture(FluidShaderProperties._FluidScreenSpaceParticles, Texture2D.blackTexture);
				surfaceParticlesSystem?.Render();
			}
		}

		public override void Step(FluidSimulation simulation, float deltaTime, int numSteps)
		{
			if (!SystemInfo.supportsComputeShaders)
				return;

			Vector4 blitScale = Vector2.one;
			if (!breakingWaveSplashes && !turbulenceSplashes && !turbulenceSurface)
				return;
			// Particles
			m_internalPropertyBlock.Clear();
			m_internalPropertyBlock.SetFloat(FluidShaderProperties._TerrainHeightScale, simulation.terrainScale);
			m_internalPropertyBlock.SetTexture(FluidShaderProperties._FluidHeightField, simulation.fluidHeight);
			m_internalPropertyBlock.SetTexture(FluidShaderProperties._PreviousFluidHeightField, simulation.nextFluidHeight);
			m_internalPropertyBlock.SetTexture(FluidShaderProperties._VelocityField, simulation.velocityTexture);
			m_internalPropertyBlock.SetTexture(FluidShaderProperties._TerrainHeightField, simulation.terrainHeight);
			m_internalPropertyBlock.SetVector(FluidShaderProperties._BlitScaleBias, blitScale);
			m_internalPropertyBlock.SetVector(FluidShaderProperties._BlitScaleBiasRt, blitScale);

			m_internalPropertyBlock.SetConstantBuffer("ParticleEmitter0", splashParticleSystem.emitterDescBuffer, 0, splashParticleSystem.emitterDescBuffer.stride);
			m_internalPropertyBlock.SetConstantBuffer("ParticleEmitter1", surfaceParticlesSystem.emitterDescBuffer, 0, surfaceParticlesSystem.emitterDescBuffer.stride);
			m_internalPropertyBlock.SetFloat(FluidShaderProperties._ParticleEmissionRate, 1);

			int iteration = Time.frameCount;
			int divergenceLimit = surfaceGridStagger;
			int x = iteration % divergenceLimit; // Column
			int y = (iteration / divergenceLimit) % divergenceLimit; // Row
			Vector3 turbulenceOffsetRange = new Vector3(1, 0, 1) * simulation.cellWorldSize * 0.5f * divergenceLimit;

			m_internalPropertyBlock.SetVector(FluidShaderProperties._DivergenceStagger, new Vector2(x, y));
			m_internalPropertyBlock.SetInteger(FluidShaderProperties._DivergenceGridLimit, divergenceLimit);
			m_internalPropertyBlock.SetVector(FluidShaderProperties._SurfaceOffsetRange, turbulenceOffsetRange);

			int splashWaveLimit = turbulenceSplashGridStagger;
			x = iteration % splashWaveLimit; // Column
			y = (iteration / splashWaveLimit) % splashWaveLimit; // Row
			Vector3 splashOffsetRange = new Vector3(1, 0, 1) * simulation.cellWorldSize * 0.5f * splashWaveLimit;

			m_internalPropertyBlock.SetVector(FluidShaderProperties._SplashDivergenceStagger, new Vector2(x, y));
			m_internalPropertyBlock.SetInteger(FluidShaderProperties._SplashDivergenceGridLimit, splashWaveLimit);
			m_internalPropertyBlock.SetVector(FluidShaderProperties._SplashOffsetRange, splashOffsetRange);

			int breakingWaveLimit = breakingWaveGridStagger;
			x = iteration % breakingWaveLimit; // Column
			y = (iteration / breakingWaveLimit) % breakingWaveLimit; // Row
			Vector3 breakingWaveOffsetRange = new Vector3(1, 0, 1) * simulation.cellWorldSize * 0.5f * breakingWaveLimit;

			m_internalPropertyBlock.SetVector(FluidShaderProperties._BreakingWavesStagger, new Vector2(x, y));
			m_internalPropertyBlock.SetInteger(FluidShaderProperties._BreakingWavesGridLimit, breakingWaveLimit);

			float cellSizeWorld = simulation.cellWorldSize;
			float acceleration = simulation.settings.acceleration;
			Vector2 cellSize = simulation.GetCellSize();

			Vector2 beta = (Vector2.one * 2);
			Vector2 heightAvgMax = beta * (Vector2.one / cellSize / (acceleration * deltaTime));
			m_internalPropertyBlock.SetVector(FluidShaderProperties._HeightAvgMax, heightAvgMax);
			m_internalPropertyBlock.SetFloat(FluidShaderProperties._SteepnessThreshold, steepnessThreshold * ((acceleration * deltaTime) / cellSizeWorld));
			m_internalPropertyBlock.SetFloat(FluidShaderProperties._RiseRateThreshold, riseRateThreshold);
			m_internalPropertyBlock.SetFloat(FluidShaderProperties._WaveLengthThreshold, waveLengthThreshold);

			m_internalPropertyBlock.SetFloat(FluidShaderProperties._SprayDivergenceThreshold, sprayTurbelenceThreshold);

			m_internalPropertyBlock.SetFloat(FluidShaderProperties._SurfaceDivergenceThreshold, surfaceTurblenceThreshold);

			m_internalPropertyBlock.SetVector(FluidShaderProperties._VelocityScale, simulation.GetWorldVelocityScale());

			m_commandBuffer.SetGlobalInt("_FrameCount", Time.frameCount);
			m_commandBuffer.SetGlobalVector(FluidShaderProperties._CellSize, cellSize);
			m_commandBuffer.SetKeyword(m_fluidSolverMaterial, m_fluidSplashBreakingwave, breakingWaveSplashes);
			m_commandBuffer.SetKeyword(m_fluidSolverMaterial, m_fluidSplashTurbulence, turbulenceSplashes);
			m_commandBuffer.SetKeyword(m_fluidSolverMaterial, m_fluidSurfaceTurbulence, turbulenceSurface);

			m_commandBuffer.SetRandomWriteTarget(3, splashParticleSystem.freeParticleIndices);
			m_commandBuffer.SetRandomWriteTarget(4, splashParticleSystem.particleBuffer);

			m_commandBuffer.SetRandomWriteTarget(5, surfaceParticlesSystem.freeParticleIndices);
			m_commandBuffer.SetRandomWriteTarget(6, surfaceParticlesSystem.particleBuffer);
			FluidSimulation.BlitQuad(m_commandBuffer, null, m_visualizeSplash, m_fluidSolverMaterial, m_internalPropertyBlock, m_fluidSolverSplashAndSprayPass);
			m_commandBuffer.ClearRandomWriteTargets();

			splashParticleSystem.Process(m_commandBuffer, deltaTime );
			surfaceParticlesSystem.Process(m_commandBuffer, deltaTime );

			Graphics.ExecuteCommandBuffer(m_commandBuffer);
			m_commandBuffer.Clear();
		}


		private void AddCameraCallbacks()
		{
			RemoveCameraCallbacks();
			Camera.onPreCull += PreCull;
			Camera.onPostRender += PostRender;
			RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;

#if FLUIDFRENZY_RUNTIME_HDRP_SUPPORT
			s_renderScreenspaceParticlesHDRP.RegisterParticles(this);
#endif
		}

		private void RemoveCameraCallbacks()
		{
			Camera.onPreCull -= PreCull;
			Camera.onPostRender -= PostRender;
			RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;

#if FLUIDFRENZY_RUNTIME_HDRP_SUPPORT
			s_renderScreenspaceParticlesHDRP.DeregisterParticles(this);
#endif
		}

		private bool RenderInCamera(Camera camera)
		{
			if (camera.cameraType == CameraType.Preview)
				return false;

			if (camera.cameraType == CameraType.Game && camera != Camera.main)
				return false;

			if ((camera.cullingMask & (1 << surfaceParticlesSystem.layer)) == 0)
				return false;

			return true;
		}

		private void PreCull(Camera camera)
		{
			if (!RenderInCamera(camera) || !renderOffscreen)
				return;
			CommandBuffer afterForwardOpaque = CameraEventCommandBuffer.GetOrCreateAndAttach(camera, CameraEvent.AfterForwardOpaque, "FluidParticles");
			CommandBuffer afterForwardAlpha = CameraEventCommandBuffer.GetOrCreateAndAttach(camera, CameraEvent.AfterForwardAlpha, "FluidParticles");

			afterForwardOpaque.GetTemporaryRT(_FluidFrenzySurfaceParticlesID, camera.pixelWidth, camera.pixelHeight, 0, FilterMode.Bilinear, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
			afterForwardOpaque.SetRenderTarget(_FluidFrenzySurfaceParticlesID, BuiltinRenderTextureType.Depth);
			afterForwardOpaque.ClearRenderTarget(false, true, Color.clear);
			surfaceParticlesSystem.Render(afterForwardOpaque);
			afterForwardOpaque.SetGlobalTexture(FluidShaderProperties._FluidScreenSpaceParticles, _FluidFrenzySurfaceParticlesID);
			afterForwardAlpha.ReleaseTemporaryRT(_FluidFrenzySurfaceParticlesID);
		}


		internal void Render(CommandBuffer cmd, Camera camera)
		{
			cmd.GetTemporaryRT(_FluidFrenzySurfaceParticlesID, camera.pixelWidth, camera.pixelHeight, 0, FilterMode.Bilinear, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
			cmd.SetRenderTarget(_FluidFrenzySurfaceParticlesID, BuiltinRenderTextureType.Depth);
			cmd.ClearRenderTarget(false, true, Color.clear);
			surfaceParticlesSystem.Render(cmd);
			cmd.SetGlobalTexture(FluidShaderProperties._FluidScreenSpaceParticles, _FluidFrenzySurfaceParticlesID);
		}
		internal void PostRender(CommandBuffer cmd, Camera camera)
		{
			if (!RenderInCamera(camera) || !renderOffscreen)
				return;
			cmd.ReleaseTemporaryRT(_FluidFrenzySurfaceParticlesID);
		}

		private void PostRender(Camera camera)
		{
			if (!RenderInCamera(camera) || !renderOffscreen)
				return;
			CameraEventCommandBuffer.Detach(camera, CameraEvent.AfterForwardOpaque);
			CameraEventCommandBuffer.Detach(camera, CameraEvent.AfterForwardAlpha);
		}

		private void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera)
		{
#if FLUIDFRENZY_RUNTIME_URP_SUPPORT
			if (!RenderInCamera(camera) || !renderOffscreen)
				return;

			camera.GetUniversalAdditionalCameraData().scriptableRenderer.EnqueuePass(m_renderScreenspaceParticlesURP);
#endif
		}

		public override RenderTexture GetDebugBuffer(DebugBuffer buffer)
		{
			switch (buffer)
			{
				case DebugBuffer.SplashAndSpray:
					return m_visualizeSplash;
			}
			return null;
		}

		public override IEnumerable<DebugBuffer> EnumerateBuffers()
		{
			yield return DebugBuffer.SplashAndSpray;
		}
#else
		public override void Awake()
		{
		}

		public override void CopyFrom(FluidLayer source)
		{
		}

		public override IEnumerable<DebugBuffer> EnumerateBuffers()
		{
			yield break;
		}

		public override RenderTexture GetDebugBuffer(DebugBuffer buffer)
		{
			return null;
		}

		public override void OnDestroy()
		{
		}

		public override void ResetLayer(FluidSimulation simulation)
		{
		}

		public override void Step(FluidSimulation simulation, float deltaTime, int step)
		{
		}
#endif
	}
}