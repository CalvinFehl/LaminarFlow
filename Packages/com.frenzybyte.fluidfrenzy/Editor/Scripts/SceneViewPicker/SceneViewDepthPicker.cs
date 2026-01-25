using System;
using Unity.Collections;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;

#if FLUIDFRENZY_EDITOR_URP_SUPPORT
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;
#endif

namespace FluidFrenzy.Editor
{
	/// <summary>
	/// A unified Scene View depth picker that works for both Built-in (BiRP) and URP.
	/// It computes the world position of the mouse cursor in the Scene View using the GPU.
	/// </summary>
	[InitializeOnLoad]
	public static class SceneViewDepthPicker
	{
		/// <summary>
		/// The calculated world position of the mouse on the scene geometry.
		/// </summary>
		public static Vector3 WorldPosition { get; private set; }

		/// <summary>
		/// The world space movement delta since the last update.
		/// </summary>
		public static Vector3 Delta { get; private set; }

		/// <summary>
		/// Set to true to enable continuous depth picking. 
		/// Set to false to stop all GPU overhead.
		/// </summary>
		public static bool IsActive
		{
			get => m_IsActive;
			set
			{
				if (m_IsActive != value)
				{
					m_IsActive = value;
					// Force a repaint so the render pass runs immediately upon activation
					// if (m_IsActive) SceneView.RepaintAll();
				}
			}
		}

		private static bool m_IsActive = false;
		private static bool m_OneShotRequest = false;
		private static Vector3? m_LastPosition;
		private static Vector2 m_CurrentPixelPos;
		private static bool m_IsMouseInSceneView;

		private static ComputeShader m_DepthToWorldCS;
		private static int m_KernelHandle;
		private static ComputeBuffer m_Buffer;
		private static NativeArray<Vector4> m_ReadbackArray;

#if FLUIDFRENZY_EDITOR_URP_SUPPORT
		private static SceneViewPickingPassURP m_PickingPass;
#endif

		static SceneViewDepthPicker()
		{
			// Listen to the SceneView GUI loop to track mouse position
			SceneView.duringSceneGui += OnSceneGUI;

			// Hook into rendering to dispatch the compute shader
			Camera.onPostRender += OnPostRenderBiRP;

#if FLUIDFRENZY_EDITOR_URP_SUPPORT
			RenderPipelineManager.beginCameraRendering += OnBeginCameraRenderingURP;
#endif

			AssemblyReloadEvents.beforeAssemblyReload += Cleanup;
		}

		/// <summary>
		/// Request a single depth update for the next frame. 
		/// Useful for "Click to place" tools where you don't need continuous tracking.
		/// </summary>
		public static void RequestSingleUpdate()
		{
			m_OneShotRequest = true;
			SceneView.RepaintAll();
		}

		private static bool ShouldRun()
		{
			return m_IsActive || m_OneShotRequest;
		}

		private static void ConsumeOneShot()
		{
			if (m_OneShotRequest) m_OneShotRequest = false;
		}

		private static void Cleanup()
		{
			SceneView.duringSceneGui -= OnSceneGUI;
			Camera.onPostRender -= OnPostRenderBiRP;
#if FLUIDFRENZY_EDITOR_URP_SUPPORT
			RenderPipelineManager.beginCameraRendering -= OnBeginCameraRenderingURP;
#endif
			AssemblyReloadEvents.beforeAssemblyReload -= Cleanup;

			if (m_Buffer != null)
			{
				m_Buffer.Dispose();
				m_Buffer = null;
			}

			if (m_ReadbackArray.IsCreated)
			{
				m_ReadbackArray.Dispose();
			}
		}

		private static void InitResources()
		{
			if (m_DepthToWorldCS == null)
			{
				// Locate the compute shader by name in the AssetDatabase so we don't rely on a Resources folder
				string[] guids = AssetDatabase.FindAssets("SceneViewDepthPicker t:ComputeShader");
				if (guids.Length > 0)
				{
					string path = AssetDatabase.GUIDToAssetPath(guids[0]);
					m_DepthToWorldCS = AssetDatabase.LoadAssetAtPath<ComputeShader>(path);
				}

				if (m_DepthToWorldCS != null)
					m_KernelHandle = m_DepthToWorldCS.FindKernel("CSMain");
			}

			if (m_Buffer == null || !m_Buffer.IsValid())
				m_Buffer = new ComputeBuffer(1, 16);

			if (!m_ReadbackArray.IsCreated)
				m_ReadbackArray = new NativeArray<Vector4>(1, Allocator.Persistent, NativeArrayOptions.ClearMemory);
		}

		private static void OnSceneGUI(SceneView view)
		{
			// Avoid processing input if the tool isn't active
			if (!ShouldRun()) return;

			if (view == null || view.camera == null || Event.current == null) return;

			Rect viewport = view.camera.pixelRect;
			// Convert GUI coordinates (top-left) to Screen coordinates (bottom-left)
			Vector2 screenPixelPos = HandleUtility.GUIPointToScreenPixelCoordinate(Event.current.mousePosition);

			bool inBounds = screenPixelPos.x >= 0 && screenPixelPos.x <= viewport.width &&
							screenPixelPos.y >= 0 && screenPixelPos.y <= viewport.height;

			m_IsMouseInSceneView = inBounds;
			if (inBounds)
			{
				m_CurrentPixelPos = screenPixelPos;
			}
		}

