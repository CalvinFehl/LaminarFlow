#ifndef FLUIDFRENZY_WATER_FORWARD_HDRP_INCLUDED
#define FLUIDFRENZY_WATER_FORWARD_HDRP_INCLUDED

// Defines
#define SHADERPASS SHADERPASS_FORWARD
#define SUPPORT_BLENDMODE_PRESERVE_SPECULAR_LIGHTING 1
#define HAS_LIGHTLOOP 1
#define RAYTRACING_SHADER_GRAPH_DEFAULT
#define SHADER_LIT 1
#define SUPPORT_GLOBAL_MIP_BIAS 1
#define REQUIRE_OPAQUE_TEXTURE

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/GeometricTools.hlsl" // Required by Tessellation.hlsl
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Tessellation.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl" // Required to be include before we include properties as it define DECLARE_STACK_CB
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl" // Required before including properties as it defines UNITY_TEXTURE_STREAMING_DEBUG_VARS
        
// --------------------------------------------------
// Defines
        
// Attribute
#define ATTRIBUTES_NEED_NORMAL
#define ATTRIBUTES_NEED_TANGENT
#define ATTRIBUTES_NEED_TEXCOORD0
#define ATTRIBUTES_NEED_TEXCOORD1
#define VARYINGS_NEED_POSITION_WS
#define VARYINGS_NEED_TANGENT_TO_WORLD
#define VARYINGS_NEED_TEXCOORD0
#define VARYINGS_NEED_TEXCOORD1
        
#define HAVE_MESH_MODIFICATION
        
//Strip down the FragInputs.hlsl (on graphics), so we can only optimize the interpolators we use.
//if by accident something requests contents of FragInputs.hlsl, it will be caught as a compiler error
//Frag inputs stripping is only enabled when FRAG_INPUTS_ENABLE_STRIPPING is set
#if !defined(SHADER_STAGE_RAY_TRACING) && SHADERPASS != SHADERPASS_RAYTRACING_GBUFFER && SHADERPASS != SHADERPASS_FULL_SCREEN_DEBUG
#define FRAG_INPUTS_ENABLE_STRIPPING
#endif
#define FRAG_INPUTS_USE_TEXCOORD0
#define FRAG_INPUTS_USE_TEXCOORD1
        
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl"
        
// We need isFrontFace when using double sided - it is not required for unlit as in case of unlit double sided only drive the cullmode
// VARYINGS_NEED_CULLFACE can be define by VaryingsMeshToPS.FaceSign input if a IsFrontFace Node is included in the shader graph.
#if defined(_DOUBLESIDED_ON) && !defined(VARYINGS_NEED_CULLFACE)
    #define VARYINGS_NEED_CULLFACE
#endif
        
// Specific Material Define
#define _SPECULAR_OCCLUSION_FROM_AO 1
#define _ENERGY_CONSERVING_SPECULAR 1
        
// See Lit.shader
#if SHADERPASS == SHADERPASS_MOTION_VECTORS && defined(WRITE_DECAL_BUFFER_AND_RENDERING_LAYER)
    #define WRITE_DECAL_BUFFER
#endif
        
// In case of opaque we don't want to perform the alpha test, it is done in depth prepass and we use depth equal for ztest (setup from UI)
// Don't do it with debug display mode as it is possible there is no depth prepass in this case
#if !defined(_SURFACE_TYPE_TRANSPARENT)
    #if SHADERPASS == SHADERPASS_FORWARD
    #define SHADERPASS_FORWARD_BYPASS_ALPHA_TEST
    #elif SHADERPASS == SHADERPASS_GBUFFER
    #define SHADERPASS_GBUFFER_BYPASS_ALPHA_TEST
    #endif
#endif
        
// Define _DEFERRED_CAPABLE_MATERIAL for shader capable to run in deferred pass
#if defined(SHADER_LIT) && !defined(_SURFACE_TYPE_TRANSPARENT)
    #define _DEFERRED_CAPABLE_MATERIAL
#endif
        
// Translate transparent motion vector define
#if (defined(_TRANSPARENT_WRITES_MOTION_VEC) || defined(_TRANSPARENT_REFRACTIVE_SORT)) && defined(_SURFACE_TYPE_TRANSPARENT)
    #define _WRITE_TRANSPARENT_MOTION_VECTOR
#endif
        
// Object and Global properties
        
// -- Property used by ScenePickingPass
#ifdef SCENEPICKINGPASS
float4 _SelectionID;
#endif
        
