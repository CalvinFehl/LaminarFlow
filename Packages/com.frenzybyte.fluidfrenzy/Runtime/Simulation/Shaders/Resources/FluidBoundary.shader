Shader "Hidden/FluidFrenzy/Boundary"
{
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

		HLSLINCLUDE

		#include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Simulation/Shaders/Resources/FluidSimulationCommon.hlsl"

		SamplerState point_clamp_sampler;

		Texture2D<float4> _MainTex;
        int _RotateBlit;
        int _RotateSample;
		float4 _BoundaryValue;

		struct v2f
		{
			float2 uv : TEXCOORD0;
			float4 vertex : SV_POSITION;
		};


		v2f vert(uint vid : SV_VertexID)
		{
			v2f o;
			o.vertex = GetQuadVertexPosition(vid) * float4(_BlitScaleBiasRt.x, _BlitScaleBiasRt.y, 1, 1) + float4(_BlitScaleBiasRt.z, _BlitScaleBiasRt.w, 0, 0);;
			o.vertex.xy = o.vertex.xy * float2(2.0f, 2.0f) + float2(-1.0f, -1.0f); //convert to -1..1
		#if UNITY_UV_STARTS_AT_TOP
            o.vertex.xy = _RotateBlit ? (o.vertex.yx) : o.vertex.xy * float2(1,-1);
        #else
            o.vertex.xy = _RotateBlit ? o.vertex.yx : o.vertex.xy;
        #endif
			o.uv.xy = GetQuadTexCoord(vid)  * _BlitScaleBias.xy + _BlitScaleBias.zw;
			o.uv.xy = _RotateSample ? o.uv.yx : o.uv.xy;


			return o;
		}

		ENDHLSL

        Pass
        {
			Name "ResetBoundary"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            float4 frag (v2f i) : SV_Target
            {
				float4 col = float4(0,0,0,0);
                return col;
            }
			ENDHLSL
        }

        Pass
        {
			Name "StoreBoundary"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            float4 frag (v2f i) : SV_Target
            {
				float4 col = _MainTex.Sample(point_clamp_sampler, i.uv.xy);
                return col;
            }
			ENDHLSL
        }

        Pass
        {
			Name "CopyBoundary"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            float4 frag (v2f i) : SV_Target
            {
			#if UNITY_UV_STARTS_AT_TOP
				i.uv.y = _RotateSample ? 1-i.uv.y : i.uv.y;
			#endif
				float4 col = _MainTex.Sample(point_clamp_sampler, i.uv.xy);
                return col;
            }
			ENDHLSL
        }        
		
		Pass
        {
			Name "SetBoundary"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            float4 frag (v2f i) : SV_Target
            {
                return _BoundaryValue;
            }
			ENDHLSL
        }

    }
}
