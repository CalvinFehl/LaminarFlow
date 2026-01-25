#ifndef FLUIDFRENZY_PROCEDURAL_PARTICLE_COMMON_INCLUDED
#define FLUIDFRENZY_PROCEDURAL_PARTICLE_COMMON_INCLUDED

#include  "Packages/com.frenzybyte.fluidfrenzy/Runtime/Simulation/Shaders/Resources/FluidParticlesCommon.hlsl"

StructuredBuffer<Particle> _ParticleBuffer;
StructuredBuffer<uint> _DrawIndices;
float4x4 _ParticleSystemObjectToWorld;

struct VaryingsParticle
{
    float4 positionCS               : SV_POSITION;
    float2 texcoord                 : TEXCOORD0;
    half4 color                     : COLOR;

    #if !defined(PARTICLES_EDITOR_META_PASS)
        float4 positionWS           : TEXCOORD1;

		#if !defined(_BILLBOARDMODE_CAMERA_NORMAL_UP) && !defined(_BILLBOARDMODE_UP)
			#if _NORMALMAP
				float3 tangentWS           : TEXCOORD2;
				float3 bitangentWS           : TEXCOORD3;
			#endif
			float3 normalWS           : TEXCOORD4;
		#endif
        half3 viewDirWS        : TEXCOORD5;

        #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
            float4 shadowCoord      : TEXCOORD7;
        #endif

        half3 vertexSH             : TEXCOORD8; // SH
    #endif

    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

float4 GetTriangleQuadVertexPosition(uint vertexID, float z = UNITY_NEAR_CLIP_VALUE)
{
	float3 vertices[4] = {
		// Positions
		 float3(0.0f,  0.0f, 0.0f), // Bottom-left	
		 float3(1.0f,  0.0f, 0.0f), // Bottom-right
		 float3(0.0f,  1.0f, 0.0f), // Top-left
		 float3(1.0f,  1.0f, 0.0f)  // Top-right
	};

	int indices[] = {
		0, 1, 2, // First triangle
		1, 3, 2  // Second triangle
	};

    // note: the triangle vertex position coordinates are x2 so the returned UV coordinates are in range -1, 1 on the screen.
    float2 uv = vertices[indices[vertexID]].xy;//float2((vertexID << 1) & 2, vertexID & 2);
    float4 pos = float4(uv, z, 1.0);
    return pos;
}


float2 GetTriangleQuadTexCoord(uint vertexID)
{
	float3 vertices[4] = {
		// Positions
		 float3(0.0f,  0.0f, 0.0f), // Bottom-left	
		 float3(1.0f,  0.0f, 0.0f), // Bottom-right
		 float3(0.0f,  1.0f, 0.0f), // Top-left
		 float3(1.0f,  1.0f, 0.0f)  // Top-right
	};

	int indices[] = {
		0, 1, 2, // First triangle
		1, 3, 2  // Second triangle
	};

    // note: the triangle vertex position coordinates are x2 so the returned UV coordinates are in range -1, 1 on the screen.
    float2 uv = vertices[indices[vertexID]].xy;//float2((vertexID << 1) & 2, vertexID & 2);
#if UNITY_UV_STARTS_AT_TOP
	uv.y = 1.0 - uv.y;
#endif
    return uv;
}

float3 RotatePointZ(float3 p, float angle)
{
	float cosAngle = cos(angle);
	float sinAngle = sin(angle);

	float x = p.x * cosAngle - p.y * sinAngle;
	float y = p.x * sinAngle + p.y * cosAngle;
	float z = p.z;

	return float3(x, y, z);
}

float PingPong(float value, float length)
{
	float t = fmod(value, length * 2.0);
	return length - abs(t - length);
}

void SampleParticleData(in uint instanceID, out float3 position, out float2 size, out float rotation, out float angularVelocity, out float4 color, out float life, out float maxlife)
{
	uint particleID = _DrawIndices[instanceID];
	Particle particle = _ParticleBuffer[particleID];

	float2 rotation_angularvelocity = UnpackHalf2x16(particle.vel_accel_rot_angularvel.w);
	color = UnpackFromR8G8B8A8(particle.life_maxlife_color.y);
	float2 life_maxlife = UnpackHalf2x16(particle.life_maxlife_color.x);

	position = particle.position_size.xyz;
	size = UnpackHalf2x16(asuint(particle.position_size.w));

	rotation = rotation_angularvelocity.x;
	angularVelocity = rotation_angularvelocity.y;
	life = life_maxlife.x;
	maxlife = life_maxlife.y;
}

void SampleParticleVertexData(in uint vertexID, in float4x4 worldToView, out float3 vertex, out float2 texcoord, out float3 normalOS, out float4 tangentOS)
{
	vertex = GetTriangleQuadVertexPosition(vertexID % 6).xyz;
	vertex.xy = vertex.xy * 2 - 1;

	#if !defined(_BILLBOARDMODE_CAMERA_NORMAL_UP) && !defined(_BILLBOARDMODE_UP)
		normalOS = worldToView[2].xyz;
		tangentOS = float4(worldToView[0].xyz, 1);
	#else
		normalOS = float3(0, 1, 0);
		tangentOS = float4(1, 0, 0, 1);
	#endif
	texcoord = GetTriangleQuadTexCoord(vertexID % 6);
}

void TransformParticleToBillboard(in float3 position, in float4x4 viewToWorld, in float3 vertex, in float2 size, in float rotation, in float angularVelocity, out float3 positionOS)
{
	vertex = vertex * float3(size,0);
	vertex = RotatePointZ(vertex, rotation + angularVelocity * _Time.y);
	#if _BILLBOARDMODE_UP
		positionOS = position + vertex.xzy;
	#else
		positionOS = position + mul((float3x3)(viewToWorld), vertex);
	#endif

	positionOS = mul(_ParticleSystemObjectToWorld, float4(positionOS,1)).xyz;
}

#endif // FLUIDFRENZY_PROCEDURAL_PARTICLE_COMMON_INCLUDED