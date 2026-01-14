Shader "Hidden/FluidFrenzy/PackHeightmap"
{

    SubShader
    {
        // No culling or depth

        Cull Off ZWrite Off ZTest Always

		HLSLINCLUDE

		#define USETEXTURE2D
		#include "FluidSimulationCommon.hlsl"

		struct v2f
		{
			float2 uv : TEXCOORD0;
			float4 vertex : SV_POSITION;
		};

		SamplerState linear_clamp_sampler;

		Texture2D<float4> _MainTex;
		float _HeightScale;
		float4 _HeightMapMask;

		v2f vert(uint vid : SV_VertexID)
		{
			v2f o;
			o.vertex = GetQuadVertexPosition(vid) * float4(_BlitScaleBiasRt.x, _BlitScaleBiasRt.y, 1, 1) + float4(_BlitScaleBiasRt.z, _BlitScaleBiasRt.w, 0, 0);;
			o.vertex.xy = o.vertex.xy * float2(2.0f, -2.0f) + float2(-1.0f, 1.0f); //convert to -1..1
			o.uv.xy = GetQuadTexCoord(vid) * _BlitScaleBias.xy + _BlitScaleBias.zw;;
			return o;
		}

		ENDHLSL

		Pass
		{
			Name "Copy"
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			float4 frag (v2f i) : SV_Target
			{
				float2 uv = i.uv.xy;
				float4 height = _MainTex.Sample(linear_clamp_sampler, uv);
				return dot(height.rgba / _HeightScale, _HeightMapMask);
			}
			ENDHLSL
		}
	}
}
