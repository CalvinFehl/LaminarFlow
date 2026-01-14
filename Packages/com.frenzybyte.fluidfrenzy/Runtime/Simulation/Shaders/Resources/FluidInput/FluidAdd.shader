Shader "Hidden/FluidFrenzy/AddFluid"
{
    Properties
    {
		_MainTex("Texture", 2D) = "black" {}
		_FluidHeightField("Texture", 2D) = "white" {}
    }
    SubShader
    {
		HLSLINCLUDE

		#define INPUT_IS_FLUID
		#include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Simulation/Shaders/Resources/FluidSimulationCommon.hlsl"
		#include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Simulation/Shaders/Resources/FluidInputCommon.hlsl"

		float4 fragCircleSetHeight(v2f i, float4 texelSize) 
		{
			float4 screenPos = i.vertex;
			DiscardBoundary(screenPos.xy, texelSize);

			float dist = DistanceCircle(i.uv.xy);
			clip(1 - dist);

			float4 terrainHeight = SampleTerrainHeight(screenPos.xy * texelSize.xy);
			return max(CalculateStrength(dist, screenPos.xy, texelSize) - terrainHeight.xxxx, 0);
		}

		float4 fragSquareSetHeight(v2f i, float4 texelSize) 
		{
			float4 screenPos = i.vertex;
			DiscardBoundary(screenPos.xy, texelSize);

			float dist = DistanceBox(i.uv.xy);
			clip(1 - dist);

			float4 terrainHeight = SampleTerrainHeight(screenPos.xy * texelSize.xy);
			return max(CalculateStrength(dist, screenPos.xy, texelSize) - terrainHeight.xxxx, 0);
		}
		
		float4 fragTexSetHeight(float2 uv, float2 screenPos, float4 texelSize)
		{
			DiscardBoundary(screenPos, texelSize);

			float4 terrainHeight = SampleTerrainHeight(screenPos.xy * texelSize.xy);
			return max((tex2D(_MainTex, uv).rrrr * _IncreaseStrength * _LayerMask) - terrainHeight.xxxx, 0);
		}

		float4 fragTexStatic(v2f i)
		{
			return tex2D(_FluidHeightField, i.uv.xy).rgba + tex2D(_MainTex, i.uv.xy).rgba * _FluidSimDeltaTime;
		}

		float4 fragTexDynamic(v2f i)
		{
			return tex2D(_FluidHeightField, i.uv.xy).rgba + tex2D(_MainTex, i.uv.xy).rgba;
		}


		ENDHLSL
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

		
		// Additive

		// 0
        Pass
        {	
			Name "AddFluidCircle"
			Blend One One
			ColorMask RG
			HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
			#pragma multi_compile ___ OPEN_BORDER

			float4 frag(v2f i) : SV_Target
			{
				return fragCircleAdd(i.uv, i.vertex.xy, _Simulation_TexelSize);
			}

			ENDHLSL
        }

		// 1		
		Pass
        {	
			Name "AddFluidSquare"
			Blend One One
			ColorMask RG
			HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
			#pragma multi_compile ___ OPEN_BORDER

			float4 frag(v2f i) : SV_Target
			{
				return fragSquareAdd(i.uv, i.vertex.xy, _Simulation_TexelSize);
			}

			ENDHLSL
        }
		// 2
		Pass
		{
			Name "AddFluidTexture"
			Blend One One
			ColorMask RG
			HLSLPROGRAM
			#pragma vertex vertTex
			#pragma fragment frag

			float4 frag(v2f i) : SV_Target
			{
				return fragTexAdd(i.uv, i.vertex.y, _Simulation_TexelSize);
			}

			ENDHLSL
		}
		// 3
		Pass
		{
			Name "AddFluidTextureStatic"
			//Blend One One
			ColorMask RG
			HLSLPROGRAM
			#pragma vertex vertTex
			#pragma fragment frag
			
			float4 frag(v2f i) : SV_Target
			{
				return fragTexStatic(i);
			}

			ENDHLSL
		}
		// 4
		Pass
		{
			Name "AddFluidTextureDynamic"
			ColorMask RG
			HLSLPROGRAM
			#pragma vertex vertTexNoFlip
			#pragma fragment frag

			float4 frag(v2f i) : SV_Target
			{
				return fragTexDynamic(i);
			}

			ENDHLSL
		}

		// Set Height
		// 5
		Pass
        {	
			Name "MixFluidHeightCircle"
			Blend One Zero
			BlendOp [_BlendOpFluidInteraction]
			ColorMask [_ColorMaskFluidInteraction]
			HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
			#pragma multi_compile ___ OPEN_BORDER
			#pragma multi_compile ___ _FLUID_UNITY_TERRAIN

			float4 frag(v2f i) : SV_Target
			{
				return fragCircleSetHeight(i, _Simulation_TexelSize);
			}

			ENDHLSL
        }
		// 6
		Pass
        {	
			Name "MixFluidHeightSquare"
			Blend One Zero
			BlendOp [_BlendOpFluidInteraction]
			ColorMask [_ColorMaskFluidInteraction]
			HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
			#pragma multi_compile ___ OPEN_BORDER
			#pragma multi_compile ___ _FLUID_UNITY_TERRAIN

			float4 frag(v2f i) : SV_Target
			{
				return fragSquareSetHeight(i, _Simulation_TexelSize);
			}

			ENDHLSL
        }
		// 7
		Pass
        {	
			Name "MixFluidHeightTexture"
			Blend One Zero
			BlendOp [_BlendOpFluidInteraction]
			ColorMask [_ColorMaskFluidInteraction]
			HLSLPROGRAM
            #pragma vertex vertTex
            #pragma fragment frag
			#pragma multi_compile ___ OPEN_BORDER
			#pragma multi_compile ___ _FLUID_UNITY_TERRAIN

			float4 frag(v2f i) : SV_Target
			{
				return fragTexSetHeight(i.uv, i.vertex.xy, _Simulation_TexelSize);
			}

			ENDHLSL
        }

		// Set Depth
		// 8
		Pass
        {	
			Name "MixFluidDepthCircle"
			Blend One Zero
			BlendOp [_BlendOpFluidInteraction]
			ColorMask [_ColorMaskFluidInteraction]
			HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
			#pragma multi_compile ___ OPEN_BORDER
			#pragma multi_compile ___ _FLUID_UNITY_TERRAIN

			float4 frag(v2f i) : SV_Target
			{
				return fragCircleSetDepth(i.uv, i.vertex.xy, _Simulation_TexelSize);
			}

			ENDHLSL
        }

		// 13
		Pass
        {	
			Name "MixFluidDepthSquare"
			Blend One Zero
			BlendOp [_BlendOpFluidInteraction]
			ColorMask [_ColorMaskFluidInteraction]
			HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
			#pragma multi_compile ___ OPEN_BORDER
			#pragma multi_compile ___ _FLUID_UNITY_TERRAIN

			float4 frag(v2f i) : SV_Target
			{
				return fragSquareSetDepth(i.uv, i.vertex.xy, _Simulation_TexelSize);
			}

			ENDHLSL
        }

		// 15
		Pass
        {	
			Name "MixFluidDepthTexture"
			Blend One Zero
			BlendOp [_BlendOpFluidInteraction]
			ColorMask [_ColorMaskFluidInteraction]
			HLSLPROGRAM
            #pragma vertex vertTex
            #pragma fragment frag
			#pragma multi_compile ___ OPEN_BORDER
			#pragma multi_compile ___ _FLUID_UNITY_TERRAIN

			float4 frag(v2f i) : SV_Target
			{
				return fragTexSetDepth(i.uv, i.vertex.xy, _Simulation_TexelSize);
			}

			ENDHLSL
        }
    }
}
