Shader "FluidFrenzy/TerraformTerrain"
{
    Properties
    {
        [CurvedWorldBendSettings] _CurvedWorldBendSettings("0|1|1", Vector) = (0, 0, 0, 0)

		_BaseLayerColor0("Base Layer 0 Color", Color) = (1,1,1,1)
		_BaseLayerColor1("Base Layer 1 Color", Color) = (1,1,1,1)
		_BaseLayerColor2("Base Layer 2 Color", Color) = (1,1,1,1)
		_BaseLayerColor3("Base Layer 3 Color", Color) = (1,1,1,1)
		_BaseLayerAlbedo("Base Layer Albedo", 2DArray) = "white"{}
		_BaseLayerMaskMap("Base Layer Mask Map", 2DArray) = "white"{}
		[Normal] _BaseLayerBumpMap("Base Layer Normal Map", 2DArray) = "bump" {}
		_BaseLayerBumpScale("Base Layer Normal Scale", Vector) = (1, 1, 1, 1)
		_BaseLayer0_ST("Base Layer Tiling/Offset", Vector) = (30, 30, 0, 0)
		_BaseLayer1_ST("Base Layer Tiling/Offset", Vector) = (30, 30, 0, 0)
		_BaseLayer2_ST("Base Layer Tiling/Offset", Vector) = (30, 30, 0, 0)
		_BaseLayer3_ST("Base Layer Tiling/Offset", Vector) = (30, 30, 0, 0)
		
		[Header(Top layer)]
		_DynamicLayerAlbedo("Dynamic Layer Albedo", 2DArray) = "white"{}
		_DynamicLayerMaskMap("Dynamic Layer Mask Map", 2DArray) = "white"{}
		[Normal] _DynamicLayerBumpMap("Dynamic Layer Normal Map", 2DArray) = "bump" {}
		_DynamicLayerBumpScale("Dynamic Layer Normal Scale", Vector) = (1, 1, 1, 1)
		_DynamicLayer0_ST("Dynamic Layer Tiling/Offset", Vector) = (30, 30, 0, 0)
		_DynamicLayer1_ST("Dynamic Layer Tiling/Offset", Vector) = (30, 30, 0, 0)
		_DynamicLayer2_ST("Dynamic Layer Tiling/Offset", Vector) = (30, 30, 0, 0)

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

			#define TERRAFORM_TEXTURE_ARRAY
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

			#define TERRAFORM_TEXTURE_ARRAY
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

			#define TERRAFORM_TEXTURE_ARRAY
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

			#define TERRAFORM_TEXTURE_ARRAY
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

			#define TERRAFORM_TEXTURE_ARRAY
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

			#define TERRAFORM_TEXTURE_ARRAY
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

			#define TERRAFORM_TEXTURE_ARRAY
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

			#define TERRAFORM_TEXTURE_ARRAY
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

			#define TERRAFORM_TEXTURE_ARRAY
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

	CustomEditor "FluidFrenzy.Editor.TerraformTerrainShaderGUI"
}
