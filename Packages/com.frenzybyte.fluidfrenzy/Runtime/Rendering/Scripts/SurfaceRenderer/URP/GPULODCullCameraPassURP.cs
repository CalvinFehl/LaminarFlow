#if FLUIDFRENZY_RUNTIME_URP_SUPPORT
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
#if UNITY_6000_0_OR_NEWER
using UnityEngine.Rendering.RenderGraphModule;
#endif

namespace FluidFrenzy
{
	public class GPULODCullCameraPassURP : ScriptableRenderPass
    {
		public class PassData
		{
			internal Transform transform;
			internal Texture heightmap;
			internal int iterations;
			internal Camera camera;
			internal GPULODSurface surface;
		}

		private GPULODSurface m_surface;

		private Transform m_transform;
		public GPULODCullCameraPassURP(GPULODSurface surface)
		{
#if UNITY_2021_1_OR_NEWER
			renderPassEvent = RenderPassEvent.BeforeRenderingPrePasses;
#else
			renderPassEvent = RenderPassEvent.BeforeRenderingPrepasses;
#endif
			m_surface = surface;
		}

		public void SetTransform(Transform transform)
		{
			m_transform = transform;
		}

#if UNITY_6000_0_OR_NEWER
		[System.Obsolete]
#endif
		public override void Execute(ScriptableRenderContext context,
			ref RenderingData renderingData)
		{
			//Get a CommandBuffer from pool.
			CommandBuffer cmd = CommandBufferPool.Get();

			m_surface.Cull(cmd, m_transform, renderingData.cameraData.camera);

			//Execute the command buffer and release it back to the pool.
			context.ExecuteCommandBuffer(cmd);
			CommandBufferPool.Release(cmd);
		}

#if UNITY_6000_0_OR_NEWER
		public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
		{
			using (var builder = renderGraph.AddUnsafePass<PassData>("GPULOD Cull Quadtree", out PassData passData))
			{
				passData.transform = m_transform;

				UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
				passData.camera = cameraData.camera;
				passData.surface = m_surface;

				builder.AllowPassCulling(false);
				builder.SetRenderFunc((PassData data, UnsafeGraphContext ctx) =>
				{
					passData.surface.Cull(CommandBufferHelpers.GetNativeCommandBuffer(ctx.cmd), data.transform, data.camera);
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
