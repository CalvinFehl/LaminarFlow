Shader "Hidden/FluidFrenzy/FluidCreateRenderData"
{
    Properties
    {
    }
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

		HLSLINCLUDE
		#define SUPPORT_GATHER (!(defined(SHADER_API_VULKAN) || defined(SHADER_API_GLES) || defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE) || defined(SHADER_API_WEBGPU)))

		#define USETEXTURE2D
		#include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Simulation/Shaders/Resources/FluidSimulationCommon.hlsl"

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
			o.uv.xy = GetQuadTexCoord(vid);
			o.uv.zw = GetQuadTexCoord(vid) * _BlitScaleBias.xy + _BlitScaleBias.zw;
			return o;
		}

		CBUFFER_START(WaterSimCombineHeightVelocity)
		float4 _TexelWorldSize;
		CBUFFER_END

		struct fragout
		{
			float4 color0 : SV_Target0;
			float4 color1 : SV_Target1;
		};

		float3 GetNormal(float state_l, float state_r, float state_b, float state_t)
		{
			float texelwss = _TexelWorldSize.x;
			float dhdu = ((state_l)-(state_r));
			float dhdv = ((state_b)-(state_t));
			float3 normal = normalize(float3(dhdu, texelwss.x, dhdv));
			return normal;
		}

		#if FLUID_MULTILAYER || FLUID_MULTILAYER_VELOCITY
		#define MAXFLUID(a) max(a.x, a.y)
		#else
		#define MAXFLUID(a) a.x
		#endif

		#if FLUID_MULTILAYER || FLUID_MULTILAYER_VELOCITY
		//#define FLUIDLAYER(a) ((a.x >= a.y) ? 1.0f : -1.0f)
		#define FLUIDLAYER(a) sign(a.x - a.y)
		#else
		#define FLUIDLAYER(a) 1
		#endif


		SamplerState linear_clamp_sampler;

		fragout CreateRenderData(v2f i, float4 screenPos)
		{
			if (screenPos.x == 0.5f) screenPos.x -= 1;
			if (screenPos.y == 0.5f) screenPos.y -= 1;
			
			screenPos.xy += _BoundaryCells;

			float3 heightUV = (float3(screenPos.xy,0));
			float4 waterHeight = _FluidHeightField.Load(heightUV);

			float layer = FLUIDLAYER(waterHeight);

#if FLUID_FLOW_SIMULATION
			float4 velocitySample = _VelocityField.Sample(linear_clamp_sampler , i.uv.zw);
#else
			float4 velocitySample = _VelocityField.Sample(linear_clamp_sampler , i.uv.zw);
#endif
			#if FLUID_MULTILAYER_VELOCITY
				float2 velocity = layer == 1 ? velocitySample.xy : velocitySample.zw;
			#else
				float2 velocity = velocitySample.xy;
			#endif
			float maxFluidHeight = MAXFLUID(waterHeight);

			float2 waterHeight1 = _FluidHeightField.Load(heightUV + float3(-1,-1, 0)).xy;
			float2 waterHeight2 = _FluidHeightField.Load(heightUV + float3( 1, 1, 0)).xy;
			float2 waterHeight3 = _FluidHeightField.Load(heightUV + float3( 1,-1, 0)).xy;
			float2 waterHeight4 = _FluidHeightField.Load(heightUV + float3(-1, 1, 0)).xy;
			float2 waterHeight5 = _FluidHeightField.Load(heightUV + float3(-1, 0, 0)).xy;
			float2 waterHeight6 = _FluidHeightField.Load(heightUV + float3( 1, 0, 0)).xy;
			float2 waterHeight7 = _FluidHeightField.Load(heightUV + float3( 0,-1, 0)).xy;
			float2 waterHeight8 = _FluidHeightField.Load(heightUV + float3( 0, 1, 0)).xy;

			float max_waterHeight1 = MAXFLUID(waterHeight1);
			float max_waterHeight2 = MAXFLUID(waterHeight2);
			float max_waterHeight3 = MAXFLUID(waterHeight3);
			float max_waterHeight4 = MAXFLUID(waterHeight4);
			float max_waterHeight5 = MAXFLUID(waterHeight5);
			float max_waterHeight6 = MAXFLUID(waterHeight6);
			float max_waterHeight7 = MAXFLUID(waterHeight7);
			float max_waterHeight8 = MAXFLUID(waterHeight8);
		

			#if (SHADER_TARGET >= 40) && (SUPPORT_GATHER || !_FLUID_UNITY_TERRAIN)
				float terrainHeight, terrainHeight1, terrainHeight2, terrainHeight3, terrainHeight4, terrainHeight5 ,terrainHeight6, terrainHeight7, terrainHeight8;
				GatherTerrainAll(heightUV.xy * _TerrainHeightField_TexelSize.xy,terrainHeight4, terrainHeight8, terrainHeight2, 
								terrainHeight5, terrainHeight , terrainHeight6,
								terrainHeight1, terrainHeight7, terrainHeight3);
			#else
				float terrainHeight = LoadTerrainHeight(heightUV.xy).x;
				float terrainHeight1 = LoadTerrainHeight(heightUV.xy, float2(-1,-1)).x;
				float terrainHeight2 = LoadTerrainHeight(heightUV.xy, float2( 1, 1)).x;
				float terrainHeight3 = LoadTerrainHeight(heightUV.xy, float2( 1,-1)).x;
				float terrainHeight4 = LoadTerrainHeight(heightUV.xy, float2(-1, 1)).x;
				float terrainHeight5 = LoadTerrainHeight(heightUV.xy, float2(-1, 0)).x;
				float terrainHeight6 = LoadTerrainHeight(heightUV.xy, float2( 1, 0)).x;
				float terrainHeight7 = LoadTerrainHeight(heightUV.xy, float2( 0,-1)).x;
				float terrainHeight8 = LoadTerrainHeight(heightUV.xy, float2( 0, 1)).x;
			#endif

			float combinedHeight = terrainHeight + maxFluidHeight;
			float totalHeight1 = (max_waterHeight1 >= _FluidClipHeight ? (max_waterHeight1 + terrainHeight1) : combinedHeight);
			float totalHeight2 = (max_waterHeight2 >= _FluidClipHeight ? (max_waterHeight2 + terrainHeight2) : combinedHeight);
			float totalHeight3 = (max_waterHeight3 >= _FluidClipHeight ? (max_waterHeight3 + terrainHeight3) : combinedHeight);
			float totalHeight4 = (max_waterHeight4 >= _FluidClipHeight ? (max_waterHeight4 + terrainHeight4) : combinedHeight);

			float totalHeight5 = (max_waterHeight5 >= _FluidClipHeight ? (max_waterHeight5 + terrainHeight5) : combinedHeight);
			float totalHeight6 = (max_waterHeight6 >= _FluidClipHeight ? (max_waterHeight6 + terrainHeight6) : combinedHeight);
			float totalHeight7 = (max_waterHeight7 >= _FluidClipHeight ? (max_waterHeight7 + terrainHeight7) : combinedHeight);
			float totalHeight8 = (max_waterHeight8 >= _FluidClipHeight ? (max_waterHeight8 + terrainHeight8) : combinedHeight);

			float3 normal = GetNormal(totalHeight5,
				totalHeight6,
				totalHeight7,
				totalHeight8);

			float maskHeight = maxFluidHeight * layer;

			maskHeight += max_waterHeight1 * FLUIDLAYER(waterHeight1);
			maskHeight += max_waterHeight2 * FLUIDLAYER(waterHeight2);
			maskHeight += max_waterHeight3 * FLUIDLAYER(waterHeight3);
			maskHeight += max_waterHeight4 * FLUIDLAYER(waterHeight4);
			maskHeight += max_waterHeight5 * FLUIDLAYER(waterHeight5);
			maskHeight += max_waterHeight6 * FLUIDLAYER(waterHeight6);
			maskHeight += max_waterHeight7 * FLUIDLAYER(waterHeight7);
			maskHeight += max_waterHeight8 * FLUIDLAYER(waterHeight8);
			maskHeight /= 9;

			float offset =  (maxFluidHeight >= _FluidClipHeight) ? any(maxFluidHeight - (_FluidClipHeight)) * _FluidBaseHeightOffset : 0;
			float modifiedHeight = maxFluidHeight + offset;
			if (maxFluidHeight < _FluidClipHeight * normal.y)
			{
				if (max_waterHeight1 >= _FluidClipHeight && terrainHeight > terrainHeight1)
				{
					modifiedHeight = totalHeight1 - combinedHeight;
				}
				else if (max_waterHeight2 >= _FluidClipHeight && terrainHeight > terrainHeight2)
				{
					modifiedHeight = totalHeight2 - combinedHeight;
				}
				else if (max_waterHeight3 >= _FluidClipHeight && terrainHeight > terrainHeight3)
				{
					modifiedHeight = totalHeight3 - combinedHeight;
				}
				else if (max_waterHeight4 >= _FluidClipHeight && terrainHeight > terrainHeight4)
				{
					modifiedHeight = totalHeight4 - combinedHeight;
				}
				else if (max_waterHeight5 >= _FluidClipHeight && terrainHeight > terrainHeight5)
				{
					modifiedHeight = totalHeight5 - combinedHeight;
				}
				else if (max_waterHeight6 >= _FluidClipHeight && terrainHeight > terrainHeight6)
				{
					modifiedHeight = totalHeight6 - combinedHeight;
				}
				else if (max_waterHeight7 >= _FluidClipHeight && terrainHeight > terrainHeight7)
				{
					modifiedHeight = totalHeight7 - combinedHeight;
				}
				else if (max_waterHeight8 >= _FluidClipHeight && terrainHeight > terrainHeight8)
				{
					modifiedHeight = totalHeight8 - combinedHeight;
				}
				else
				{
					modifiedHeight = -terrainHeight;
				}
				normal = float3(0,1,0);
				maxFluidHeight = 0;
				maskHeight = 0;
			}

			fragout o;
			float finalTotalHeight = terrainHeight + maxFluidHeight;
			o.color0 = float4(finalTotalHeight * layer, modifiedHeight, velocity );
			o.color1 = float4(normal, maskHeight);
			return o;
		}

		fragout frag(v2f i)
		{
			return CreateRenderData(i, i.vertex);
		}


		
		ENDHLSL

        Pass
        {
			Name "CopyHeightVelocity"
            HLSLPROGRAM
			#pragma target 5.0
            #pragma vertex vert
            #pragma fragment frag
			#pragma multi_compile_local _ FLUID_MULTILAYER FLUID_MULTILAYER_VELOCITY
			#pragma multi_compile_local _ _FLUID_UNITY_TERRAIN
			#pragma multi_compile_local _ FLUID_FLOW_SIMULATION
            ENDHLSL
        }
    }

	SubShader
    {
		Pass
        {
			Name "CopyHeightVelocity"
            HLSLPROGRAM
			#pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
			#pragma multi_compile_local _ FLUID_MULTILAYER FLUID_MULTILAYER_VELOCITY
			#pragma multi_compile_local _ _FLUID_UNITY_TERRAIN
			#pragma multi_compile_local _ FLUID_FLOW_SIMULATION
            ENDHLSL
        }
	}
}
