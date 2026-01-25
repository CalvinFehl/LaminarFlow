using UnityEngine;
using UnityEngine.Rendering;

namespace FluidFrenzy
{
	/// <summary>
	/// <see cref="ShadowGrabber"/> is a component used for sampling shadows on transparent objects.
	/// Attach this component to a <see cref="Light"/> with it's <see cref="Light.type"/> set to <see cref="LightType.Directional"/>
	/// This will allow the <see cref="WaterSurface"/>/<see cref="LavaSurface"/> and its shader to read shadows while they are rendered after opaque geometry.
	/// </summary>
	public class ShadowGrabber : MonoBehaviour
	{
#if !FLUIDFRENZY_RUNTIME_URP_SUPPORT
		Light m_light;
		CommandBuffer m_commandBuffer;

#if UNITY_2021_1_OR_NEWER
		GlobalKeyword m_transparentShadowsCloseFitKeyword;
		GlobalKeyword m_transparentShadowsStableFitKeyword;
		GlobalKeyword m_shadowsSplitSphereskeyword;
#endif
		void Awake()
		{
			m_light = GetComponent<Light>();

			m_commandBuffer = new CommandBuffer();
			m_commandBuffer.name = "CopyShadowCascades";

			// Change shadow sampling mode for m_Light's shadowmap.
			m_commandBuffer.SetShadowSamplingMode(BuiltinRenderTextureType.CurrentActive, ShadowSamplingMode.RawDepth);
			m_commandBuffer.SetGlobalTexture(FluidShaderProperties._TransparentShadowMapTexture, BuiltinRenderTextureType.CurrentActive);

#if UNITY_2021_1_OR_NEWER
			m_transparentShadowsCloseFitKeyword = GlobalKeyword.Create("_TRANSPARENT_RECEIVE_SHADOWS_CLOSE_FIT");
			m_transparentShadowsStableFitKeyword = GlobalKeyword.Create("_TRANSPARENT_RECEIVE_SHADOWS_SPLIT_SPHERES");

			if (QualitySettings.shadowProjection == ShadowProjection.StableFit)
			{
				m_commandBuffer.EnableKeyword(m_transparentShadowsStableFitKeyword);
			}
			else
			{
				m_commandBuffer.EnableKeyword(m_transparentShadowsCloseFitKeyword);
			}
#else
			
			if (QualitySettings.shadowProjection == ShadowProjection.StableFit)
			{
				m_commandBuffer.EnableShaderKeyword("_TRANSPARENT_RECEIVE_SHADOWS_SPLIT_SPHERES");
			}
			else
			{
				m_commandBuffer.EnableShaderKeyword("_TRANSPARENT_RECEIVE_SHADOWS_CLOSE_FIT");
			}
#endif

			// Execute after the shadowmap has been filled.
			m_light.AddCommandBuffer(LightEvent.AfterShadowMap, m_commandBuffer);
		}

        private void Update()
        {
#if UNITY_2021_1_OR_NEWER
			Shader.DisableKeyword(m_transparentShadowsStableFitKeyword);
			Shader.DisableKeyword(m_transparentShadowsCloseFitKeyword);
#else
			Shader.DisableKeyword("_TRANSPARENT_RECEIVE_SHADOWS_SPLIT_SPHERES");
			Shader.DisableKeyword("_TRANSPARENT_RECEIVE_SHADOWS_CLOSE_FIT");
#endif
		}
#endif
	}
}