#ifndef _FLUID_PARTICLES_EMITTER_HLSL_
#define _FLUID_PARTICLES_EMITTER_HLSL_


#include  "Packages/com.frenzybyte.fluidfrenzy/Runtime/Simulation/Shaders/Resources/FluidParticlesCommon.hlsl"

int xorshift(in int value) {
    // Xorshift*32
    // Based on George Marsaglia's work: http://www.jstatsoft.org/v08/i14/paper
    value ^= value << 13;
    value ^= value >> 17;
    value ^= value << 5;
    return value;
}

int nextInt(inout int seed) {
    seed = xorshift(seed);
    return seed;
}

float nextFloat(inout int seed) {
    seed = xorshift(seed);
    // FIXME: This should have been a seed mapped from MIN..MAX to 0..1 instead
    return abs(frac(float(seed) / 3141.592653));
}

float nextFloat(inout int seed, in float max) {
    return nextFloat(seed) * max;
}

// Advances the prng state and returns the corresponding random float.
float rand(inout uint state)
{
	return nextFloat(state);
}

float2 rand2(inout uint state)
{
	return float2(nextFloat(state), nextFloat(state));
}

float3 rand3(inout uint state)
{
	return float3(nextFloat(state), nextFloat(state), nextFloat(state));
}

struct ParticleEmitterDesc
{
	float4 minColor, maxColor;
	float4 minVelocity, maxVelocity;
	float4 minAcceleration, maxAcceleration;
	float4 minOffset, maxOffset;
	float minAngularVelocity, maxAngularVelocity;
	float minSize, maxSize;
	float minLife, maxLife;
};

#define DECLARE_EMITTER_DESC(x) \
	float4 x##_minColor, x##_maxColor;\
	float4 x##_minVelocity, x##_maxVelocity;\
	float4 x##_minAcceleration, x##_maxAcceleration;\
	float4 x##_minOffset, x##_maxOffset;\
	float x##_minAngularVelocity, x##_maxAngularVelocity;\
	float x##_minSize, x##_maxSize;\
	float x##_minLife, x##_maxLife;

#define CREATE_ENMITTER_STRUCT(x)\
	ParticleEmitterDesc x;\
	x.minColor = x##_minColor;\
	x.maxColor = x##_maxColor;\
	x.minVelocity = x##_minVelocity;\
	x.maxVelocity = x##_maxVelocity;\
	x.minAcceleration = x##_minAcceleration;\
	x.maxAcceleration = x##_maxAcceleration;\
	x.minOffset = x##_minOffset;\
	x.maxOffset = x##_maxOffset;\
	x.minAngularVelocity = x##_minAngularVelocity;\
	x.maxAngularVelocity = x##_maxAngularVelocity;\
	x.minSize = x##_minSize;\
	x.maxSize = x##_maxSize;\
	x.minLife = x##_minLife;\
	x.maxLife = x##_maxLife;


CBUFFER_START(ParticleEmitter0)
DECLARE_EMITTER_DESC(_ParticleEmitter0);
CBUFFER_END

CBUFFER_START(ParticleEmitter1)
DECLARE_EMITTER_DESC(_ParticleEmitter1);
CBUFFER_END

RWStructuredBuffer<int> _FreeIndices : register(u4);
RWStructuredBuffer<Particle> _ParticleBuffer : register(u5);
Texture2D<float4> _SimulationData;
unsigned int _ParticleCount;

#define FLUID_EMITPARTICLE(position)  CREATE_ENMITTER_STRUCT(_ParticleEmitter0); EmitParticle(_FreeIndices, _ParticleBuffer, _ParticleEmitter0, position)
#define FLUID_EMITPARTICLE_CUSTOM(position, velocity, rng) EmitParticleCustom(_FreeIndices, _ParticleBuffer, position, velocity, rng)

