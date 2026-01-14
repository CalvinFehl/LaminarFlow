#ifndef FLUIDFRENZY_WATER_LIGHTING_URP_INCLUDED
#define FLUIDFRENZY_WATER_LIGHTING_URP_INCLUDED

#include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Rendering/Shaders/Water/WaterLighting.hlsl"
#include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Rendering/Shaders/Water/WaterRenderingCommon.hlsl"

#if UNITY_VERSION <= 202110
#define LIGHT_LOOP_BEGIN(lightCount) \
for (uint lightIndex = 0u; lightIndex < lightCount; ++lightIndex) {

#define LIGHT_LOOP_END }
#endif

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
	float3 halfDir = SafeNormalize(lightDirectionWSFloat3 + float3(viewDirectionWS));

	float NoH = saturate(dot(float3(normalWS), halfDir));
	half LoH = half(saturate(dot(lightDirectionWSFloat3, halfDir)));
	float d = NoH * NoH * roughness2MinusOne + 1.00001f;

	half LoH2 = LoH * LoH;
	half specularTerm = roughness2 / ((d * d) * max(0.1h, LoH2) * normalizationTerm);
	return specularTerm;
}

half3 SampleReflectionColor(in WaterSurfaceData waterSurfaceData, in WaterInputData waterInput)
{
	half3 reflectVector = reflect(-waterInput.viewDirectionWS, waterInput.waveNormalWS);

	half3 indirectSpecular = CalculateIrradianceFromReflectionProbes(
		reflectVector, 
		waterInput.positionWS, 
		0.05f, 
		waterInput.normalizedScreenSpaceUV
	);

#if defined(_PLANAR_REFLECTION_ON)
	float2 planarUV = waterInput.normalizedScreenSpaceUV + waterInput.waveNormalWS.xz * _ReflectionDistortion;

	// Handle Stereo/VR
#if defined(UNITY_STEREO_INSTANCING_ENABLED)
	planarUV.x *= 0.5f;
	planarUV.x += 0.5 * (float)unity_StereoEyeIndex;
#endif

	half3 planarRefl = SAMPLE_TEXTURE2D(_PlanarReflections, linear_clamp_sampler, planarUV).rgb;
	half3 waterReflection = lerp(indirectSpecular, planarRefl, pow(abs(waterInput.normalWS.y), 5));
#else
	half3 waterReflection = indirectSpecular;
#endif

	return waterReflection;
}

void WaterReflectionPBR(inout half4 color, in WaterSurfaceData waterSurfaceData, in WaterInputData waterInput)
{

	half NdV = dot(waterInput.waveNormalWS, waterInput.viewDirectionWS);

	// 2. Apply Directional Logic (Front Face vs Underwater/Back Face)
	if (waterInput.frontFaceMask > 0.5)
	{
		// --- Surface Reflection ---
		half fresnelTerm = FresnelTerm(max(NdV, 0.0));
        
		// We use our calculated 'envReflection' here
		color.rgb = lerp(color.rgb, waterInput.reflectionColor, saturate(fresnelTerm + _ReflectivityMin));
	}
	else
	{
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
		float3 fogColor = _ScatterColor.rgb * 0.1f; 
        
		reflectionColor = (reflectionColor * absorptionColor) + (fogColor * (1.0 - absorptionColor));

		color.rgb = lerp(color.rgb, reflectionColor, reflectionFactor);
	}
}

inline void InitializeFoamBRDFData(inout WaterSurfaceData waterSurfaceData, in WaterInputData waterInput, out BRDFData brdfData)
{
	float3 sampledColor = 0;
	half3 foamColor = _FoamColor.rgb;

#if defined(_FOAMMAP) || defined(_FOAM_NORMALMAP)
	sampledColor = waterSurfaceData.foamAlbedo.rgb;
#endif // _FOAMMAP

	sampledColor += waterInput.screenspaceParticles.rgb;

#if defined(_FOAMMODE_ALBEDO)
	foamColor *= sampledColor;
#endif // _FOAM_MODE_MASK
	
	float metallic = 0.165;
	float smoothness = 0.5f;
	float alpha = 1;
	half3 specular = 0;
	InitializeBRDFData(foamColor.rgb, metallic, specular, smoothness, alpha, brdfData);
}

