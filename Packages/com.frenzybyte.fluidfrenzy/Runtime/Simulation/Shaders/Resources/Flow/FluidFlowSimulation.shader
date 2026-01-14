Shader "Hidden/FluidFrenzy/Simulation/Flow/FlowSimulation" 
{ 
	SubShader
	{
		Cull Off ZWrite Off ZTest Always

		HLSLINCLUDE

		#define USETEXTURE2D
		#include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Simulation/Shaders/Resources/FluidSimulationCommon.hlsl"
		#include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Simulation/Shaders/Resources/Flow/FluidFlowCommon.hlsl"


		// Output vertex structure
		struct v2f {
			float2 uv : TEXCOORD0;
			float4 vertex : SV_POSITION;
		};

		v2f vert(uint vid : SV_VertexID)
		{
			v2f o;
			o.vertex = GetQuadVertexPosition(vid) * float4(_BlitScaleBiasRt.x, _BlitScaleBiasRt.y, 1, 1) + float4(_BlitScaleBiasRt.z, _BlitScaleBiasRt.w, 0, 0);
			o.vertex.xy = o.vertex.xy * float2(2.0f, -2.0f) + float2(-1.0f, 1.0f); //convert to -1..1
			o.uv = GetQuadTexCoord(vid)  * _BlitScaleBias.xy + _BlitScaleBias.zw;
			return o;
		}

		ENDHLSL

		Pass
		{
			Name "AdvectVelocity"
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_local _ FLUID_MULTILAYER
			#pragma multi_compile_local _ _FLUID_UNITY_TERRAIN
			// Fragment shader
			float4 frag(v2f i) : SV_Target {
				//Section 2.1.1 Velocity Advection
				return SampleVelocityMacCormack(i.uv, float2(0, 0));
			}
			ENDHLSL
		}

		Pass 
		{
			Name "IntegrateHeight"
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_local _ FLUID_MULTILAYER
			#pragma multi_compile_local _ _FLUID_UNITY_TERRAIN
			#pragma multi_compile _ OPEN_BORDER

			// Fragment shader
			float4 frag(v2f i) : SV_Target {
				
				//Section 2.1.2 Height Integration
				float2 fluidHeight = SampleFluidHeight(i.uv);
				float2 heightDelta = GetFluidHeightDelta(i.uv);

				fluidHeight.xy -= heightDelta * _FluidSimDeltaTime;

				fluidHeight.xy -= LINEAR_EVAPORATION;
				fluidHeight.xy -= fluidHeight.xy * PROPORTIONAL_EVAPORATION;

				fluidHeight = max(0, fluidHeight);

				if(fluidHeight.x < _FluidClipHeight) fluidHeight.x = 0;
				if(fluidHeight.y < _FluidClipHeight) fluidHeight.y = 0;
				
				return float4(fluidHeight,0,0);
			}
			ENDHLSL
		}

		Pass
		{
			Name "IntegrateVelocity"
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_local _ FLUID_MULTILAYER
			#pragma multi_compile_local _ _FLUID_UNITY_TERRAIN
			#pragma multi_compile _ OPEN_BORDER

			// Fragment shader
			float4 frag(v2f i) : SV_Target 
			{
				//Section 2.1.1 Velocity Advection
				float4 velocity = SampleVelocityMacCormack(i.uv, float2(0,0));
				//Section 2.1.3 Velocity Integration
				float2 fluidHeightC = SampleFluidHeight(i.uv, float2(0, 0));
				float2 fluidHeightR = SampleFluidHeight(i.uv, float2(1, 0));
				float2 fluidHeightT = SampleFluidHeight(i.uv, float2(0, 1));

				float terrainHeightC = SampleTerrainHeight(i.uv, float2(0, 0));
				float terrainHeightR = SampleTerrainHeight(i.uv, float2(1, 0));
				float terrainHeightT = SampleTerrainHeight(i.uv, float2(0, 1));

				{
					float totalHeightC = fluidHeightC.x + terrainHeightC;
					float totalHeightR = fluidHeightR.x + terrainHeightR;
					float totalHeightT = fluidHeightT.x + terrainHeightT;

					float heightC = fluidHeightC.x + terrainHeightC;
					float2 terrainHeightRT = float2(terrainHeightR, terrainHeightT);
					float2 heightSumRT = float2(totalHeightR, totalHeightT);

					float2 heightDelta = (heightC - heightSumRT) * CELLSIZE_RCP_LAYER1;
				
					float2 acceleration = ACCELERATION_LAYER1 * _FluidSimDeltaTime * heightDelta;
					velocity.xy += ClampVector(acceleration, _AccelerationMax.x).xy;
					velocity.xy = ClampVector(velocity.xy, _VelocityMax.x).xy;

					//Section 2.1.4. Boundary Condition
					if (fluidHeightC.x <= _FluidClipHeight && terrainHeightC > totalHeightR)velocity.x = 0;
					if (fluidHeightC.x <= _FluidClipHeight && terrainHeightC > totalHeightT)velocity.y = 0;

					if (fluidHeightR.x <= _FluidClipHeight && terrainHeightR > totalHeightC)velocity.x = 0;
					if (fluidHeightT.x <= _FluidClipHeight && terrainHeightT > totalHeightC)velocity.y = 0;
				}

				#if FLUID_MULTILAYER
				{
					float totalHeightC = fluidHeightC.y + terrainHeightC;
					float totalHeightR = fluidHeightR.y + terrainHeightR;
					float totalHeightT = fluidHeightT.y + terrainHeightT;

					float heightC = fluidHeightC.y + terrainHeightC;
					float2 terrainHeightRT = float2(terrainHeightR, terrainHeightT);
					float2 heightSumRT = float2(totalHeightR, totalHeightT);

					float2 heightDelta = (heightC - heightSumRT) * CELLSIZE_RCP_LAYER2;
				
					float2 acceleration = ACCELERATION_LAYER2 * _FluidSimDeltaTime * heightDelta;
					velocity.zw += ClampVector(acceleration, _AccelerationMax.y);
					velocity.zw = ClampVector(velocity.zw, _VelocityMax.y);
					//Section 2.1.4. Boundary Condition
					if (fluidHeightC.y <= _FluidClipHeight && terrainHeightC > totalHeightR) velocity.z = 0;
					if (fluidHeightC.y <= _FluidClipHeight && terrainHeightC > totalHeightT) velocity.w = 0;

					if (fluidHeightR.y <= _FluidClipHeight && terrainHeightR > totalHeightC) velocity.z = 0;
					if (fluidHeightT.y <= _FluidClipHeight && terrainHeightT > totalHeightC) velocity.w = 0;
				}
				#endif


				velocity += -0.5 * velocity * _Damping.xxyy;
				velocity -= velocity * min((1-smoothstep(0, 0.0001, fluidHeightC.xxyy)) * _FluidSimDeltaTime * 10, 1);

				//2.1.5. Stability Enhancements
				float4 maxVelocity = 0.5 * (_CellSize.xyzw / _FluidSimDeltaTime);
				velocity = clamp(velocity, -maxVelocity, maxVelocity);


			#if !OPEN_BORDER
				//if(i.uv.x < _VelocityField_TexelSize.x)velocity.xyzw = 0;
				//if(i.uv.y < _VelocityField_TexelSize.y)velocity.xyzw = 0;				
				//
				//if(i.uv.x > 1 - _VelocityField_TexelSize.x * 2)velocity.xyzw = 0;
				//if(i.uv.y > 1 - _VelocityField_TexelSize.y * 2)velocity.xyzw = 0;
			#else
				//if(i.uv.x < _VelocityField_TexelSize.x)velocity.xz = -_VelocityMax;
				//if(i.uv.y < _VelocityField_TexelSize.y)velocity.yw = -_VelocityMax;				
				//
				//if(i.uv.x > 1 - _VelocityField_TexelSize.x)velocity.xz = _VelocityMax;
				//if(i.uv.y > 1 - _VelocityField_TexelSize.y)velocity.yw = _VelocityMax;	
			#endif

				return velocity;
			}
			ENDHLSL
		}

		Pass
		{
			Name "OvershootReduction"
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_local _ FLUID_MULTILAYER
			#pragma multi_compile_local _ _FLUID_UNITY_TERRAIN
			#pragma multi_compile _ OPEN_BORDER

			// Fragment shader
			float4 frag(v2f i) : SV_Target {
				//2.1.5. Stability Enhancements
				float2 fluidHeightC = SampleFluidHeight(i.uv, float2(0, 0));
				float2 fluidHeightR = SampleFluidHeight(i.uv, float2(1, 0));
				float2 fluidHeightT = SampleFluidHeight(i.uv, float2(0, 1));				
				float2 fluidHeightL = SampleFluidHeight(i.uv, float2(-1, 0));
				float2 fluidHeightB = SampleFluidHeight(i.uv, float2(0, -1));

				float terrainHeightC = SampleTerrainHeight(i.uv, float2(0, 0));
				float terrainHeightR = SampleTerrainHeight(i.uv, float2(1, 0));
				float terrainHeightT = SampleTerrainHeight(i.uv, float2(0, 1));
				float terrainHeightL = SampleTerrainHeight(i.uv, float2(-1, 0));
				float terrainHeightB = SampleTerrainHeight(i.uv, float2(0, -1));

				float totalHeightC = terrainHeightC + fluidHeightC.x;
				float totalHeightR = terrainHeightR + fluidHeightR.x;
				float totalHeightL = terrainHeightL + fluidHeightL.x;
				float totalHeightT = terrainHeightT + fluidHeightT.x;
				float totalHeightB = terrainHeightB + fluidHeightB.x;

				const float alpha = _OvershootingScale;
				const float lambda = _OvershootingEdge;

				float2 newFluidHeight = fluidHeightC;

				if ((totalHeightC - totalHeightB > lambda) && (totalHeightC > totalHeightT))
				{
					newFluidHeight.x += alpha * (max(0.0, 0.5f * (fluidHeightC.x + fluidHeightT.x))-fluidHeightC.x);
				}		
				if ((totalHeightC - totalHeightT > lambda) && (totalHeightC > totalHeightB))
				{
					newFluidHeight.x += alpha * (max(0.0, 0.5f * (fluidHeightC.x + fluidHeightB.x))-fluidHeightC.x);
				}

				if ((totalHeightC - totalHeightR > lambda) && (totalHeightC > totalHeightL))
				{
					newFluidHeight.x += alpha * (max(0.0, 0.5f * (fluidHeightC.x + fluidHeightL.x))-fluidHeightC.x);
				}
				if ((totalHeightC - totalHeightL > lambda) && (totalHeightC > totalHeightR))
				{
					newFluidHeight.x += alpha * (max(0.0, 0.5f * (fluidHeightC.x + fluidHeightR.x))-fluidHeightC.x);
				}
				return float4(newFluidHeight,0,0);
			}
			ENDHLSL
		}
	}
}
