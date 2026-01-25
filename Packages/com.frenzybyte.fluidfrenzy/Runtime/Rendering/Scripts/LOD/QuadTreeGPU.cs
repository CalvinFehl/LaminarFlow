using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Rendering;
using System.Runtime.InteropServices;
using System;
using UnityEditor;
using System.Linq;

namespace FluidFrenzy
{
	/// <summary>
	/// A class used to compute a GPU Accelerated QuadTree.
	/// </summary>
	public class QuadTreeGPU : IDisposable
	{
		readonly string kUpdateDispatchArgsKernel = "UpdateDispatchArgs";
		readonly string kTraverseQuadTreeKernel = "TraverseQuadTree";
		readonly string kCullQuadTreeKernel = "CullQuadTree";
		readonly string kCullQuadTreeCustomKernel = "CullQuadTreeCustom";
		readonly string kFillRenderBufferNoCullKernel = "FillRenderBufferNoCull";

#if UNITY_2021_1_OR_NEWER
		GraphicsBuffer m_quadTree1;
		GraphicsBuffer m_quadTree2;
		GraphicsBuffer m_dispatchArgs1;
		GraphicsBuffer m_dispatchArgs2;
		GraphicsBuffer m_drawArgs;
		GraphicsBuffer m_nodes;
#else
		ComputeBuffer m_quadTree1;
		ComputeBuffer m_quadTree2;
		ComputeBuffer m_dispatchArgs1;
		ComputeBuffer m_dispatchArgs2;
		ComputeBuffer m_drawArgs;
		ComputeBuffer m_nodes;
#endif

		ComputeShader m_traverseCS;

		int m_updateDispatchArgsKernelID = -1;
		int m_traverseQuadTreeKernelID = -1;
		int m_cullQuadTreeKernelID = -1;
		int m_cullQuadTreeCustomKernelID = -1;
		int m_fillRenderBufferNoCullKernelID = -1;

		Vector4 m_heightMapMask;
		int[] m_lodMinMax;

		public QuadTreeGPU(uint indexCount, Vector4 heightMapMask, Vector2Int lodMinMax)
		{
			m_heightMapMask = heightMapMask;
			m_lodMinMax = new int[] { Mathf.Min(lodMinMax.x,7), lodMinMax.y};
			m_traverseCS = Resources.Load("QuadTreeLOD") as ComputeShader;
			if (lodMinMax.x != 0 || lodMinMax.y != 15)
				m_traverseCS.EnableKeyword("QUADTREE_MIN_MAX_LEVEL");
			else
				m_traverseCS.DisableKeyword("QUADTREE_MIN_MAX_LEVEL");

			m_updateDispatchArgsKernelID = m_traverseCS.FindKernel(kUpdateDispatchArgsKernel);
			m_traverseQuadTreeKernelID = m_traverseCS.FindKernel(kTraverseQuadTreeKernel);
			m_cullQuadTreeKernelID = m_traverseCS.FindKernel(kCullQuadTreeKernel);
			m_cullQuadTreeCustomKernelID = m_traverseCS.FindKernel(kCullQuadTreeCustomKernel);
			m_fillRenderBufferNoCullKernelID = m_traverseCS.FindKernel(kFillRenderBufferNoCullKernel);
#if UNITY_2021_1_OR_NEWER
			m_quadTree1 = new GraphicsBuffer(GraphicsBuffer.Target.Append | GraphicsBuffer.Target.CopySource | GraphicsBuffer.Target.CopyDestination, 1 << 16, Marshal.SizeOf(typeof(uint)));
#else
			m_quadTree1 = new ComputeBuffer(1 << 16, Marshal.SizeOf(typeof(uint)), ComputeBufferType.Append);
#endif
			m_quadTree1.name = "QuadTree1";
			m_quadTree1.SetData(Enumerable.Repeat(0u, m_quadTree1.count).ToArray());
			m_quadTree1.SetCounterValue(0);

#if UNITY_2021_1_OR_NEWER
			m_quadTree2 = new GraphicsBuffer(GraphicsBuffer.Target.Append | GraphicsBuffer.Target.CopySource | GraphicsBuffer.Target.CopyDestination, 1 << 16, Marshal.SizeOf(typeof(uint)));
#else
			m_quadTree2 = new ComputeBuffer(1 << 16, Marshal.SizeOf(typeof(uint)), ComputeBufferType.Append);
#endif
			m_quadTree2.name = "QuadTree2";
			m_quadTree2.SetData(Enumerable.Repeat(0u, m_quadTree1.count).ToArray());
			m_quadTree2.SetCounterValue(0);

#if UNITY_2021_1_OR_NEWER
			m_dispatchArgs1 = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, 4, Marshal.SizeOf(typeof(uint)));
#else
			m_dispatchArgs1 = new ComputeBuffer(4, Marshal.SizeOf(typeof(uint)), ComputeBufferType.IndirectArguments);
#endif
			m_dispatchArgs1.name = "dispatchArgs1";
			m_dispatchArgs1.SetData(new uint[] { 1, 1, 1, 1 });

#if UNITY_2021_1_OR_NEWER
			m_dispatchArgs2 = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, 4, Marshal.SizeOf(typeof(uint)));
#else
			m_dispatchArgs2 = new ComputeBuffer(4, Marshal.SizeOf(typeof(uint)), ComputeBufferType.IndirectArguments);
#endif
			m_dispatchArgs2.name = "dispatchArgs2";
			m_dispatchArgs2.SetData(new uint[] { 1, 1, 1, 1 });

#if UNITY_2021_1_OR_NEWER
			m_drawArgs = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, 5, Marshal.SizeOf(typeof(uint)));