void WaterSpecularPBR(inout half4 color, Light light, in WaterSurfaceData waterSurfaceData, in WaterInputData waterInput)
{
	float NdotL = dot(waterInput.waveNormalWS, light.direction);
	float clampedNdotL = saturate(NdotL);
	color.rgb += kDielectricSpec.rgb * NdotL * DirectBRDFSpecular(0.95f, waterInput.waveNormalWS, light.direction, waterInput.viewDirectionWS) * light.color * light.distanceAttenuation * light.shadowAttenuation * _SpecularIntensity;
}

void ProcessLightsPBR(inout half4 color, in WaterSurfaceData waterSurfaceData, in WaterInputData waterInput, in half4 absorptionColor)
{
	#define inputData waterInput
	BRDFData foamBrdfData;
	InitializeFoamBRDFData(waterSurfaceData, waterInput, foamBrdfData);
	Light mainLight = GetMainLight(waterInput.shadowCoord, waterInput.positionWS, 1);

#if UNITY_VERSION >= 202210
	uint meshRenderingLayers = GetMeshRenderingLayer();
#elif UNITY_VERSION >= 202110
	uint meshRenderingLayers = GetMeshRenderingLightLayer();
#else
	uint meshRenderingLayers = 0;
#endif
	half3 scatterColor = lerp(_ScatterColor.rgb, _ScatterColor.rgb + _FoamColor.rgb * 0.125f, waterInput.foamMask);

	half3 sss = 0;
	half4 specularColor = 0;
	half3 foamColor = GlobalIllumination(foamBrdfData, waterInput.bakedGI, 1, waterInput.foamNormalWS, waterInput.viewDirectionWS);
#ifdef _LIGHT_LAYERS
	if (IsMatchingLightLayer(mainLight.layerMask, meshRenderingLayers))
#endif
	{
		// Gather specular BRDF Lighting.
		WaterSpecularPBR(specularColor, mainLight, waterSurfaceData, waterInput);
		// Gather sub surface scattering.
		WaterSubSurfaceScatteringLightPBR(sss, scatterColor, mainLight.direction, mainLight.color * mainLight.distanceAttenuation * mainLight.shadowAttenuation, waterSurfaceData, waterInput);
		// Gather foam,
		foamColor += LightingPhysicallyBased(foamBrdfData, mainLight, waterInput.foamNormalWS, waterInput.viewDirectionWS, false);
	}

#if defined(_ADDITIONAL_LIGHTS)
	uint pixelLightCount = GetAdditionalLightsCount();

#if USE_FORWARD_PLUS || USE_CLUSTER_LIGHT_LOOP
	[loop] for (uint lightIndex = 0; lightIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); lightIndex++)
	{
#if UNITY_VERSION < 60010000
		FORWARD_PLUS_SUBTRACTIVE_LIGHT_CHECK
#else
		CLUSTER_LIGHT_LOOP_SUBTRACTIVE_LIGHT_CHECK
#endif

		Light light = GetAdditionalLight(lightIndex, waterInput.positionWS, 0);

#ifdef _LIGHT_LAYERS
		if (IsMatchingLightLayer(light.layerMask, meshRenderingLayers))
#endif
		{
			// Gather specular BRDF Lighting.
			WaterSpecularPBR(specularColor, light, waterSurfaceData, waterInput);
			// Gather sub surface scattering.
			WaterSubSurfaceScatteringLightPBR(sss, scatterColor, light.direction, light.color * light.distanceAttenuation * light.shadowAttenuation, waterSurfaceData, waterInput);
			// Gather foam,
			foamColor += LightingPhysicallyBased(foamBrdfData, light, waterInput.foamNormalWS, waterInput.viewDirectionWS, false);
		}
	}
