#if FLUIDFRENZY_RUNTIME_HDRP_SUPPORT
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Experimental.Rendering;

namespace FluidFrenzy
{
	public class FluidRefractionHDRP : CustomPass
	{
		private RTHandle m_RefractionHandle;

		protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd)
		{
			m_RefractionHandle = RTHandles.Alloc(
				scaleFactor: Vector2.one,
				slices: TextureXR.slices, dimension: TextureXR.dimension,
				filterMode: FilterMode.Bilinear,
				colorFormat: GraphicsFormat.B10G11R11_UFloatPack32,
				useDynamicScale: true, name: "_FluidRefraction"
			);
		}

		protected override void Execute(CustomPassContext ctx)
		{
			if (ctx.hdCamera.camera.cameraType == CameraType.Preview) return;
			if (!FluidRenderPipeline.IsPipelineCamera(ctx.hdCamera.camera)) return;

			HDUtils.BlitCameraTexture(ctx.cmd, ctx.cameraColorBuffer, m_RefractionHandle);

			ctx.cmd.SetGlobalTexture(FluidRenderPipeline.RefractionID, m_RefractionHandle);
		}

		protected override void Cleanup()
		{
			RTHandles.Release(m_RefractionHandle);
		}
	}
}
#endif