#else
			m_drawArgs = new ComputeBuffer(5, Marshal.SizeOf(typeof(uint)), ComputeBufferType.IndirectArguments);
#endif
			m_drawArgs.SetData(new uint[] { indexCount, 1, 0, 0, 0 });
			m_drawArgs.name = "drawArgs";

#if UNITY_2021_1_OR_NEWER
			m_nodes = new GraphicsBuffer(GraphicsBuffer.Target.Append, 1 << 16, Marshal.SizeOf(typeof(Vector4)));
#else
			m_nodes = new ComputeBuffer(1 << 16, Marshal.SizeOf(typeof(Vector4)), ComputeBufferType.Append);
#endif
			m_nodes.SetCounterValue(0);
			m_nodes.name = "nodes";
		}

		public void Dispose()
		{
			m_quadTree1.Release();
			m_quadTree2.Release();
			m_dispatchArgs1.Release();
			m_dispatchArgs2.Release();
			m_drawArgs.Release();
			m_nodes.Release();
		}


#if UNITY_2021_1_OR_NEWER
		public void Traverse(Camera camera, CommandBuffer commandBuffer, Texture heightmap, Matrix4x4 objectToWorld, float terrainSize, float terrainHeight, float maxHeight, out GraphicsBuffer nodes, out GraphicsBuffer drawArgs)
#else
		public void Traverse(Camera camera, CommandBuffer commandBuffer, Texture heightmap, Matrix4x4 objectToWorld, float terrainSize, float terrainHeight, float maxHeight, out ComputeBuffer nodes, out ComputeBuffer drawArgs)
