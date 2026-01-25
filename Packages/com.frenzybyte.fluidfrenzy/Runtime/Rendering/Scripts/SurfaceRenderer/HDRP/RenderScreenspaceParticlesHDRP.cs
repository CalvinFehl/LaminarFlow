#if FLUIDFRENZY_RUNTIME_HDRP_SUPPORT
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using FluidFrenzy;
using System.Collections.Generic;

namespace FluidFrenzy
{
	class RenderScreenspaceParticlesHDRP : CustomPass
	{

		RTHandle screenspaceParticles;
		private List<FluidParticleGenerator> m_particles;

		public RenderScreenspaceParticlesHDRP()
		{
			m_particles = new List<FluidParticleGenerator>();
		}

		public void RegisterParticles(FluidParticleGenerator particles)
		{
			m_particles.Add(particles);
		}

		public void DeregisterParticles(FluidParticleGenerator particles)
		{
			m_particles.Remove(particles);
		}


		// It can be used to configure render targets and their clear state. Also to create temporary render target textures.
		// When empty this render pass will render to the active camera render target.
		// You should never call CommandBuffer.SetRenderTarget. Instead call <c>ConfigureTarget</c> and <c>ConfigureClear</c>.
		// The render pipeline will ensure target setup and clearing happens in an performance manner.
		protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd)
		{
			screenspaceParticles = RTHandles.Alloc(
				Vector2.one, TextureXR.slices, dimension: TextureDimension.Tex2D,
				colorFormat: GraphicsFormat.R16G16B16A16_SFloat,
				// We don't need alpha for this effect
				useDynamicScale: true, name: "ScreenspaceParticles"
			);
		}

		protected override void Execute(CustomPassContext ctx)
		{
			// Executed every frame for all the camera inside the pass volume.
			// The context contains the command buffer to use to enqueue graphics commands.
			//Get a CommandBuffer from pool.
			ctx.cmd.SetRenderTarget(screenspaceParticles);
			ctx.cmd.ClearRenderTarget(false, true, Color.clear);
			foreach (FluidParticleGenerator particles in m_particles)
			{
				particles.surfaceParticlesSystem.Render(ctx.cmd);
			}
			ctx.cmd.SetGlobalTexture(FluidShaderProperties._FluidScreenSpaceParticles, screenspaceParticles);
		}

		protected override void Cleanup()
		{
			// Cleanup code
			screenspaceParticles.Release();
		}
	}
}
#endif