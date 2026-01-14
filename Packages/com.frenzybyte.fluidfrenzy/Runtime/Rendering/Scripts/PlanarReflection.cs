using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

#if FLUIDFRENZY_RUNTIME_URP_SUPPORT
using UnityEngine.Rendering.Universal;
#endif

namespace FluidFrenzy
{
	//http://wiki.unity3d.com/index.php/SurfaceReflection
	/// <summary>
	/// <see cref="PlanarReflection"/> is a component that generates **real-time planar reflections** for the water surface to enhance rendering quality.
	/// </summary>
	/// <remarks>
	/// <para>
	/// This is achieved by rendering the scene again from a mirrored perspective-flipped around the water plane and capturing the result to a texture. This reflection texture is then applied to the water material.
	/// </para>
	/// <para>
	/// The script reads the height of the fluid simulation to set the reflection plane as accurately as possible. It includes built-in smoothing (controlled by <see cref="m_smoothPosition"/>) to prevent quick, jittering changes caused by small, rapid waves on the fluid surface.
	/// </para>
	/// <para>
	/// **Note:** To see the results of the reflection, the water material (e.g., <c>FluidRenderer.fluidMaterial</c>) must have planar reflections enabled in its shader.
	/// </para>
	/// </remarks>
	[HelpURL("https://frenzybyte.github.io/fluidfrenzy/docs/fluid_rendering_components/")]
	public class PlanarReflection : MonoBehaviour
	{
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

		class PlanarReflectionSettingData
		{
			private int _maxLod;
			private float _lodBias;

			public void Set()
			{
				_maxLod = QualitySettings.maximumLODLevel;
				_lodBias = QualitySettings.lodBias;

				GL.invertCulling = true;
				QualitySettings.maximumLODLevel = 1;
				QualitySettings.lodBias = _lodBias * 0.5f;
			}

			public void Restore()
			{
				GL.invertCulling = false;
				QualitySettings.maximumLODLevel = _maxLod;
				QualitySettings.lodBias = _lodBias;
			}
		}

#if FLUIDFRENZY_RUNTIME_URP_SUPPORT
		/// <summary>
		/// SRP Renderer to use for the planar reflection pass. Use this to select a cheaper render pass for the reflection camera.
		/// </summary>
		[Tooltip("SRP Renderer to use for the planar reflection pass. Use this to set cheaper passes for planar reflections.")]
		public int rendererID = 0;
#endif

		/// <summary>
		/// Which layers the planar reflection camera renders.
		/// </summary>
		[Tooltip("Which layers the planar reflections render.")]
		[FormerlySerializedAs("reflectionCullingLayers")]
		public LayerMask cullingMask = -1;

		/// <summary>
		/// What to display in empty areas of the planar reflection's view (e.g., Skybox, Solid Color).
		/// </summary>
		[Tooltip("What to display in empty areas of the planar reflection's view.")]
		public CameraClearFlags clearFlags = CameraClearFlags.Skybox;

		/// <summary>
		/// The quality/resolution of the generated planar reflection texture.
		/// </summary>
		[Tooltip("The quality of the planar reflections.")]
		[FormerlySerializedAs("textureSize")]
		public ReflectionTextureSize resolution = ReflectionTextureSize.x512;

		/// <summary>
		/// A vertical offset to apply to the reflection plane. This can be used to prevent clipping artifacts with the water surface.
		/// </summary>
		[Tooltip("A offset to apply to the sampled simulation's height.")]
		public float clipPlane = 0;

		/// <summary>
		/// Overrides the object whose position is used for sampling the water height, which defines the plane of reflection. If null, the component's GameObject position is used.
		/// </summary>
		[Tooltip("Override the location to use for sampling the height of the simulation.")]
		public GameObject heightSampleTransform = null;

