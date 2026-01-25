#ifndef FLUIDFRENZY_WATER_LIGHTING_BRP_INCLUDED
#define FLUIDFRENZY_WATER_LIGHTING_BRP_INCLUDED

#include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Rendering/Shaders/Water/WaterLighting.hlsl"
#include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Rendering/Shaders/Water/WaterRenderingCommon.hlsl"


#define FLT_MIN  1.175494351e-38 // Minimum normalized positive floating-point number

half DirectBRDFSpecular(float smoothness, half3 normalWS, half3 lightDirectionWS, half3 viewDirectionWS)
{
#define HALF_MIN_SQRT 0.0078125  // 2^-7 == sqrt(HALF_MIN), useful for ensuring HALF_MIN after x^2
#define HALF_MIN 6.103515625e-5  // 2^-14, the same value for 10, 11 and 16-bit: https://www.khronos.org/opengl/wiki/Small_Float_Formats

	float perceptualRoughness = 1.0 - smoothness;
	float roughness = max(PerceptualRoughnessToRoughness(perceptualRoughness), HALF_MIN_SQRT);
	float roughness2 = max(roughness * roughness, HALF_MIN);
	float normalizationTerm = roughness * half(4.0) + half(2.0);
	float roughness2MinusOne = roughness2 - half(1.0);

	float3 lightDirectionWSFloat3 = float3(lightDirectionWS);
	float3 halfDir = Unity_SafeNormalize(lightDirectionWSFloat3 + float3(viewDirectionWS));

	float NoH = saturate(dot(float3(normalWS), halfDir));
	half LoH = half(saturate(dot(lightDirectionWSFloat3, halfDir)));
	float d = NoH * NoH * roughness2MinusOne + 1.00001f;

	half LoH2 = LoH * LoH;
	half specularTerm = roughness2 / ((d * d) * max(0.1h, LoH2) * normalizationTerm);
	return specularTerm;
}

void WaterSubSurfaceScatteringPBR(inout half4 color, UnityLight light, in WaterSurfaceData waterSurfaceData, in WaterInputData waterInput, half3 absorptionColor)
{
	half3 ambient = waterInput.bakedGI;
	half3 scatterColor = lerp(_ScatterColor, _ScatterColor + _FoamColor.rgb * 0.125f, waterInput.foamMask);

	half3 sss = 0;
	WaterSubSurfaceScatteringLightPBR(sss, scatterColor, light.dir, light.color, waterSurfaceData, waterInput);

	float scatterView = _ScatterViewIntensity * max(0, pow(dot(waterInput.viewDirectionWS, waterInput.waveNormalWS), 2.0));
	sss += ambient * scatterColor * (_ScatterAmbient + scatterView);
	sss *= _ScatterIntensity;
	sss *= 1 - absorptionColor;
	color.rgb += sss;
}

float3 SampleReflectionColor(in WaterSurfaceData waterSurfaceData, in WaterInputData waterInput)
{
	half3 reflVec = reflect(-waterInput.viewDirectionWS, waterInput.waveNormalWS);

	//Reflection
	Unity_GlossyEnvironmentData g;
	g.roughness = 0.05f;
	g.reflUVW = reflVec;

	half3 indirectSpecular = Unity_GlossyEnvironment(UNITY_PASS_TEXCUBE(unity_SpecCube0), unity_SpecCube0_HDR, g);

#if defined(_PLANAR_REFLECTION_ON)
	float2 planarUV = waterInput.normalizedScreenSpaceUV + waterInput.waveNormalWS.xz * _ReflectionDistortion;
#if UNITY_UV_STARTS_AT_TOP
	planarUV.y = 1.0f - planarUV.y;
#endif
	if (_ProjectionParams.x < 0)
		planarUV.y = 1-planarUV.y;
#if defined(UNITY_STEREO_INSTANCING_ENABLED)
	planarUV.x *= 0.5f;
	planarUV.x += 0.5 * (float)unity_StereoEyeIndex;
#endif

	half3 planarRefl = SAMPLE_TEXTURE2D(_PlanarReflections, linear_clamp_sampler, planarUV);
	half3 waterReflection = lerp(indirectSpecular, planarRefl, pow(abs(waterInput.normalWS.y), 5));
#else
	half3 waterReflection = indirectSpecular;
#endif
	return waterReflection;
}



void WaterReflectionPBR(inout half4 color, in WaterSurfaceData waterSurfaceData, in WaterInputData waterInput)
{
	half3 reflVec = reflect(-waterInput.viewDirectionWS, waterInput.waveNormalWS);
	half NdV = dot(waterInput.waveNormalWS, waterInput.viewDirectionWS);
    
	if (waterInput.frontFaceMask > 0.5)
	{
		half fresnelTerm = FresnelTerm(max(NdV, 0.0));
        
		color.rgb = lerp(color.rgb, waterInput.reflectionColor, saturate(fresnelTerm + _ReflectivityMin));
	}
	else
	{
		// Snell's Window
		float3 N = normalize(waterInput.waveNormalWS);
		float3 V = normalize(waterInput.viewDirectionWS);
        
		float3 refractedRay = refract(-V, N, 1.33);
		float isTIR = (dot(refractedRay, refractedRay) < 0.0001) ? 1.0 : 0.0;
        
		// Internal Fresnel
		float internalNdotV = saturate(dot(N, V));
		float internalFresnel = 0.02 + (1.0 - 0.02) * pow(1.0 - internalNdotV, 5.0);
        
		float reflectionFactor = max(isTIR, internalFresnel);
		float3 reflectionColor = waterInput.reflectionColor.rgb; 
		float reflectionDist = max(waterSurfaceData.fluidHeight, 1.0); 
		
		float4 absorptionColor = 0;
		WaterAbsorption(_WaterColor, reflectionDist, waterInput.fade, _AbsorptionDepthScale, absorptionColor);
		float3 fogColor = _ScatterColor.rgb * _ScatterAmbient; 
        
		reflectionColor = (reflectionColor * absorptionColor) + (fogColor * (1.0 - absorptionColor));

		color.rgb = lerp(color.rgb, reflectionColor, reflectionFactor);
	}
}


