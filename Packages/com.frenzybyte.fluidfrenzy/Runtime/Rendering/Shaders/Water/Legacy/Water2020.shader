Shader "FluidFrenzy/Legacy/2020/Water"
{
    Properties
    {
		//[Header(Lighting)][Space(5)]
		_SpecularIntensity("Specular Intensity", Range(0,10)) = 1
		[KeywordEnum(Off,Hard,Soft)] _ShadowMap("Shadows",Int) = 0
		[Toggle(_PLANAR_REFLECTION_ON)] _PlanarReflections("Planar Reflections", Int) = 0
		_ReflectionDistortion("Distortion", Range(0,0.2)) = 0.1
		_ReflectivityMin("Base Reflectivity", Range(0,1)) = 0.0
		_ScreenSpaceRefraction("Refraction", Int) = 1
		_WaterColor("Water Color", Color) = (0,0.2356132,0.254717,1)
		_AbsorptionDepthScale("Absorption Depth Scale", Range(0,1)) = 0.5
		_RefractionDistortion("Distortion", Range(0,0.2)) = 0.05

		[Header(Subsurface Scattering)][Space(5)]
		_ScatterColor("Scatter Color", Color) = (0.3490196,0.6313726,0.5843138,1)
		_ScatterIntensity("Scatter Intensity", Range(0,5)) = 1
		_ScatterLightIntensity("Scatter Light Angle Contribution", Range(0,10)) = 10
		_ScatterViewIntensity("Scatter View Angle Contribution", Range(0,1)) = 0.1
		_ScatterFoamIntensity("Scatter Foam Contribution", Range(0,1)) = 0.5
		_ScatterAmbient("Scatter Ambient", Range(0,1)) = 0.1

		//[Header(Waves)][Space(5)]
		[Toggle(_DISPLACEMENTWAVES_ON)] _DisplacementWaves("Wave Displacement", Int) = 0
		_DisplacementWaveAmplitude("Wave Displacement Amplitude", Range(0,1)) = 0.25
		_DisplacementPhase("Displacement Phase Speed", Range(0,1)) = 1
		_DisplacementWaveSpeed("Displacement Wave Speed", Range(0,1)) = 0.1
		_DisplacementWaveLength("Displacement Wave Length", Range(0.01,10)) = 2
		_DisplacementSteepness("Displacement Wave Steepness", Range(0,1)) = 0
		_DisplacementScale("Displacement Scale", Range(0,500)) = 200
		_WaveNormals("Wave Normals", 2D) = "bump" {}
		_WaveNormalStrength("Wave Normal Strength", Float) = 0.5

		[Header(Foam)][Space(5)]
		_FoamColor("Foam Color", Color) = (0.85,0.85,0.85,1)
		_FoamVisibility("Foam Map Visbility(Strength, Power)", Vector) = (0,1,0,0)
		_FoamTexture("Foam Mask", 2D) = "black" {}
		_FoamNormalMap("Foam Normal", 2D) = "bump" {}
		_FoamNormalStrength("Foam Normal Strength", Float) = 2

		[Toggle(_SCREENSPACE_FOAMMASK_ON)] _FoamScreenSpace("Foam Screenspace",Float) = 0
		[KeywordEnum(Albedo, Clip ,Mask)] _FoamMode("Foam Mode",Float) = 0

		_FadeHeight("Fade Height", Range(0.00001, 1)) = 0.5
		_LinearClipOffset("Clipping Offset", Range(0.0, 10.0)) = 0
		_ExponentialClipOffset("Clipping Offset", Range(0.0, 1.0)) = 0.25

		[Enum(Layer1,0,Layer2,1)] _Layer("Layer", Float) = 0

		_SrcBlend("__src", Int) = 1
		_DstBlend("__dst", Int) = 0
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
		Tags{"RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "UniversalMaterialType" = "Lit" "IgnoreProjector" = "True" "ShaderModel" = "3.0"}
		LOD 100

		// ------------------------------------------------------------------
		//  Forward pass. Shades all light in a single pass. GI + emission + Fog
		Pass
		{
			// Lightmode matches the ShaderPassName set in UniversalRenderPipeline.cs. SRPDefaultUnlit and passes with
			// no LightMode tag are also rendered by Universal Render Pipeline
			Name "ForwardLit"
			Tags{"LightMode" = "UniversalForward"}

			Blend One Zero
			ZWrite On
			Cull Back

			HLSLPROGRAM
			#pragma exclude_renderers gles gles3 glcore
			#pragma target 4.5

			#pragma multi_compile_local _ _FLUID_UNITY_TERRAIN
			#pragma multi_compile _ _TRANSPARENT_RECEIVE_SHADOWS_CLOSE_FIT _TRANSPARENT_RECEIVE_SHADOWS_SPLIT_SPHERES
			#pragma multi_compile_local _ _FLUID_FLOWMAPPING_STATIC _FLUID_FLOWMAPPING_DYNAMIC
			#pragma multi_compile_local _ _FOAMMASK_ON

			#pragma shader_feature_local _SHADOWMAP_OFF _SHADOWMAP_HARD _SHADOWMAP_SOFT
			#pragma shader_feature_local _SCREENSPACE_REFRACTION_ALPHA _SCREENSPACE_REFRACTION_ON _SCREENSPACE_REFRACTION_OPAQUE
			#pragma shader_feature_local _ _PLANAR_REFLECTION_ON
			#pragma shader_feature_local _ _DISPLACEMENTWAVES_ON

			#pragma shader_feature_local _ _NORMALMAP
			#pragma shader_feature_local _ _FOAMMAP _FOAM_NORMALMAP
			#pragma shader_feature_local _FOAMMODE_ALBEDO _FOAMMODE_CLIP _FOAMMODE_MASK
			#pragma shader_feature_local _ _SCREENSPACE_FOAMMASK_ON

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


			#include_with_pragmas "Packages/com.frenzybyte.fluidfrenzy/Runtime/Rendering/Shaders/Water/URP/WaterForwardURP.hlsl"

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
			Tags{ "LightMode" = "ForwardBase" }
			Blend [_SrcBlend] [_DstBlend]

            CGPROGRAM

			#pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog
			#pragma multi_compile_instancing
			#pragma multi_compile _ _FLUIDFRENZY_INSTANCING
			#pragma multi_compile_local _ _FLUID_UNITY_TERRAIN
			#pragma multi_compile _ _TRANSPARENT_RECEIVE_SHADOWS_CLOSE_FIT _TRANSPARENT_RECEIVE_SHADOWS_SPLIT_SPHERES
			#pragma multi_compile_local _ _FLUID_FLOWMAPPING_STATIC _FLUID_FLOWMAPPING_DYNAMIC
			#pragma multi_compile_local _ _FOAMMASK_ON

			#pragma shader_feature_local _SHADOWMAP_OFF _SHADOWMAP_HARD _SHADOWMAP_SOFT
			#pragma shader_feature_local _SCREENSPACE_REFRACTION_ALPHA _SCREENSPACE_REFRACTION_ON _SCREENSPACE_REFRACTION_OPAQUE
			#pragma shader_feature_local _ _PLANAR_REFLECTION_ON
			#pragma shader_feature_local _ _DISPLACEMENTWAVES_ON

			#pragma shader_feature_local _ _NORMALMAP
			#pragma shader_feature_local _ _FOAMMAP 
			#pragma shader_feature_local _ _FOAM_NORMALMAP 
			#pragma shader_feature_local _FOAMMODE_ALBEDO _FOAMMODE_CLIP _FOAMMODE_MASK
			#pragma shader_feature_local _ _SCREENSPACE_FOAMMASK_ON

			#include_with_pragmas "Packages/com.frenzybyte.fluidfrenzy/Runtime/Rendering/Shaders/Water/Builtin/WaterForwardBRP.hlsl"

			ENDCG
		}
    }
	CustomEditor "FluidFrenzy.Editor.FluidWaterShaderGUI"
}
