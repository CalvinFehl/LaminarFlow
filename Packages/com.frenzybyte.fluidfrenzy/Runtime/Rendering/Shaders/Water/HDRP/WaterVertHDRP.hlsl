#ifndef FLUIDFRENZY_WATER_VERT_HDRP_INCLUDED
#define FLUIDFRENZY_WATER_VERT_HDRP_INCLUDED

// --------------------------------------------------
// Structs and Packing
        
struct AttributesMesh
{
	float3 positionOS : POSITION;
	float3 normalOS : NORMAL;
	float4 tangentOS : TANGENT;
	float4 uv0 : TEXCOORD0;
	float4 uv1 : TEXCOORD1;
#if UNITY_ANY_INSTANCING_ENABLED || defined(ATTRIBUTES_NEED_INSTANCEID)
	uint instanceID : INSTANCEID_SEMANTIC;
#else 
	FLUID_VERTEX_INPUT_INSTANCE_ID
#endif
};

struct VaryingsMeshToPS
{
SV_POSITION_QUALIFIERS float4 positionCS : SV_POSITION;
	float3 positionRWS;
	float3 normalWS;
	float4 tangentWS;
	float4 texCoord0;
	float4 texCoord1;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
	uint instanceID : CUSTOM_INSTANCE_ID;
#endif
};
struct VertexDescriptionInputs
{
	float3 ObjectSpaceNormal;
	float3 ObjectSpaceTangent;
	float3 ObjectSpacePosition;
};

struct PackedVaryingsMeshToPS
{
SV_POSITION_QUALIFIERS float4 positionCS : SV_POSITION;
	float4 tangentWS : INTERP0;
	float4 texCoord0 : INTERP1;
	float4 texCoord1 : INTERP2;
	float3 positionRWS : INTERP3;
	float3 normalWS : INTERP4;
#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
	uint instanceID : CUSTOM_INSTANCE_ID;
#endif
};
        
PackedVaryingsMeshToPS PackVaryingsMeshToPS (VaryingsMeshToPS input)
{
	PackedVaryingsMeshToPS output;
	ZERO_INITIALIZE(PackedVaryingsMeshToPS, output);
	output.positionCS = input.positionCS;
	output.tangentWS.xyzw = input.tangentWS;
	output.texCoord0.xyzw = input.texCoord0;
	output.texCoord1.xyzw = input.texCoord1;
	output.positionRWS.xyz = input.positionRWS;
	output.normalWS.xyz = input.normalWS;
	#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
	output.instanceID = input.instanceID;
	#endif
	return output;
}
        
VaryingsMeshToPS UnpackVaryingsMeshToPS (PackedVaryingsMeshToPS input)
{
	VaryingsMeshToPS output;
	output.positionCS = input.positionCS;
	output.tangentWS = input.tangentWS.xyzw;
	output.texCoord0 = input.texCoord0.xyzw;
	output.texCoord1 = input.texCoord1.xyzw;
	output.positionRWS = input.positionRWS.xyz;
	output.normalWS = input.normalWS.xyz;
	#if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
	output.instanceID = input.instanceID;
	#endif
	return output;
}
        
// Graph Vertex
struct VertexDescription
{
	float3 Position;
	float3 Normal;
	float3 Tangent;
};
        
VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
{
	VertexDescription description = (VertexDescription)0;
	description.Position = IN.ObjectSpacePosition;
	description.Normal = IN.ObjectSpaceNormal;
	description.Tangent = IN.ObjectSpaceTangent;
	return description;
}
        
VertexDescriptionInputs AttributesMeshToVertexDescriptionInputs(AttributesMesh input)
{
	VertexDescriptionInputs output;
	ZERO_INITIALIZE(VertexDescriptionInputs, output);
        
	output.ObjectSpaceNormal =                          input.normalOS;
	output.ObjectSpaceTangent =                         input.tangentOS.xyz;
	output.ObjectSpacePosition =                        input.positionOS;
        
	return output;
}
        
VertexDescription GetVertexDescription(AttributesMesh input, float3 timeParameters)
{
	// build graph inputs
	VertexDescriptionInputs vertexDescriptionInputs = AttributesMeshToVertexDescriptionInputs(input);
	// Override time parameters with used one (This is required to correctly handle motion vectors for vertex animation based on time)
        
	// evaluate vertex graph
	VertexDescription vertexDescription = VertexDescriptionFunction(vertexDescriptionInputs);
	return vertexDescription;
}
        
