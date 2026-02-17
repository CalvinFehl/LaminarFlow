#if FLUIDFRENZY_RUNTIME_HDRP_SUPPORT
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Experimental.Rendering;

namespace FluidFrenzy
{
	public class FluidPrePassHDRP : CustomPass
	{
		private RTHandle m_MaskHandle;
		private RTHandle m_DepthHandle;

		protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd)
		{
			targetColorBuffer = CustomPass.TargetBuffer.None;
			targetDepthBuffer = CustomPass.TargetBuffer.None;

			// Allocate with TextureXR for VR support
			m_MaskHandle = RTHandles.Alloc(
				scaleFactor: Vector2.one,
				slices: TextureXR.slices, dimension: TextureXR.dimension,
				filterMode: FilterMode.Point,
				colorFormat: GraphicsFormat.R8_UNorm,
				useDynamicScale: true, name: "_FluidMaskRT"
			);

			m_DepthHandle = RTHandles.Alloc(
				scaleFactor: Vector2.one,
				slices: TextureXR.slices, dimension: TextureXR.dimension,
				filterMode: FilterMode.Point,
				colorFormat: GraphicsFormat.D24_UNorm_S8_UInt, // Standard Depth
				useDynamicScale: true, name: "_FluidDepthRT"
			);
		}

		protected override void Execute(CustomPassContext ctx)
		{
			if (ctx.hdCamera.camera.cameraType == CameraType.Preview) return;
			if (!FluidRenderPipeline.IsPipelineCamera(ctx.hdCamera.camera)) return;
			
			// Only execute if needed
			if (!FluidRenderPipeline.ShouldRenderPrePass(ctx.hdCamera.camera)) return;

			CoreUtils.SetRenderTarget(ctx.cmd, m_MaskHandle, m_DepthHandle, ClearFlag.All, Color.black);

			// Execute Shared Logic
			FluidPrePass.Execute(ctx.cmd, ctx.hdCamera.camera, FluidRenderPipeline.Surfaces, m_MaskHandle, m_DepthHandle);

			ctx.cmd.SetGlobalTexture(FluidRenderPipeline.FluidMaskID, m_MaskHandle);
			ctx.cmd.SetGlobalTexture(FluidRenderPipeline.FluidDepthID, m_DepthHandle);
		}

		protected override void Cleanup()
		{
			RTHandles.Release(m_MaskHandle);
			RTHandles.Release(m_DepthHandle);
		}
	}
}
#endif