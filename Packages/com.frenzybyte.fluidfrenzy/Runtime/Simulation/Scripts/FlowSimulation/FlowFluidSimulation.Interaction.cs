using UnityEngine;
using UnityEngine.Rendering;

namespace FluidFrenzy
{
	public partial class FlowFluidSimulation : FluidSimulation
	{
		protected override void BindSolidToFluidShared(int kernel, Matrix4x4 localToWorld, Matrix4x4 prevLocalToWorld, FluidRigidBody.FluidDisplacementProfile displacementProfile)
		{
			base.BindSolidToFluidShared(kernel, localToWorld, prevLocalToWorld, displacementProfile);
			m_dynamicCommandBuffer.SetComputeTextureParam(m_solidToFluidCS, kernel, FluidShaderProperties._VelocityField, m_activeVelocity);
		}

		protected override void UpdateSolidToFluid()
		{
			m_dynamicCommandBuffer.SetComputeTextureParam(m_solidToFluidCS, m_solidToFluidApplyAccumulatedDeltasKernel, FluidShaderProperties._FluidHeightField, m_activeWaterHeight);
			m_dynamicCommandBuffer.SetComputeTextureParam(m_solidToFluidCS, m_solidToFluidApplyAccumulatedDeltasKernel, FluidShaderProperties._VelocityField, m_activeVelocity);
			m_dynamicCommandBuffer.SetComputeTextureParam(m_solidToFluidCS, m_solidToFluidApplyAccumulatedDeltasKernel, FluidShaderProperties._HeightTarget, m_nextWaterHeight);
			m_dynamicCommandBuffer.SetComputeTextureParam(m_solidToFluidCS, m_solidToFluidApplyAccumulatedDeltasKernel, FluidShaderProperties._VelocityTarget, m_nextVelocity);

			m_dynamicCommandBuffer.SetComputeBufferParam(m_solidToFluidCS, m_solidToFluidApplyAccumulatedDeltasKernel, FluidShaderProperties._HeightAccumulator, m_solidToFluidHeightDelta);
			m_dynamicCommandBuffer.SetComputeBufferParam(m_solidToFluidCS, m_solidToFluidApplyAccumulatedDeltasKernel, FluidShaderProperties._VelocityAccumulator, m_solidToFluidVelocityDelta);

			m_dynamicCommandBuffer.SetComputeIntParam(m_solidToFluidCS, FluidShaderProperties._BufferWidth, m_activeWaterHeight.width);


			m_solidToFluidCS.GetKernelThreadGroupSizes(m_solidToFluidApplyAccumulatedDeltasKernel, out uint x, out uint y, out _);
			m_dynamicCommandBuffer.DispatchCompute(m_solidToFluidCS, m_solidToFluidApplyAccumulatedDeltasKernel,
													GraphicsHelpers.DCS(m_activeWaterHeight.width, x),
													GraphicsHelpers.DCS(m_activeWaterHeight.height, y), 1);

			SwapVelocity();
			SwapFluidRT();
		}

	/// <summary>
	/// Applies a force to the <see cref="FluxFluidSimulation"/> at a specified world position.
	/// </summary>
	/// <param name="worldPos">The world position where the force is applied.</param>
	/// <param name="direction">The direction of the force.</param>
	/// <param name="size">The size of the area affected by the force.</param>
	/// <param name="strength">The strength of the force effect.</param>
	/// <param name="falloff">The falloff used to control the gradient of the force effect.</param>
	/// <param name="timestep">The time delta since the last update.</param>
	/// <param name="splash">If true, applies the force as a splash(outward) effect; otherwise, applies as a directional force.</param>

	public override void ApplyForce(Vector3 worldPos, Vector2 direction, Vector2 size, float strength, float falloff, float timestep, bool splash)
		{
			if ((direction.sqrMagnitude < Mathf.Epsilon || falloff < Mathf.Epsilon) && !splash)
				return;
			using (new ProfilingScope(m_dynamicCommandBuffer, ProfilingSampler.Get(WaterSimProfileID.Velocity)))
			{
				Vector2 position = WorldSpaceToPaddedUVSpace(worldPos);
				Vector2 uvSize = WorldSizeToUVSize(size);

				m_externalPropertyBlock.Clear();
				m_externalPropertyBlock.SetVector(FluidShaderProperties._BlitScaleBiasRt, new Vector4(uvSize.x, uvSize.y, position.x, position.y));
				m_externalPropertyBlock.SetVector(FluidShaderProperties._VelocityDir, direction);
				m_externalPropertyBlock.SetFloat(FluidShaderProperties._IncreaseStrength, strength * timestep);
				m_externalPropertyBlock.SetFloat(FluidShaderProperties._IncreaseExponent, falloff);
				BlitQuad(m_dynamicCommandBuffer, null, m_activeVelocity, m_applyVelocityMaterial, m_externalPropertyBlock, splash ? m_applyVelocityOutward : m_applyVelocityDirection);
			}
		}

		/// <summary>
		/// Applies a force effect based on a texture to the <see cref="FluidSimulation"/>.
		/// </summary>
		/// <param name="texture">The texture that defines the force effect.</param>
		/// <param name="strength">The strength of the force effect.</param>
		/// <param name="timestep">The time delta since the last update.</param>
		public override void ApplyForce(Texture texture, float strength, float timestep)
		{
			using (new ProfilingScope(m_dynamicCommandBuffer, ProfilingSampler.Get(WaterSimProfileID.ExternalForceTexture)))
			{
				m_internalPropertyBlock.Clear();
				m_internalPropertyBlock.SetFloat(FluidShaderProperties._IncreaseStrength, strength * timestep);
				m_internalPropertyBlock.SetVector(FluidShaderProperties._BlitScaleBiasRt, new Vector4(1.0f, 1.0f, 0.5f, 0.5f));
				BlitQuad(m_dynamicCommandBuffer, texture, m_activeVelocity, m_applyVelocityMaterial, m_internalPropertyBlock, m_applyVelocityTexture);
			}
		}

		/// <summary>
		/// Applies a force effect based on a texture to the <see cref="FluidSimulation"/>.
		/// </summary>
		/// <param name="texture">The texture that defines the force effect.</param>
		/// <param name="strength">The strength of the force effect.</param>
		/// <param name="timestep">The time delta since the last update.</param>
		public override void ApplyForce(int texture, float strength, float timestep)
		{
			using (new ProfilingScope(m_dynamicCommandBuffer, ProfilingSampler.Get(WaterSimProfileID.ExternalForceTexture)))
			{
				m_internalPropertyBlock.Clear();
				m_internalPropertyBlock.SetFloat(FluidShaderProperties._IncreaseStrength, strength * timestep);
				m_internalPropertyBlock.SetVector(FluidShaderProperties._BlitScaleBiasRt, new Vector4(1.0f, 1.0f, 0.5f, 0.5f));
				BlitQuad(m_dynamicCommandBuffer, texture, m_activeVelocity, m_applyVelocityMaterial, m_internalPropertyBlock, m_applyVelocityTexture);
			}
		}
	}
}