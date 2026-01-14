#ifndef _FLUIDSIMULATION_COMMON_HLSL_
#define _FLUIDSIMULATION_COMMON_HLSL_

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"

//#define FORCE_DXC
#if (defined(SHADER_API_XBOXONE) || defined(FORCE_DXC)) && (defined(SHADER_API_D3D11) || defined(SHADER_API_D3D12))
#pragma use_dxc
#endif

#define DAMPING_LAYER1 _Damping.x
#define DAMPING_LAYER2 _Damping.y


// World CellSize is the actual size of each cell in world space.
#define WORLD_CELLSIZE_LAYER1 _WorldCellSize.xy
#define WORLD_CELLSIZE_LAYER2 _WorldCellSize.zw

#define WORLD_CELLSIZE_RCP_LAYER1 _WorldCellSizeRcp.xy
#define WORLD_CELLSIZE_RCP_LAYER2 _WorldCellSizeRcp.zw

// Cell Size is the World CellSize scaled to increase/decrease the simulation speed.
#define CELLSIZE_SCALE_LAYER1 _CellSizeScale.x
#define CELLSIZE_SCALE_LAYER2 _CellSizeScale.y

#define CELLSIZE_LAYER1 _CellSize.xy
#define CELLSIZE_LAYER2 _CellSize.zw

#define CELLSIZE_RCP_LAYER1 _CellSizeRcp.xy
#define CELLSIZE_RCP_LAYER2 _CellSizeRcp.zw

#define CELLSIZESQ_LAYER1 _CellSizeSq.x
#define CELLSIZESQ_LAYER2 _CellSizeSq.z

#define ACCELERATION_LAYER1 _FluidAcceleration.x
#define ACCELERATION_LAYER2 _FluidAcceleration.y

#define ACCEL_DT_CELLSIZE_LAYER1 _AccelCellSizeDeltaTime.xxyy
#define ACCEL_DT_CELLSIZE_LAYER2 _AccelCellSizeDeltaTime.zzww

#define RCP_DT_CELLSIZESQ_LAYER1 _RcpCellSizeSqDeltaTime.x
#define RCP_DT_CELLSIZESQ_LAYER2 _RcpCellSizeSqDeltaTime.z

#define VELOCITY_SCALE_LAYER1 _VelocityScale.x
#define VELOCITY_SCALE_LAYER2 _VelocityScale.y

#define LINEAR_EVAPORATION _Evaporation.xz
#define PROPORTIONAL_EVAPORATION _Evaporation.yw

#define LINEAR_EVAPORATION_LAYER1 _Evaporation.x
#define PROPORTIONAL_EVAPORATION_LAYER1 _Evaporation.y

#define LINEAR_EVAPORATION_LAYER2 _Evaporation.z
#define PROPORTIONAL_EVAPORATION_LAYER2 _Evaporation.w

#ifdef USETEXTURE2D
Texture2D _TerrainHeightField;
SamplerState sampler_TerrainHeightField;

Texture2D<float4> _FluidHeightField;
SamplerState sampler_FluidHeightField;

Texture2D<float4> _PreviousFluidHeightField;
SamplerState sampler_PreviousFluidHeightField;

Texture2D<float4> _OutflowField;
Texture2D<float4> _OutflowFieldLayer2;
Texture2D<float4> _ExternalOutflowField;

Texture2D _PressureField;
SamplerState sampler_PressureField;

Texture2D _DivergenceField;
SamplerState sampler_DivergenceField;

Texture2D<float4> _VelocityField;
SamplerState sampler_VelocityField;

Texture2D _PreviousVelocityField;
SamplerState sampler_PreviousVelocityField;

#else
sampler2D _TerrainHeightField;
sampler2D _FluidHeightField;
sampler2D _PreviousFluidHeightField;
sampler2D _OutflowField;
sampler2D _OutflowFieldLayer1;
sampler2D _ExternalOutflowField;


sampler2D _PressureField;
sampler2D _DivergenceField;
sampler2D _VelocityField;
sampler2D _PreviousVelocityField;
#endif

CBUFFER_START(FluidSimDynamics)
uniform float _FluidSimDeltaTime;
uniform float _FluidSimStepDeltaTime;
CBUFFER_END


CBUFFER_START(FluidSimSettings)
uniform float4 _WorldCellSize;
uniform float4 _WorldCellSizeRcp;
uniform float4 _WorldCellSizeSq;
uniform float4 _WorldCellSizeRcpSq;
uniform float4 _CellSize;
uniform float4 _CellSizeRcp;
uniform float4 _CellSizeSq;
uniform float4 _CellSizeRcpSq;
uniform float2 _CellSizeScale;
uniform float4 _AccelCellSizeDeltaTime;
uniform float4 _RcpCellSizeSqDeltaTime;
uniform float2 _FluidAcceleration;
uniform float2 _VelocityScale;
uniform float2 _Damping;

