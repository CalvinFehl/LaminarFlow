using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

#if FLUIDFRENZY_RUNTIME_URP_SUPPORT
using UnityEngine.Rendering.Universal;
#endif

namespace FluidFrenzy
{
	/// <summary>
	/// Manages the generation of surface reflections (e.g., Planar Reflections) for the WaterSurface.
	/// This class is managed internally by the <see cref="WaterSurface"/> component.
	/// </summary>
	public class SurfaceReflections
	{
		/// <summary>
		/// Overrides the object whose position is used for sampling the water height, which defines the plane of reflection. If null, the component's GameObject position is used.
		/// </summary>
		public static Transform HeightReference = null;

		/// <summary>
		/// Defines the resolution/size of the generated reflection texture.
		/// </summary>
		public enum ReflectionTextureSize
		{
			x128 = 128,
			x256 = 256,
			x512 = 512,
			x1024 = 1024
		}

		[Serializable]
		public class Settings
		{
			/// <summary>
			/// The quality/resolution of the generated planar reflection texture.
			/// </summary>
			[Tooltip("The quality of the planar reflections.")]
			public ReflectionTextureSize resolution = ReflectionTextureSize.x512;

			/// <summary>
			/// Which layers the planar reflection camera renders.
			/// </summary>
			[Tooltip("Which layers the planar reflections render.")]
			public LayerMask cullingMask = ~(1 << 4); // Exclude Water layer (4) by default

			/// <summary>
			/// What to display in empty areas of the planar reflection's view (e.g., Skybox, Solid Color).
			/// </summary>
			[Tooltip("What to display in empty areas of the planar reflection's view.")]
			public CameraClearFlags clearFlags = CameraClearFlags.Skybox;

			/// <summary>
			/// A vertical offset to apply to the reflection plane. This can be used to prevent clipping artifacts with the water surface.
			/// </summary>
			[Tooltip("A offset to apply to the sampled simulation's height.")]
			public float clipPlane = 0;

			/// <summary>
			/// Smoothes the reflection plane's height and position over multiple frames to prevent jittering caused by rapid fluid simulation updates.
			/// </summary>
			[Tooltip("Smoothes the sampling height and position over multiple frames to prevent planar reflections jittering as the fluid simulation updates.")]
			public bool smoothPosition = true;

#if FLUIDFRENZY_RUNTIME_URP_SUPPORT
			/// <summary>
			/// SRP Renderer to use for the planar reflection pass. Use this to select a cheaper render pass for the reflection camera.
			/// </summary>
			[Tooltip("SRP Renderer to use for the planar reflection pass. Use this to set cheaper passes for planar reflections.")]
			public int rendererID = 0;
#else
			/// <summary>
			/// Controls shadow rendering in the reflection (BiRP Only).
			/// </summary>
			[Tooltip("Controls shadow rendering in the reflection.")]
			public UnityEngine.ShadowQuality shadowQuality = UnityEngine.ShadowQuality.Disable;
#endif
		}

		private struct ScopedQualitySettings : IDisposable
		{
			private readonly int _maxLod;
			private readonly float _lodBias;
			private readonly int _pixelLightCount;
			private readonly UnityEngine.ShadowQuality _shadowQuality;
			private readonly bool _modifyShadows;

			public ScopedQualitySettings(UnityEngine.ShadowQuality? overrideShadows)
			{
				_maxLod = QualitySettings.maximumLODLevel;
				_lodBias = QualitySettings.lodBias;

				// Always invert culling and lower LOD for reflections
				GL.invertCulling = true;
				QualitySettings.maximumLODLevel = 1;
				QualitySettings.lodBias = _lodBias * 0.5f;

				// Only modify global shadow/pixel settings if we are in BiRP (overrideShadows has a value)
				_modifyShadows = overrideShadows.HasValue;
				if (_modifyShadows)
				{
					_pixelLightCount = QualitySettings.pixelLightCount;
					_shadowQuality = QualitySettings.shadows;

					QualitySettings.pixelLightCount = 0;
					QualitySettings.shadows = overrideShadows.Value;
				}
				else
				{
					_pixelLightCount = 0;
					_shadowQuality = UnityEngine.ShadowQuality.Disable;
				}
			}

			public void Dispose()
			{
				GL.invertCulling = false;
				QualitySettings.maximumLODLevel = _maxLod;
				QualitySettings.lodBias = _lodBias;

				if (_modifyShadows)
				{
					QualitySettings.pixelLightCount = _pixelLightCount;
					QualitySettings.shadows = _shadowQuality;
				}
			}
		}

		private Settings m_settings;
		private WaterSurface m_surface;

