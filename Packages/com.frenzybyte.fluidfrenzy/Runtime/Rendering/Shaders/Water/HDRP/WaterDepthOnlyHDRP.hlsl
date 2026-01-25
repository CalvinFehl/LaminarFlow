#ifndef FLUIDFRENZY_WATER_DEPTHONLY_HDRP_INCLUDED
#define FLUIDFRENZY_WATER_DEPTHONLY_HDRP_INCLUDED

// Defines
#define SHADERPASS SHADERPASS_TRANSPARENT_DEPTH_PREPASS
#define RAYTRACING_SHADER_GRAPH_DEFAULT
#define SUPPORT_GLOBAL_MIP_BIAS 1
        
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
        
        
#ifndef SHADER_UNLIT
// We need isFrontFace when using double sided - it is not required for unlit as in case of unlit double sided only drive the cullmode
// VARYINGS_NEED_CULLFACE can be define by VaryingsMeshToPS.FaceSign input if a IsFrontFace Node is included in the shader graph.
#if defined(_DOUBLESIDED_ON) && !defined(VARYINGS_NEED_CULLFACE)
    #define VARYINGS_NEED_CULLFACE
#endif
#endif
        
// Specific Material Define
#define _SPECULAR_OCCLUSION_FROM_AO 1
#define _ENERGY_CONSERVING_SPECULAR 1
        
#if _MATERIAL_FEATURE_COLORED_TRANSMISSION
	// Colored Transmission doesn't support clear coat
	#undef _MATERIAL_FEATURE_CLEAR_COAT
#endif
        
// If we use subsurface scattering, enable output split lighting (for forward pass)
#if defined(_MATERIAL_FEATURE_SUBSURFACE_SCATTERING) && !defined(_SURFACE_TYPE_TRANSPARENT)
	#define OUTPUT_SPLIT_LIGHTING
#endif

// Caution: we can use the define SHADER_UNLIT onlit after the above Material include as it is the Unlit template who define it
        
// To handle SSR on transparent correctly with a possibility to enable/disable it per framesettings
// we should have a code like this:
// if !defined(_DISABLE_SSR_TRANSPARENT)
// pragma multi_compile _ WRITE_NORMAL_BUFFER
// endif
// i.e we enable the multicompile only if we can receive SSR or not, and then C# code drive
// it based on if SSR transparent in frame settings and not (and stripper can strip it).
// this is currently not possible with our current preprocessor as _DISABLE_SSR_TRANSPARENT is a keyword not a define
// so instead we used this and chose to pay the extra cost of normal write even if SSR transaprent is disabled.
// Ideally the shader graph generator should handle it but condition below can't be handle correctly for now.
#if SHADERPASS == SHADERPASS_TRANSPARENT_DEPTH_PREPASS
#if !defined(_DISABLE_SSR_TRANSPARENT) && !defined(SHADER_UNLIT)
    #define WRITE_NORMAL_BUFFER
#endif
#endif
        
// See Lit.shader
#if SHADERPASS == SHADERPASS_MOTION_VECTORS && defined(WRITE_DECAL_BUFFER_AND_RENDERING_LAYER)
    #define WRITE_DECAL_BUFFER
#endif
        
#ifndef DEBUG_DISPLAY
    // In case of opaque we don't want to perform the alpha test, it is done in depth prepass and we use depth equal for ztest (setup from UI)
    // Don't do it with debug display mode as it is possible there is no depth prepass in this case
    #if !defined(_SURFACE_TYPE_TRANSPARENT)
        #if SHADERPASS == SHADERPASS_FORWARD
        #define SHADERPASS_FORWARD_BYPASS_ALPHA_TEST
        #elif SHADERPASS == SHADERPASS_GBUFFER
        #define SHADERPASS_GBUFFER_BYPASS_ALPHA_TEST
        #endif
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

#include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Rendering/Shaders/Water/WaterInput.hlsl"

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Debug/DebugDisplay.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/NormalSurfaceGradient.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/Lit.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/DecalUtilities.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/LitDecalData.hlsl"

#include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Rendering/Shaders/Water/WaterInputData.hlsl"
#include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Rendering/Shaders/Water/WaterSurfaceData.hlsl"
#include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Rendering/Shaders/Water/WaterRenderingCommon.hlsl"
#include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Rendering/Shaders/Water/HDRP/WaterVertHDRP.hlsl"
#include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Rendering/Shaders/Water/HDRP/WaterCommonHDRP.hlsl"

#if defined(WRITE_NORMAL_BUFFER) && defined(WRITE_MSAA_DEPTH)
#define SV_TARGET_DECAL SV_Target2
#elif defined(WRITE_NORMAL_BUFFER) || defined(WRITE_MSAA_DEPTH)
#define SV_TARGET_DECAL SV_Target1
#else
#define SV_TARGET_DECAL SV_Target0
#endif

