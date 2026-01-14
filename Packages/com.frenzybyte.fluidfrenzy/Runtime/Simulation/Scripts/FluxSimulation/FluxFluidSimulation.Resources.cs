using UnityEngine;
using UnityEngine.Rendering;

namespace FluidFrenzy
{
	public partial class FluxFluidSimulation : FluidSimulation
	{
		//External input materials
		protected Material m_applyForceMaterial = null;
		protected int m_applyForceDirectionPass = 0;
		protected int m_applyForceSplashPass = 0;
		protected int m_applyForceVortexPass = 0;
		protected int m_applyForceTexturePass = 0;

		protected int m_dampenForceCirclePass = 0;
		protected int m_dampenForceTexturePass = 0;

		//Fluid Simulation Materials
		protected Material m_fluidSimulationMaterial = null;
		protected int m_fluidSimulationFluxPass = 0;
		protected int m_fluidSimulationApplyFluxPass = 0;
		protected Material m_integrateVelocityMaterial = null;

		//Velocity integration Materials
		protected Material m_solveVelocityMaterial = null;
		protected int m_advectVelocityPass = 0;
		protected int m_divergencePass = 0;
		protected int m_jacobianReducePressurePass = 0;
		protected int m_jacobianPass = 0;
		protected int m_applyPressurePass = 0;

		protected override void InitShaders()
		{
			base.InitShaders();


			m_fluidSimulationMaterial = new Material(Shader.Find("Hidden/FluidFrenzy/Simulation/Flux/FluxSimulation"));
			m_fluidSimulationFluxPass = m_fluidSimulationMaterial.FindPass("Flux");
			m_fluidSimulationApplyFluxPass = m_fluidSimulationMaterial.FindPass("ApplyFlux");
			if (multiLayeredFluid)
				m_fluidSimulationMaterial.EnableKeyword("FLUID_MULTILAYER");

			if(terrainType == TerrainType.UnityTerrain)
				m_fluidSimulationMaterial.EnableKeyword("_FLUID_UNITY_TERRAIN");

			m_integrateVelocityMaterial = new Material(Shader.Find("Hidden/FluidFrenzy/IntegrateVelocity"));
			if (multiLayeredFluid)
				m_integrateVelocityMaterial.EnableKeyword("FLUID_MULTILAYER");

			m_solveVelocityMaterial = new Material(Shader.Find("Hidden/FluidFrenzy/SolveVelocity"));
			m_advectVelocityPass = m_solveVelocityMaterial.FindPass("AdvectVelocity");
			m_divergencePass = m_solveVelocityMaterial.FindPass("Divergence");
			m_jacobianReducePressurePass = m_solveVelocityMaterial.FindPass("JacobianReducePressure");
			m_jacobianPass = m_solveVelocityMaterial.FindPass("Jacobian");
			m_applyPressurePass = m_solveVelocityMaterial.FindPass("ApplyPressure");

			m_applyForceMaterial = new Material(Shader.Find("Hidden/FluidFrenzy/ApplyForce"));
			m_applyForceDirectionPass = m_applyForceMaterial.FindPass("ApplyForceDirection");
			m_applyForceSplashPass = m_applyForceMaterial.FindPass("ApplyForceSplash");
			m_applyForceVortexPass = m_applyForceMaterial.FindPass("ApplyForceVortex");
			m_applyForceTexturePass = m_applyForceMaterial.FindPass("ApplyForceTexture");

			m_dampenForceCirclePass = m_applyForceMaterial.FindPass("DampenForceCircle");
			m_dampenForceTexturePass = m_applyForceMaterial.FindPass("DampenForceTexture");
		}

		protected override void DestroyMaterials()
		{
			base.DestroyMaterials();
			Destroy(m_applyForceMaterial);
			Destroy(m_fluidSimulationMaterial);
			Destroy(m_integrateVelocityMaterial);
			Destroy(m_solveVelocityMaterial);
		}
	}
}