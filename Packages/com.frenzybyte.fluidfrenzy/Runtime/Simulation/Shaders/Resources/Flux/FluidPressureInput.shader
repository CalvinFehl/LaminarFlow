Shader "Hidden/FluidFrenzy/PressureInput"
{
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

		v2f vert(uint vid : SV_VertexID)
		{
			v2f o;
			o.vertex = GetQuadVertexPosition(vid);
			o.vertex.xy = o.vertex.xy * float2(2.0f, -2.0f) + float2(-1.0f, 1.0f); //convert to -1..1
			o.uv.xy = GetQuadTexCoord(vid) * _BlitScaleBias.xy + _BlitScaleBias.zw;
			return o;
		}
		ENDHLSL

        Pass
        {
			Blend One One, One One
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

			float _Strength;
			float2 _PressureMinMax;

            float4 frag (v2f i) : SV_Target
            {
				float pressureSample = tex2D(_PressureField, i.uv.xy).r ;
				float pressureSign = sign(pressureSample);
				float pressure = smoothstep(_PressureMinMax.x, _PressureMinMax.y, abs(pressureSample))* pressureSign;
				//float pressure = smoothstep(0.01f, 0.1f, abs(pressureSample))* pressureSign;
				return float4(-pressure * _Strength, 0,0,0);
            }
            ENDHLSL
        }

			Pass
			{
				HLSLPROGRAM
				#pragma vertex vert
				#pragma fragment frag

				sampler2D _PreviousPressureField;
				sampler2D _PressureTex;

				float4 frag(v2f i) : SV_Target
				{
					float previousPressure = tex2D(_PreviousPressureField, i.uv.xy).r;
					float currentPressure = tex2D(_PressureField, i.uv.xy).r * VELOCITY_SCALE_LAYER1;
					float pressure = lerp(previousPressure, currentPressure, 0.1f);
					return float4(pressure, 0,0,0);
				}
				ENDHLSL
			}
    }
}
