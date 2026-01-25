#ifndef FLUIDFRENZY_WATER_FORWARD_URP_INCLUDED
#define FLUIDFRENZY_WATER_FORWARD_URP_INCLUDED

#if _SHADOWMAP_SOFT
#define _SHADOWS_SOFT
#endif
#define FLUIDFRENZY_URP
#define FLUIDFRENZY_WATER_INPUT_INCLUDED_REQUIRES_CAMERADEPTH

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"

#include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Rendering/Shaders/Library/FluidRenderingHelpers.hlsl"


#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

#include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Rendering/Shaders/Library/FluidRenderingCommon.hlsl"
#include_with_pragmas "Packages/com.frenzybyte.fluidfrenzy/Runtime/Rendering/ExternalCompatibility/FluidThirdPartyHeadersURP.hlsl"
#include_with_pragmas "Packages/com.frenzybyte.fluidfrenzy/Runtime/Rendering/Shaders/Library/FluidFogHelpers.hlsl"

#include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Rendering/Shaders/Water/WaterInput.hlsl"
#include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Rendering/Shaders/Water/WaterInputData.hlsl"
#include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Rendering/Shaders/Water/WaterSurfaceData.hlsl"
#include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Rendering/Shaders/Water/URP/WaterLightingURP.hlsl"

float RealLinearEyeDepth(float rawDepth, float4 zBufferParam)
{
	float persp = LinearEyeDepth(rawDepth, zBufferParam);
	float ortho = (_ProjectionParams.z-_ProjectionParams.y)*(1-rawDepth)+_ProjectionParams.y;
	return lerp(persp,ortho,unity_OrthoParams.w);
}

// keep this file in sync with LitGBufferPass.hlsl

struct Attributes
{
	float4 positionOS	: POSITION;
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

#if defined(_NORMALMAP) || defined(_FOAM_NORMALMAP)
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

void InitializeWaterInputData(Varyings input, bool isFrontFace, in FluidInputData fluidInput, in WaterSurfaceData waterSurfaceData, out WaterInputData outWaterInput)
{
	outWaterInput = (WaterInputData)(0);

	outWaterInput.frontFaceMask = isFrontFace ? 1.0f : -1.0f;
	input.normalWS = input.normalWS * outWaterInput.frontFaceMask;

	outWaterInput.positionWS = input.positionWS.xyz;

	outWaterInput.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);

#if defined(_SCREENSPACE_REFRACTION_ON) || defined(_SCREENSPACE_REFRACTION_ALPHA) || defined(_SCREENSPACE_REFRACTION_ABSORB)
	outWaterInput.pixelZ = RealLinearEyeDepth(input.positionCS.z, _ZBufferParams);
	outWaterInput.sceneDepth = SAMPLE_SCREENSPACE_TEXTURE(_CameraDepthTexture, outWaterInput.normalizedScreenSpaceUV).x;
	outWaterInput.sceneZ = RealLinearEyeDepth(outWaterInput.sceneDepth, _ZBufferParams);
	outWaterInput.waterDepth = outWaterInput.sceneZ - outWaterInput.pixelZ;
#endif

	outWaterInput.normalWS = normalize(input.normalWS);
#if defined(_NORMALMAP) || defined(_FOAM_NORMALMAP)
	outWaterInput.tangentWS = normalize(input.tangentWS.xyz);
	outWaterInput.bitangentWS = cross(outWaterInput.tangentWS, outWaterInput.normalWS);
#endif

	outWaterInput.viewDirectionWS = normalize(_WorldSpaceCameraPos.xyz - outWaterInput.positionWS);

	outWaterInput.shadow = 1;
#if (defined(_SHADOWMAP_HARD) || defined(_SHADOWMAP_SOFT)) && defined(_TRANSPARENT_RECEIVE_SHADOWS)
	//outWaterInput.shadow = getShadowValue(float4(outWaterInput.positionWS, 1));
#endif

#if defined(_NORMALMAP)
	outWaterInput.waveNormalWS = normalize(waterSurfaceData.waveNormalTS.x * outWaterInput.tangentWS +
		waterSurfaceData.waveNormalTS.y * outWaterInput.bitangentWS +
		waterSurfaceData.waveNormalTS.z * outWaterInput.normalWS);
#else
	outWaterInput.waveNormalWS = outWaterInput.normalWS;
#endif

#if defined(_FOAM_NORMALMAP)
	outWaterInput.foamNormalWS = normalize(waterSurfaceData.foamNormalTS.x * outWaterInput.tangentWS +
		waterSurfaceData.foamNormalTS.y * outWaterInput.bitangentWS +
		waterSurfaceData.foamNormalTS.z * outWaterInput.normalWS);

#else
	outWaterInput.foamNormalWS = outWaterInput.normalWS;
#endif

