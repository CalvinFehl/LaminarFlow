#ifndef FLUIDFRENZY_TERRAFORM_TERRAIN_SHADOWCASTER_URP_INCLUDED
#define FLUIDFRENZY_TERRAFORM_TERRAIN_SHADOWCASTER_URP_INCLUDED

#include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Rendering/Shaders/Library/LOD/FluidInstancingCommon.hlsl"
#include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Rendering/Shaders/Terrain/TerraformTerrainInput.hlsl"

#include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Rendering/ExternalCompatibility/FluidExternalCompatibility.cs.hlsl"

#ifdef FLUIDFRENZY_EXTERNALCOMPATIBILITY_CURVEDWORLD
#include "Assets/Amazing Assets/Curved World/Shaders/Core/CurvedWorldTransform.cginc"
#endif // FLUIDFRENZY_EXTERNALCOMPATIBILITY_CURVEDWORLD

// Shadow Casting Light geometric parameters. These variables are used when applying the shadow Normal Bias and are set by UnityEngine.Rendering.Universal.ShadowUtils.SetupShadowCasterConstantBuffer in com.unity.render-pipelines.universal/Runtime/ShadowUtils.cs
// For Directional lights, _LightDirection is used when applying shadow Normal Bias.
// For Spot lights and Point lights, _LightPosition is used to compute the actual light direction because it is different at each shadow caster geometry vertex.
float3 _LightDirection;
float3 _LightPosition;

struct Attributes
{
	float4 positionOS   : POSITION;
	float2 texcoord     : TEXCOORD0;
	FLUID_VERTEX_INPUT_INSTANCE_ID
		UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct ShadowAttributes
{
	float4 positionOS;
	float3 normalOS;
};

struct Varyings
{
	float4 positionCS   : SV_POSITION;
};

float4 GetShadowPositionHClip(ShadowAttributes input)
{
	float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
	float3 normalWS = TransformObjectToWorldNormal(input.normalOS);

#if _CASTING_PUNCTUAL_LIGHT_SHADOW
	float3 lightDirectionWS = normalize(_LightPosition - positionWS);
#else
	float3 lightDirectionWS = _LightDirection;
#endif

	float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));

#if UNITY_REVERSED_Z
	positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
#else
	positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
#endif

	return positionCS;
}

Varyings ShadowPassVertex(Attributes input)
{
	Varyings output = (Varyings)(0);
	UNITY_SETUP_INSTANCE_ID(input);

	TerrainVertexInputs terrainInputs = GetTerrainVertexInputs(input.positionOS.xyz, input.texcoord, FLUID_GET_INSTANCE_ID(input));

	float4 tangentOS = float4(cross(terrainInputs.normalOS, float3(0, 0, 1)), 1);
#if defined(CURVEDWORLD_IS_INSTALLED) && !defined(CURVEDWORLD_DISABLED_ON)
   #ifdef CURVEDWORLD_NORMAL_TRANSFORMATION_ON
      CURVEDWORLD_TRANSFORM_VERTEX_AND_NORMAL(terrainInputs.positionOS, terrainInputs.normalOS, tangentOS)
   #else
      CURVEDWORLD_TRANSFORM_VERTEX(terrainInputs.positionOS)
   #endif
#endif

	ShadowAttributes shadowInputs;
	shadowInputs.positionOS = float4(terrainInputs.positionOS, 1);
	shadowInputs.normalOS = terrainInputs.normalOS;
	output.positionCS = GetShadowPositionHClip(shadowInputs);

	return output;
}

half4 ShadowPassFragment(Varyings input) : SV_TARGET
{
	return 0;
}

#endif // FLUIDFRENZY_TERRAFORM_TERRAIN_SHADOWCASTER_URP_INCLUDED