using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace FluidFrenzy
{
	public static class FluidPrePass
	{
		private static readonly MaterialPropertyBlock s_Props = new MaterialPropertyBlock();

		public static void Execute(CommandBuffer cmd, Camera camera, HashSet<WaterSurface> surfaces, RenderTargetIdentifier maskTarget, RenderTargetIdentifier depthTarget)
		{
			cmd.ClearRenderTarget(true, true, Color.black);

			Material mat = FluidRenderPipeline.GetPrePassMaterial();
			if (mat == null) return;

			// Mesh Pass
			foreach (var surface in surfaces)
			{
				if (!surface.isActiveAndEnabled) continue;
				if ((camera.cullingMask & (1 << surface.gameObject.layer)) == 0) continue;

				s_Props.Clear();
				FluidRenderPipeline.UpdateSurfaceProperties(s_Props, surface);

				// Front Faces (Cull Back)
				surface.surfaceRenderer.Render(cmd, surface.transform.localToWorldMatrix, mat, s_Props, 0);

				// Back Faces (Cull Front)
				surface.surfaceRenderer.Render(cmd, surface.transform.localToWorldMatrix, mat, s_Props, 1);
			}

			// Volume pass
			WaterSurface activeSurface = FluidRenderPipeline.GetSurfaceCameraIsInside(camera, surfaces);
			if (activeSurface != null)
			{
				FluidRenderPipeline.UpdateMaterialProperties(camera, activeSurface, mat);

				cmd.SetGlobalTexture(FluidRenderPipeline.FluidDepthID, depthTarget);
				cmd.SetRenderTarget(maskTarget, RenderBufferLoadAction.Load, RenderBufferStoreAction.Store);

				s_Props.Clear();
				// Volume Fallback (Full Screen)
				CoreUtils.DrawFullScreen(cmd, mat, s_Props, 2);
			}

			cmd.SetGlobalTexture(FluidRenderPipeline.FluidMaskID, maskTarget);
			cmd.SetGlobalTexture(FluidRenderPipeline.FluidDepthID, depthTarget);
		}
	}
}