void Frag(  PackedVaryingsToPS packedInput
            #if defined(SCENESELECTIONPASS) || defined(SCENEPICKINGPASS)
            , out float4 outColor : SV_Target0
            #else
                #ifdef WRITE_MSAA_DEPTH
                // We need the depth color as SV_Target0 for alpha to coverage
                , out float4 depthColor : SV_Target0
                    #ifdef WRITE_NORMAL_BUFFER
                    , out float4 outNormalBuffer : SV_Target1
                    #endif
                #else
                    #ifdef WRITE_NORMAL_BUFFER
                    , out float4 outNormalBuffer : SV_Target0
                    #endif
                #endif

                // Decal buffer must be last as it is bind but we can optionally write into it (based on _DISABLE_DECALS)
                #if (defined(WRITE_DECAL_BUFFER) && !defined(_DISABLE_DECALS)) || defined(WRITE_RENDERING_LAYER)
                , out float4 outDecalBuffer : SV_TARGET_DECAL
                #endif
            #endif

            #if defined(_DEPTHOFFSET_ON) && !defined(SCENEPICKINGPASS)
            , out float outputDepth : DEPTH_OFFSET_SEMANTIC
            #endif
        )
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(packedInput);
    FragInputs input = UnpackVaryingsToFragInputs(packedInput);

    // input.positionSS is SV_Position
    PositionInputs posInput = GetPositionInput(input.positionSS.xy, _ScreenSize.zw, input.positionSS.z, input.positionSS.w, input.positionRWS);
	FluidInputData fluidInput;
	InitializeFluidInputData(input, fluidInput);

	ClipFluid(fluidInput.fluidMask, _Layer);

	WaterSurfaceData waterSurfaceData;
	InitializeWaterSurfaceData(fluidInput, waterSurfaceData);

	WaterInputData waterInput;
	InitializeWaterInputData(input, fluidInput, waterSurfaceData, waterInput);

    SurfaceData surfaceData;
    BuiltinData builtinData;
    GetSurfaceAndBuiltinData(input, waterInput.viewDirectionWS, posInput, surfaceData, builtinData, fluidInput, waterSurfaceData, waterInput);

#if defined(_DEPTHOFFSET_ON) && !defined(SCENEPICKINGPASS)
    outputDepth = posInput.deviceDepth;


#if SHADERPASS == SHADERPASS_SHADOWS
    // If we are using the depth offset and manually outputting depth, the slope-scale depth bias is not properly applied
    // we need to manually apply.
    float bias = max(abs(ddx(posInput.deviceDepth)), abs(ddy(posInput.deviceDepth))) * _SlopeScaleDepthBias;
    outputDepth += bias;
#endif

#endif

#ifdef SCENESELECTIONPASS
    // We use depth prepass for scene selection in the editor, this code allow to output the outline correctly
    outColor = float4(_ObjectId, _PassValue, 1.0, 1.0);
#elif defined(SCENEPICKINGPASS)
    outColor = unity_SelectionID;
#else

    // Depth and Alpha to coverage
    #ifdef WRITE_MSAA_DEPTH
    // In case we are rendering in MSAA, reading the an MSAA depth buffer is way too expensive. To avoid that, we export the depth to a color buffer
    depthColor = packedInput.vmesh.positionCS.z;

    // Alpha channel is used for alpha to coverage
    depthColor.a = SharpenAlpha(builtinData.opacity, builtinData.alphaClipTreshold);
    #endif

    #if defined(WRITE_NORMAL_BUFFER)
    EncodeIntoNormalBuffer(ConvertSurfaceDataToNormalData(surfaceData), outNormalBuffer);
    #endif

#if (defined(WRITE_DECAL_BUFFER) && !defined(_DISABLE_DECALS)) || defined(WRITE_RENDERING_LAYER)
    DecalPrepassData decalPrepassData;
    #ifdef _DISABLE_DECALS
    ZERO_INITIALIZE(DecalPrepassData, decalPrepassData);
    #else
    // We don't have the right to access SurfaceData in a shaderpass.
    // However it would be painful to have to add a function like ConvertSurfaceDataToDecalPrepassData() to every Material to return geomNormalWS anyway
    // Here we will put the constrain that any Material requiring to support Decal, will need to have geomNormalWS as member of surfaceData (and we already require normalWS anyway)
    decalPrepassData.geomNormalWS = surfaceData.geomNormalWS;
    #endif
    decalPrepassData.renderingLayerMask = GetMeshRenderingLayerMask();
    EncodeIntoDecalPrepassBuffer(decalPrepassData, outDecalBuffer);
#endif

#endif // SCENESELECTIONPASS
}

#endif // FLUIDFRENZY_WATER_DEPTHONLY_HDRP_INCLUDED