#endif
		{
			if (camera != Camera.main && camera.cameraType != CameraType.SceneView)
			{
				nodes = m_nodes;
				drawArgs = m_drawArgs;
				return;
			}

#if UNITY_2021_1_OR_NEWER
#if FLUIDFRENZY_RUNTIME_XR_SUPPORT
			if (UnityEngine.XR.XRSettings.stereoRenderingMode == UnityEngine.XR.XRSettings.StereoRenderingMode.SinglePassInstanced)
				m_traverseCS.EnableKeyword("STEREO_INSTANCING_ON");
			else
				m_traverseCS.DisableKeyword("STEREO_INSTANCING_ON");
#endif
#else
			if (UnityEngine.XR.XRSettings.stereoRenderingMode == UnityEngine.XR.XRSettings.StereoRenderingMode.SinglePassInstanced)
				m_traverseCS.EnableKeyword("STEREO_INSTANCING_ON");
			else
				m_traverseCS.DisableKeyword("STEREO_INSTANCING_ON");
#endif


			// Reset quadtree node count
#if UNITY_2021_1_OR_NEWER
			commandBuffer.SetBufferCounterValue(m_quadTree1, 0);
			commandBuffer.SetBufferCounterValue(m_quadTree2, 0);			
#else
			commandBuffer.SetComputeBufferCounterValue(m_quadTree1, 0);
			commandBuffer.SetComputeBufferCounterValue(m_quadTree2, 0);
#endif

			//General pramaters
			commandBuffer.SetComputeIntParams(m_traverseCS, FluidShaderProperties._LODMinMax, m_lodMinMax);
			commandBuffer.SetComputeMatrixParam(m_traverseCS, FluidShaderProperties._ObjectToWorld, objectToWorld);
			commandBuffer.SetComputeVectorParam(m_traverseCS, FluidShaderProperties._WorldSpaceCameraPos, camera.transform.position);
			commandBuffer.SetComputeVectorParam(m_traverseCS, FluidShaderProperties._LocalSpaceCameraPos, objectToWorld.inverse.MultiplyPoint(camera.transform.position));
			commandBuffer.SetComputeVectorParam(m_traverseCS, FluidShaderProperties._WorldCameraPos, camera.transform.position);
			commandBuffer.SetComputeVectorParam(m_traverseCS, FluidShaderProperties._SurfaceLocalBounds, new Vector3(terrainSize, maxHeight, terrainSize));
			commandBuffer.SetComputeVectorParam(m_traverseCS, FluidShaderProperties._HeightMapMask, m_heightMapMask);
			commandBuffer.SetComputeFloatParam(m_traverseCS, FluidShaderProperties._SurfaceHeightScale, terrainHeight);

			// Traverse quadtree
			commandBuffer.SetComputeTextureParam(m_traverseCS, m_traverseQuadTreeKernelID, FluidShaderProperties._HeightMap, heightmap);
			commandBuffer.SetComputeBufferParam(m_traverseCS, m_traverseQuadTreeKernelID, FluidShaderProperties._SrcDispatchArgs, m_dispatchArgs1);
			commandBuffer.SetComputeBufferParam(m_traverseCS, m_traverseQuadTreeKernelID, FluidShaderProperties._SrcQuadTree, m_quadTree1);
			commandBuffer.SetComputeBufferParam(m_traverseCS, m_traverseQuadTreeKernelID, FluidShaderProperties._DstQuadTree, m_quadTree2);
			commandBuffer.DispatchCompute(m_traverseCS, m_traverseQuadTreeKernelID, m_dispatchArgs1, 0);

			// For some reason in Unity 2022 the quadtree on the next frame is filled with 0s again, but the count isnt reset so it will create a endless list of low lods until it overflows and stalls the GPU.
			// Maybe this is a bug in my code, but it works in unity 2021 and 6 preview.
#if UNITY_2021_1_OR_NEWER
			commandBuffer.CopyBuffer(m_quadTree2, m_quadTree1);
#endif

			// Update dispatch args
			commandBuffer.CopyCounterValue(m_quadTree2, m_dispatchArgs2, 12);
			commandBuffer.SetComputeBufferParam(m_traverseCS, m_updateDispatchArgsKernelID, FluidShaderProperties._SrcDispatchArgs, m_dispatchArgs2);
			commandBuffer.SetComputeBufferParam(m_traverseCS, m_updateDispatchArgsKernelID, FluidShaderProperties._DstDispatchArgs, m_dispatchArgs1);
			commandBuffer.DispatchCompute(m_traverseCS, m_updateDispatchArgsKernelID, 1, 1, 1);
			//
			// Perform culling and fill draw list
#if UNITY_2021_1_OR_NEWER
			commandBuffer.SetBufferCounterValue(m_nodes, 0);
#else
			commandBuffer.SetComputeBufferCounterValue(m_nodes, 0);
#endif
			commandBuffer.SetComputeBufferParam(m_traverseCS, m_cullQuadTreeKernelID, FluidShaderProperties._SrcDispatchArgs, m_dispatchArgs1);
			commandBuffer.SetComputeBufferParam(m_traverseCS, m_cullQuadTreeKernelID, FluidShaderProperties._SrcQuadTree, m_quadTree2);
			commandBuffer.SetComputeBufferParam(m_traverseCS, m_cullQuadTreeKernelID, FluidShaderProperties._RenderBuffer, m_nodes);
			commandBuffer.DispatchCompute(m_traverseCS, m_cullQuadTreeKernelID, m_dispatchArgs1, 0);
			commandBuffer.CopyCounterValue(m_nodes, m_drawArgs, 4);

#if !UNITY_2021_1_OR_NEWER
			var tmp = m_quadTree2;
			m_quadTree2 = m_quadTree1;
			m_quadTree1 = tmp;
#endif

			nodes = m_nodes;
			drawArgs = m_drawArgs;
		}

