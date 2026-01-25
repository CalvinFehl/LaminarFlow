#ifndef FLUIDFRENZY_TERRAFORM_TERRAIN_DEPTHNORMALS_URP_INCLUDED
#define FLUIDFRENZY_TERRAFORM_TERRAIN_DEPTHNORMALS_URP_INCLUDED

#include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Rendering/Shaders/Library/LOD/FluidInstancingCommon.hlsl"
#include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Rendering/Shaders/Terrain/TerraformTerrainInput.hlsl"
#include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Rendering/Shaders/Terrain/URP/TerraformTerrainLitURP.hlsl"

#include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Rendering/ExternalCompatibility/FluidExternalCompatibility.cs.hlsl"

#ifdef FLUIDFRENZY_EXTERNALCOMPATIBILITY_CURVEDWORLD
#include "Assets/Amazing Assets/Curved World/Shaders/Core/CurvedWorldTransform.cginc"
#endif // FLUIDFRENZY_EXTERNALCOMPATIBILITY_CURVEDWORLD

struct Attributes
{
	float4 positionOS   : POSITION;
	float2 texcoord : TEXCOORD0;
	FLUID_VERTEX_INPUT_INSTANCE_ID
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
	float4 positionCS   : SV_POSITION;
	float3 normalWS		: TEXCOORD1;
	float4 tangentWS	: TEXCOORD2;
	float2 uv			: TEXCOORD3;
	UNITY_VERTEX_INPUT_INSTANCE_ID
	UNITY_VERTEX_OUTPUT_STEREO
};

Varyings DepthNormalsVertex(Attributes input)
{
	Varyings output = (Varyings)(0);
	UNITY_SETUP_INSTANCE_ID(input);
	UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

	TerrainVertexInputs terrainInputs = GetTerrainVertexInputs(input.positionOS.xyz, input.texcoord, FLUID_GET_INSTANCE_ID(input));

	float4 tangentOS = float4(cross(terrainInputs.normalOS, float3(0, 0, 1)), 1);

#if defined(CURVEDWORLD_IS_INSTALLED) && !defined(CURVEDWORLD_DISABLED_ON)
   #ifdef CURVEDWORLD_NORMAL_TRANSFORMATION_ON
      CURVEDWORLD_TRANSFORM_VERTEX_AND_NORMAL(terrainInputs.positionOS, terrainInputs.normalOS, tangentOS)
   #else
      CURVEDWORLD_TRANSFORM_VERTEX(terrainInputs.positionOS)
   #endif
#endif

	VertexPositionInputs vertexInput = GetVertexPositionInputs(terrainInputs.positionOS);
	output.positionCS = vertexInput.positionCS;
	output.uv = terrainInputs.uv;

	VertexNormalInputs normalInput = GetVertexNormalInputs(terrainInputs.normalOS, tangentOS);
	float sign = tangentOS.w * float(GetOddNegativeScale());
	half4 tangentWS = half4(normalInput.tangentWS.xyz, sign);

	output.normalWS = half3(normalInput.normalWS);
	output.tangentWS = tangentWS;

	return output;
}

half4 DepthNormalsFragment(Varyings input) : SV_TARGET
{
	UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

	#if defined(_FLUIDFRENZY_INSTANCING)
		input.normalWS = CalculateNormalFromHeightField(_HeightField, sampler_HeightField, _HeightField_TexelSize, _TexelWorldSize.xy, input.uv.xy, 1, 1);
		input.normalWS = mul((float3x3)unity_ObjectToWorld, input.normalWS);
		input.tangentWS.xyz = normalize(cross(input.normalWS.xyz, float3(0, 0, 1)));
		input.tangentWS.w = -unity_WorldTransformParams.w;
	#endif

	#if defined(_GBUFFER_NORMALS_OCT)
		float3 normalWS = normalize(input.normalWS);
		float2 octNormalWS = PackNormalOctQuadEncode(normalWS);           // values between [-1, +1], must use fp32 on some platforms
		float2 remappedOctNormalWS = saturate(octNormalWS * 0.5 + 0.5);   // values between [ 0,  1]
		half3 packedNormalWS = PackFloat2To888(remappedOctNormalWS);      // values between [ 0,  1]
		return half4(packedNormalWS, 0.0);
	#else
		float2 uv = input.uv;
		float sgn = input.tangentWS.w;      // should be either +1 or -1
		float3 bitangent = sgn * cross(input.normalWS.xyz, input.tangentWS.xyz);
		float3 normalTS = SampleTerrainNormal(uv);
		float3 normalWS = TransformTangentToWorld(normalTS, half3x3(input.tangentWS.xyz, bitangent.xyz, input.normalWS.xyz));

		return half4(NormalizeNormalPerPixel(normalWS), 0.0);
	#endif
}

#endif // FLUIDFRENZY_TERRAFORM_TERRAIN_DEPTHNORMALS_URP_INCLUDED