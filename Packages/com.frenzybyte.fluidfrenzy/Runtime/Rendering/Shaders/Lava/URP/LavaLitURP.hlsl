#ifndef FLUIDFRENZY_LAVA_LIT_URP_INCLUDED
#define FLUIDFRENZY_LAVA_LIT_URP_INCLUDED

inline void InitializeStandardLitSurfaceData(FluidInputData fluidInput, out SurfaceData outSurfaceData)
{
	LavaSurfaceData lavaSurfaceData;
	InitializeLavaSurfaceData(fluidInput, lavaSurfaceData);

	outSurfaceData.alpha = lavaSurfaceData.alpha;
	outSurfaceData.albedo = lavaSurfaceData.albedo.rgb;
	outSurfaceData.specular = half3(0.0, 0.0, 0.0);;
	outSurfaceData.metallic = lavaSurfaceData.metallic;
	outSurfaceData.smoothness = lavaSurfaceData.smoothness;
	outSurfaceData.normalTS = lavaSurfaceData.normalTS;
	outSurfaceData.occlusion = 1;

	outSurfaceData.emission = lavaSurfaceData.emission;
	outSurfaceData.clearCoatMask = half(0.0);
	outSurfaceData.clearCoatSmoothness = half(0.0);
}

#endif // FLUIDFRENZY_LAVA_LIT_URP_INCLUDED