uniform float _FluidClipHeight;

uniform float _VelocityDamping;
uniform float _VelocityDeltaTime;
uniform float _VelocityDeltaTimeRcp;
uniform float2 _VelocityMax;

uniform float2 _AccelerationMax;
uniform float _OvershootingEdge;
uniform float _OvershootingScale;


uniform float _TerrainHeightScale;
uniform float _FluidBaseHeightOffset;

// second layer specific
uniform float _FluidViscosity;
uniform float _FluidFlowHeight;

// Evaporation Linear(X), Exponential(Y)
uniform float4 _Evaporation;

CBUFFER_END

CBUFFER_START(FluidSimConstants)

uniform float4 _VelocityPadding_ST;
uniform float4 _Simulation_TexelSize;

uniform float4 _VelocityField_TexelSize;
uniform float4 _TerrainHeightField_TexelSize;
uniform float4 _FluidHeightField_TexelSize;
uniform float4 _OutflowField_TexelSize;
uniform float4 _ExternalOutflowField_TexelSize;
uniform float4 _ExternalOutflowFieldClamp;

uniform float2 _HeightFieldRcp;

uniform float4 _FluxClampMinMax; //minMaxX(x,y) minMaxY(z,w)

uniform float4 _BlitScaleBias;
uniform float4 _BlitScaleBiasRt;

uniform float4 _Padding_ST;
uniform float4 _ObstaclePadding_ST;

uniform float2 _WorldSize;
uniform int2 _BoundaryCells;
CBUFFER_END

float2 ClampVector(in float2 v, in float x)
{
	float l = length(v);
	return min(1, x / l) * v;
}


float2 LocalPositionToSimulationUV(float3 position)
{
	return (position.xz / (_WorldSize * 0.5)) * 0.5 + 0.5;
}

float3 SimulationUVToLocalPosition(float2 uv)
{
	return float3((uv.xy - 0.5f) * _WorldSize, 0).xzy;
}

float2 TransformToPadded(float2 uv)
{
	return uv * _Padding_ST.xy + _Padding_ST.zw;
}

float2 TransformObstacleToPadded(float2 uv)
{
	return uv * _ObstaclePadding_ST.xy + _ObstaclePadding_ST.zw;
}

float2 InverseTransformObstacleToPadded(float2 padded_uv)
{
    return (padded_uv - _ObstaclePadding_ST.zw) / _ObstaclePadding_ST.xy;
}

float2 PadVelocityUV(float2 uv)
{
	return uv * _VelocityPadding_ST.xy + _VelocityPadding_ST.zw;
}

float2 WorldPosFromPaddedUV(float2 screenUV)
{
	// 1. Un-pad the UV: Shift the UV so the padded start point is 0,0.
	float2 unshiftedUV = screenUV - _Padding_ST.zw;

	// 2. Normalize the UV: Scale the UV so the padded area now spans [0, 1].
	float2 paddedUV = unshiftedUV / _Padding_ST.xy;

	// 3. Convert to World XZ plane position
	float2 worldXZ = (paddedUV - 0.5f) * _WorldSize.xy; 

	return worldXZ;
}

float Poly(float r, float h)
{
	if (r >= 0.0f && r < h)
	{
		//float v = (315.0f / (64.0f * 3.14*pow(h,9)));
		//float h2 = h * h;
		//float r2 = r * r;
		return 1.0f - (r * r);
	}
	return 0;
}

float UnpackTerrain(float4 height)
{
#if _FLUID_UNITY_TERRAIN
	return UnpackHeightmap(height) * _TerrainHeightScale;
#else
	return height.x * _TerrainHeightScale;
#endif
}

#ifdef USETEXTURE2D
void GetNeighbourData(Texture2D<float4> tex, float4 texelSize, float2 uv, out float4 left, out float4 right, out float4 top, out float4 bottom)
{
#ifndef SHADER_API_WEBGPU
	left = tex.Load(float3(uv.xy, 0), -float2(1, 0));
	right = tex.Load(float3(uv.xy, 0), float2(1, 0));
	top = tex.Load(float3(uv.xy, 0), -float2(0, 1));
	bottom = tex.Load(float3(uv.xy, 0), float2(0, 1));
#else
	left = tex.Load(float3(uv.xy - float2(1, 0), 0));
	right = tex.Load(float3(uv.xy + float2(1, 0), 0));
	top = tex.Load(float3(uv.xy - float2(0, 1), 0));
	bottom = tex.Load(float3(uv.xy + float2(0, 1), 0));
#endif
}

