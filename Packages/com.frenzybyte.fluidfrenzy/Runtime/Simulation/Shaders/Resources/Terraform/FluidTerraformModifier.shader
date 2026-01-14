Shader "Hidden/FluidFrenzy/TerraformModifier"
{
    Properties
    {
		_MainTex("Texture", 2D) = "black" {}
		_FluidHeightField("Texture", 2D) = "white" {}
    }
    SubShader
    {
		HLSLINCLUDE

		#define USETEXTURE2D

		#include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Simulation/Shaders/Resources/FluidSimulationCommon.hlsl"
		#include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Simulation/Shaders/Resources/FluidInputCommon.hlsl"

		
		struct FragOut_TerraformModifier
		{
			float4 fluid : SV_Target0;
			float4 terrain : SV_Target1;
			float4 splatmap : SV_Target2;
		};

		Texture2D _Splatmap;

		float4 _LiquifyTerrainLayerMask;
		float4 _LiquifyFluidLayerMask;
		float4 _LiquifyTotalHeightMask; // so we dont check distance against the highest point

		float4 _SolidifyTerrainLayerMask;
		float4 _SolidifyFluidLayerMask;
		float4 _SolidifyTotalHeightMask; // so we dont check distance against the highest point

		#define SHAPE_SPHERE    2
		#define SHAPE_CUBE      3
		#define SHAPE_CYLINDER  4
		#define SHAPE_CAPSULE   5

		// Add this helper to your HLSLINCLUDE
		float Calculate3DDistance(float heightY, float2 screenUV, int shapeType)
		{
			float2 worldXZ = WorldPosFromPaddedUV(screenUV);
			float3 worldPos = float3(worldXZ.x, heightY, worldXZ.y); 

			float3 localPos = worldPos - _ModifierCenter.xyz;
			float3 rotatedLocalPos = mul(_ModifierRotationMatrix, float4(localPos, 1.0)).xyz;

			float dist = 0;
    
			// The compiler will compile-out the branches based on the hardcoded shapeType value
			if (shapeType == SHAPE_SPHERE)
			{
				// SPHERE: sizeA is radius.
				float radius = _ModifierSize.x * 0.5;
				dist = length(rotatedLocalPos) / radius;
			}
			else if (shapeType == SHAPE_CUBE)
			{
				// CUBE
				float3 normalizedLocalPos = abs(rotatedLocalPos / (_ModifierSize.xyz * 0.5));
				dist = max(normalizedLocalPos.x, max(normalizedLocalPos.y, normalizedLocalPos.z));
			}
			else if (shapeType == SHAPE_CYLINDER)
			{
				// CYLINDER
				float radius = _ModifierSize.x * 0.5;
				float halfHeight = _ModifierSize.y * 0.5;
        
				float dist_XZ = length(rotatedLocalPos.xz) / radius;
				float dist_Y = abs(rotatedLocalPos.y) / halfHeight; 
				dist = max(dist_XZ, dist_Y);
			}
			else if (shapeType == SHAPE_CAPSULE)
			{
				// CAPSULE
				float radius = _ModifierSize.x * 0.5;
				float segmentHalfLength = _ModifierSize.y * 0.5;
        
				float h = clamp(rotatedLocalPos.y / segmentHalfLength, -1.0, 1.0);
				float distToSegment = length(rotatedLocalPos - float3(0, h * segmentHalfLength, 0));
				dist = distToSegment / radius; 
			}

			return dist;
		}

		
		FragOut_TerraformModifier ApplyTerraformLogic(float liquifyDist, float solidifyDist, float4 screenPos)
		{
			FragOut_TerraformModifier Out = (FragOut_TerraformModifier)(0);

			float2 screenUV = screenPos.xy / _TerrainHeightField_TexelSize.zw;
			float2 fluid = SampleFluidHeight(screenUV);
			float4 terrain = SampleBase(screenUV);
			float4 splatmap = _Splatmap.Sample(sampler_FluidHeightField, screenUV);

			// Calculate strength for LIQUIFY using liquifyDist
			float liquifyAmount = CalculateStrength(liquifyDist, screenPos.xy, _Simulation_TexelSize).x;

			// Liquify Logic (uses liquifyAmount)
			float terrainToLiquify = dot(terrain, _LiquifyTerrainLayerMask);
			float2 liquifyMask = _LiquifyFluidLayerMask.xy;
			float liquifyRate = _LiquifyFluidLayerMask.z * liquifyAmount;
			float liquifyFinalAmount = _LiquifyFluidLayerMask.w;
			liquifyRate = min(terrainToLiquify, liquifyRate);
			terrain -= liquifyRate * _LiquifyTerrainLayerMask;
			fluid.xy += liquifyRate * liquifyMask * liquifyFinalAmount;


			// Calculate strength for SOLIDIFY using solidifyDist
			float solidifyAmount = CalculateStrength(solidifyDist, screenPos.xy, _Simulation_TexelSize).x;

			// Solidify logic (uses solidifyAmount)
			float fluidToSolidify = dot(fluid.xy, _SolidifyFluidLayerMask.xy);
			float2 solidifyMask = _SolidifyFluidLayerMask.xy;
			float solidifyRate = _SolidifyFluidLayerMask.z;
			float solidifyFinalAmount = _SolidifyFluidLayerMask.w;
			solidifyRate = min(fluidToSolidify, solidifyRate) * solidifyAmount; // Apply the solidify amount
			terrain += solidifyRate * _SolidifyTerrainLayerMask * solidifyFinalAmount;
			splatmap += solidifyRate * _SplatmapMask * solidifyFinalAmount;
			splatmap -= solidifyRate * (1-_SplatmapMask) * solidifyFinalAmount;
			fluid.xy -= solidifyRate * solidifyMask;

			Out.fluid = max(float4(fluid,0,0), 0);
			Out.terrain = max(terrain,0);
			Out.splatmap = saturate(splatmap);

			return Out;
		}


		ENDHLSL
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

		// 0
        Pass
        {	
			Name "TerraformCircle"
			ColorMask RGBA
			HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
			#pragma multi_compile ___ OPEN_BORDER

			FragOut_TerraformModifier frag(v2f i, float4 screenPos : SV_POSITION)
			{
				float dist = DistanceCircle(i.uv.xy);

				return ApplyTerraformLogic(dist, dist, screenPos);
			}

			ENDHLSL
        }
		
		// 1
        Pass
        {	
			Name "TerraformBox"
			ColorMask RGBA
			HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
			#pragma multi_compile ___ OPEN_BORDER

			FragOut_TerraformModifier frag(v2f i, float4 screenPos : SV_POSITION)
			{
				float dist = DistanceBox(i.uv.xy);

				return ApplyTerraformLogic(dist, dist, screenPos);
			}

			ENDHLSL
        }

		// 2
		Pass
		{	
			Name "TerraformSphere"
			ColorMask RGBA
			HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
			#pragma multi_compile ___ OPEN_BORDER

			FragOut_TerraformModifier frag(v2f i, float4 screenPos : SV_POSITION)
			{
				float2 screenUV = screenPos.xy / _TerrainHeightField_TexelSize.zw;
				float4 terrainSample = SampleBase(screenUV);
				float2 fluidSample = SampleFluidHeight(screenUV);
		
				// 1. LIQUIFY DISTANCE (uses TERRAIN height)
				float liquifyHeightY = dot(terrainSample, _LiquifyTotalHeightMask); 
				float liquifyDist = Calculate3DDistance(liquifyHeightY, screenUV, SHAPE_SPHERE); // Hardcoded ID
		
				// 2. SOLIDIFY DISTANCE (uses FLUID height)
				float solidifyHeightY = dot(terrainSample, (1.0f).xxxx) + dot(fluidSample, _SolidifyTotalHeightMask.xy); // Masking fluid's RG/XY channels
				float solidifyDist = Calculate3DDistance(solidifyHeightY, screenUV, SHAPE_SPHERE); // Hardcoded ID

				return ApplyTerraformLogic(liquifyDist, solidifyDist, screenPos);
			}
			ENDHLSL
		}

		// 3
		Pass
		{	
			Name "TerraformCube"
			ColorMask RGBA
			HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
			#pragma multi_compile ___ OPEN_BORDER

			FragOut_TerraformModifier frag(v2f i, float4 screenPos : SV_POSITION)
			{
				float2 screenUV = screenPos.xy / _TerrainHeightField_TexelSize.zw;
				float4 terrainSample = SampleBase(screenUV);
				float2 fluidSample = SampleFluidHeight(screenUV);
		
				// 1. LIQUIFY DISTANCE
				float liquifyHeightY = dot(terrainSample, _LiquifyTotalHeightMask); 
				float liquifyDist = Calculate3DDistance(liquifyHeightY, screenUV, SHAPE_CUBE); // Hardcoded ID
		
				// 2. SOLIDIFY DISTANCE
				float solidifyHeightY = dot(terrainSample, (1.0f).xxxx) + dot(fluidSample, _SolidifyTotalHeightMask.xy);
				float solidifyDist = Calculate3DDistance(solidifyHeightY, screenUV, SHAPE_CUBE); // Hardcoded ID

				return ApplyTerraformLogic(liquifyDist, solidifyDist, screenPos);
			}
			ENDHLSL
		}

		// 4
		Pass
		{	
			Name "TerraformCylinder"
			ColorMask RGBA
			HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
			#pragma multi_compile ___ OPEN_BORDER

			FragOut_TerraformModifier frag(v2f i, float4 screenPos : SV_POSITION)
			{
				float2 screenUV = screenPos.xy / _TerrainHeightField_TexelSize.zw;
				float4 terrainSample = SampleBase(screenUV);
				float2 fluidSample = SampleFluidHeight(screenUV);
		
				// 1. LIQUIFY DISTANCE
				float liquifyHeightY = dot(terrainSample, _LiquifyTotalHeightMask); 
				float liquifyDist = Calculate3DDistance(liquifyHeightY, screenUV, SHAPE_CYLINDER); // Hardcoded ID
		
				// 2. SOLIDIFY DISTANCE
				float solidifyHeightY = dot(terrainSample, (1.0f).xxxx) + dot(fluidSample, _SolidifyTotalHeightMask.xy);
				float solidifyDist = Calculate3DDistance(solidifyHeightY, screenUV, SHAPE_CYLINDER); // Hardcoded ID

				return ApplyTerraformLogic(liquifyDist, solidifyDist, screenPos);
			}
			ENDHLSL
		}

		// 5
		Pass
		{	
			Name "TerraformCapsule"
			ColorMask RGBA
			HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
			#pragma multi_compile ___ OPEN_BORDER

			FragOut_TerraformModifier frag(v2f i, float4 screenPos : SV_POSITION)
			{
				float2 screenUV = screenPos.xy / _TerrainHeightField_TexelSize.zw;
				float4 terrainSample = SampleBase(screenUV);
				float2 fluidSample = SampleFluidHeight(screenUV);
		
				// 1. LIQUIFY DISTANCE
				float liquifyHeightY = dot(terrainSample, _LiquifyTotalHeightMask); 
				float liquifyDist = Calculate3DDistance(liquifyHeightY, screenUV, SHAPE_CAPSULE); // Hardcoded ID
		
				// 2. SOLIDIFY DISTANCE
				float solidifyHeightY = dot(terrainSample, (1.0f).xxxx) + dot(fluidSample, _SolidifyTotalHeightMask.xy);
				float solidifyDist = Calculate3DDistance(solidifyHeightY, screenUV, SHAPE_CAPSULE); // Hardcoded ID

				return ApplyTerraformLogic(liquifyDist, solidifyDist, screenPos);
			}
			ENDHLSL
		}

    }
}