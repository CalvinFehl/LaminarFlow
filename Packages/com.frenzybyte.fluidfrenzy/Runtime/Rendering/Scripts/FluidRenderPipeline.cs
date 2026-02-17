using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;
#if FLUIDFRENZY_RUNTIME_URP_SUPPORT
using UnityEngine.Rendering.Universal;
#endif
#if FLUIDFRENZY_RUNTIME_HDRP_SUPPORT
using UnityEngine.Rendering.HighDefinition;
#endif
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FluidFrenzy
{
#if UNITY_EDITOR
	[InitializeOnLoad]
#endif
	public static class FluidRenderPipeline
	{
		private static readonly HashSet<WaterSurface> s_Surfaces = new HashSet<WaterSurface>();
		public static HashSet<WaterSurface> Surfaces => s_Surfaces;

		private static Material s_PrePassMat;
		private static Material s_UnderwaterMat;

		public static readonly int InverseViewProjectionID = Shader.PropertyToID("_InverseViewProjection");
		public static readonly int FluidMaskID = Shader.PropertyToID("_FluidMaskRT");
		public static readonly int FluidDepthID = Shader.PropertyToID("_FluidDepthRT");
		public static readonly int RefractionID = Shader.PropertyToID("_FluidRefraction");

		// URP Helper IDs
		public static readonly int MeniscusID = Shader.PropertyToID("_MeniscusMaskRT");
		public static readonly int ScreenCopyID = Shader.PropertyToID("_ScreenCopyTexture");
		public static readonly int ScreenCopyScaleID = Shader.PropertyToID("_ScreenCopyTexture_RTHandleScale");

		static FluidRenderPipeline()
		{
			Initialize();
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void Initialize()
		{
#if UNITY_EDITOR
			// Prevent leaks on Domain Reload by cleaning up before C# domain is torn down
			AssemblyReloadEvents.beforeAssemblyReload -= OnBeforeAssemblyReload;
			AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
#endif

			s_Surfaces.Clear();
			Cleanup();

			Camera.onPreCull -= OnCameraPreCull;
			Camera.onPreCull += OnCameraPreCull;

			Camera.onPostRender -= OnCameraPostRender;
			Camera.onPostRender += OnCameraPostRender;

			RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
			RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
		}

		public static void Cleanup()
		{
			if (s_PrePassMat != null) Object.DestroyImmediate(s_PrePassMat);
			s_PrePassMat = null;
			if (s_UnderwaterMat != null) Object.DestroyImmediate(s_UnderwaterMat);
			s_UnderwaterMat = null;

			CausticsEffect.Cleanup();

#if FLUIDFRENZY_RUNTIME_HDRP_SUPPORT
			// Unregister HDRP Passes
			if (m_HdrpPrePass != null) { CustomPassVolume.UnregisterGlobalCustomPass(m_HdrpPrePass); m_HdrpPrePass = null; }
			//if (m_HdrpRefraction != null) { CustomPassVolume.UnregisterGlobalCustomPass(m_HdrpRefraction); m_HdrpRefraction = null; }
			if (m_HdrpCaustics != null) { CustomPassVolume.UnregisterGlobalCustomPass(m_HdrpCaustics); m_HdrpCaustics = null; }
			if (m_HdrpUnderwater != null) { CustomPassVolume.UnregisterGlobalCustomPass(m_HdrpUnderwater); m_HdrpUnderwater = null; }
#endif
		}

#if UNITY_EDITOR
		private static void OnBeforeAssemblyReload()
		{
			Cleanup();
		}
#endif

		public static Material GetUnderwaterMaterial()
		{
			if (s_UnderwaterMat == null)
			{
				Shader s = Shader.Find("Hidden/FluidFrenzy/Underwater");
				if (s != null) s_UnderwaterMat = new Material(s);
			}
			return s_UnderwaterMat;
		}

		public static Material GetPrePassMaterial()
		{
			if (s_PrePassMat == null)
			{
				Shader s = Shader.Find("Hidden/FluidFrenzy/PrePass");
				if (s != null) s_PrePassMat = new Material(s);
			}
			return s_PrePassMat;
		}


		/// <summary>
		/// Populates a PropertyBlock with all the simulation data required to render 
		/// the water mesh (used for the Mask pass).
		/// </summary>
		public static void UpdateSurfaceProperties(MaterialPropertyBlock props, WaterSurface surface)
		{
			if (surface == null || surface.simulation == null) return;
			FluidSimulation simulation = surface.simulation;
			Texture heightmap = simulation.fluidRenderData;
			if (heightmap == null) return;
			// Transform
			props.SetMatrix(FluidShaderProperties._FluidGridWorldToObject, surface.transform.worldToLocalMatrix);

			// Grid
			Vector2 heightmapRcp = (Vector2.one / new Vector2(heightmap.width, heightmap.height)) * new Vector2(heightmap.width - 1, heightmap.height - 1);
			Vector2 dimension = surface.surfaceProperties.dimension;
			Vector2 dimensionRcp = new Vector2(1.0f / dimension.x, 1.0f / dimension.y);
			Vector2 meshResolution = surface.surfaceProperties.meshResolution;

			props.SetVector(FluidShaderProperties._FluidGridMeshDimensions, new Vector4(dimension.x, dimension.y, dimensionRcp.x, dimensionRcp.y));
			props.SetVector(FluidShaderProperties._FluidGridMeshResolution, Vector2.one * meshResolution);
			props.SetVector(FluidShaderProperties._FluidGridMeshRcp, Vector2.one / (meshResolution + Vector2.one));

			// Simulation Data
			props.SetTexture(FluidShaderProperties._FluidHeightVelocityField, simulation.fluidRenderData);
			props.SetTexture(FluidShaderProperties._FluidNormalField, simulation.normalTexture);
			props.SetFloat(FluidShaderProperties._FluidClipHeight, simulation.clipHeight);

			// Terrain Data
			props.SetFloat(FluidShaderProperties._TerrainHeightScale, simulation.terrainScale);
			props.SetTexture(FluidShaderProperties._TerrainHeightField, simulation.terrainHeight);
			props.SetVector(FluidShaderProperties._TerrainHeightField_ST, simulation.terrainTextureST);
			props.SetVector(FluidShaderProperties._HeightmapRcpScale, heightmapRcp);

		}

		public static void UpdateMaterialProperties(Camera camera, WaterSurface surface, Material mat)
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
			mat.SetMatrix(FluidShaderProperties._FluidGridWorldToObject, fluidMatrix.inverse);
			mat.SetFloat(FluidShaderProperties._TerrainHeightScale, simulation.terrainScale);
			mat.SetVector(FluidShaderProperties._TerrainHeightField_ST, simulation.terrainTextureST);
			mat.SetTexture(FluidShaderProperties._TerrainHeightField, simulation.terrainHeight);
			mat.SetTexture(FluidShaderProperties._FluidHeightVelocityField, simulation.fluidRenderData);
			mat.SetTexture(FluidShaderProperties._FluidNormalField, simulation.normalTexture);
			mat.SetFloat(FluidShaderProperties._FluidClipHeight, simulation.clipHeight);
			mat.SetVector(FluidShaderProperties._HeightmapRcpScale, heightmapRcp);
			mat.SetMatrix(FluidShaderProperties._ObjectToWorld, fluidMatrix);

			Matrix4x4 gpuProj = GL.GetGPUProjectionMatrix(camera.projectionMatrix, true);
			Matrix4x4 viewProj = gpuProj * camera.worldToCameraMatrix;
			mat.SetMatrix(InverseViewProjectionID, viewProj.inverse);
		}

		public static void Register(WaterSurface surface)
		{
			if (!s_Surfaces.Contains(surface)) s_Surfaces.Add(surface);
		}

		public static void Deregister(WaterSurface surface)
		{
			if (s_Surfaces.Contains(surface)) s_Surfaces.Remove(surface);
		}

		public static bool IsPipelineCamera(Camera cam)
		{
			if (cam.cameraType == CameraType.Preview) return false;
			if (cam.cameraType == CameraType.Reflection) return false;

			if (!cam.enabled && cam.cameraType != CameraType.SceneView) return false;

			foreach (var surface in s_Surfaces)
			{
				if (surface != null && surface.isActiveAndEnabled)
				{
					if ((cam.cullingMask & (1 << surface.gameObject.layer)) != 0) return true;
				}
			}
			return false;
		}

		public static bool ShouldRenderPrePass(Camera cam)
		{
			foreach (var surface in s_Surfaces)
			{
				if (surface != null && surface.isActiveAndEnabled)
				{
					if ((cam.cullingMask & (1 << surface.gameObject.layer)) == 0) continue;

					// Caustics removed, they do not require pre-pass data
					if (surface.IsUnderwaterEnabled) return true;
				}
			}
			return false;
		}

		public static bool ShouldRenderUnderwater(Camera cam)
		{
			foreach (var surface in s_Surfaces)
			{
				if (surface != null && surface.isActiveAndEnabled)
				{
					if ((cam.cullingMask & (1 << surface.gameObject.layer)) == 0) continue;
					if (surface.IsUnderwaterEnabled) return true;
				}
			}
			return false;
		}

		public static bool ShouldRenderCaustics(Camera cam)
		{
			foreach (var surface in s_Surfaces)
			{
				if (surface != null && surface.isActiveAndEnabled)
				{
					if ((cam.cullingMask & (1 << surface.gameObject.layer)) == 0) continue;
					if (surface.IsCausticsEnabled) return true;
				}
			}
			return false;
		}

		public static WaterSurface GetSurfaceCameraIsInside(Camera cam, HashSet<WaterSurface> surfaces)
		{
			Vector3 camPos = cam.transform.position;
			foreach (var surface in surfaces)
			{
				if ((cam.cullingMask & (1 << surface.gameObject.layer)) == 0) continue;

				if (surface.simulation.bounds.Contains(camPos))
				{
					return surface;
				}
			}
			return null;
		}

		// SRP Entry Points
#if FLUIDFRENZY_RUNTIME_URP_SUPPORT
		private static FluidPrePassURP m_UrpPrePass;
		// private static FluidRefractionURPPass m_UrpRefractionPass;
		private static CausticsEffectURPPass m_UrpCausticsPass;
		private static UnderwaterEffectURPPass m_UrpUnderwaterPass;
#endif

#if FLUIDFRENZY_RUNTIME_HDRP_SUPPORT
		private static FluidPrePassHDRP m_HdrpPrePass;
		// private static FluidRefractionHDRP m_HdrpRefraction;
		private static CausticsEffectHDRPPass m_HdrpCaustics;
		private static UnderwaterEffectHDRPPass m_HdrpUnderwater;
#endif

		private static void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera)
		{
			if (!IsPipelineCamera(camera)) return;

			// URP Injection
#if FLUIDFRENZY_RUNTIME_URP_SUPPORT
			RenderReflectionsSRP(context, camera);

			if (GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset)
			{
				if (m_UrpPrePass == null) m_UrpPrePass = new FluidPrePassURP();
				// if (m_UrpRefractionPass == null) m_UrpRefractionPass = new FluidRefractionURPPass();
				if (m_UrpCausticsPass == null) m_UrpCausticsPass = new CausticsEffectURPPass();
				if (m_UrpUnderwaterPass == null) m_UrpUnderwaterPass = new UnderwaterEffectURPPass();

				var additionalData = camera.GetComponent<UniversalAdditionalCameraData>();
				if (additionalData != null && additionalData.scriptableRenderer != null)
				{
					if (ShouldRenderPrePass(camera)) additionalData.scriptableRenderer.EnqueuePass(m_UrpPrePass);

					// additionalData.scriptableRenderer.EnqueuePass(m_UrpRefractionPass);

					if (ShouldRenderCaustics(camera)) additionalData.scriptableRenderer.EnqueuePass(m_UrpCausticsPass);

					if (ShouldRenderUnderwater(camera)) additionalData.scriptableRenderer.EnqueuePass(m_UrpUnderwaterPass);
				}
			}
#endif

			// HDRP Injection
#if FLUIDFRENZY_RUNTIME_HDRP_SUPPORT
			if (GraphicsSettings.currentRenderPipeline is HDRenderPipelineAsset)
			{
				// Lazy Init Global Passes
				if (m_HdrpPrePass == null)
				{
					m_HdrpPrePass = new FluidPrePassHDRP { name = "Fluid PrePass" };
					CustomPassVolume.RegisterGlobalCustomPass(CustomPassInjectionPoint.BeforePreRefraction, m_HdrpPrePass);
				}

				// if (m_HdrpRefraction == null)
				// {
				// 	m_HdrpRefraction = new FluidRefractionHDRP { name = "Fluid Refraction" };
				// 	CustomPassVolume.RegisterGlobalCustomPass(CustomPassInjectionPoint.BeforeTransparent, m_HdrpRefraction);
				// }

				if (m_HdrpCaustics == null)
				{
					m_HdrpCaustics = new CausticsEffectHDRPPass { name = "Fluid Caustics" };
					// BeforeTransparent ensures it renders on opaque objects before the water surface
					CustomPassVolume.RegisterGlobalCustomPass(CustomPassInjectionPoint.BeforePreRefraction, m_HdrpCaustics);
				}

				if (m_HdrpUnderwater == null)
				{
					m_HdrpUnderwater = new UnderwaterEffectHDRPPass { name = "Fluid Underwater" };
					CustomPassVolume.RegisterGlobalCustomPass(CustomPassInjectionPoint.BeforePostProcess, m_HdrpUnderwater);
				}
			}
#endif
		}

		// BiRP Entry Point
		private static void OnCameraPreCull(Camera cam)
		{
			if (GraphicsSettings.defaultRenderPipeline != null) return;
			if (!IsPipelineCamera(cam)) return;

			RenderReflectionsBiRP(cam);

			// Determine if we need the PrePass based on active effects
			bool renderPrePass = ShouldRenderPrePass(cam);

			var beforeForwardOpaqueCmd = CameraEventCommandBuffer.GetOrCreateAndAttach(cam, CameraEvent.BeforeForwardOpaque, "Fluid PrePass");
			if (renderPrePass)
			{
				var cmd = beforeForwardOpaqueCmd.commandBuffer;
				cmd.Clear();

				cmd.GetTemporaryRT(FluidMaskID, -1, -1, 0, FilterMode.Point, RenderTextureFormat.R8);
				cmd.GetTemporaryRT(FluidDepthID, -1, -1, 24, FilterMode.Point, RenderTextureFormat.Depth);

				cmd.SetRenderTarget(FluidMaskID, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store,
									FluidDepthID, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);

				FluidPrePass.Execute(cmd, cam, s_Surfaces, FluidMaskID, FluidDepthID);
			}
			else
			{
				beforeForwardOpaqueCmd.commandBuffer.Clear();
			}

			var causticsCmd = CameraEventCommandBuffer.GetOrCreateAndAttach(cam, CameraEvent.AfterForwardOpaque, "Fluid Caustics");
			var cmdC = causticsCmd.commandBuffer;
			cmdC.Clear();

			if (ShouldRenderCaustics(cam))
			{
				foreach (var surface in s_Surfaces)
				{
					if (!surface.isActiveAndEnabled) continue;
					CausticsEffect.Render(cmdC, cam, surface);
				}
			}

			var afterForwardAlphaCmd = CameraEventCommandBuffer.GetOrCreateAndAttach(cam, CameraEvent.AfterForwardAlpha, "Fluid Underwater");
			var cmdPost = afterForwardAlphaCmd.commandBuffer;
			cmdPost.Clear();

			bool renderUnderwater = ShouldRenderUnderwater(cam);

			if (renderUnderwater)
			{
				cmdPost.GetTemporaryRT(MeniscusID, -1, -1, 0, FilterMode.Bilinear, RenderTextureFormat.ARGBHalf);
				cmdPost.GetTemporaryRT(ScreenCopyID, -1, -1, 0, FilterMode.Bilinear, RenderTextureFormat.ARGBHalf);

				UnderwaterEffect.Render(cmdPost, cam, s_Surfaces,
					BuiltinRenderTextureType.CameraTarget, BuiltinRenderTextureType.CameraTarget,
					MeniscusID, ScreenCopyID);

				cmdPost.ReleaseTemporaryRT(MeniscusID);
				cmdPost.ReleaseTemporaryRT(ScreenCopyID);
			}

			if (renderPrePass)
			{
				cmdPost.ReleaseTemporaryRT(FluidMaskID);
				cmdPost.ReleaseTemporaryRT(FluidDepthID);
			}
		}

		private static void OnCameraPostRender(Camera cam)
		{
			if (GraphicsSettings.defaultRenderPipeline != null) return;
			CameraEventCommandBuffer.Detach(cam, CameraEvent.BeforeForwardOpaque);
			CameraEventCommandBuffer.Detach(cam, CameraEvent.AfterForwardAlpha);
		}

		public static void RenderReflectionsSRP(ScriptableRenderContext context, Camera camera)
		{
			foreach (var surface in s_Surfaces)
			{
				if (surface.IsReflectionsEnabled && surface.isActiveAndEnabled)
				{
					if ((camera.cullingMask & (1 << surface.gameObject.layer)) != 0)
						surface.SurfaceReflections.RenderSRP(context, camera);
				}
			}
		}

		private static void RenderReflectionsBiRP(Camera camera)
		{
			foreach (var surface in s_Surfaces)
			{
				if (surface.IsReflectionsEnabled && surface.isActiveAndEnabled)
				{
					if ((camera.cullingMask & (1 << surface.gameObject.layer)) != 0)
						surface.SurfaceReflections.Render(camera);
				}
			}
		}
	}
}