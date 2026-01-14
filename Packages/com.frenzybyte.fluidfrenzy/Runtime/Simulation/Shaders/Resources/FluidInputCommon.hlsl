#ifndef _FLUIDINPUT_COMMON_HLSL_
#define _FLUIDINPUT_COMMON_HLSL_

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"

#define MIX_MODE_SET 0.0f
#define MIX_MODE_MIN 3.0f
#define MIX_MODE_MAX 4.0f

sampler2D _MainTex;
float _IncreaseStrength;
float _IncreaseExponent;
float2 _RemapRange;

float4 _ModifierCenter;
float4 _ModifierSize;
float4x4 _ModifierRotationMatrix;
float4 _LayerMask;
float4 _SplatmapMask;
float4 _BottomLayersMask;
float2 _BlitRotation;
float _FallbackBlendOp;

float RemapNormalizedToRange(float input, float2 range)
{
    float output = range.x + (range.y - range.x) * input;
    return output;
}

float4 SampleBase(float2 uv)
{
#ifdef INPUT_IS_FLUID
	return SampleTerrainHeight(uv);
#else
#ifdef USETEXTURE2D
	return _TerrainHeightField.Sample(sampler_TerrainHeightField, uv);
#else
	return tex2D(_TerrainHeightField, uv).rgba;
#endif
#endif
}

float2 TransformBlit(float2 v)
{
	float2 sinCos = _BlitRotation;
	sinCos.x = -sinCos.x;
	v.xy -= 0.5f;

	v.xy = v.xy * _BlitScaleBiasRt.xy;
	v.xy = float2(v.x * sinCos.y - v.y * sinCos.x, v.x * sinCos.x + v.y * sinCos.y);
	v.xy = v.xy + _BlitScaleBiasRt.zw;

    return v;
}

struct v2f
{
	float2 uv : TEXCOORD0;
	float4 vertex : SV_POSITION;
};

v2f vert(uint vid : SV_VertexID)
{
	v2f o;
	o.vertex = GetQuadVertexPosition(vid);
	o.vertex.xy = TransformBlit(o.vertex.xy);
#if UNITY_UV_STARTS_AT_TOP
	o.vertex.xy = o.vertex.xy * float2(2.0f, -2.0f) + float2(-1.0f, 1.0f); //convert to -1..1
#else
	o.vertex.xy = o.vertex.xy * float2(2.0f, 2.0f) + float2(-1.0f, -1.0f); //convert to -1..1
#endif
	o.uv.xy = GetQuadTexCoord(vid) * 2 - 1;
	return o;
}

		
v2f vertTex(uint vid : SV_VertexID)
{
	v2f o;
	o.vertex = GetQuadVertexPosition(vid);
	o.vertex.xy = TransformBlit(o.vertex.xy);
#if UNITY_UV_STARTS_AT_TOP
	o.vertex.xy = o.vertex.xy * float2(2.0f, -2.0f) + float2(-1.0f, 1.0f); //convert to -1..1
#else
	o.vertex.xy = o.vertex.xy * float2(2.0f, 2.0f) + float2(-1.0f, -1.0f); //convert to -1..1
#endif

#if UNITY_UV_STARTS_AT_TOP
	o.uv.xy = GetQuadTexCoord(vid);
#else
	o.uv.xy = GetQuadTexCoord(vid);
	o.uv.y = 1 - o.uv.y;
#endif
	return o;
}


v2f vertTexNoFlip(uint vid : SV_VertexID)
{
	v2f o;
	o.vertex = GetQuadVertexPosition(vid);
	o.vertex.xy = o.vertex.xy * _BlitScaleBiasRt.xy + _BlitScaleBiasRt.zw - _BlitScaleBiasRt.xy * 0.5f; //Focus around the center of the volume
	o.vertex.xy = o.vertex.xy * float2(2.0f, -2.0f) + float2(-1.0f, 1.0f); //convert to -1..1
	o.uv.xy = GetQuadTexCoord(vid);
	return o;
}