	outWaterInput.foamNormalWS = outWaterInput.foamNormalWS * outWaterInput.frontFaceMask;
	outWaterInput.fade = smoothstep(0, _FadeHeight, max(0, (fluidInput.fluidMask * FluidLayerToMask(_Layer)) - _FluidClipHeight));

	//Foam
#if defined(_FOAMMASK_ON)
	half foamField = SampleFluidFoamField(fluidInput.fluidUV.zw);
	outWaterInput.foamMask = smoothstep(_FoamVisibility.x, _FoamVisibility.y, foamField);
#else
	outWaterInput.foamMask = 0;
#endif

#if defined(_SCREENSPACE_REFRACTION_ON) || defined(_SCREENSPACE_REFRACTION_ABSORB)
	half2 refractOffset = outWaterInput.waveNormalWS.xz * _RefractionDistortion * min(1, outWaterInput.waterDepth);
	outWaterInput.refractedSceneDepth = SAMPLE_SCREENSPACE_TEXTURE(_CameraDepthTexture, outWaterInput.normalizedScreenSpaceUV + refractOffset).x;
	outWaterInput.refractedSceneZ = RealLinearEyeDepth(outWaterInput.refractedSceneDepth, _ZBufferParams);
	outWaterInput.refractionColor = SAMPLE_SCREENSPACE_TEXTURE(_CameraOpaqueTexture, outWaterInput.normalizedScreenSpaceUV + refractOffset);
	outWaterInput.refractedDistance = max(outWaterInput.refractedSceneZ - outWaterInput.pixelZ, outWaterInput.waterDepth);
#else
	outWaterInput.refractedDistance = outWaterInput.waterDepth;
#endif


#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
	outWaterInput.shadowCoord = input.shadowCoord;
#elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
	outWaterInput.shadowCoord = TransformWorldToShadowCoord(outWaterInput.positionWS.xyz);
#else
	outWaterInput.shadowCoord = float4(0, 0, 0, 0);
#endif

#if UNITY_VERSION >= 202100
#ifdef _ADDITIONAL_LIGHTS_VERTEX
	outWaterInput.fogCoord = InitializeInputDataFog(float4(input.positionWS.xyz, 1.0), input.fogFactorAndVertexLight.x);
	outWaterInput.vertexLighting = input.fogFactorAndVertexLight.yzw;
#else
	outWaterInput.fogCoord = InitializeInputDataFog(float4(input.positionWS.xyz, 1.0), input.fogFactor);
#endif
#else
	outWaterInput.fogCoord = input.fogFactor;
#endif

	outWaterInput.bakedGI = SAMPLE_GI(input.staticLightmapUV, input.vertexSH, outWaterInput.normalWS);

	outWaterInput.reflectionColor = SampleReflectionColor(waterSurfaceData, outWaterInput);


#if _SCREENSPACE_FOAMMASK_ON
	outWaterInput.screenspaceParticles = SAMPLE_TEXTURE2D(_FluidScreenSpaceParticles, linear_clamp_sampler, outWaterInput.normalizedScreenSpaceUV);
#endif // _SCREENSPACE_FOAMMASK_ON
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
	SampleFluidSimulationData(input.positionOS.xyz, input.fluidUV.xy, input.textureUV.xy, FLUID_GET_INSTANCE_ID(input), fluidData);
	
	float4 tangentOS = float4(cross(fluidData.normalOS, float3(0, 0, 1)), 1);
	ApplyDisplacementWaves(fluidData);

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

#if defined(_NORMALMAP) || defined(_FOAM_NORMALMAP)
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

// Used in Standard (Physically Based) shader
half4 LitPassFragment(Varyings input, bool isFrontFace : SV_IsFrontFace) : SV_Target
{
	UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

	FluidInputData fluidInput;
	InitializeFluidInputData(input, fluidInput);
	ClipFluid(fluidInput.fluidMask, _Layer);

	WaterSurfaceData waterSurfaceData;
	InitializeWaterSurfaceData(fluidInput, waterSurfaceData);

	WaterInputData waterInput;
	InitializeWaterInputData(input, isFrontFace, fluidInput, waterSurfaceData, waterInput);

	half4 color = half4(0, 0, 0, 1);
	color = WaterLightingPBR(waterSurfaceData, waterInput);

	ApplyFog(color, waterInput.positionWS.xyz, waterInput.normalizedScreenSpaceUV, waterInput.fogCoord);

	return color;
}

#endif // FLUIDFRENZY_WATER_FORWARD_URP_INCLUDED