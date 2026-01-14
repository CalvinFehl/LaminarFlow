using UnityEngine;

namespace FluidFrenzy
{
	/// <summary>
	/// A collection of shader properties so all properties are precached and do not need to be processed at runtime.
	/// </summary>
	public class FluidShaderProperties
	{
		public static readonly int _MainTex = Shader.PropertyToID("_MainTex");
		public static readonly int _Obstacles = Shader.PropertyToID("_Obstacles");
		public static readonly int _TerrainHeightField = Shader.PropertyToID("_TerrainHeightField");
		public static readonly int _FluidHeightField = Shader.PropertyToID("_FluidHeightField");
		public static readonly int _PreviousFluidHeightField = Shader.PropertyToID("_PreviousFluidHeightField");
		public static readonly int _WorldNormal = Shader.PropertyToID("_WorldNormal");

		public static readonly int _OutflowField = Shader.PropertyToID("_OutflowField");
		public static readonly int _OutflowFieldLayer2 = Shader.PropertyToID("_OutflowFieldLayer2");
		public static readonly int _ExternalOutflowField = Shader.PropertyToID("_ExternalOutflowField");
		public static readonly int _ExternalOutflowFieldClamp = Shader.PropertyToID("_ExternalOutflowFieldClamp");

		public static readonly int _VelocityField = Shader.PropertyToID("_VelocityField");
		public static readonly int _VelocityField_ST = Shader.PropertyToID("_VelocityField_ST");
		public static readonly int _PreviousVelocityField = Shader.PropertyToID("_PreviousVelocityField");
		public static readonly int _PressureField = Shader.PropertyToID("_PressureField");
		public static readonly int _DivergenceField = Shader.PropertyToID("_DivergenceField");
		public static readonly int _HeightFieldRcp = Shader.PropertyToID("_HeightFieldRcp");
		public static readonly int _Simulation_TexelSize = Shader.PropertyToID("_Simulation_TexelSize");
		public static readonly int _VelocityPadding_ST = Shader.PropertyToID("_VelocityPadding_ST");


		public static readonly int _FluidBaseHeightOffset = Shader.PropertyToID("_FluidBaseHeightOffset");
		public static readonly int _FluidSimDeltaTime = Shader.PropertyToID("_FluidSimDeltaTime");
		public static readonly int _FluidSimStepDeltaTime = Shader.PropertyToID("_FluidSimStepDeltaTime");
		public static readonly int _FluidAcceleration = Shader.PropertyToID("_FluidAcceleration");
		public static readonly int _VelocityDeltaTime = Shader.PropertyToID("_VelocityDeltaTime");
		public static readonly int _VelocityDeltaTimeRcp = Shader.PropertyToID("_VelocityDeltaTimeRcp");
		public static readonly int _TexelWorldSize = Shader.PropertyToID("_TexelWorldSize");
		public static readonly int _OffsetUVScale = Shader.PropertyToID("_OffsetUVScale");
		public static readonly int _HeightScale = Shader.PropertyToID("_HeightScale");
		public static readonly int _WaterHeight = Shader.PropertyToID("_WaterHeight");
		public static readonly int _VelocityDir = Shader.PropertyToID("_VelocityDir");
		public static readonly int _ForceDir = Shader.PropertyToID("_ForceDir");

		public static readonly int _IncreaseStrength = Shader.PropertyToID("_IncreaseStrength");
		public static readonly int _IncreaseStrengthOuter = Shader.PropertyToID("_IncreaseStrengthOuter");
		public static readonly int _IncreaseExponent = Shader.PropertyToID("_IncreaseExponent");
		public static readonly int _Strength = Shader.PropertyToID("_Strength");
		public static readonly int _LayerMask = Shader.PropertyToID("_LayerMask");
		public static readonly int _BottomLayersMask = Shader.PropertyToID("_BottomLayersMask");
		public static readonly int _TotalHeightLayerMask = Shader.PropertyToID("_TotalHeightLayerMask");
		public static readonly int _TopLayerMask = Shader.PropertyToID("_TopLayerMask");
		public static readonly int _NumLayers = Shader.PropertyToID("_NumLayers");
		public static readonly int _RemapRange = Shader.PropertyToID("_RemapRange");

