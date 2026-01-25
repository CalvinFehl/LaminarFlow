using System.IO;
using UnityEngine;
using UnityEngine.Rendering;

namespace FluidFrenzy
{
	using static FluidFrenzy.SimulationIO;

	public partial class FluidSimulation : MonoBehaviour
	{
		public virtual void Save(string directory, string filename)
		{
			RenderTextureDescriptor desc = m_activeWaterHeight.descriptor;
			desc.colorFormat = RenderTextureFormat.ARGBHalf;
			desc.sRGB = false;
			RenderTexture tempRT = RenderTexture.GetTemporary(desc);
			CommandBuffer commandBuffer = new CommandBuffer();

			commandBuffer.Blit(m_activeWaterHeight, tempRT);
			Graphics.ExecuteCommandBuffer(commandBuffer);

#if UNITY_2021_1_OR_NEWER
			SimulationIO.SaveTexture(tempRT, Path.Join(directory, filename), FileFormat.RAW);
#endif
			RenderTexture.ReleaseTemporary(tempRT);
		}

		public virtual void Load(string directory, string filename)
		{
#if UNITY_2021_1_OR_NEWER
			Texture2D texture = SimulationIO.LoadTexture(Path.Join(directory, filename));
			Graphics.Blit(texture, m_activeWaterHeight);
			Graphics.Blit(texture, m_nextWaterHeight);
#endif
		}
	}
}
