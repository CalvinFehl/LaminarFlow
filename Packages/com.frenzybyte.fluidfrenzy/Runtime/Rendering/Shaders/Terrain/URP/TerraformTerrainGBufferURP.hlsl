#ifndef FLUIDFRENZY_TERRAFORM_TERRAIN_GBUFFER_URP_INCLUDED
#define FLUIDFRENZY_TERRAFORM_TERRAIN_GBUFFER_URP_INCLUDED

#include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Rendering/Shaders/Library/LOD/FluidInstancingCommon.hlsl"
#include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Rendering/Shaders/Terrain/TerraformTerrainInput.hlsl"
#include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Rendering/Shaders/Terrain/URP/TerraformTerrainLitURP.hlsl"

#include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Rendering/ExternalCompatibility/FluidExternalCompatibility.cs.hlsl"

#ifdef FLUIDFRENZY_EXTERNALCOMPATIBILITY_CURVEDWORLD
#include "Assets/Amazing Assets/Curved World/Shaders/Core/CurvedWorldTransform.cginc"
#endif // FLUIDFRENZY_EXTERNALCOMPATIBILITY_CURVEDWORLD

struct Attributes
{
	float4 positionOS   : POSITION;
	float3 normalOS     : NORMAL;
	float4 tangentOS    : TANGENT;
	float2 texcoord     : TEXCOORD0;
	float2 staticLightmapUV   : TEXCOORD1;
	float2 dynamicLightmapUV  : TEXCOORD2;
	FLUID_VERTEX_INPUT_INSTANCE_ID
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
	float2 uv                       : TEXCOORD0;

	float3 positionWS               : TEXCOORD1;

	half3 normalWS                  : TEXCOORD2;
	half4 tangentWS                 : TEXCOORD3;    // xyz: tangent, w: sign
#ifdef _ADDITIONAL_LIGHTS_VERTEX
	half3 vertexLighting            : TEXCOORD4;    // xyz: vertex lighting
#endif

#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
	float4 shadowCoord              : TEXCOORD5;
#endif

	DECLARE_LIGHTMAP_OR_SH(staticLightmapUV, vertexSH, 7);
#ifdef DYNAMICLIGHTMAP_ON
	float2  dynamicLightmapUV       : TEXCOORD8; // Dynamic lightmap UVs
#endif

	float4 positionCS               : SV_POSITION;
	UNITY_VERTEX_INPUT_INSTANCE_ID
	UNITY_VERTEX_OUTPUT_STEREO
};

void InitializeInputData(Varyings input, half3 normalTS, out InputData inputData)
{
	inputData = (InputData)0;

	inputData.positionWS = input.positionWS;
#if UNITY_VERSION >= 202100
	inputData.positionCS = input.positionCS;
#endif
	half3 viewDirWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
	float sgn = input.tangentWS.w;      // should be either +1 or -1
	float3 bitangent = sgn * cross(input.normalWS.xyz, input.tangentWS.xyz);
	inputData.normalWS = TransformTangentToWorld(normalTS, half3x3(input.tangentWS.xyz, bitangent.xyz, input.normalWS.xyz));


	inputData.normalWS = NormalizeNormalPerPixel(inputData.normalWS);
	inputData.viewDirectionWS = viewDirWS;

#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
	inputData.shadowCoord = input.shadowCoord;
#elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
	inputData.shadowCoord = TransformWorldToShadowCoord(inputData.positionWS);
#else
	inputData.shadowCoord = float4(0, 0, 0, 0);
#endif

	inputData.fogCoord = 0.0; // we don't apply fog in the guffer pass

#ifdef _ADDITIONAL_LIGHTS_VERTEX
	inputData.vertexLighting = input.vertexLighting.xyz;
#else
	inputData.vertexLighting = half3(0, 0, 0);
#endif

#if defined(DYNAMICLIGHTMAP_ON)
	inputData.bakedGI = SAMPLE_GI(input.staticLightmapUV, input.dynamicLightmapUV, input.vertexSH, inputData.normalWS);
#else
	inputData.bakedGI = SAMPLE_GI(input.staticLightmapUV, input.vertexSH, inputData.normalWS);
#endif

	inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
	inputData.shadowMask = SAMPLE_SHADOWMASK(input.staticLightmapUV);
}

///////////////////////////////////////////////////////////////////////////////
//                  Vertex and Fragment functions                            //
///////////////////////////////////////////////////////////////////////////////