		/// <summary>
		/// Smoothes the reflection plane's height and position over multiple frames to prevent jittering caused by rapid fluid simulation updates.
		/// </summary>
		[SerializeField]
		[FormerlySerializedAs("m_smoothPosition")]
		[Tooltip("Smoothes the sampling height and position over multiple frames to prevent planar reflections jittering as the fluid simulation updates.")]
		public bool smoothPosition = true;

		private Vector3 m_fluidSimSamplePosition = Vector3.zero;

		private RenderTexture m_reflectionTexture;
		private Camera m_reflectionCamera;

		private Material m_blitCopy;

		private bool m_firstFrame = true;

		private PlanarReflectionSettingData m_qualitySettings;

		private void OnEnable()
		{
			RenderPipelineManager.beginCameraRendering += PreRenderSRP;
			Camera.onPreRender += PreRender;
		}

		private void OnDisable()
		{
			RenderPipelineManager.beginCameraRendering -= PreRenderSRP;
			Camera.onPreRender -= PreRender;
		}

		private void OnDestroy()
		{
			RenderPipelineManager.beginCameraRendering -= PreRenderSRP;
			Camera.onPreRender -= PreRender;

			// Cleanup all the objects we possibly have created
			if (m_reflectionTexture)
			{
				m_reflectionTexture.Release();
				Destroy(m_reflectionTexture);
				m_reflectionTexture = null;
			}
			if (m_reflectionCamera)
			{
				Destroy(m_reflectionCamera.gameObject);
				m_reflectionCamera = null;
			}
		}

		private void Start()
		{
			m_blitCopy = new Material(Shader.Find("Hidden/FluidFrenzy/BlitCopy"));
			m_qualitySettings = new PlanarReflectionSettingData();
			if (heightSampleTransform)
			{
				m_fluidSimSamplePosition = heightSampleTransform.transform.position;
			}
			CreateWaterObjects();
		}


