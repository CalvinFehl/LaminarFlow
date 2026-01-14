#ifndef FLUIDFRENZY_WATER_SURFACEDATA_INCLUDED
#define FLUIDFRENZY_WATER_SURFACEDATA_INCLUDED

#ifdef BUILTIN_TARGET_API
#define UnpackNormalScale(x,y) UnpackNormalWithScale(x,y)
#endif

struct WaterSurfaceData
{
	half3 waveNormalTS;
	half4 foamAlbedo;
	half3 foamNormalTS;
	half fluidHeight;
};

inline void InitializeWaterSurfaceData(FluidInputData fluidInput, out WaterSurfaceData outWaterSurfaceData)
{
	outWaterSurfaceData.fluidHeight = fluidInput.fluidHeight;
	outWaterSurfaceData.waveNormalTS = half3(0, 0, 1);
	outWaterSurfaceData.foamAlbedo = half4(0, 0, 0, 0);;
	outWaterSurfaceData.foamNormalTS = half3(0, 0, 1);
#if defined(_NORMALMAP)
	half normalStrength = _WaveNormalStrength * clamp(length(fluidInput.velocity), 0.25f, 1);
	#if defined(_FLUID_FLOWMAPPING_DYNAMIC)
		outWaterSurfaceData.waveNormalTS = UnpackNormalScale(SampleTextureDynamicUV(_WaveNormals, _WaveNormals_ST, fluidInput.flowUV.xy, fluidInput.flowUV.zw), normalStrength);
	#elif defined(_FLUID_FLOWMAPPING_STATIC)
		outWaterSurfaceData.waveNormalTS = UnpackNormalScale(SampleTextureStaticUV(_WaveNormals, _WaveNormals_ST, fluidInput.flowUV.xy, fluidInput.velocity), normalStrength);
	#else
		outWaterSurfaceData.waveNormalTS = UnpackNormalScale(tex2D(_WaveNormals, fluidInput.flowUV.xy * _WaveNormals_ST.xy), normalStrength);
	#endif
#endif

#if defined(_FOAMMAP) || defined(_FOAM_NORMALMAP)
	#if defined(_FLUID_FLOWMAPPING_DYNAMIC)
		outWaterSurfaceData.foamAlbedo = SampleTextureDynamicUV(_FoamTexture, _FoamTexture_ST, fluidInput.flowUV.xy, fluidInput.flowUV.zw);
	#elif defined(_FLUID_FLOWMAPPING_STATIC)
		outWaterSurfaceData.foamAlbedo = SampleTextureStaticUV(_FoamTexture, _FoamTexture_ST, fluidInput.flowUV.xy, fluidInput.velocity);
	#else
		outWaterSurfaceData.foamAlbedo = tex2D(_FoamTexture, fluidInput.flowUV.xy * _FoamTexture_ST.xy);
	#endif		
#endif

#if defined(_FOAM_NORMALMAP)
	#if defined(_FLUID_FLOWMAPPING_DYNAMIC)
		float4 foamNormal = SampleTextureDynamicUV(_FoamNormalMap, _FoamTexture_ST, fluidInput.flowUV.xy, fluidInput.flowUV.zw);
	#elif defined(_FLUID_FLOWMAPPING_STATIC)
		float4 foamNormal = SampleTextureStaticUV(_FoamNormalMap, _FoamTexture_ST, fluidInput.flowUV.xy, fluidInput.velocity);
	#else
		float4 foamNormal = tex2D(_FoamNormalMap, fluidInput.flowUV.xy * _FoamTexture_ST.xy);
	#endif			
	outWaterSurfaceData.foamNormalTS = UnpackNormalScale(foamNormal, _FoamNormalStrength);
#endif

}

#endif // FLUIDFRENZY_WATER_SURFACEDATA_INCLUDED