		public static readonly int _BlendOpFluidInteraction = Shader.PropertyToID("_BlendOpFluidInteraction");
		public static readonly int _ColorMaskFluidInteraction = Shader.PropertyToID("_ColorMaskFluidInteraction");

		public static readonly int _WorldSize = Shader.PropertyToID("_WorldSize");
		public static readonly int _WorldCellSize = Shader.PropertyToID("_WorldCellSize");
		public static readonly int _WorldCellSizeRcp = Shader.PropertyToID("_WorldCellSizeRcp");
		public static readonly int _SimulationPositionWS = Shader.PropertyToID("_SimulationPositionWS");

		public static readonly int _CellSize = Shader.PropertyToID("_CellSize");
		public static readonly int _CellSizeScale = Shader.PropertyToID("_CellSizeScale");
		public static readonly int _CellSizeRcp = Shader.PropertyToID("_CellSizeRcp");
		public static readonly int _CellSizeSq = Shader.PropertyToID("_CellSizeSq");
		public static readonly int _CellSizeRcpSq = Shader.PropertyToID("_CellSizeRcpSq");
		public static readonly int _Damping = Shader.PropertyToID("_Damping");
		public static readonly int _AccelCellSizeDeltaTime = Shader.PropertyToID("_AccelCellSizeDeltaTime");
		public static readonly int _RcpCellSizeSqDeltaTime = Shader.PropertyToID("_RcpCellSizeSqDeltaTime");

		public static readonly int _FluidViscosity = Shader.PropertyToID("_FluidViscosity");
		public static readonly int _FluidFlowHeight = Shader.PropertyToID("_FluidFlowHeight");

		public static readonly int _AdvectScale = Shader.PropertyToID("_AdvectScale");
		public static readonly int _VelocityScale = Shader.PropertyToID("_VelocityScale");
		public static readonly int _VelocityMax = Shader.PropertyToID("_VelocityMax");
		public static readonly int _AccelerationMax = Shader.PropertyToID("_AccelerationMax");
		public static readonly int _VelocityDamping = Shader.PropertyToID("_VelocityDamping");
		public static readonly int _OvershootingEdge = Shader.PropertyToID("_OvershootingEdge");
		public static readonly int _OvershootingScale = Shader.PropertyToID("_OvershootingScale");
		public static readonly int _FluidDeltaMax = Shader.PropertyToID("_FluidDeltaMax");
		public static readonly int _Epsilon = Shader.PropertyToID("_Epsilon");
		public static readonly int _Pressure = Shader.PropertyToID("_Pressure");
		public static readonly int _VelocityFieldBoundary = Shader.PropertyToID("_VelocityFieldBoundary");
		public static readonly int _FluxClampMinMax = Shader.PropertyToID("_FluxClampMinMax");
		public static readonly int _BoundaryCells = Shader.PropertyToID("_BoundaryCells");

		public static readonly int _FoamValues = Shader.PropertyToID("_FoamValues");
		public static readonly int _FoamFadeValues = Shader.PropertyToID("_FoamFadeValues");
		public static readonly int _FoamWaveSmoothStep = Shader.PropertyToID("_FoamWaveSmoothStep");
		public static readonly int _FoamPressureSmoothStep = Shader.PropertyToID("_FoamPressureSmoothStep");
		public static readonly int _FoamShallowVelocitySmoothStep = Shader.PropertyToID("_FoamShallowVelocitySmoothStep");
		public static readonly int _FoamShallowDepth = Shader.PropertyToID("_FoamShallowDepth");
		public static readonly int _FoamTurbulenceAmount = Shader.PropertyToID("_FoamTurbulenceAmount");
		public static readonly int _FoamDivergenceThreshold = Shader.PropertyToID("_FoamDivergenceThreshold");

