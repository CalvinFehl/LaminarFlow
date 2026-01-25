Shader "FluidFrenzy/TerraformTerrain(Single Layer)"
{
    Properties
    {
        [CurvedWorldBendSettings] _CurvedWorldBendSettings("0|1|1", Vector) = (0, 0, 0, 0)

		_Layer0Color("Layer 0 Color", Color) = (1,1,1,1)
		_Layer0Albedo("Layer 0 Albedo", 2D) = "white"{}
		_Layer0MaskMap("Layer 0 Mask Map", 2D) = "white"{}
		[Normal] _Layer0BumpMap("Layer 0 Normal Map", 2D) = "bump" {}
		_Layer0BumpScale("Layer 0 Scale", Float) = 1.0

		_Layer1Color("Layer 1 Color", Color) = (1,1,1,1)
		_Layer1Albedo("Layer 1 Albedo", 2D) = "white"{}
		_Layer1MaskMap("Layer 1 Mask Map", 2D) = "white"{}
		[Normal] _Layer1BumpMap("Layer 1 Normal Map", 2D) = "bump" {}
		_Layer1BumpScale("Layer 1 Scale", Float) = 1.0

		_Layer2Color("Layer 2 Color", Color) = (1,1,1,1)
		_Layer2Albedo("Layer 2 Albedo", 2D) = "white"{}
		_Layer2MaskMap("Layer 2 Mask Map", 2D) = "white"{}
		[Normal] _Layer2BumpMap("Layer 2 Normal Map", 2D) = "bump" {}
		_Layer2BumpScale("Layer 2 Scale", Float) = 1.0

		_Layer3Color("Layer 3 Color", Color) = (1,1,1,1)
		_Layer3Albedo("Layer 3 Albedo", 2D) = "white"{}
		_Layer3MaskMap("Layer 3 Mask Map", 2D) = "white"{}
		[Normal] _Layer3BumpMap("Layer 2 Normal Map", 2D) = "bump" {}
		_Layer3BumpScale("Layer 3 Scale", Float) = 1.0
		
		[Header(Top layer)]
		_TopLayerAlbedo("Top Layer Albedo", 2D) = "white"{}
		_TopLayerMaskMap("Top Layer Mask Map", 2D) = "white"{}
		[Normal] _TopLayerBumpMap("Top Layer Normal Map", 2D) = "bump" {}
		_TopLayerBumpScale("Top Layer Scale", Float) = 1.0

		_Splatmap("Splatmap", 2D) = "white"{}
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
		Tags{"RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "UniversalMaterialType" = "Lit" "IgnoreProjector" = "True" "ShaderModel" = "4.5"}
		LOD 300

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
			Cull Off

			HLSLPROGRAM
			#pragma target 3.0

			// -------------------------------------
			// Universal Pipeline keywords
#if UNITY_VERSION >= 202100
			#pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
#else
			#pragma multi_compile _ _MAIN_LIGHT_SHADOWS
			#pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
#endif
			#pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
			#pragma multi_compile _ EVALUATE_SH_MIXED EVALUATE_SH_VERTEX
			#pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
			#pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
			#pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
			#pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
			#pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
			#pragma multi_compile_fragment _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3
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

			#define CURVEDWORLD_BEND_ID_1

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
			#include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Rendering/Shaders/Library/LOD/FluidInstancingCommon.hlsl"
			#include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Rendering/Shaders/Terrain/TerraformTerrainInput.hlsl"
			#include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Rendering/Shaders/Terrain/TerraformTerrainSurfaceData.hlsl"
			#include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Rendering/Shaders/Terrain/URP/TerraformTerrainForwardLitURP.hlsl"

			#if defined(FLUIDFRENZY_EXTERNALCOMPATIBILITY_CURVEDWORLD)
				#pragma shader_feature_local CURVEDWORLD_BEND_TYPE_CLASSICRUNNER_X_POSITIVE
				#pragma shader_feature_local CURVEDWORLD_DISABLED_ON
				#pragma shader_feature_local CURVEDWORLD_NORMAL_TRANSFORMATION_ON
			#endif

			ENDHLSL
		}

		Pass
		{
			Name "ShadowCaster"
			Tags{"LightMode" = "ShadowCaster"}

			ZWrite On
			ZTest LEqual
			ColorMask 0
			Cull Off

			HLSLPROGRAM
			#pragma target 3.0

			// -------------------------------------
			// Material Keywords
			#pragma shader_feature_local_fragment _ALPHATEST_ON
			#pragma shader_feature_local_fragment _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A

			//--------------------------------------
			// GPU Instancing
			#pragma multi_compile_instancing
			#pragma multi_compile _ _FLUIDFRENZY_INSTANCING

			// -------------------------------------
			// Universal Pipeline keywords

			// This is used during shadow map generation to differentiate between directional and punctual light shadows, as they use different formulas to apply Normal Bias
			#pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

			#pragma vertex ShadowPassVertex
			#pragma fragment ShadowPassFragment

			#define CURVEDWORLD_BEND_ID_1

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
			#include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Rendering/Shaders/Terrain/URP/TerraformTerrainShadowCasterURP.hlsl"

			#if defined(FLUIDFRENZY_EXTERNALCOMPATIBILITY_CURVEDWORLD)
				#pragma shader_feature_local CURVEDWORLD_BEND_TYPE_CLASSICRUNNER_X_POSITIVE
				#pragma shader_feature_local CURVEDWORLD_DISABLED_ON
				#pragma shader_feature_local CURVEDWORLD_NORMAL_TRANSFORMATION_ON
			#endif

			ENDHLSL
		}

		Pass
		{
			// Lightmode matches the ShaderPassName set in UniversalRenderPipeline.cs. SRPDefaultUnlit and passes with
			// no LightMode tag are also rendered by Universal Render Pipeline
			Name "GBuffer"
			Tags{"LightMode" = "UniversalGBuffer"}

			ZWrite On
			ZTest LEqual
			Cull Off

			HLSLPROGRAM
			#pragma target 3.0

			// -------------------------------------
			// Universal Pipeline keywords
			#pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
			//#pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
			//#pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
			#pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
			#pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
			#pragma multi_compile_fragment _ _SHADOWS_SOFT
			#pragma multi_compile_fragment _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3
			#pragma multi_compile_fragment _ _LIGHT_LAYERS
			#pragma multi_compile_fragment _ _RENDER_PASS_ENABLED

#if UNITY_VERSION >= 202210
			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
#endif

			// -------------------------------------
			// Unity defined keywords
			#pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT

			//--------------------------------------
			// GPU Instancing
			#pragma multi_compile_instancing
			#pragma instancing_options renderinglayer
			#pragma multi_compile _ _FLUIDFRENZY_INSTANCING

			#pragma vertex LitGBufferPassVertex
			#pragma fragment LitGBufferPassFragment

			#define CURVEDWORLD_BEND_ID_1

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#if UNITY_VERSION < 60010000
				#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/UnityGBuffer.hlsl"
			#else
				#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/GBufferOutput.hlsl"
			#endif
			#include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Rendering/Shaders/Terrain/URP/TerraformTerrainGBufferURP.hlsl"

			#if defined(FLUIDFRENZY_EXTERNALCOMPATIBILITY_CURVEDWORLD)
				#pragma shader_feature_local CURVEDWORLD_BEND_TYPE_CLASSICRUNNER_X_POSITIVE
				#pragma shader_feature_local CURVEDWORLD_DISABLED_ON
				#pragma shader_feature_local CURVEDWORLD_NORMAL_TRANSFORMATION_ON
			#endif

			ENDHLSL
		}

		Pass
		{
			Name "DepthOnly"
			Tags{"LightMode" = "DepthOnly"}

			ZWrite On
			ColorMask 0
			Cull Off

			HLSLPROGRAM
			#pragma target 2.0

			//--------------------------------------
			// GPU Instancing
			#pragma multi_compile_instancing
			#pragma multi_compile _ _FLUIDFRENZY_INSTANCING

			#pragma vertex DepthOnlyVertex
			#pragma fragment DepthOnlyFragment

			#define CURVEDWORLD_BEND_ID_1

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Rendering/Shaders/Terrain/URP/TerraformTerrainDepthURP.hlsl"

			#if defined(FLUIDFRENZY_EXTERNALCOMPATIBILITY_CURVEDWORLD)
				#pragma shader_feature_local CURVEDWORLD_BEND_TYPE_CLASSICRUNNER_X_POSITIVE
				#pragma shader_feature_local CURVEDWORLD_DISABLED_ON
				#pragma shader_feature_local CURVEDWORLD_NORMAL_TRANSFORMATION_ON
			#endif

			ENDHLSL
		}

		// This pass is used when drawing to a _CameraNormalsTexture texture
		Pass
		{
			Name "DepthNormals"
			Tags{"LightMode" = "DepthNormals"}

			ZWrite On
			Cull Off

			HLSLPROGRAM
			#pragma target 3.0

			#pragma vertex DepthNormalsVertex
			#pragma fragment DepthNormalsFragment

			#pragma multi_compile_instancing
			#pragma multi_compile _ _FLUIDFRENZY_INSTANCING

			#define CURVEDWORLD_BEND_ID_1

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Rendering/Shaders/Terrain/URP/TerraformTerrainDepthNormalsURP.hlsl"

			#if defined(FLUIDFRENZY_EXTERNALCOMPATIBILITY_CURVEDWORLD)
				#pragma shader_feature_local CURVEDWORLD_BEND_TYPE_CLASSICRUNNER_X_POSITIVE
				#pragma shader_feature_local CURVEDWORLD_DISABLED_ON
				#pragma shader_feature_local CURVEDWORLD_NORMAL_TRANSFORMATION_ON
			#endif

			ENDHLSL
		}
	}

	SubShader
	{
		Tags{ "RenderType" = "Opaque" "Queue" = "Geometry" }
		LOD 100
		Cull Off

		Pass
		{
			Name "FORWARD"
			Tags{ "LightMode" = "ForwardBase" }

			CGPROGRAM
			#pragma target 3.0
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_fwdbase
			#pragma multi_compile_fog
			#pragma multi_compile_instancing
			#pragma multi_compile _ _FLUIDFRENZY_INSTANCING

			#define CURVEDWORLD_BEND_ID_1

			#include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Rendering/Shaders/Terrain/Builtin/TerraformTerrainSharedBRP.hlsl"
			
			#if defined(FLUIDFRENZY_EXTERNALCOMPATIBILITY_CURVEDWORLD)
				#pragma shader_feature_local CURVEDWORLD_BEND_TYPE_CLASSICRUNNER_X_POSITIVE
				#pragma shader_feature_local CURVEDWORLD_DISABLED_ON
				#pragma shader_feature_local CURVEDWORLD_NORMAL_TRANSFORMATION_ON
			#endif

			#include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Rendering/Shaders/Terrain/Builtin/TerraformTerrainForwardBRP.hlsl"

			ENDCG
		}

		Pass
		{
			Name "FORWARD_DELTA"
			Tags{ "LightMode" = "ForwardAdd" }
			Blend One One
			Fog { Color(0,0,0,0) } // in additive pass fog should be black
			ZWrite Off
			ZTest LEqual

			CGPROGRAM
			#pragma target 3.0
			#pragma vertex vert
			#pragma fragment fragAdd
			#pragma multi_compile_fwdadd_fullshadows
			#pragma multi_compile_fog
			#pragma multi_compile _ _FLUIDFRENZY_INSTANCING

			#define CURVEDWORLD_BEND_ID_1

			#include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Rendering/Shaders/Terrain/Builtin/TerraformTerrainSharedBRP.hlsl"
			
			#if defined(FLUIDFRENZY_EXTERNALCOMPATIBILITY_CURVEDWORLD)
				#pragma shader_feature_local CURVEDWORLD_BEND_TYPE_CLASSICRUNNER_X_POSITIVE
				#pragma shader_feature_local CURVEDWORLD_DISABLED_ON
				#pragma shader_feature_local CURVEDWORLD_NORMAL_TRANSFORMATION_ON
			#endif

			#include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Rendering/Shaders/Terrain/Builtin/TerraformTerrainForwardBRP.hlsl"

			ENDCG
		}

		// ------------------------------------------------------------------
		//  Deferred pass
		Pass
		{
			Name "DEFERRED"
			Tags { "LightMode" = "Deferred" }

			CGPROGRAM
			#pragma target 3.0
			#pragma exclude_renderers nomrt

			// -------------------------------------
			#pragma multi_compile _ _FLUIDFRENZY_INSTANCING
			#pragma multi_compile_prepassfinal
			#pragma multi_compile_instancing

			#pragma vertex vert
			#pragma fragment fragDeferred

			#define CURVEDWORLD_BEND_ID_1

			#include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Rendering/Shaders/Terrain/Builtin/TerraformTerrainSharedBRP.hlsl"
			
			#if defined(FLUIDFRENZY_EXTERNALCOMPATIBILITY_CURVEDWORLD)
				#pragma shader_feature_local CURVEDWORLD_BEND_TYPE_CLASSICRUNNER_X_POSITIVE
				#pragma shader_feature_local CURVEDWORLD_DISABLED_ON
				#pragma shader_feature_local CURVEDWORLD_NORMAL_TRANSFORMATION_ON
			#endif

			#include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Rendering/Shaders/Terrain/Builtin/TerraformTerrainDeferredBRP.hlsl"

			ENDCG
		}

		Pass {
			Name "ShadowCaster"
			Tags { "LightMode" = "ShadowCaster" }

			ZWrite On ZTest LEqual
			CGPROGRAM

			#pragma target 3.0
			#pragma multi_compile_shadowcaster
			#pragma multi_compile_instancing
			#pragma multi_compile _ _FLUIDFRENZY_INSTANCING

			#pragma vertex vertShadowCaster
			#pragma fragment fragShadowCaster

			#define CURVEDWORLD_BEND_ID_1

			#include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Rendering/Shaders/Terrain/Builtin/TerraformTerrainSharedBRP.hlsl"
			
			#if defined(FLUIDFRENZY_EXTERNALCOMPATIBILITY_CURVEDWORLD)
				#pragma shader_feature_local CURVEDWORLD_BEND_TYPE_CLASSICRUNNER_X_POSITIVE
				#pragma shader_feature_local CURVEDWORLD_DISABLED_ON
				#pragma shader_feature_local CURVEDWORLD_NORMAL_TRANSFORMATION_ON
			#endif

			#include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Rendering/Shaders/Terrain/Builtin/TerraformTerrainShadowCasterBRP.hlsl"

			ENDCG
		}
	}

	CustomEditor "FluidFrenzy.Editor.TerraformTerrainSingleLayerShaderGUI"
}
