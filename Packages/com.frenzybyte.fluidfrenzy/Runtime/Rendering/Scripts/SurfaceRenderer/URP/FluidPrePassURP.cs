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
#if UNITY_6000_0_OR_NEWER
	public class FluidFrameData : ContextItem
	{
		public TextureHandle fluidMask;
		public TextureHandle fluidDepth;

		public override void Reset()
		{
			fluidMask = TextureHandle.nullHandle;
			fluidDepth = TextureHandle.nullHandle;
		}
	}
#endif

	public class FluidPrePassURP : ScriptableRenderPass
	{
		public FluidPrePassURP()
		{
			renderPassEvent = RenderPassEvent.BeforeRenderingTransparents;
		}

#if UNITY_6000_0_OR_NEWER
		private class PassData
		{
			internal Camera camera;
			internal TextureHandle mask;
			internal TextureHandle depth;
		}

		public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
		{
			UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

			TextureDesc maskDesc = new TextureDesc(cameraData.camera.pixelWidth, cameraData.camera.pixelHeight);
			maskDesc.colorFormat = GraphicsFormat.R8_UNorm;
			maskDesc.name = "FluidMaskRT";

			TextureDesc depthDesc = new TextureDesc(cameraData.camera.pixelWidth, cameraData.camera.pixelHeight);
			depthDesc.colorFormat = GraphicsFormat.None;
			depthDesc.depthBufferBits = DepthBits.Depth24;
			depthDesc.name = "FluidDepthRT";

			TextureHandle mask = renderGraph.CreateTexture(maskDesc);
			TextureHandle depth = renderGraph.CreateTexture(depthDesc);

			FluidFrameData fluidData = frameData.Create<FluidFrameData>();
			fluidData.fluidMask = mask;
			fluidData.fluidDepth = depth;

			using (var builder = renderGraph.AddUnsafePass<PassData>("Fluid PrePass", out var data))
			{
				data.camera = cameraData.camera;
				data.mask = mask;
				data.depth = depth;

				builder.UseTexture(mask, AccessFlags.Write);
				builder.UseTexture(depth, AccessFlags.Write);
				builder.AllowPassCulling(false);

				builder.SetRenderFunc((PassData d, UnsafeGraphContext ctx) =>
				{
					var cmd = CommandBufferHelpers.GetNativeCommandBuffer(ctx.cmd);
                    
					cmd.SetRenderTarget(d.mask, d.depth);
                    
					// Call Shared Logic
					FluidPrePass.Execute(cmd, d.camera, FluidRenderPipeline.Surfaces, d.mask, d.depth);

					cmd.SetGlobalTexture(FluidRenderPipeline.FluidMaskID, d.mask);
					cmd.SetGlobalTexture(FluidRenderPipeline.FluidDepthID, d.depth);
				});
			}
		}
#endif
		private RTHandle m_MaskHandle;
		private RTHandle m_DepthHandle;

#if UNITY_6000_0_OR_NEWER
		[Obsolete]
#endif
		public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
		{
			var desc = renderingData.cameraData.cameraTargetDescriptor;
			desc.colorFormat = RenderTextureFormat.R8;
			desc.depthBufferBits = 0;
			desc.msaaSamples = 1;
			RenderingUtils.ReAllocateIfNeeded(ref m_MaskHandle, desc, FilterMode.Point, TextureWrapMode.Clamp, name: "_FluidMaskRT");

			desc.colorFormat = RenderTextureFormat.Depth;
			desc.depthBufferBits = 24;
			RenderingUtils.ReAllocateIfNeeded(ref m_DepthHandle, desc, FilterMode.Point, TextureWrapMode.Clamp, name: "_FluidDepthRT");
		}

#if UNITY_6000_0_OR_NEWER
		[Obsolete]
#endif
		public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
		{
			var cmd = CommandBufferPool.Get("Fluid PrePass");
			CoreUtils.SetRenderTarget(cmd, m_MaskHandle, m_DepthHandle, ClearFlag.All, Color.black);
            
			// Call Shared Logic
			FluidPrePass.Execute(cmd, renderingData.cameraData.camera, FluidRenderPipeline.Surfaces, m_MaskHandle, m_DepthHandle);
            
			cmd.SetGlobalTexture(FluidRenderPipeline.FluidMaskID, m_MaskHandle);
			cmd.SetGlobalTexture(FluidRenderPipeline.FluidDepthID, m_DepthHandle);
            
			context.ExecuteCommandBuffer(cmd);
			CommandBufferPool.Release(cmd);
		}
	}
}
#endif