		public static readonly int _BlitTexture = Shader.PropertyToID("_BlitTexture");
		public static readonly int _BlitScaleBias = Shader.PropertyToID("_BlitScaleBias");
		public static readonly int _BlitScaleBiasRt = Shader.PropertyToID("_BlitScaleBiasRt");
		public static readonly int _BlitTextureSize = Shader.PropertyToID("_BlitTextureSize");
		public static readonly int _BlitRotation = Shader.PropertyToID("_BlitRotation");

		public static readonly int _NormalBlitScaleBias = Shader.PropertyToID("_NormalBlitScaleBias");
		public static readonly int _VelocityBlitScaleBias = Shader.PropertyToID("_VelocityBlitScaleBias");

		public static readonly int _VelocityDst = Shader.PropertyToID("_VelocityDst");
		public static readonly int _VelocityReadUp = Shader.PropertyToID("_VelocityReadUp");
		public static readonly int _VelocityReadDown = Shader.PropertyToID("_VelocityReadDown");
		public static readonly int _VelocityReadRight = Shader.PropertyToID("_VelocityReadRight");
		public static readonly int _VelocityReadLeft = Shader.PropertyToID("_VelocityReadLeft");

		public static readonly int _VelocityWriteUp = Shader.PropertyToID("_VelocityWriteUp");
		public static readonly int _VelocityWriteDown = Shader.PropertyToID("_VelocityWriteDown");
		public static readonly int _VelocityWriteRight = Shader.PropertyToID("_VelocityWriteRight");
		public static readonly int _VelocityWriteLeft = Shader.PropertyToID("_VelocityWriteLeft");

		public static readonly int _PressureDst = Shader.PropertyToID("_PressureDst");
		public static readonly int _PressureReadUp = Shader.PropertyToID("_PressureReadUp");
		public static readonly int _PressureReadDown = Shader.PropertyToID("_PressureReadDown");
		public static readonly int _PressureReadRight = Shader.PropertyToID("_PressureReadRight");
		public static readonly int _PressureReadLeft = Shader.PropertyToID("_PressureReadLeft");

		public static readonly int _PressureWriteUp = Shader.PropertyToID("_PressureWriteUp");
		public static readonly int _PressureWriteDown = Shader.PropertyToID("_PressureWriteDown");
		public static readonly int _PressureWriteRight = Shader.PropertyToID("_PressureWriteRight");
		public static readonly int _PressureWriteLeft = Shader.PropertyToID("_PressureWriteLeft");

		public static readonly int _RotateBlit = Shader.PropertyToID("_RotateBlit");
		public static readonly int _RotateSample = Shader.PropertyToID("_RotateSample");
		public static readonly int _SrcUp = Shader.PropertyToID("_SrcUp");
		public static readonly int _SrcDown = Shader.PropertyToID("_SrcDown");
		public static readonly int _SrcRight = Shader.PropertyToID("_SrcRight");
		public static readonly int _SrcLeft = Shader.PropertyToID("_SrcLeft");

		public static readonly int _DstUp = Shader.PropertyToID("_DstUp");
		public static readonly int _DstDown = Shader.PropertyToID("_DstDown");
		public static readonly int _DstRight = Shader.PropertyToID("_DstRight");
		public static readonly int _DstLeft = Shader.PropertyToID("_DstLeft");
		public static readonly int _ResetDst = Shader.PropertyToID("_ResetDst");

		public static readonly int _HeightDst1 = Shader.PropertyToID("_HeightDst1");
		public static readonly int _HeightDst2 = Shader.PropertyToID("_HeightDst2");

		public static readonly int _ReadIndex = Shader.PropertyToID("_ReadIndex");
		public static readonly int _ReadScale = Shader.PropertyToID("_ReadScale");
		public static readonly int _WriteIndex = Shader.PropertyToID("_WriteIndex");
		public static readonly int _BoundarySize = Shader.PropertyToID("_BoundarySize");

