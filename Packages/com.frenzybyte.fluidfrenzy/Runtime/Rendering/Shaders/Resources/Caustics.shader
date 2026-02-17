Shader "Hidden/FluidFrenzy/Caustics"
{
	Properties
	{
		_CausticsTexture ("Caustics Texture", 2D) = "black" {}
		_CausticsTiling ("Tiling", Float) = 0.5
		_CausticsIntensity ("Intensity", Float) = 1.0
		_CausticsChromaOffset ("Chromatic Aberration", Float) = 0.005
		_CausticsDepthFade ("Depth Fade", Float) = 0.2
	}

	// =========================================================================
	// HDRP SubShader
	// =========================================================================
	SubShader
	{
		PackageRequirements { "com.unity.render-pipelines.high-definition" }
		Tags{"RenderPipeline" = "HDRenderPipeline" "RenderType" = "Transparent"}

		Pass 
		{
			Name "FluidCaustics"
			ZTest Always ZWrite Off Cull Off
			Blend DstColor SrcColor
            
			HLSLPROGRAM
			#pragma target 4.5
			#pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch
			#pragma vertex vertSimple
			#pragma fragment fragCaustics
			#pragma multi_compile_local_fragment _ _CAUSTICS_DISPERSION
			#pragma multi_compile_local_fragment _ _CAUSTICS_TEXTURE_OFF
			#pragma multi_compile_local_fragment _ _CAUSTICS_TRIPLANAR
			#pragma multi_compile_local_fragment _ _FOAMMASK_ON
			#pragma multi_compile_local_fragment _ _FLUID_FLOWMAPPING_STATIC _FLUID_FLOWMAPPING_DYNAMIC
            
			#define FLUID_PIPELINE_HDRP
			#include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Rendering/Shaders/Library/CausticsEffectPasses.hlsl"
			ENDHLSL
		}
	}

	// =========================================================================
	// URP SubShader
	// =========================================================================
	SubShader
	{
		PackageRequirements { "com.unity.render-pipelines.universal" }
		Tags{"RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True"}

		Pass 
		{ 
			Name "FluidCaustics" 
			ZTest Always ZWrite Off Cull Off 
			Blend DstColor SrcColor
            
			HLSLPROGRAM 
			#pragma vertex vertSimple 
			#pragma fragment fragCaustics 
			#pragma multi_compile_local_fragment _ _CAUSTICS_DISPERSION
			#pragma multi_compile_local_fragment _ _CAUSTICS_TEXTURE_OFF
			#pragma multi_compile_local_fragment _ _CAUSTICS_TRIPLANAR
			#pragma multi_compile_local_fragment _ _FOAMMASK_ON
			#pragma multi_compile_local_fragment _ _FLUID_FLOWMAPPING_STATIC _FLUID_FLOWMAPPING_DYNAMIC
            
			#define FLUID_PIPELINE_URP 
			#include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Rendering/Shaders/Library/CausticsEffectPasses.hlsl" 
			ENDHLSL 
		}
	}

	// =========================================================================
	// BiRP SubShader
	// =========================================================================
	SubShader
	{
		Tags { "RenderType"="Transparent" }

		Pass 
		{ 
			Name "FluidCaustics" 
			ZTest Always ZWrite Off Cull Off 
			Blend DstColor SrcColor
            
			HLSLPROGRAM 
			#pragma vertex vertSimple 
			#pragma fragment fragCaustics 
			#pragma multi_compile_local_fragment _ _CAUSTICS_DISPERSION
			#pragma multi_compile_local_fragment _ _CAUSTICS_TEXTURE_OFF
			#pragma multi_compile_local_fragment _ _CAUSTICS_TRIPLANAR
			#pragma multi_compile_local_fragment _ _FOAMMASK_ON
			#pragma multi_compile_local_fragment _ _FLUID_FLOWMAPPING_STATIC _FLUID_FLOWMAPPING_DYNAMIC
			#pragma multi_compile_fragment _ _TRANSPARENT_RECEIVE_SHADOWS_CLOSE_FIT _TRANSPARENT_RECEIVE_SHADOWS_SPLIT_SPHERES
            
			#pragma multi_compile _ SHADOWS_SCREEN
            
			#include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Rendering/Shaders/Library/CausticsEffectPasses.hlsl" 
			ENDHLSL 
		}
	}
}