void WaterFoamPBR(inout half4 color, in UnityLight light, in WaterSurfaceData waterSurfaceData, in WaterInputData waterInput)
{
#if defined(_FOAMMAP) || defined(_FOAM_NORMALMAP) || defined(_SCREENSPACE_FOAMMASK_ON)
	waterInput.foamNormalWS = waterInput.foamNormalWS * waterInput.frontFaceMask;
	half3 ambient = ShadeSH9(half4(waterInput.foamNormalWS, 1.0));

	Unity_GlossyEnvironmentData g;
	g.roughness = 1;
	g.reflUVW = reflect(-waterInput.viewDirectionWS, waterInput.foamNormalWS);;

	UnityGI gi = (UnityGI)(0);
	gi.indirect.diffuse = ShadeSHPerPixel(waterInput.foamNormalWS, ambient, waterInput.positionWS.xyz);
	gi.indirect.specular = Unity_GlossyEnvironment(UNITY_PASS_TEXCUBE(unity_SpecCube0), unity_SpecCube0_HDR, g);


	half oneMinusReflectivity = 0;
	half3 foamSpec = 0;
	half3 foamColor = _FoamColor.rgb;

	float3 sampledColor = 0;

	float4 screenFoam = waterInput.screenspaceParticles;
#if defined(_FOAMMAP) || defined(_FOAM_NORMALMAP)
	sampledColor = waterSurfaceData.foamAlbedo.rgb;
#endif // _FOAMMAP

#if defined(_SCREENSPACE_FOAMMASK_ON)
	sampledColor += screenFoam.rgb;
#endif // _SCREENSPACE_FOAMMASK_ON

#if defined(_FOAMMODE_ALBEDO)
	foamColor *= sampledColor;
#endif // _FOAM_MODE_MASK
	
	half3 foamAlbedo = DiffuseAndSpecularFromMetallic(foamColor, 0.165, foamSpec, oneMinusReflectivity);
	light.color = _LightColor0 * waterInput.shadow;
	foamColor = BRDF1_Unity_PBS(foamAlbedo, foamSpec, oneMinusReflectivity, 0.75f, waterInput.foamNormalWS, waterInput.viewDirectionWS, light, gi.indirect);

	half foamAmount = WaterFoamAmount(waterInput.foamMask, waterSurfaceData.foamAlbedo, _FoamColor.a, screenFoam);
	color.rgb = lerp(color.rgb, foamColor, foamAmount);
#endif
}

void WaterSpecularPBR(inout half4 color, UnityLight light, in WaterSurfaceData waterSurfaceData, in WaterInputData waterInput)
{
	float NdotL = dot(waterInput.waveNormalWS, light.dir);
	float clampedNdotL = saturate(NdotL);
	color.rgb += unity_ColorSpaceDielectricSpec.rgb * NdotL * DirectBRDFSpecular(0.95f, waterInput.waveNormalWS, light.dir, waterInput.viewDirectionWS) * light.color * waterInput.shadow * _SpecularIntensity;
}

half4 WaterLightingPBR(in WaterSurfaceData waterSurfaceData, in WaterInputData waterInput)
{
	half4 color = float4(0, 0, 0, 1);

	UnityLight light;
	light.color = _LightColor0;
	light.dir = _WorldSpaceLightPos0.xyz;

	// Absorption/transmittance
	half4 absorptionColor;
	WaterAbsorption(waterInput, absorptionColor);

	// Refraction
	WaterRefraction(color, absorptionColor, waterSurfaceData, waterInput);

	// Reflection
	WaterReflectionPBR(color, waterSurfaceData, waterInput);
	if (waterInput.frontFaceMask > 0.0) 
	{
		WaterSpecularPBR(color, light, waterSurfaceData, waterInput);
	}

	// Apply SSS
	WaterSubSurfaceScatteringPBR(color, light, waterSurfaceData, waterInput, absorptionColor);

	// Apply foam
	WaterFoamPBR(color, light, waterSurfaceData, waterInput);

	// Apply Unity or Third-Party fog.
	ApplyFog(color, waterInput.positionWS.xyz, waterInput.normalizedScreenSpaceUV, waterInput.fogCoord);

	// Apply softening around edges (Fluid Depth fade)
	color.rgb = lerp(waterInput.refractionColor, color.rgb, waterInput.fade);

	return color;
}


#endif // FLUIDFRENZY_WATER_LIGHTING_BRP_INCLUDED