void GetNeighbourDataTerrain(float2 uv, out float4 left, out float4 right, out float4 top, out float4 bottom)
{
	GetNeighbourData(_TerrainHeightField, _FluidHeightField_TexelSize, uv, left, right, top, bottom);
	left = UnpackTerrain(left);
	right = UnpackTerrain(right);
	top = UnpackTerrain(top);
	bottom = UnpackTerrain(bottom);
}

void GetNeighbourDataClamp(Texture2D<float4> tex, float4 texelSize, float2 clampMinMaxX, float2 clampMinMaxY, float2 uv, out float4 left, out float4 right, out float4 top, out float4 bottom)
{
	float3 uvLeft = float3(uv.xy - float2(1, 0), 0);//float3(max(uv.xy - float2(1, 0), clampMinMaxX.x), 0);
	uvLeft.x = max(uvLeft.x, clampMinMaxX.x);
	left = tex.Load(uvLeft);

	float3 uvRight = float3(uv.xy + float2(1, 0), 0);//float3(min(uv.xy + float2(1, 0), clampMinMaxX.y), 0);
	uvRight.x = min(uvRight.x, clampMinMaxX.y);
	right = tex.Load(uvRight);

	float3 uvTop = float3(uv.xy - float2(0, 1), 0);// float3(max(uv.xy - float2(0, 1), clampMinMaxY.x), 0);
	uvTop.y = max(uvTop.y, clampMinMaxY.x);
	top = tex.Load(uvTop);

	float3 uvBot = float3(uv.xy + float2(0, 1), 0);//float3(min(uv.xy + float2(0, 1), clampMinMaxY.y), 0);
	uvBot.y = min(uvBot.y, clampMinMaxY.y);
	bottom = tex.Load(uvBot);
}

void GetNeighbourDataTerrainClamp(float2 clampMinMaxX, float2 clampMinMaxY, float2 uv, out float4 left, out float4 right, out float4 top, out float4 bottom)
{
	GetNeighbourDataClamp(_TerrainHeightField, _FluidHeightField_TexelSize, clampMinMaxX, clampMinMaxY, uv, left, right, top, bottom);
	left = UnpackTerrain(left);
	right = UnpackTerrain(right);
	top = UnpackTerrain(top);
	bottom = UnpackTerrain(bottom);
}

float LoadTerrainHeight(float2 uv, float2 offset = float2(0,0))
{
	return UnpackTerrain(_TerrainHeightField.Load(float3(uv + offset,0)));
}

float4 LoadFluidHeight(float2 uv, float2 offset = float2(0,0))
{
	return _FluidHeightField.Load(float3(uv + offset,0));
}

float4 LoadVelocity(float2 uv, float2 offset = float2(0,0))
{
	return _VelocityField.Load(float3(uv + offset,0));
}

#if SHADER_TARGET >= 40
void GatherTerrainAll(in float2 uv, out float top_left, out float top, out float top_right, 
									 out float center_left, out float center, out float center_right, 
									 out float bottom_left, out float bottom, out float bottom_right)
{
	//[gather1.r,gather0.r,gather0.g]
	//[gather1.w,gather0.w,gather0.b]
	//[gather2.w,gather3.w,gather3.b]

	float4 gather0 = _TerrainHeightField.Gather(sampler_TerrainHeightField, uv);
	float4 gather1 = _TerrainHeightField.Gather(sampler_TerrainHeightField, uv, int2(-1, 0));
	float4 gather2 = _TerrainHeightField.Gather(sampler_TerrainHeightField, uv, int2(-1, -1));
	float4 gather3 = _TerrainHeightField.Gather(sampler_TerrainHeightField, uv, int2( 0, -1));

	top_left = UnpackTerrain(gather1.r); top = UnpackTerrain(gather0.r); top_right = UnpackTerrain(gather0.g);
	center_left = UnpackTerrain(gather1.w); center = UnpackTerrain(gather0.w); center_right = UnpackTerrain(gather0.b);
	bottom_left = UnpackTerrain(gather2.w); bottom = UnpackTerrain(gather3.w); bottom_right = UnpackTerrain(gather3.b);
}
#endif

float SampleTerrainHeight(float2 uv)
{
	return UnpackTerrain(_TerrainHeightField.Sample(sampler_TerrainHeightField, uv));
}

float SampleTerrainHeight(float2 uv, float2 offset)
{
	return UnpackTerrain(_TerrainHeightField.Sample(sampler_TerrainHeightField, uv + offset * _TerrainHeightField_TexelSize.xy));
}

float4 SampleVelocity(in float2 uv)
{
	return _VelocityField.Sample(sampler_VelocityField, uv);
}

float4 SampleVelocity(float2 uv, float2 offset)
{
	return _VelocityField.Sample(sampler_VelocityField, uv + _VelocityField_TexelSize.xy * offset);
}

