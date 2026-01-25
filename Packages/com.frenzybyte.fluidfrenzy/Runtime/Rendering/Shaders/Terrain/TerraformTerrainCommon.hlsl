#ifndef FLUIDFRENZY_TERRAFORM_TERRAIN_COMMON_INCLUDED
#define FLUIDFRENZY_TERRAFORM_TERRAIN_COMMON_INCLUDED

#ifdef HLSL_SUPPORT_INCLUDED
#undef SAMPLE_DEPTH_TEXTURE
#undef SAMPLE_DEPTH_TEXTURE_LOD
#undef GLOBAL_CBUFFER_START
#endif

#ifdef UNITY_CG_INCLUDED
#undef TRANSFORM_TEX
#endif

#ifndef UNITY_DECLARE_TEX2D
#define UNITY_DECLARE_TEX2D(textureName) TEXTURE2D(textureName); SAMPLER(sampler##textureName)
#define UNITY_DECLARE_TEX2D_NOSAMPLER(textureName) TEXTURE2D(textureName)
#define UNITY_SAMPLE_TEX2D_SAMPLER(tex,samplertex,coord) SAMPLE_TEXTURE2D(tex, sampler##samplertex, coord)
#endif

#ifndef UNITY_DECLARE_TEX2DARRAY
#define UNITY_DECLARE_TEX2DARRAY(textureName) TEXTURE2D_ARRAY(textureName); SAMPLER(sampler##textureName)
#define UNITY_DECLARE_TEX2DARRAY_NOSAMPLER(textureName) TEXTURE2D_ARRAY(textureName)
#define UNITY_SAMPLE_TEX2DARRAY_SAMPLER(tex,samplertex,coord) SAMPLE_TEXTURE2D_ARRAY(tex, sampler##samplertex, coord.xy, coord.z)
#endif

Texture2D<float4> _HeightField;
SamplerState sampler_HeightField;
UNITY_DECLARE_TEX2D(_Splatmap);

float4 _TexelWorldSize;
float4 _HeightField_TexelSize;

struct TerrainSurfaceData
{
	float4 albedo;
	float smoothness;
	float metallic;
	float occlusion;
	float3 normal;
};

#ifdef BUILTIN_TARGET_API
#define UnpackNormalScale(x,y) UnpackNormalWithScale(x,y)
#endif

float4 SampleTextureLOD0(
    Texture2D<float4> tex,
    SamplerState      tex_sampler,
    float2            uv)
{
    return tex.SampleLevel(tex_sampler, uv, 0.0f);
}

float4 SampleTerrain(float2 uv)
{
	return SampleTextureLOD0(_HeightField, sampler_HeightField, uv);
}