		public static readonly int _PressureStrength = Shader.PropertyToID("_PressureStrength");
		public static readonly int _PreviousPressureField = Shader.PropertyToID("_PreviousPressureField");
		public static readonly int _PressureMinMax = Shader.PropertyToID("_PressureMinMax");

		public static readonly int _UVTextureSize = Shader.PropertyToID("_UVTextureSize");
		public static readonly int _Offset = Shader.PropertyToID("_Offset");

		public static readonly int _Transform = Shader.PropertyToID("_Transform");
		public static readonly int _Center = Shader.PropertyToID("_Center");
		public static readonly int _Size = Shader.PropertyToID("_Size");
		public static readonly int _Dimensions = Shader.PropertyToID("_Dimensions");
		public static readonly int _TexelSize = Shader.PropertyToID("_TexelSize");
		public static readonly int _Padding_ST = Shader.PropertyToID("_Padding_ST");
		public static readonly int _ObstaclePadding_ST = Shader.PropertyToID("_ObstaclePadding_ST");

		public static readonly int _TransformScale = Shader.PropertyToID("_TransformScale");
		public static readonly int _SampleCountX = Shader.PropertyToID("_SampleCountX");
		public static readonly int _SampleCountY = Shader.PropertyToID("_SampleCountY");
		public static readonly int _CaptureHeight = Shader.PropertyToID("_CaptureHeight");

		public static readonly int _StepSize = Shader.PropertyToID("_StepSize");
		public static readonly int _AspectRatio = Shader.PropertyToID("_AspectRatio");

		//Erosion
		public static readonly int _MaxHeightDif = Shader.PropertyToID("_MaxHeightDif");
		public static readonly int _SlopeSmoothness = Shader.PropertyToID("_SlopeSmoothness");
		public static readonly int _MaxHeightField = Shader.PropertyToID("_MaxHeightField");
		public static readonly int _ErosionOutflowRate = Shader.PropertyToID("_ErosionOutflowRate");
		public static readonly int _SedimentField = Shader.PropertyToID("_SedimentField");
		public static readonly int _InterField1 = Shader.PropertyToID("_InterField1");
		public static readonly int _InterField2 = Shader.PropertyToID("_InterField2");
		public static readonly int _SedimentMax = Shader.PropertyToID("_SedimentMax");
		public static readonly int _DissolveRate = Shader.PropertyToID("_DissolveRate");
		public static readonly int _DepositRate = Shader.PropertyToID("_DepositRate");
		public static readonly int _MinTiltAngle = Shader.PropertyToID("_MinTiltAngle");
		public static readonly int _MixRate = Shader.PropertyToID("_MixRate");
		public static readonly int _MixScale = Shader.PropertyToID("_MixScale");
		public static readonly int _LayerModify = Shader.PropertyToID("_LayerModify");
		public static readonly int _MixDepositRate = Shader.PropertyToID("_MixDepositRate");
		public static readonly int _TerrainDepositMask = Shader.PropertyToID("_TerrainDepositMask");
		public static readonly int _TerrainDepositSplatMask = Shader.PropertyToID("_TerrainDepositSplatMask");

		public static readonly int _Evaporation = Shader.PropertyToID("_Evaporation");

		public static readonly int _LiquifyLayerMask = Shader.PropertyToID("_LiquifyLayerMask");
		public static readonly int _ReactionFactors_F1 = Shader.PropertyToID("_ReactionFactors_F1");
		public static readonly int _ReactionFactors_F2 = Shader.PropertyToID("_ReactionFactors_F2");
		public static readonly int _AddTerrainMask_F1 = Shader.PropertyToID("_AddTerrainMask_F1");
		public static readonly int _AddTerrainMask_F2 = Shader.PropertyToID("_AddTerrainMask_F2");
		public static readonly int _AddFluidMask_F1 = Shader.PropertyToID("_AddFluidMask_F1");
		public static readonly int _AddFluidMask_F2 = Shader.PropertyToID("_AddFluidMask_F2");
		public static readonly int _SetSplatMask_F1 = Shader.PropertyToID("_SetSplatMask_F1");
		public static readonly int _SetSplatMask_F2 = Shader.PropertyToID("_SetSplatMask_F2");

