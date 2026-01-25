#ifndef FLUIDFRENZY_WATER_LIGHTING_URP_INCLUDED
#define FLUIDFRENZY_WATER_LIGHTING_URP_INCLUDED

#include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Rendering/Shaders/Water/WaterLighting.hlsl"

void ProcessLightsPBR(inout half4 color, in PositionInputs posInput, in SurfaceData surfaceData, in BuiltinData builtinData, BSDFData bsdfData, PreLightData preLightData, in WaterSurfaceData waterSurfaceData, in WaterInputData waterInput, in half4 absorptionColor)
{
	uint featureFlags = LIGHT_FEATURE_MASK_FLAGS_TRANSPARENT;
    
	LightLoopOutput lightLoopOutput;
	LightLoop(waterInput.viewDirectionWS, posInput, preLightData, bsdfData, builtinData, featureFlags, lightLoopOutput);

	float3 diffuseLighting = lightLoopOutput.diffuseLighting * GetCurrentExposureMultiplier();
	float3 specularLighting = lightLoopOutput.specularLighting * GetCurrentExposureMultiplier();

	if (waterInput.frontFaceMask < 0.0) // Camera is Underwater
	{
		float3 N = waterInput.waveNormalWS * waterInput.frontFaceMask; 
		float3 V = waterInput.viewDirectionWS;
        
		float3 refractedRay = refract(-V, N, 1.33);
		float isTIR = (dot(refractedRay, refractedRay) < 0.0001) ? 1.0 : 0.0;
        
		float internalNdotV = saturate(dot(N, -V));
		float internalFresnel = 0.02 + (1.0 - 0.02) * pow(1.0 - internalNdotV, 5.0);
		float reflectionFactor = max(isTIR, internalFresnel);

		float reflectionDist = max(waterSurfaceData.fluidHeight, 1.0); 
		float4 refrAbsorption;
		WaterAbsorption(_WaterColor, reflectionDist, waterInput.fade, _AbsorptionDepthScale, refrAbsorption);
        
		float3 reflectionFogColor = _ScatterColor.rgb * 0.1f * GetCurrentExposureMultiplier(); 
        
		float3 underwaterReflectionColor = (specularLighting * refrAbsorption.rgb) + (reflectionFogColor * (1.0 - refrAbsorption.rgb));
		color.rgb = lerp(color.rgb, underwaterReflectionColor, reflectionFactor);
        
		color.rgb += diffuseLighting;
		absorptionColor = 1;
	}
	else 
	{
		color.rgb += diffuseLighting + specularLighting;
	}

	// Subsurface scattering
	half3 scatterColor = lerp(_ScatterColor.rgb, _ScatterColor.rgb + _FoamColor.rgb * 0.125f * GetCurrentExposureMultiplier(), waterInput.foamMask);

	half3 sss = 0;
	uint i = 0; // Declare once to avoid the D3D11 compiler warning.
	LightLoopContext context;
	context.shadowContext    = InitShadowContext();
	context.shadowValue      = 1;
	context.sampleReflection = 0;
#ifdef APPLY_FOG_ON_SKY_REFLECTIONS
	context.positionWS       = posInput.positionWS;
#endif

	// Initialize the contactShadow and contactShadowFade fields
	InitContactShadow(posInput, context);

	// First of all we compute the shadow value of the directional light to reduce the VGPR pressure
	if (featureFlags & LIGHTFEATUREFLAGS_DIRECTIONAL)
	{
		// Evaluate sun shadows.
		if (_DirectionalShadowIndex >= 0)
		{
			DirectionalLightData light = _DirectionalLightDatas[_DirectionalShadowIndex];

#if defined(SCREEN_SPACE_SHADOWS_ON) && !defined(_SURFACE_TYPE_TRANSPARENT)
			if (UseScreenSpaceShadow(light, bsdfData.normalWS))
			{
				context.shadowValue = GetScreenSpaceColorShadow(posInput, light.screenSpaceShadowIndex).SHADOW_TYPE_SWIZZLE;
			}
			else
#endif
			{
				// TODO: this will cause us to load from the normal buffer first. Does this cause a performance problem?
				float3 L = -light.forward;
				float3 V = waterInput.viewDirectionWS;
				// Is it worth sampling the shadow map?
				if ((light.lightDimmer > 0) && (light.shadowDimmer > 0) && // Note: Volumetric can have different dimmer, thus why we test it here
					IsNonZeroBSDF(V, L, preLightData, bsdfData) &&
					!ShouldEvaluateThickObjectTransmission(V, L, preLightData, bsdfData, light.shadowIndex))
				{
					context.shadowValue = GetDirectionalShadowAttenuation(context.shadowContext,
																			posInput.positionSS, posInput.positionWS, GetNormalForShadowBias(bsdfData),
																			light.shadowIndex, L);
				}
			}
		}
	}


	if (featureFlags & LIGHTFEATUREFLAGS_PUNCTUAL)
	{
		uint lightCount, lightStart;

#ifndef LIGHTLOOP_DISABLE_TILE_AND_CLUSTER
		GetCountAndStart(posInput, LIGHTCATEGORY_PUNCTUAL, lightStart, lightCount);
#else   // LIGHTLOOP_DISABLE_TILE_AND_CLUSTER
		lightCount = _PunctualLightCount;
		lightStart = 0;
#endif

		bool fastPath = false;
	#if SCALARIZE_LIGHT_LOOP
		uint lightStartLane0;
		fastPath = IsFastPath(lightStart, lightStartLane0);

		if (fastPath)
		{
			lightStart = lightStartLane0;
		}
	#endif

		// Scalarized loop. All lights that are in a tile/cluster touched by any pixel in the wave are loaded (scalar load), only the one relevant to current thread/pixel are processed.
		// For clarity, the following code will follow the convention: variables starting with s_ are meant to be wave uniform (meant for scalar register),
		// v_ are variables that might have different value for each thread in the wave (meant for vector registers).
		// This will perform more loads than it is supposed to, however, the benefits should offset the downside, especially given that light data accessed should be largely coherent.
		// Note that the above is valid only if wave intriniscs are supported.
		uint v_lightListOffset = 0;
		uint v_lightIdx = lightStart;

#if NEED_TO_CHECK_HELPER_LANE
		// On some platform helper lanes don't behave as we'd expect, therefore we prevent them from entering the loop altogether.
		// IMPORTANT! This has implications if ddx/ddy is used on results derived from lighting, however given Lightloop is called in compute we should be
		// sure it will not happen.
		bool isHelperLane = WaveIsHelperLane();
		while (!isHelperLane && v_lightListOffset < lightCount)
#else
		while (v_lightListOffset < lightCount)
#endif
		{
			v_lightIdx = FetchIndex(lightStart, v_lightListOffset);
#if SCALARIZE_LIGHT_LOOP
			uint s_lightIdx = ScalarizeElementIndex(v_lightIdx, fastPath);
#else
			uint s_lightIdx = v_lightIdx;
#endif
			if (s_lightIdx == -1)
				break;

			LightData s_lightData = FetchLight(s_lightIdx);

			// If current scalar and vector light index match, we process the light. The v_lightListOffset for current thread is increased.
			// Note that the following should really be ==, however, since helper lanes are not considered by WaveActiveMin, such helper lanes could
			// end up with a unique v_lightIdx value that is smaller than s_lightIdx hence being stuck in a loop. All the active lanes will not have this problem.
			if (s_lightIdx >= v_lightIdx)
			{
				v_lightListOffset++;
				if (IsMatchingLightLayer(s_lightData.lightLayers, builtinData.renderingLayers))
				{
					float3 L;
					float4 distances; // {d, d^2, 1/d, d_proj}
					GetPunctualLightVectors(posInput.positionWS, s_lightData, L, distances);
					float4 lightColor = EvaluateLight_Punctual(context, posInput, s_lightData, L, distances);
					WaterSubSurfaceScatteringLightPBR(sss, scatterColor, L, lightColor.rgb, waterSurfaceData, waterInput);
				}
			}
		}
	}

	if (featureFlags & LIGHTFEATUREFLAGS_DIRECTIONAL)
	{
		for (i = 0; i < _DirectionalLightCount; ++i)
		{
			if (IsMatchingLightLayer(_DirectionalLightDatas[i].lightLayers, builtinData.renderingLayers))
			{
				WaterSubSurfaceScatteringLightPBR(sss, scatterColor, -_DirectionalLightDatas[i].forward, _DirectionalLightDatas[i].color * context.shadowValue, waterSurfaceData, waterInput);
			}
		}
	}

	float scatterView = _ScatterViewIntensity * max(0, pow(dot(waterInput.viewDirectionWS, waterInput.waveNormalWS), 2.0));

	sss *= GetCurrentExposureMultiplier();
	sss += scatterColor * (_ScatterAmbient + scatterView);
	sss *= 1 - absorptionColor.rgb;
	sss *= _ScatterIntensity;
	color.rgb += sss;
}