		private void PreRender(Camera cam)
		{
			
			if (cam.cameraType == CameraType.Preview || m_reflectionCamera == cam) return;
			CreateWaterObjects();

			if (!m_reflectionCamera)
			{
				return;
			}

			// find out the reflection plane: position and normal in world space
			Vector3 heightLookupPosition = heightSampleTransform ? heightSampleTransform.transform.position : cam.transform.position;
			Vector2 waterSimPos = Vector2.zero;
			bool underWater = false;
			if (FluidSimulationManager.GetNearestFluidLocation2D(heightLookupPosition, out Vector3 nearestFluidPos))
			{
				heightLookupPosition = nearestFluidPos;
			}
			if (FluidSimulationManager.GetHeight(heightLookupPosition, out waterSimPos))
			{
				if (waterSimPos.y > 0)
				{
					bool smoothPositionUpdate = smoothPosition;
					if (Mathf.Abs(m_fluidSimSamplePosition.y - waterSimPos.x) > 10 || m_firstFrame) smoothPositionUpdate = false;
					heightLookupPosition.y = waterSimPos.x;
					m_fluidSimSamplePosition = smoothPositionUpdate ? Vector3.Lerp(m_fluidSimSamplePosition, heightLookupPosition, 0.1f) : heightLookupPosition;

					underWater = cam.transform.position.y < waterSimPos.x;
				}
			}
			Vector3 planePos = m_fluidSimSamplePosition;
			Vector3 planeNormal = underWater ? Vector3.down : Vector3.up;

			int oldPixelLightCount = QualitySettings.pixelLightCount;
			UnityEngine.ShadowQuality previousShadowQuality = QualitySettings.shadows;
			QualitySettings.pixelLightCount = 0;
			QualitySettings.shadows = UnityEngine.ShadowQuality.Disable;

			UpdateCameraModes(cam, m_reflectionCamera);

			// Reflect camera around reflection plane
			float d = -Vector3.Dot(planeNormal, planePos) - (underWater ? 0.0f : this.clipPlane);
			Vector4 reflectionPlane = new Vector4(planeNormal.x, planeNormal.y, planeNormal.z, d);

			Matrix4x4 reflection = Matrix4x4.zero;
			CalculateReflectionMatrix(ref reflection, reflectionPlane);
			Vector3 newpos = reflection.MultiplyPoint(cam.transform.position);
			m_reflectionCamera.worldToCameraMatrix = cam.worldToCameraMatrix * reflection;
			// Setup oblique projection matrix so that near plane is our reflection
			// plane. This way we clip everything below/above it for free.
			Vector4 clipPlane = CameraSpacePlane(m_reflectionCamera, planePos, planeNormal, 1.0f);

			m_reflectionCamera.targetTexture = m_reflectionTexture;

			// Invert culling because view is mirrored
			bool oldCulling = GL.invertCulling;
			GL.invertCulling = !oldCulling;

#if !UNITY_2021_1_OR_NEWER || (UNITY_2021_1_OR_NEWER && FLUIDFRENZY_RUNTIME_XR_SUPPORT)
			if (UnityEngine.XR.XRSettings.stereoRenderingMode == UnityEngine.XR.XRSettings.StereoRenderingMode.SinglePassInstanced)
			{
				RenderTextureDescriptor desc = m_reflectionTexture.descriptor;
				desc.width /= 2;
				RenderTexture tempRT = RenderTexture.GetTemporary(desc);
				CameraClearFlags flags = m_reflectionCamera.clearFlags;
				m_reflectionCamera.targetTexture = tempRT;
				m_reflectionCamera.projectionMatrix = cam.GetStereoProjectionMatrix(Camera.StereoscopicEye.Left);
				m_reflectionCamera.projectionMatrix = m_reflectionCamera.CalculateObliqueMatrix(clipPlane);
				m_reflectionCamera.worldToCameraMatrix = cam.GetStereoViewMatrix(Camera.StereoscopicEye.Left) * reflection;
				m_reflectionCamera.Render();

				//Graphics.CopyTexture(tempRT, 0, 0, 0, 0, tempRT.width, tempRT.height, m_reflectionTexture, 0, 0, 0, 0);
				m_blitCopy.SetVector(FluidShaderProperties._BlitScaleBiasRt, new Vector4(0.5f, 1, -0.5f, 0));
				Graphics.Blit(tempRT, m_reflectionTexture, m_blitCopy);

				m_reflectionCamera.clearFlags = CameraClearFlags.Skybox;
				m_reflectionCamera.projectionMatrix = cam.GetStereoProjectionMatrix(Camera.StereoscopicEye.Right);
				m_reflectionCamera.projectionMatrix = m_reflectionCamera.CalculateObliqueMatrix(clipPlane);
				m_reflectionCamera.worldToCameraMatrix = cam.GetStereoViewMatrix(Camera.StereoscopicEye.Right) * reflection;
				m_reflectionCamera.Render();
				m_reflectionCamera.clearFlags = flags;

				//Graphics.Blit(tempRT, m_reflectionTexture);
				//Graphics.CopyTexture(tempRT, 0, 0, 0, 0, tempRT.width, tempRT.height, m_reflectionTexture, 0, 0, tempRT.width, 0);

				m_blitCopy.SetVector(FluidShaderProperties._BlitScaleBiasRt, new Vector4(0.5f, 1, 0.5f, 0));
				Graphics.Blit(tempRT, m_reflectionTexture, m_blitCopy);

				RenderTexture.ReleaseTemporary(tempRT);
			}
			else
#endif
			{
				m_reflectionCamera.projectionMatrix = cam.CalculateObliqueMatrix(clipPlane);
				m_reflectionCamera.worldToCameraMatrix = cam.worldToCameraMatrix * reflection;

				m_reflectionCamera.Render();
			}

			GL.invertCulling = oldCulling;

			QualitySettings.shadows = previousShadowQuality;
			QualitySettings.pixelLightCount = oldPixelLightCount;
			m_firstFrame = false;
		}