float DistanceCircle(float2 uv)
{
	return length(uv.xy);
}

float DistanceBox(float2 uv)
{
	return max(abs(uv.x), abs(uv.y));
}

float4 CalculateStrength(float dist, float2 screenPos, float4 texelSize)
{
#if !OPEN_BORDER
	screenPos -= 0.5f;
	if (screenPos.x <= 1 || screenPos.x >= texelSize.z - 1 || screenPos.y <= 1 || screenPos.y >= texelSize.w - 1)
		return 0;
#endif
	return pow(max(0, 1 - dist), _IncreaseExponent) * _IncreaseStrength * _LayerMask;
}

void DiscardBoundary(float2 screenPos, float4 texelSize)
{
#if !OPEN_BORDER
	screenPos -= 0.5f;
	if (screenPos.x <= 1 || screenPos.x >= texelSize.z - 1 || screenPos.y <= 1 || screenPos.y >= texelSize.w - 1)
		discard;
#endif
}

float4 fragCircleAdd(float2 uv, float2 screenPos, float4 texelSize) 
{
	float dist = DistanceCircle(uv.xy);
	float4 scale = CalculateStrength(dist, screenPos.xy, texelSize);

	return scale;
}	
		
float4 fragSquareAdd(float2 uv, float2 screenPos, float4 texelSize) 
{
	float dist = DistanceBox(uv.xy);
	float4 scale = CalculateStrength(dist, screenPos.xy, texelSize);

	return scale;
}	


float4 fragTexAdd(float2 uv, float2 screenPos, float4 texelSize)
{
	DiscardBoundary(screenPos, texelSize);

	return RemapNormalizedToRange(tex2D(_MainTex, uv).r, _RemapRange).xxxx * _IncreaseStrength * _LayerMask;
}


float4 fragCircleSetDepth(float2 uv, float2 screenPos, float4 texelSize) 
{
	DiscardBoundary(screenPos.xy, texelSize);

	float dist = DistanceCircle(uv.xy);
	clip(1 - dist);
	float4 result = CalculateStrength(dist, screenPos.xy, texelSize);

	float4 base = 0;
	#if _BLEND_NOT_SUPPORTED
		base = SampleBase(screenPos.xy * texelSize.xy);
		if(_FallbackBlendOp == MIX_MODE_MIN)
		{
			result = min(result, base);
		}
		else if(_FallbackBlendOp == MIX_MODE_MAX)
		{
			result = max(result, base);
		}
	#endif

	return max(0,result);
}

float4 fragSquareSetDepth(float2 uv, float2 screenPos, float4 texelSize) 
{
	DiscardBoundary(screenPos.xy, texelSize);

	float dist = DistanceBox(uv.xy);
	clip(1 - dist);
	float4 result = CalculateStrength(dist, screenPos.xy, texelSize);

	float4 base = 0;
	#if _BLEND_NOT_SUPPORTED
		base = SampleBase(screenPos.xy * texelSize.xy);
		if(_FallbackBlendOp == MIX_MODE_MIN)
		{
			result = min(result, base);
		}
		else if(_FallbackBlendOp == MIX_MODE_MAX)
		{
			result = max(result, base);
		}
	#endif

	return max(0,result);
}

float4 fragTexSetDepth(float2 uv, float2 screenPos, float4 texelSize)
{
	DiscardBoundary(screenPos, texelSize);
	float4 result = RemapNormalizedToRange(tex2D(_MainTex, uv).r, _RemapRange).xxxx * _IncreaseStrength * _LayerMask;
	float4 base = 0;
	#if _BLEND_NOT_SUPPORTED
		base = SampleBase(screenPos.xy * texelSize.xy);

		if(_FallbackBlendOp == MIX_MODE_MIN)
		{
			result = min(result, base);
		}
		else if(_FallbackBlendOp == MIX_MODE_MAX)
		{
			result = max(result, base);
		}

	#endif
	
	return max(0,result);
}

#endif