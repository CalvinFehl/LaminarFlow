#ifndef FLUIDFRENZY_WATER_RENDERING_COMMON_INCLUDED
#define FLUIDFRENZY_WATER_RENDERING_COMMON_INCLUDED


float WaterFoamAmount(float foamMask, float4 foamAlbedo, float foamAlpha, float4 screenSpaceFoam)
{
	float softFoamMask = smoothstep(0.25f, 1, foamMask);
	float oneMinusFoamMask = 1 - foamMask;
	float oneMinusFoamAlpha = 1-foamAlpha;
	half foamAmount = 0;
#if defined(_FOAMMODE_ALBEDO)
	foamAmount = saturate((foamAlbedo.r * foamAlpha) - oneMinusFoamMask);
	foamAmount = lerp(foamMask * foamAlbedo.a * 0.5f, foamAmount, softFoamMask) + screenSpaceFoam.r;

#elif defined(_FOAMMODE_MASK)
	float maskRange = 1.0f / 3.0f;
	float overlap = 0.0f;
	float weightR = smoothstep(1 - maskRange + overlap, 1, foamMask);
	float weightG = smoothstep(maskRange+ overlap, 1 - maskRange - overlap, foamMask * (1-weightR ));
	float weightB = smoothstep(0, maskRange - overlap, foamMask * (1-weightG ));
	
	float sampledFoamMask = saturate(length(foamAlbedo.rgb * float3(weightR, weightG, weightB)));	

	foamAmount = max(saturate(sampledFoamMask - oneMinusFoamAlpha),  saturate(screenSpaceFoam.r - oneMinusFoamAlpha));	
#elif defined(_FOAMMODE_CLIP)
	float sampledFoamMask = (foamAlbedo.r);
	foamAmount = max(saturate(sampledFoamMask - oneMinusFoamAlpha - oneMinusFoamMask),  saturate(screenSpaceFoam.r - oneMinusFoamAlpha));
#endif

	return saturate(foamAmount);
}

#endif // FLUIDFRENZY_WATER_RENDERING_COMMON_INCLUDED