		private Vector3 m_fluidSimSamplePosition = Vector3.zero;
		private RenderTexture m_reflectionTexture;
		private Camera m_reflectionCamera;
		private Material m_blitCopy;
		private bool m_firstFrame = true;
		private bool m_isActive = false;

		public SurfaceReflections(WaterSurface surface, Settings settings)
		{
			m_surface = surface;
			m_settings = settings;
			m_blitCopy = new Material(Shader.Find("Hidden/FluidFrenzy/BlitCopy"));
		}

		public void Enable()
		{
			if (m_isActive) return;
			CreateWaterObjects();
			m_isActive = true;
		}

		public void Disable()
		{
			if (!m_isActive) return;
			Cleanup();
			m_isActive = false;
		}

		public void Cleanup()
		{
			if (m_reflectionTexture)
			{
				m_reflectionTexture.Release();
				UnityEngine.Object.Destroy(m_reflectionTexture);
				m_reflectionTexture = null;
			}
			if (m_reflectionCamera)
			{
				UnityEngine.Object.Destroy(m_reflectionCamera.gameObject);
				m_reflectionCamera = null;
			}
			if (m_blitCopy)
			{
				UnityEngine.Object.Destroy(m_blitCopy);
				m_blitCopy = null;
			}
		}

		// Update settings reference if they change in the editor while running
		public void UpdateSettings(Settings newSettings)
		{
			m_settings = newSettings;
			// Force check for texture recreation if resolution changed
			CreateWaterObjects();
		}

		// BiRP Entry Point
		public void Render(Camera cam)
		{
			if (!PrepareRender(cam, out Vector4 clipPlane, out Matrix4x4 reflectionMatrix)) return;

#if !FLUIDFRENZY_RUNTIME_URP_SUPPORT
			UnityEngine.ShadowQuality? shadows = m_settings.shadowQuality;
#else
			// Fallback if URP support is defined but we are running in a context where Render is called (hybrid/error state)
			UnityEngine.ShadowQuality? shadows = null;
#endif

			using (new ScopedQualitySettings(shadows))
			{
				ExecuteRenderLoop(cam, clipPlane, reflectionMatrix, (c) => c.Render());
			}
		}

		// SRP Entry Point
		public void RenderSRP(ScriptableRenderContext context, Camera cam)
		{
			if (!PrepareRender(cam, out Vector4 clipPlane, out Matrix4x4 reflectionMatrix)) return;

			// Pass null to indicate we shouldn't touch QualitySettings.shadows/pixelLights for SRP
			using (new ScopedQualitySettings(null))
			{
				ExecuteRenderLoop(cam, clipPlane, reflectionMatrix, (c) =>
				{
#if FLUIDFRENZY_RUNTIME_URP_SUPPORT
#if UNITY_6000_0_OR_NEWER
					UniversalRenderPipeline.SingleCameraRequest request = new UniversalRenderPipeline.SingleCameraRequest()
					{
						destination = c.targetTexture,
					};
					UniversalRenderPipeline.SubmitRenderRequest(c, request);
#else
#pragma warning disable CS0618
					UniversalRenderPipeline.RenderSingleCamera(context, c);
#pragma warning restore CS0618
#endif
#endif
				});
			}
		}

		// Shared: Checks validity, calculates plane, matrices, and prepares reflection camera state
		private bool PrepareRender(Camera cam, out Vector4 clipPlane, out Matrix4x4 reflectionMatrix)
		{
			clipPlane = Vector4.zero;
			reflectionMatrix = Matrix4x4.identity;

			if (cam.cameraType == CameraType.Preview || m_reflectionCamera == cam) return false;

			CreateWaterObjects();
			if (!m_reflectionCamera) return false;

			m_reflectionCamera.cullingMask = m_settings.cullingMask;

			// Calculate Plane
			UpdateReflectionCamera(cam);

			// Calculate Matrices
			Vector3 planePos = m_fluidSimSamplePosition;
			bool underWater = cam.transform.position.y < planePos.y;
			Vector3 planeNormal = underWater ? Vector3.down : Vector3.up;

			UpdateCameraModes(cam, m_reflectionCamera);

			float d = -Vector3.Dot(planeNormal, planePos) - (underWater ? 0.0f : m_settings.clipPlane);
			Vector4 reflectionPlane = new Vector4(planeNormal.x, planeNormal.y, planeNormal.z, d);

			CalculateReflectionMatrix(ref reflectionMatrix, reflectionPlane);

			// Apply WorldToCamera immediately as it's common
			m_reflectionCamera.worldToCameraMatrix = cam.worldToCameraMatrix * reflectionMatrix;

			// Calculate Clip Plane for Oblique Matrix
			clipPlane = CameraSpacePlane(m_reflectionCamera, planePos, planeNormal, 1.0f);

			return true;
		}

