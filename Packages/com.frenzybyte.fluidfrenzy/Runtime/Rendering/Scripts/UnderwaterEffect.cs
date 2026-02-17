using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

#if FLUIDFRENZY_RUNTIME_URP_SUPPORT
using UnityEngine.Rendering.Universal;
#endif

#if FLUIDFRENZY_RUNTIME_HDRP_SUPPORT
using UnityEngine.Rendering.HighDefinition;
#endif

namespace FluidFrenzy
{
	/// <summary>
	/// A static utility class responsible for managing the rendering state, shader properties, and command buffer execution 
	/// required for the underwater visual effect.
	/// </summary>
	public static class UnderwaterShared
	{
		public static readonly int _FluidMaskRT = Shader.PropertyToID("_FluidMaskRT");
		public static readonly int _FluidDepthRT = Shader.PropertyToID("_FluidDepthRT");
		public static readonly int _ScreenCopyTexture = Shader.PropertyToID("_ScreenCopyTexture");
		public static readonly int _MeniscusMaskRT = Shader.PropertyToID("_MeniscusMaskRT");
		public static readonly int _FluidScreenSize = Shader.PropertyToID("_FluidScreenSize");

		// Settings IDs
		private static readonly int _AbsorptionDepthScaleID = Shader.PropertyToID("_AbsorptionDepthScale");
		private static readonly int _AbsorptionLimitsID = Shader.PropertyToID("_AbsorptionLimits");
		private static readonly int _WaterColorID = Shader.PropertyToID("_WaterColor");
		private static readonly int _ScatterColorID = Shader.PropertyToID("_ScatterColor");
		private static readonly int _ScatterLightIntensityID = Shader.PropertyToID("_ScatterLightIntensity");
		private static readonly int _ScatterAmbientID = Shader.PropertyToID("_ScatterAmbient");
		private static readonly int _ScatterIntensityID = Shader.PropertyToID("_ScatterIntensity");
		private static readonly int _MeniscusThicknessID = Shader.PropertyToID("_MeniscusThickness");
		private static readonly int _MeniscusBlurID = Shader.PropertyToID("_MeniscusBlur");
		private static readonly int _MeniscusDarknessID = Shader.PropertyToID("_MeniscusDarkness");
		private static readonly int _UnderwaterAmbientID = Shader.PropertyToID("_UnderwaterAmbient");

		public static void UpdateMaterialProperties(Camera camera, UnderwaterEffect.UnderwaterSettings settings, Material mat)
		{
			if (mat == null) return;

			mat.SetFloat(_AbsorptionDepthScaleID, settings.absorptionDepthScale);
			mat.SetVector(_AbsorptionLimitsID, settings.absorptionLimits);
			mat.SetColor(_WaterColorID, settings.waterColor);
			mat.SetColor(_ScatterColorID, settings.scatterColor);
			mat.SetFloat(_ScatterLightIntensityID, settings.scatterLightIntensity);
			mat.SetFloat(_ScatterAmbientID, settings.scatterAmbientIntensity);
			mat.SetFloat(_ScatterIntensityID, settings.scatterIntensity);
			mat.SetFloat(_MeniscusThicknessID, settings.meniscusThickness);
			mat.SetFloat(_MeniscusBlurID, settings.meniscusBlur);
			mat.SetFloat(_MeniscusDarknessID, settings.meniscusDarkness);

			Color ambientColor = SphericalHarmonicsUtil.GetAmbientColorUp(camera.transform.position);
			mat.SetColor(_UnderwaterAmbientID, ambientColor);
		}

		/// <summary>
		/// Helper to render a full screen quad with safe RenderTarget setup
		/// </summary>
		public static void RenderFullScreenPass(CommandBuffer cmd, Material mat, MaterialPropertyBlock props, int passIndex, RenderTargetIdentifier dest, RenderTargetIdentifier? depthBuffer = null, bool clear = false)
		{
			if (depthBuffer.HasValue)
				cmd.SetRenderTarget(dest, depthBuffer.Value);
			else
				cmd.SetRenderTarget(dest);

			if (clear)
				cmd.ClearRenderTarget(depthBuffer.HasValue, true, Color.black);

			CoreUtils.DrawFullScreen(cmd, mat, props, passIndex);
		}

		public static void SetScreenSizeParam(MaterialPropertyBlock props, Camera cam)
		{
			props.SetVector(_FluidScreenSize, new Vector4(cam.pixelWidth, cam.pixelHeight, 1.0f / cam.pixelWidth, 1.0f / cam.pixelHeight));
		}
	}


