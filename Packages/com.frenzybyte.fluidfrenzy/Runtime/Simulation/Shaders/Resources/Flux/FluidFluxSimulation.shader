Shader "Hidden/FluidFrenzy/Simulation/Flux/FluxSimulation"
{
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

		HLSLINCLUDE
		#define USETEXTURE2D
		#include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Simulation/Shaders/Resources/FluidSimulationCommon.hlsl"

		struct v2f
		{
			float2 uv : TEXCOORD0;
			float4 vertex : SV_POSITION;
		};

		v2f vert(uint vid : SV_VertexID)
		{
			v2f o;
			o.vertex = GetQuadVertexPosition(vid) * float4(_BlitScaleBiasRt.x, _BlitScaleBiasRt.y, 1, 1) + float4(_BlitScaleBiasRt.z, _BlitScaleBiasRt.w, 0, 0);
			o.vertex.xy = o.vertex.xy * float2(2.0f, -2.0f) + float2(-1.0f, 1.0f); //convert to -1..1
			o.uv = GetQuadTexCoord(vid) * _BlitScaleBias.xy + _BlitScaleBias.zw;
			return o;
		}

		ENDHLSL

		Pass
		{
			Name "Flux"
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_local _ FLUID_MULTILAYER
			#pragma multi_compile_local _ _FLUID_UNITY_TERRAIN

			struct fragout
			{
				float4 color0 : SV_Target0;
#if FLUID_MULTILAYER
				float4 color1 : SV_Target1;
#endif
			};

			fragout frag(v2f i)
			{
				float4 screenPos = i.vertex;
				fragout o = (fragout)(0);
				screenPos.xy -= 0.5f;
				float terrainHeight = LoadTerrainHeight(screenPos.xy).x;
				float4 fluidHeight = _FluidHeightField.Load(float3(screenPos.xy, 0));
				float addedHeight = _ExternalOutflowField.Load(float3(screenPos.xy, 0)).x;

				float4 terrainTexelLeft, terrainTexelRight, terrainTexelTop, terrainTexelBottom;
				float4 fluidTexelLeft, fluidTexelRight, fluidTexelTop, fluidTexelBottom;
				float4 addedHeightLeft, addedHeightRight, addedHeightTop, addedHeightBottom;

				GetNeighbourDataTerrainClamp(_FluxClampMinMax.xy, _FluxClampMinMax.zw, screenPos.xy, terrainTexelLeft, terrainTexelRight, terrainTexelTop, terrainTexelBottom);
				GetNeighbourDataClamp(_FluidHeightField, _FluidHeightField_TexelSize, _FluxClampMinMax.xy, _FluxClampMinMax.zw, screenPos.xy, fluidTexelLeft, fluidTexelRight, fluidTexelTop, fluidTexelBottom);
				GetNeighbourDataClamp(_ExternalOutflowField, _FluidHeightField_TexelSize, _ExternalOutflowFieldClamp.xy, _ExternalOutflowFieldClamp.zw, screenPos.xy, addedHeightLeft, addedHeightRight, addedHeightTop, addedHeightBottom);

				{
					float4 oldOutflow = DAMPING_LAYER1 * _OutflowField.Load(float3(screenPos.xy, 0));
					float totalHeight = (terrainHeight + fluidHeight.x) + addedHeight;
					float4 heightDifference;
					heightDifference.x = totalHeight - ((fluidTexelLeft.x + terrainTexelLeft.x) + addedHeightLeft.x);
					heightDifference.y = totalHeight - ((fluidTexelRight.x + terrainTexelRight.x) + addedHeightRight.x);
					heightDifference.z = totalHeight - ((fluidTexelTop.x + terrainTexelTop.x) + addedHeightTop.x);
					heightDifference.w = totalHeight - ((fluidTexelBottom.x + terrainTexelBottom.x) + addedHeightBottom.x);

					float4 newFlow = max(float4(0,0,0,0), oldOutflow + ACCEL_DT_CELLSIZE_LAYER1 * heightDifference);
					float totalOutFlow = ((newFlow.x + newFlow.y + newFlow.z + newFlow.w) * _FluidSimDeltaTime);
					float K = min(1.0f,(fluidHeight.x * CELLSIZESQ_LAYER1) / totalOutFlow);
					K = max(0, K);
					newFlow *= K;

					o.color0 = newFlow;
				}

				#if FLUID_MULTILAYER
				{
					float4 oldOutflow = DAMPING_LAYER2 * _OutflowFieldLayer2.Load(float3(screenPos.xy, 0));
					float totalHeight = (terrainHeight + fluidHeight.y) + addedHeight;
					float4 heightDifference;
					heightDifference.x = totalHeight - ((fluidTexelLeft.y + terrainTexelLeft.x) + addedHeightLeft.x);
					heightDifference.y = totalHeight - ((fluidTexelRight.y + terrainTexelRight.x) + addedHeightRight.x);
					heightDifference.z = totalHeight - ((fluidTexelTop.y + terrainTexelTop.x) + addedHeightTop.x);
					heightDifference.w = totalHeight - ((fluidTexelBottom.y + terrainTexelBottom.x) + addedHeightBottom.x);

					float4 newFlow = max(float4(0, 0, 0, 0), oldOutflow + ACCEL_DT_CELLSIZE_LAYER2 * heightDifference);
					float totalOutFlow = ((newFlow.x + newFlow.y + newFlow.z + newFlow.w) * _FluidSimDeltaTime);
					float K = min(1.0f, (fluidHeight.y * CELLSIZESQ_LAYER2) / totalOutFlow);
					K = max(0, K);
					newFlow *= K;

					o.color1 = newFlow;
				}
				#endif

				return o;
			}

			ENDHLSL
		}

		Pass
		{
			Name "ApplyFlux"
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_local _ FLUID_MULTILAYER
			#pragma multi_compile_local _ FLUID_CUSTOMVISCOSITY

			struct fragout
			{
				float4 color0 : SV_Target0;
				float4 color1 : SV_Target1;
			};

			fragout frag(v2f i)
			{
				float4 screenPos = i.vertex;
				screenPos.xy -= 0.5f;
				float4 fluidHeight = _FluidHeightField.Load(int3(screenPos.xy, 0));
				float4 flowC = _OutflowField.Load(int3(screenPos.xy, 0));

				float4 flowL, flowR, flowT, flowB;
				GetNeighbourData(_OutflowField, _OutflowField_TexelSize, screenPos.xy, flowL, flowR, flowT, flowB);
				float v = RCP_DT_CELLSIZESQ_LAYER1 * ((flowL.y + flowR.x + flowT.w + flowB.z) - (flowC.x + flowC.y + flowC.z + flowC.w));
				fluidHeight.x = fluidHeight.x + v; 
				fluidHeight.x -= LINEAR_EVAPORATION_LAYER1;
				fluidHeight.x -= fluidHeight.x * PROPORTIONAL_EVAPORATION_LAYER1;
				fluidHeight.x = max(fluidHeight.x, 0);

				if(fluidHeight.x < _FluidClipHeight) fluidHeight.x = 0;

				float2 velocityDataLayer1;
				velocityDataLayer1.x = ((flowL.y + flowC.y) - (flowR.x + flowC.x))*0.5f;
				velocityDataLayer1.y = ((flowT.w + flowC.w) - (flowB.z + flowC.z))*0.5f;
				velocityDataLayer1 *= VELOCITY_SCALE_LAYER1;
				
			#if FLUID_MULTILAYER
				float4 flowCLayer2 = _OutflowFieldLayer2.Load(int3(screenPos.xy, 0));
				float4 flowLLayer2, flowRLayer2, flowTLayer2, flowBLayer2;
				GetNeighbourData(_OutflowFieldLayer2, _OutflowField_TexelSize, screenPos.xy, flowLLayer2, flowRLayer2, flowTLayer2, flowBLayer2);

			#if FLUID_CUSTOMVISCOSITY
				float4 fluidL, fluidR, fluidT, fluidB;
				GetNeighbourData(_FluidHeightField, _OutflowField_TexelSize, screenPos.xy, fluidL, fluidR, fluidT, fluidB);

				flowCLayer2 *= max(_FluidViscosity, min(fluidHeight.y - _FluidFlowHeight,1));
				flowLLayer2 *= max(_FluidViscosity, min(fluidL.y - _FluidFlowHeight,1));
				flowRLayer2 *= max(_FluidViscosity, min(fluidR.y - _FluidFlowHeight,1));
				flowTLayer2 *= max(_FluidViscosity, min(fluidT.y - _FluidFlowHeight,1));
				flowBLayer2 *= max(_FluidViscosity, min(fluidB.y - _FluidFlowHeight,1));
			#endif
				float vLayer2 = RCP_DT_CELLSIZESQ_LAYER2 * ((flowLLayer2.y + flowRLayer2.x + flowTLayer2.w + flowBLayer2.z) - (flowCLayer2.x + flowCLayer2.y + flowCLayer2.z + flowCLayer2.w));
				fluidHeight.y = fluidHeight.y + vLayer2;
				fluidHeight.y -= LINEAR_EVAPORATION_LAYER2;
				fluidHeight.y -= fluidHeight.y * PROPORTIONAL_EVAPORATION_LAYER2;
				fluidHeight.y = max(fluidHeight.y, 0);
				if(fluidHeight.y < _FluidClipHeight) fluidHeight.y = 0;

				float2 velocityDataLayer2;
				velocityDataLayer2.x = ((flowLLayer2.y + flowCLayer2.y) - (flowRLayer2.x + flowCLayer2.x))*0.5f;
				velocityDataLayer2.y = ((flowTLayer2.w + flowCLayer2.w) - (flowBLayer2.z + flowCLayer2.z))*0.5f;
				velocityDataLayer2 *= VELOCITY_SCALE_LAYER2;
			#endif


				fragout o;
				o.color0 = fluidHeight;


				float border = 1;
				float layer = 0;
#if FLUID_MULTILAYER
				float fluidMask = fluidHeight.x > fluidHeight.y ? 0 : 1;
				layer = fluidMask;
				float2 velocityFactor = max(fluidHeight.xx, fluidHeight.yy);
				velocityFactor = min(velocityFactor, 1);
				velocityFactor *= lerp(CELLSIZE_LAYER1, CELLSIZE_LAYER2, fluidMask);
				float2 outVelocity = lerp(velocityDataLayer1, velocityDataLayer2, fluidMask);

				float resetVelocity = (fluidHeight.x > _FluidClipHeight && fluidHeight.y) > _FluidClipHeight ? 0.0f : 1.0f;
				border = step(_FluidClipHeight, max(fluidHeight.x, fluidHeight.y) );
#else
				float2 velocityFactor = fluidHeight.xx;
				velocityFactor = min(velocityFactor, 1);
				velocityFactor *= CELLSIZE_LAYER1;
				float2 outVelocity = velocityDataLayer1;
				border = step(_FluidClipHeight, fluidHeight.x);
#endif
				if (velocityFactor.x > 0.0001)
					outVelocity /= velocityFactor;

				o.color1 = float4(outVelocity, border, layer);
				return o;
			}
			ENDHLSL
		}
    }
}
