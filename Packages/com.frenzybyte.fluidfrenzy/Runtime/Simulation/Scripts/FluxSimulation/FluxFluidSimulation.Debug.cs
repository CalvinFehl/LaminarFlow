using System;
using System.Collections.Generic;
using UnityEngine;

namespace FluidFrenzy
{
	public partial class FluxFluidSimulation : FluidSimulation
	{
		public override RenderTexture GetDebugBuffer(DebugBuffer buffer)
		{
			RenderTexture rt = base.GetDebugBuffer(buffer);
			if(!rt)
			{
				switch (buffer)
				{
					case DebugBuffer.Divergence:
						return m_divergence;
					case DebugBuffer.Pressure:
						return m_activePressure;
					case DebugBuffer.Outflow:
						return m_activeOutFlow;					
					case DebugBuffer.OutflowVelocity:
						return m_outFlowVelocity;
				}
			}

			return rt;
		}

		public override IEnumerable<DebugBuffer> EnumerateBuffers()
		{
			foreach (var number in base.EnumerateBuffers())
			{
				yield return number;
			}

			yield return DebugBuffer.Divergence;
			yield return DebugBuffer.Pressure;
			yield return DebugBuffer.Outflow;
			yield return DebugBuffer.OutflowVelocity;
		}
	}
}