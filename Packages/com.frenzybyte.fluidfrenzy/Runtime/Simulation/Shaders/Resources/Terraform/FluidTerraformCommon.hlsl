#ifndef _FLUID_TERRAFORM_COMMON_HLSL_
#define _FLUID_TERRAFORM_COMMON_HLSL_

#include  "Packages/com.frenzybyte.fluidfrenzy/Runtime/Simulation/Shaders/Resources/FluidSimulationCommon.hlsl"
#ifdef SUPPORT_GPUPARTICLES
#include  "Packages/com.frenzybyte.fluidfrenzy/Runtime/Simulation/Shaders/Resources/FluidParticlesEmitter.hlsl"
#endif

#if !_FLUID_UNITY_TERRAIN
#define TERRAFORM_TERRAIN
#endif

struct v2f
{
	float2 uv : TEXCOORD0;
	float4 vertex : SV_POSITION;
};

v2f vert(uint vid : SV_VertexID)
{
	v2f o;
	o.vertex = GetQuadVertexPosition(vid) * float4(_BlitScaleBiasRt.x, _BlitScaleBiasRt.y, 1, 1) + float4(_BlitScaleBiasRt.z, _BlitScaleBiasRt.w, 0, 0);
	o.vertex.xy = o.vertex.xy * float2(2.0f, -2.0f) + float2(-1.0f, 1.0f); //convert to -1..1
	o.uv = GetQuadTexCoord(vid) * _BlitScaleBias.xy + _BlitScaleBias.zw;
	return o;
}

struct fragout_mix
{
	float4 fluid : SV_Target0;
	float4 modify : SV_Target1;
#ifdef TERRAFORM_TERRAIN
	float4 terrain : SV_Target2;
	float4 splatmap : SV_Target3;
#endif
};

float _MixRate;
float _MixScale;
float _MixDepositRate;
float _ParticleEmissionRate;
float4 _TerrainDepositMask;
float4 _TerrainDepositSplatMask;

float4 _LiquifyLayerMask[4];

float4 _ReactionFactors_F1[4];
float4 _ReactionFactors_F2[4];

float4 _AddTerrainMask_F1[4];
float4 _AddTerrainMask_F2[4];

float2 _AddFluidMask_F1[4];
float2 _AddFluidMask_F2[4];

float4 _SetSplatMask_F1[4];
float4 _SetSplatMask_F2[4];

#define REACTION_RATE(x) x.r
#define TERRAIN_CONSUME(x) x.g
#define FLUID_CONSUME(x) x.b

sampler2D _LayerModify;
sampler2D _Splatmap;

