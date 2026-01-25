#if FLUIDFRENZY_RUNTIME_URP_SUPPORT
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
#if UNITY_6000_0_OR_NEWER
using UnityEngine.Rendering.RenderGraphModule;
#endif

namespace FluidFrenzy
{
	public class GPULODCullShadowPassURP : ScriptableRenderPass
    {
		public class PassData
		{
			internal GPULODSurface surface;
		}

		private GPULODSurface m_surface;

		public GPULODCullShadowPassURP(GPULODSurface surface)
		{
			renderPassEvent = RenderPassEvent.BeforeRenderingShadows;
			m_surface = surface;
		}

#if UNITY_6000_0_OR_NEWER
		[System.Obsolete]
#endif
		public override void Execute(ScriptableRenderContext context,
			ref RenderingData renderingData)
		{
			//Get a CommandBuffer from pool.
			CommandBuffer cmd = CommandBufferPool.Get();


			//Don't do any culling, there is no guarantee this works on other lights than main directional light. To support this a custom light pass needs to be done for these meshes.
			m_surface.FillRenderBufferNoCull(cmd);

			//m_surface.CullShadowLight(cmd, m_transform, renderingData.cameraData.camera, renderingData.lightData.visibleLights[renderingData.lightData.mainLightIndex].light);

			//Execute the command buffer and release it back to the pool.
			context.ExecuteCommandBuffer(cmd);
			CommandBufferPool.Release(cmd);
		}

#if UNITY_6000_0_OR_NEWER
		public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
		{
			using (var builder = renderGraph.AddUnsafePass<PassData>("GPULOD Cull Shadow Pass", out PassData passData))
			{
				UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
				passData.surface = m_surface;

				builder.AllowPassCulling(false);
				builder.SetRenderFunc((PassData data, UnsafeGraphContext ctx) =>
				{
					passData.surface.FillRenderBufferNoCull(CommandBufferHelpers.GetNativeCommandBuffer(ctx.cmd));
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
