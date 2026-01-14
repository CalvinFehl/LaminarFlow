#ifndef FLUIDFRENZY_WATER_INPUT_INCLUDED
#define FLUIDFRENZY_WATER_INPUT_INCLUDED

#ifndef UNITY_DECLARE_TEX2D_NOSAMPLER
#define UNITY_DECLARE_TEX2D_NOSAMPLER(textureName) TEXTURE2D(textureName)
#endif

#ifndef SAMPLE_TEXTURE2D
#define SAMPLE_TEXTURE2D(textureName, textureSampler, uv) textureName.Sample(textureSampler, uv)
#endif

#if defined(UNITY_STEREO_INSTANCING_ENABLED) || defined(UNITY_STEREO_MULTIVIEW_ENABLED)
UNITY_DECLARE_TEX2DARRAY(_BackgroundTexture);
UNITY_DECLARE_TEX2DARRAY(_CameraOpaqueTexture);

#ifdef FLUIDFRENZY_WATER_INPUT_INCLUDED_REQUIRES_CAMERADEPTH
UNITY_DECLARE_TEX2DARRAY(_CameraDepthTexture);
#endif

#define SAMPLE_SCREENSPACE_TEXTURE(textureName, uv) UNITY_SAMPLE_TEX2DARRAY_SAMPLER(textureName, textureName, float3(uv.xy, (float)unity_StereoEyeIndex))

#else
UNITY_DECLARE_TEX2D(_BackgroundTexture);
UNITY_DECLARE_TEX2D(_CameraOpaqueTexture);

#ifdef FLUIDFRENZY_WATER_INPUT_INCLUDED_REQUIRES_CAMERADEPTH
UNITY_DECLARE_TEX2D(_CameraDepthTexture);
#endif

#define SAMPLE_SCREENSPACE_TEXTURE(textureName, uv) UNITY_SAMPLE_TEX2D_SAMPLER(textureName, textureName, uv.xy)

#endif
UNITY_DECLARE_TEX2D_NOSAMPLER(_PlanarReflections);
UNITY_DECLARE_TEX2D_NOSAMPLER(_FluidScreenSpaceParticles);

CBUFFER_START(UnityPerMaterial)
float _ReflectionDistortion;
float _ReflectivityMin;
float _AbsorptionDepthScale;
float _RefractionDistortion;
float4 _WaterColor;

sampler2D _WaveNormals;
float4 _WaveNormals_ST;
float _WaveNormalStrength;

float _DisplacementWaveAmplitude;
float _DisplacementPhase;
float _DisplacementWaveSpeed;
float _DisplacementWaveLength;
float _DisplacementSteepness;
float _DisplacementScale;

float _SpecularIntensity;
float4 _ScatterColor;
float _ScatterIntensity;
float _ScatterLightIntensity;
float _ScatterViewIntensity;
float _ScatterFoamIntensity;
float _ScatterAmbient;

float4 _FoamColor;
float _FoamNormalStrength;
sampler2D _FoamTexture;
sampler2D _FoamNormalMap;
float4 _FoamTexture_ST;
float4 _FoamVisibility;

float _FadeHeight;
float _LinearClipOffset;
float _ExponentialClipOffset;
float _Layer;

#if defined(RENDERPIPELINE_HDRP)
float _BlendMode;
float _EnableBlendModePreserveSpecularLighting;
float _RayTracing;
float4 _DoubleSidedConstants;
#endif
CBUFFER_END

void ApplyDisplacementWaves(inout FluidData fluidData)
{
#if defined(_DISPLACEMENTWAVES_ON)
	float3 waveOffset = 0, waveNormal = 0, waveTangent = 0, waveBinormal = 0;
	float waveLength = _DisplacementWaveLength;
	float waveSpeed = _DisplacementWaveSpeed;
	float amplitude = 0.125f * _DisplacementWaveAmplitude * length(fluidData.velocity) * saturate(fluidData.layerHeight);
	float steepness = _DisplacementSteepness;
	float phaseSpeed = 60 * _DisplacementPhase;

	float uvScale = _DisplacementScale;
	float2 dir0 = float2(1, 1);
	float2 dir1 = float2(-1, 1);

#if defined(_FLUID_FLOWMAPPING_DYNAMIC)
	GerstnerDynamicUV(phaseSpeed, waveLength, waveSpeed, amplitude, steepness, uvScale, dir0, fluidData.flowUV.xy, fluidData.flowUV.zw, waveOffset, waveNormal, waveTangent, waveBinormal);
	GerstnerDynamicUV(phaseSpeed, waveLength, waveSpeed, amplitude, steepness, uvScale, dir1, fluidData.flowUV.xy, fluidData.flowUV.zw, waveOffset, waveNormal, waveTangent, waveBinormal);
#elif defined(_FLUID_FLOWMAPPING_STATIC)
	GerstnerStaticUV(fluidData.velocity, phaseSpeed, waveLength, waveSpeed, amplitude, steepness, uvScale, dir0, fluidData.uv.xy, waveOffset, waveNormal, waveTangent, waveBinormal);
	GerstnerStaticUV(fluidData.velocity, phaseSpeed, waveLength, waveSpeed, amplitude, steepness, uvScale, dir1, fluidData.uv.xy, waveOffset, waveNormal, waveTangent, waveBinormal);
#else
	GerstnerWaveTessendorf(phaseSpeed, waveLength, waveSpeed, amplitude, steepness, dir0, float3(fluidData.uv.x, 0, fluidData.uv.y) * uvScale, waveOffset, waveNormal, waveTangent, waveBinormal);
	GerstnerWaveTessendorf(phaseSpeed, waveLength, waveSpeed, amplitude, steepness, dir1, float3(fluidData.uv.x, 0, fluidData.uv.y) * uvScale, waveOffset, waveNormal, waveTangent, waveBinormal);
#endif
	fluidData.normalOS = normalize(fluidData.normalOS + normalize(waveNormal) * float3(-1, 0, -1));
	fluidData.positionOS.xyz += waveOffset;
#endif
}

#endif // FLUIDFRENZY_WATER_INPUT_INCLUDED