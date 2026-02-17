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
	public class UnderwaterEffectURPPass : ScriptableRenderPass
	{
		public UnderwaterEffectURPPass()
		{
			renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
		}

#if UNITY_6000_0_OR_NEWER
		private class PassData
		{
			internal Camera camera;
			internal TextureHandle source;
			internal TextureHandle mask;
			internal TextureHandle depth;
			internal TextureHandle meniscus;
			internal TextureHandle copy;
		}

		public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
		{
			// Check if we have the necessary data from PrePass. If not, don't run.
			if (!frameData.Contains<FluidFrameData>()) return;

			UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
			UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
			TextureHandle source = resourceData.activeColorTexture;

			FluidFrameData fluidData = frameData.Get<FluidFrameData>();
			TextureHandle fluidMask = fluidData.fluidMask;
			TextureHandle fluidDepth = fluidData.fluidDepth;

			if (!fluidMask.IsValid() || !fluidDepth.IsValid()) return;

			TextureDesc desc = new TextureDesc(cameraData.camera.pixelWidth, cameraData.camera.pixelHeight);
			desc.colorFormat = GraphicsFormat.R8_UNorm;
			desc.name = "_MeniscusMaskRT";
			TextureHandle meniscus = renderGraph.CreateTexture(desc);

			desc.colorFormat = GraphicsFormat.B10G11R11_UFloatPack32;
			desc.name = "_ScreenCopyTexture";
			TextureHandle copy = renderGraph.CreateTexture(desc);

			using (var builder = renderGraph.AddUnsafePass<PassData>("Fluid Underwater", out var data))
			{
				data.camera = cameraData.camera;
				data.source = source;
				data.mask = fluidMask;
				data.depth = fluidDepth;
				data.meniscus = meniscus;
				data.copy = copy;

				builder.UseTexture(source, AccessFlags.ReadWrite);
				builder.UseTexture(fluidMask, AccessFlags.Read);
				builder.UseTexture(fluidDepth, AccessFlags.Read);
				builder.UseTexture(meniscus, AccessFlags.Write);
				builder.UseTexture(copy, AccessFlags.Write);

				builder.SetRenderFunc((PassData d, UnsafeGraphContext ctx) =>
				{
					var cmd = CommandBufferHelpers.GetNativeCommandBuffer(ctx.cmd);
					// Call Shared Logic
					UnderwaterEffect.Render(cmd, d.camera, FluidRenderPipeline.Surfaces, d.source, d.source, d.meniscus, d.copy);
				});
			}
		}
#endif

		RTHandle m_MeniscusHandle;
		RTHandle m_CopyHandle;

#if UNITY_6000_0_OR_NEWER
		[Obsolete]
#endif
		public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
		{
			var desc = renderingData.cameraData.cameraTargetDescriptor;
			desc.depthBufferBits = 0;

			RenderingUtils.ReAllocateIfNeeded(ref m_MeniscusHandle, desc, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_MeniscusMaskRT");
			RenderingUtils.ReAllocateIfNeeded(ref m_CopyHandle, desc, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_ScreenCopyTexture");
		}

#if UNITY_6000_0_OR_NEWER
		[Obsolete]
#endif
		public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
		{
			var cmd = CommandBufferPool.Get("Fluid Underwater");

			RTHandle source = renderingData.cameraData.renderer.cameraColorTargetHandle;

			// Call Shared Logic
			UnderwaterEffect.Render(cmd, renderingData.cameraData.camera, FluidRenderPipeline.Surfaces,
				source, source,
				m_MeniscusHandle, m_CopyHandle);

			context.ExecuteCommandBuffer(cmd);
			CommandBufferPool.Release(cmd);
		}
	}
}
#endif