AttributesMesh ApplyMeshModification(AttributesMesh input, float3 timeParameters
#ifdef USE_CUSTOMINTERP_SUBSTRUCT
	#ifdef TESSELLATION_ON
	, inout VaryingsMeshToDS varyings
	#else
	, inout VaryingsMeshToPS varyings
	#endif
#endif
	)
{
	VertexDescription vertexDescription = GetVertexDescription(input, timeParameters);
        
	// copy graph output to the results
	input.positionOS = vertexDescription.Position;
	input.normalOS = vertexDescription.Normal;
	input.tangentOS.xyz = vertexDescription.Tangent;

	FluidData fluidData;
	SampleFluidSimulationData(input.positionOS, input.uv0.xy, input.uv1.xy, FLUID_GET_INSTANCE_ID(input), fluidData);
    input.positionOS = fluidData.positionOS;
    input.normalOS = fluidData.normalOS;
    input.uv0 = fluidData.uv;
    input.uv1 = fluidData.flowUV;
	return input;
}
        
#if defined(_ADD_CUSTOM_VELOCITY) // For shader graph custom velocity
// Return precomputed Velocity in object space
float3 GetCustomVelocity(AttributesMesh input)
{
	VertexDescription vertexDescription = GetVertexDescription(input, _TimeParameters.xyz);
	return vertexDescription.CustomVelocity;
}
#endif

FragInputs BuildFragInputs(VaryingsMeshToPS input)
{
	FragInputs output;
	ZERO_INITIALIZE(FragInputs, output);
        
	// Init to some default value to make the computer quiet (else it output 'divide by zero' warning even if value is not used).
	// TODO: this is a really poor workaround, but the variable is used in a bunch of places
	// to compute normals which are then passed on elsewhere to compute other values...
	output.tangentToWorld = k_identity3x3;
	output.positionSS = input.positionCS;       // input.positionCS is SV_Position
        
	output.positionRWS =                input.positionRWS;
	output.positionPixel =              input.positionCS.xy; // NOTE: this is not actually in clip space, it is the VPOS pixel coordinate value
	output.tangentToWorld =             BuildTangentToWorld(input.tangentWS, input.normalWS);
	output.texCoord0 =                  input.texCoord0;
	output.texCoord1 =                  input.texCoord1;
        
	return output;
}
        
// existing HDRP code uses the combined function to go directly from packed to frag inputs
FragInputs UnpackVaryingsMeshToFragInputs(PackedVaryingsMeshToPS input)
{
	UNITY_SETUP_INSTANCE_ID(input);
	VaryingsMeshToPS unpacked = UnpackVaryingsMeshToPS(input);
	return BuildFragInputs(unpacked);
}


#ifdef _WRITE_TRANSPARENT_MOTION_VECTOR
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/MotionVectorVertexShaderCommon.hlsl"

PackedVaryingsType Vert(AttributesMesh inputMesh, AttributesPass inputPass)
{
    VaryingsType varyingsType;
    varyingsType.vmesh = VertMesh(inputMesh);
    return MotionVectorVS(varyingsType, inputMesh, inputPass);
}

#ifdef TESSELLATION_ON

PackedVaryingsToPS VertTesselation(VaryingsToDS input)
{
    VaryingsToPS output;
    output.vmesh = VertMeshTesselation(input.vmesh);
    return MotionVectorTessellation(output, input);
}

#endif // TESSELLATION_ON

#else // _WRITE_TRANSPARENT_MOTION_VECTOR

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/VertMesh.hlsl"

PackedVaryingsType Vert(AttributesMesh inputMesh)
{
    VaryingsType varyingsType;

#if defined(HAVE_RECURSIVE_RENDERING)
    // If we have a recursive raytrace object, we will not render it.
    // As we don't want to rely on renderqueue to exclude the object from the list,
    // we cull it by settings position to NaN value.
    // TODO: provide a solution to filter dyanmically recursive raytrace object in the DrawRenderer
    if (_EnableRecursiveRayTracing && _RayTracing > 0.0)
    {
        ZERO_INITIALIZE(VaryingsType, varyingsType); // Divide by 0 should produce a NaN and thus cull the primitive.
    }
    else
#endif
    {
        varyingsType.vmesh = VertMesh(inputMesh);
    }

    return PackVaryingsType(varyingsType);
}

#ifdef TESSELLATION_ON

PackedVaryingsToPS VertTesselation(VaryingsToDS input)
{
    VaryingsToPS output;
    output.vmesh = VertMeshTesselation(input.vmesh);

    return PackVaryingsToPS(output);
}

#endif // TESSELLATION_ON

#endif // _WRITE_TRANSPARENT_MOTION_VECTOR


#ifdef TESSELLATION_ON
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/TessellationShare.hlsl"
#endif

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Debug/DebugDisplayMaterial.hlsl"

#if defined(_TRANSPARENT_REFRACTIVE_SORT) || defined(_ENABLE_FOG_ON_TRANSPARENT)
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Water/Shaders/UnderWaterUtilities.hlsl"
#endif


#endif // FLUIDFRENZY_WATER_VERT_HDRP_INCLUDED