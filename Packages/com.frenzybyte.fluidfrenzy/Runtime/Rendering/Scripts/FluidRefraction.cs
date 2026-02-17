using UnityEngine;
using UnityEngine.Rendering;

namespace FluidFrenzy
{
	public static class FluidRefraction
	{
		public static void Execute(CommandBuffer cmd, RenderTargetIdentifier source, int destID)
		{
			cmd.GetTemporaryRT(destID, -1, -1, 0, FilterMode.Bilinear, RenderTextureFormat.Default);
			cmd.Blit(source, destID);
			cmd.SetGlobalTexture(FluidRenderPipeline.RefractionID, destID);
		}
	}
}