#if UNITY_2021_1_OR_NEWER
		public void CullQuadtree(Camera camera, CommandBuffer commandBuffer, Matrix4x4 objectToWorld, float terrainSize, float maxHeight, out GraphicsBuffer nodes, out GraphicsBuffer drawArgs)
#else
		public void CullQuadtree(Camera camera, CommandBuffer commandBuffer, Matrix4x4 objectToWorld, float terrainSize, float maxHeight, out ComputeBuffer nodes, out ComputeBuffer drawArgs)
#endif
		{
			commandBuffer.SetComputeMatrixParam(m_traverseCS, FluidShaderProperties._ObjectToWorld, objectToWorld);
			commandBuffer.SetComputeVectorParam(m_traverseCS, FluidShaderProperties._WorldSpaceCameraPos, camera.transform.position);
			commandBuffer.SetComputeVectorParam(m_traverseCS, FluidShaderProperties._SurfaceLocalBounds, new Vector3(terrainSize, maxHeight, terrainSize));

			// Perform culling and fill draw list
#if UNITY_2021_1_OR_NEWER
			commandBuffer.SetBufferCounterValue(m_nodes, 0);
#else
			commandBuffer.SetComputeBufferCounterValue(m_nodes, 0);
#endif
			commandBuffer.SetComputeBufferParam(m_traverseCS, m_cullQuadTreeKernelID, FluidShaderProperties._SrcDispatchArgs, m_dispatchArgs1);
			commandBuffer.SetComputeBufferParam(m_traverseCS, m_cullQuadTreeKernelID, FluidShaderProperties._SrcQuadTree, m_quadTree1);
			commandBuffer.SetComputeBufferParam(m_traverseCS, m_cullQuadTreeKernelID, FluidShaderProperties._RenderBuffer, m_nodes);
			commandBuffer.DispatchCompute(m_traverseCS, m_cullQuadTreeKernelID, m_dispatchArgs1, 0);
			commandBuffer.CopyCounterValue(m_nodes, m_drawArgs, 4);
			nodes = m_nodes;
			drawArgs = m_drawArgs;
		}

#if UNITY_2021_1_OR_NEWER
		public void CullQuadtree(Camera camera, CommandBuffer commandBuffer, Matrix4x4 objectToWorld, float terrainSize, float maxHeight, Vector4[] planes, out GraphicsBuffer nodes, out GraphicsBuffer drawArgs)
#else
		public void CullQuadtree(Camera camera, CommandBuffer commandBuffer, Matrix4x4 objectToWorld, float terrainSize, float maxHeight, Vector4[] planes, out ComputeBuffer nodes, out ComputeBuffer drawArgs)
