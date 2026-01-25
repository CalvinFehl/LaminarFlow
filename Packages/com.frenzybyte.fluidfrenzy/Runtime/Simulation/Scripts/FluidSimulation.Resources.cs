using UnityEngine;
using UnityEngine.Rendering;

namespace FluidFrenzy
{
	public partial class FluidSimulation : MonoBehaviour
	{
		//External input materials
		protected Material m_applyVelocityMaterial = null;
		protected int m_applyVelocityDirection = 0;
		protected int m_applyVelocityVortexAdditive = 0;
		protected int m_applyVelocityTexture = 0;
		protected int m_applyVelocityTextureRemapped = 0;
		protected int m_applyVelocityOutward = 0;

		protected int m_setVelocityVortexBlend = 0;
		protected int m_setVelocityDirection = 0;
		protected int m_setVelocityTextureRemapped = 0;

		protected int m_dampenVelocityCircle = 0;
		protected int m_dampenVelocityTexture = 0;

		protected Material m_addFluidMaterial = null;
		protected int m_addFluidCirclePass = 0;
		protected int m_addFluidSquarePass = 0;
		protected int m_addFluidTextureDynamicPass = 0;
		protected int m_addFluidTexturePass = 0;
		protected int m_addFluidTextureStaticPass = 0;

		protected int m_mixFluidHeightCirclePass = 0;
		protected int m_mixFluidHeightSquarePass = 0;
		protected int m_mixFluidHeightTexturePass = 0;

		protected int m_mixFluidDepthCirclePass = 0;
		protected int m_mixFluidDepthSquarePass = 0;
		protected int m_mixFluidDepthTexturePass = 0;

		//Fluid Simulation Materials
		protected Material m_initSimulationMaterial = null;
		protected int m_initSimulationCopyHeightmapPass = 0;
		protected int m_initSimulationCopyHeightmapCombinePass = 0;
		protected int m_initSimulationCopyTerrainPass = 0;
		protected int m_initSimulationFluidHeightPass = 0;
		protected int m_initSimulationFluidHeightUnityPass = 0;
		protected int m_initSimulationCopyFromDepthPass = 0;

		protected Material m_jumpFloodMaterial = null;
		protected int m_jumpFloodInit = 0;
		protected int m_jumpFloodStep = 1;

		protected Material m_createRenderDataMaterial = null;

		protected Material m_boundaryMaterial = null;
		protected int m_boundaryResetPass = 0;
		protected int m_boundaryStorePass = 0;
		protected int m_boundaryCopyPass = 0;
		protected int m_boundarySetPass = 0;

		protected Material m_copyTextureMaterial = null;

		protected Material m_obstacleMaterial = null;
		protected Material m_obstacleProceduralMaterial = null;

		protected ComputeShader m_solidToFluidCS = null;
		protected int m_solidToFluidClearBuffersKernel = 0;
		protected int m_solidToFluidMeshKernel = 0;
		protected int m_solidToFluidSphereKernel = 0;
		protected int m_solidToFluidBoxKernel = 0;
		protected int m_solidToFluidCapsuleKernel = 0;
		protected int m_solidToFluidApplyAccumulatedDeltasKernel = 0;

