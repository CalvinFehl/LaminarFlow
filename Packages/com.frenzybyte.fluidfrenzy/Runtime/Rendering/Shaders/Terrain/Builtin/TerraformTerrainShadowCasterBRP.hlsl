#ifndef FLUIDFRENZY_TERRAFORM_TERRAIN_SHADOWCASTER_BRP_INCLUDED
#define FLUIDFRENZY_TERRAFORM_TERRAIN_SHADOWCASTER_BRP_INCLUDED

#if (defined(_ALPHABLEND_ON) || defined(_ALPHAPREMULTIPLY_ON)) && defined(UNITY_USE_DITHER_MASK_FOR_ALPHABLENDED_SHADOWS)
    #define UNITY_STANDARD_USE_DITHER_MASK 1
#endif

// Need to output UVs in shadow caster, since we need to sample texture and do clip/dithering based on it
#if defined(_ALPHATEST_ON) || defined(_ALPHABLEND_ON) || defined(_ALPHAPREMULTIPLY_ON)
#define UNITY_STANDARD_USE_SHADOW_UVS 1
#endif

// Has a non-empty shadow caster output struct (it's an error to have empty structs on some platforms...)
#if !defined(V2F_SHADOW_CASTER_NOPOS_IS_EMPTY) || defined(UNITY_STANDARD_USE_SHADOW_UVS)
#define UNITY_STANDARD_USE_SHADOW_OUTPUT_STRUCT 1
#endif

#ifdef UNITY_STEREO_INSTANCING_ENABLED
#define UNITY_STANDARD_USE_STEREO_SHADOW_OUTPUT_STRUCT 1
#endif


struct VertexInputShadow
{
	float4 vertex		: POSITION;
	float3 normal		: NORMAL;
	float2 texcoord0	: TEXCOORD0;

	UNITY_VERTEX_INPUT_INSTANCE_ID
		FLUID_VERTEX_INPUT_INSTANCE_ID
};

struct VertexOutput
{
	UNITY_POSITION(pos);
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

#ifdef UNITY_STANDARD_USE_SHADOW_OUTPUT_STRUCT
struct VertexOutputShadowCaster
{
	V2F_SHADOW_CASTER_NOPOS
};
#endif

#ifdef UNITY_STANDARD_USE_STEREO_SHADOW_OUTPUT_STRUCT
struct VertexOutputStereoShadowCaster
{
	UNITY_VERTEX_OUTPUT_STEREO
};
#endif

void vertShadowCaster(VertexInputShadow v
	, out VertexOutput output
#ifdef UNITY_STANDARD_USE_SHADOW_OUTPUT_STRUCT
	, out VertexOutputShadowCaster o
#endif
#ifdef UNITY_STANDARD_USE_STEREO_SHADOW_OUTPUT_STRUCT
	, out VertexOutputStereoShadowCaster os
#endif
)
{
	UNITY_SETUP_INSTANCE_ID(v);
	UNITY_TRANSFER_INSTANCE_ID(v, output);

	TerrainVertexInputs terrainInputs = GetTerrainVertexInputs(v.vertex.xyz, v.texcoord0, FLUID_GET_INSTANCE_ID(v));

	float4 tangentOS = float4(cross(terrainInputs.normalOS, float3(0, 0, 1)), 1);
#if defined(CURVEDWORLD_IS_INSTALLED) && !defined(CURVEDWORLD_DISABLED_ON)
   #ifdef CURVEDWORLD_NORMAL_TRANSFORMATION_ON
      CURVEDWORLD_TRANSFORM_VERTEX_AND_NORMAL(terrainInputs.positionOS, terrainInputs.normalOS, tangentOS)
   #else
      CURVEDWORLD_TRANSFORM_VERTEX(terrainInputs.positionOS)
   #endif
#endif

	v.vertex.xyz = terrainInputs.positionOS.xyz;
	v.normal = terrainInputs.normalOS;

#ifdef UNITY_STANDARD_USE_STEREO_SHADOW_OUTPUT_STRUCT
	UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(os);
#endif

	output.pos = UnityClipSpaceShadowCasterPos(v.vertex, v.normal);
	output.pos = UnityApplyLinearShadowBias(output.pos);
}

half4 fragShadowCaster(VertexOutput input
#ifdef UNITY_STANDARD_USE_SHADOW_OUTPUT_STRUCT
	, VertexOutputShadowCaster i
#endif
) : SV_Target
{
	UNITY_SETUP_INSTANCE_ID(input);
	SHADOW_CASTER_FRAGMENT(i)
}

#endif // FLUIDFRENZY_TERRAFORM_TERRAIN_SHADOWCASTER_BRP_INCLUDED