		private void PreRenderSRP(ScriptableRenderContext context, Camera cam)
		{

			if (cam.cameraType == CameraType.Preview || m_reflectionCamera == cam) return;
			CreateWaterObjects();

			if (!m_reflectionCamera)
			{
				return;
			}

			// find out the reflection plane: position and normal in world space
			Vector3 heightLookupPosition = heightSampleTransform ? heightSampleTransform.transform.position : cam.transform.position;
			Vector2 waterSimPos = Vector2.zero;
			bool underWater = false;
			if (FluidSimulationManager.GetNearestFluidLocation2D(heightLookupPosition, out Vector3 nearestFluidPos))
			{
				heightLookupPosition = nearestFluidPos;
			}
			if (FluidSimulationManager.GetHeight(heightLookupPosition, out waterSimPos))
			{
				if (waterSimPos.y > 0)
				{
					bool smoothPositionUpdate = smoothPosition;
					if (Mathf.Abs(m_fluidSimSamplePosition.y - waterSimPos.x) > 10 || m_firstFrame) smoothPositionUpdate = false;
					heightLookupPosition.y = waterSimPos.x;
					m_fluidSimSamplePosition = smoothPositionUpdate ? Vector3.Lerp(m_fluidSimSamplePosition, heightLookupPosition, 0.1f) : heightLookupPosition;
					underWater = cam.transform.position.y < waterSimPos.x;
				}
			}
			Vector3 planePos = m_fluidSimSamplePosition;
			Vector3 planeNormal = underWater ? Vector3.down : Vector3.up;

			UpdateCameraModes(cam, m_reflectionCamera);

			// Reflect camera around reflection plane
			float d = -Vector3.Dot(planeNormal, planePos) - (underWater ? 0.0f : this.clipPlane);
			Vector4 reflectionPlane = new Vector4(planeNormal.x, planeNormal.y, planeNormal.z, d);

			Matrix4x4 reflection = Matrix4x4.zero;
			CalculateReflectionMatrix(ref reflection, reflectionPlane);
			Vector3 newpos = reflection.MultiplyPoint(cam.transform.position);
			m_reflectionCamera.worldToCameraMatrix = cam.worldToCameraMatrix * reflection;

			// Setup oblique projection matrix so that near plane is our reflection
			// plane. This way we clip everything below/above it for free.
			Vector4 clipPlane = CameraSpacePlane(m_reflectionCamera, planePos, planeNormal, 1.0f);

			m_qualitySettings.Set();

#if !UNITY_2021_1_OR_NEWER || (UNITY_2021_1_OR_NEWER && FLUIDFRENZY_RUNTIME_XR_SUPPORT)
			if (UnityEngine.XR.XRSettings.stereoRenderingMode == UnityEngine.XR.XRSettings.StereoRenderingMode.SinglePassInstanced)
			{
				RenderTextureDescriptor desc = m_reflectionTexture.descriptor;
				desc.width /= 2;
				RenderTexture tempRT = RenderTexture.GetTemporary(desc);
				CameraClearFlags flags = m_reflectionCamera.clearFlags;
				m_reflectionCamera.targetTexture = tempRT;
				m_reflectionCamera.projectionMatrix = cam.GetStereoProjectionMatrix(Camera.StereoscopicEye.Left);
				m_reflectionCamera.projectionMatrix = m_reflectionCamera.CalculateObliqueMatrix(clipPlane);
				m_reflectionCamera.worldToCameraMatrix = cam.GetStereoViewMatrix(Camera.StereoscopicEye.Left) * reflection;

#if FLUIDFRENZY_RUNTIME_URP_SUPPORT
#if UNITY_6000_0_OR_NEWER
				UniversalRenderPipeline.SingleCameraRequest request = new UniversalRenderPipeline.SingleCameraRequest()
				{
					destination = tempRT,
				};
				UniversalRenderPipeline.SubmitRenderRequest(m_reflectionCamera, request);
#else

#pragma warning disable CS0618
				UniversalRenderPipeline.RenderSingleCamera(context, m_reflectionCamera); // render planar reflections
#pragma warning restore CS0618
#endif
#endif

				m_blitCopy.SetVector(FluidShaderProperties._BlitScaleBiasRt, new Vector4(0.5f, 1, -0.5f, 0));
				Graphics.Blit(tempRT, m_reflectionTexture, m_blitCopy);

				m_reflectionCamera.clearFlags = CameraClearFlags.Skybox;
				m_reflectionCamera.projectionMatrix = cam.GetStereoProjectionMatrix(Camera.StereoscopicEye.Right);
				m_reflectionCamera.projectionMatrix = m_reflectionCamera.CalculateObliqueMatrix(clipPlane);
				m_reflectionCamera.worldToCameraMatrix = cam.GetStereoViewMatrix(Camera.StereoscopicEye.Right) * reflection;
				m_reflectionCamera.clearFlags = flags;

#if FLUIDFRENZY_RUNTIME_URP_SUPPORT
#if UNITY_6000_0_OR_NEWER
				request = new UniversalRenderPipeline.SingleCameraRequest()
				{
					destination = tempRT,
				};
				UniversalRenderPipeline.SubmitRenderRequest(m_reflectionCamera, request);
#else

#pragma warning disable CS0618
				UniversalRenderPipeline.RenderSingleCamera(context, m_reflectionCamera); // render planar reflections
#pragma warning restore CS0618
#endif
#endif

				m_blitCopy.SetVector(FluidShaderProperties._BlitScaleBiasRt, new Vector4(0.5f, 1, 0.5f, 0));
				Graphics.Blit(tempRT, m_reflectionTexture, m_blitCopy);

				RenderTexture.ReleaseTemporary(tempRT);
			}
			else
#endif
			{

				m_reflectionCamera.projectionMatrix = cam.CalculateObliqueMatrix(clipPlane);
				m_reflectionCamera.targetTexture = m_reflectionTexture;
#if FLUIDFRENZY_RUNTIME_URP_SUPPORT
#if UNITY_6000_0_OR_NEWER
			UniversalRenderPipeline.SingleCameraRequest request = new UniversalRenderPipeline.SingleCameraRequest()
			{
				destination = m_reflectionTexture,
			};
			UniversalRenderPipeline.SubmitRenderRequest(m_reflectionCamera, request);
#else

#pragma warning disable CS0618
				UniversalRenderPipeline.RenderSingleCamera(context, m_reflectionCamera); // render planar reflections
#pragma warning restore CS0618
#endif
#endif
			}
			m_qualitySettings.Restore();

			m_firstFrame = false;
		}