// Used in Standard (Physically Based) shader
Varyings LitGBufferPassVertex(Attributes input)
{
	Varyings output = (Varyings)0;

	UNITY_SETUP_INSTANCE_ID(input);
	UNITY_TRANSFER_INSTANCE_ID(input, output);
	UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

	TerrainVertexInputs terrainInputs = GetTerrainVertexInputs(input.positionOS.xyz, input.texcoord, FLUID_GET_INSTANCE_ID(input));

	float4 tangentOS = float4(cross(terrainInputs.normalOS, float3(0, 0, 1)), 1);

#if defined(CURVEDWORLD_IS_INSTALLED) && !defined(CURVEDWORLD_DISABLED_ON)
   #ifdef CURVEDWORLD_NORMAL_TRANSFORMATION_ON
      CURVEDWORLD_TRANSFORM_VERTEX_AND_NORMAL(terrainInputs.positionOS, terrainInputs.normalOS, tangentOS)
   #else
      CURVEDWORLD_TRANSFORM_VERTEX(terrainInputs.positionOS)
   #endif
#endif

	VertexPositionInputs vertexInput = GetVertexPositionInputs(terrainInputs.positionOS);
	output.positionCS = vertexInput.positionCS;
	output.positionWS = vertexInput.positionWS;
	output.uv = terrainInputs.uv;

	VertexNormalInputs normalInput = GetVertexNormalInputs(terrainInputs.normalOS, tangentOS);
	float sign = tangentOS.w * float(GetOddNegativeScale());
	half4 tangentWS = half4(normalInput.tangentWS.xyz, sign);

	output.normalWS = half3(normalInput.normalWS);
	output.tangentWS = tangentWS;

#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
	output.shadowCoord = GetShadowCoord(vertexInput);
#endif
	OUTPUT_SH(output.normalWS.xyz, output.vertexSH);

#ifdef _ADDITIONAL_LIGHTS_VERTEX
	half3 vertexLight = VertexLighting(vertexInput.positionWS, normalInput.normalWS);
	output.vertexLighting = vertexLight;
#endif
	return output;
}

// Used in Standard (Physically Based) shader
#if UNITY_VERSION >= 60010000
GBufferFragOutput LitGBufferPassFragment(Varyings input)
#else
FragmentOutput LitGBufferPassFragment(Varyings input)
#endif
{
	UNITY_SETUP_INSTANCE_ID(input);
	UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

	SurfaceData surfaceData;
	InitializeStandardLitSurfaceData(input.uv, surfaceData);

	InputData inputData;
	InitializeInputData(input, surfaceData.normalTS, inputData);

#if UNITY_VERSION >= 60000000
	SETUP_DEBUG_TEXTURE_DATA(inputData, UNDO_TRANSFORM_TEX(input.uv, _BaseMap));
#elif UNITY_VERSION >= 202100
	SETUP_DEBUG_TEXTURE_DATA(inputData, input.uv, _BaseMap);
#endif

#ifdef _DBUFFER
	ApplyDecalToSurfaceData(input.positionCS, surfaceData, inputData);
#endif

	// Stripped down version of UniversalFragmentPBR().

	// in LitForwardPass GlobalIllumination (and temporarily LightingPhysicallyBased) are called inside UniversalFragmentPBR
	// in Deferred rendering we store the sum of these values (and of emission as well) in the GBuffer
	BRDFData brdfData;
	InitializeBRDFData(surfaceData.albedo, surfaceData.metallic, surfaceData.specular, surfaceData.smoothness, surfaceData.alpha, brdfData);

	Light mainLight = GetMainLight(inputData.shadowCoord, inputData.positionWS, inputData.shadowMask);
	MixRealtimeAndBakedGI(mainLight, inputData.normalWS, inputData.bakedGI, inputData.shadowMask);
#if UNITY_VERSION >= 60010000
	half3 color = GlobalIllumination(brdfData, inputData.bakedGI, surfaceData.occlusion, inputData.positionWS, inputData.normalWS, inputData.viewDirectionWS);
	return PackGBuffersBRDFData(brdfData, inputData, surfaceData.smoothness, surfaceData.emission + color, surfaceData.occlusion);
#elif UNITY_VERSION >= 202100
	half3 color = GlobalIllumination(brdfData, inputData.bakedGI, surfaceData.occlusion, inputData.positionWS, inputData.normalWS, inputData.viewDirectionWS);
	return BRDFDataToGbuffer(brdfData, inputData, surfaceData.smoothness, surfaceData.emission + color, surfaceData.occlusion);
#else
	half3 color = GlobalIllumination(brdfData, inputData.bakedGI, surfaceData.occlusion, inputData.normalWS, inputData.viewDirectionWS);
	return BRDFDataToGbuffer(brdfData, inputData, surfaceData.smoothness, surfaceData.emission + color);
#endif
}
#endif // FLUIDFRENZY_TERRAFORM_TERRAIN_GBUFFER_URP_INCLUDED