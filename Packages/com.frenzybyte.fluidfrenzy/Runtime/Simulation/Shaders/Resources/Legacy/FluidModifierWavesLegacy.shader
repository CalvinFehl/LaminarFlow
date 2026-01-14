Shader "Hidden/FluidFrenzy/Legacy/Waves"
{
    Properties
    {
		_MainTex("Texture", 2D) = "white" {}
    }
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

			#include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Simulation/Shaders/Resources/FluidSimulationCommon.hlsl"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

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
				o.uv = GetQuadTexCoord(vid);
				return o;
			}

			struct Wave {
				float frequency;
				float amplitude;
				float speed;
				float2 direction;
			};

			#define DECLARE_WAVES(x)\
					float x##_frequency;\
					float x##_amplitude;\
					float x##_speed;\
					float2 x##_direction;

			#define CREATE_WAVES_STRUCT(x)\
				Wave x;\
				x.frequency = x##_frequency;\
				x.amplitude = x##_amplitude;\
				x.speed = x##_speed;\
				x.direction = x##_direction;



			sampler2D _MainTex;
			CBUFFER_START(FluidModifierWaves)
			DECLARE_WAVES(_BigWaves)
			DECLARE_WAVES(_SmallWaves)
			DECLARE_WAVES(_NoiseWaves)
			float4 _Time;
			CBUFFER_END

			float GetWaveCoord(float2 v, float2 d) {
				return v.x * d.x + v.y * d.y;
			}

			float SineWave(float2 v, Wave w) {

				if(dot(w.direction, w.direction) > 0)
				{
					float2 d = normalize(w.direction);
					float xy = GetWaveCoord(v, d);
					float t = _Time.y * w.speed;
					return  w.amplitude * sin(w.frequency * xy + t);
				}
				return 0;
			}

            float4 frag (v2f i) : SV_Target
            {
				CREATE_WAVES_STRUCT(_BigWaves);
				CREATE_WAVES_STRUCT(_SmallWaves);
				CREATE_WAVES_STRUCT(_NoiseWaves);

				float waveHeight = SineWave(i.uv, _BigWaves);
				Wave w = _BigWaves;
				w.direction.x = -w.direction.x;
				w.frequency *= 0.5;
				w.speed *= 0.5f;
				waveHeight += SineWave(i.uv, w);

				waveHeight += SineWave(i.uv, _SmallWaves);

				float n = tex2D(_MainTex, i.uv).x;
				waveHeight += SineWave(i.uv + n, _NoiseWaves);
				return waveHeight;

            }
            ENDHLSL
        }
    }
}