		// Modifier
		public static readonly int _LiquifyFluidLayerMask = Shader.PropertyToID("_LiquifyFluidLayerMask");
		public static readonly int _LiquifyTerrainLayerMask = Shader.PropertyToID("_LiquifyTerrainLayerMask");
		public static readonly int _SolidifyFluidLayerMask = Shader.PropertyToID("_SolidifyFluidLayerMask");
		public static readonly int _SolidifyTerrainLayerMask = Shader.PropertyToID("_SolidifyTerrainLayerMask");
		public static readonly int _ModifierCenter = Shader.PropertyToID("_ModifierCenter");
		public static readonly int _ModifierSize = Shader.PropertyToID("_ModifierSize"); 
		public static readonly int _ModifierRotationMatrix = Shader.PropertyToID("_ModifierRotationMatrix");
		public static readonly int _LiquifyTotalHeightMask = Shader.PropertyToID("_LiquifyTotalHeightMask");
		public static readonly int _SolidifyTotalHeightMask = Shader.PropertyToID("_SolidifyTotalHeightMask");

		//FluidSurface Renderer
		public static readonly int _HeightField = Shader.PropertyToID("_HeightField");
		public static readonly int _HeightmapRcpScale = Shader.PropertyToID("_HeightmapRcpScale");
		public static readonly int _UnityInstancing_InstanceProperties = Shader.PropertyToID("UnityInstancing_InstanceProperties");
		public static readonly int _MeshUVOffsetScale = Shader.PropertyToID("_MeshUVOffsetScale");
		public static readonly int _TextureUVOffsetScale = Shader.PropertyToID("_TextureUVOffsetScale");
		public static readonly int _FluidHeightVelocityField = Shader.PropertyToID("_FluidHeightVelocityField");
		public static readonly int _FluidNormalField = Shader.PropertyToID("_FluidNormalField");
		public static readonly int _FluidClipHeight = Shader.PropertyToID("_FluidClipHeight");
		public static readonly int _TerrainHeightScale = Shader.PropertyToID("_TerrainHeightScale");
		public static readonly int _Layer = Shader.PropertyToID("_Layer");

		public static readonly int _FluidUVOffsetField = Shader.PropertyToID("_FluidUVOffsetField");
		public static readonly int _FlowBlend = Shader.PropertyToID("_FlowBlend");
		public static readonly int _FlowTimer = Shader.PropertyToID("_FlowTimer");
		public static readonly int _FlowUVOffset = Shader.PropertyToID("_FlowUVOffset");

		public static readonly int _FluidGridMeshDimensions = Shader.PropertyToID("_FluidGridMeshDimensions");
		public static readonly int _FluidGridMeshResolution = Shader.PropertyToID("_FluidGridMeshResolution");
		public static readonly int _FluidGridMeshRcp = Shader.PropertyToID("_FluidGridMeshRcp");
		public static readonly int _TerrainHeightField_ST = Shader.PropertyToID("_TerrainHeightField_ST");

		public static readonly int _FlowSpeed = Shader.PropertyToID("_FlowSpeed");
		public static readonly int _FluidScreenSpaceParticles = Shader.PropertyToID("_FluidScreenSpaceParticles");

		//WaterSurface Renderer
		public static readonly int _FluidFoamField = Shader.PropertyToID("_FluidFoamField");
		public static readonly int _FluidFoamField_ST = Shader.PropertyToID("_FluidFoamField_ST");
		public static readonly int _WaterColor = Shader.PropertyToID("_WaterColor");
		public static readonly int _ScatterColor = Shader.PropertyToID("_ScatterColor");

		//LavaSurface Renderer
		public static readonly int _HeatLUT = Shader.PropertyToID("_HeatLUT");

		// Shadows
		public static readonly int _TransparentShadowMapTexture = Shader.PropertyToID("_TransparentShadowMapTexture");

