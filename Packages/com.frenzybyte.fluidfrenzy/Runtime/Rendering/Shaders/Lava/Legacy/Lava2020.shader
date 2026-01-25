Shader "FluidFrenzy/Legacy/2020/Lava"
{
    Properties
    {
		_LightIntensity("Light Intensity", Range(0,2)) = 1
		[KeywordEnum(Off,Hard,Soft)] _ShadowMap("Shadows",Int) = 0

		_HeatLUT("Heat LUT", 2D) = "black" {}
		_LUTScale("Heat scale", Range(0.0, 5.0)) = 3.0

		_MainTex("Albedo", 2D) = "white"{}
		_GlossMapScale("Smoothness Scale", Range(0.0, 1.0)) = 1.0
		_Color("Albedo Color", Color) = (0.25, 0.25, 0.25, 1.0)

		_BumpScale("Scale", Float) = 1.0
		[Normal] _BumpMap("Normal Map", 2D) = "bump" {}

		_EmissionMap("Emission Map", 2D) = "white" {}
		_Emission("Emission", Range(0.0, 5.0)) = 2.0

		_Noise("Noise", 2D) = "white" {}

		_FadeHeight("Fade Height", Range(0.00001, 0.1)) = 0.001
		_LinearClipOffset("Clipping Offset", Range(0.0, 10.0)) = 10
		_ExponentialClipOffset("Clipping Offset", Range(0.0, 1.0)) = 0

		[Enum(Layer1,0,Layer2,1)] _Layer("Layer", Float) = 0
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
		Tags{"RenderType" = "Opaque" "Queue" = "Geometry+510" "RenderPipeline" = "UniversalPipeline" "UniversalMaterialType" = "Lit" "IgnoreProjector" = "True" "ShaderModel" = "4.5"}
		LOD 300

		// ------------------------------------------------------------------
		//  Forward pass. Shades all light in a single pass. GI + emission + Fog
		Pass
		{
			// Lightmode matches the ShaderPassName set in UniversalRenderPipeline.cs. SRPDefaultUnlit and passes with
			// no LightMode tag are also rendered by Universal Render Pipeline
			Name "ForwardLit"
			Tags{"LightMode" = "UniversalForward"}

			Blend SrcAlpha OneMinusSrcAlpha, Zero One
			ZWrite On
			Cull Back

			HLSLPROGRAM
			#pragma exclude_renderers gles gles3 glcore
			#pragma target 4.5

			#pragma multi_compile_local _ _FLUID_UNITY_TERRAIN
			#pragma multi_compile _ _TRANSPARENT_RECEIVE_SHADOWS_CLOSE_FIT _TRANSPARENT_RECEIVE_SHADOWS_SPLIT_SPHERES
			#pragma multi_compile_local _ _FLUID_FLOWMAPPING_STATIC _FLUID_FLOWMAPPING_DYNAMIC
			#pragma shader_feature_local _ _NORMALMAP

			// -------------------------------------
			// Universal Pipeline keywords
			#pragma multi_compile _ _MAIN_LIGHT_SHADOWS 
			#pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
			#pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
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
			#pragma multi_compile _ _FLUIDFRENZY_INSTANCING

			#pragma vertex LitPassVertex
			#pragma fragment LitPassFragment

			#include_with_pragmas "Packages/com.frenzybyte.fluidfrenzy/Runtime/Rendering/Shaders/Lava/URP/LavaLitForwardURP.hlsl"

			ENDHLSL
		}
	}

    SubShader
    {
		Tags{ "RenderType" = "Opaque" "Queue" = "Geometry+510" }
        LOD 100
		Blend SrcAlpha OneMinusSrcAlpha, Zero One

		CGINCLUDE

		#include_with_pragmas "Packages/com.frenzybyte.fluidfrenzy/Runtime/Rendering/Shaders/Lava/Builtin/LavaForwardBRP.hlsl"

		ENDCG

		Pass
        {
			Tags{ "LightMode" = "ForwardBase" }

            CGPROGRAM

			#pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag

			// make sh work
			#pragma multi_compile_fwdbase
			#pragma skip_variants DIRECTIONAL LIGHTMAP_ON DIRLIGHTMAP_COMBINED DYNAMICLIGHTMAP_ON SHADOWS_SHADOWMASK LIGHTMAP_SHADOW_MIXING
            // make fog work
            #pragma multi_compile_fog
			#pragma multi_compile_instancing
			#pragma multi_compile _ _FLUIDFRENZY_INSTANCING
			#pragma multi_compile _ _TRANSPARENT_RECEIVE_SHADOWS_CLOSE_FIT _TRANSPARENT_RECEIVE_SHADOWS_SPLIT_SPHERES
			#pragma multi_compile_local _ _FLUID_FLOWMAPPING_STATIC _FLUID_FLOWMAPPING_DYNAMIC
			#pragma multi_compile_local _ _FLUID_UNITY_TERRAIN

			#pragma shader_feature_local _ _NORMALMAP
			#pragma shader_feature_local _SHADOWMAP_OFF _SHADOWMAP_HARD _SHADOWMAP_SOFT

			ENDCG
		}
    }
	CustomEditor "FluidFrenzy.Editor.FluidLavaShaderGUI"
}
