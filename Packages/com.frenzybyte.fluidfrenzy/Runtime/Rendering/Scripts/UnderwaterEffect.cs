using System;
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

		private static readonly int _FluidGridWorldToObject = Shader.PropertyToID("_FluidGridWorldToObject");
		private static readonly int _InverseViewProjection = Shader.PropertyToID("_InverseViewProjection");

		// Settings IDs
		private static readonly int _AbsorptionDepthScale = Shader.PropertyToID("_AbsorptionDepthScale");
		private static readonly int _AbsorptionLimits = Shader.PropertyToID("_AbsorptionLimits");
		private static readonly int _WaterColor = Shader.PropertyToID("_WaterColor");
		private static readonly int _ScatterColor = Shader.PropertyToID("_ScatterColor");
		private static readonly int _ScatterLightIntensity = Shader.PropertyToID("_ScatterLightIntensity");
		private static readonly int _ScatterAmbient = Shader.PropertyToID("_ScatterAmbient");
		private static readonly int _ScatterIntensity = Shader.PropertyToID("_ScatterIntensity");
		private static readonly int _MeniscusThickness = Shader.PropertyToID("_MeniscusThickness");
		private static readonly int _MeniscusBlur = Shader.PropertyToID("_MeniscusBlur");
		private static readonly int _MeniscusDarkness = Shader.PropertyToID("_MeniscusDarkness");
		private static readonly int _UnderwaterAmbient = Shader.PropertyToID("_UnderwaterAmbient");
		public struct PassData
		{
			public int mask;
			public int fallback;
			public int meniscus;
			public int composite;
			public int debug;

			public PassData(Material mat)
			{
				if (mat == null)
				{
					mask = fallback = meniscus = composite = debug = 0;
					return;
				}
				mask = mat.FindPass("FluidMask");
				fallback = mat.FindPass("VolumeFallback");
				meniscus = mat.FindPass("MeniscusBlur");
				composite = mat.FindPass("UnderwaterEffect");
				debug = mat.FindPass("DebugOverlay");
			}
		}

		public static void UpdateMaterialProperties(Camera camera, UnderwaterEffect.UnderwaterSettings settings, WaterSurface surface, Material mat)
		{
			if (mat == null || surface == null || surface.simulation == null) return;

			FluidSimulation simulation = surface.simulation;
			Matrix4x4 fluidMatrix = surface.transform.localToWorldMatrix;
			Texture heightmap = simulation.fluidRenderData;
			if (heightmap == null) return;

			Vector2 heightmapRcp = (Vector2.one / new Vector2(heightmap.width, heightmap.height)) * new Vector2(heightmap.width - 1, heightmap.height - 1);
			Vector2 dimension = surface.surfaceProperties.dimension;
			Vector2 dimensionRcp = new Vector2(1.0f / dimension.x, 1.0f / dimension.y);
			Vector2 meshResolution = surface.surfaceProperties.meshResolution;

			mat.SetVector(FluidShaderProperties._FluidGridMeshDimensions, new Vector4(dimension.x, dimension.y, dimensionRcp.x, dimensionRcp.y));
			mat.SetVector(FluidShaderProperties._FluidGridMeshResolution, Vector2.one * meshResolution);
			mat.SetVector(FluidShaderProperties._FluidGridMeshRcp, Vector2.one / (meshResolution + Vector2.one));
			mat.SetMatrix(_FluidGridWorldToObject, fluidMatrix.inverse);
			mat.SetFloat(FluidShaderProperties._TerrainHeightScale, simulation.terrainScale);
			mat.SetVector(FluidShaderProperties._TerrainHeightField_ST, simulation.terrainTextureST);
			mat.SetTexture(FluidShaderProperties._TerrainHeightField, simulation.terrainHeight);
			mat.SetTexture(FluidShaderProperties._FluidHeightVelocityField, simulation.fluidRenderData);
			mat.SetTexture(FluidShaderProperties._FluidNormalField, simulation.normalTexture);
			mat.SetFloat(FluidShaderProperties._FluidClipHeight, simulation.clipHeight);
			mat.SetVector(FluidShaderProperties._HeightmapRcpScale, heightmapRcp);
			mat.SetMatrix(FluidShaderProperties._ObjectToWorld, fluidMatrix);

			mat.SetFloat(_AbsorptionDepthScale, settings.absorptionDepthScale);
			mat.SetVector(_AbsorptionLimits, settings.absorptionLimits);
			mat.SetColor(_WaterColor, settings.waterColor);
			mat.SetColor(_ScatterColor, settings.scatterColor);
			mat.SetFloat(_ScatterLightIntensity, settings.scatterLightIntensity);
			mat.SetFloat(_ScatterAmbient, settings.scatterAmbientIntensity);
			mat.SetFloat(_ScatterIntensity, settings.scatterIntensity);
			mat.SetFloat(_MeniscusThickness, settings.meniscusThickness);
			mat.SetFloat(_MeniscusBlur, settings.meniscusBlur);
			mat.SetFloat(_MeniscusDarkness, settings.meniscusDarkness);

			Matrix4x4 gpuProj = GL.GetGPUProjectionMatrix(camera.projectionMatrix, true);
			Matrix4x4 viewProj = gpuProj * camera.worldToCameraMatrix;
			mat.SetMatrix(_InverseViewProjection, viewProj.inverse);

			Color ambientColor = SphericalHarmonicsUtil.GetAmbientColorUp(camera.transform.position);
			mat.SetColor(_UnderwaterAmbient, ambientColor);
		}

		public static void SetScreenSizeParam(MaterialPropertyBlock props, Camera cam)
		{
			props.SetVector(_FluidScreenSize, new Vector4(cam.pixelWidth, cam.pixelHeight, 1.0f / cam.pixelWidth, 1.0f / cam.pixelHeight));
		}

		public static void RenderFluidMask(CommandBuffer cmd, WaterSurface surface, Material mat, MaterialPropertyBlock props, int passIndex, RenderTargetIdentifier targetMask, RenderTargetIdentifier targetDepth)
		{
			CoreUtils.SetRenderTarget(cmd, targetMask, targetDepth, ClearFlag.All, Color.black);

			// Set Globals 
			cmd.SetGlobalTexture(_FluidMaskRT, targetMask);
			cmd.SetGlobalTexture(_FluidDepthRT, targetDepth);

			Matrix4x4 fluidMatrix = surface.transform.localToWorldMatrix;
			surface.surfaceRenderer.Render(cmd, fluidMatrix, mat, props, passIndex);
		}

		public static void RenderFullScreenPass(CommandBuffer cmd, Material mat, MaterialPropertyBlock props, int passIndex, RenderTargetIdentifier dest, RenderTargetIdentifier? depthBuffer = null, bool clear = false)
		{
			if (depthBuffer.HasValue)
			{
				CoreUtils.SetRenderTarget(cmd, dest, depthBuffer.Value, clear ? ClearFlag.All : ClearFlag.None, Color.black);
			}
			else
			{
				CoreUtils.SetRenderTarget(cmd, dest, clear ? ClearFlag.All : ClearFlag.None, Color.black);
			}

			CoreUtils.DrawFullScreen(cmd, mat, props, passIndex);
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
	public class UnderwaterEffect : RenderModule
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

		/// <summary>
		/// Descriptor struct used to initialize the <see cref="UnderwaterEffect"/> module.
		/// </summary>
		public struct Desc
		{
			/// <summary>
			/// The visual settings configuration.
			/// </summary>
			public UnderwaterSettings settings;
			/// <summary>The water surface instance this effect is attached to.</summary>
			public WaterSurface surface;
		}

		private UnderwaterSettings m_settings;
		private WaterSurface m_waterSurface;
		private Material m_underwaterMat;
		private MaterialPropertyBlock m_propertyBlock;
		private UnderwaterShared.PassData m_passes;
		public bool debugMask = false;

#if FLUIDFRENZY_RUNTIME_URP_SUPPORT
		private UnderwaterEffectURPPass m_urpPass;
#endif

#if FLUIDFRENZY_RUNTIME_HDRP_SUPPORT
		private UnderwaterEffectHDRPPass m_hdrpPass;
#endif
		public UnderwaterEffect(Desc desc) : base()
		{
			m_settings = desc.settings;
			m_waterSurface = desc.surface;

			m_underwaterMat = new Material(Shader.Find("Hidden/FluidFrenzy/Underwater"));
			m_passes = new UnderwaterShared.PassData(m_underwaterMat);
			m_propertyBlock = new MaterialPropertyBlock();

#if FLUIDFRENZY_RUNTIME_URP_SUPPORT
			m_urpPass = new UnderwaterEffectURPPass(m_settings, m_waterSurface, m_underwaterMat, m_passes, debugMask);
#endif

			if (m_waterSurface.simulation.terrainType == FluidSimulation.TerrainType.UnityTerrain)
				m_underwaterMat.EnableKeyword(new LocalKeyword(m_underwaterMat.shader, "_FLUID_UNITY_TERRAIN"));
		}

		public override void OnEnable()
		{
#if FLUIDFRENZY_RUNTIME_HDRP_SUPPORT
			if (m_hdrpPass == null)
			{
				m_hdrpPass = new UnderwaterEffectHDRPPass();
				m_hdrpPass.name = "Fluid Underwater";
				m_hdrpPass.settings = m_settings;
				m_hdrpPass.surface = m_waterSurface;
				m_hdrpPass.material = m_underwaterMat;
				m_hdrpPass.passIndices = m_passes;
				CustomPassVolume.RegisterGlobalCustomPass(CustomPassInjectionPoint.BeforePostProcess, m_hdrpPass);
			}
#endif
		}

		public override void OnDisable()
		{
#if FLUIDFRENZY_RUNTIME_HDRP_SUPPORT
			if (m_hdrpPass != null)
			{
				CustomPassVolume.UnregisterGlobalCustomPass(m_hdrpPass);
				m_hdrpPass = null;
			}
#endif
		}

		public override void AddCommandBuffers(Camera camera)
		{
			if (camera.name != "CameraManager") return;

#if FLUIDFRENZY_RUNTIME_HDRP_SUPPORT
			if (m_hdrpPass != null)
			{
				m_hdrpPass.settings = m_settings;
				m_hdrpPass.surface = m_waterSurface;
				m_hdrpPass.debug = debugMask;
				return;
			}
#endif

			// BiRP Logic
			UnderwaterShared.UpdateMaterialProperties(camera, m_settings, m_waterSurface, m_underwaterMat);

			CommandBuffer cmd = CameraEventCommandBuffer.GetOrCreateAndAttach(camera, CameraEvent.AfterForwardAlpha, "UnderwaterEffect");
			cmd.Clear();

			int w = camera.pixelWidth;
			int h = camera.pixelHeight;

			cmd.GetTemporaryRT(UnderwaterShared._FluidMaskRT, w, h, 0, FilterMode.Point, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
			cmd.GetTemporaryRT(UnderwaterShared._FluidDepthRT, w, h, 24, FilterMode.Point, RenderTextureFormat.Depth, RenderTextureReadWrite.Linear);
			cmd.GetTemporaryRT(UnderwaterShared._MeniscusMaskRT, w, h, 0, FilterMode.Bilinear, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
			cmd.GetTemporaryRT(UnderwaterShared._ScreenCopyTexture, w, h, 0, FilterMode.Bilinear, RenderTextureFormat.ARGBFloat);

			m_propertyBlock.Clear();
			UnderwaterShared.SetScreenSizeParam(m_propertyBlock, camera);

			// Mask
			UnderwaterShared.RenderFluidMask(cmd, m_waterSurface, m_underwaterMat, m_propertyBlock, m_passes.mask,
				UnderwaterShared._FluidMaskRT, UnderwaterShared._FluidDepthRT);

			// Volume Fallback
			cmd.SetGlobalTexture(UnderwaterShared._FluidDepthRT, UnderwaterShared._FluidDepthRT);

			UnderwaterShared.RenderFullScreenPass(cmd, m_underwaterMat, m_propertyBlock, m_passes.fallback,
				UnderwaterShared._FluidMaskRT, null, false);

			// Meniscus Blur
			m_propertyBlock.Clear();
			UnderwaterShared.SetScreenSizeParam(m_propertyBlock, camera);
			cmd.SetGlobalTexture(UnderwaterShared._FluidMaskRT, UnderwaterShared._FluidMaskRT);

			UnderwaterShared.RenderFullScreenPass(cmd, m_underwaterMat, m_propertyBlock, m_passes.meniscus,
				UnderwaterShared._MeniscusMaskRT, null, true);

			// Copy
			cmd.Blit(BuiltinRenderTextureType.CameraTarget, UnderwaterShared._ScreenCopyTexture);
			cmd.SetGlobalTexture(UnderwaterShared._ScreenCopyTexture, UnderwaterShared._ScreenCopyTexture);

			// Composite
			m_propertyBlock.Clear();
			UnderwaterShared.SetScreenSizeParam(m_propertyBlock, camera);

			int finalPass = debugMask ? m_passes.debug : m_passes.composite;
			UnderwaterShared.RenderFullScreenPass(cmd, m_underwaterMat, m_propertyBlock, finalPass,
				BuiltinRenderTextureType.CameraTarget, null, false);

			cmd.ReleaseTemporaryRT(UnderwaterShared._ScreenCopyTexture);
			cmd.ReleaseTemporaryRT(UnderwaterShared._MeniscusMaskRT);
			cmd.ReleaseTemporaryRT(UnderwaterShared._FluidDepthRT);
			cmd.ReleaseTemporaryRT(UnderwaterShared._FluidMaskRT);
		}

		public override void RemoveCommandBuffers(Camera camera)
		{
			CameraEventCommandBuffer.Detach(camera, CameraEvent.AfterForwardAlpha);
		}

		public override void RenderSRP(ScriptableRenderContext context, Camera camera)
		{
#if FLUIDFRENZY_RUNTIME_URP_SUPPORT
			if (camera.cameraType == CameraType.Preview) return;
			if (camera.cameraType == CameraType.Game && camera != Camera.main) return;

			UnderwaterShared.UpdateMaterialProperties(camera, m_settings, m_waterSurface, m_underwaterMat);

			var data = camera.GetUniversalAdditionalCameraData();
			if (data != null && data.scriptableRenderer != null)
			{
				if (m_urpPass == null)
					m_urpPass = new UnderwaterEffectURPPass(m_settings, m_waterSurface, m_underwaterMat, m_passes, debugMask);

				m_urpPass.SetDebug(debugMask);
				data.scriptableRenderer.EnqueuePass(m_urpPass);
			}
#endif
		}
	}
}