		private static void OnReadbackComplete(AsyncGPUReadbackRequest request)
		{
			if (request.hasError || !m_ReadbackArray.IsCreated) return;

			m_LastPosition = WorldPosition;

			NativeArray<Vector4> result = request.GetData<Vector4>(0);
			result.CopyTo(m_ReadbackArray);

			Vector4 rawData = m_ReadbackArray[0];
			Vector2 mousePos = new Vector2(rawData.x, rawData.y);
			float depth = rawData.z;

			// We need the active Scene View to access the correct projection matrices for unprojection
			var view = SceneView.lastActiveSceneView;
			if (view == null || view.camera == null) return;
			var cam = view.camera;

			Vector2 normalizedMousePos = mousePos / new Vector2(cam.pixelWidth, cam.pixelHeight);
			Vector2 ndcMousePos = normalizedMousePos * 2.0f - Vector2.one;
			Vector4 ndcPos = new Vector4(ndcMousePos.x, ndcMousePos.y, depth, 1);

			// Unproject from NDC to World Space
			Matrix4x4 vp = GL.GetGPUProjectionMatrix(cam.projectionMatrix, false) * cam.worldToCameraMatrix;
			Matrix4x4 invVP = vp.inverse;

			Vector4 wsPos = invVP * ndcPos;
			if (wsPos.w != 0) wsPos /= wsPos.w;

			WorldPosition = wsPos;
			Delta = WorldPosition - m_LastPosition.GetValueOrDefault(WorldPosition);
		}

		// Implementation for Built-in Render Pipeline
		private static void OnPostRenderBiRP(Camera cam)
		{
			if (GraphicsSettings.currentRenderPipeline != null) return;

			if (!ShouldRun()) return;

			if (cam.cameraType != CameraType.SceneView || !m_IsMouseInSceneView) return;

			InitResources();
			if (m_DepthToWorldCS == null) return;

			// Ensure BiRP generates a depth texture we can sample
			if ((cam.depthTextureMode & DepthTextureMode.Depth) == 0)
			{
				cam.depthTextureMode |= DepthTextureMode.Depth;
			}

			m_DepthToWorldCS.SetBuffer(m_KernelHandle, "_Buffer", m_Buffer);
			m_DepthToWorldCS.SetVector("_ScreenPos", m_CurrentPixelPos);

			var depthTex = Shader.GetGlobalTexture("_CameraDepthTexture");
			if (depthTex != null)
			{
				m_DepthToWorldCS.SetTexture(m_KernelHandle, "_CameraDepthTexture", depthTex);
			}

			m_DepthToWorldCS.Dispatch(m_KernelHandle, 1, 1, 1);

			AsyncGPUReadback.Request(m_Buffer, 16, 0, OnReadbackComplete);
			ConsumeOneShot();
		}

		// Implementation for Universal Render Pipeline
#if FLUIDFRENZY_EDITOR_URP_SUPPORT
		private static void OnBeginCameraRenderingURP(ScriptableRenderContext context, Camera cam)
		{
			if (!ShouldRun()) return;

			if (cam.cameraType != CameraType.SceneView || !m_IsMouseInSceneView) return;

			InitResources();
			if (m_DepthToWorldCS == null) return;

			if (m_PickingPass == null)
				m_PickingPass = new SceneViewPickingPassURP();

			m_PickingPass.Setup(m_DepthToWorldCS, m_KernelHandle, m_Buffer, m_CurrentPixelPos, OnReadbackComplete);

			var data = cam.GetUniversalAdditionalCameraData();
			if (data != null)
			{
				data.scriptableRenderer.EnqueuePass(m_PickingPass);
			}
			ConsumeOneShot();
		}

		class SceneViewPickingPassURP : ScriptableRenderPass
		{
			private ComputeShader m_CS;
			private int m_Kernel;
			private ComputeBuffer m_TargetBuffer;
			private Vector2 m_PixelPos;
			private Action<AsyncGPUReadbackRequest> m_Callback;

			class PassData { }

			public SceneViewPickingPassURP()
			{
				// Run before PostFX to ensure we get the depth before any distortion or UI overlay
				renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
			}

			public void Setup(ComputeShader cs, int kernel, ComputeBuffer buffer, Vector2 pixelPos, Action<AsyncGPUReadbackRequest> callback)
			{
				m_CS = cs;
				m_Kernel = kernel;
				m_TargetBuffer = buffer;
				m_PixelPos = pixelPos;
				m_Callback = callback;
			}

			[Obsolete]
			public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
			{
				CommandBuffer cmd = CommandBufferPool.Get("SceneViewDepthPicker");

				cmd.SetComputeBufferParam(m_CS, m_Kernel, "_Buffer", m_TargetBuffer);
				cmd.SetComputeVectorParam(m_CS, "_ScreenPos", m_PixelPos);
				cmd.DispatchCompute(m_CS, m_Kernel, 1, 1, 1);
				cmd.RequestAsyncReadback(m_TargetBuffer, 16, 0, m_Callback);

				context.ExecuteCommandBuffer(cmd);
				CommandBufferPool.Release(cmd);
			}

#if UNITY_6000_0_OR_NEWER
			public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
			{
				// Use an unsafe pass to get raw command buffer access for the compute dispatch
				using (var builder = renderGraph.AddUnsafePass<PassData>("SceneViewDepthPicker", out var passData))
				{
					builder.AllowPassCulling(false);
					UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
					builder.UseTexture(resourceData.cameraDepthTexture, AccessFlags.Read);

					builder.SetRenderFunc((PassData data, UnsafeGraphContext ctx) =>
					{
						var cmd = CommandBufferHelpers.GetNativeCommandBuffer(ctx.cmd);
						cmd.SetComputeBufferParam(m_CS, m_Kernel, "_Buffer", m_TargetBuffer);
						cmd.SetComputeVectorParam(m_CS, "_ScreenPos", m_PixelPos);
						cmd.DispatchCompute(m_CS, m_Kernel, 1, 1, 1);
						cmd.RequestAsyncReadback(m_TargetBuffer, 16, 0, m_Callback);
					});
				}
			}
#endif
		}
#endif
	}
}