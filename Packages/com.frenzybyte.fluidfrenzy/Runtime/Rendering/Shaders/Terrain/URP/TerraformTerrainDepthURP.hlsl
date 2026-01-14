#ifndef FLUIDFRENZY_TERRAFORM_TERRAIN_DEPTH_URP_INCLUDED
#define FLUIDFRENZY_TERRAFORM_TERRAIN_DEPTH_URP_INCLUDED

#include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Rendering/Shaders/Library/LOD/FluidInstancingCommon.hlsl"
#include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Rendering/Shaders/Terrain/TerraformTerrainInput.hlsl"

#include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Rendering/ExternalCompatibility/FluidExternalCompatibility.cs.hlsl"

#ifdef FLUIDFRENZY_EXTERNALCOMPATIBILITY_CURVEDWORLD
#include "Assets/Amazing Assets/Curved World/Shaders/Core/CurvedWorldTransform.cginc"
#endif // FLUIDFRENZY_EXTERNALCOMPATIBILITY_CURVEDWORLD

struct Attributes
{
	float4 positionOS     : POSITION;
	float2 texcoord     : TEXCOORD0;
	FLUID_VERTEX_INPUT_INSTANCE_ID
		UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
	float4 positionCS   : SV_POSITION;
	UNITY_VERTEX_INPUT_INSTANCE_ID
	UNITY_VERTEX_OUTPUT_STEREO
};

Varyings DepthOnlyVertex(Attributes input)
{
	Varyings output = (Varyings)0;
	UNITY_SETUP_INSTANCE_ID(input);
	UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

	TerrainVertexInputs terrainInputs = GetTerrainVertexInputs(input.positionOS.xyz, input.texcoord, FLUID_GET_INSTANCE_ID(input));

#if defined(CURVEDWORLD_IS_INSTALLED) && !defined(CURVEDWORLD_DISABLED_ON)
    CURVEDWORLD_TRANSFORM_VERTEX(terrainInputs.positionOS)
#endif

	output.positionCS = TransformObjectToHClip(terrainInputs.positionOS.xyz);
	return output;
}

half4 DepthOnlyFragment(Varyings input) : SV_TARGET
{
	return 0;
}

#endif // FLUIDFRENZY_TERRAFORM_TERRAIN_DEPTH_URP_INCLUDED