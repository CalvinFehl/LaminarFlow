Shader "Hidden/FluidFrenzy/Waves"
{
    Properties
    {
		_MainTex("Texture", 2D) = "white" {}
    }
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

		HLSLINCLUDE
		
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
			float phase;
			float2 direction;
		};

		Wave CreateWave(float frequency, float amplitude, float phase, float2 direction)
		{
			Wave w = (Wave)(0);
			w.frequency = frequency;
			w.amplitude = amplitude;
			w.phase = phase;
			w.direction = direction;
			return w;
		}

		#define DEG2RAD (360 / PI)
		Wave ConstructWave(float wavelength, float amplitude, float speed, float direction)
		{
			Wave w = (Wave)(0);
			w.frequency = 2.0f / wavelength;
			w.amplitude = amplitude;
			w.phase = speed * sqrt(9.8f * 2.0f * PI / wavelength);;

			w.direction = float2(cos(DEG2RAD * direction), sin(DEG2RAD * direction));
			w.direction = normalize(w.direction);
			return w;
		}

		#define CREATE_WAVES_STRUCT(x) CreateWave(_Wave_frequency[x], _Wave_amplitude[x], _Wave_phase[x], _Wave_direction[x].xy)

		sampler2D _MainTex;
		CBUFFER_START(FluidModifierWaves)
		float _Wave_frequency[4];
		float _Wave_amplitude[4];
		float _Wave_phase[4];
		float4 _Wave_direction[4]; 
		float _NoiseAmplitude;
		float4 _Time;
		CBUFFER_END


		float3 Gerstner(float2 v, Wave w) {
			float2 d = w.direction;
			float xz = dot(v,d);
			float t = _Time.y * w.phase;

			float3 g = float3(0.0f, 0.0f, 0.0f);
			g.x = w.amplitude * d.x * cos(w.frequency * xz + t);
			g.z = w.amplitude * d.y * cos(w.frequency * xz + t);
			g.y = w.amplitude * sin(w.frequency * xz + t);
				
			return g;
		}

		ENDHLSL

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag


            float4 frag (v2f i) : SV_Target
            {
				Wave waves[4] = {
					CREATE_WAVES_STRUCT(0),
					CREATE_WAVES_STRUCT(1),
					CREATE_WAVES_STRUCT(2),
					CREATE_WAVES_STRUCT(3)
				};

				float waveHeight = 0;

				float n = tex2D(_MainTex, i.uv * 0.1 ).x;
				float noiseSmall = tex2D(_MainTex, i.uv).x;

				float3 h = 0;
				const int waveCount = 4;

				for (int wi = 0; wi < 4; ++wi) 
				{
					float l = (float)wi / waveCount;
					float2 p = i.uv+ n * 0.2 * l;
					h += Gerstner(p, waves[wi]);
				}

				Wave noiseWaves = ConstructWave(0.02f, _NoiseAmplitude * 0.01f, 0.1, 1);
				h += Gerstner(i.uv + noiseSmall + _Time.yy * 0.01f, noiseWaves);
				return h.yyyy;

            }
            ENDHLSL
        }

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag


            float4 frag (v2f i) : SV_Target
            {
				Wave waves[4] = {
					CREATE_WAVES_STRUCT(0),
					CREATE_WAVES_STRUCT(1),
					CREATE_WAVES_STRUCT(2),
					CREATE_WAVES_STRUCT(3)
				};

				float waveHeight = 0;

				float n = tex2D(_MainTex, i.uv * 0.1 ).x;
				float noiseSmall = tex2D(_MainTex, i.uv ).x;

				float3 h = 0;
				const int waveCount = 4;

				for (int wi = 0; wi < 4; ++wi) 
				{
					float l = (float)wi / waveCount;
					float2 p = i.uv+ n * 0.2 * l;
					h += Gerstner(p, waves[wi]);
				}
				
				Wave noiseWaves = ConstructWave(0.02f, _NoiseAmplitude, 0.1f ,1.0);
				h += Gerstner(i.uv + noiseSmall * 0.05f, noiseWaves).yyy;

				return h.xzxz ;

            }
            ENDHLSL
        }
    }
}