		// Terrain
		public static readonly int _FallbackBlendOp = Shader.PropertyToID("_FallbackBlendOp");
		public static readonly int _BlendOpTerrain = Shader.PropertyToID("_BlendOpTerrain");
		public static readonly int _ColorMaskTerrain = Shader.PropertyToID("_ColorMaskTerrain");

		public static readonly int _Splatmap = Shader.PropertyToID("_Splatmap");
		public static readonly int _SplatmapMask = Shader.PropertyToID("_SplatmapMask");

		// GPU Particles
		public static readonly int _ParticleCount = Shader.PropertyToID("_ParticleCount");
		public static readonly int _FreeIndices = Shader.PropertyToID("_FreeIndices");
		public static readonly int _DrawIndices = Shader.PropertyToID("_DrawIndices");
		public static readonly int _DrawArgs = Shader.PropertyToID("_DrawArgs");
		public static readonly int _DispatchArgs = Shader.PropertyToID("_DispatchArgs");
		public static readonly int _SimulationData = Shader.PropertyToID("_SimulationData");
		public static readonly int _ParticleEmissionRate = Shader.PropertyToID("_ParticleEmissionRate");
		public static readonly int _ParticleBuffer = Shader.PropertyToID("_ParticleBuffer");

		public static readonly int _DivergenceStagger = Shader.PropertyToID("_DivergenceStagger");
		public static readonly int _DivergenceGridLimit = Shader.PropertyToID("_DivergenceGridLimit");
		public static readonly int _SplashDivergenceStagger = Shader.PropertyToID("_SplashDivergenceStagger");
		public static readonly int _SplashDivergenceGridLimit = Shader.PropertyToID("_SplashDivergenceGridLimit");
		public static readonly int _BreakingWavesGridLimit = Shader.PropertyToID("_BreakingWavesGridLimit");
		public static readonly int _BreakingWavesStagger = Shader.PropertyToID("_BreakingWavesStagger");

		public static readonly int _HeightAvgMax = Shader.PropertyToID("_HeightAvgMax");
		public static readonly int _SteepnessThreshold = Shader.PropertyToID("_SteepnessThreshold");
		public static readonly int _RiseRateThreshold = Shader.PropertyToID("_RiseRateThreshold");
		public static readonly int _WaveLengthThreshold = Shader.PropertyToID("_WaveLengthThreshold");
		public static readonly int _SprayDivergenceThreshold = Shader.PropertyToID("_SprayDivergenceThreshold");
		public static readonly int _SplashOffsetRange = Shader.PropertyToID("_SplashOffsetRange");
		public static readonly int _SurfaceOffsetRange = Shader.PropertyToID("_SurfaceOffsetRange");
		public static readonly int _SurfaceDivergenceThreshold = Shader.PropertyToID("_SurfaceDivergenceThreshold");
		public static readonly int _ParticleSystemObjectToWorld = Shader.PropertyToID("_ParticleSystemObjectToWorld");

		//GPU LOD
		public static readonly int _HeightMapMask = Shader.PropertyToID("_HeightMapMask");
		public static readonly int _ObjectToWorld = Shader.PropertyToID("_ObjectToWorld");
		public static readonly int _WorldSpaceCameraPos = Shader.PropertyToID("_WorldSpaceCameraPos");
		public static readonly int _SurfaceLocalBounds = Shader.PropertyToID("_SurfaceLocalBounds");
		public static readonly int _SurfaceHeightScale = Shader.PropertyToID("_SurfaceHeightScale");
		public static readonly int _HeightMap = Shader.PropertyToID("_HeightMap");
		public static readonly int _SrcDispatchArgs = Shader.PropertyToID("_SrcDispatchArgs");
		public static readonly int _DstDispatchArgs = Shader.PropertyToID("_DstDispatchArgs");
		public static readonly int _SrcQuadTree = Shader.PropertyToID("_SrcQuadTree");
		public static readonly int _DstQuadTree = Shader.PropertyToID("_DstQuadTree");
		public static readonly int _RenderBuffer = Shader.PropertyToID("_RenderBuffer");
		public static readonly int _CullingPlanes = Shader.PropertyToID("_CullingPlanes");
		public static readonly int _LODMeshDim = Shader.PropertyToID("_LODMeshDim");
		public static readonly int _QuadTreeNodes = Shader.PropertyToID("_QuadTreeNodes");
		public static readonly int _LODMinMax = Shader.PropertyToID("_LODMinMax");
		public static readonly int _LocalSpaceCameraPos = Shader.PropertyToID("_LocalSpaceCameraPos");
		public static readonly int _WorldCameraPos = Shader.PropertyToID("_WorldCameraPos");

