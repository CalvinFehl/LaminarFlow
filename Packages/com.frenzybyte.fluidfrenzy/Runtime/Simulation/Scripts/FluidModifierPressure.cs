using UnityEngine;

namespace FluidFrenzy
{
	/// <summary>
	/// A specialized <see cref="FluidModifier"/> that applies vertical displacement forces based on the internal pressure of the fluid.
	/// </summary>
	/// <remarks>
	/// This component simulates the physical phenomenon where fluid "piles up" when colliding with obstacles or terrain. It creates localized elevation in high-pressure zones, effectively bulging waves and dips.
	/// <para>
	/// <b>Requirement:</b> This modifier relies on pressure field data, which is only calculated by the <see cref="FluxFluidSimulation"/>. This component will have no effect if used with a <see cref="FlowFluidSimulation"/>.
	/// </para>
	/// </remarks>
	[HelpURL("https://frenzybyte.github.io/fluidfrenzy/docs/fluid_modifiers/#fluid-modifier-pressure")]
	public class FluidModifierPressure : FluidModifier
	{
		/// <summary>
		/// Defines the pressure threshold range for applying displacement forces.
		/// </summary>
		/// <remarks>
		/// <list type="bullet">
		/// 	<item>
		/// 		<term>X (Min)</term>
		/// 		<description>Pressure values below this threshold generate no displacement.</description>
		/// 	</item>
		/// 	<item>
		/// 		<term>Y (Max)</term>
		/// 		<description>Pressure values above this threshold apply the full displacement strength.</description>
		/// 	</item>
		/// </list>
		/// Intermediate values are interpolated using <see href="https://en.wikipedia.org/wiki/Smoothstep">Smoothstep</see>.
		/// </remarks>
		public Vector2 pressureRange = new Vector2(0, 1);

		/// <summary>
		/// A global multiplier applied to the displacement force in high-pressure regions.
		/// </summary>
		/// <remarks>
		/// Higher values result in more exaggerated peaks where the fluid accumulates against obstacles.
		/// </remarks>
		public float strength = 1;

		Material m_pressureInputMaterial;
		RenderTexture m_pressureInput1 = null;
		RenderTexture m_pressureInput2 = null;

		MaterialPropertyBlock m_accumulatePressureBlock;
		MaterialPropertyBlock m_applyPressureBlock;

		protected override void Awake()
		{
			base.Awake();
			m_pressureInputMaterial = new Material(Shader.Find("Hidden/FluidFrenzy/PressureInput"));

			m_accumulatePressureBlock = new MaterialPropertyBlock();
			m_applyPressureBlock = new MaterialPropertyBlock();
		}

        protected override void OnDestroy()
        {
			GraphicsHelpers.ReleaseSimulationRT(m_pressureInput1);
			GraphicsHelpers.ReleaseSimulationRT(m_pressureInput2);
			Destroy(m_pressureInputMaterial);
            base.OnDestroy();
        }

        public override void Process(FluidSimulation fluidSim, float dt)
		{
			if (!(fluidSim is FluxFluidSimulation fluxFluidSim))
				return;

			if (!m_pressureInput1)
				m_pressureInput1 = new RenderTexture(fluxFluidSim.pressureTexture.descriptor);
			if (!m_pressureInput2)
				m_pressureInput2 = new RenderTexture(fluxFluidSim.pressureTexture.descriptor);

			m_accumulatePressureBlock.SetTexture(FluidShaderProperties._PressureField, fluxFluidSim.pressureTexture);
			m_accumulatePressureBlock.SetTexture(FluidShaderProperties._PreviousPressureField, m_pressureInput1);
			m_accumulatePressureBlock.SetVector(FluidShaderProperties._BlitScaleBias, Vector2.one);
			m_accumulatePressureBlock.SetVector(FluidShaderProperties._VelocityScale, new Vector2(1.0f / fluidSim.velocityScale, 1.0f / fluidSim.velocityScale));
			fluidSim.BlitQuadExternal(null, m_pressureInput2, m_pressureInputMaterial, m_accumulatePressureBlock, 1);

			m_applyPressureBlock.SetTexture(FluidShaderProperties._PressureField, m_pressureInput2);
			m_applyPressureBlock.SetFloat(FluidShaderProperties._Strength, strength * dt);
			m_applyPressureBlock.SetVector(FluidShaderProperties._PressureMinMax, pressureRange);
			m_applyPressureBlock.SetVector(FluidShaderProperties._BlitScaleBias, fluidSim.velocityTextureST);
			fluidSim.BlitQuadExternal(null, fluxFluidSim.addedOutFlowTexture, m_pressureInputMaterial, m_applyPressureBlock, 0);

			RenderTexture tmp = m_pressureInput2;
			m_pressureInput2 = m_pressureInput1;
			m_pressureInput1 = tmp;
		}
	}
}