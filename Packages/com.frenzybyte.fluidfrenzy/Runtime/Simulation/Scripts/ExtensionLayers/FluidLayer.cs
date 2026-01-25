using System.Collections.Generic;
using UnityEngine;

namespace FluidFrenzy
{
	/// <summary>
	/// <see cref="FluidLayer"/> is the base class that can be used to extend the <see cref="FluidSimulation"/> by building extra layers ontop of it.
	/// Internally this is used for <see cref="FluidFlowMapping"/>, <see cref="FoamLayer"/>, <see cref="ErosionLayer"/>, and <see cref="TerraformLayer"/>.
	/// The <see cref="FluidSimulation"/> can be interacted with in the <see cref="Step(FluidSimulation, float, int)"/> function, which will be called once per <see cref="FluidSimulation.Step(float)"/>.
	/// </summary>
	public abstract class FluidLayer : MonoBehaviour
	{
		public RenderTexture activeLayer { get; protected set; }
		public RenderTexture nextLayer { get; protected set; }
		public Vector4 textureST { get { return m_textureST; } protected set { m_textureST = value; } }
		public virtual bool copyNeighbours { get { return false; } }
		public Vector2 dimension { get { return m_parentSimulation.dimension; } }

		private Vector4 m_textureST = Vector2.one;
		protected FluidSimulation m_parentSimulation;

		public abstract void Awake();

		public abstract void OnDestroy();

		/// <summary>
		/// Copies all the relevation data to this <see cref="FluidLayer"/> from the <paramref name="source"/> <see cref="FluidLayer"/>.
		/// </summary>
		public abstract void CopyFrom(FluidLayer source);

		public virtual void Init(FluidSimulation simulation)
		{
			m_parentSimulation = simulation;
		}

		public abstract void ResetLayer(FluidSimulation simulation);

		public abstract void Step(FluidSimulation simulation, float deltaTime, int step);
		protected virtual void SwapActiveLayer()
		{
			RenderTexture tmp = nextLayer;
			nextLayer = activeLayer;
			activeLayer = tmp;
		}

		public abstract RenderTexture GetDebugBuffer(FluidSimulation.DebugBuffer buffer);

		public abstract IEnumerable<FluidSimulation.DebugBuffer> EnumerateBuffers();
	}
}