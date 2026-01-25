using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;

namespace FluidFrenzy
{
	public partial class FluxFluidSimulation : FluidSimulation
	{
		/// <summary>
		/// The current active outflow field used to simulate the fluid waves and terrain intersection.
		/// </summary>
		public RenderTexture outFlowTexture { get { return m_activeOutFlow; } private set { } }
		/// <summary>
		/// The outflow amount from external forces to be applied to the next outflow field.
		/// </summary>
		public RenderTexture addedOutFlowTexture { get { return m_externalOutFlow; } private set { } }
		/// <summary>
		/// The current divergence of the <see cref="velocityTexture"/> used for calculating the pressure in the velocty field.
		/// </summary>
		public RenderTexture divergenceTexture { get { return m_divergence; } private set { } }
		/// <summary>
		/// The current pressure of the <see cref="velocityTexture"/> used for causing swirling and border effects in the velocity field used in <see cref="FoamLayer"/>, <see cref="FluidFlowMapping"/> and <see cref="ErosionLayer"/>.
		/// </summary>
		public RenderTexture pressureTexture { get { return m_activePressure; } private set { } }
		/// <summary>
		/// The current velocity calculated from the currently active <see cref="m_activeOutFlow">out flow field</see>.
		/// When using <see cref="FluidSimulationSettings.additiveVelocity"/> this is the velocity that is added every frame to <see cref="m_activeHeightVelocityTexture"/>.
		/// When <see cref="FluidSimulationSettings.additiveVelocity"/> this velocity is used directly in for effects like <see cref="FoamLayer"/>, <see cref="FluidFlowMapping"/>, and <see cref="ErosionLayer"/>
		/// </summary>
		public RenderTexture outflowVelocity { get { return m_outFlowVelocity; } private set { } }
		/// <summary>
		/// Returns the size that the velocity buffer should be in a <see cref="FluidSimulation"></see>.
		/// </summary>
		protected internal override Vector2Int velocityTextureSize { get { return m_internalSettings.velocityTextureSize; } protected set { } }
		/// <summary>
		/// Returns the advection scale for this <see cref="FluidSimulation"/></see>.
		/// </summary>
		protected internal override float advectionScale { get { return m_internalSettings.advectionScale; } protected set { } }
		/// <summary>
		/// Returns the velocity scale for this <see cref="FluidSimulation"/></see>.
		/// </summary>
		protected internal override float velocityScale { get { return m_internalSettings.velocityScale; } protected set { } }
	}
}