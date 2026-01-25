#ifndef FLUIDFRENZY_LAVA_FORWARD_LIT_URP_INCLUDED
#define FLUIDFRENZY_LAVA_FORWARD_LIT_URP_INCLUDED

#define FLUIDFRENZY_URP

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

#include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Rendering/Shaders/Library/FluidRenderingCommon.hlsl"
#include_with_pragmas "Packages/com.frenzybyte.fluidfrenzy/Runtime/Rendering/ExternalCompatibility/FluidThirdPartyHeadersURP.hlsl"
#include_with_pragmas "Packages/com.frenzybyte.fluidfrenzy/Runtime/Rendering/Shaders/Library/FluidFogHelpers.hlsl"

#include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Rendering/Shaders/Lava/LavaInput.hlsl"
#include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Rendering/Shaders/Lava/URP/LavaLitURP.hlsl"

// keep this file in sync with LitGBufferPass.hlsl

struct Attributes
{
	float4 positionOS		: POSITION;
	float2 fluidUV		: TEXCOORD0;
	float2 textureUV	: TEXCOORD1;
	UNITY_VERTEX_INPUT_INSTANCE_ID
	FLUID_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
	float4 positionCS		: SV_POSITION;
	float4 positionWS		: TEXCOORD1;
	float3 normalWS			: TEXCOORD2;
	float4 uv01				: TEXCOORD3;
	float4 uv23				: TEXCOORD4;
	float4 projPos			: TEXCOORD5;

#if defined(_NORMALMAP)
	half4 tangentWS		: TEXCOORD6;
#endif

#ifdef _ADDITIONAL_LIGHTS_VERTEX
	half4 fogFactorAndVertexLight   : TEXCOORD7; // x: fogFactor, yzw: vertex light
#else
	half  fogFactor                 : TEXCOORD7;
#endif

#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
	float4 shadowCoord              : TEXCOORD8;
#endif
	DECLARE_LIGHTMAP_OR_SH(staticLightmapUV, vertexSH, 9);

	UNITY_VERTEX_OUTPUT_STEREO
};

void InitializeFluidInputData(Varyings input, out FluidInputData outFluidInputData)
{
	outFluidInputData = (FluidInputData)(0);
	outFluidInputData.flowUV = input.uv23;
	outFluidInputData.fluidUV = input.uv01;
	outFluidInputData.velocity = SampleFluidVelocity(outFluidInputData.fluidUV.xy);
	outFluidInputData.fluidMask = SampleFluidNormal(outFluidInputData.fluidUV.xy).w;
}

///////////////////////////////////////////////////////////////////////////////
//                  Vertex and Fragment functions                            //
///////////////////////////////////////////////////////////////////////////////

// Used in Standard (Physically Based) shader
Varyings LitPassVertex(Attributes input)
{
	Varyings output = (Varyings)0;

	UNITY_SETUP_INSTANCE_ID(input);
	UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

	FluidData fluidData;
	SampleFluidSimulationData(input.positionOS.xyz, input.fluidUV, input.textureUV, FLUID_GET_INSTANCE_ID(input), fluidData);

	float4 tangentOS = float4(cross(fluidData.normalOS, float3(0, 0, 1)), 1);
#if defined(CURVEDWORLD_IS_INSTALLED) && !defined(CURVEDWORLD_DISABLED_ON)
   #ifdef CURVEDWORLD_NORMAL_TRANSFORMATION_ON
      CURVEDWORLD_TRANSFORM_VERTEX_AND_NORMAL(fluidData.positionOS, fluidData.normalOS, tangentOS)
   #else
      CURVEDWORLD_TRANSFORM_VERTEX(fluidData.positionOS)
   #endif
#endif

	VertexPositionInputs vertexInput = GetVertexPositionInputs(fluidData.positionOS);
	output.positionCS = vertexInput.positionCS;
	output.positionWS = float4(vertexInput.positionWS, fluidData.layerHeight);

#if defined(_NORMALMAP)
	VertexNormalInputs normalInput = GetVertexNormalInputs(fluidData.normalOS, tangentOS);
	float sign = tangentOS.w * float(GetOddNegativeScale());
	output.tangentWS = half4(normalInput.tangentWS.xyz, sign);
#else
	VertexNormalInputs normalInput = GetVertexNormalInputs(fluidData.normalOS);
#endif
	output.normalWS.xyz = normalInput.normalWS;

	output.uv01 = fluidData.uv;
	output.uv23 = fluidData.flowUV;
	output.positionCS = vertexInput.positionCS;
	output.projPos = vertexInput.positionCS;
	ApplyClipSpaceOffset(output.positionCS, _LinearClipOffset, _ExponentialClipOffset);

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

void InitializeInputData(Varyings input, half3 normalTS, out InputData inputData)
{
	inputData = (InputData)0;

#if defined(REQUIRES_WORLD_SPACE_POS_INTERPOLATOR)
	inputData.positionWS = input.positionWS.xyz;
#endif

	half3 viewDirWS = GetWorldSpaceNormalizeViewDir(input.positionWS.xyz);

#if defined(_NORMALMAP)
	float sgn = input.tangentWS.w;      // should be either +1 or -1
	float3 bitangent = sgn * cross(input.normalWS.xyz, input.tangentWS.xyz);
	half3x3 tangentToWorld = half3x3(input.tangentWS.xyz, bitangent.xyz, input.normalWS.xyz);
	inputData.tangentToWorld = tangentToWorld;
	inputData.normalWS = TransformTangentToWorld(normalTS, tangentToWorld);
#else
	inputData.normalWS = input.normalWS.xyz;
#endif

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
	inputData.fogCoord = InitializeInputDataFog(float4(input.positionWS.xyz, 1.0), input.fogFactorAndVertexLight.x);
	inputData.vertexLighting = input.fogFactorAndVertexLight.yzw;
#else
	inputData.fogCoord = InitializeInputDataFog(float4(input.positionWS.xyz, 1.0), input.fogFactor);
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

// Used in Standard (Physically Based) shader
half4 LitPassFragment(Varyings input) : SV_Target
{
	UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

	FluidInputData fluidInput;
	InitializeFluidInputData(input, fluidInput);
	ClipFluid(fluidInput.fluidMask, _Layer);

	SurfaceData surfaceData;
	InitializeStandardLitSurfaceData(fluidInput, surfaceData);

	InputData inputData;
	InitializeInputData(input, surfaceData.normalTS, inputData);
	half4 color = UniversalFragmentPBR(inputData, surfaceData);

	ApplyFog(color, input.positionWS.xyz, inputData.normalizedScreenSpaceUV, inputData.fogCoord);

	color.a = surfaceData.alpha;
	return color;
}
#endif // FLUIDFRENZY_LAVA_FORWARD_LIT_URP_INCLUDED