fragout_mix fragMix(v2f i)
{
	fragout_mix o;

	float4 fluid = tex2D(_FluidHeightField, i.uv.xy);
	float4 modify = tex2D(_LayerModify, i.uv.xy);

#ifdef TERRAFORM_TERRAIN
	float4 terrain = tex2D(_TerrainHeightField, i.uv.xy);
	float4 splatmap = tex2D(_Splatmap, i.uv.xy);

	for(int k = 3; k >= 0; k--)
	{
		float terrainK = 0;
		if (k == 3) { terrainK = terrain.w; }
		else if (k == 2) { terrainK = terrain.z; }
		else if (k == 1) { terrainK = terrain.y; }
		else { terrainK = terrain.x; } // k == 0

#if _FLUID_LIQUIFY || _FLUID_CONTACT
		if(terrainK > 0.0001f)
		{

#if _FLUID_LIQUIFY
			float2 liquifyMask = _LiquifyLayerMask[k].xy;
			float liquifyRate = _LiquifyLayerMask[k].z;
			float liquifyAmount = _LiquifyLayerMask[k].w;
			terrainK -= liquifyRate;
			fluid.xy += liquifyRate * liquifyMask * liquifyAmount;
#endif

#if _FLUID_CONTACT
			// Calculate the rate of conversion Layer 1
			{
				// Mix dissolve/absorbe the layers coming into contact
				float minConversion = min(terrainK, fluid.x);
				float terrainConversion = REACTION_RATE(_ReactionFactors_F1[k]) * minConversion;
				terrainK -= TERRAIN_CONSUME(_ReactionFactors_F1[k]) * terrainConversion;
				fluid.x -= FLUID_CONSUME(_ReactionFactors_F1[k]) * terrainConversion;

				// Modify the terrain by removing
				terrain += _AddTerrainMask_F1[k] * terrainConversion;
				fluid.xy += _AddFluidMask_F1[k] * terrainConversion;

				// Modify the splatmap
				float4 splatmask = _SetSplatMask_F1[k];
				float shouldSplat = dot(splatmask, (1.0f).xxxx);
				splatmap += splatmask * terrainConversion * shouldSplat;
				splatmap -= (1-splatmask) * terrainConversion * shouldSplat;
			}

			// Calculate the rate of conversion Layer 2
			{
				// Mix dissolve/absorbe the layers coming into contact
				float minConversion = min(terrainK, fluid.y);
				float terrainConversion = REACTION_RATE(_ReactionFactors_F2[k]) * minConversion;
				terrainK -= TERRAIN_CONSUME(_ReactionFactors_F2[k]) * terrainConversion;
				fluid.y -= FLUID_CONSUME(_ReactionFactors_F2[k]) * terrainConversion;

				// Modify the terrain by removing
				terrain += _AddTerrainMask_F2[k] * terrainConversion;
				fluid.xy += _AddFluidMask_F2[k] * terrainConversion;

				// Modify the splatmap
				float4 splatmask = _SetSplatMask_F2[k];
				float shouldSplat = dot(splatmask, (1.0f).xxxx);
				splatmap += splatmask * terrainConversion * shouldSplat;
				splatmap -= (1-splatmask) * terrainConversion * shouldSplat;
			}
			if (k == 3) { terrain.w = terrainK; }
			else if (k == 2) { terrain.z = terrainK; }
			else if (k == 1) { terrain.y = terrainK; }
			else { terrain.x = terrainK; } // k == 0
			fluid = max(0, fluid);
#endif
			break;
		}
		else
		{
			if (k == 3) { terrain.w = 0; }
			else if (k == 2) { terrain.z = 0; }
			else if (k == 1) { terrain.y = 0; }
			else { terrain.x = 0; } // k == 0
		}
#endif
	}

	terrain = max(0, terrain);
#else
	float4 terrain = float4(SampleTerrainHeight(i.uv.xy) , 0,0,0);
#endif

#if _FLUID_MIX
	float maxMixAmount = min(fluid.x, fluid.y);
	float mixAmount = min(maxMixAmount, _MixRate) * _MixScale;
	fluid.xy = max(0, fluid.xy - mixAmount);

#ifdef SUPPORT_GPUPARTICLES
	if (mixAmount > 0.01f && modify.y == 0)
	{
		float3 position = SimulationUVToLocalPosition(i.uv);
		position.y = dot(terrain, (1.0f).xxxx) + max(fluid.x, fluid.y);
		FLUID_EMITPARTICLE(position);

		int r = (int)(i.uv.x * 1000 + i.uv.y * 1000);
		float rnd = rand(r);
		modify.y = lerp(_ParticleEmissionRate * 0.5f, _ParticleEmissionRate * 1.5f, rnd);
	}
#endif
#endif

#ifdef TERRAFORM_TERRAIN
#if _FLUID_MIX
	modify.x += mixAmount;
	splatmap += _TerrainDepositSplatMask * mixAmount;
	splatmap -= (1-_TerrainDepositSplatMask) * mixAmount;
#endif

	o.terrain = terrain;
	o.splatmap = saturate(splatmap);
#endif

	o.modify = modify;
	o.fluid = fluid;
	return o;
}

struct fragout_applylayer
{
	float4 modify : SV_Target0;
#if _FLUID_MIX
#ifdef TERRAFORM_TERRAIN
	float4 terrain : SV_Target1;
#endif
#endif
};



fragout_applylayer fragApplyLayer(v2f i)
{
	fragout_applylayer o;

	float4 modify = tex2D(_LayerModify, i.uv.xy);
	float amount = modify.x * _MixDepositRate;

#if _FLUID_MIX
#ifdef TERRAFORM_TERRAIN
	float4 terrain = tex2D(_TerrainHeightField, i.uv.xy);
	// Deposit the mixed fluid to the selected layers
	terrain += _TerrainDepositMask * amount;
	o.terrain = terrain;

	modify.x -= amount;
	if(modify.x < 0.00001f) modify.x = 0;
#endif
#endif

	modify.y -= _FluidSimDeltaTime;
	modify = max(modify,0);
	o.modify = modify;

	return o;
}

#endif //_FLUID_TERRAFORM_COMMON_HLSL_