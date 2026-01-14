#ifndef FLUIDFRENZY_TERRAFORM_TERRAIN_SHARED_BRP_INCLUDED
#define FLUIDFRENZY_TERRAFORM_TERRAIN_SHARED_BRP_INCLUDED


#include "UnityStandardUtils.cginc"
#define BUILTIN_TARGET_API

#include "UnityShadowLibrary.cginc"
#include "UnityStandardCore.cginc"
#include "UnityStandardInput.cginc"
#include "UnityStandardBRDF.cginc"

#include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Rendering/Shaders/Library/LOD/FluidInstancingCommon.hlsl"
#include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Rendering/Shaders/Terrain/TerraformTerrainInput.hlsl"
#include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Rendering/Shaders/Terrain/TerraformTerrainSurfaceData.hlsl"

#include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Rendering/ExternalCompatibility/FluidExternalCompatibility.cs.hlsl"

#ifdef FLUIDFRENZY_EXTERNALCOMPATIBILITY_CURVEDWORLD
#include "Assets/Amazing Assets/Curved World/Shaders/Core/CurvedWorldTransform.cginc"
#endif // FLUIDFRENZY_EXTERNALCOMPATIBILITY_CURVEDWORLD

struct Attributes
{
	float4 vertex : POSITION;
	float2 texcoord0 : TEXCOORD0;
	UNITY_VERTEX_INPUT_INSTANCE_ID
	FLUID_VERTEX_INPUT_INSTANCE_ID
};
struct Varyings
{
	float4 pos : SV_POSITION;
	float2 uv0 : TEXCOORD0;
	float3 worldPos : TEXCOORD1;
	float3 normalWS : TEXCOORD3;
	float4 tangentWS : TEXCOORD4;
	UNITY_LIGHTING_COORDS(6,7)
	UNITY_FOG_COORDS(8)
	UNITY_VERTEX_OUTPUT_STEREO
};

Varyings vert(Attributes v)
{
	Varyings output = (Varyings)(0);
	UNITY_SETUP_INSTANCE_ID(v);
	UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

	TerrainVertexInputs terrainInputs = GetTerrainVertexInputs(v.vertex.xyz, v.texcoord0, FLUID_GET_INSTANCE_ID(v));

	float4 tangentOS = float4(cross(output.normalWS.xyz, float3(0, 0, 1)),1 );
#if defined(CURVEDWORLD_IS_INSTALLED) && !defined(CURVEDWORLD_DISABLED_ON)
#ifdef CURVEDWORLD_NORMAL_TRANSFORMATION_ON
CURVEDWORLD_TRANSFORM_VERTEX_AND_NORMAL(terrainInputs.positionOS, terrainInputs.normalOS, tangentOS)
#else
CURVEDWORLD_TRANSFORM_VERTEX(terrainInputs.positionOS)
#endif
#endif
	float3 positionWS = mul(unity_ObjectToWorld, float4(terrainInputs.positionOS, 1)).xyz;
	float3 normalWS = UnityObjectToWorldNormal(terrainInputs.normalOS);

	output.pos = UnityWorldToClipPos(positionWS);
	output.worldPos = positionWS;
	output.normalWS = normalWS;
	output.uv0 = terrainInputs.uv;

	output.tangentWS.xyz = cross(output.normalWS.xyz, float3(0, 0, 1));
	output.tangentWS.w = -unity_WorldTransformParams.w;

	UNITY_TRANSFER_LIGHTING(output, output.uv0);
	UNITY_TRANSFER_FOG(output, output.pos);
	return output;
}


inline FragmentCommonData RoughnessSetup(Varyings i, out float occlusion)
{
	TerrainSurfaceData terrainData = GetTerrainSurfaceData(i.uv0);

	half oneMinusReflectivity;
	half3 specColor;
	half3 diffColor = DiffuseAndSpecularFromMetallic(terrainData.albedo, terrainData.metallic, /*out*/ specColor, /*out*/ oneMinusReflectivity);

	FragmentCommonData o = (FragmentCommonData)0;
	o.diffColor = diffColor;
	o.specColor = specColor;
	o.oneMinusReflectivity = oneMinusReflectivity;
	o.smoothness = terrainData.smoothness;

	float3 normalTS = terrainData.normal;
	float3 geomTangent = normalize(i.tangentWS.xyz);
	float3 geomBitangent = cross(geomTangent, i.normalWS);
	float3 normalWS = normalTS.x * geomTangent + normalTS.y * geomBitangent + normalTS.z * i.normalWS;
	normalWS.xyz = normalize(normalWS.xyz);
	o.normalWorld = normalWS.xyz;
	o.alpha = 1;
	occlusion = terrainData.occlusion;
	return o;
}

inline FragmentCommonData FragmentSetup(Varyings i, out float occlusion)
{
	FragmentCommonData o = RoughnessSetup(i, occlusion);
	o.eyeVec = normalize(i.worldPos.xyz - _WorldSpaceCameraPos.xyz);
	o.posWorld = i.worldPos.xyz;
	return o;
}

#endif // FLUIDFRENZY_TERRAFORM_TERRAIN_SHARED_BRP_INCLUDED