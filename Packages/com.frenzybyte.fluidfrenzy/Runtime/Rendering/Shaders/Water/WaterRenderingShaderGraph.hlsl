#ifndef FLUIDFRENZY_WATER_RENDERING_SHADERGRAPH_INCLUDED
#define FLUIDFRENZY_WATER_RENDERING_SHADERGRAPH_INCLUDED

#include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Rendering/Shaders/Library/FluidRenderingCommon.hlsl"
#include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Rendering/Shaders/Library/FluidRenderingHelpers.hlsl"
#include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Rendering/Shaders/Water/WaterInputData.hlsl"
#include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Rendering/Shaders/Water/WaterSurfaceData.hlsl"
#include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Rendering/Shaders/Water/WaterRenderingCommon.hlsl"
#include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Rendering/Shaders/Water/WaterLighting.hlsl"


void WaterFoamAmount_float(in float foamMask, in float4 foamAlbedo, in float foamAlpha, in float4 screenSpaceFoam, out float foamAmount)
{
	foamAmount = WaterFoamAmount(foamMask, foamAlbedo, foamAlpha, screenSpaceFoam);
}

void ComputeWaterAbsorption_float(in float4 waterColor, in float refractedDistance, in float waterFade, in float absorptionDepthScale, out float4 absorptionColor)
{
	WaterAbsorption(waterColor,  refractedDistance,  waterFade, absorptionDepthScale, absorptionColor);
}

void WaterRefraction_float(in float4 absorptionColor, float4 sceneColor, out float4 output)
{
	WaterRefraction(absorptionColor, sceneColor, output);
}

void WaterSubSurfaceScatteringLightPBR_float(in float3 scatterColor, in float3 lightDir, in float3 lightColor, in float shadow, in float3 viewDirectionWS, in float3 normalWS, in float scatterViewIntensity, in float scatterLightIntensity, in float foam, out float3 sss)
{
	WaterSubSurfaceScatteringLightPBR(scatterColor, lightDir, lightColor, viewDirectionWS, normalWS, scatterViewIntensity, scatterLightIntensity, shadow, foam, sss);
}

void WaterFoamAmount_half(in half foamMask, in half4 foamAlbedo, in half foamAlpha, in half4 screenSpaceFoam, out half foamAmount)
{
	foamAmount = WaterFoamAmount(foamMask, foamAlbedo, foamAlpha, screenSpaceFoam);
}

void ComputeWaterAbsorption_half(in half4 waterColor, in half refractedDistance, in half waterFade, in half absorptionDepthScale, out half4 absorptionColor)
{
	WaterAbsorption(waterColor,  refractedDistance,  waterFade, absorptionDepthScale, absorptionColor);
}

void WaterRefraction_half(in half4 absorptionColor, half4 sceneColor, out half4 output)
{
	WaterRefraction(absorptionColor, sceneColor, output);
}

void WaterSubSurfaceScatteringLightPBR_half(in half3 scatterColor, in half3 lightDir, in half3 lightColor, in half shadow, in half3 viewDirectionWS, in half3 normalWS, in half scatterViewIntensity, in half scatterLightIntensity, in half foam, out half3 sss)
{
	WaterSubSurfaceScatteringLightPBR(scatterColor, lightDir, lightColor, viewDirectionWS, normalWS, scatterViewIntensity, scatterLightIntensity, shadow, foam, sss);
}

#endif // FLUIDFRENZY_WATER_RENDERING_SHADERGRAPH_INCLUDED