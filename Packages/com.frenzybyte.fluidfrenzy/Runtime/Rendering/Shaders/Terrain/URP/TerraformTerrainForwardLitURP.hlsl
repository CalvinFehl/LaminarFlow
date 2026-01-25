#ifndef FLUIDFRENZY_TERRAFORM_TERRAIN_FORWARD_LIT_URP_INCLUDED
#define FLUIDFRENZY_TERRAFORM_TERRAIN_FORWARD_LIT_URP_INCLUDED

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
	float2 texcoord	: TEXCOORD0;

	FLUID_VERTEX_INPUT_INSTANCE_ID
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
	float4 positionCS   : SV_POSITION;
	float3 positionWS	: TEXCOORD0;
	float3 normalWS		: TEXCOORD1;
	float4 tangentWS	: TEXCOORD2;
	float2 uv			: TEXCOORD3;

#ifdef _ADDITIONAL_LIGHTS_VERTEX
	half4 fogFactorAndVertexLight   : TEXCOORD5; // x: fogFactor, yzw: vertex light
#else
	half  fogFactor                 : TEXCOORD5;
#endif

#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
	float4 shadowCoord              : TEXCOORD6;
#endif
	DECLARE_LIGHTMAP_OR_SH(staticLightmapUV, vertexSH, 8);

	UNITY_VERTEX_INPUT_INSTANCE_ID
	UNITY_VERTEX_OUTPUT_STEREO
};

void InitializeInputData(Varyings input, half3 normalTS, out InputData inputData)
{
	inputData = (InputData)0;

#if defined(REQUIRES_WORLD_SPACE_POS_INTERPOLATOR)
	inputData.positionWS = input.positionWS;
#endif

	half3 viewDirWS = GetWorldSpaceNormalizeViewDir(input.positionWS);

	float sgn = input.tangentWS.w;      // should be either +1 or -1
	float3 bitangent = sgn * cross(input.normalWS.xyz, input.tangentWS.xyz);
	half3x3 tangentToWorld = half3x3(input.tangentWS.xyz, bitangent.xyz, input.normalWS.xyz);
	//inputData.tangentToWorld = tangentToWorld;
	inputData.normalWS = TransformTangentToWorld(normalTS, tangentToWorld);


	inputData.normalWS = NormalizeNormalPerPixel(inputData.normalWS);
	inputData.viewDirectionWS = viewDirWS;

#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
	inputData.shadowCoord = input.shadowCoord;
#elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
	inputData.shadowCoord = TransformWorldToShadowCoord(inputData.positionWS);
#else
	inputData.shadowCoord = float4(0, 0, 0, 0);
#endif

#if UNITY_VERSION >= 202100
#ifdef _ADDITIONAL_LIGHTS_VERTEX
	inputData.fogCoord = InitializeInputDataFog(float4(input.positionWS, 1.0), input.fogFactorAndVertexLight.x);
	inputData.vertexLighting = input.fogFactorAndVertexLight.yzw;
#else
	inputData.fogCoord = InitializeInputDataFog(float4(input.positionWS, 1.0), input.fogFactor);
#endif
#else
    inputData.fogCoord = input.fogFactor;
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
Varyings LitPassVertex(Attributes input)
{
	Varyings output = (Varyings)(0);
	UNITY_SETUP_INSTANCE_ID(input);
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
	half3 vertexLight = VertexLighting(vertexInput.positionWS, normalInput.normalWS);

	half fogFactor = 0;
#if !defined(_FOG_FRAGMENT)
	fogFactor = ComputeFogFactor(vertexInput.positionCS.z);
#endif

	OUTPUT_SH(output.normalWS.xyz, output.vertexSH);
#ifdef _ADDITIONAL_LIGHTS_VERTEX
	output.fogFactorAndVertexLight = half4(fogFactor, vertexLight);
#else
	output.fogFactor = fogFactor;
#endif

	return output;
}

// Used in Standard (Physically Based) shader
half4 LitPassFragment(Varyings input) : SV_Target
{
	UNITY_SETUP_INSTANCE_ID(input);
	UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

	#if defined(_FLUIDFRENZY_INSTANCING)
		input.normalWS = CalculateNormalFromHeightField(_HeightField, sampler_HeightField, _HeightField_TexelSize, _TexelWorldSize.xy, input.uv.xy, 0.5, 1);
		input.normalWS = mul((float3x3)unity_ObjectToWorld, input.normalWS);
		input.tangentWS.xyz = normalize(cross(input.normalWS.xyz, float3(0, 0, 1)));
		input.tangentWS.w = -unity_WorldTransformParams.w;
	#endif
	
	SurfaceData surfaceData;
	InitializeStandardLitSurfaceData(input.uv, surfaceData);

	InputData inputData;
	InitializeInputData(input, surfaceData.normalTS, inputData);
	half4 color = UniversalFragmentPBR(inputData, surfaceData);
	color.rgb = MixFog(color.rgb, inputData.fogCoord);

	return color;
}

#endif // FLUIDFRENZY_TERRAFORM_TERRAIN_FORWARD_LIT_URP_INCLUDED