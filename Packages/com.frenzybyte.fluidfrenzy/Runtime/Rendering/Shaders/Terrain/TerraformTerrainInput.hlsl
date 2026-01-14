#ifndef FLUIDFRENZY_TERRAFORM_TERRAIN_INPUT_INCLUDED
#define FLUIDFRENZY_TERRAFORM_TERRAIN_INPUT_INCLUDED


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


#ifdef TERRAFORM_TEXTURE_ARRAY 

UNITY_DECLARE_TEX2DARRAY (_BaseLayerAlbedo);
UNITY_DECLARE_TEX2DARRAY (_BaseLayerMaskMap);
UNITY_DECLARE_TEX2DARRAY (_BaseLayerBumpMap);

UNITY_DECLARE_TEX2DARRAY (_DynamicLayerAlbedo);
UNITY_DECLARE_TEX2DARRAY (_DynamicLayerMaskMap);
UNITY_DECLARE_TEX2DARRAY (_DynamicLayerBumpMap);


CBUFFER_START(UnityPerMaterial)
float _HeightScale;

float4 _BaseLayerColor0;
float4 _BaseLayerColor1;
float4 _BaseLayerColor2;
float4 _BaseLayerColor3;

float4 _BaseLayer0_ST;
float4 _BaseLayer1_ST;
float4 _BaseLayer2_ST;
float4 _BaseLayer3_ST;

float4 _BaseLayerBumpScale;

float4 _DynamicLayer0_ST;
float4 _DynamicLayer1_ST;
float4 _DynamicLayer2_ST;

float4 _DynamicLayerBumpScale;
CBUFFER_END

#else

UNITY_DECLARE_TEX2D (_Layer0Albedo);
UNITY_DECLARE_TEX2D_NOSAMPLER (_Layer1Albedo);
#if !defined(UNITY_PLATFORM_WEBGL)
	UNITY_DECLARE_TEX2D_NOSAMPLER (_Layer2Albedo);
	UNITY_DECLARE_TEX2D_NOSAMPLER (_Layer3Albedo);
#endif
UNITY_DECLARE_TEX2D_NOSAMPLER (_TopLayerAlbedo);

UNITY_DECLARE_TEX2D (_Layer0BumpMap);
UNITY_DECLARE_TEX2D_NOSAMPLER (_Layer1BumpMap);
#if !defined(UNITY_PLATFORM_WEBGL)
	UNITY_DECLARE_TEX2D_NOSAMPLER (_Layer2BumpMap);
	UNITY_DECLARE_TEX2D_NOSAMPLER (_Layer3BumpMap);
#endif
UNITY_DECLARE_TEX2D_NOSAMPLER (_TopLayerBumpMap);

UNITY_DECLARE_TEX2D (_Layer0MaskMap);
UNITY_DECLARE_TEX2D_NOSAMPLER (_Layer1MaskMap);
#if !defined(UNITY_PLATFORM_WEBGL)
UNITY_DECLARE_TEX2D_NOSAMPLER (_Layer2MaskMap);
UNITY_DECLARE_TEX2D_NOSAMPLER (_Layer3MaskMap);
#endif
UNITY_DECLARE_TEX2D_NOSAMPLER (_TopLayerMaskMap);


CBUFFER_START(UnityPerMaterial)
float _HeightScale;
float4 _Layer0Color;
float4 _Layer0Albedo_ST;
float _Layer0BumpScale;

float4 _Layer1Color;
float4 _Layer1Albedo_ST;
float _Layer1BumpScale;

float4 _Layer2Color;
float4 _Layer2Albedo_ST;
float _Layer2BumpScale;

float4 _Layer3Color;
float4 _Layer3Albedo_ST;
float _Layer3BumpScale;

float4 _TopLayerAlbedo_ST;
float _TopLayerBumpScale;
CBUFFER_END

#endif

#include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Rendering/Shaders/Terrain/TerraformTerrainCommon.hlsl"

			
#endif // FLUIDFRENZY_TERRAFORM_TERRAIN_INPUT_INCLUDED