TerrainSurfaceData GetTerrainSurfaceData(float2 uv,
	Texture2D layer0Albedo, SamplerState samplerlayer0Albedo,
	Texture2D layer0MaskMap, SamplerState samplerlayer0MaskMap,
	Texture2D layer0BumpMap, SamplerState samplerlayer0BumpMap,
	float4 layer0Color,
	float4 layer0Albedo_ST,
	float layer0BumpScale,

	Texture2D layer1Albedo,
	Texture2D layer1MaskMap,
	Texture2D layer1BumpMap,
	float4 layer1Color,
	float4 layer1Albedo_ST,
	float layer1BumpScale,

#if !defined(UNITY_PLATFORM_WEBGL)
	Texture2D layer2Albedo,
	Texture2D layer2MaskMap,
	Texture2D layer2BumpMap,
	float4 layer2Color,
	float4 layer2Albedo_ST,
	float layer2BumpScale,

	Texture2D layer3Albedo,
	Texture2D layer3MaskMap,
	Texture2D layer3BumpMap,
	float4 layer3Color,
	float4 layer3Albedo_ST,
	float layer3BumpScale,
#endif

	Texture2D topLayerAlbedo,
	Texture2D topLayerMaskMap,
	Texture2D topLayerBumpMap,
	float4 topLayerAlbedo_ST,
	float topLayerBumpScale
)
{

	TerrainSurfaceData surfaceData = (TerrainSurfaceData)(0);
	float4 heightSample = SampleTerrain(uv);
	float4 spat = UNITY_SAMPLE_TEX2D_SAMPLER(_Splatmap, _Splatmap, uv);
	spat.rgba /= dot(spat.rgba, 1);

	float4 maskMapLayer0 = UNITY_SAMPLE_TEX2D_SAMPLER(layer0MaskMap, layer0MaskMap, uv * layer0Albedo_ST.xy) * spat.r;
	float4 albedoLayer0 = UNITY_SAMPLE_TEX2D_SAMPLER(layer0Albedo, layer0Albedo, uv * layer0Albedo_ST.xy) * spat.r * layer0Color;
	float smoothnessLayer0 = maskMapLayer0.a;
	float metallicLayer0 = maskMapLayer0.r;
	float occlusionLayer0 = maskMapLayer0.g;
	float3 normalLayer0 = UnpackNormalScale(UNITY_SAMPLE_TEX2D_SAMPLER(layer0BumpMap, layer0BumpMap, uv * layer0Albedo_ST.xy), layer0BumpScale) * spat.r;

	float4 maskMapLayer1 = UNITY_SAMPLE_TEX2D_SAMPLER(layer1MaskMap, layer0MaskMap, uv * layer1Albedo_ST.xy) * spat.g;
	float4 albedoLayer1 = UNITY_SAMPLE_TEX2D_SAMPLER(layer1Albedo, layer0Albedo, uv * layer1Albedo_ST.xy) * spat.g * layer1Color;
	float smoothnessLayer1 = maskMapLayer1.a;
	float metallicLayer1 = maskMapLayer1.r;
	float occlusionLayer1 = maskMapLayer1.g;
	float3 normalLayer1 = UnpackNormalScale(UNITY_SAMPLE_TEX2D_SAMPLER(layer1BumpMap, layer0BumpMap, uv * layer1Albedo_ST.xy), layer1BumpScale) * spat.g;
	
	#if !defined(UNITY_PLATFORM_WEBGL)
		float4 maskMapLayer2 = UNITY_SAMPLE_TEX2D_SAMPLER(layer2MaskMap, layer0MaskMap, uv * layer2Albedo_ST.xy) * spat.b;
		float4 albedoLayer2 = UNITY_SAMPLE_TEX2D_SAMPLER(layer2Albedo, layer0Albedo, uv * layer2Albedo_ST.xy) * spat.b * layer2Color;
		float smoothnessLayer2 = maskMapLayer2.a;
		float metallicLayer2 = maskMapLayer2.r;
		float occlusionLayer2 = maskMapLayer2.g;
		float3 normalLayer2 = UnpackNormalScale(UNITY_SAMPLE_TEX2D_SAMPLER(layer2BumpMap, layer0BumpMap, uv * layer2Albedo_ST.xy), layer2BumpScale) * spat.b;
	
		float4 maskMapLayer3 = UNITY_SAMPLE_TEX2D_SAMPLER(layer3MaskMap, layer0MaskMap, uv * layer3Albedo_ST.xy) * spat.a;
		float4 albedoLayer3 = UNITY_SAMPLE_TEX2D_SAMPLER(layer3Albedo, layer0Albedo, uv * layer3Albedo_ST.xy) * spat.a * layer3Color;
		float smoothnessLayer3 = maskMapLayer3.a;
		float metallicLayer3 = maskMapLayer3.r;
		float occlusionLayer3 = maskMapLayer3.g;
		float3 normalLayer3 = UnpackNormalScale(UNITY_SAMPLE_TEX2D_SAMPLER(layer3BumpMap, layer0BumpMap, uv * layer3Albedo_ST.xy), layer3BumpScale) * spat.a;
		
		float4 bottomMaskMap = maskMapLayer0 + maskMapLayer1 + maskMapLayer2 + maskMapLayer3;
		float4 bottomAlbedo = albedoLayer0 + albedoLayer1 + albedoLayer2 + albedoLayer3;
		float bottomSmoothness = smoothnessLayer0 + smoothnessLayer1 + smoothnessLayer2 + smoothnessLayer3;
		float bottomMetallic = metallicLayer0 + metallicLayer1 + metallicLayer2 + metallicLayer3;
		float bottomOcclusion = occlusionLayer0 + occlusionLayer1 + occlusionLayer2 + occlusionLayer3;
		float3 bottomNormal = normalLayer0 + normalLayer1 + normalLayer2 + normalLayer3;
	#else
		float4 bottomMaskMap = maskMapLayer0 + maskMapLayer1;
		float4 bottomAlbedo = albedoLayer0 + albedoLayer1;
		float bottomSmoothness = smoothnessLayer0 + smoothnessLayer1;
		float bottomMetallic = metallicLayer0 + metallicLayer1;
		float bottomOcclusion = occlusionLayer0 + occlusionLayer1;
		float3 bottomNormal = normalLayer0 + normalLayer1;
	#endif

	float4 topMaskMap = UNITY_SAMPLE_TEX2D_SAMPLER(topLayerMaskMap, layer0MaskMap, uv * topLayerAlbedo_ST.xy);
	float4 topAlbedo = UNITY_SAMPLE_TEX2D_SAMPLER(topLayerAlbedo, layer0Albedo, uv * topLayerAlbedo_ST.xy);
	float topSmoothness = topMaskMap.a;
	float topMetallic = topMaskMap.r;
	float topOcclusion = topMaskMap.g;
	float3 topNormal = UnpackNormalScale(UNITY_SAMPLE_TEX2D_SAMPLER(topLayerBumpMap, layer0BumpMap, uv * topLayerAlbedo_ST.xy), topLayerBumpScale);

	float remainingThickness = 1.0;
	float materialThickness = 1;
	float thickness = min(materialThickness, heightSample.y);
	float thicknessFactor = thickness / materialThickness;

	surfaceData.albedo += topAlbedo * thicknessFactor;
	surfaceData.smoothness += topSmoothness * thicknessFactor;
	surfaceData.occlusion += topOcclusion * thicknessFactor;
	surfaceData.metallic += topMetallic * thicknessFactor;
	surfaceData.normal += topNormal * thicknessFactor;
	remainingThickness -= thicknessFactor;


	surfaceData.albedo += bottomAlbedo * remainingThickness;
	surfaceData.smoothness += bottomSmoothness * remainingThickness;
	surfaceData.occlusion += bottomOcclusion * remainingThickness;
	surfaceData.metallic += bottomMetallic * remainingThickness;
	surfaceData.normal += bottomNormal * remainingThickness;

	return surfaceData;
}

