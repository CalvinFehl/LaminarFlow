#ifndef FLUIDFRENZY_TERRAFORM_TERRAIN_SURFACEDATA_INCLUDED
#define FLUIDFRENZY_TERRAFORM_TERRAIN_SURFACEDATA_INCLUDED

#include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Rendering/Shaders/Terrain/TerraformTerrainCommon.hlsl"

TerrainSurfaceData GetTerrainSurfaceData(float2 uv)
{
#ifdef TERRAFORM_TEXTURE_ARRAY 
	return GetTerrainSurfaceData(uv, 
		_BaseLayerAlbedo, sampler_BaseLayerAlbedo,
		_BaseLayerMaskMap, sampler_BaseLayerMaskMap,
		_BaseLayerBumpMap, sampler_BaseLayerBumpMap,
		_BaseLayer0_ST,
		_BaseLayer1_ST,
		_BaseLayer2_ST,
		_BaseLayer3_ST,
		_BaseLayerColor0,
		_BaseLayerColor1,
		_BaseLayerColor2,
		_BaseLayerColor3,
		_BaseLayerBumpScale,

		_DynamicLayerAlbedo, sampler_DynamicLayerAlbedo,
		_DynamicLayerMaskMap, sampler_DynamicLayerMaskMap,
		_DynamicLayerBumpMap, sampler_DynamicLayerBumpMap,

		_DynamicLayer0_ST,
		_DynamicLayer1_ST,
		_DynamicLayer2_ST,
		
		_DynamicLayerBumpScale
	);

#else
	return GetTerrainSurfaceData(uv,
		_Layer0Albedo, sampler_Layer0Albedo,
		_Layer0MaskMap, sampler_Layer0MaskMap,
		_Layer0BumpMap, sampler_Layer0BumpMap,
		_Layer0Color,
		_Layer0Albedo_ST,
		_Layer0BumpScale,

		_Layer1Albedo,
		_Layer1MaskMap,
		_Layer1BumpMap,
		_Layer1Color,
		_Layer1Albedo_ST,
		_Layer1BumpScale,

#if !defined(UNITY_PLATFORM_WEBGL)
		_Layer2Albedo,
		_Layer2MaskMap,
		_Layer2BumpMap,
		_Layer2Color,
		_Layer2Albedo_ST,
		_Layer2BumpScale,

		_Layer3Albedo,
		_Layer3MaskMap,
		_Layer3BumpMap,
		_Layer3Color,
		_Layer3Albedo_ST,
		_Layer3BumpScale,
#endif

		_TopLayerAlbedo,
		_TopLayerMaskMap,
		_TopLayerBumpMap,
		_TopLayerAlbedo_ST,
		_TopLayerBumpScale
	);
#endif
}
			
#endif // FLUIDFRENZY_TERRAFORM_TERRAIN_SURFACEDATA_INCLUDED