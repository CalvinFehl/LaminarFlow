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
	public class FluidRefractionURPPass : ScriptableRenderPass
	{
		public FluidRefractionURPPass()
		{
			renderPassEvent = RenderPassEvent.BeforeRenderingTransparents;
		}

#if UNITY_6000_0_OR_NEWER
		private class PassData 
		{ 
			internal TextureHandle source;
			internal TextureHandle dest;
		}

		public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
		{
			UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
			UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

			TextureDesc desc = new TextureDesc(cameraData.camera.pixelWidth, cameraData.camera.pixelHeight);
			desc.colorFormat = GraphicsFormat.B10G11R11_UFloatPack32;
			desc.name = "_FluidRefraction";

			TextureHandle dest = renderGraph.CreateTexture(desc);
			TextureHandle source = resourceData.activeColorTexture;

			using (var builder = renderGraph.AddRasterRenderPass<PassData>("Fluid Refraction", out var data))
			{
				data.source = source;
				data.dest = dest;

				builder.UseTexture(source, AccessFlags.Read);
				builder.SetRenderAttachment(dest, 0);
				builder.SetGlobalTextureAfterPass(dest, FluidRenderPipeline.RefractionID);

				builder.SetRenderFunc((PassData d, RasterGraphContext ctx) =>
				{
					Blitter.BlitTexture(ctx.cmd, d.source, new Vector4(1,1,0,0), 0, false);
				});
			}
		}
#endif
		RTHandle m_RefractionHandle;

#if UNITY_6000_0_OR_NEWER
		[Obsolete]
#endif
		public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
		{
			var desc = renderingData.cameraData.cameraTargetDescriptor;
			desc.depthBufferBits = 0;
			RenderingUtils.ReAllocateIfNeeded(ref m_RefractionHandle, desc, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_FluidRefraction");
		}

#if UNITY_6000_0_OR_NEWER
		[Obsolete]
#endif
		public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
		{
			var cmd = CommandBufferPool.Get("Fluid Refraction");
			RTHandle source = renderingData.cameraData.renderer.cameraColorTargetHandle;

			Blitter.BlitCameraTexture(cmd, source, m_RefractionHandle);
			cmd.SetGlobalTexture(FluidRenderPipeline.RefractionID, m_RefractionHandle);

			context.ExecuteCommandBuffer(cmd);
			CommandBufferPool.Release(cmd);
		}
	}
}
#endif