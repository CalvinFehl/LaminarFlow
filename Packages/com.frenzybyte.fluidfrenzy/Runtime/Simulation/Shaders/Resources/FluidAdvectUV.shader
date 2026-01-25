Shader "Hidden/FluidFrenzy/AdvectUV"
{
    Properties
    {

    }
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

		HLSLINCLUDE
		
		#include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Simulation/Shaders/Resources/FluidSimulationCommon.hlsl"

		struct v2f
		{
			float2 uv : TEXCOORD0;
			float4 vertex : SV_POSITION;
		};

		sampler2D _MainTex;

		CBUFFER_START(WaterSimResetUV)
		float2 _FlowSpeed;
		float2 _UVTextureSize;
		float4 _VelocityBlitScaleBias;
		CBUFFER_END

			v2f vert(uint vid : SV_VertexID)
		{
			v2f o;
			o.vertex = GetQuadVertexPosition(vid);
			o.vertex.xy = o.vertex.xy * float2(2.0f, -2.0f) + float2(-1.0f, 1.0f); //convert to -1..1
			o.uv.xy = GetQuadTexCoord(vid);
			return o;
		}

		ENDHLSL

        Pass
        {
			ColorMask RG
			HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            float4 frag (v2f i) : SV_Target
            {
				return float4(0, 0, 0, 0);
            }
			ENDHLSL
        }

		Pass
		{
			ColorMask BA
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			float4 frag(v2f i) : SV_Target
			{
				return float4(0, 0, 0, 0);
			}
			ENDHLSL
		}

		Pass
		{
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			float4 frag(v2f i, float4 screenPos : SV_POSITION) : SV_Target
			{
				screenPos.xy *= _UVTextureSize.xy;
				float4 uv = tex2D(_MainTex, i.uv)  + screenPos.xyxy;
				float4 velocityUV = uv.xyzw * _VelocityBlitScaleBias.xyxy + _VelocityBlitScaleBias.zwzw;
				float2 vel0 = tex2D(_VelocityField, velocityUV.xy).xy;
				float2 vel1 = tex2D(_VelocityField, velocityUV.zw).xy;
				return ((uv - float4(vel0, vel1) * _FlowSpeed.xyxy) - screenPos.xyxy) ;
			}
			ENDHLSL
		}
    }
}