		void UpdateCameraModes(Camera src, Camera dest)
		{
			dest.ResetProjectionMatrix();
			dest.backgroundColor = new Color(0f, 0f, 0f, 0f);
			dest.clearFlags = clearFlags;
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
			int textureSizeWidth = (int)this.resolution;
			int textureSizeHeight = (int)this.resolution;
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
					m_reflectionTexture.name = this.name + "PlanarReflection";
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
				m_reflectionCamera.cullingMask = cullingMask;
				go.hideFlags = HideFlags.HideAndDontSave;

#if FLUIDFRENZY_RUNTIME_URP_SUPPORT
				var cameraData = go.AddComponent(typeof(UniversalAdditionalCameraData)) as UniversalAdditionalCameraData;
				cameraData.requiresColorOption = CameraOverrideOption.Off;
				cameraData.SetRenderer(rendererID);
#endif

#if ENVIRO_3
				Enviro.EnviroManager.instance?.AddAdditionalCamera(m_reflectionCamera, true);
#endif
			}

		}

		// Given position/normal of the plane, calculates plane in camera space.
		Vector4 CameraSpacePlane(Camera cam, Vector3 pos, Vector3 normal, float sideSign)
		{
			Vector3 offsetPos = pos + normal * clipPlane;
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

		private void OnDrawGizmos()
		{
			Gizmos.DrawCube(m_fluidSimSamplePosition, Vector3.one);
		}
	}
}