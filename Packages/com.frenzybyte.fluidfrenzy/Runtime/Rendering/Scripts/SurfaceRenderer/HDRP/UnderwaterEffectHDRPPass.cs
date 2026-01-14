#if FLUIDFRENZY_RUNTIME_HDRP_SUPPORT
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Experimental.Rendering;

namespace FluidFrenzy
{
	[System.Serializable]
	public class UnderwaterEffectHDRPPass : CustomPass
	{
		public UnderwaterEffect.UnderwaterSettings settings;
		public WaterSurface surface;
		public Material material;
		public UnderwaterShared.PassData passIndices;
		public bool debug;

		private MaterialPropertyBlock m_props;

		// RTHandles with TextureXR support for VR
		private RTHandle m_FluidMaskHandle;
		private RTHandle m_FluidDepthHandle;
		private RTHandle m_MeniscusHandle;
		private RTHandle m_ScreenCopyHandle;

		protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd)
		{
			m_props = new MaterialPropertyBlock();

			targetColorBuffer = CustomPass.TargetBuffer.Camera;
			targetDepthBuffer = CustomPass.TargetBuffer.None;

			// Allocate using TextureXR settings
			m_FluidMaskHandle = RTHandles.Alloc(
				scaleFactor: Vector2.one,
				slices: TextureXR.slices, dimension: TextureXR.dimension,
				filterMode: FilterMode.Point,
				colorFormat: GraphicsFormat.R8_UNorm,
				useDynamicScale: true, name: "_FluidMaskRT"
			);

			m_FluidDepthHandle = RTHandles.Alloc(
				scaleFactor: Vector2.one,
				slices: TextureXR.slices, dimension: TextureXR.dimension,
				filterMode: FilterMode.Point,
				colorFormat: GraphicsFormat.D32_SFloat,
				useDynamicScale: true, name: "_FluidDepthRT"
			);

			m_MeniscusHandle = RTHandles.Alloc(
				scaleFactor: Vector2.one,
				slices: TextureXR.slices, dimension: TextureXR.dimension,
				filterMode: FilterMode.Bilinear,
				colorFormat: GraphicsFormat.R8_UNorm,
				useDynamicScale: true, name: "_MeniscusMaskRT"
			);

			m_ScreenCopyHandle = RTHandles.Alloc(
				scaleFactor: Vector2.one,
				slices: TextureXR.slices, dimension: TextureXR.dimension,
				filterMode: FilterMode.Bilinear,
				colorFormat: GraphicsFormat.B10G11R11_UFloatPack32,
				useDynamicScale: true, name: "_ScreenCopyTexture"
			);
		}

		protected override void Execute(CustomPassContext ctx)
		{
			if (material == null || surface == null || settings == null) return;
			if (ctx.hdCamera.camera.cameraType == CameraType.Preview) return;

			UnderwaterShared.UpdateMaterialProperties(ctx.hdCamera.camera, settings, surface, material);
			CommandBuffer cmd = ctx.cmd;
			m_props.Clear();

			// Fluid Mask (Mesh)
			UnderwaterShared.RenderFluidMask(cmd, surface, material, m_props, passIndices.mask, m_FluidMaskHandle, m_FluidDepthHandle);

			// Volume Fallback
			m_props.Clear();
			UnderwaterShared.SetScreenSizeParam(m_props, ctx.hdCamera.camera);
			m_props.SetTexture(UnderwaterShared._FluidDepthRT, m_FluidDepthHandle);

			UnderwaterShared.RenderFullScreenPass(cmd, material, m_props, passIndices.fallback, m_FluidMaskHandle, null, false);

			// Meniscus Blur
			m_props.Clear();
			UnderwaterShared.SetScreenSizeParam(m_props, ctx.hdCamera.camera);
			m_props.SetTexture(UnderwaterShared._FluidMaskRT, m_FluidMaskHandle);

			UnderwaterShared.RenderFullScreenPass(cmd, material, m_props, passIndices.meniscus, m_MeniscusHandle, null, true);

			// Copy Screen
			Blitter.BlitCameraTexture(cmd, ctx.cameraColorBuffer, m_ScreenCopyHandle);

			// Composite
			m_props.Clear();
			UnderwaterShared.SetScreenSizeParam(m_props, ctx.hdCamera.camera);
			m_props.SetTexture(UnderwaterShared._FluidDepthRT, m_FluidDepthHandle);
			m_props.SetTexture(UnderwaterShared._FluidMaskRT, m_FluidMaskHandle);
			m_props.SetTexture(UnderwaterShared._MeniscusMaskRT, m_MeniscusHandle);
			m_props.SetTexture(UnderwaterShared._ScreenCopyTexture, m_ScreenCopyHandle);

			int finalPass = debug ? passIndices.debug : passIndices.composite;
			UnderwaterShared.RenderFullScreenPass(cmd, material, m_props, finalPass, ctx.cameraColorBuffer, null, false);
		}

		protected override void Cleanup()
		{
			if (m_FluidMaskHandle != null) RTHandles.Release(m_FluidMaskHandle);
			if (m_FluidDepthHandle != null) RTHandles.Release(m_FluidDepthHandle);
			if (m_MeniscusHandle != null) RTHandles.Release(m_MeniscusHandle);
			if (m_ScreenCopyHandle != null) RTHandles.Release(m_ScreenCopyHandle);
		}
	}
}
#endif