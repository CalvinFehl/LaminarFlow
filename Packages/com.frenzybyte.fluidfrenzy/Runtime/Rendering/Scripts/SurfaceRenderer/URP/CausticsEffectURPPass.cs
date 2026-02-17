#if FLUIDFRENZY_RUNTIME_URP_SUPPORT
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Experimental.Rendering;
using System;

#if UNITY_6000_0_OR_NEWER
using UnityEngine.Rendering.RenderGraphModule;
#endif

namespace FluidFrenzy
{
	public class CausticsEffectURPPass : ScriptableRenderPass
	{
		public CausticsEffectURPPass()
		{
			// Caustics should render after opaque geometry but before transparency
			// to ensure they project onto the sea floor but are visible through the water surface.
			renderPassEvent = RenderPassEvent.AfterRenderingOpaques; 
		}

#if UNITY_6000_0_OR_NEWER
		private class PassData
		{
			internal Camera camera;
		}

		public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
		{
			UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
			UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
			TextureHandle color = resourceData.activeColorTexture;

			using (var builder = renderGraph.AddUnsafePass<PassData>("Fluid Caustics", out var data))
			{
				data.camera = cameraData.camera;

				builder.UseTexture(color, AccessFlags.ReadWrite);

				builder.SetRenderFunc((PassData d, UnsafeGraphContext ctx) =>
				{
					var cmd = CommandBufferHelpers.GetNativeCommandBuffer(ctx.cmd);
					
					foreach (var surface in FluidRenderPipeline.Surfaces)
					{
						if (!surface.isActiveAndEnabled) continue;
						CausticsEffect.Render(cmd, d.camera, surface);
					}
				});
			}
		}
#endif

#if UNITY_6000_0_OR_NEWER
		[Obsolete]
#endif
		public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
		{
			var cmd = CommandBufferPool.Get("Fluid Caustics");

			foreach (var surface in FluidRenderPipeline.Surfaces)
			{
				if (!surface.isActiveAndEnabled) continue;
				CausticsEffect.Render(cmd, renderingData.cameraData.camera, surface);
			}

			context.ExecuteCommandBuffer(cmd);
			CommandBufferPool.Release(cmd);
		}
	}
}
#endif