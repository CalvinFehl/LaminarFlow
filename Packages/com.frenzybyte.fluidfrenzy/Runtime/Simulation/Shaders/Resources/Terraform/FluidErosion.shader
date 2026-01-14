Shader "Hidden/FluidFrenzy/Erosion"
{
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

		HLSLINCLUDE
		#include  "Packages/com.frenzybyte.fluidfrenzy/Runtime/Simulation/Shaders/Resources/FluidSimulationCommon.hlsl"
		float4 _LayerMask;
		float4 _TotalHeightLayerMask;
		float4 _TopLayerMask;
		struct v2f
		{
			float4 uv : TEXCOORD0;
			float4 vertex : SV_POSITION;
		};

		v2f vert(uint vid : SV_VertexID)
		{
			v2f o;
			o.vertex = GetQuadVertexPosition(vid) * float4(_BlitScaleBiasRt.x, _BlitScaleBiasRt.y, 1, 1) + float4(_BlitScaleBiasRt.z, _BlitScaleBiasRt.w, 0, 0);
			o.vertex.xy = o.vertex.xy * float2(2.0f, -2.0f) + float2(-1.0f, 1.0f); //convert to -1..1
			o.uv.xy = GetQuadTexCoord(vid) * _BlitScaleBias.xy + _BlitScaleBias.zw;
			o.uv.zw = GetQuadTexCoord(vid);
			return o;
		}

		ENDHLSL

		Pass
		{
			Name "MaxSlippage"
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			float _MaxHeightDif;
			float _SlopeSmoothness;

			float4 frag(v2f i) : SV_Target
			{
				float terrainHeight = dot(tex2D(_TerrainHeightField, i.uv.xy), _TotalHeightLayerMask);
				float4 terrainDataLeft, terrainDataRight, terrainDataTop, terrainDataBottom;
				GetNeighbourData(_TerrainHeightField, _TerrainHeightField_TexelSize, i.uv.xy, terrainDataLeft, terrainDataRight, terrainDataTop, terrainDataBottom);

				float terrainLeft = dot(terrainDataLeft, _TotalHeightLayerMask);
				float terrainRight = dot(terrainDataRight, _TotalHeightLayerMask);
				float terrainTop = dot(terrainDataTop, _TotalHeightLayerMask);
				float terrainBottom = dot(terrainDataBottom, _TotalHeightLayerMask);

				float4 totalHeight;
				totalHeight.x = terrainLeft.x;
				totalHeight.y = terrainRight.x;
				totalHeight.z = terrainTop.x;
				totalHeight.w = terrainBottom.x;

				float averageHeight = dot(totalHeight,(1.0f).xxxx) * 0.25f;
				float avgDif = (averageHeight)  - terrainHeight;

				avgDif = _SlopeSmoothness * max(abs(avgDif), 0);
				return max(_MaxHeightDif - avgDif, 0);;
			}
			ENDHLSL
		}

		Pass
		{
			Name "ErosionOutflow"
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			sampler2D _MaxHeightField;

			float _ErosionOutflowRate;

			float4 frag(v2f i) : SV_Target
			{
				float4 terrainSample = tex2D(_TerrainHeightField, i.uv.xy);
				float terrainHeight = dot(terrainSample, _TotalHeightLayerMask);
				float4 terrainDataLeft, terrainDataRight, terrainDataTop, terrainDataBottom;
				GetNeighbourData(_TerrainHeightField, _TerrainHeightField_TexelSize, i.uv.xy, terrainDataLeft, terrainDataRight, terrainDataTop, terrainDataBottom);

				float terrainLeft = dot(terrainDataLeft, _TotalHeightLayerMask);
				float terrainRight = dot(terrainDataRight, _TotalHeightLayerMask);
				float terrainTop = dot(terrainDataTop, _TotalHeightLayerMask);
				float terrainBottom = dot(terrainDataBottom, _TotalHeightLayerMask);

				float maxHeight = tex2D(_MaxHeightField, i.uv.xy).x;
				float4 maxHeightLeft, maxHeightRight, maxHeightTop, maxHeightBottom;
				GetNeighbourData(_MaxHeightField, _TerrainHeightField_TexelSize, i.uv.xy, maxHeightLeft, maxHeightRight, maxHeightTop, maxHeightBottom);

				float4 newFlow;
				newFlow.x = terrainHeight - terrainLeft - (maxHeightLeft.r + maxHeight) * 0.5;
				newFlow.y = terrainHeight - terrainRight - (maxHeightRight.r + maxHeight) * 0.5;
				newFlow.z = terrainHeight - terrainTop - (maxHeightTop.r + maxHeight) * 0.5;
				newFlow.w = terrainHeight - terrainBottom - (maxHeightBottom.r + maxHeight) * 0.5;

				newFlow = max(float4(0,0,0,0), newFlow) * _ErosionOutflowRate;

				float outFactor = ((newFlow.x + newFlow.y + newFlow.z + newFlow.w) * _FluidSimStepDeltaTime)  ;
				outFactor = min(dot(terrainSample, _LayerMask) / outFactor, 1);
				outFactor = max(0, outFactor);
				newFlow *= outFactor;
				
				return newFlow;
			}
			ENDHLSL
		}


		Pass
		{
			Name "ErosionApply"
			HLSLPROGRAM
				
			#pragma vertex vert
			#pragma fragment frag

			float4 frag(v2f i) : SV_Target
			{
				float4 terrainHeight = tex2D(_TerrainHeightField, i.uv.xy);
				float4 flowC = tex2D(_OutflowField, i.uv.xy);
				float4 flowL, flowR, flowT, flowB;
				GetNeighbourData(_OutflowField, _OutflowField_TexelSize, i.uv.xy, flowL, flowR, flowT, flowB);

				float v = _FluidSimStepDeltaTime * ((flowL.y + flowR.x + flowT.w + flowB.z) - (flowC.x + flowC.y + flowC.z + flowC.w));
				return terrainHeight + (v * _LayerMask);
			}
			ENDHLSL
		}


		Pass
		{
			Name "Sediment"
			HLSLPROGRAM
			#pragma vertex vert_velocity
			#pragma fragment frag

			struct v2f_advection
			{
				float4 uv01 : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			float4 _VelocityBlitScaleBias;

			v2f_advection vert_velocity(uint vid : SV_VertexID)
			{
				v2f_advection o;
				o.vertex = GetQuadVertexPosition(vid);
				o.vertex.xy = o.vertex.xy * float2(2.0f, -2.0f) + float2(-1.0f, 1.0f); //convert to -1..1
				float2 uv = GetQuadTexCoord(vid);
				o.uv01.xy = uv;
				o.uv01.zw = uv * _VelocityBlitScaleBias.xy + _VelocityBlitScaleBias.zw;
				return o;
			}

			struct fragout
			{
				float4 terrain : SV_Target0;
				float4 sediment : SV_Target1;
			};

			sampler2D _SedimentField;
			float4 _SedimentMax;
			float4 _DissolveRate;
			float4 _DepositRate;
			float _MinTiltAngle;
			float _TexelWorldSize;

			int _NumLayers;

			float GetTiltAngle(float2 uv)
			{
				float texelwss = _TexelWorldSize.x;
				float2 du = float2(_TerrainHeightField_TexelSize.x, 0);
				float2 dv = float2(0, _TerrainHeightField_TexelSize.y);

				float state_l = dot(tex2Dlod(_TerrainHeightField, float4(uv.xy + du, 0, 0)), (1.0f).xxxx);
				float state_r = dot(tex2Dlod(_TerrainHeightField, float4(uv.xy - du, 0, 0)), (1.0f).xxxx);
				float state_t = dot(tex2Dlod(_TerrainHeightField, float4(uv.xy + dv, 0, 0)), (1.0f).xxxx);
				float state_b = dot(tex2Dlod(_TerrainHeightField, float4(uv.xy - dv, 0, 0)), (1.0f).xxxx);

				float dhdu = ((state_r)-(state_l));
				float dhdv = ((state_b)-(state_t));

				float3 n = float3(dhdu, texelwss, dhdv); 
				float3 normal = normalize(n);

				return length(normal.xz);
			}

			fragout frag(v2f_advection i)
			{
				float tiltAngle = GetTiltAngle(i.uv01.xy);
				tiltAngle = (tiltAngle < _MinTiltAngle) ? _MinTiltAngle : tiltAngle;
				float4 terrainC = max(0, tex2D(_TerrainHeightField, i.uv01.xy));
				float4 fluid = tex2D(_FluidHeightField, i.uv01.xy);
				float4 sedimentC = tex2D(_SedimentField, i.uv01.xy);

				float velocity = length(tex2D(_VelocityField, i.uv01.zw).xy) / max(fluid.x, 1) * (1-any(fluid.y));

				float sedimentCapacityFactor = tiltAngle * velocity;

				float finalMaxSediment[4];
				finalMaxSediment[0] = _SedimentMax[0] * sedimentCapacityFactor;
				finalMaxSediment[1] = _SedimentMax[1] * sedimentCapacityFactor;
				finalMaxSediment[2] = _SedimentMax[2] * sedimentCapacityFactor;
				finalMaxSediment[3] = _SedimentMax[3] * sedimentCapacityFactor;

				float4 terrainDif = 0.0f;
				float4 totalSedimentDif = 0.0f;

				// This loop iterates from the highest possible layer (3) down to the first erodible layer (0).
				// It will only process the FIRST layer it finds that has material.
				bool hasEroded = false;
				for (int k = _NumLayers - 1; k >= 0; k--)
				{
					// Get the current amount of sediment from this layer's type.
					float currentSedimentOfTypeK = sedimentC[k];
					// Get the maximum amount of this sediment type the water can hold.
					float capacityForTypeK = finalMaxSediment[k];
				
					// Create a mask to target this specific layer.
					float4 layerMask = float4(k == 0, k == 1, k == 2, k == 3);

					// Compare the current amount to the capacity for this layer's material.
					if (currentSedimentOfTypeK > capacityForTypeK)
					{
						float excessSediment = currentSedimentOfTypeK - capacityForTypeK;
						float sedimentToDeposit = excessSediment * _DepositRate[k];
						sedimentToDeposit = min(sedimentToDeposit, currentSedimentOfTypeK);
						totalSedimentDif -= sedimentToDeposit * layerMask;
						terrainDif += sedimentToDeposit * layerMask;
					}
					// Check if this is the highest layer with a non-zero height.
					else if (terrainC[k] > 0.0001f && hasEroded == false)
					{
						float remainingCapacity = capacityForTypeK - currentSedimentOfTypeK;
						float sedimentToErode = remainingCapacity * _DissolveRate[k];
						sedimentToErode = min(sedimentToErode, terrainC[k]);
						totalSedimentDif += sedimentToErode * layerMask;
						terrainDif -= sedimentToErode * layerMask;
						hasEroded = true;
					}
				}

				fragout o = (fragout)(0);
				o.terrain = terrainC + terrainDif;
				o.sediment = sedimentC + totalSedimentDif;

				return o;
			}
			ENDHLSL
		}


		Pass
		{
			Name "AdvectSedimentFast"
			HLSLPROGRAM
			#pragma vertex vert_velocity
			#pragma fragment frag

			#include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Simulation/Shaders/Resources/FluidSimulationCommon.hlsl"

			struct v2f_advection
			{
				float4 uv01 : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			sampler2D _SedimentField;
			float4 _VelocityBlitScaleBias;

			v2f_advection vert_velocity(uint vid : SV_VertexID)
			{
				v2f_advection o;
				o.vertex = GetQuadVertexPosition(vid);
				o.vertex.xy = o.vertex.xy * float2(2.0f, -2.0f) + float2(-1.0f, 1.0f); //convert to -1..1
				float2 uv = GetQuadTexCoord(vid);
				o.uv01.xy = uv;
				o.uv01.zw = uv * _VelocityBlitScaleBias.xy + _VelocityBlitScaleBias.zw;
				return o;
			}

			CBUFFER_START(Advect)
			float2 _AdvectScale;
			CBUFFER_END

			float4 frag(v2f_advection i) : SV_Target
			{
				float4 fluid = tex2D(_FluidHeightField, i.uv01.xy);
				float4 vel = tex2D(_VelocityField, i.uv01.zw) / max(fluid.x, 1);;
				float2 pos = i.uv01.xy - (vel.xy * _AdvectScale );
				return tex2D(_SedimentField, pos);
			}
			ENDHLSL
		}


		Pass
		{
			Name "AdvectSediment"
			HLSLPROGRAM
			#pragma vertex vert_velocity
			#pragma fragment frag

			#include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Simulation/Shaders/Resources/FluidSimulationCommon.hlsl"

			struct v2f_advection
			{
				float4 uv01 : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			sampler2D _SedimentField;
			float4 _SedimentField_TexelSize;
			float4 _VelocityBlitScaleBias;


			v2f_advection vert_velocity(uint vid : SV_VertexID)
			{
				v2f_advection o;
				o.vertex = GetQuadVertexPosition(vid);
				o.vertex.xy = o.vertex.xy * float2(2.0f, -2.0f) + float2(-1.0f, 1.0f); //convert to -1..1
				float2 uv = GetQuadTexCoord(vid);
				o.uv01.xy = uv;
				o.uv01.zw = uv * _VelocityBlitScaleBias.xy + _VelocityBlitScaleBias.zw;
				return o;
			}

			CBUFFER_START(Advect)
			float2 _AdvectScale;
			CBUFFER_END

			float4 frag(v2f_advection i) : SV_Target
			{
                float2 sedimentTexelSize = _SedimentField_TexelSize.xy;
				float4 fluid = tex2D(_FluidHeightField, i.uv01.xy);
				float4 vel = tex2D(_VelocityField, i.uv01.zw) / max(fluid.x, 1);
				float2 pos = i.uv01.xy - (vel.xy * _AdvectScale ); 
				return GetBilinearSample(_SedimentField, pos, sedimentTexelSize);
			}
			ENDHLSL
		}

		Pass
		{
			Name "ProcessMacCormack"
			HLSLPROGRAM
			#pragma vertex vert_velocity
			#pragma fragment frag

			#include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Simulation/Shaders/Resources/FluidSimulationCommon.hlsl"

			struct v2f_advection
			{
				float4 uv01 : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};
			
			sampler2D _SedimentField; 
			sampler2D _InterField1;
			sampler2D _InterField2;
			float4 _VelocityBlitScaleBias;
			float4 _SedimentField_TexelSize;

			v2f_advection vert_velocity(uint vid : SV_VertexID)
			{
				v2f_advection o;
				o.vertex = GetQuadVertexPosition(vid);
				o.vertex.xy = o.vertex.xy * float2(2.0f, -2.0f) + float2(-1.0f, 1.0f); //convert to -1..1
				float2 uv = GetQuadTexCoord(vid);
				o.uv01.xy = uv;
				o.uv01.zw = uv * _VelocityBlitScaleBias.xy + _VelocityBlitScaleBias.zw; 
				return o;
			}

			CBUFFER_START(MacCormackParams)
			float2 _AdvectScale;
			CBUFFER_END

			float4 frag(v2f_advection i) : SV_Target
			{
				float2 uv = i.uv01.xy; 
				float4 fluid = tex2D(_FluidHeightField, i.uv01.xy);
				float4 velocity = tex2D(_VelocityField, i.uv01.zw) / max(fluid.x, 1); 
				float2 targetUV = uv - (velocity.xy * _AdvectScale); // targetUV is 0..1
				float2 targetPx = targetUV * _SedimentField_TexelSize.zw; 

				float4 st_px;
				st_px.xy = floor(targetPx - 0.5) + 0.5; 
				st_px.zw = st_px.xy + 1.0;            

				float2 uv0 = st_px.xy * _SedimentField_TexelSize.xy; 
				float2 uv1 = float2(st_px.z, st_px.y) * _SedimentField_TexelSize.xy; 
				float2 uv2 = float2(st_px.x, st_px.w) * _SedimentField_TexelSize.xy; 
				float2 uv3 = st_px.zw * _SedimentField_TexelSize.xy; 

				float4 nodeVal[4];
				nodeVal[0] = tex2D(_SedimentField, uv0);
				nodeVal[1] = tex2D(_SedimentField, uv1);
				nodeVal[2] = tex2D(_SedimentField, uv2);
				nodeVal[3] = tex2D(_SedimentField, uv3);	

				float4 clampMin = min(min(nodeVal[0], nodeVal[1]), min(nodeVal[2], nodeVal[3]));
				float4 clampMax = max(max(nodeVal[0], nodeVal[1]), max(nodeVal[2], nodeVal[3]));
				float4 inter1 = tex2D(_InterField1, uv);
				float4 adv = tex2D(_SedimentField, uv);
				float4 inter2 = tex2D(_InterField2, uv);

				float4 res = inter1 + 0.5 * (adv - inter2);

				return max(min(res, clampMax), clampMin);
			}
			ENDHLSL
		}

		Pass
		{
			Name "CopyTerrain"
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment fragTerrain

			float4 fragTerrain(v2f i) : SV_Target
			{
				float4 terrain = tex2D(_TerrainHeightField, i.uv.xy);
				return terrain;
			}
			ENDHLSL
		}

		Pass
		{
			Name "CombineTerrain"
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment fragTerrain

			sampler2D _Obstacles;

			float4 fragTerrain(v2f i) : SV_Target
			{
				float terrain = dot(tex2D(_TerrainHeightField, i.uv.xy), (1.0f).xxxx);
				float obstacles = tex2D(_Obstacles, (i.uv.xy)).x;
				terrain = max(terrain, obstacles);
				return terrain.xxxx;
			}
			ENDHLSL
		}
    }
}
