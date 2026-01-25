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
		private UnderwaterEffect.UnderwaterSettings m_settings;
		private WaterSurface m_surface;
		private Material m_material;
		private MaterialPropertyBlock m_props;
		private UnderwaterShared.PassData m_passes;
		private bool m_debug;

		// RTHandles for Legacy Execution
		private RTHandle m_FluidMaskHandle;
		private RTHandle m_FluidDepthHandle;
		private RTHandle m_MeniscusHandle;
		private RTHandle m_ScreenCopyHandle;

		public UnderwaterEffectURPPass(UnderwaterEffect.UnderwaterSettings settings, WaterSurface surface, Material material, UnderwaterShared.PassData passes, bool debug)
		{
			renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
			m_settings = settings;
			m_surface = surface;
			m_material = material;
			m_passes = passes;
			m_debug = debug;
			m_props = new MaterialPropertyBlock();
		}

		public void SetDebug(bool debug) => m_debug = debug;

		// Legacy Setup (Non RenderGraph)
#if UNITY_6000_0_OR_NEWER
		[Obsolete]
#endif
		public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
		{
			m_FluidMaskHandle = RTHandles.Alloc(
				scaleFactor: Vector2.one,
				slices: 1, dimension: TextureDimension.Tex2D,
				filterMode: FilterMode.Point,
				colorFormat: GraphicsFormat.R8_UNorm,
				useDynamicScale: true, name: "_FluidMaskRT"
			);

			m_FluidDepthHandle = RTHandles.Alloc(
				scaleFactor: Vector2.one,
				slices: 1, dimension: TextureDimension.Tex2D,
				filterMode: FilterMode.Point,
				colorFormat: GraphicsFormat.D24_UNorm_S8_UInt,
				useDynamicScale: true, name: "_FluidDepthRT"
			);

			m_MeniscusHandle = RTHandles.Alloc(
				scaleFactor: Vector2.one,
				slices: 1, dimension: TextureDimension.Tex2D,
				filterMode: FilterMode.Bilinear,
				colorFormat: GraphicsFormat.R8_UNorm,
				useDynamicScale: true, name: "_MeniscusMaskRT"
			);

			m_ScreenCopyHandle = RTHandles.Alloc(
				scaleFactor: Vector2.one,
				slices: 1, dimension: TextureDimension.Tex2D,
				filterMode: FilterMode.Bilinear,
				colorFormat: GraphicsFormat.B10G11R11_UFloatPack32,
				useDynamicScale: true, name: "_ScreenCopyTexture"
			);
		}

		// RenderGraph Path (Unity 6+)