#endif
		{
			commandBuffer.SetComputeMatrixParam(m_traverseCS, FluidShaderProperties._ObjectToWorld, objectToWorld);
			commandBuffer.SetComputeVectorParam(m_traverseCS, FluidShaderProperties._WorldSpaceCameraPos, camera.transform.position);
			commandBuffer.SetComputeVectorParam(m_traverseCS, FluidShaderProperties._SurfaceLocalBounds, new Vector3(terrainSize, maxHeight, terrainSize));
			commandBuffer.SetComputeVectorArrayParam(m_traverseCS, FluidShaderProperties._CullingPlanes, planes);

			// Perform culling and fill draw list
#if UNITY_2021_1_OR_NEWER
			commandBuffer.SetBufferCounterValue(m_nodes, 0);
#else
			commandBuffer.SetComputeBufferCounterValue(m_nodes, 0);
#endif
			commandBuffer.SetComputeBufferParam(m_traverseCS, m_cullQuadTreeCustomKernelID, FluidShaderProperties._SrcDispatchArgs, m_dispatchArgs1);
			commandBuffer.SetComputeBufferParam(m_traverseCS, m_cullQuadTreeCustomKernelID, FluidShaderProperties._SrcQuadTree, m_quadTree1);
			commandBuffer.SetComputeBufferParam(m_traverseCS, m_cullQuadTreeCustomKernelID, FluidShaderProperties._RenderBuffer, m_nodes);
			commandBuffer.DispatchCompute(m_traverseCS, m_cullQuadTreeCustomKernelID, m_dispatchArgs1, 0);
			commandBuffer.CopyCounterValue(m_nodes, m_drawArgs, 4);
			nodes = m_nodes;
			drawArgs = m_drawArgs;
		}

#if UNITY_2021_1_OR_NEWER
		public void FillRenderBufferNoCull(CommandBuffer commandBuffer, float terrainSize, float maxHeight, out GraphicsBuffer nodes, out GraphicsBuffer drawArgs)
#else
		public void FillRenderBufferNoCull(CommandBuffer commandBuffer, float terrainSize, float maxHeight, out ComputeBuffer nodes, out ComputeBuffer drawArgs)
#endif
		{
			commandBuffer.SetComputeVectorParam(m_traverseCS, FluidShaderProperties._SurfaceLocalBounds, new Vector3(terrainSize, maxHeight, terrainSize));

			// Perform culling and fill draw list
#if UNITY_2021_1_OR_NEWER
			commandBuffer.SetBufferCounterValue(m_nodes, 0);
#else
			commandBuffer.SetComputeBufferCounterValue(m_nodes, 0);
#endif
			commandBuffer.SetComputeBufferParam(m_traverseCS, m_fillRenderBufferNoCullKernelID, FluidShaderProperties._SrcDispatchArgs, m_dispatchArgs1);
			commandBuffer.SetComputeBufferParam(m_traverseCS, m_fillRenderBufferNoCullKernelID, FluidShaderProperties._SrcQuadTree, m_quadTree1);
			commandBuffer.SetComputeBufferParam(m_traverseCS, m_fillRenderBufferNoCullKernelID, FluidShaderProperties._RenderBuffer, m_nodes);
			commandBuffer.DispatchCompute(m_traverseCS, m_fillRenderBufferNoCullKernelID, m_dispatchArgs1, 0);
			commandBuffer.CopyCounterValue(m_nodes, m_drawArgs, 4);
			nodes = m_nodes;
			drawArgs = m_drawArgs;
		}

#if UNITY_2021_1_OR_NEWER
		public void GetRenderData(out GraphicsBuffer nodes, out GraphicsBuffer drawArgs)
		{
			nodes = m_nodes;
			drawArgs = m_drawArgs;
		}
#else
		public void GetRenderData(out ComputeBuffer nodes, out ComputeBuffer drawArgs)
		{
			nodes = m_nodes;
			drawArgs = m_drawArgs;
		}
#endif

		public static int DCS(int total, int threadGroupCnt)
		{
			return (total + threadGroupCnt - 1) / threadGroupCnt;
		}
	}
}