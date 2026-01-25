#ifndef FLUIDFRENZY_LAVA_FORWARD_BRP_INCLUDED
#define FLUIDFRENZY_LAVA_FORWARD_BRP_INCLUDED
#define BUILTIN_TARGET_API
#if _SHADOWMAP_SOFT
#define SOFT_SHADOWS
#endif

#define FLUIDFRENZY_BIRP

#if (_TRANSPARENT_RECEIVE_SHADOWS_CLOSE_FIT || _TRANSPARENT_RECEIVE_SHADOWS_SPLIT_SPHERES) 
#define _TRANSPARENT_RECEIVE_SHADOWS
#endif

#ifdef _TRANSPARENT_RECEIVE_SHADOWS
#include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Rendering/Shaders/Library/ShadowSampling.cginc"
#endif

#include "UnityCG.cginc"
#include "UnityPBSLighting.cginc"
#include "UnityStandardCore.cginc"

#define _NON_TILED_SAMPLING
#include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Rendering/Shaders/Library/FluidPipelineCompatibility.hlsl"
#include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Rendering/Shaders/Library/FluidRenderingCommon.hlsl"
#include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Rendering/Shaders/Lava/LavaInput.hlsl"

#include_with_pragmas "Packages/com.frenzybyte.fluidfrenzy/Runtime/Rendering/ExternalCompatibility/FluidThirdPartyHeadersBRP.hlsl"
#include_with_pragmas "Packages/com.frenzybyte.fluidfrenzy/Runtime/Rendering/Shaders/Library/FluidFogHelpers.hlsl"

#ifdef FLUIDFRENZY_EXTERNALCOMPATIBILITY_CURVEDWORLD
#include "Assets/Amazing Assets/Curved World/Shaders/Core/CurvedWorldTransform.cginc"
#endif // FLUIDFRENZY_EXTERNALCOMPATIBILITY_CURVEDWORLD

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
	float3 normalWS			: TEXCOORD3;
#if defined(_NORMALMAP)
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
#if defined(CURVEDWORLD_IS_INSTALLED) && !defined(CURVEDWORLD_DISABLED_ON)
   #ifdef CURVEDWORLD_NORMAL_TRANSFORMATION_ON
      CURVEDWORLD_TRANSFORM_VERTEX_AND_NORMAL(fluidData.positionOS, fluidData.normalOS, tangentOS)
   #else
      CURVEDWORLD_TRANSFORM_VERTEX(fluidData.positionOS)
   #endif
#endif

	o.normalWS.xyz = fluidData.normalOS;
	o.uv01 = fluidData.uv;
	o.uv23 = fluidData.flowUV;
	o.worldPos.xyz = mul(unity_ObjectToWorld, float4(fluidData.positionOS, 1));
	o.worldPos.w = fluidData.layerHeight;
	o.vertex = UnityObjectToClipPos(fluidData.positionOS);
	ApplyClipSpaceOffset(o.vertex, _LinearClipOffset, _ExponentialClipOffset);


#if defined(_NORMALMAP)
	o.tangentWS.xyz = tangentOS.xyz;
	o.tangentWS.w = -1;
#endif

	UNITY_TRANSFER_FOG(o, o.vertex);
	return o;
}

void InitializeFluidInputData(Varyings input, out FluidInputData outFluidInputData)
{
	outFluidInputData = (FluidInputData)(0);
	outFluidInputData.flowUV = input.uv23;
	outFluidInputData.fluidUV = input.uv01;
	outFluidInputData.velocity = SampleFluidVelocity(outFluidInputData.fluidUV.xy);
	outFluidInputData.fluidMask = SampleFluidNormal(outFluidInputData.fluidUV.xy).w;
}

#ifdef BUILTIN_TARGET_API
#define UnpackNormalScale(x,y) UnpackNormalWithScale(x,y)
#endif

inline FragmentCommonData RoughnessSetup(in Varyings i, in FluidInputData fluidInput, in LavaSurfaceData lavaSurfaceData, out float3 emission)
{
	float3 normalWS = normalize(i.normalWS);

	half oneMinusReflectivity;
	half3 specColor;
	half3 diffColor = DiffuseAndSpecularFromMetallic(lavaSurfaceData.albedo, lavaSurfaceData.metallic, /*out*/ specColor, /*out*/ oneMinusReflectivity);

	FragmentCommonData o = (FragmentCommonData)0;
	o.diffColor = diffColor;
	o.specColor = specColor;
	o.oneMinusReflectivity = oneMinusReflectivity;
	o.smoothness = lavaSurfaceData.smoothness;
	o.normalWorld = normalWS;

#if defined(_NORMALMAP)
	float3 geomTangent = normalize(i.tangentWS.xyz);
	float3 geomBitangent = cross(geomTangent, normalWS);
	o.normalWorld = normalize(lavaSurfaceData.normalTS.x * geomTangent +
		lavaSurfaceData.normalTS.y * geomBitangent +
		lavaSurfaceData.normalTS.z * normalWS);
#endif

	emission = lavaSurfaceData.emission;
	o.alpha = lavaSurfaceData.alpha;
	return o;
}

inline FragmentCommonData FragmentSetup(Varyings i, out float3 emission)
{
	FluidInputData fluidInput;
	InitializeFluidInputData(i, fluidInput);
	ClipFluid(fluidInput.fluidMask, _Layer);

	LavaSurfaceData lavaSurfaceData;
	InitializeLavaSurfaceData(fluidInput, lavaSurfaceData);

	FragmentCommonData o = RoughnessSetup(i, fluidInput, lavaSurfaceData, emission);
	o.eyeVec = normalize(i.worldPos.xyz - _WorldSpaceCameraPos.xyz);
	o.posWorld = i.worldPos.xyz;
	return o;
}

float4 frag(Varyings i) : SV_Target
{
	float4 col = float4(0, 0, 0, 1);
	float3 emission;
	FragmentCommonData s = FragmentSetup(i, emission);

	UnityLight mainLight = MainLight();
	mainLight.color *= _LightIntensity;
	UNITY_LIGHT_ATTENUATION(atten, i, s.posWorld);

	float shadow = 1;
	#if (defined(_SHADOWMAP_HARD) || defined(_SHADOWMAP_SOFT)) && defined(_TRANSPARENT_RECEIVE_SHADOWS)
		shadow = getShadowValue(float4(i.worldPos.xyz, 1));
		atten *= shadow;
	#endif

	half occlusion = 1;
	UnityGI gi = FragmentGI(s, occlusion, float4(0,0,0,0), 1, mainLight);

	col = UNITY_BRDF_PBS(s.diffColor, s.specColor, s.oneMinusReflectivity, s.smoothness, s.normalWorld, -s.eyeVec, gi.light, gi.indirect);
	col.rgb += emission;
	
	// Apply Unity or Third-Party fog.
	ApplyFog(col, i.worldPos.xyz, i.vertex.xy * rcp(_ScreenParams.xy), UNITY_GET_FOGCOORD(i));

	col.a = s.alpha;
	return col;
}

#endif // FLUIDFRENZY_LAVA_FORWARD_BRP_INCLUDED