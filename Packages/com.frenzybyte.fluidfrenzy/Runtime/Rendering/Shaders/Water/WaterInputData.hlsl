#ifndef FLUIDFRENZY_WATER_INPUTDATA_INCLUDED
#define FLUIDFRENZY_WATER_INPUTDATA_INCLUDED

struct WaterInputData
{
	float3  positionWS;

	float2 normalizedScreenSpaceUV;
	half sceneDepth;
	half sceneZ;
	half pixelZ;
	half waterDepth;
	half frontFaceMask;

	half refractedSceneDepth;
	half refractedSceneZ;
	half refractedDistance;
	half4 refractionColor;

	half3 reflectionColor;

	float3 normalWS;
	float3 tangentWS;
	float3 bitangentWS;

    half3 viewDirectionWS;

	half shadow;

	float3 waveNormalWS;
	float3 foamNormalWS;

	half fade;
	half foamMask;

	half4 screenspaceParticles;

	float4  shadowCoord;
	half	fogCoord;
	half3   bakedGI;
	half3   vertexLighting;
};

#endif // FLUIDFRENZY_WATER_INPUTDATA_INCLUDED