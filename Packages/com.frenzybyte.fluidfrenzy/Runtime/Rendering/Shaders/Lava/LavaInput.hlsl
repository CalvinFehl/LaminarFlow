#ifndef FLUIDFRENZY_LAVA_INPUT_INCLUDED
#define FLUIDFRENZY_LAVA_INPUT_INCLUDED

sampler2D _HeatLUT;

#ifdef BUILTIN_TARGET_API
#define UnpackNormalScale(x,y) UnpackNormalWithScale(x,y)
#endif

CBUFFER_START(UnityPerMaterial)
#ifndef UNITY_STANDARD_INPUT_INCLUDED
half4		_Color;
sampler2D	_MainTex;
float4		_MainTex_ST;
sampler2D	_EmissionMap;
sampler2D	_BumpMap;
half        _BumpScale;
float		_GlossMapScale;
#endif

float _LightIntensity;
float _LUTScale;
float _Emission;

float _FadeHeight;
float _LinearClipOffset;
float _ExponentialClipOffset;
float _Layer;
CBUFFER_END

struct LavaSurfaceData
{
	half4 albedo;
	half3 normalTS;
	half3 emission;
	half emissiveMask;
	half smoothness;
	half metallic;
	half alpha;
};

float2 PingPong(float2 t, float2 minVal, float2 maxVal)
{
    return lerp(minVal, maxVal, abs(frac(t) * 2.0 - 1.0));
}

inline void InitializeLavaSurfaceData(FluidInputData fluidInput, out LavaSurfaceData outLavaSurfaceData)
{
	outLavaSurfaceData.normalTS = half3(0, 0, 1);
	outLavaSurfaceData.albedo = half4(0, 0, 0, 0);
	outLavaSurfaceData.emissiveMask = 0;
	outLavaSurfaceData.emission = half3(0, 0, 0);
	outLavaSurfaceData.smoothness = 0;
	outLavaSurfaceData.metallic = 0;


	float2 pingponguv = PingPong(fluidInput.fluidUV.xy, 0, 0.5f);
	DEFINE_NOISEKEY(noiseKey, pingponguv.xy * _MainTex_ST.xy);

#if defined(_FLUID_FLOWMAPPING_DYNAMIC)
	outLavaSurfaceData.albedo = SampleTextureDynamicUVWithNoise(_MainTex, _MainTex_ST, fluidInput.flowUV.xy, fluidInput.flowUV.zw, PASS_NOISEKEY(noiseKey));
	outLavaSurfaceData.emissiveMask = SampleTextureDynamicUVWithNoise(_EmissionMap, _MainTex_ST, fluidInput.flowUV.xy, fluidInput.flowUV.zw, PASS_NOISEKEY(noiseKey)).r;
#elif defined(_FLUID_FLOWMAPPING_STATIC)
	outLavaSurfaceData.albedo = SampleTextureStaticUVWithNoise(_MainTex, _MainTex_ST, fluidInput.flowUV.xy, fluidInput.velocity, PASS_NOISEKEY(noiseKey));
	outLavaSurfaceData.emissiveMask = SampleTextureStaticUVWithNoise(_EmissionMap, _MainTex_ST, fluidInput.flowUV.xy, fluidInput.velocity, PASS_NOISEKEY(noiseKey)).r;
#else
	outLavaSurfaceData.albedo = sampleSampler2DWithNoise(_MainTex, fluidInput.flowUV.xy * _MainTex_ST.xy, PASS_NOISEKEY(noiseKey));
	outLavaSurfaceData.emissiveMask = sampleSampler2DWithNoise(_EmissionMap, fluidInput.flowUV.xy * _MainTex_ST.xy, PASS_NOISEKEY(noiseKey)).r;
#endif		
	outLavaSurfaceData.albedo.rgb *= _Color.rgb;
	outLavaSurfaceData.smoothness = outLavaSurfaceData.albedo.a * _GlossMapScale;

#if defined(_NORMALMAP)
#if defined(_FLUID_FLOWMAPPING_DYNAMIC)
	float4 lavaNormal = SampleTextureDynamicUVWithNoise(_BumpMap, _MainTex_ST, fluidInput.flowUV.xy, fluidInput.flowUV.zw, PASS_NOISEKEY(noiseKey));
#elif defined(_FLUID_FLOWMAPPING_STATIC)
	float4 lavaNormal = SampleTextureStaticUVWithNoise(_BumpMap, _MainTex_ST, fluidInput.flowUV.xy, fluidInput.velocity, PASS_NOISEKEY(noiseKey));
#else
	float4 lavaNormal = sampleSampler2DWithNoise(_BumpMap, fluidInput.flowUV.xy * _MainTex_ST.xy, PASS_NOISEKEY(noiseKey));
#endif		
	outLavaSurfaceData.normalTS = UnpackNormalScale(lavaNormal, _BumpScale);
#endif
	outLavaSurfaceData.alpha = smoothstep(0, _FadeHeight, max(0, (fluidInput.fluidMask * FluidLayerToMask(_Layer)) - _FluidClipHeight));

	float speed = length(fluidInput.velocity);
	float3 lavaEmissionMask = outLavaSurfaceData.emissiveMask * saturate(speed) * _LUTScale * outLavaSurfaceData.alpha;
	outLavaSurfaceData.emission = tex2D(_HeatLUT, lavaEmissionMask.x).rgb * _Emission * outLavaSurfaceData.alpha;
}

#endif // FLUIDFRENZY_LAVA_INPUT_INCLUDED