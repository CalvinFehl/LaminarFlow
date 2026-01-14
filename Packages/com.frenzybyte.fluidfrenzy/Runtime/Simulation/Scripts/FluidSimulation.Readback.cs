using System;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace FluidFrenzy
{
	public partial class FluidSimulation : MonoBehaviour
	{
		//Async readback data
		private int m_currentHeightVelocityReadbackSlice = 0;
		private bool m_processingHeightVelocityReadBack = true;
		NativeArray<long> m_heightVelocityData; //Async readback data containing height and velocity compressed.
		private int m_renderDataWidth, m_renderDataHeight;
		private event Action<AsyncGPUReadbackRequest> m_readbackHeightVelocityAction;


		private int m_currentSDFReadbackSlice = 0;
		private bool m_processingSDFReadBack = true;
		NativeArray<int> m_sdfData; //Async readback data containing sdf data.
		private int m_sdfDataWidth, m_sdfDataHeight;
		private event Action<AsyncGPUReadbackRequest> m_readbackSDFAction;


		protected void ReadbackSimulationData()
		{
#if FLUIDFRENZY_RPCORE_15_OR_NEWER
			using (new ProfilingScope(ProfilingSampler.Get(WaterSimProfileID.WaterSimulationDynamic)))
#else
			using (new ProfilingScope(null, ProfilingSampler.Get(WaterSimProfileID.WaterSimulationDynamic)))
#endif
			{
				ReadBackHeightVelocity();
				ReadBackSDF();
				Graphics.ExecuteCommandBuffer(m_dynamicCommandBuffer);
				m_dynamicCommandBuffer.Clear();
			}
		}

		private void ReadBackHeightVelocity()
		{
			InitHeightReadbackData();
			if (settings.readBackHeight)
			{
				if (m_processingHeightVelocityReadBack)
				{
					using (new ProfilingScope(m_dynamicCommandBuffer, ProfilingSampler.Get(WaterSimProfileID.AsyncReadbackHeightVelocity)))
					{
						int height = m_activeHeightVelocityTexture.height / settings.readBackTimeSliceFrames;
						int y = height * m_currentHeightVelocityReadbackSlice;
						m_dynamicCommandBuffer.RequestAsyncReadback(m_activeHeightVelocityTexture, 0, 0, m_activeHeightVelocityTexture.width, y, height, 0, 1, m_readbackHeightVelocityAction);
						m_processingHeightVelocityReadBack = false;
					}
				}
			}
		}

		private void ReadbackHeightVelocityCallback(AsyncGPUReadbackRequest request)
		{
			if (request.done && m_heightVelocityData.IsCreated)
			{
				if (request.hasError == false)
				{
#if FLUIDFRENZY_RPCORE_15_OR_NEWER
					using (new ProfilingScope(ProfilingSampler.Get(WaterSimProfileID.AsyncReadbackVelocityResult)))
#else
					using (new ProfilingScope(null, ProfilingSampler.Get(WaterSimProfileID.AsyncReadbackVelocityResult)))
#endif
					{
						NativeArray<long> data = request.GetData<long>(0);
						int idx = data.Length * m_currentHeightVelocityReadbackSlice;
						NativeArray<long>.Copy(data, 0, m_heightVelocityData, idx, data.Length);
					}
					m_currentHeightVelocityReadbackSlice = (m_currentHeightVelocityReadbackSlice + 1) % settings.readBackTimeSliceFrames;
				}
				m_processingHeightVelocityReadBack = true;
			}
		}

		protected void ReadBackSDF()
		{
			if (settings.distanceFieldReadback && m_processingSDFReadBack)
			{
				using (new ProfilingScope(m_dynamicCommandBuffer, ProfilingSampler.Get(WaterSimProfileID.AsyncReadbackDistanceField)))
				{
					int height = m_fluidSDF.height / settings.distanceFieldTimeSliceFrames;
					int y = height * m_currentSDFReadbackSlice;
					m_dynamicCommandBuffer.RequestAsyncReadback(m_fluidSDF, 0, 0, m_fluidSDF.width, 0, m_fluidSDF.height, 0, 1, m_readbackSDFAction);
					m_processingSDFReadBack = false;
				}
			}
		}

		private void ReadbackSDFCallback(AsyncGPUReadbackRequest request)
		{
			if (request.done && m_sdfData.IsCreated)
			{
				if (request.hasError == false)
				{
#if FLUIDFRENZY_RPCORE_15_OR_NEWER
					using (new ProfilingScope(ProfilingSampler.Get(WaterSimProfileID.AsyncReadbackDistanceFieldResult)))
#else
					using (new ProfilingScope(null, ProfilingSampler.Get(WaterSimProfileID.AsyncReadbackDistanceFieldResult)))
#endif
					{
						NativeArray<int> data = request.GetData<int>(0);
						int idx = data.Length * m_currentSDFReadbackSlice;
						NativeArray<int>.Copy(data, 0, m_sdfData, 0, data.Length);
					}
					m_currentSDFReadbackSlice = (m_currentSDFReadbackSlice + 1) % settings.distanceFieldTimeSliceFrames;
				}
				m_processingSDFReadBack = true;
			}
		}

		private static Vector2Int GetHeightTexturePos(Vector2 uvPos, int width, int height)
		{
			return new Vector2Int
			{
				x = (int)(uvPos.x * width),
				y = (int)(uvPos.y * height)
			};
		}


		private static int HeightToLayer(Vector2 height)
		{
			return height.x < 0 ? 2 : 1;
		}	

		private Vector2 PixelToHeight(long pixel)
		{
			return PixelToHeight(pixel, m_cachedPosition);
		}

		public static Vector2 PixelToHeight(long pixel, Vector3 position)
		{
			ushort s2 = (ushort)(pixel >> 16);
			ushort s1 = (ushort)(pixel & 0x0000FFFF);
			return new Vector2(GraphicsHelpers.HalfToAbsFloat(s1) + position.y, GraphicsHelpers.HalfToFloat(s2));
		}

		public static Vector3 PixelToVelocity(long pixel)
		{
			ushort s4 = (ushort)(pixel >> 48);
			ushort s3 = (ushort)(pixel >> 32);
			return new Vector3(GraphicsHelpers.HalfToFloat(s3), 0, GraphicsHelpers.HalfToFloat(s4));
		}

		private static long GetHeightVelocityPixel(NativeArray<long> heightVelocityData, Vector2Int texPos, int width, int height)
		{
			int x = Mathf.Clamp(texPos.x, 0, width - 1);
			int y = Mathf.Clamp(texPos.y, 0, height - 1);
			int index = x + y * width;
			return heightVelocityData[index];
		}

		private bool GetHeight(Vector2Int texPos, out Vector2 outPos)
		{
			long pixel = GetHeightVelocityPixel(m_heightVelocityData, texPos, m_renderDataWidth, m_renderDataHeight);
			outPos = PixelToHeight(pixel);
			return true;
		}

		private bool GetHeightLayer(Vector2Int texPos, out Vector2 outPos, out int outLayer)
		{
			long pixel = GetHeightVelocityPixel(m_heightVelocityData, texPos, m_renderDataWidth, m_renderDataHeight);
			outPos = PixelToHeight(pixel);
			outLayer = HeightToLayer(PixelToHeight(pixel));
			return true;
		}

		private bool GetHeightVelocity(Vector2Int texPos, out Vector2 outPos, out Vector3 outVel)
		{
			long pixel = GetHeightVelocityPixel(m_heightVelocityData, texPos, m_renderDataWidth, m_renderDataHeight);

			outPos = PixelToHeight(pixel);
			outVel = PixelToVelocity(pixel);
			return true;
		}

		/// <summary>
		/// Samples the height data of the <see cref="FluidSimulation"/> as the specified world position when inside the bounds of the <see cref="FluidSimulation"/>.
		/// </summary>
		/// <param name="worldPos">
		/// The world space position where the height of the <see cref="FluidSimulation"/> should be sampled.
		/// </param>
		/// <param name="heightData">
		/// The result of the sample's height. 
		/// heightData.x contains the total height in worldspace, including the height of the underlying terrain.
		/// heightData.y contains depth of the fluid in relation to the underlying terrain.
		/// This paramter will returns Vector2.zero if there is no valid height for this simulation or if <paramref name="worldPos"/> is outside of the simulation's bounds.
		/// </param>
		/// <returns>Returns true when valid data has been sampled.
		/// Returns false if there was no valid data or if if <paramref name="worldPos"/> is outside of the simulation's bounds.
		/// </returns>
		public bool GetHeight(Vector3 worldPos, out Vector2 heightData)
		{
			if (m_heightVelocityData == null || m_heightVelocityData.Length == 0 || !bounds.Contains(worldPos))
			{
				heightData = Vector2.zero;
				return false;
			}

			Vector2 uvPos = WorldSpaceToUVSpace(worldPos);

			Vector2Int texPos = GetHeightTexturePos(uvPos, m_renderDataWidth, m_renderDataHeight);
			return GetHeight(texPos, out heightData);
		}

		/// <summary>
		/// Samples the height and layer data of the <see cref="FluidSimulation"/> at the specified world position when inside the bounds of the <see cref="FluidSimulation"/>.
		/// </summary>
		/// <param name="worldPos">
		/// The world space position where the height of the <see cref="FluidSimulation"/> should be sampled.
		/// </param>
		/// <param name="heightData">
		/// The result of the sample's height. 
		/// heightData.x contains the total height in worldspace, including the height of the underlying terrain.
		/// heightData.y contains depth of the fluid in relation to the underlying terrain.
		/// This paramter will returns Vector2.zero if there is no valid height for this simulation or if <paramref name="worldPos"/> is outside of the simulation's bounds.
		/// </param>
		/// <param name="layer">
		/// The most dominant/heighest layer index in the fluid field.
		/// </param> 
		/// <returns>Returns true when valid data has been sampled.
		/// Returns false if there was no valid data or if if <paramref name="worldPos"/> is outside of the simulation's bounds.
		/// </returns>
		public bool GetHeightLayer(Vector3 worldPos, out Vector2 heightData, out int layer)
		{
			if (m_heightVelocityData == null || m_heightVelocityData.Length == 0 || !bounds.Contains(worldPos))
			{
				heightData = Vector2.zero;
				layer = 0;
				return false;
			}

			Vector2 uvPos = WorldSpaceToUVSpace(worldPos);

			Vector2Int texPos = GetHeightTexturePos(uvPos, m_renderDataWidth, m_renderDataHeight);
			return GetHeightLayer(texPos, out heightData, out layer);
		}

		/// <summary>
		/// Samples the height and velocity data of the <see cref="FluidSimulation"/> at the specified world position when inside the bounds of the <see cref="FluidSimulation"/>.
		/// </summary>
		/// <param name="worldPos">
		/// The world space position where the height and velocity of the <see cref="FluidSimulation"/> should be sampled.
		/// </param>
		/// <param name="heightData">
		/// The result of the sample's height. 
		/// heightData.x contains the total height in worldspace, including the height of the underlying terrain.
		/// heightData.y contains depth of the fluid in relation to the underlying terrain.
		/// This paramter will returns Vector2.zero if there is no valid height data for this simulation or if <paramref name="worldPos"/> is outside of the simulation's bounds.
		/// </param>
		/// <param name="velocity">
		/// The result of the sample's velocity in world space. 
		/// This paramter will returns Vector3.zero if there is no valid data for this simulation or if <paramref name="worldPos"/> is outside of the simulation's bounds.
		/// </param>
		/// <returns>Returns true when valid data has been sampled.
		/// Returns false if there was no valid data or if if <paramref name="worldPos"/> is outside of the simulation's bounds.
		/// </returns>
		public bool GetHeightVelocity(Vector3 worldPos, out Vector2 heightData, out Vector3 velocity)
		{
			if (m_heightVelocityData == null || m_heightVelocityData.Length == 0 || !bounds.Contains(worldPos))
			{
				heightData = Vector2.zero;
				velocity = Vector3.zero;
				return false;
			}

			Vector2 uvPos = WorldSpaceToUVSpace(worldPos);

			Vector2Int texPos = GetHeightTexturePos(uvPos, m_renderDataWidth, m_renderDataHeight);
			return GetHeightVelocity(texPos, out heightData, out velocity);
		}


		/// <summary>
		/// Samples the and calculates a world space surface normal of the <see cref="FluidSimulation"/> at the specified world position when inside the bounds of the <see cref="FluidSimulation"/>.
		/// </summary>
		/// <param name="worldPos">
		/// The world space position where the height of the <see cref="FluidSimulation"/> should be sampled.
		/// </param>
		/// <param name="normal">
		/// The resulting normal that is sampled and calculated for the specified <param name="worldPos">. 
		/// </param>
		/// <returns>Returns true when valid data has been sampled.
		/// Returns false if there was no valid data or if if <paramref name="worldPos"/> is outside of the simulation's bounds.
		/// </returns>
		public bool GetNormal(Vector3 worldPos, out Vector3 normal)
		{
			if (m_heightVelocityData == null || m_heightVelocityData.Length == 0 || !bounds.Contains(worldPos))
			{
				normal = Vector3.zero;
				return false;
			}

			Vector2 uvPos = WorldSpaceToUVSpace(worldPos);
			Vector2Int texPos = GetHeightTexturePos(uvPos, m_renderDataWidth, m_renderDataHeight);

			GetHeight(texPos + Vector2Int.right, out Vector2 h0);
			GetHeight(texPos - Vector2Int.right, out Vector2 h1);
			GetHeight(texPos + Vector2Int.up, out Vector2 h2);
			GetHeight(texPos - Vector2Int.up, out Vector2 h3);

			float wsTexelSizeX = dimension.x / m_renderDataWidth;
			float dhdu = ((h1.x) - (h0.x));
			float dhdv = ((h3.x) - (h2.x));
			normal = new Vector3(dhdu, wsTexelSizeX, dhdv).normalized;
			return true;
		}

		public static bool GetHeight(Vector3 worldPos, Bounds bounds, Vector3 dimension, int width, int height, Vector3 cachedPosition, in NativeArray<long> data, out Vector2 heightData)
		{
			Vector2 uvPos = WorldSpaceToUVSpace(worldPos, bounds, dimension);
			Vector2Int texPos = GetHeightTexturePos(uvPos, width, height);
			long pixel = GetHeightVelocityPixel(data, texPos, width, height);
			heightData = PixelToHeight(pixel, cachedPosition);
			return true;
		}

		public static bool GetHeightLayer(Vector3 worldPos, Bounds bounds, Vector3 dimension, int width, int height, Vector3 cachedPosition, in NativeArray<long> data, out Vector2 heightData, out int layer)
		{
			Vector2 uvPos = WorldSpaceToUVSpace(worldPos, bounds, dimension);
			Vector2Int texPos = GetHeightTexturePos(uvPos, width, height);
			long pixel = GetHeightVelocityPixel(data, texPos, width, height);
			heightData = PixelToHeight(pixel, cachedPosition);
			layer = HeightToLayer(heightData);
			return true;
		}

		public static bool GetHeightVelocity(Vector3 worldPos, Bounds bounds, Vector3 dimension, int width, int height, Vector3 cachedPosition, in NativeArray<long> data, out Vector2 heightData, out Vector3 velocity)
		{
			Vector2 uvPos = WorldSpaceToUVSpace(worldPos, bounds, dimension);
			Vector2Int texPos = GetHeightTexturePos(uvPos, width, height);
			long pixel = GetHeightVelocityPixel(data, texPos, width, height);
			heightData = PixelToHeight(pixel, cachedPosition);
			velocity = PixelToVelocity(pixel);
			return true;
		}

		#region SDF

		private Vector2Int GetSDFTexturePos(Vector2 uvPos)
		{
			return new Vector2Int
			{
				x = Mathf.RoundToInt(uvPos.x * m_sdfDataWidth),
				y = Mathf.RoundToInt(uvPos.y * m_sdfDataHeight)
			};
		}

		private static Vector2 PixelToUV(int pixel)
		{
			ushort s2 = (ushort)(pixel >> 16);
			ushort s1 = (ushort)(pixel & 0x0000FFFF);
			return new Vector2(Mathf.HalfToFloat(s1), Mathf.HalfToFloat(s2));
		}

		private int GetSDFPixel(Vector2Int texPos)
		{
			int x = Mathf.Clamp(texPos.x, 0, m_sdfDataWidth - 1);
			int y = Mathf.Clamp(texPos.y, 0, m_sdfDataHeight - 1);
			int index = x + y * m_sdfDataWidth;
			return m_sdfData[index];
		}

		private bool GetNearestFluidLocation(Vector2Int texPos, out Vector2 outUV)
		{
			int pixel = GetSDFPixel(texPos);

			outUV = PixelToUV(pixel);
			return true;
		}

		/// <summary>
		/// Samples the fluid distance field to find the nearest fluid location.
		/// </summary>
		/// <param name="worldPos">
		/// The world space position where the height of the <see cref="FluidSimulation"/> should be sampled.
		/// </param>
		/// <param name="fluidLocation">
		/// The resulting nearest location to <param name="worldPos"> containing fluid in 2D space. The x and z are the location sampled from the distance field. The Y is the <see cref="worldPos"/>.y 
		/// </param>
		/// <returns>Returns true when valid data has been sampled.
		/// Returns false if there was no valid data or if if <paramref name="worldPos"/> is outside of the simulation's bounds.
		/// </returns>
		public bool GetNearestFluidLocation2D(Vector3 worldPos, out Vector3 fluidLocation)
		{
			if (m_sdfData == null || m_sdfData.Length == 0 || !bounds.Contains(worldPos))
			{
				fluidLocation = Vector3.zero;
				return false;
			}

			Vector2 uvPos = WorldSpaceToUVSpace(worldPos);
			Vector2Int texPos = GetSDFTexturePos(uvPos);

			GetNearestFluidLocation(texPos, out Vector2 nearestUV);
			fluidLocation = new Vector3((nearestUV.x - 0.5f) * dimension.x, 0, (nearestUV.y - 0.5f) * dimension.y) + m_cachedTransform.position;
			fluidLocation.y = worldPos.y;
			return true;
		}
		[Obsolete("Replaced by GetNearestFluidLocation2D")]
		public bool GetNeartestFluidLocation2D(Vector3 worldPos, out Vector3 fluidLocation)
		{
			return GetNearestFluidLocation2D(worldPos, out fluidLocation);
		}

		/// <summary>
		/// Samples the fluid distance field to find the nearest fluid location.
		/// </summary>
		/// <param name="worldPos">
		/// The world space position where the height of the <see cref="FluidSimulation"/> should be sampled.
		/// </param>
		/// <param name="fluidLocation">
		/// The resulting nearest location to <param name="worldPos"> containing fluid.
		/// </param>
		/// <returns>Returns true when valid data has been sampled.
		/// Returns false if there was no valid data or if if <paramref name="worldPos"/> is outside of the simulation's bounds.
		/// </returns>
		public bool GetNearestFluidLocation3D(Vector3 worldPos, out Vector3 fluidLocation)
		{
			if (!GetNearestFluidLocation2D(worldPos, out fluidLocation))
				return false;

			GetHeight(fluidLocation, out Vector2 fluidHeight);
			fluidLocation.y = fluidHeight.x;

			return true;
		}

		[Obsolete("Replaced by GetNearestFluidLocation3D")]
		public bool GetNeartestFluidLocation3D(Vector3 worldPos, out Vector3 fluidLocation)
		{
			return GetNearestFluidLocation3D(worldPos, out fluidLocation);
		}


		#endregion
	}
}