TerrainSurfaceData GetTerrainSurfaceData(
    float2 uv,

    Texture2DArray baseLayerAlbedo, SamplerState samplerbaseLayerAlbedo,
    Texture2DArray baseLayerMaskMap, SamplerState samplerbaseLayerMaskMap,
    Texture2DArray baseLayerNormalMap, SamplerState samplerbaseLayerNormalMap,
    float4 baseLayer0_ST,
    float4 baseLayer1_ST,
    float4 baseLayer2_ST,
    float4 baseLayer3_ST,

	float4 baseLayer0Color,
	float4 baseLayer1Color,
	float4 baseLayer2Color,
	float4 baseLayer3Color,

    float4 baseLayerNormalScale,

    Texture2DArray dynamicLayerAlbedo, SamplerState samplerdynamicLayerAlbedo, 
	Texture2DArray dynamicLayerMaskMap, SamplerState samplerdynamicLayerMaskMap,
    Texture2DArray dynamicLayerNormalMap, SamplerState samplerdynamicLayerNormalMap,
    float4 dynamicLayer0_ST,
    float4 dynamicLayer1_ST,
    float4 dynamicLayer2_ST,
    float4 dynamicLayerNormalScale
)
{
	TerrainSurfaceData surfaceData = (TerrainSurfaceData)(0);
	float4 heightSample = SampleTerrain(uv);
	float4 spat = UNITY_SAMPLE_TEX2D_SAMPLER(_Splatmap, _Splatmap, uv);
	spat.rgba /= dot(spat.rgba, 1);

	float4 baseLayer_ST[4] = {baseLayer0_ST, baseLayer1_ST, baseLayer2_ST, baseLayer3_ST};
	float4 baseLayerColors[4] = {baseLayer0Color, baseLayer1Color, baseLayer2Color, baseLayer3Color};

	float4 baseMaskMap = 0;
	float4 baseAlbedo = 0;
	float baseSmoothness = 0;
	float baseMetallic = 0;
	float baseOcclusion = 0;
	float3 baseNormal = 0;

	[unroll]
	for (int i = 0; i < 4; i++)
	{
		float splatWeight = spat[i];

		if (splatWeight < 0.001)
			continue;

		float4 baseLayer_UV = float4(uv * baseLayer_ST[i].xy, i, 0);

		float4 maskMapLayer = UNITY_SAMPLE_TEX2DARRAY_SAMPLER(baseLayerMaskMap, baseLayerMaskMap, baseLayer_UV);
		float4 albedoLayer = UNITY_SAMPLE_TEX2DARRAY_SAMPLER(baseLayerAlbedo, baseLayerAlbedo, baseLayer_UV);
		float3 normalSample = UnpackNormalScale(UNITY_SAMPLE_TEX2DARRAY_SAMPLER(baseLayerNormalMap, baseLayerNormalMap, baseLayer_UV), baseLayerNormalScale[i]);

		baseMaskMap     += maskMapLayer * splatWeight;
		baseAlbedo      += albedoLayer * splatWeight * baseLayerColors[i];
		baseSmoothness  += maskMapLayer.a * splatWeight;
		baseMetallic    += maskMapLayer.r * splatWeight;
		baseOcclusion   += maskMapLayer.g * splatWeight;
		baseNormal      += normalSample * splatWeight;
	}

	float4 dynamicLayer_ST[3] = {dynamicLayer0_ST, dynamicLayer1_ST, dynamicLayer2_ST};

	float materialSmoothness[4];
	float materialMetallic[4];
	float materialOcclusion[4];
	float4 materialColor[4];
	float3 materialNormal[4];

	materialSmoothness[0] = baseSmoothness;
	materialMetallic[0] = baseMetallic;
	materialOcclusion[0] = baseOcclusion;
	materialColor[0] = baseAlbedo;
	materialNormal[0] = baseNormal;

	[unroll]
	for (i = 0; i < 3; i++)
	{
		float4 dynamicLayer_UV = float4(uv * dynamicLayer_ST[i].xy, i, 0);

		float4 maskMapLayer = UNITY_SAMPLE_TEX2DARRAY_SAMPLER(dynamicLayerMaskMap, dynamicLayerMaskMap, dynamicLayer_UV);
		float4 albedoLayer = UNITY_SAMPLE_TEX2DARRAY_SAMPLER(dynamicLayerAlbedo, dynamicLayerAlbedo, dynamicLayer_UV);
    
		float3 normalSample = UnpackNormalScale(
			UNITY_SAMPLE_TEX2DARRAY_SAMPLER(dynamicLayerNormalMap, dynamicLayerNormalMap, dynamicLayer_UV), 
			dynamicLayerNormalScale[i]
		);

		materialSmoothness[i + 1] = maskMapLayer.a;
		materialMetallic[i + 1]   = maskMapLayer.r;
		materialOcclusion[i + 1]  = maskMapLayer.g;
		materialColor[i + 1]      = albedoLayer;
		materialNormal[i + 1]     = normalSample;
	}

	float remainingThickness = 1.0f;
	float materialThickness = 1.0f;

	heightSample[0] = max(heightSample[0],1);
	for(i = 3; i >= 0; i--) 
	{
		float layerRemainingThickness = remainingThickness * materialThickness;
		float thickness = min(layerRemainingThickness, heightSample[i]);
		float thicknessFactor = thickness/materialThickness;
		surfaceData.albedo += materialColor[i] * thickness;
		surfaceData.smoothness += materialSmoothness[i] * thickness;
		surfaceData.occlusion += materialOcclusion[i] * thickness;
		surfaceData.metallic += materialMetallic[i] * thickness;
		surfaceData.normal += materialNormal[i] * thickness;
		remainingThickness -= thicknessFactor;
	}

	return surfaceData;
}

