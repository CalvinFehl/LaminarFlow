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
		// RTHandles with TextureXR support for VR
		private RTHandle m_MeniscusHandle;
		private RTHandle m_ScreenCopyHandle;

		protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd)
		{
			targetColorBuffer = CustomPass.TargetBuffer.Camera;
			targetDepthBuffer = CustomPass.TargetBuffer.None;

			// Allocate using TextureXR settings
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
			if (ctx.hdCamera.camera.cameraType == CameraType.Preview) return;
			if (!FluidRenderPipeline.IsPipelineCamera(ctx.hdCamera.camera)) return;
			
			// Only execute if needed
			if (!FluidRenderPipeline.ShouldRenderUnderwater(ctx.hdCamera.camera)) return;

			HDUtils.BlitCameraTexture(ctx.cmd, ctx.cameraColorBuffer, m_ScreenCopyHandle);

			UnderwaterEffect.Render(ctx.cmd, ctx.hdCamera.camera, FluidRenderPipeline.Surfaces,
				m_ScreenCopyHandle,
				ctx.cameraColorBuffer,
				m_MeniscusHandle,
				m_ScreenCopyHandle
			);
		}

		protected override void Cleanup()
		{
			RTHandles.Release(m_MeniscusHandle);
			RTHandles.Release(m_ScreenCopyHandle);
		}
	}
}
#endif