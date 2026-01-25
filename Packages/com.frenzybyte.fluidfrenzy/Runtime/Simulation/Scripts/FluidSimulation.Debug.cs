using System;
using System.Collections.Generic;
using UnityEngine;

namespace FluidFrenzy
{
	public partial class FluidSimulation : MonoBehaviour
	{
		public enum DebugBuffer
		{
			Terrain,
			Obstacles,
			Depth,
			Fluid,
			
			Velocity,
			Divergence,
			Pressure,

			Outflow,
			OutflowVelocity,

			RenderData,
			NormalMap,
			SDF,

			FlowUV,
			Foam,
			Sediment,
			Slippage,
			ErosionTerrain,

			SplashAndSpray,
		}

		public virtual Vector3 GetDebugLevels(DebugBuffer buffer)
		{
			switch (buffer)
			{
				case DebugBuffer.Terrain:
					{
						if(terrainType == TerrainType.UnityTerrain)
							return new Vector3(0, 1, 1);
						else if (terrainType == TerrainType.SimpleTerrain)
							return new Vector3(0, simpleTerrain.heightScale, 1);						
						else if (terrainType == TerrainType.Heightmap)
							return new Vector3(0, heightmapScale, 1);
						else if (terrainType == TerrainType.MeshCollider && meshCollider)
							return new Vector3(0, meshCollider.bounds.size.y * 2, 1);
						break;
					}
				case DebugBuffer.Obstacles:
					return new Vector3(0, terrainScale, 1);
				case DebugBuffer.Fluid:
					return new Vector3(0, 10, 1);
				case DebugBuffer.Velocity:
					return new Vector3(0, 5, 1);
				case DebugBuffer.RenderData:
					if (terrainType == TerrainType.UnityTerrain)
						return new Vector3(0, unityTerrain.terrainData.heightmapScale.y * (65535.0f / 32766), 1);
					else if (terrainType == TerrainType.SimpleTerrain)
						return new Vector3(0, simpleTerrain.heightScale, 1);
					else if (terrainType == TerrainType.Heightmap)
						return new Vector3(0, heightmapScale, 1);
					else if (terrainType == TerrainType.MeshCollider && meshCollider)
						return new Vector3(0, meshCollider.bounds.size.y * 2, 1);
					break;
				case DebugBuffer.NormalMap:
					return new Vector3(0, 1, 1);
			}

			return new Vector3(0, 1, 1);
		}

		public virtual RenderTexture GetDebugBuffer(DebugBuffer buffer)
		{
			switch(buffer)
			{
				case DebugBuffer.Terrain:
					return m_terrainHeight;
				case DebugBuffer.Obstacles:
					return m_obstacleHeight;				
				case DebugBuffer.Fluid:
					return m_activeWaterHeight;
				case DebugBuffer.Velocity:
					return m_activeVelocity;
				case DebugBuffer.RenderData:
					return m_activeHeightVelocityTexture;
				case DebugBuffer.NormalMap:
					return m_normalMap;				
				case DebugBuffer.SDF:
					return m_fluidSDF;				
			}

			foreach (FluidLayer layer in extensionLayers)
			{
				RenderTexture rt = layer?.GetDebugBuffer(buffer);
				if(rt)
				{
					return rt;
				}
			}

			return null;
		}

		public virtual IEnumerable<DebugBuffer> EnumerateBuffers()
		{
			yield return DebugBuffer.Terrain;
			yield return DebugBuffer.Obstacles;
			yield return DebugBuffer.Depth;
			yield return DebugBuffer.Fluid;
			yield return DebugBuffer.Velocity;
			yield return DebugBuffer.RenderData;
			yield return DebugBuffer.NormalMap;
			yield return DebugBuffer.SDF;

			foreach (FluidLayer layer in extensionLayers)
			{
				foreach (var buffer in layer?.EnumerateBuffers())
				{
					yield return buffer;
				}
			}
		}
	}
}