		public static readonly int _LocalToWorld = Shader.PropertyToID("_LocalToWorld");
		public static readonly int _PrevLocalToWorld = Shader.PropertyToID("_PrevLocalToWorld");
		public static readonly int _PointCount = Shader.PropertyToID("_PointCount");
		public static readonly int _PrimitiveArea = Shader.PropertyToID("_PrimitiveArea");


		/// Solid To Fluid
		// Fluid State Textures
		public static readonly int _HeightTarget = Shader.PropertyToID("_HeightTarget");
		public static readonly int _VelocityTarget = Shader.PropertyToID("_VelocityTarget");
		public static readonly int _HeightAccumulator = Shader.PropertyToID("_HeightAccumulatorBuffer");
		public static readonly int _VelocityAccumulator = Shader.PropertyToID("_VelocityAccumulatorBuffer");
		public static readonly int _BufferWidth = Shader.PropertyToID("_BufferWidth");

		// Influence Profile Properties
		public static readonly int _HeightInfluence = Shader.PropertyToID("_HeightInfluence");
		public static readonly int _VelocityInfluence = Shader.PropertyToID("_VelocityInfluence");
		public static readonly int _SolidToFluidScale = Shader.PropertyToID("_SolidToFluidScale");

		// Mesh Specific Properties
		public static readonly int _Vertices = Shader.PropertyToID("_Vertices");
		public static readonly int _NumTriangles = Shader.PropertyToID("_NumTriangles");

		// Sphere Specific Properties
		public static readonly int _SphereRadius = Shader.PropertyToID("_SphereRadius");

		// Box Specific Properties
		public static readonly int _BoxSize = Shader.PropertyToID("_BoxSize");
		public static readonly int _BoxArea = Shader.PropertyToID("_BoxArea");
		public static readonly int _BoxSamplesPerAxis = Shader.PropertyToID("_BoxSamplesPerAxis");

		// Capsule Specific Properties
		public static readonly int _CapsuleRadius = Shader.PropertyToID("_CapsuleRadius");
		public static readonly int _CapsuleHeight = Shader.PropertyToID("_CapsuleHeight");
		public static readonly int _CapsuleDirection = Shader.PropertyToID("_CapsuleDirection");
	}

	enum WaterSimProfileID
	{
		UserInput,
		WaterSimulation_Fluid,
		WaterSimulation_Velocity,
		WaterSimulation_RenderInfo,
		WaterSimulationDynamic,

		SlipFreeBoundary,
		Outflow,
		AddWater,
		RemoveAddedFlow,
		UpdateWaterHeight,
		Foam,
		Velocity,
		Advect,
		FlowToVelocity,
		Pressure,
		Jacobi,
		Divergence,
		Boundary,
		ResetUV,
		AdvectUV,
		CombineRenderInfo,
		CombineHeightVelocity,
		CreateSDF,
		CopyTerrain,
		NormalMap,

		InitTerrainheight,
		DrawObstacles,
		AsyncReadbackHeightVelocity,
		AsyncReadbackVelocityResult,

		AsyncReadbackDistanceField,
		AsyncReadbackDistanceFieldResult,

		ExternalFoam,
		ExternalForce,
		ExternalForceTexture,
		ExternalVelocity,
		ExternalWater,
		ExternalWhirlpoolForce,
		ExternalWhirlpoolVelocity,

		Erosion,
		Terraforming,
		Terraforming_Particles,
	}
}