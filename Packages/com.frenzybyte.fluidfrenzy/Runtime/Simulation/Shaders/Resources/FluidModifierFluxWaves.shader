Shader "Hidden/FluidFrenzy/Flux/Waves"
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

			/*struct Wave {
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

				*/

			sampler2D _MainTex;
			//CBUFFER_START(FluidModifierWaves)
			//DECLARE_WAVES(_BigWaves)
			//DECLARE_WAVES(_SmallWaves)
			//DECLARE_WAVES(_NoiseWaves)
			float4 _Time;
			//CBUFFER_END



			struct Wave {
				float2 direction;
				float frequency;
				float amplitude;
				float phase;
			};
			#define DEG2RAD (360 / PI)
			Wave CreateWave(float wavelength, float amplitude, float speed, float direction, int waveCount)
			{
				Wave w = (Wave)(0);
			    w.frequency = 2.0f / wavelength;
				w.amplitude = amplitude;
				w.phase = speed * sqrt(9.8f * 2.0f * PI / wavelength);;

				w.direction = float2(cos(DEG2RAD * direction), sin(DEG2RAD * direction));
				w.direction = normalize(w.direction);
				return w;
			}


			float2 GetDirection(float3 v, Wave w) {
				return w.direction;
			}

			float GetWaveCoord(float3 v, float2 d, Wave w) {
				return v.x * d.x + v.z * d.y;
			}

			float GetTime(Wave w) {
				return _Time.y * w.phase;
			}

			float3 Gerstner(float3 v, Wave w) {
				float2 d = GetDirection(v, w);
				float xz = GetWaveCoord(v, d, w);
				float t = GetTime(w);

				float3 g = float3(0.0f, 0.0f, 0.0f);
				g.x = w.amplitude * d.x * cos(w.frequency * xz + t);
				g.z = w.amplitude * d.y * cos(w.frequency * xz + t);
				g.y = w.amplitude * sin(w.frequency * xz + t);
				
				return g;
			}

			float3 GerstnerWave (float3 p,
				Wave w
			) {
				float steepness = w.amplitude;
				float wavelength = w.frequency;
				float k = 2 * PI / wavelength;
				float c = sqrt(9.8 / k);
				float2 d = normalize(w.direction);
				float f = k * (dot(d, p.xz) - c * _Time.y);
				float a = w.amplitude;
			

				return float3(
					d.x * (a * cos(f)),
					a * sin(f),
					d.y * (a * cos(f))
				);
			}


            float4 frag (v2f i) : SV_Target
            {
				float waveHeight = 0;

				float2 test = i.uv;
				float n = tex2D(_MainTex, i.uv).x;
				float medianWavelength = 1.56  ;
				float wavelengthRange = 1;

				float medianDirection = 172;
				float directionalRange = 360;

				float medianAmplitude = 0.07;
				float medianSpeed = 0.12;
				float speedRange = 1;

				float wavelengthMin = medianWavelength / (1.0f + wavelengthRange);
				float wavelengthMax = medianWavelength * (1.0f + wavelengthRange);
				float directionMin = medianDirection - directionalRange;
				float directionMax = medianDirection + directionalRange;
				float speedMin = max(0.01f, medianSpeed - speedRange);
				float speedMax = medianSpeed + speedRange;
				float ampOverLen = medianAmplitude / medianWavelength;

				float3 h = 0;
				const int waveCount = 4;

				for (int wi = 0; wi < waveCount; ++wi) 
				{
					float l = (float)wi / waveCount;
					float wavelength = lerp(wavelengthMin, wavelengthMax, l);
					float direction = lerp(directionMin, directionMax, l);
					float amplitude = wavelength * ampOverLen;
					float speed = lerp(speedMin, speedMax, l);

					Wave wave = CreateWave(wavelength, amplitude, speed, direction, waveCount);

					h += Gerstner(float3(test.x+ 0.25 * l, 0, test.y + 0.25 * l) * 10 + n , wave);
				}

				return h.xzxz;

            }
            ENDHLSL
        }
    }
}
