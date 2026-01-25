#ifndef _FLUID_PARTICLES_COMMON_HLSL_
#define _FLUID_PARTICLES_COMMON_HLSL_

//#define FORCE_DXC
#if (defined(SHADER_API_XBOXONE) || defined(FORCE_DXC)) && (defined(SHADER_API_D3D11) || defined(SHADER_API_D3D12))
#pragma use_dxc
#endif

uint PackToR8G8B8A8(float4 color)
{
    // Clamp the values to ensure they are in the range [0, 1]
    color = clamp(color, float4(0.0, 0.0, 0.0, 0.0), float4(1.0, 1.0, 1.0, 1.0));

    // Scale to [0, 255] and convert to integers
    uint r = (uint)(color.x * 255.0);
    uint g = (uint)(color.y * 255.0);
    uint b = (uint)(color.z * 255.0);
    uint a = (uint)(color.w * 255.0);

    // Pack the components into a single 32-bit integer
    return (a << 24) | (b << 16) | (g << 8) | r;
}

#ifndef UNITY_PACKING_INCLUDED
float4 UnpackFromR8G8B8A8(uint rgba)
{
    return float4(rgba & 255, (rgba >> 8) & 255, (rgba >> 16) & 255, (rgba >> 24) & 255) * (1.0 / 255);
}
#endif

uint PackHalf2x16(float2 val)
{
	return f32tof16(val.x) | f32tof16(val.y) << 16;
}

float2 UnpackHalf2x16(uint val)
{
	return float2(f16tof32((val << 16) >> 16), f16tof32(val >> 16));
}

uint2 PackFloat4ToHalf2(in float4 val)
{
	uint2 result = 0;
	result.x = f32tof16(val.x);
	result.x |= f32tof16(val.y) << 16;
	result.y = f32tof16(val.z);
	result.y |= f32tof16(val.w) << 16;

	return result;
}

float4 UnpackHalf2ToFloat4(in uint2 val)
{
	float4 result;
	result.x = f16tof32((val.x << 16) >> 16);
	result.y = f16tof32(val.x >> 16);
	result.z = f16tof32((val.y << 16) >> 16);
	result.w = f16tof32(val.y >> 16);

	return result;
}

struct Particle
{
	float4 position_size;
	uint4 vel_accel_rot_angularvel;
	uint2 life_maxlife_color;
};

#endif