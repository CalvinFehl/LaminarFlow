Shader "FluidFrenzy/Water"
{
    Properties
    {
        [HideInInspector][CurvedWorldBendSettings] _CurvedWorldBendSettings("0|1|1", Vector) = (0, 0, 0, 0)
		//[Header(Lighting)][Space(5)]

		[HideInInspector]_SpecularIntensity("Specular Intensity", Range(0,10)) = 1
		[HideInInspector][KeywordEnum(Off,Hard,Soft)] _ShadowMap("Shadows",Integer) = 1
		[HideInInspector][Toggle(_PLANAR_REFLECTION_ON)] _PlanarReflections("Planar Reflections", Integer) = 0
		[HideInInspector]_ReflectionDistortion("Distortion", Range(0,0.2)) = 0.1
		[HideInInspector]_ReflectivityMin("Base Reflectivity", Range(0,1)) = 0.0
		[HideInInspector]_ScreenSpaceRefraction("Refraction", Integer) = 1
		[HideInInspector]_WaterColor("Water Color", Color) = (0,0.2356132,0.254717,1)
		[HideInInspector]_AbsorptionDepthScale("Absorption Depth Scale", Range(0,1)) = 0.5
		[HideInInspector]_RefractionDistortion("Distortion", Range(0,0.2)) = 0.05

		[Header(Subsurface Scattering)][Space(5)]
		[HideInInspector]_ScatterColor("Scatter Color", Color) = (0.3490196,0.6313726,0.5843138,1)
		[HideInInspector]_ScatterIntensity("Scatter Intensity", Range(0,5)) = 1
		[HideInInspector]_ScatterLightIntensity("Scatter Light Angle Contribution", Range(0,10)) = 10
		[HideInInspector]_ScatterViewIntensity("Scatter View Angle Contribution", Range(0,1)) = 0.1
		[HideInInspector]_ScatterFoamIntensity("Scatter Foam Contribution", Range(0,1)) = 0.5
		[HideInInspector]_ScatterAmbient("Scatter Ambient", Range(0,1)) = 0.1

		//[Header(Waves)][Space(5)]
		[HideInInspector][Toggle(_DISPLACEMENTWAVES_ON)] _DisplacementWaves("Wave Displacement", Integer) = 0
		[HideInInspector]_DisplacementWaveAmplitude("Wave Displacement Amplitude", Range(0,1)) = 0.25
		[HideInInspector]_DisplacementPhase("Displacement Phase Speed", Range(0,1)) = 1
		[HideInInspector]_DisplacementWaveSpeed("Displacement Wave Speed", Range(0,1)) = 0.1
		[HideInInspector]_DisplacementWaveLength("Displacement Wave Length", Range(0.01,10)) = 2
		[HideInInspector]_DisplacementSteepness("Displacement Wave Steepness", Range(0,1)) = 0
		[HideInInspector]_DisplacementScale("Displacement Scale", Range(0,500)) = 200
		[HideInInspector]_WaveNormals("Wave Normals", 2D) = "bump" {}
		[HideInInspector]_WaveNormalStrength("Wave Normal Strength", Float) = 0.5

		[Header(Foam)][Space(5)]
		[HideInInspector]_FoamColor("Foam Color", Color) = (0.85,0.85,0.85,1)
		[HideInInspector]_FoamVisibility("Foam Map Visbility(Strength, Power)", Vector) = (0,1,0,0)
		[HideInInspector]_FoamTexture("Foam Mask", 2D) = "black" {}
		[HideInInspector]_FoamNormalMap("Foam Normal", 2D) = "bump" {}
		[HideInInspector]_FoamNormalStrength("Foam Normal Strength", Float) = 2

		[HideInInspector][Toggle(_SCREENSPACE_FOAMMASK_ON)] _FoamScreenSpace("Foam Screenspace",Integer) = 0
		[HideInInspector][KeywordEnum(Albedo, Clip ,Mask)] _FoamMode("Foam Mode",Integer) = 0

		[HideInInspector]_FadeHeight("Fade Height", Range(0.00001, 1)) = 0.5
		[HideInInspector]_LinearClipOffset("Clipping Offset", Range(0.0, 10.0)) = 0
		[HideInInspector]_ExponentialClipOffset("Clipping Offset", Range(0.0, 1.0)) = 0.25

		[HideInInspector][Enum(Layer1,0,Layer2,1)] _Layer("Layer", Float) = 0

		[HideInInspector]_SrcBlend("Float", Integer) = 1
        [HideInInspector]_DstBlend("Float", Integer) = 0


        [HideInInspector]_RenderQueueType("Float", Float) = 4
        [HideInInspector][ToggleUI]_AddPrecomputedVelocity("Boolean", Float) = 0
        [HideInInspector][ToggleUI]_DepthOffsetEnable("Boolean", Float) = 0
        [HideInInspector][ToggleUI]_ConservativeDepthOffsetEnable("Boolean", Float) = 0
        [HideInInspector][ToggleUI]_TransparentWritingMotionVec("Boolean", Float) = 0
        [HideInInspector][ToggleUI]_AlphaCutoffEnable("Boolean", Float) = 0
        [HideInInspector]_TransparentSortPriority("_TransparentSortPriority", Float) = 0
        [HideInInspector][ToggleUI]_UseShadowThreshold("Boolean", Float) = 0
        [HideInInspector][ToggleUI]_DoubleSidedEnable("Boolean", Float) = 0
        [HideInInspector][Enum(Flip, 0, Mirror, 1, None, 2)]_DoubleSidedNormalMode("Float", Float) = 2
        [HideInInspector]_DoubleSidedConstants("Vector4", Vector) = (1, 1, -1, 0)
        [HideInInspector][Enum(Auto, 0, On, 1, Off, 2)]_DoubleSidedGIMode("Float", Float) = 0
        [HideInInspector][ToggleUI]_TransparentDepthPrepassEnable("Boolean", Float) = 0
        [HideInInspector][ToggleUI]_TransparentDepthPostpassEnable("Boolean", Float) = 0
        [HideInInspector][ToggleUI]_PerPixelSorting("Boolean", Float) = 0
        [HideInInspector]_SurfaceType("Float", Float) = 1

        [HideInInspector]_DstBlend2("Float", Float) = 0
        [HideInInspector]_AlphaSrcBlend("Float", Float) = 1
        [HideInInspector]_AlphaDstBlend("Float", Float) = 0
        [HideInInspector][ToggleUI]_ZWrite("Boolean", Float) = 0
        [HideInInspector][ToggleUI]_TransparentZWrite("Boolean", Float) = 0
        [HideInInspector]_CullMode("Float", Float) = 2
        [HideInInspector][ToggleUI]_EnableFogOnTransparent("Boolean", Float) = 1
        [HideInInspector]_CullModeForward("Float", Float) = 2
        [HideInInspector][Enum(Front, 1, Back, 2)]_TransparentCullMode("Float", Float) = 2
        [HideInInspector][Enum(Front, 1, Back, 2)]_OpaqueCullMode("Float", Float) = 2
        [HideInInspector]_ZTestDepthEqualForOpaque("Float", Int) = 4
        [HideInInspector][Enum(UnityEngine.Rendering.CompareFunction)]_ZTestTransparent("Float", Float) = 4
        [HideInInspector][ToggleUI]_TransparentBackfaceEnable("Boolean", Float) = 0
        [HideInInspector][ToggleUI]_ReceivesSSR("Boolean", Float) = 1
        [HideInInspector][ToggleUI]_ReceivesSSRTransparent("Boolean", Float) = 1
        [HideInInspector][ToggleUI]_EnableBlendModePreserveSpecularLighting("Boolean", Float) = 1
        [HideInInspector][ToggleUI]_SupportDecals("Boolean", Float) = 1
        [HideInInspector][ToggleUI]_ExcludeFromTUAndAA("Boolean", Float) = 0
        [HideInInspector]_StencilRef("Float", Int) = 0
        [HideInInspector]_StencilWriteMask("Float", Int) = 6
        [HideInInspector]_StencilRefDepth("Float", Int) = 8
        [HideInInspector]_StencilWriteMaskDepth("Float", Int) = 9
        [HideInInspector]_StencilRefMV("Float", Int) = 40
        [HideInInspector]_StencilWriteMaskMV("Float", Int) = 41
        [HideInInspector]_StencilRefDistortionVec("Float", Int) = 4
        [HideInInspector]_StencilWriteMaskDistortionVec("Float", Int) = 4
        [HideInInspector]_StencilWriteMaskGBuffer("Float", Int) = 15
        [HideInInspector]_StencilRefGBuffer("Float", Int) = 10
        [HideInInspector]_ZTestGBuffer("Float", Int) = 4
        [HideInInspector][ToggleUI]_RayTracing("Boolean", Float) = 0
        [HideInInspector][Enum(SpecularColor, 4)]_MaterialID("_MaterialID", Float) = 4
        [HideInInspector]_MaterialTypeMask("_MaterialTypeMask", Float) = 16
        [HideInInspector][ToggleUI]_TransmissionEnable("Boolean", Float) = 1
		[HideInInspector] _RenderingLayerMask("Rendering Layer Mask", Float) = 0
    }
    SubShader
    {
		PackageRequirements
		{
			"com.unity.render-pipelines.high-definition"
		}
        Tags
        {
            "RenderPipeline"="HDRenderPipeline"
            "RenderType"="HDLitShader"
            "Queue"="Transparent+1"
            "DisableBatching"="False"
            "ShaderGraphShader"="true"
            "ShaderGraphTargetId"="HDLitSubTarget"
        }
        
        Pass
        {
            Name "Forward"
            Tags
            {
                "LightMode" = "Forward"
            }
        
            // Render State
            Cull [_CullModeForward]
			Blend [_SrcBlend] [_DstBlend], [_AlphaSrcBlend] [_AlphaDstBlend]
			Blend 1 One OneMinusSrcAlpha
			Blend 2 One [_DstBlend2]
			Blend 3 One [_DstBlend2]
			Blend 4 One OneMinusSrcAlpha
			ZTest [_ZTestDepthEqualForOpaque]
			ZWrite [_ZWrite]
			ColorMask [_ColorMaskTransparentVelOne] 1
			ColorMask [_ColorMaskTransparentVelTwo] 2
			Stencil
			{
				WriteMask [_StencilWriteMask]
				Ref [_StencilRef]
				CompFront Always
				PassFront Replace
				CompBack Always
				PassBack Replace
			}
        
            // --------------------------------------------------
            // Pass
        
            HLSLPROGRAM
        
            // Pragmas
            #pragma multi_compile _ DOTS_INSTANCING_ON
			#pragma instancing_options renderinglayer
			#pragma target 4.5
			#pragma vertex Vert
			#pragma fragment Frag
			#pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch
			#pragma multi_compile_instancing
        
            // Keywords
            #pragma shader_feature _ _SURFACE_TYPE_TRANSPARENT
			#pragma shader_feature_local _ _DOUBLESIDED_ON
			#pragma shader_feature_local _ _ADD_PRECOMPUTED_VELOCITY
			#pragma shader_feature_local _ _TRANSPARENT_WRITES_MOTION_VEC _TRANSPARENT_REFRACTIVE_SORT
			#pragma shader_feature_local_fragment _ _ENABLE_FOG_ON_TRANSPARENT
			#pragma multi_compile _ DEBUG_DISPLAY
			#pragma shader_feature_local_fragment _ _DISABLE_DECALS
			#pragma shader_feature_local_raytracing _ _DISABLE_DECALS
			#pragma shader_feature_local_fragment _ _DISABLE_SSR
			#pragma shader_feature_local_raytracing _ _DISABLE_SSR
			#pragma shader_feature_local_fragment _ _DISABLE_SSR_TRANSPARENT
			#pragma shader_feature_local_raytracing _ _DISABLE_SSR_TRANSPARENT
			#pragma multi_compile_fragment _ PROBE_VOLUMES_L1 PROBE_VOLUMES_L2
			#pragma multi_compile_raytracing _ PROBE_VOLUMES_L1 PROBE_VOLUMES_L2
			#pragma multi_compile_fragment DECALS_OFF DECALS_3RT DECALS_4RT
			#pragma multi_compile_fragment _ DECAL_SURFACE_GRADIENT
			#pragma multi_compile_fragment PUNCTUAL_SHADOW_LOW PUNCTUAL_SHADOW_MEDIUM PUNCTUAL_SHADOW_HIGH
			#pragma multi_compile_fragment DIRECTIONAL_SHADOW_LOW DIRECTIONAL_SHADOW_MEDIUM DIRECTIONAL_SHADOW_HIGH
			#pragma multi_compile_fragment AREA_SHADOW_MEDIUM AREA_SHADOW_HIGH
			#pragma multi_compile_fragment SCREEN_SPACE_SHADOWS_OFF SCREEN_SPACE_SHADOWS_ON
			#pragma multi_compile_fragment USE_FPTL_LIGHTLIST USE_CLUSTERED_LIGHTLIST

			#pragma shader_feature_local_fragment _MATERIAL_FEATURE_ANISOTROPY
			#pragma shader_feature_local_raytracing _MATERIAL_FEATURE_ANISOTROPY
			#pragma shader_feature_local_fragment _MATERIAL_FEATURE_SPECULAR_COLOR
			#pragma shader_feature_local_raytracing _MATERIAL_FEATURE_SPECULAR_COLOR

			#pragma multi_compile_vertex _ _FLUIDFRENZY_INSTANCING

			#pragma multi_compile_local_vertex _ _FLUID_UNITY_TERRAIN
			#pragma multi_compile_local _ _FLUID_FLOWMAPPING_STATIC _FLUID_FLOWMAPPING_DYNAMIC
			#pragma multi_compile_local_fragment _ _FOAMMASK_ON

			#pragma shader_feature_local_fragment _SCREENSPACE_REFRACTION_ALPHA _SCREENSPACE_REFRACTION_ON _SCREENSPACE_REFRACTION_OPAQUE _SCREENSPACE_REFRACTION_ABSORB
			#pragma shader_feature_local_fragment _ _PLANAR_REFLECTION_ON
			#pragma shader_feature_local_vertex _ _DISPLACEMENTWAVES_ON

			#pragma shader_feature_local _ _NORMALMAP
			#pragma shader_feature_local _ _FOAMMAP _FOAM_NORMALMAP
			#pragma shader_feature_local_fragment _FOAMMODE_ALBEDO _FOAMMODE_CLIP _FOAMMODE_MASK
			#pragma shader_feature_local_fragment _ _SCREENSPACE_FOAMMASK_ON

            // --------------------------------------------------
            // Main
        
            #define RENDERPIPELINE_HDRP
			#include_with_pragmas "Packages/com.frenzybyte.fluidfrenzy/Runtime/Rendering/Shaders/Water/HDRP/WaterForwardHDRP.hlsl"
        
            ENDHLSL
        }

        Pass
        {
            Name "TransparentDepthPrepass"
            Tags
            {
                "LightMode" = "TransparentDepthPrepass"
            }
        
            // Render State
            Cull [_CullMode]
			Blend One Zero
			ZWrite On
			Stencil
			{
				WriteMask [_StencilWriteMaskDepth]
				Ref [_StencilRefDepth]
				CompFront Always
				PassFront Replace
				CompBack Always
				PassBack Replace
			}
        
            // Debug
            // <None>
        
            // --------------------------------------------------
            // Pass
        
            HLSLPROGRAM
        
            // Pragmas
            #pragma multi_compile _ DOTS_INSTANCING_ON
			#pragma instancing_options renderinglayer
			#pragma target 4.5
			#pragma vertex Vert
			#pragma fragment Frag
			#pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch
			#pragma multi_compile_instancing
        
			// Keywords
			#pragma shader_feature _ _SURFACE_TYPE_TRANSPARENT
			#pragma shader_feature_local _ _DOUBLESIDED_ON
			#pragma shader_feature_local _ _TRANSPARENT_WRITES_MOTION_VEC _TRANSPARENT_REFRACTIVE_SORT
			#pragma shader_feature_local_fragment _ _DISABLE_DECALS
			#pragma shader_feature_local_raytracing _ _DISABLE_DECALS

			#pragma multi_compile_vertex _ _FLUIDFRENZY_INSTANCING

			#pragma multi_compile_local_vertex _ _FLUID_UNITY_TERRAIN
			#pragma multi_compile_local _ _FLUID_FLOWMAPPING_STATIC _FLUID_FLOWMAPPING_DYNAMIC
			#pragma multi_compile_local_fragment _ _FOAMMASK_ON

			#pragma shader_feature_local_fragment _SCREENSPACE_REFRACTION_ALPHA _SCREENSPACE_REFRACTION_ON _SCREENSPACE_REFRACTION_OPAQUE _SCREENSPACE_REFRACTION_ABSORB
			#pragma shader_feature_local_vertex _ _DISPLACEMENTWAVES_ON

			#pragma shader_feature_local _ _NORMALMAP
			#pragma shader_feature_local _ _FOAMMAP _FOAM_NORMALMAP
			#pragma shader_feature_local_fragment _FOAMMODE_ALBEDO _FOAMMODE_CLIP _FOAMMODE_MASK
			#pragma shader_feature_local_fragment _ _SCREENSPACE_FOAMMASK_ON
        
            // --------------------------------------------------
            // Main
			#define RENDERPIPELINE_HDRP
            #include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Rendering/Shaders/Water/HDRP/WaterDepthOnlyHDRP.hlsl"
        
            ENDHLSL
        }
    }

	SubShader
	{
		PackageRequirements
		{
			"com.unity.render-pipelines.universal"
		}

		// Universal Pipeline tag is required. If Universal render pipeline is not set in the graphics settings
		// this Subshader will fail. One can add a subshader below or fallback to Standard built-in to make this
		// material work with both Universal Render Pipeline and Builtin Unity Pipeline
		Tags{"RenderType" = "Transparent" "Queue" = "Geometry+511" "RenderPipeline" = "UniversalPipeline" "UniversalMaterialType" = "Lit" "IgnoreProjector" = "True" "ShaderModel" = "3.0"}
		LOD 100

		// ------------------------------------------------------------------
		//  Forward pass. Shades all light in a single pass. GI + emission + Fog
		Pass
		{
			// Lightmode matches the ShaderPassName set in UniversalRenderPipeline.cs. SRPDefaultUnlit and passes with
			// no LightMode tag are also rendered by Universal Render Pipeline
			Name "ForwardLit"
			Tags{"LightMode" = "UniversalForward"}

			Blend [_SrcBlend] [_DstBlend]
			ZWrite On
			Cull Off

			HLSLPROGRAM
			//#pragma exclude_renderers gles gles3 glcore
			#pragma target 3.0


			#pragma multi_compile_local_vertex _ _FLUID_UNITY_TERRAIN
			#pragma multi_compile_local _ _FLUID_FLOWMAPPING_STATIC _FLUID_FLOWMAPPING_DYNAMIC
			#pragma multi_compile_local_fragment _ _FOAMMASK_ON

			#pragma shader_feature_local_fragment _SCREENSPACE_REFRACTION_ALPHA _SCREENSPACE_REFRACTION_ON _SCREENSPACE_REFRACTION_OPAQUE _SCREENSPACE_REFRACTION_ABSORB
			#pragma shader_feature_local_fragment _ _PLANAR_REFLECTION_ON
			#pragma shader_feature_local_vertex _ _DISPLACEMENTWAVES_ON

			#pragma shader_feature_local _ _NORMALMAP
			#pragma shader_feature_local _ _FOAMMAP _FOAM_NORMALMAP
			#pragma shader_feature_local_fragment _FOAMMODE_ALBEDO _FOAMMODE_CLIP _FOAMMODE_MASK
			#pragma shader_feature_local_fragment _ _SCREENSPACE_FOAMMASK_ON

			// -------------------------------------
			// Universal Pipeline keywords
			#pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
			#pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
			#pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
			#pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
			#pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
			#pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
			#pragma multi_compile_fragment _ _LIGHT_LAYERS
			#pragma multi_compile_fragment _ _LIGHT_COOKIES
			#if UNITY_VERSION < 60010000
				#pragma multi_compile _ _FORWARD_PLUS
			#else
				#pragma multi_compile _ _CLUSTER_LIGHT_LOOP
			#endif
			#pragma multi_compile _ _CLUSTERED_RENDERING

			// -------------------------------------
			// Unity defined keywords
			#pragma multi_compile_fog

			//--------------------------------------
			// GPU Instancing
			#pragma multi_compile_instancing
			#pragma instancing_options renderinglayer
			#pragma multi_compile_vertex _ _FLUIDFRENZY_INSTANCING

#define CURVEDWORLD_BEND_ID_1

			#include_with_pragmas "Packages/com.frenzybyte.fluidfrenzy/Runtime/Rendering/Shaders/Water/URP/WaterForwardURP.hlsl"

			#if defined(FLUIDFRENZY_EXTERNALCOMPATIBILITY_CURVEDWORLD)
#pragma shader_feature_local CURVEDWORLD_BEND_TYPE_CLASSICRUNNER_X_POSITIVE
				#pragma shader_feature_local CURVEDWORLD_DISABLED_ON
				#pragma shader_feature_local CURVEDWORLD_NORMAL_TRANSFORMATION_ON
			#endif

			#pragma vertex LitPassVertex
			#pragma fragment LitPassFragment

			ENDHLSL
		}
	}

    SubShader
    {
		Tags{ "RenderType" = "Opaque" "Queue" = "Geometry+511" }
		GrabPass
		{
			"_BackgroundTexture"
		}

        LOD 100

		Pass
        {
			Name "ForwardBase"
			Tags{ "LightMode" = "ForwardBase" }
			Blend [_SrcBlend] [_DstBlend]
			Cull Off
            CGPROGRAM


			#pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog
			#pragma multi_compile_instancing
			#pragma multi_compile_vertex _ _FLUIDFRENZY_INSTANCING

			#pragma multi_compile_local_vertex _ _FLUID_UNITY_TERRAIN
			#pragma multi_compile_fragment _ _TRANSPARENT_RECEIVE_SHADOWS_CLOSE_FIT _TRANSPARENT_RECEIVE_SHADOWS_SPLIT_SPHERES
			#pragma multi_compile_local _ _FLUID_FLOWMAPPING_STATIC _FLUID_FLOWMAPPING_DYNAMIC
			#pragma multi_compile_local_fragment _ _FOAMMASK_ON

			#pragma shader_feature_local_fragment _SHADOWMAP_OFF _SHADOWMAP_HARD _SHADOWMAP_SOFT
			#pragma shader_feature_local_fragment _SCREENSPACE_REFRACTION_ALPHA _SCREENSPACE_REFRACTION_ON _SCREENSPACE_REFRACTION_OPAQUE _SCREENSPACE_REFRACTION_ABSORB
			#pragma shader_feature_local_fragment _ _PLANAR_REFLECTION_ON
			#pragma shader_feature_local_vertex _ _DISPLACEMENTWAVES_ON

			#pragma shader_feature_local _ _NORMALMAP
			#pragma shader_feature_local _ _FOAMMAP _FOAM_NORMALMAP
			#pragma shader_feature_local_fragment _FOAMMODE_ALBEDO _FOAMMODE_CLIP _FOAMMODE_MASK
			#pragma shader_feature_local_fragment _ _SCREENSPACE_FOAMMASK_ON

#define CURVEDWORLD_BEND_ID_1

			#include_with_pragmas "Packages/com.frenzybyte.fluidfrenzy/Runtime/Rendering/Shaders/Water/Builtin/WaterForwardBRP.hlsl"

			
			#if defined(FLUIDFRENZY_EXTERNALCOMPATIBILITY_CURVEDWORLD)
#pragma shader_feature_local CURVEDWORLD_BEND_TYPE_CLASSICRUNNER_X_POSITIVE
				#pragma shader_feature_local CURVEDWORLD_DISABLED_ON
				#pragma shader_feature_local CURVEDWORLD_NORMAL_TRANSFORMATION_ON
			#endif

			ENDCG
		}
    }
	CustomEditor "FluidFrenzy.Editor.FluidWaterShaderGUI"
}