#else
#if USE_CLUSTERED_LIGHTING
	for (uint lightIndex = 0; lightIndex < min(_AdditionalLightsDirectionalCount, MAX_VISIBLE_LIGHTS); lightIndex++)
	{
		Light light = GetAdditionalLight(lightIndex, waterInput.positionWS, 0);

#ifdef _LIGHT_LAYERS
		if (IsMatchingLightLayer(light.layerMask, meshRenderingLayers))
#endif
		{
			// Gather specular BRDF Lighting.
			WaterSpecularPBR(specularColor, light, waterSurfaceData, waterInput);
			// Gather sub surface scattering.
			WaterSubSurfaceScatteringLightPBR(sss, scatterColor, light.direction, light.color * light.distanceAttenuation * light.shadowAttenuation, waterSurfaceData, waterInput);
			// Gather foam,
			foamColor += LightingPhysicallyBased(foamBrdfData, light, waterInput.foamNormalWS, waterInput.viewDirectionWS, false);
		}
	}
#endif
#endif

	LIGHT_LOOP_BEGIN(pixelLightCount)
		Light light = GetAdditionalLight(lightIndex, waterInput.positionWS, 0);
#ifdef _LIGHT_LAYERS
	if (IsMatchingLightLayer(light.layerMask, meshRenderingLayers))
#endif
	{
		// Gather specular BRDF Lighting.
		WaterSpecularPBR(specularColor, light, waterSurfaceData, waterInput);
		// Gather sub surface scattering.
		WaterSubSurfaceScatteringLightPBR(sss, scatterColor, light.direction, light.color * light.distanceAttenuation * light.shadowAttenuation, waterSurfaceData, waterInput);
		// Gather foam,
		foamColor += LightingPhysicallyBased(foamBrdfData, light, waterInput.foamNormalWS, waterInput.viewDirectionWS, false);
	}
	LIGHT_LOOP_END
#endif


	float scatterView = _ScatterViewIntensity * max(0, pow(dot(waterInput.viewDirectionWS, waterInput.waveNormalWS), 2.0));

	// Apply sub surface scattering.
	half3 ambient = waterInput.bakedGI;
	sss += ambient * scatterColor * (_ScatterAmbient + scatterView);
	sss *= _ScatterIntensity;
	sss *= 1 - absorptionColor.rgb;
	color.rgb += sss;

	// Apply specular BRDF Lighting.
	color.rgb += specularColor.rgb;

	// Apply foam
	half foamAmount = WaterFoamAmount(waterInput.foamMask, waterSurfaceData.foamAlbedo, _FoamColor.a, waterInput.screenspaceParticles);
	color.rgb = lerp(color.rgb, foamColor, foamAmount);
}

half4 WaterLightingPBR(in WaterSurfaceData waterSurfaceData, in WaterInputData waterInput)
{
	half4 color = float4(0, 0, 0, 1);

	Light mainLight = GetMainLight(waterInput.shadowCoord, waterInput.positionWS, 1);

	// Apply absorption and refraction.
	half4 absorptionColor;
	WaterAbsorption(waterInput, absorptionColor);
	WaterRefraction(color, absorptionColor, waterSurfaceData, waterInput);

	// Apply fresnel based reflection
	WaterReflectionPBR(color, waterSurfaceData, waterInput);

	ProcessLightsPBR(color, waterSurfaceData, waterInput, absorptionColor);

	// Apply softening around edges where the water is shallow in fluid space.
	color.rgb = lerp(waterInput.refractionColor.rgb, color.rgb, waterInput.fade);

	return color;
}


#endif // FLUIDFRENZY_WATER_LIGHTING_URP_INCLUDED