		protected virtual void InitShaders()
		{
			m_initSimulationMaterial = new Material(Shader.Find("Hidden/FluidFrenzy/InitSimulation"));
			if (Application.platform != RuntimePlatform.WebGLPlayer)
			{
				m_initSimulationMaterial.EnableKeyword("_ALLOW_SCALE");
			}

			m_initSimulationCopyHeightmapPass = m_initSimulationMaterial.FindPass("CopyHeightmap");
			m_initSimulationCopyHeightmapCombinePass = m_initSimulationMaterial.FindPass("CopyHeightmapCombine");
			m_initSimulationCopyTerrainPass = m_initSimulationMaterial.FindPass("CopyUnityTerrain");
			m_initSimulationCopyFromDepthPass = m_initSimulationMaterial.FindPass("CopyFromDepth");
			m_initSimulationFluidHeightPass = m_initSimulationMaterial.FindPass("FluidHeight");
			m_initSimulationFluidHeightUnityPass = m_initSimulationMaterial.FindPass("FluidHeightUnityTerrain");

			m_obstacleMaterial = new Material(Shader.Find("Hidden/FluidFrenzy/ObjectToHeightmap"));
			m_obstacleProceduralMaterial = new Material(Shader.Find("Hidden/FluidFrenzy/ProceduralObstacle"));

			m_createRenderDataMaterial = new Material(Shader.Find("Hidden/FluidFrenzy/FluidCreateRenderData"));
			if (multiLayeredFluid)
			{
				if (m_hasSecondVelocityLayer)
					m_createRenderDataMaterial.EnableKeyword("FLUID_MULTILAYER_VELOCITY");
				else
					m_createRenderDataMaterial.EnableKeyword("FLUID_MULTILAYER");
			}

			m_jumpFloodMaterial = new Material(Shader.Find("Hidden/FluidFrenzy/JumpFlood"));
			m_jumpFloodInit = m_jumpFloodMaterial.FindPass("InitJumpFlood");
			m_jumpFloodStep = m_jumpFloodMaterial.FindPass("StepJumpFlood");

			if (terrainType == TerrainType.UnityTerrain)
				m_createRenderDataMaterial.EnableKeyword("_FLUID_UNITY_TERRAIN");

			if (simulationType == FluidSimulationType.Flow)
				m_createRenderDataMaterial.EnableKeyword("FLUID_FLOW_SIMULATION");

			m_boundaryMaterial = new Material(Shader.Find("Hidden/FluidFrenzy/Boundary"));
			m_boundaryResetPass = m_boundaryMaterial.FindPass("ResetBoundary");
			m_boundaryStorePass = m_boundaryMaterial.FindPass("StoreBoundary");
			m_boundaryCopyPass = m_boundaryMaterial.FindPass("CopyBoundary");
			m_boundarySetPass = m_boundaryMaterial.FindPass("SetBoundary");

			m_copyTextureMaterial = new Material(Shader.Find("Hidden/FluidFrenzy/CopyTexture"));

			m_addFluidMaterial = new Material(Shader.Find("Hidden/FluidFrenzy/AddFluid"));
			m_addFluidCirclePass = m_addFluidMaterial.FindPass("AddFluidCircle");
			m_addFluidSquarePass = m_addFluidMaterial.FindPass("AddFluidSquare");
			m_addFluidTexturePass = m_addFluidMaterial.FindPass("AddFluidTexture");
			m_addFluidTextureStaticPass = m_addFluidMaterial.FindPass("AddFluidTextureStatic");
			m_addFluidTextureDynamicPass = m_addFluidMaterial.FindPass("AddFluidTextureDynamic");

			m_mixFluidHeightCirclePass = m_addFluidMaterial.FindPass("MixFluidHeightCircle");
			m_mixFluidHeightSquarePass = m_addFluidMaterial.FindPass("MixFluidHeightSquare");			
			m_mixFluidHeightTexturePass = m_addFluidMaterial.FindPass("MixFluidHeightTexture");			
			
			m_mixFluidDepthCirclePass = m_addFluidMaterial.FindPass("MixFluidDepthCircle");
			m_mixFluidDepthSquarePass = m_addFluidMaterial.FindPass("MixFluidDepthSquare");
			m_mixFluidDepthTexturePass = m_addFluidMaterial.FindPass("MixFluidDepthTexture");

			if (terrainType == TerrainType.UnityTerrain)
				m_addFluidMaterial.EnableKeyword("_FLUID_UNITY_TERRAIN");

			m_applyVelocityMaterial = new Material(Shader.Find("Hidden/FluidFrenzy/ApplyVelocity"));
			m_applyVelocityDirection = m_applyVelocityMaterial.FindPass("ApplyVelocityDirection");
			m_applyVelocityTexture = m_applyVelocityMaterial.FindPass("ApplyVelocityTexture");
			m_applyVelocityTextureRemapped = m_applyVelocityMaterial.FindPass("ApplyVelocityTextureRemapped");

			m_setVelocityDirection = m_applyVelocityMaterial.FindPass("SetVelocityDirection");
			m_setVelocityTextureRemapped = m_applyVelocityMaterial.FindPass("SetVelocityTextureRemapped");

			m_dampenVelocityCircle = m_applyVelocityMaterial.FindPass("DampenVelocityCircle");
			m_dampenVelocityTexture = m_applyVelocityMaterial.FindPass("DampenVelocityTexture");

			if (this is FluxFluidSimulation)
			{
				m_setVelocityVortexBlend = m_applyVelocityMaterial.FindPass("ApplyVelocityVortexBlend");
				m_applyVelocityVortexAdditive = m_applyVelocityMaterial.FindPass("ApplyVelocityVortexAdd");
			}
			else
			{
				m_setVelocityVortexBlend = m_applyVelocityMaterial.FindPass("SetVelocityVortexFlowSim");
				m_applyVelocityVortexAdditive = m_applyVelocityMaterial.FindPass("ApplyVelocityVortexFlowSim"); 
			}

			if (SystemInfo.supportsComputeShaders)
			{
				m_solidToFluidCS = Resources.Load("FluidInput/SolidToFluid") as ComputeShader;
				m_solidToFluidClearBuffersKernel = m_solidToFluidCS.FindKernel("SolidToFluidClearBuffers");
				m_solidToFluidMeshKernel = m_solidToFluidCS.FindKernel("SolidToFluidMesh");
				m_solidToFluidSphereKernel = m_solidToFluidCS.FindKernel("SolidToFluidSphere");
				m_solidToFluidBoxKernel = m_solidToFluidCS.FindKernel("SolidToFluidBox");
				m_solidToFluidCapsuleKernel = m_solidToFluidCS.FindKernel("SolidToFluidCapsule");
				m_solidToFluidApplyAccumulatedDeltasKernel = m_solidToFluidCS.FindKernel("ApplyAccumulatedDeltas");
			}
		}

		protected virtual void DestroyMaterials()
		{
			Destroy(m_applyVelocityMaterial);
			Destroy(m_addFluidMaterial);
			Destroy(m_initSimulationMaterial);
			Destroy(m_createRenderDataMaterial);
			Destroy(m_boundaryMaterial);
			Destroy(m_copyTextureMaterial);
			Destroy(m_obstacleMaterial);
			Destroy(m_obstacleProceduralMaterial);
		}
	}
}