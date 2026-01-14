Shader "Hidden/FluidFrenzy/UpdateFoam"
{
    Properties
    {
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

			#pragma multi_compile_fragment _ _APPLY_PRESSURE_FOAM 
			#pragma multi_compile_fragment _ _APPLY_WAVE_FOAM 
			#pragma multi_compile_fragment _ _APPLY_SHALLOW_FOAM
			#pragma multi_compile_fragment _ _APPLY_TURBULENCE_FOAM

			#include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Simulation/Shaders/Resources/FluidSimulationCommon.hlsl"
			#include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Simulation/Shaders/Resources/Flow/FluidFlowCommon.hlsl"
						
			sampler2D _MainTex;
			sampler2D _WorldNormal;
			sampler2D _PressureTex;

			CBUFFER_START(WaterSimUpdateFoam)
			float4 _FoamFadeValues;
			float4 _FoamValues;
			float2 _FoamWaveSmoothStep;
			float _FoamTurbulenceAmount;
			float _FoamDivergenceThreshold;

			float2 _FoamPressureSmoothStep;

			float2 _FoamShallowVelocitySmoothStep;
			float _FoamShallowDepth;

			float4 _MainTex_TexelSize;
			float4 _VelocityBlitScaleBias;
			float4 _NormalBlitScaleBias;
			float2 _AdvectScale;
			CBUFFER_END

            struct v2f
            {
                float2 uv0 : TEXCOORD0;
                float4 uv12 : TEXCOORD1;
                float4 vertex : SV_POSITION;
            };


			v2f vert(uint vid : SV_VertexID)
			{
				v2f o;
				o.vertex = GetQuadVertexPosition(vid);
				o.vertex.xy = o.vertex.xy * float2(2.0f, -2.0f) + float2(-1.0f, 1.0f); //convert to -1..1
				float2 uv = GetQuadTexCoord(vid);
				o.uv0.xy = uv;
				o.uv12.xy = uv * _VelocityBlitScaleBias.xy + _VelocityBlitScaleBias.zw;
				o.uv12.zw = uv * _NormalBlitScaleBias.xy + _NormalBlitScaleBias.zw;
				return o;
			}


            float4 frag (v2f i) : SV_Target
            {
				float2 velocityUV = i.uv12.xy;
				float2 normalUV = i.uv12.zw;

				float2 velocityC = SampleVelocity(velocityUV, float2( 0, 0)).xy;
				float2 pos = (i.uv0.xy - velocityC.xy * _AdvectScale);

				float4 normal = tex2D(_WorldNormal, normalUV).rgba;
				if(normal.w <= 0)
					return 0;
				float4 fluidHeight = tex2D(_FluidHeightField, normalUV).rgba;
				float4 prevFluidHeight = tex2D(_PreviousFluidHeightField, normalUV).rgba;

#if OPEN_BORDER
				if (pos.x < 0 || pos.y < 0 || pos.x > (1.0f) || pos.y > (1.0f))
					pos = i.uv0.xy;
#endif
				float depth = fluidHeight.y;
				float foam = tex2D(_MainTex, pos).r;
				foam *= _FoamFadeValues.x;
				foam -= _FoamFadeValues.y;
				foam = max(0,foam);

#if _APPLY_PRESSURE_FOAM
				float pressureSample = saturate(abs(tex2D(_PressureField, velocityUV).r) * VELOCITY_SCALE_LAYER1);
				float pressure = pressureSample * pressureSample * 150;
				float pressureFoam = smoothstep(_FoamPressureSmoothStep.x, _FoamPressureSmoothStep.y, pressure) * _FoamValues.x;
				foam += pressureFoam;
#endif


#if _APPLY_WAVE_FOAM
				float tiltAngle = normal.y;
				float hVelocityFoam = (prevFluidHeight.y - fluidHeight.y) *_FoamValues.z;
				float angleFoam = smoothstep(_FoamWaveSmoothStep.x, _FoamWaveSmoothStep.y,tiltAngle) * _FoamValues.y;
				foam += angleFoam;
				foam += max(0, hVelocityFoam);
#endif

#if _APPLY_TURBULENCE_FOAM
				float2 velocityL = SampleVelocity(velocityUV, -float2(1, 0)).xy;
				float2 velocityB = SampleVelocity(velocityUV,  float2(0, 1)).xy;

				float divergence = abs((velocityC.x - velocityL.x) + (velocityC.y - velocityB.y)) * 2;
				foam += divergence > _FoamDivergenceThreshold ? divergence * _FoamTurbulenceAmount : 0;
#endif

#if _APPLY_SHALLOW_FOAM
				float speed = length(velocityC);
				float shallowScale = saturate(1 - (depth - _FoamShallowDepth));
				float velocityFoam = smoothstep(_FoamShallowVelocitySmoothStep.x, _FoamShallowVelocitySmoothStep.y, speed) * _FoamValues.w;
				foam += velocityFoam * shallowScale  ;
#endif

				// Since GLES is s_float not unorm we need to saturate.
#if SHADER_API_GLES3
				foam = saturate(foam);
#endif
				return float4(foam, 0,0,0);
            }
            ENDHLSL
        }
    }
}
