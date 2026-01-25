#ifndef FLUIDFRENZY_WATER_FORWARD_BRP_INCLUDED
#define FLUIDFRENZY_WATER_FORWARD_BRP_INCLUDED
#define BUILTIN_TARGET_API
#if _SHADOWMAP_SOFT
#define SOFT_SHADOWS
#endif

#define FLUIDFRENZY_WATER_INPUT_INCLUDED_REQUIRES_CAMERADEPTH
#define FLUIDFRENZY_BIRP

#if _TRANSPARENT_RECEIVE_SHADOWS_CLOSE_FIT || _TRANSPARENT_RECEIVE_SHADOWS_SPLIT_SPHERES
#define _TRANSPARENT_RECEIVE_SHADOWS
#endif
#include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Rendering/Shaders/Library/ShadowSampling.cginc"

#include "UnityCG.cginc"
#include "UnityStandardUtils.cginc"
#include "UnityShadowLibrary.cginc"
#include "UnityStandardCore.cginc"
#include "UnityStandardInput.cginc"
#include "UnityStandardBRDF.cginc"

#include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Rendering/Shaders/Library/FluidPipelineCompatibility.hlsl"
#include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Rendering/Shaders/Library/FluidRenderingCommon.hlsl"
#include_with_pragmas "Packages/com.frenzybyte.fluidfrenzy/Runtime/Rendering/ExternalCompatibility/FluidThirdPartyHeadersBRP.hlsl"
#include_with_pragmas "Packages/com.frenzybyte.fluidfrenzy/Runtime/Rendering/Shaders/Library/FluidFogHelpers.hlsl"
#include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Rendering/Shaders/Water/WaterInput.hlsl"
#include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Rendering/Shaders/Water/WaterInputData.hlsl"
#include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Rendering/Shaders/Water/WaterSurfaceData.hlsl"
#include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Rendering/Shaders/Water/Builtin/WaterLightingBRP.hlsl"

float RealLinearEyeDepth(float rawDepth)
{
	float persp = LinearEyeDepth(rawDepth);
	float ortho = (_ProjectionParams.z-_ProjectionParams.y)*(1-rawDepth)+_ProjectionParams.y;
	return lerp(persp,ortho,unity_OrthoParams.w);
}

struct Attributes
{
	float4 vertex : POSITION;
	float2 fluidUV : TEXCOORD0;
	float2 textureUV : TEXCOORD1;
	UNITY_VERTEX_INPUT_INSTANCE_ID
	FLUID_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
	float4 vertex			: SV_POSITION;
	float4 uv01				: TEXCOORD0;
	float4 uv23				: TEXCOORD1;
	float4 worldPos			: TEXCOORD2;
	float4 projPos			: TEXCOORD3;
	float3 normalWS			: TEXCOORD4;

#if defined(_NORMALMAP) || defined(_FOAM_NORMALMAP)
	float4 tangentWS		: TEXCOORD5;
#endif
	UNITY_FOG_COORDS(6)
	UNITY_VERTEX_OUTPUT_STEREO
};

Varyings vert(Attributes v)
{
	Varyings o;
	UNITY_SETUP_INSTANCE_ID(v);
	UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

	FluidData fluidData;
	SampleFluidSimulationData(v.vertex, v.fluidUV, v.textureUV, FLUID_GET_INSTANCE_ID(v), fluidData);

	float4 tangentOS = float4(cross(fluidData.normalOS, float3(0, 0, 1)), 1);
	ApplyDisplacementWaves(fluidData);

#if defined(CURVEDWORLD_IS_INSTALLED) && !defined(CURVEDWORLD_DISABLED_ON)
	#ifdef CURVEDWORLD_NORMAL_TRANSFORMATION_ON
		CURVEDWORLD_TRANSFORM_VERTEX_AND_NORMAL(fluidData.positionOS, fluidData.normalOS, tangentOS)
	#else
		CURVEDWORLD_TRANSFORM_VERTEX(fluidData.positionOS)
	#endif
#endif

	o.uv01 = fluidData.uv;
	o.uv23 = fluidData.flowUV;
	o.normalWS.xyz = fluidData.normalOS;
	o.worldPos.xyz = mul(unity_ObjectToWorld, float4(fluidData.positionOS, 1));
	o.worldPos.w = fluidData.fluidHeight;
	o.vertex = UnityObjectToClipPos(fluidData.positionOS);
	UNITY_TRANSFER_FOG(o, o.vertex);
	o.projPos = ComputeGrabScreenPos(o.vertex);

	ApplyClipSpaceOffset(o.vertex, _LinearClipOffset, _ExponentialClipOffset);

#if defined(_NORMALMAP) || defined(_FOAM_NORMALMAP)
	o.tangentWS.xyz = tangentOS.xyz;
	o.tangentWS.w = -1;
#endif

	return o;
}

