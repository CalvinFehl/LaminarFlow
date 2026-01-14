#ifndef FLUIDFRENZY_WATER_LIGHTING_INCLUDED
#define FLUIDFRENZY_WATER_LIGHTING_INCLUDED


half FresnelTerm(half cosTheta)
{
	return pow(max(0., 1.0 - cosTheta), 5);
}

void WaterAbsorption(in half4 waterColor, in half refractedDistance, in half waterFade, in half absorptionDepthScale, out half4 absorptionColor)
{
#if defined(_SCREENSPACE_REFRACTION_ABSORB)
	half3 extinction = (-log(0.02) * absorptionDepthScale * waterFade * waterColor.a) * max(1-waterColor.rgb, 0.01f);
	half3 absorption = exp(-refractedDistance * extinction);
	absorptionColor = half4(absorption, 1);
#elif defined(_SCREENSPACE_REFRACTION_ON)
	half absorptionFactor = 1 - exp(-refractedDistance * absorptionDepthScale);
	absorptionFactor = saturate(absorptionFactor * waterFade * waterColor.a);
	absorptionColor = half4(waterColor.rgb, absorptionFactor);
#elif defined(_SCREENSPACE_REFRACTION_ALPHA)
	half absorptionFactor = 1 - exp(-refractedDistance * absorptionDepthScale);
	absorptionColor = half4(waterColor.rgb, waterColor.a * saturate(absorptionFactor * waterFade));
#else
	absorptionColor = half4(waterColor.rgb, 1);
#endif
}

void WaterRefraction(in half4 absorptionColor, half4 refractionColor, out half4 output)
{
#if defined(_SCREENSPACE_REFRACTION_ABSORB)
	output = float4(refractionColor.rgb * absorptionColor.rgb, 1);
#elif defined(_SCREENSPACE_REFRACTION_ON)
	output = float4(lerp(refractionColor.rgb, absorptionColor.rgb, absorptionColor.a), 1);
#elif defined(_SCREENSPACE_REFRACTION_ALPHA)
	output = absorptionColor;
#else
	output = absorptionColor;
#endif
}

void WaterSubSurfaceScatteringLightPBR(in half3 scatterColor, in half3 lightDir, in half3 lightColor, in half3 viewDirectionWS, in half3 normalWS, in half scatterViewIntensity, in half scatterLightIntensity, in half shadow, in half foam, out half3 sss)
{
	half scatterFactor = scatterFactor = pow(max(0.0, dot(lightDir, -viewDirectionWS)), 4.0);
	scatterFactor *= scatterLightIntensity * pow(max(0.0, 0.5 - 0.5 * dot(lightDir, normalWS)), 3.0);

	sss = (lightColor * shadow * scatterColor * scatterFactor);
	sss += lightColor * shadow * scatterColor * foam ;
}

#if defined(FLUIDFRENZY_WATER_INPUT_INCLUDED)

void WaterAbsorption(in WaterInputData waterInput, out half4 absorptionColor)
{
	WaterAbsorption(_WaterColor, waterInput.refractedDistance, waterInput.fade, _AbsorptionDepthScale, absorptionColor);
}

void WaterRefraction(inout half4 color, in half4 absorptionColor, in WaterSurfaceData waterSurfaceData, in WaterInputData waterInput)
{
	if (waterInput.frontFaceMask > 0.5)
	{
		// Above water
		WaterRefraction(absorptionColor, waterInput.refractionColor, color);
	}
	else
	{
		// Snell's Window
		color.rgb = waterInput.refractionColor;
		color.a = 1.0;
	}
}

void WaterSubSurfaceScatteringLightPBR(inout half3 color, in half3 scatterColor, in half3 lightDir, in half3 lightColor, in WaterSurfaceData waterSurfaceData, in WaterInputData waterInput)
{
	half3 sss = 0;
	half foamScatter = waterInput.foamMask * _ScatterFoamIntensity;
	WaterSubSurfaceScatteringLightPBR(scatterColor, lightDir, lightColor, waterInput.viewDirectionWS, waterInput.waveNormalWS, _ScatterViewIntensity, _ScatterLightIntensity, waterInput.shadow, foamScatter, sss);
	color += sss;
}
#endif


#endif // FLUIDFRENZY_WATER_LIGHTING_INCLUDED