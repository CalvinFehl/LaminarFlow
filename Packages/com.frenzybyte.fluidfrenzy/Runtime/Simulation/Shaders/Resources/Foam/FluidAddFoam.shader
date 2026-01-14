Shader "Hidden/FluidFrenzy/AddFoam"
{
    Properties
    {
		_MainTex("Texture", 2D) = "white" {}
    }
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always
		ColorMask R
		Blend One One, One One
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

			#include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Simulation/Shaders/Resources/FluidSimulationCommon.hlsl"

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

			v2f vert(uint vid : SV_VertexID)
			{
				v2f o;
				o.vertex = GetQuadVertexPosition(vid);
				o.vertex.xy = o.vertex.xy * _BlitScaleBiasRt.xy + _BlitScaleBiasRt.zw - _BlitScaleBiasRt.xy * 0.5f;
			#if UNITY_UV_STARTS_AT_TOP
				o.vertex.xy = o.vertex.xy * float2(2.0f, -2.0f) + float2(-1.0f, 1.0f); //convert to -1..1
			#else
				o.vertex.xy = o.vertex.xy * float2(2.0f, 2.0f) + float2(-1.0f, -1.0f); //convert to -1..1
			#endif
				o.uv.xy = GetQuadTexCoord(vid) * 2 - 1;
				return o;
			}

			sampler2D _MainTex;
			float _IncreaseStrength;
			float _IncreaseExponent;

            float4 frag (v2f i) : SV_Target
            {
				float dist = length(i.uv);
				float scale = pow(max(0, 1 - dist), _IncreaseExponent) * _IncreaseStrength;
				return scale;

            }
            ENDHLSL
        }
    }
}