// -- Properties used by SceneSelectionPass
#ifdef SCENESELECTIONPASS
int _ObjectId;
int _PassValue;
#endif
        
// Includes
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"

#include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Rendering/Shaders/Library/FluidRenderingHelpers.hlsl"

#include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Rendering/Shaders/Library/FluidRenderingCommon.hlsl"
#include_with_pragmas "Packages/com.frenzybyte.fluidfrenzy/Runtime/Rendering/ExternalCompatibility/FluidThirdPartyHeadersHDRP.hlsl"
#include_with_pragmas "Packages/com.frenzybyte.fluidfrenzy/Runtime/Rendering/Shaders/Library/FluidFogHelpers.hlsl"

#include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Rendering/Shaders/Water/WaterInput.hlsl"

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Debug/DebugDisplay.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/NormalSurfaceGradient.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/LightLoopDef.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/Lit.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/LightLoop.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/DecalUtilities.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/LitDecalData.hlsl"

#include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Rendering/Shaders/Water/WaterInputData.hlsl"
#include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Rendering/Shaders/Water/WaterSurfaceData.hlsl"
#include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Rendering/Shaders/Water/WaterRenderingCommon.hlsl"
#include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Rendering/Shaders/Water/HDRP/WaterVertHDRP.hlsl"
#include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Rendering/Shaders/Water/HDRP/WaterCommonHDRP.hlsl"
#include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Rendering/Shaders/Water/HDRP/WaterLightingHDRP.hlsl"


#if defined(_WRITE_TRANSPARENT_MOTION_VECTOR)
    #define MOTION_VECTOR_TARGET SV_Target1
    #ifdef _TRANSPARENT_REFRACTIVE_SORT
        #define BEFORE_REFRACTION_TARGET SV_Target2
        #define BEFORE_REFRACTION_ALPHA_TARGET SV_Target3
    #endif
#endif

void Frag(PackedVaryingsToPS packedInput
    , out float4 outColor : SV_Target0  // outSpecularLighting when outputting split lighting
	#if defined(_WRITE_TRANSPARENT_MOTION_VECTOR)
          , out float4 outMotionVec : MOTION_VECTOR_TARGET
        #ifdef _TRANSPARENT_REFRACTIVE_SORT
          , out float4 outBeforeRefractionColor : BEFORE_REFRACTION_TARGET
          , out float4 outBeforeRefractionAlpha : BEFORE_REFRACTION_ALPHA_TARGET
        #endif
    #endif
)
{
#ifdef _WRITE_TRANSPARENT_MOTION_VECTOR
    // Init outMotionVector here to solve compiler warning (potentially unitialized variable)
    // It is init to the value of forceNoMotion (with 2.0)
    // Always write 1.0 in alpha since blend mode could be active on this target as a side effect of VT feedback buffer
    // motion vector expected output format is RG16
    outMotionVec = float4(2.0, 0.0, 0.0, 1.0);
#endif

    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(packedInput);
    FragInputs input = UnpackVaryingsToFragInputs(packedInput);

    AdjustFragInputsToOffScreenRendering(input, _OffScreenRendering > 0, _OffScreenDownsampleFactor);

	FluidInputData fluidInput;
	InitializeFluidInputData(input, fluidInput);

	ClipFluid(fluidInput.fluidMask, _Layer);

	WaterSurfaceData waterSurfaceData;
	InitializeWaterSurfaceData(fluidInput, waterSurfaceData);

	WaterInputData waterInput;
	InitializeWaterInputData(input, fluidInput, waterSurfaceData, waterInput);

	outColor = WaterLightingPBR(input, fluidInput, waterSurfaceData,  waterInput);

#ifdef _WRITE_TRANSPARENT_MOTION_VECTOR
    VaryingsPassToPS inputPass = UnpackVaryingsPassToPS(packedInput.vpass);
    bool forceNoMotion = any(unity_MotionVectorsParams.yw == 0.0);
    // outMotionVec is already initialize at the value of forceNoMotion (see above)

    if (!forceNoMotion)
    {
        float2 motionVec = CalculateMotionVector(inputPass.positionCS, inputPass.previousPositionCS);
        EncodeMotionVector(motionVec * 0.5, outMotionVec);
        // Always write 1.0 in alpha since blend mode could be active on this target as a side effect of VT feedback buffer
        // motion vector expected output format is RG16
        outMotionVec.zw = 1.0;
    }
#endif

}


#endif // FLUIDFRENZY_WATER_FORWARD_HDRP_INCLUDED