#if UNITY_6000_0_OR_NEWER
		private class PassData
		{
			internal WaterSurface surface;
			internal Material mat;
			internal MaterialPropertyBlock props;
			internal UnderwaterShared.PassData passIndices;
			internal TextureHandle mask;
			internal TextureHandle depth;
			internal TextureHandle meniscus;
			internal TextureHandle copy;
			internal TextureHandle cameraColor;
			internal bool debug;
		}

		public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
		{
			if (m_surface == null || m_material == null) return;

			UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
			UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
			TextureHandle source = resourceData.activeColorTexture;

			var desc = cameraData.cameraTargetDescriptor;
			int w = desc.width;
			int h = desc.height;

			// Standard 2D Textures
			TextureDesc maskDesc = new TextureDesc(w, h) { colorFormat = GraphicsFormat.R8_UNorm, name = "FluidMaskRT", dimension = TextureDimension.Tex2D };
			TextureDesc depthDesc = new TextureDesc(w, h) { depthBufferBits = DepthBits.Depth24, name = "FluidDepthRT", dimension = TextureDimension.Tex2D };
			TextureDesc meniscusDesc = new TextureDesc(w, h) { colorFormat = GraphicsFormat.R8_UNorm, name = "MeniscusMaskRT", dimension = TextureDimension.Tex2D };
			TextureDesc copyDesc = new TextureDesc(w, h) { colorFormat = GraphicsFormat.B10G11R11_UFloatPack32, name = "ScreenCopyTexture", dimension = TextureDimension.Tex2D };

			TextureHandle fluidMask = renderGraph.CreateTexture(maskDesc);
			TextureHandle fluidDepth = renderGraph.CreateTexture(depthDesc);
			TextureHandle meniscusMask = renderGraph.CreateTexture(meniscusDesc);
			TextureHandle screenCopy = renderGraph.CreateTexture(copyDesc);

			UnderwaterShared.SetScreenSizeParam(m_props, cameraData.camera);

			// 1. Mask
			using (var builder = renderGraph.AddUnsafePass<PassData>("Underwater: Mask", out var data))
			{
				data.surface = m_surface; data.mat = m_material; data.props = m_props; data.passIndices = m_passes;
				data.mask = fluidMask; data.depth = fluidDepth;

				builder.UseTexture(fluidMask, AccessFlags.Write);
				builder.UseTexture(fluidDepth, AccessFlags.Write);
				builder.AllowPassCulling(false);

				builder.SetRenderFunc((PassData d, UnsafeGraphContext ctx) =>
				{
					var cmd = CommandBufferHelpers.GetNativeCommandBuffer(ctx.cmd);
					UnderwaterShared.RenderFluidMask(cmd, d.surface, d.mat, d.props, d.passIndices.mask, d.mask, d.depth);
				});
			}

			// Volume Fallback
			using (var builder = renderGraph.AddUnsafePass<PassData>("Underwater: Fallback", out var data))
			{
				data.mat = m_material; data.props = m_props; data.passIndices = m_passes;
				data.mask = fluidMask; data.depth = fluidDepth;

				builder.UseTexture(fluidMask, AccessFlags.ReadWrite);
				builder.UseTexture(fluidDepth, AccessFlags.Read);

				builder.SetRenderFunc((PassData d, UnsafeGraphContext ctx) =>
				{
					var cmd = CommandBufferHelpers.GetNativeCommandBuffer(ctx.cmd);
					d.props.SetTexture(UnderwaterShared._FluidDepthRT, d.depth);
					UnderwaterShared.RenderFullScreenPass(cmd, d.mat, d.props, d.passIndices.fallback, d.mask, null, false);
				});
			}

			// Meniscus Blur
			using (var builder = renderGraph.AddUnsafePass<PassData>("Underwater: Meniscus", out var data))
			{
				data.mat = m_material; data.props = m_props; data.passIndices = m_passes;
				data.mask = fluidMask; data.meniscus = meniscusMask;

				builder.UseTexture(fluidMask, AccessFlags.Read);
				builder.UseTexture(meniscusMask, AccessFlags.Write);

				builder.SetRenderFunc((PassData d, UnsafeGraphContext ctx) =>
				{
					var cmd = CommandBufferHelpers.GetNativeCommandBuffer(ctx.cmd);
					d.props.SetTexture(UnderwaterShared._FluidMaskRT, d.mask);
					UnderwaterShared.RenderFullScreenPass(cmd, d.mat, d.props, d.passIndices.meniscus, d.meniscus, null, true);
				});
			}

			// Composite
			using (var builder = renderGraph.AddUnsafePass<PassData>("Underwater: Composite", out var data))
			{
				data.mat = m_material; data.props = m_props; data.passIndices = m_passes; data.debug = m_debug;
				data.cameraColor = source; data.copy = screenCopy; data.mask = fluidMask; data.depth = fluidDepth; data.meniscus = meniscusMask;

				builder.UseTexture(source, AccessFlags.ReadWrite);
				builder.UseTexture(screenCopy, AccessFlags.Write);
				builder.UseTexture(fluidMask, AccessFlags.Read);
				builder.UseTexture(fluidDepth, AccessFlags.Read);
				builder.UseTexture(meniscusMask, AccessFlags.Read);

				builder.SetRenderFunc((PassData d, UnsafeGraphContext ctx) =>
				{
					var cmd = CommandBufferHelpers.GetNativeCommandBuffer(ctx.cmd);

					Blitter.BlitCameraTexture(cmd, d.cameraColor, d.copy);

					d.props.SetTexture(UnderwaterShared._FluidDepthRT, d.depth);
					d.props.SetTexture(UnderwaterShared._FluidMaskRT, d.mask);
					d.props.SetTexture(UnderwaterShared._MeniscusMaskRT, d.meniscus);
					d.props.SetTexture(UnderwaterShared._ScreenCopyTexture, d.copy);

					int finalPass = d.debug ? d.passIndices.debug : d.passIndices.composite;
					UnderwaterShared.RenderFullScreenPass(cmd, d.mat, d.props, finalPass, d.cameraColor, null, false);
				});
			}
		}
