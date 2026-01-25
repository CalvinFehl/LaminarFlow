Shader "Hidden/FluidFrenzy/Simulation/FluidParticles" 
{ 
	SubShader
	{
		Cull Off ZWrite Off ZTest Always

		HLSLINCLUDE

		#include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Simulation/Shaders/Resources/FluidSimulationCommon.hlsl"
		#include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Simulation/Shaders/Resources/Flow/FluidFlowCommon.hlsl"
		#include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Simulation/Shaders/Resources/FluidParticlesEmitter.hlsl"

		RWStructuredBuffer<int> _SplashFreeIndices : register(u3);
		RWStructuredBuffer<Particle> _SplashParticleBuffer : register(u4);

		RWStructuredBuffer<int> _FoamFreeIndices : register(u5);
		RWStructuredBuffer<Particle> _FoamParticleBuffer : register(u6);

		// Output vertex structure
		struct v2f {
			float2 uv : TEXCOORD0;
			float4 vertex : SV_POSITION;
		};

		v2f vert(uint vid : SV_VertexID)
		{
			v2f o;
			o.vertex = GetQuadVertexPosition(vid);// * float4(_BlitScaleBiasRt.x, _BlitScaleBiasRt.y, 1, 1) + float4(_BlitScaleBiasRt.z, _BlitScaleBiasRt.w, 0, 0);
			o.vertex.xy = o.vertex.xy * float2(2.0f, -2.0f) + float2(-1.0f, 1.0f); //convert to -1..1
			o.uv = GetQuadTexCoord(vid);// * _BlitScaleBias.xy + _BlitScaleBias.zw;
			return o;
		}

		ENDHLSL

		Pass
		{
			Name "SplashAndSpray"
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 5.0
			#pragma multi_compile_local _ FLUID_MULTILAYER
			#pragma multi_compile_local _ _FLUID_UNITY_TERRAIN

			#pragma multi_compile_local _ FLUID_SPLASH_BREAKINGWAVE
			#pragma multi_compile_local _ FLUID_SPLASH_TURBULENCE
			#pragma multi_compile_local _ FLUID_SURFACE_TURBULENCE

			uint _DivergenceGridLimit;
			uint2 _DivergenceStagger;			
			
			uint _BreakingWavesGridLimit;
			uint2 _BreakingWavesStagger;
			uint _SplashDivergenceGridLimit;
			uint2 _SplashDivergenceStagger;
			float _SprayDivergenceThreshold;

			float3 _SplashOffsetRange;
			float _SurfaceDivergenceThreshold;

			float _SteepnessThreshold;
			float _RiseRateThreshold;
			float _WaveLengthThreshold;

			float3 _SurfaceOffsetRange;

			float2 _HeightAvgMax;

			uint _FrameCount;

			float maxabs(float s, float t)
			{
				return (abs(s) > abs(t)) ? s : t;
			}

			// Fragment shader
			float4 frag(v2f i) : SV_Target {

				uint2 gridPos = round(i.uv.xy * _FluidHeightField_TexelSize.zw);

				//2.1.5. Stability Enhancements
				float2 fluidHeightC = SampleFluidHeight(i.uv, float2(0, 0));
				float2 fluidHeightR = SampleFluidHeight(i.uv, float2(1, 0));
				float2 fluidHeightT = SampleFluidHeight(i.uv, float2(0, 1));				
				float2 fluidHeightL = SampleFluidHeight(i.uv, float2(-1, 0));
				float2 fluidHeightB = SampleFluidHeight(i.uv, float2(0, -1));

				float2 prevFluidHeightC = SamplePreviousFluidHeight(i.uv, float2(0, 0));

				float terrainHeightC = SampleTerrainHeight(i.uv, float2(0, 0));
				float terrainHeightR = SampleTerrainHeight(i.uv, float2(1, 0));
				float terrainHeightT = SampleTerrainHeight(i.uv, float2(0, 1));
				float terrainHeightL = SampleTerrainHeight(i.uv, float2(-1, 0));
				float terrainHeightB = SampleTerrainHeight(i.uv, float2(0, -1));

				float totalHeightC = terrainHeightC + fluidHeightC.x;
				float totalHeightR = terrainHeightR + fluidHeightR.x;
				float totalHeightL = terrainHeightL + fluidHeightL.x;
				float totalHeightT = terrainHeightT + fluidHeightT.x;
				float totalHeightB = terrainHeightB + fluidHeightB.x;

				float2 delta_eta = 0;
				delta_eta.x = maxabs(totalHeightR - totalHeightC, totalHeightC - totalHeightL) / WORLD_CELLSIZE_LAYER1.x;
				delta_eta.y = maxabs(totalHeightT - totalHeightC, totalHeightC - totalHeightB) / WORLD_CELLSIZE_LAYER1.y;

				float alpha_minSplash = _SteepnessThreshold;
				float vmin_Splash = _RiseRateThreshold;
				float lmin_Splash = _WaveLengthThreshold;
				float steep_factor = alpha_minSplash;// * ((ACCELERATION_LAYER1 * _FluidSimDeltaTime) / WORLD_CELLSIZE_LAYER1.x);

				float2 delta_depth = (fluidHeightC - max(prevFluidHeightC,0)) / _FluidSimDeltaTime;
				float2 delta_height = dot((1.0f).xxxx, float4(totalHeightR, totalHeightL, totalHeightT, totalHeightB)) - (totalHeightC * 4);
				delta_height = delta_height / (WORLD_CELLSIZE_LAYER1.x * WORLD_CELLSIZE_LAYER1.y);

				float2 maxVelocity = ((-delta_eta * sqrt(ACCELERATION_LAYER1 * min(fluidHeightC.xx, _HeightAvgMax.xx))) / length(delta_eta));

				float2 velocityC = SampleVelocity(i.uv, float2(0,0)).xy;
				float2 velocityL = SampleVelocity(i.uv, -float2(1,0)).xy;
				float2 velocityB = SampleVelocity(i.uv, -float2(0,1)).xy;

				float2 velocity = float2(velocityL.x, velocityB.y);
				float divergence = abs((velocityC.x - velocityL.x) + (velocityB.y - velocityC.y)) * 2;
				float hvelscale = 1 + 1 * ACCELERATION_LAYER1 * (fluidHeightC.x - prevFluidHeightC.x);

				velocity *= _VelocityScale.xy * hvelscale;
				velocity = clamp(velocity, -abs(maxVelocity / CELLSIZE_SCALE_LAYER1),abs(maxVelocity / CELLSIZE_SCALE_LAYER1));
				bool is_steep = length(delta_eta) > steep_factor;
				bool is_rising = delta_depth.x > vmin_Splash;
				bool is_top = delta_height.x < lmin_Splash;

				bool should_splash = false;
				#if FLUID_SPLASH_BREAKINGWAVE
					should_splash = is_steep && is_rising && is_top;
					should_splash = should_splash && (gridPos.x % _BreakingWavesGridLimit) == _BreakingWavesStagger.x && (gridPos.y % _BreakingWavesGridLimit) == _BreakingWavesStagger.y;
				#endif

				bool divergence_splash = false;
				#if FLUID_SPLASH_TURBULENCE
					divergence_splash = (divergence > _SprayDivergenceThreshold && fluidHeightC.x > 0.01f) ;
					divergence_splash = divergence_splash && (gridPos.x % _SplashDivergenceGridLimit) == _SplashDivergenceStagger.x && (gridPos.y % _SplashDivergenceGridLimit) == _SplashDivergenceStagger.y;
				#endif

				bool divergence_surface = false;
				#if FLUID_SURFACE_TURBULENCE
					divergence_surface = (divergence > _SurfaceDivergenceThreshold && fluidHeightC.x > 0.01f);
					divergence_surface = divergence_surface && (gridPos.x % _DivergenceGridLimit) == _DivergenceStagger.x && (gridPos.y % _DivergenceGridLimit) == _DivergenceStagger.y;
				#endif

				float3 position = SimulationUVToLocalPosition(i.uv) + float3(0, totalHeightC.x, 0) + float3(normalize(velocity),0).xzy * WORLD_CELLSIZE_LAYER1.xxy;

				float2 uv = i.uv.xy * _HeightFieldRcp;
				if(should_splash || divergence_splash)
				{	
					float divergenceVerticalVelocity = min(divergence * 2,2);
					float waveVelocity = min(delta_depth.x * 0.1, maxVelocity.x);
					float heightVelocity = should_splash ? waveVelocity : divergenceVerticalVelocity ;
					float3 particleVelocity = float3(velocity.x,velocity.y,heightVelocity).xzy;

					if(is_steep)
					{
						CREATE_ENMITTER_STRUCT(_ParticleEmitter0);
						_ParticleEmitter0.minOffset += float4(-_SplashOffsetRange, 0); 
						_ParticleEmitter0.maxOffset += float4( _SplashOffsetRange, 0); 
						EmitParticleCustom(_SplashFreeIndices, _SplashParticleBuffer, _ParticleEmitter0, position, particleVelocity, _FrameCount * 10);
					}
				}
				if(divergence_surface || should_splash)
				{
					CREATE_ENMITTER_STRUCT(_ParticleEmitter1);
					_ParticleEmitter1.minOffset += float4(-_SurfaceOffsetRange, 0); 
					_ParticleEmitter1.maxOffset += float4( _SurfaceOffsetRange, 0); 
					EmitParticleCustom(_FoamFreeIndices, _FoamParticleBuffer, _ParticleEmitter1, position, 0, _FrameCount* 10);
				}

				return float4(divergence, is_rising, divergence_splash,divergence_surface);
			}
			ENDHLSL
		}
	}
}
