using Unity.Collections;
using UnityEngine;

namespace FluidFrenzy
{

	public partial class FluidSimulation : MonoBehaviour
	{
		public struct FluidSimulationJobData
		{
			public Bounds bounds;
			public Vector2Int resolution;
			public Vector3 dimension;
			public Vector3 cachedPosition;
			public NativeArray<long> heightVelocityData;
		}

		// This method packages the simulation's data into the Burst-friendly struct.
		public FluidSimulationJobData GetSimulationDataForJob()
		{
			return new FluidSimulationJobData
			{
				bounds = bounds,
				heightVelocityData = m_heightVelocityData,
				resolution = new Vector2Int(m_renderDataWidth, m_renderDataHeight),
				dimension = dimension,
				cachedPosition = m_cachedPosition
			};
		}
	}
}