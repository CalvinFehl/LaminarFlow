#ifndef FLUIDFRENZY_TERRAFORM_TERRAIN_SHADERGRAPH_INCLUDED
#define FLUIDFRENZY_TERRAFORM_TERRAIN_SHADERGRAPH_INCLUDED

#include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Rendering/Shaders/Library/LOD/FluidInstancingCommon.hlsl"
#include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Rendering/Shaders/Terrain/TerraformTerrainCommon.hlsl"

void GetTerrainSurfaceData_float(float2 uv,
	in UnityTexture2D layer0Albedo, 
	in UnityTexture2D layer0MaskMap,
	in UnityTexture2D layer0BumpMap,
	in float4 layer0Color,
	in float layer0BumpScale,

	in UnityTexture2D layer1Albedo,
	in UnityTexture2D layer1MaskMap,
	in UnityTexture2D layer1BumpMap,
	in float4 layer1Color,
	in float layer1BumpScale,

	in UnityTexture2D layer2Albedo,
	in UnityTexture2D layer2MaskMap,
	in UnityTexture2D layer2BumpMap,
	in float4 layer2Color,
	in float layer2BumpScale,
	
	in UnityTexture2D layer3Albedo,
	in UnityTexture2D layer3MaskMap,
	in UnityTexture2D layer3BumpMap,
	in float4 layer3Color,
	in float layer3BumpScale,

	in UnityTexture2D topLayerAlbedo,
	in UnityTexture2D topLayerMaskMap,
	in UnityTexture2D topLayerBumpMap,
	in float topLayerBumpScale,

	out float4 albedo,
	out float smoothness,
	out float metallic,
	out float occlusion,
	out float3 normal
)
{
	TerrainSurfaceData surfaceData = GetTerrainSurfaceData(uv,
		layer0Albedo.tex, layer0Albedo.samplerstate,
		layer0MaskMap.tex, layer0MaskMap.samplerstate,
		layer0BumpMap.tex, layer0BumpMap.samplerstate,
		layer0Color,
		layer0Albedo.scaleTranslate,
		layer0BumpScale,

		layer1Albedo.tex,
		layer1MaskMap.tex,
		layer1BumpMap.tex,
		layer1Color,
		layer1Albedo.scaleTranslate,
		layer1BumpScale,

		layer2Albedo.tex,
		layer2MaskMap.tex,
		layer2BumpMap.tex,
		layer2Color,
		layer2Albedo.scaleTranslate,
		layer2BumpScale,

		layer3Albedo.tex,
		layer3MaskMap.tex,
		layer3BumpMap.tex,
		layer3Color,
		layer3Albedo.scaleTranslate,
		layer3BumpScale,

		topLayerAlbedo.tex,
		topLayerMaskMap.tex,
		topLayerBumpMap.tex,
		topLayerAlbedo.scaleTranslate,
		topLayerBumpScale

	);

	albedo = surfaceData.albedo;
	smoothness = surfaceData.smoothness;
	metallic = surfaceData.metallic;
	occlusion = surfaceData.occlusion;
	normal = surfaceData.normal;
}

void GetTerrainSurfaceDataArray_float(float2 uv,
    in UnityTexture2DArray baseLayerAlbedo,
    in UnityTexture2DArray baseLayerMaskMap,
    in UnityTexture2DArray baseLayerNormalMap,
    in float4 baseLayer0Albedo_ST,
    in float4 baseLayer1Albedo_ST,
    in float4 baseLayer2Albedo_ST,
    in float4 baseLayer3Albedo_ST,

	in float4 baseLayer0Color,
	in float4 baseLayer1Color,
	in float4 baseLayer2Color,
	in float4 baseLayer3Color,

    in float4 baseLayerNormalScale,

    in UnityTexture2DArray dynamicLayerAlbedo,
	in UnityTexture2DArray dynamicLayerMaskMap,
    in UnityTexture2DArray dynamicLayerNormalMap,
    in float4 dynamicLayer0Albedo_ST,
    in float4 dynamicLayer1Albedo_ST,
    in float4 dynamicLayer2Albedo_ST,
    in float4 dynamicLayerNormalScale,

	out float4 albedo,
	out float smoothness,
	out float metallic,
	out float occlusion,
	out float3 normal
)
{
	TerrainSurfaceData surfaceData = GetTerrainSurfaceData(uv,

		baseLayerAlbedo.tex, baseLayerAlbedo.samplerstate,
		baseLayerMaskMap.tex, baseLayerMaskMap.samplerstate,
		baseLayerNormalMap.tex, baseLayerNormalMap.samplerstate,
		baseLayer0Albedo_ST,
		baseLayer1Albedo_ST,
		baseLayer2Albedo_ST,
		baseLayer3Albedo_ST,

		baseLayer0Color,
		baseLayer1Color,
		baseLayer2Color,
		baseLayer3Color,

		baseLayerNormalScale,

		dynamicLayerAlbedo.tex, dynamicLayerAlbedo.samplerstate,
		dynamicLayerMaskMap.tex, dynamicLayerMaskMap.samplerstate,
		dynamicLayerNormalMap.tex, dynamicLayerNormalMap.samplerstate,
		dynamicLayer0Albedo_ST,
		dynamicLayer1Albedo_ST,
		dynamicLayer2Albedo_ST,
		dynamicLayerNormalScale
	);

	albedo = surfaceData.albedo;
	smoothness = surfaceData.smoothness;
	metallic = surfaceData.metallic;
	occlusion = surfaceData.occlusion;
	normal = surfaceData.normal;
}

void GetTerrainVertexInputs_float(in float3 positionOS, in float2 uv, uint instanceID, out float3 out_positionOS, out float3 out_normalOS, out float2 out_uv)
{
	TerrainVertexInputs vertexInputs = GetTerrainVertexInputs(positionOS, uv, instanceID);
	out_positionOS = vertexInputs.positionOS;
	out_normalOS = vertexInputs.normalOS;
	out_uv = vertexInputs.uv;
}
			
#endif // FLUIDFRENZY_TERRAFORM_TERRAIN_SHADERGRAPH_INCLUDED