	/// <summary>
	/// The <see cref="UnderwaterEffect"/> module renders the visuals you see when the camera goes underwater. It is supported in all render pipelines.
	/// <para>
	/// It uses the same simulation math as the water surface to ensure the underwater volume matches the waves perfectly. 
	/// However it has its own independent visual settings, allowing you to style the underwater atmosphere separately from the surface itself.
	/// </para>
	/// <para>
	/// This distinction is useful for gameplay as you can make the underwater view clearer or brighter than the surface to help players see further. 
	/// The effect handles features like light absorption, fog scattering, and directional lighting to create the underwater atmosphere.
	/// </para>
	/// </summary>
	public class UnderwaterEffect
	{
		/// <summary>
		/// Settings  for all configurable visual parameters of the <see cref="UnderwaterEffect"/>.
		/// This class defines how light interacts with the water volume, including absorption rates, scattering colors, and the appearance of the surface meniscus.
		/// </summary>
		/// <docgen-target>WaterSurfaceEditor</docgen-target>
		[Serializable]
		public class UnderwaterSettings
		{

			/// <summary>
			/// The base transmission color of the water.
			/// </summary>
			/// <remarks>
			/// This defines the color of the water as light passes through it. Brighter colors make the water look clear while darker colors make the water look thick and deep. This works with the alpha value and the absorption depth scale to decide how much the scene behind the water is tinted.
			/// </remarks>
			public Color waterColor = new Color(0.8078431f, 0.9098039f, 0.9058824f, 1.0f);

			/// <summary>
			/// Controls the rate at which light is absorbed as it travels through the water.
			/// </summary>
			/// <remarks>
			/// Higher values result in darker water where light cannot penetrate as deeply. This scaling factor applies to the exponential decay of the <see cref="waterColor"/>.
			/// </remarks>
			[Range(0, 1)]
			public float absorptionDepthScale = 0.2f;

			/// <summary>
			/// Clamps the calculated absorption to a specific range (Min, Max).
			/// </summary>
			/// <remarks>
			/// Useful for preventing the water from becoming completely black at extreme depths or ensuring a minimum amount of visibility.
			/// </remarks>
			public Vector2 absorptionLimits = new Vector2(0, 1);

			/// <summary>
			/// The vertical thickness of the meniscus line (the water-air boundary) on the camera lens.
			/// </summary>
			[Range(0, 0.1f)]
			public float meniscusThickness = 0.05f;

			/// <summary>
			/// The amount of blur applied to the meniscus line to soften the transition between underwater and above-water.
			/// </summary>
			[Range(0, 20)]
			public float meniscusBlur = 5.0f;

			/// <summary>
			/// Controls the intensity/darkness of the meniscus line effect.
			/// </summary>
			[Range(0, 1)]
			public float meniscusDarkness = 1.0f;

			/// <summary>
			/// The color of the light scattered within the water volume (subsurface scattering/fog color).
			/// </summary>
			public Color scatterColor = new Color(0.0784f, 0.3255f, 0.5098f, 1.0f);

			/// <summary>
			/// The base ambient contribution to the scattering effect, independent of direct lighting.
			/// </summary>
			[Range(0, 1)]
			public float scatterAmbientIntensity = 0.1f;

			/// <summary>
			/// Scales the influence of the main directional light on the scattering effect.
			/// </summary>
			[Range(0, 1)]
			public float scatterLightIntensity = 0.1f;

			/// <summary>
			/// A global multiplier for the overall scattering intensity.
			/// </summary>
			[Range(0, 1)]
			public float scatterIntensity = 1.0f;
		}

		private static readonly MaterialPropertyBlock s_Props = new MaterialPropertyBlock();

		public static void Render(CommandBuffer cmd, Camera camera, HashSet<WaterSurface> surfaces,
			RenderTargetIdentifier source, RenderTargetIdentifier dest,
			RenderTargetIdentifier meniscus, RenderTargetIdentifier copy)
		{
			WaterSurface activeSurface = FluidRenderPipeline.GetSurfaceCameraIsInside(camera, surfaces);
			if (activeSurface == null || !activeSurface.IsUnderwaterEnabled) return;

			Material mat = FluidRenderPipeline.GetUnderwaterMaterial();

			FluidRenderPipeline.UpdateMaterialProperties(camera, activeSurface, mat);
			UnderwaterShared.UpdateMaterialProperties(camera, activeSurface.underWaterSettings, mat);

			s_Props.Clear();
			UnderwaterShared.SetScreenSizeParam(s_Props, camera);

			// Render Meniscus
			cmd.SetRenderTarget(meniscus);
			CoreUtils.DrawFullScreen(cmd, mat, s_Props, 0);

			if (!source.Equals(copy)) cmd.Blit(source, copy);

			cmd.SetGlobalTexture(FluidRenderPipeline.MeniscusID, meniscus);
			cmd.SetGlobalTexture(FluidRenderPipeline.ScreenCopyID, copy);

			// Composite
			UnderwaterShared.RenderFullScreenPass(cmd, mat, s_Props, 1, dest);
		}
	}
}