using UnityEngine;

namespace FluidFrenzy
{
	public partial class FlowFluidSimulation : FluidSimulation
	{
		Material m_fluidSolverMaterial;
		protected int m_fluidSolverInitPass = 0;
		protected int m_fluidSolverCopyTerrainPass = 0;
		protected int m_fluidSolverAdvectPass = 0;
		protected int m_fluidSolverIntegrateHeightPass = 0;
		protected int m_fluidSolverIntegrateVelocityPass = 0;
		protected int m_fluidSolverOvershootReductionPass = 0;


		Material m_fluidSolverSolidsToFluid;
		protected int m_fluidSolverSolidsToFluidPass = 0;
		protected override void InitShaders()
		{
			base.InitShaders();

			m_fluidSolverMaterial = new Material(Shader.Find("Hidden/FluidFrenzy/Simulation/Flow/FlowSimulation"));

			m_fluidSolverInitPass = m_fluidSolverMaterial.FindPass("Init");
			m_fluidSolverCopyTerrainPass = m_fluidSolverMaterial.FindPass("CopyTerrain");
			m_fluidSolverAdvectPass = m_fluidSolverMaterial.FindPass("AdvectVelocity");
			m_fluidSolverIntegrateHeightPass = m_fluidSolverMaterial.FindPass("IntegrateHeight");
			m_fluidSolverIntegrateVelocityPass = m_fluidSolverMaterial.FindPass("IntegrateVelocity");
			m_fluidSolverOvershootReductionPass = m_fluidSolverMaterial.FindPass("OvershootReduction");

			m_fluidSolverSolidsToFluid = new Material(Shader.Find("Hidden/FluidFrenzy/Simulation/Flow/SolidsToFluid"));
			m_fluidSolverSolidsToFluidPass = m_fluidSolverSolidsToFluid.FindPass("ApplyDirection");

			if (terrainType == TerrainType.UnityTerrain)
				m_fluidSolverMaterial.EnableKeyword("_FLUID_UNITY_TERRAIN");

			if (settings.secondLayer)
			{
				m_fluidSolverMaterial.EnableKeyword("FLUID_MULTILAYER");
			}
		}

		protected override void DestroyMaterials()
		{
			base.DestroyMaterials();
			Destroy(m_fluidSolverMaterial);
		}
	}
}