void EmitParticle(RWStructuredBuffer<int> freeIndices, RWStructuredBuffer<Particle> particleBuffer, ParticleEmitterDesc desc, float3 position)
{
	int freeIndex = 0;
	InterlockedAdd(freeIndices[0], -1, freeIndex);
	freeIndex--;
	if (freeIndex < 0)
	{
		InterlockedAdd(freeIndices[0], 1);
	}
	else
	{
		int r = position.x * position.z * position.y + 1000;
		float rnd = rand(r);

		int particleindex = freeIndices[freeIndex + 1];

		float3 offset = lerp(desc.minOffset.xyz, desc.maxOffset.xyz, rand3(r));

		float3 velocity = lerp(desc.minVelocity.xyz, desc.maxVelocity.xyz, rand3(r));
		float3 acceleration = lerp(desc.minAcceleration.xyz, desc.maxAcceleration.xyz, rand3(r));
		float4 color = lerp(desc.minColor, desc.maxColor, rnd);
		float rotation = lerp(-3.14f, 3.14f, rnd);
		float angularVelocity = lerp(desc.minAngularVelocity, desc.maxAngularVelocity, rnd);
		float size = lerp(desc.minSize, desc.maxSize, rnd);
		float life = lerp(desc.minLife, desc.maxLife, rnd);

		Particle p = (Particle)(0);
		p.position_size.xyz = position + offset;
		p.position_size.w = asfloat(PackHalf2x16(float2(size,size)));
		p.life_maxlife_color.x = PackHalf2x16(float2(life,life));
		p.life_maxlife_color.y = PackToR8G8B8A8(color);

		p.vel_accel_rot_angularvel.x = PackHalf2x16(velocity.xy);
		p.vel_accel_rot_angularvel.y = PackHalf2x16(float2(velocity.z, acceleration.x));
		p.vel_accel_rot_angularvel.z = PackHalf2x16(acceleration.yz);
		p.vel_accel_rot_angularvel.w = PackHalf2x16(float2(rotation, angularVelocity));

		particleBuffer[particleindex] = p;
	}
}

void EmitParticleCustom(RWStructuredBuffer<int> freeIndices, RWStructuredBuffer<Particle> particleBuffer, ParticleEmitterDesc desc, float3 position, float3 velocity, int rng)
{
	int freeIndex = 0;
	InterlockedAdd(freeIndices[0], -1, freeIndex);
	freeIndex--;
	if (freeIndex < 0)
	{
		InterlockedAdd(freeIndices[0], 1);
	}
	else
	{
		int r = (position.x * position.z * position.y) * 100 + 1000 + rng;
		float rnd = rand(r);
		float3 rnd3 = rand3(r);

		int particleindex = freeIndices[freeIndex + 1];

		float3 offset = lerp(desc.minOffset.xyz, desc.maxOffset.xyz, rnd3);
		velocity = velocity + lerp(desc.minVelocity.xyz, desc.maxVelocity.xyz, rnd3.yxz);
		float3 acceleration = lerp(desc.minAcceleration.xyz, desc.maxAcceleration.xyz, rnd3.xzy);
		float4 color = lerp(desc.minColor, desc.maxColor, rnd);
		float rotation = lerp(-3.14f, 3.14f, rnd);
		float angularVelocity = lerp(desc.minAngularVelocity, desc.maxAngularVelocity, rnd);;
		float size = lerp(desc.minSize, desc.maxSize, rnd);;
		float life = lerp(desc.minLife, desc.maxLife, rnd);

		Particle p = (Particle)(0);
		p.position_size.xyz = position + offset;
		p.position_size.w = asfloat(PackHalf2x16(float2(size,size)));
		p.life_maxlife_color.x = PackHalf2x16(float2(life,life));
		p.life_maxlife_color.y = PackToR8G8B8A8(color);

		p.vel_accel_rot_angularvel.x = PackHalf2x16(velocity.xy);
		p.vel_accel_rot_angularvel.y = PackHalf2x16(float2(velocity.z, acceleration.x));
		p.vel_accel_rot_angularvel.z = PackHalf2x16(acceleration.yz);
		p.vel_accel_rot_angularvel.w = PackHalf2x16(float2(rotation, angularVelocity));

		particleBuffer[particleindex] = p;
	}
}

#endif