#endif

		// Legacy Execute Path (Unity 2022 or RenderGraph Disabled)
#if UNITY_6000_0_OR_NEWER
		[Obsolete]
#endif
		public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
		{
			if (m_surface == null || m_material == null) return;

			CommandBuffer cmd = CommandBufferPool.Get("UnderwaterEffect");
			Camera camera = renderingData.cameraData.camera;

			RTHandle cameraTargetHandle = null;
			RenderTargetIdentifier cameraTargetId = BuiltinRenderTextureType.CameraTarget;

#if UNITY_6000_0_OR_NEWER
			cameraTargetHandle = renderingData.cameraData.renderer.cameraColorTargetHandle;
			cameraTargetId = cameraTargetHandle;
#else
			cameraTargetId = renderingData.cameraData.renderer.cameraColorTarget;
#endif

			m_props.Clear();
			UnderwaterShared.SetScreenSizeParam(m_props, camera);

			// Mesh Mask 
			UnderwaterShared.RenderFluidMask(cmd, m_surface, m_material, m_props, m_passes.mask, m_FluidMaskHandle, m_FluidDepthHandle);

			// Volume Fallback
			m_props.SetTexture(UnderwaterShared._FluidDepthRT, m_FluidDepthHandle);
			UnderwaterShared.RenderFullScreenPass(cmd, m_material, m_props, m_passes.fallback, m_FluidMaskHandle, null, false);

			// Meniscus
			m_props.Clear();
			UnderwaterShared.SetScreenSizeParam(m_props, camera);
			m_props.SetTexture(UnderwaterShared._FluidMaskRT, m_FluidMaskHandle);
			UnderwaterShared.RenderFullScreenPass(cmd, m_material, m_props, m_passes.meniscus, m_MeniscusHandle, null, true);

			// Copy Screen
			if (cameraTargetHandle != null)
			{
				Blitter.BlitCameraTexture(cmd, cameraTargetHandle, m_ScreenCopyHandle);
			}
			else
			{
				cmd.Blit(cameraTargetId, m_ScreenCopyHandle);
			}

			// Composite
			m_props.Clear();
			UnderwaterShared.SetScreenSizeParam(m_props, camera);
			m_props.SetTexture(UnderwaterShared._FluidDepthRT, m_FluidDepthHandle);
			m_props.SetTexture(UnderwaterShared._FluidMaskRT, m_FluidMaskHandle);
			m_props.SetTexture(UnderwaterShared._MeniscusMaskRT, m_MeniscusHandle);
			m_props.SetTexture(UnderwaterShared._ScreenCopyTexture, m_ScreenCopyHandle);

			int finalPass = m_debug ? m_passes.debug : m_passes.composite;
			UnderwaterShared.RenderFullScreenPass(cmd, m_material, m_props, finalPass, cameraTargetId, null, false);

			context.ExecuteCommandBuffer(cmd);
			CommandBufferPool.Release(cmd);
		}

		public override void OnCameraCleanup(CommandBuffer cmd)
		{
			if (m_FluidMaskHandle != null) RTHandles.Release(m_FluidMaskHandle);
			if (m_FluidDepthHandle != null) RTHandles.Release(m_FluidDepthHandle);
			if (m_MeniscusHandle != null) RTHandles.Release(m_MeniscusHandle);
			if (m_ScreenCopyHandle != null) RTHandles.Release(m_ScreenCopyHandle);
		}
	}
}
#endif