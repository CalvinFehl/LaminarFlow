#ifndef FLUIDFRENZY_PROCEDURAL_PARTICLE_SHADERGRAPH_INCLUDED
#define FLUIDFRENZY_PROCEDURAL_PARTICLE_SHADERGRAPH_INCLUDED

#include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Rendering/Shaders/Particles/ProceduralParticleCommon.hlsl"

/// Float

void SampleParticleData_float(in uint instanceID, out float3 position, out float2 size, out float rotation, out float angularVelocity, out float4 color, out float life, out float maxlife)
{
	SampleParticleData(instanceID, position, size, rotation, angularVelocity, color, life, maxlife);
}

void SampleParticleVertexData_float(in uint vertexID, in float4x4 worldToView, out float3 vertex, out float2 uv, out float3 normalOS, out float4 tangentOS)
{
	SampleParticleVertexData(vertexID, worldToView, vertex, uv, normalOS, tangentOS);
}

void TransformParticleToBillboard_float(in float3 position, in float4x4 viewToWorld, in float3 vertex, in float2 size, in float rotation, in float angularVelocity, out float3 positionOS)
{
	TransformParticleToBillboard(position, viewToWorld, vertex, size, rotation, angularVelocity, positionOS);
}

/// Half

void SampleParticleData_half(in uint instanceID, out half3 position, out half2 size, out half rotation, out half angularVelocity, out half4 color, out half life, out half maxlife)
{
	SampleParticleData(instanceID, position, size, rotation, angularVelocity, color, life, maxlife);
}

void SampleParticleVertexData_half(in uint vertexID, in half4x4 worldToView, out half3 vertex, out half2 uv, out half3 normalOS, out half4 tangentOS)
{
	SampleParticleVertexData(vertexID, worldToView, vertex, uv, normalOS, tangentOS);
}

void TransformParticleToBillboard_half(in half3 position, in half4x4 viewToWorld, in half3 vertex, in half2 size, in half rotation, in half angularVelocity, out half3 positionOS)
{
	TransformParticleToBillboard(position, viewToWorld, vertex, size, rotation, angularVelocity, positionOS);
}


#endif // FLUIDFRENZY_PROCEDURAL_PARTICLE_SHADERGRAPH_INCLUDED