		// Shared: Handles the actual render calls, including the Stereo/XR logic branch
		private void ExecuteRenderLoop(Camera src, Vector4 clipPlane, Matrix4x4 reflectionMatrix, Action<Camera> renderAction)
		{
			m_reflectionCamera.targetTexture = m_reflectionTexture;

#if !UNITY_2021_1_OR_NEWER || (UNITY_2021_1_OR_NEWER && FLUIDFRENZY_RUNTIME_XR_SUPPORT)
			if (UnityEngine.XR.XRSettings.stereoRenderingMode == UnityEngine.XR.XRSettings.StereoRenderingMode.SinglePassInstanced)
			{
				RenderTextureDescriptor desc = m_reflectionTexture.descriptor;
				desc.width /= 2;
				RenderTexture tempRT = RenderTexture.GetTemporary(desc);
				CameraClearFlags flags = m_reflectionCamera.clearFlags;
				m_reflectionCamera.targetTexture = tempRT;

				// Left Eye
				m_reflectionCamera.projectionMatrix = src.GetStereoProjectionMatrix(Camera.StereoscopicEye.Left);
				m_reflectionCamera.projectionMatrix = m_reflectionCamera.CalculateObliqueMatrix(clipPlane);
				m_reflectionCamera.worldToCameraMatrix = src.GetStereoViewMatrix(Camera.StereoscopicEye.Left) * reflectionMatrix;

				renderAction(m_reflectionCamera);

				m_blitCopy.SetVector(FluidShaderProperties._BlitScaleBiasRt, new Vector4(0.5f, 1, -0.5f, 0));
				Graphics.Blit(tempRT, m_reflectionTexture, m_blitCopy);

				// Right Eye
				m_reflectionCamera.clearFlags = CameraClearFlags.Skybox;
				m_reflectionCamera.projectionMatrix = src.GetStereoProjectionMatrix(Camera.StereoscopicEye.Right);
				m_reflectionCamera.projectionMatrix = m_reflectionCamera.CalculateObliqueMatrix(clipPlane);
				m_reflectionCamera.worldToCameraMatrix = src.GetStereoViewMatrix(Camera.StereoscopicEye.Right) * reflectionMatrix;

				renderAction(m_reflectionCamera);
				m_reflectionCamera.clearFlags = flags;

				m_blitCopy.SetVector(FluidShaderProperties._BlitScaleBiasRt, new Vector4(0.5f, 1, 0.5f, 0));
				Graphics.Blit(tempRT, m_reflectionTexture, m_blitCopy);

				RenderTexture.ReleaseTemporary(tempRT);
			}
			else
#endif
			{
				m_reflectionCamera.projectionMatrix = src.CalculateObliqueMatrix(clipPlane);
				renderAction(m_reflectionCamera);
			}

			m_firstFrame = false;
		}

		private void UpdateReflectionCamera(Camera cam)
		{
			Vector3 heightLookupPosition = HeightReference ? HeightReference.position : cam.transform.position;
			Vector2 waterSimPos = Vector2.zero;

			if (FluidSimulationManager.GetNearestFluidLocation2D(heightLookupPosition, out Vector3 nearestFluidPos))
			{
				heightLookupPosition = nearestFluidPos;
			}

			if (FluidSimulationManager.GetHeight(heightLookupPosition, out waterSimPos))
			{
				if (waterSimPos.y > 0)
				{
					bool smoothPositionUpdate = m_settings.smoothPosition;
					if (Mathf.Abs(m_fluidSimSamplePosition.y - waterSimPos.x) > 10 || m_firstFrame) smoothPositionUpdate = false;
					heightLookupPosition.y = waterSimPos.x;
					m_fluidSimSamplePosition = smoothPositionUpdate ? Vector3.Lerp(m_fluidSimSamplePosition, heightLookupPosition, 0.1f) : heightLookupPosition;
				}
			}
		}

		void UpdateCameraModes(Camera src, Camera dest)
		{
			dest.ResetProjectionMatrix();
			dest.backgroundColor = new Color(0f, 0f, 0f, 0f);
			dest.clearFlags = m_settings.clearFlags;
			dest.orthographic = src.orthographic;
			dest.orthographicSize = src.orthographicSize;
			dest.farClipPlane = src.farClipPlane;
			dest.nearClipPlane = src.nearClipPlane;
			dest.fieldOfView = src.fieldOfView;
			dest.allowMSAA = false;
			dest.aspect = src.aspect;
#if !FLUIDFRENZY_RUNTIME_URP_SUPPORT
			dest.stereoTargetEye = src.stereoTargetEye;
#endif
		}

