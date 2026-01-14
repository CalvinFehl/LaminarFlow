Shader "Hidden/FluidFrenzy/JumpFlood"
{
    Properties
    {
    }
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

		HLSLINCLUDE
		#define SUPPORT_GATHER (!(defined(SHADER_API_VULKAN) || defined(SHADER_API_GLES) || defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))

		#define USETEXTURE2D
		#include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Simulation/Shaders/Resources/FluidSimulationCommon.hlsl"

		float _StepSize;
		float _AspectRatio;
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
			o.uv.xy = GetQuadTexCoord(vid);
			return o;
		}


		SamplerState point_clamp_sampler;

		float4 frag_init(v2f i) : SV_Target0
		{
			float4 sample = _FluidHeightField.Sample(point_clamp_sampler, i.uv);

			const float2 minusOne = -1;

			return float4(sample.g > 0.2 ? i.uv : minusOne, 0, 0);
		}

		float4 frag_jumpflood(v2f i) : SV_Target0
		{
			float bestDistance = 99999.0f;
			float2 bestUV = -1;

			for(int y = -1; y <= 1; y++)
			{
				for(int x = -1; x <= 1; x++)
				{
					float2 uv = i.uv + float2(x,y) * _StepSize;

					float4 sample = _FluidHeightField.Sample(point_clamp_sampler, uv);

					float2 dir = sample.xy * float2(_AspectRatio,1) - i.uv* float2(_AspectRatio,1);
					float dist = dot(dir, dir);
					if(sample.x >= 0 && sample.y >=0 && dist < bestDistance)
					{
						bestDistance = dist;
						bestUV = sample.xy;

					}
				}
			}

			return float4(bestUV, 0, 0);
		}
		
		ENDHLSL

        Pass
        {
			Name "InitJumpFlood"
            HLSLPROGRAM
			#pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag_init
			#pragma multi_compile_local _ FLUID_MULTILAYER FLUID_MULTILAYER_VELOCITY
			#pragma multi_compile_local _ _FLUID_UNITY_TERRAIN
			#pragma multi_compile_local _ FLUID_FLOW_SIMULATION
            ENDHLSL
        }

		Pass
        {
			Name "StepJumpFlood"
            HLSLPROGRAM
			#pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag_jumpflood
			#pragma multi_compile_local _ FLUID_MULTILAYER FLUID_MULTILAYER_VELOCITY
			#pragma multi_compile_local _ _FLUID_UNITY_TERRAIN
			#pragma multi_compile_local _ FLUID_FLOW_SIMULATION
            ENDHLSL
        }
	}
}