struct TerrainVertexInputs
{
	float3 positionOS;
	float3 normalOS;
	float2 uv;
};

TerrainVertexInputs GetTerrainVertexInputs(in float3 positionOS, in float2 uv, uint instanceID)
{
	TerrainVertexInputs output;
#if defined(_FLUIDFRENZY_INSTANCING) && !defined(UNITY_INSTANCING_ENABLED)
	LODVertexToLocal_UV_Normal(_HeightField, sampler_HeightField, _HeightField_TexelSize, _TexelWorldSize, _HeightScale, positionOS, instanceID, output.positionOS, output.uv, output.normalOS);
	output.positionOS = mul((float3x3)_ObjectToWorldRotationScale, output.positionOS);
#else
	float4 uvParams = UNITY_ACCESS_INSTANCED_PROP(InstanceProperties, _MeshUVOffsetScale);
	uv = uv * uvParams.zw + uvParams.xy;

	float terrainheight = dot(SampleTerrain(uv), (1.0f).xxxx);
	output.positionOS = positionOS + float3(0, terrainheight, 0);
	output.normalOS = CalculateNormalFromHeightField(_HeightField, sampler_HeightField, _HeightField_TexelSize, _TexelWorldSize.xy, uv, 1, 1);
	output.uv = uv;
#endif

	return output;
}
			
#endif // FLUIDFRENZY_TERRAFORM_TERRAIN_COMMON_INCLUDED