		// On-demand create any objects we need for water
		void CreateWaterObjects()
		{
			// Reflection render texture
			int textureSizeWidth = (int)m_settings.resolution;
			int textureSizeHeight = (int)m_settings.resolution;
#if !UNITY_2021_1_OR_NEWER || (UNITY_2021_1_OR_NEWER && FLUIDFRENZY_RUNTIME_XR_SUPPORT)
			if (UnityEngine.XR.XRSettings.stereoRenderingMode == UnityEngine.XR.XRSettings.StereoRenderingMode.SinglePassInstanced)
			{
				textureSizeWidth *= 2;
			}
#endif
			if (m_reflectionTexture == null || m_reflectionTexture.width != textureSizeWidth || m_reflectionTexture.height != textureSizeHeight)
			{
				if (m_reflectionTexture == null)
				{
					m_reflectionTexture = new RenderTexture(textureSizeWidth, textureSizeHeight, 16, RenderTextureFormat.ARGBHalf);
					m_reflectionTexture.vrUsage = VRTextureUsage.TwoEyes;
					m_reflectionTexture.name = m_surface.name + "PlanarReflection";
					m_reflectionTexture.isPowerOfTwo = true;
					m_reflectionTexture.hideFlags = HideFlags.DontSave;
				}
				else
				{
					m_reflectionTexture.Release();
				}

				m_reflectionTexture.width = textureSizeWidth;
				m_reflectionTexture.height = textureSizeHeight;

				m_reflectionTexture.Create();
				m_reflectionTexture.SetGlobalShaderProperty("_PlanarReflections");
			}

			// Camera for reflection
			if (!m_reflectionCamera)
			{
				GameObject go = new GameObject("PlanarReflectionsCamera");
				m_reflectionCamera = go.AddComponent<Camera>();
				m_reflectionCamera.enabled = false;
				m_reflectionCamera.cullingMask = m_settings.cullingMask;
				go.hideFlags = HideFlags.HideAndDontSave;

#if FLUIDFRENZY_RUNTIME_URP_SUPPORT
				var cameraData = go.AddComponent(typeof(UniversalAdditionalCameraData)) as UniversalAdditionalCameraData;
				cameraData.requiresColorOption = CameraOverrideOption.Off;
				cameraData.SetRenderer(m_settings.rendererID);
#endif

#if ENVIRO_3
				Enviro.EnviroManager.instance?.AddAdditionalCamera(m_reflectionCamera, true);
#endif
			}
			else
			{
				// Ensure settings updates are propagated if the camera already exists
				m_reflectionCamera.cullingMask = m_settings.cullingMask;

#if FLUIDFRENZY_RUNTIME_URP_SUPPORT
				var cameraData = m_reflectionCamera.GetComponent<UniversalAdditionalCameraData>();
				if (cameraData != null)
				{
					cameraData.SetRenderer(m_settings.rendererID);
				}
#endif
			}
		}

		// Given position/normal of the plane, calculates plane in camera space.
		Vector4 CameraSpacePlane(Camera cam, Vector3 pos, Vector3 normal, float sideSign)
		{
			Vector3 offsetPos = pos + normal * m_settings.clipPlane;
			Matrix4x4 m = cam.worldToCameraMatrix;
			Vector3 cpos = m.MultiplyPoint(offsetPos);
			Vector3 cnormal = m.MultiplyVector(normal).normalized * sideSign;
			return new Vector4(cnormal.x, cnormal.y, cnormal.z, -Vector3.Dot(cpos, cnormal));
		}

		// Calculates reflection matrix around the given plane
		static void CalculateReflectionMatrix(ref Matrix4x4 reflectionMat, Vector4 plane)
		{
			reflectionMat.m00 = (1F - 2F * plane[0] * plane[0]);
			reflectionMat.m01 = (-2F * plane[0] * plane[1]);
			reflectionMat.m02 = (-2F * plane[0] * plane[2]);
			reflectionMat.m03 = (-2F * plane[3] * plane[0]);

			reflectionMat.m10 = (-2F * plane[1] * plane[0]);
			reflectionMat.m11 = (1F - 2F * plane[1] * plane[1]);
			reflectionMat.m12 = (-2F * plane[1] * plane[2]);
			reflectionMat.m13 = (-2F * plane[3] * plane[1]);

			reflectionMat.m20 = (-2F * plane[2] * plane[0]);
			reflectionMat.m21 = (-2F * plane[2] * plane[1]);
			reflectionMat.m22 = (1F - 2F * plane[2] * plane[2]);
			reflectionMat.m23 = (-2F * plane[3] * plane[2]);

			reflectionMat.m30 = 0F;
			reflectionMat.m31 = 0F;
			reflectionMat.m32 = 0F;
			reflectionMat.m33 = 1F;
		}

		public void OnDrawGizmos()
		{
			Gizmos.DrawCube(m_fluidSimSamplePosition, Vector3.one);
		}
	}
}