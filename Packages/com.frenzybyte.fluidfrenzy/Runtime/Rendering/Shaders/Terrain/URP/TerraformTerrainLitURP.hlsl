#ifndef FLUIDFRENZY_TERRAFORM_TERRAIN_LIT_URP_INCLUDED
#define FLUIDFRENZY_TERRAFORM_TERRAIN_LIT_URP_INCLUDED

#include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Rendering/Shaders/Terrain/TerraformTerrainSurfaceData.hlsl"

inline float3 SampleTerrainNormal(float2 uv)
{
	float3 normal = 0;
#ifdef TERRAFORM_TEXTURE_ARRAY
	float4 heightSample = SampleTerrain(uv);
	float4 spat = UNITY_SAMPLE_TEX2D_SAMPLER(_Splatmap, _Splatmap, uv);
	spat.rgba /= dot(spat.rgba, 1);

	float4 baseLayer_ST[4] = {_BaseLayer0_ST, _BaseLayer1_ST, _BaseLayer2_ST, _BaseLayer3_ST};
	float3 baseNormal = 0;

	[unroll]
	for (int i = 0; i < 4; i++)
	{
		float splatWeight = spat[i];
		if (splatWeight < 0.001)
			continue;

		float4 baseLayer_UV = float4(uv * baseLayer_ST[i].xy, i, 0);
		float3 normalSample = UnpackNormalScale(UNITY_SAMPLE_TEX2DARRAY_SAMPLER(_BaseLayerBumpMap, _BaseLayerBumpMap, baseLayer_UV), _BaseLayerBumpScale[i]);

		baseNormal += normalSample * splatWeight;
	}

	float4 dynamicLayer_ST[3] = {_DynamicLayer0_ST, _DynamicLayer1_ST, _DynamicLayer2_ST};
	float3 materialNormal[4];
	materialNormal[0] = baseNormal;

	[loop]
	for (i = 0; i < 3; i++)
	{
		float4 dynamicLayer_UV = float4(uv * dynamicLayer_ST[i].xy, i, 0);
		float3 normalSample = UnpackNormalScale(
			UNITY_SAMPLE_TEX2DARRAY_SAMPLER(_DynamicLayerBumpMap, _DynamicLayerBumpMap, dynamicLayer_UV), 
			_DynamicLayerBumpScale[i]
		);

		materialNormal[i + 1] = normalSample;
	}

	float remainingThickness = 1.0f;
	float materialThickness = 1.0f;
	float thickness = min(materialThickness, heightSample.z);
	float thicknessFactor = thickness / materialThickness;
	for(i = 3; i >= 0; i--) 
	{
		float layerRemainingThickness = remainingThickness * materialThickness;
		float thickness = min(layerRemainingThickness, heightSample[i]);
		float thicknessFactor = thickness/materialThickness;
		normal += materialNormal[i] * thickness;
		remainingThickness -= thicknessFactor;
	}

#else
	float4 heightSample = SampleTerrain(uv);
	float4 spat = SAMPLE_TEXTURE2D(_Splatmap, sampler_Splatmap, uv);
	spat.rgb /= dot(spat.rgb, 1);
	float3 normalLayer0 = UnpackNormalScale(SAMPLE_TEXTURE2D(_Layer0BumpMap, sampler_Layer0BumpMap, uv * _Layer0Albedo_ST.xy), _Layer0BumpScale) * spat.r;
	float3 normalLayer1 = UnpackNormalScale(SAMPLE_TEXTURE2D(_Layer1BumpMap, sampler_Layer0BumpMap, uv * _Layer1Albedo_ST.xy), _Layer1BumpScale) * spat.g;
	float3 bottomNormal = normalLayer0 + normalLayer1;
	float3 topNormal = UnpackNormalScale(SAMPLE_TEXTURE2D(_TopLayerBumpMap, sampler_Layer0BumpMap, uv * _TopLayerAlbedo_ST.xy), _TopLayerBumpScale);

	float remainingThickness = 1.0;
	float materialThickness = 1;
	float thickness = min(materialThickness, heightSample.y);
	float thicknessFactor = thickness / materialThickness;

	normal += topNormal * thicknessFactor;
	remainingThickness -= thicknessFactor;
	normal += bottomNormal * remainingThickness;
#endif
	return normal;
}

inline void InitializeStandardLitSurfaceData(float2 uv, out SurfaceData outSurfaceData)
{
	TerrainSurfaceData terrainData = GetTerrainSurfaceData(uv);
	outSurfaceData.alpha = 1;
	outSurfaceData.albedo = terrainData.albedo.rgb;
	outSurfaceData.specular = half3(0.0, 0.0, 0.0);;
	outSurfaceData.metallic = terrainData.metallic;
	outSurfaceData.smoothness = terrainData.smoothness;
	outSurfaceData.normalTS = terrainData.normal;
	outSurfaceData.occlusion = terrainData.occlusion;

	outSurfaceData.emission = 0;
	outSurfaceData.clearCoatMask = half(0.0);
	outSurfaceData.clearCoatSmoothness = half(0.0);
}



#endif // FLUIDFRENZY_TERRAFORM_TERRAIN_LIT_URP_INCLUDED