float2 SampleFluidHeight(in float2 uv)
{
	return _FluidHeightField.Sample(sampler_FluidHeightField, uv).rg;
}

float2 SampleFluidHeight(in float2 uv, float2 offset)
{
	return SampleFluidHeight(uv + _FluidHeightField_TexelSize.xy * offset);
}

float2 SamplePreviousFluidHeight(in float2 uv)
{
	return _PreviousFluidHeightField.Sample(sampler_PreviousFluidHeightField, uv).rg;
}

float2 SamplePreviousFluidHeight(in float2 uv, float2 offset)
{
	return SamplePreviousFluidHeight(uv + _FluidHeightField_TexelSize.xy * offset);
}

#else
void GetNeighbourData(sampler2D tex, float4 texelSize, float2 uv, out float4 left, out float4 right, out float4 top, out float4 bottom)
{
	left = tex2D(tex, uv + float2(-texelSize.x, 0));
	right = tex2D(tex, uv + float2(texelSize.x, 0));
	top = tex2D(tex, uv + float2(0, -texelSize.y));
	bottom = tex2D(tex, uv + float2(0, texelSize.y));
}

void GetNeighbourDataTerrain(float2 uv, out float4 left, out float4 right, out float4 top, out float4 bottom)
{
	GetNeighbourData(_TerrainHeightField, _TerrainHeightField_TexelSize, uv, left, right, top, bottom);
	left = UnpackTerrain(left);
	right = UnpackTerrain(right);
	top = UnpackTerrain(top);
	bottom = UnpackTerrain(bottom);
}

float SampleTerrainHeight(float2 uv)
{
	return UnpackTerrain(tex2D(_TerrainHeightField, uv));
}

float SampleTerrainHeight(float2 uv, float2 offset)
{
	return UnpackTerrain(tex2D(_TerrainHeightField, uv + offset * _TerrainHeightField_TexelSize.xy));
}


float4 SampleVelocity(in float2 uv)
{
	return tex2D(_VelocityField, uv);
}

float4 SampleVelocity(float2 uv, float2 offset)
{
	return SampleVelocity(uv + _VelocityField_TexelSize.xy * offset);
}

float2 SampleFluidHeight(in float2 uv)
{
	return tex2D(_FluidHeightField, uv).rg;
}

float2 SampleFluidHeight(in float2 uv, float2 offset)
{
	return SampleFluidHeight(uv + _FluidHeightField_TexelSize.xy * offset);
}

float2 SamplePreviousFluidHeight(in float2 uv)
{
	return tex2D(_PreviousFluidHeightField, uv).rg;
}

float2 SamplePreviousFluidHeight(in float2 uv, float2 offset)
{
	return SamplePreviousFluidHeight(uv + _FluidHeightField_TexelSize.xy * offset);
}


#endif

void GatherNeighbours(Texture2D tex, SamplerState samplerState, in float2 uv, out float center, out float left, out float right, out float top, out float bottom)
{
	float4 sampleGather = tex.Gather(samplerState, uv);
	center = sampleGather.w;

	float4 sampleGatherTB = tex.Gather(samplerState, uv, -int2(1, 1));
	right = sampleGather.z;
	left = sampleGatherTB.x;
	top = sampleGather.x;
	bottom = sampleGatherTB.z;
}

void GatherNeighbourBilinear(Texture2D tex, SamplerState samplerState, in float2 uv, in float2 bilinear, out float left, out float right, out float top, out float bottom)
{
	float center;
	GatherNeighbours(tex, samplerState, uv, center, left, right, top, bottom);

	left = lerp(center, left, bilinear.x);
	right = lerp(center, right, bilinear.x);

	top = lerp(center, top, bilinear.y);
	bottom = lerp(center, bottom, bilinear.y);
}


float4 GetBilinearSample(sampler2D tex, float2 uv, float2 texelSize_xy)
{
    float2 texSize = 1.0 / texelSize_xy;
                
    float2 targetPx = uv * texSize; 
                
    float2 basePx = floor(targetPx - 0.5) + 0.5;

    float2 t = targetPx - basePx;

    float2 uv00 = basePx * texelSize_xy;
    float2 uv10 = (basePx + float2(1, 0)) * texelSize_xy;
    float2 uv01 = (basePx + float2(0, 1)) * texelSize_xy;
    float2 uv11 = (basePx + float2(1, 1)) * texelSize_xy;
                
    float4 v00 = tex2D(tex, uv00);
    float4 v10 = tex2D(tex, uv10);
    float4 v01 = tex2D(tex, uv01);
    float4 v11 = tex2D(tex, uv11);
                
    float4 vX0 = lerp(v00, v10, t.x);
    float4 vX1 = lerp(v01, v11, t.x);
                
    return lerp(vX0, vX1, t.y);
}

#endif