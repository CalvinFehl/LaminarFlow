#if FLUIDFRENZY_RUNTIME_HDRP_SUPPORT
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Experimental.Rendering;

namespace FluidFrenzy
{
	public class CausticsEffectHDRPPass : CustomPass
	{
		protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd)
		{
			// No RT allocation needed, draws directly to camera buffer
			targetColorBuffer = CustomPass.TargetBuffer.Camera;
			targetDepthBuffer = CustomPass.TargetBuffer.Camera;
		}

		protected override void Execute(CustomPassContext ctx)
		{
			if (ctx.hdCamera.camera.cameraType == CameraType.Preview) return;
			if (!FluidRenderPipeline.IsPipelineCamera(ctx.hdCamera.camera)) return;

			// Optimization: Check if caustics are actually enabled on any visible surface
			if (!FluidRenderPipeline.ShouldRenderCaustics(ctx.hdCamera.camera)) return;

			foreach (var surface in FluidRenderPipeline.Surfaces)
			{
				if (!surface.isActiveAndEnabled) continue;
				CausticsEffect.Render(ctx.cmd, ctx.hdCamera.camera, surface);
			}
		}

		protected override void Cleanup()
		{
			CausticsEffect.Cleanup();
		}
	}
}
#endif