void InitializeFluidInputData(Varyings input, out FluidInputData outFluidInputData)
{
	outFluidInputData = (FluidInputData)(0);
	outFluidInputData.flowUV = input.uv23;
	outFluidInputData.fluidUV = input.uv01;
	outFluidInputData.velocity = SampleFluidVelocity(outFluidInputData.fluidUV.xy);
	outFluidInputData.fluidMask = SampleFluidNormal(outFluidInputData.fluidUV.xy).w;
	outFluidInputData.fluidHeight = input.worldPos.w;
}

void InitializeWaterInputData(Varyings input, bool isFrontFace, in FluidInputData fluidInput, in WaterSurfaceData waterSurfaceData, out WaterInputData outWaterInput)
{
	outWaterInput = (WaterInputData)(0);

	outWaterInput.frontFaceMask = isFrontFace ? 1.0f : -1.0f;
	input.normalWS = input.normalWS * outWaterInput.frontFaceMask;

	outWaterInput.positionWS = input.worldPos.xyz;
	outWaterInput.normalizedScreenSpaceUV = input.projPos.xy / input.projPos.w;
	
#if defined(_SCREENSPACE_REFRACTION_ON) || defined(_SCREENSPACE_REFRACTION_ALPHA) || defined(_SCREENSPACE_REFRACTION_ABSORB)
	float2 depthUVSample = outWaterInput.normalizedScreenSpaceUV.xy;
#if UNITY_UV_STARTS_AT_TOP
	depthUVSample.y = 1.0f - depthUVSample.y;
#endif
	if (_ProjectionParams.x < 0)
		depthUVSample.y = 1-depthUVSample.y;

	outWaterInput.pixelZ = RealLinearEyeDepth(input.vertex.z);
	outWaterInput.sceneDepth = SAMPLE_SCREENSPACE_TEXTURE(_CameraDepthTexture, depthUVSample).x;
	outWaterInput.sceneZ = RealLinearEyeDepth(outWaterInput.sceneDepth);
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
	outWaterInput.shadow = getShadowValue(float4(outWaterInput.positionWS, 1));
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
	float2 refractedUV = outWaterInput.normalizedScreenSpaceUV + refractOffset;

	depthUVSample = refractedUV;
#if UNITY_UV_STARTS_AT_TOP
	depthUVSample.y = 1.0f - depthUVSample.y;
#endif
	if (_ProjectionParams.x < 0)
		depthUVSample.y = 1-depthUVSample.y;

	outWaterInput.refractedSceneDepth = SAMPLE_SCREENSPACE_TEXTURE(_CameraDepthTexture, depthUVSample).x;
	outWaterInput.refractedSceneZ = RealLinearEyeDepth(outWaterInput.refractedSceneDepth);
	outWaterInput.refractionColor = SAMPLE_SCREENSPACE_TEXTURE(_BackgroundTexture, refractedUV);
	outWaterInput.refractedDistance = max(outWaterInput.refractedSceneZ - outWaterInput.pixelZ, outWaterInput.waterDepth);
#else
	outWaterInput.refractedDistance = outWaterInput.waterDepth;
#endif

	outWaterInput.bakedGI = ShadeSH9(half4(outWaterInput.waveNormalWS, 1.0));

	outWaterInput.reflectionColor = SampleReflectionColor(waterSurfaceData, outWaterInput);

#if _SCREENSPACE_FOAMMASK_ON
	outWaterInput.screenspaceParticles = SAMPLE_TEXTURE2D(_FluidScreenSpaceParticles, linear_clamp_sampler, outWaterInput.normalizedScreenSpaceUV);
#endif // _SCREENSPACE_FOAMMASK_ON

	UNITY_TRANSFER_FOG(outWaterInput, input.fogCoord.xxx);
}

half4 frag(Varyings i, bool isFrontFace : SV_IsFrontFace) : SV_Target
{
	UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

	FluidInputData fluidInput;
	InitializeFluidInputData(i, fluidInput);
	ClipFluid(fluidInput.fluidMask, _Layer);

	WaterSurfaceData waterSurfaceData;
	InitializeWaterSurfaceData(fluidInput, waterSurfaceData);

	WaterInputData waterInput;
	InitializeWaterInputData(i, isFrontFace, fluidInput, waterSurfaceData, waterInput);
	half4 col = WaterLightingPBR(waterSurfaceData, waterInput);

	return col;
}

#endif // FLUIDFRENZY_WATER_FORWARD_BRP_INCLUDED