#if FLUIDFRENZY_RUNTIME_URP_SUPPORT
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

#if UNITY_6000_0_OR_NEWER
using UnityEngine.Rendering.RenderGraphModule;
#endif

namespace FluidFrenzy
{
	public class GPULODTraversePassURP : ScriptableRenderPass
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
		private Texture m_heightmap;
		private int m_iterations;
		public GPULODTraversePassURP(GPULODSurface surface)
		{
			renderPassEvent = RenderPassEvent.BeforeRenderingShadows - 1;
			m_surface = surface;
		}

		public void SetRenderData(Transform transform, Texture heightmap, int traverseIterations = 1)
		{
			m_transform = transform;
			m_heightmap  = heightmap;
			m_iterations = traverseIterations;
		}

#if UNITY_6000_0_OR_NEWER
		[System.Obsolete]
#endif
		public override void Execute(ScriptableRenderContext context,
			ref RenderingData renderingData)
		{
			//Get a CommandBuffer from pool.
			CommandBuffer cmd = CommandBufferPool.Get();
			
			//Traverse the quadtree
			m_surface.Traverse(cmd, m_transform, renderingData.cameraData.camera, m_heightmap, m_iterations);

			//Execute the command buffer and release it back to the pool.
			context.ExecuteCommandBuffer(cmd);
			CommandBufferPool.Release(cmd);
		}

#if UNITY_6000_0_OR_NEWER
		public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
		{
			using (var builder = renderGraph.AddUnsafePass<PassData>("GPULOD Traverse Quadtree", out PassData passData))
			{
				passData.transform = m_transform;
				passData.heightmap = m_heightmap;
				passData.iterations = m_iterations;

				UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
				passData.camera = cameraData.camera;
				passData.surface = m_surface;

				builder.AllowPassCulling(false);
				builder.SetRenderFunc((PassData data, UnsafeGraphContext ctx) =>
                {
					passData.surface.Traverse(CommandBufferHelpers.GetNativeCommandBuffer(ctx.cmd), data.transform, data.camera, data.heightmap, data.iterations);
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