half4 WaterLightingPBR(FragInputs input, in FluidInputData fluidInput, in WaterSurfaceData waterSurfaceData, in WaterInputData waterInput)
{
	float4 color = float4(0, 0, 0, 1);

	uint2 tileIndex = uint2(input.positionSS.xy) / GetTileSize();

	PositionInputs posInput = GetPositionInput(input.positionSS.xy, _ScreenSize.zw, input.positionSS.z, input.positionSS.w, input.positionRWS.xyz, tileIndex);
	SurfaceData surfaceData;
	BuiltinData builtinData;
	GetSurfaceAndBuiltinData(input, waterInput.viewDirectionWS, posInput, surfaceData, builtinData, fluidInput, waterSurfaceData, waterInput);

	BSDFData bsdfData = ConvertSurfaceDataToBSDFData(input.positionSS.xy, surfaceData);

	PreLightData preLightData = GetPreLightData(waterInput.viewDirectionWS, posInput, bsdfData);

	// Apply absorption and refraction.
	half4 absorptionColor;
	WaterAbsorption(waterInput, absorptionColor);
	WaterRefraction(color, absorptionColor, waterSurfaceData, waterInput);

	ProcessLightsPBR(color, posInput, surfaceData, builtinData, bsdfData, preLightData, waterSurfaceData, waterInput, absorptionColor);

	// Apply softening around edges where the water is shallow in fluid space.
	color.rgb = lerp(waterInput.refractionColor.rgb, color.rgb, waterInput.fade);

	if(!ApplyFog(color, GetAbsolutePositionWS(posInput.positionWS.xyz), waterInput.normalizedScreenSpaceUV, 0))
	{
		#ifdef _ENABLE_FOG_ON_TRANSPARENT
			color = EvaluateAtmosphericScattering(posInput, waterInput.viewDirectionWS, color);
		#endif
	}

	color.a = builtinData.opacity;


	return color;
}


#endif // FLUIDFRENZY_WATER_LIGHTING_URP_INCLUDED