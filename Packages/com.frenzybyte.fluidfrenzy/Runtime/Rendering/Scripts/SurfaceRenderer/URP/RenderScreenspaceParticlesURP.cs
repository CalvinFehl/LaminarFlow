#if FLUIDFRENZY_RUNTIME_URP_SUPPORT
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

#if UNITY_6000_0_OR_NEWER
using UnityEngine.Rendering.RenderGraphModule;
#endif

namespace FluidFrenzy
{
	public class RenderScreenspaceParticlesURP : ScriptableRenderPass
    {
		private static readonly int _FluidFrenzySurfaceParticlesID = Shader.PropertyToID("_FluidFrenzySurfaceParticles");

		public class PassData
		{
			internal FluidParticleGenerator particles;
		}

		private FluidParticleGenerator m_particles;

		public RenderScreenspaceParticlesURP(FluidParticleGenerator particles)
		{
			renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
			m_particles = particles;
		}

#if !UNITY_6000_0_OR_NEWER
		public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
		{
			int width = renderingData.cameraData.cameraTargetDescriptor.width;
			int height = renderingData.cameraData.cameraTargetDescriptor.height;
			cmd.GetTemporaryRT(_FluidFrenzySurfaceParticlesID, width, height, 0, FilterMode.Bilinear, renderingData.cameraData.cameraTargetDescriptor.colorFormat, RenderTextureReadWrite.Linear);
		}

		public override void OnCameraCleanup(CommandBuffer cmd)
		{
			cmd.ReleaseTemporaryRT(_FluidFrenzySurfaceParticlesID);
		}
#endif

#if UNITY_6000_0_OR_NEWER
		[System.Obsolete]
#endif
		public override void Execute(ScriptableRenderContext context,
			ref RenderingData renderingData)
		{
			//Get a CommandBuffer from pool.
			CommandBuffer cmd = CommandBufferPool.Get();
			cmd.SetRenderTarget(_FluidFrenzySurfaceParticlesID);
			cmd.ClearRenderTarget(false, true, Color.clear);
			m_particles.surfaceParticlesSystem.Render(cmd);
			cmd.SetGlobalTexture(FluidShaderProperties._FluidScreenSpaceParticles, _FluidFrenzySurfaceParticlesID);

			//Execute the command buffer and release it back to the pool.
			context.ExecuteCommandBuffer(cmd);
			CommandBufferPool.Release(cmd);
		}

#if UNITY_6000_0_OR_NEWER
		public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
		{
			using (var builder = renderGraph.AddRasterRenderPass<PassData>("Render Screenspace Particles", out PassData passData))
			{
				UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
				UniversalResourceData renderData = frameData.Get<UniversalResourceData>();
				TextureHandle depth = renderData.cameraDepth.IsValid() ? renderData.cameraDepth : renderData.activeDepthTexture;
				TextureDesc desc = new TextureDesc();

				if (cameraData.camera.activeTexture != null)
				{
					desc = new TextureDesc(cameraData.requiresDepthTexture ? cameraData.cameraTargetDescriptor : cameraData.camera.activeTexture.descriptor);
					desc.depthBufferBits = 0;
					desc.format = cameraData.cameraTargetDescriptor.graphicsFormat;
				}
				else
				{
					desc = renderData.activeColorTexture.GetDescriptor(renderGraph);
				}
				TextureHandle dest = renderGraph.CreateTexture(desc);
				passData.particles = m_particles;

				builder.SetRenderAttachment(dest, 0);
				builder.SetRenderAttachmentDepth(depth);
				builder.SetGlobalTextureAfterPass(dest, FluidShaderProperties._FluidScreenSpaceParticles);
				builder.AllowPassCulling(false);
				builder.SetRenderFunc((PassData data, RasterGraphContext ctx) =>
                {
					ctx.cmd.ClearRenderTarget(false, true, Color.clear);
					passData.particles.surfaceParticlesSystem.Render(ctx.cmd);
				});
			}
		}
#endif
		public void Dispose()
		{

		}
  
    }
}
#endif
