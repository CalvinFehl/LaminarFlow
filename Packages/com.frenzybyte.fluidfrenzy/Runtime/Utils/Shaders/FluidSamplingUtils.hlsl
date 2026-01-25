#ifndef _FLUIDRENDERING_BICUBIC_KERNELS_HLSL_
#define _FLUIDRENDERING_BICUBIC_KERNELS_HLSL_

// Kernel Definitions

// B-Spline Kernel (Smoother, C2 Continuous)
float BSplineKernel(float x) 
{
	float f_abs = abs(x);
	if (f_abs >= 2.0f) return 0.0f;

	float f2 = f_abs * f_abs;
	float f3 = f2 * f_abs;

	if (f_abs < 1.0f) {
		return (2.0f/3.0f) - f2 + (0.5f * f3);
	} else {
		float t = 2.0f - f_abs;
		return (t * t * t) / 6.0f;
	}
}

// Catmull-Rom Kernel (Sharper, C1 Continuous, a = -0.5)
float CatmullRomKernel(float x) {
	float a = -0.5f;
	x = abs(x);
	if (x >= 2.0f) return 0.0f;
	
	float x2 = x * x;
	float x3 = x2 * x;

	if (x <= 1.0f) {
		return (a + 2.0f) * x3 + (-a - 3.0f) * x2 + 1.0f;
	} else {
		return a * x3 - 5.0f * a * x2 + 8.0f * a * x - 4.0f * a;
	}
}

// Sampler Functions

float4 SampleCubicBSpline(
	Texture2D tex, 
	SamplerState sampler_tex, 
	float2 uv, 
	float2 texelSize) 
{
	float2 U = uv / texelSize;

	U -= 0.5f; 
	float2 I_start = floor(U - 0.5f + 1e-6f); 
	float2 f_new = U - I_start;
	float2 f = f_new - 0.5f;

	float2 I = I_start + 1.0f;
	float2 uv_center = I * texelSize + (texelSize * 0.5f);

	float4 sum = 0.0f; 

	for (int j = -1; j <= 2; j++) {
		float Wy = BSplineKernel(f.y - j); 
		float4 Z_row = 0.0f; 
		
		for (int i = -1; i <= 2; i++) {
			float Wx = BSplineKernel(f.x - i);
			float2 tap_uv = uv_center + float2(i, j) * texelSize; 
			Z_row += tex.SampleLevel(sampler_tex, tap_uv, 0) * Wx;
		}
		
		sum += Z_row * Wy;
	}
	
	return sum;
}

float4 SampleCubicCatmullRom(
	Texture2D tex, 
	SamplerState sampler_tex, 
	float2 uv, 
	float2 texelSize) 
{
	// Local calculation of coordinates
	float2 uv_center = floor(uv / texelSize) * texelSize + (texelSize * 0.5f);
	float2 f = (uv - uv_center) / texelSize;
	
	float4 sum = 0.0f; 

	for (int j = -1; j <= 2; j++) {
		float Wy = CatmullRomKernel(f.y - j); 
		float4 Z_row = 0.0f; 
		
		for (int i = -1; i <= 2; i++) {
			float Wx = CatmullRomKernel(f.x - i);
			float2 tap_uv = uv_center + float2(i, j) * texelSize;
			Z_row += tex.SampleLevel(sampler_tex, tap_uv, 0) * Wx;
		}
		
		sum += Z_row * Wy;
	}
	
	return sum;
}

#endif //_FLUIDRENDERING_BICUBIC_KERNELS_HLSL_