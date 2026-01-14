Shader "FluidFrenzy/ProceduralParticle"
{
	Properties
	{
        [CurvedWorldBendSettings] _CurvedWorldBendSettings("0|1|1", Vector) = (0, 0, 0, 0)
		_MainTex("Texture", 2D) = "white" {}
		_Color("Color", Color) = (1,1,1,1)
		[Gamma] _Metallic("Metallic", Range(0.0, 1.0)) = 0.0
        _Smoothness("Smoothness", Range(0.0, 1.0)) = 0.5
		[Normal]_NormalMap("Texture", 2D) = "bump"{}

		_Blend ("__blendmode", Integer) = 0.0
        _Cutoff("__clip", Range(0,1)) = 0.5
		[Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend("Source Blend", Integer) = 4
		[Enum(UnityEngine.Rendering.BlendMode)] _DstBlend("Dest Blend", Integer) = 1
        [HideInInspector] _ZWrite("__zw", Float) = 0.0
        [HideInInspector] _AlphaToMask("__alphaToMask", Integer) = 0.0

		_BillboardMode ("Billboard Mode", Float) = 0.0
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
		Tags { "Queue" = "Transparent" }

		Pass
		{
			Tags{"LightMode" = "UniversalForward"}

			Name "FluidFrenzyParticle"
			// No culling or depth
			Cull Off ZWrite [_ZWrite] ZTest Lequal
			Blend [_SrcBlend] [_DstBlend]
			AlphaToMask [_AlphaToMask]

			HLSLPROGRAM
			#pragma exclude_renderers gles

			#pragma vertex vertForward
			#pragma fragment fragForward
			#pragma shader_feature_local _ _NORMALMAP
			#pragma shader_feature_local _ _BILLBOARDMODE_CAMERA _BILLBOARDMODE_CAMERA_NORMAL_UP _BILLBOARDMODE_UP _BILLBOARDMODE_NORMAL
			#pragma shader_feature_local _ _ALPHABLEND_ON _ALPHATEST_ON _BLENDADDITIVE_ON

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
		    #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
            //#pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            //#pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #pragma multi_compile _ _FORWARD_PLUS
            #pragma multi_compile _ EVALUATE_SH_MIXED EVALUATE_SH_VERTEX

			#pragma multi_compile_fog

			#define CURVEDWORLD_BEND_ID_1
			#define FLUIDFRENZY_URP

			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

			#include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Rendering/Shaders/Library/FluidPipelineCompatibility.hlsl"
			#include_with_pragmas "Packages/com.frenzybyte.fluidfrenzy/Runtime/Rendering/ExternalCompatibility/FluidThirdPartyHeadersURP.hlsl"
			#include_with_pragmas "Packages/com.frenzybyte.fluidfrenzy/Runtime/Rendering/Shaders/Library/FluidFogHelpers.hlsl"

			#include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Rendering/Shaders/Particles/ProceduralParticleCommon.hlsl"

			#if defined(FLUIDFRENZY_EXTERNALCOMPATIBILITY_CURVEDWORLD)
				#pragma shader_feature_local CURVEDWORLD_BEND_TYPE_CLASSICRUNNER_X_POSITIVE
				#pragma shader_feature_local CURVEDWORLD_DISABLED_ON
				#pragma shader_feature_local CURVEDWORLD_NORMAL_TRANSFORMATION_ON
			#endif


			sampler2D _MainTex;
			sampler2D _NormalMap;
			float4 _Color;
			half _Cutoff;
			half _Metallic;
			half _Smoothness;

			float4x4 _ObjectToWorld;
			#define _SURFACE_TYPE_TRANSPARENT 1

			VaryingsParticle vertForward(uint vid : SV_VertexID, uint svInstanceID : SV_InstanceID)
			{
				VaryingsParticle output;

				float2 size;
				float3 position; // Position of the particle in sim space.
				float4 color;
				float rotation, angularVelocity, life, maxlife;
				SampleParticleData(svInstanceID, position, size, rotation, angularVelocity, color, life, maxlife);

				float3 vertex, normalOS;
				float4 tangentOS;
				float2 uv;
				SampleParticleVertexData(vid, GetWorldToViewMatrix(), vertex, uv, normalOS, tangentOS);

				#if defined(CURVEDWORLD_IS_INSTALLED) && !defined(CURVEDWORLD_DISABLED_ON)
				   #ifdef CURVEDWORLD_NORMAL_TRANSFORMATION_ON
					  CURVEDWORLD_TRANSFORM_VERTEX_AND_NORMAL(position.xyz, normalOS, tangentOS)
				   #else
					  CURVEDWORLD_TRANSFORM_VERTEX(position.xyz)
				   #endif
				#endif

				float3 positionOS;
				TransformParticleToBillboard(position, GetViewToWorldMatrix(), vertex, size, rotation, angularVelocity, positionOS);

				output.positionWS.xyz = mul(_ParticleSystemObjectToWorld, float4(positionOS, 1));
				output.positionCS = TransformObjectToHClip(positionOS);

				VertexPositionInputs vertexInput;
				vertexInput.positionWS = output.positionWS.xyz;
				vertexInput.positionVS = mul(GetWorldToViewMatrix(), float4(vertexInput.positionWS, 1));
				vertexInput.positionCS = output.positionCS;

				#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
					output.shadowCoord = GetShadowCoord(vertexInput);
				#endif

				half fogFactor = 0.0;
				#if !defined(_FOG_FRAGMENT)
					fogFactor = ComputeFogFactor(vertexInput.positionCS.z);
				#endif
				output.positionWS.w = fogFactor;

				output.viewDirWS = GetWorldSpaceNormalizeViewDir(vertexInput.positionWS);

				float3 normalWS = float3(0,1,0);
				#if !defined(_BILLBOARDMODE_CAMERA_NORMAL_UP) && !defined(_BILLBOARDMODE_UP)
					#if defined(_NORMALMAP)
						output.tangentWS = tangentOS.xyz;
						output.bitangentWS = GetWorldToViewMatrix()[1];
					#endif
					normalWS = output.normalWS = normalOS;
				#endif
				
				OUTPUT_SH(normalWS, output.vertexSH);


				output.color = color * _Color;
				float progress = life / max(0.01f,maxlife);
				output.color.a *= smoothstep(0.0, 0.2,PingPong(1 - progress, 0.5f));
				output.texcoord.xy = uv;
				return output;
			}

			inline void InitializeParticleLitSurfaceData(VaryingsParticle input, out SurfaceData outSurfaceData)
			{
				half4 albedo = tex2D(_MainTex, input.texcoord) * input.color;

				half2 metallicGloss = half2(_Metallic, _Smoothness);

				#if defined(_NORMALMAP)
					half3 normalTS = UnpackNormal(tex2D(_NormalMap, input.texcoord));
				#else
					half3 normalTS = 0;
				#endif

				outSurfaceData = (SurfaceData)0;
				outSurfaceData.albedo = albedo.rgb;
				outSurfaceData.specular = half3(0.0h, 0.0h, 0.0h);
				outSurfaceData.normalTS = normalTS;
				outSurfaceData.emission = 0;
				outSurfaceData.metallic = metallicGloss.r;
				outSurfaceData.smoothness = metallicGloss.g;
				outSurfaceData.occlusion = 1.0;

				outSurfaceData.albedo = AlphaModulate(outSurfaceData.albedo, albedo.a);
				outSurfaceData.alpha = albedo.a;

				outSurfaceData.clearCoatMask       = half(0.0);
				outSurfaceData.clearCoatSmoothness = half(1.0);
			}


			void InitializeInputData(VaryingsParticle input, half3 normalTS, out InputData inputData)
			{
				inputData = (InputData)0;

				inputData.positionWS = input.positionWS.xyz;

				half3 tangentWS = half3(1,0,0);
				half3 bitangentWS = half3(0,0,1);
				half3 normalWS = half3(0,1,0);

				#if !defined(_BILLBOARDMODE_CAMERA_NORMAL_UP) && !defined(_BILLBOARDMODE_UP)
					#if defined(_NORMALMAP)
						tangentWS = input.tangentWS.xyz;
						bitangentWS = input.bitangentWS.xyz;
					#endif
					normalWS = input.normalWS.xyz;
				#endif

				#if defined(_NORMALMAP)
					inputData.tangentToWorld = half3x3(tangentWS.xyz, bitangentWS.xyz, normalWS.xyz);
					inputData.normalWS = TransformTangentToWorld(normalTS, inputData.tangentToWorld);// * sign(input.normalWS.xyz);
				#else
					inputData.normalWS = normalize(input.normalWS);
				#endif

				float3 viewDirWS = SafeNormalize(input.viewDirWS);

				inputData.viewDirectionWS = viewDirWS;

				#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
					inputData.shadowCoord = input.shadowCoord;
				#elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
					inputData.shadowCoord = TransformWorldToShadowCoord(inputData.positionWS);
				#else
					inputData.shadowCoord = float4(0, 0, 0, 0);
				#endif

				inputData.fogCoord = InitializeInputDataFog(float4(input.positionWS.xyz, 1.0), input.positionWS.w);
				inputData.vertexLighting = half3(0.0h, 0.0h, 0.0h);
				inputData.bakedGI = SampleSHPixel(input.vertexSH, inputData.normalWS);
				inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
				inputData.shadowMask = half4(1, 1, 1, 1);

				#if defined(DEBUG_DISPLAY) && !defined(PARTICLES_EDITOR_META_PASS)
				inputData.vertexSH = input.vertexSH;
				#endif
			}

			float4 fragForward(VaryingsParticle input) : SV_Target
			{
			
				SurfaceData surfaceData;
				InitializeParticleLitSurfaceData(input, surfaceData);

				InputData inputData;
				InitializeInputData(input, surfaceData.normalTS, inputData);
				half4 color = UniversalFragmentPBR(inputData, surfaceData);
				ApplyFog(color, input.positionWS.xyz, inputData.normalizedScreenSpaceUV, input.positionWS.w);


				#if defined(_ALPHATEST_ON)
					clip(surfaceData.alpha - _Cutoff);
				#endif

				return color;
			}

			ENDHLSL
		}
	}

    SubShader
    {
		Tags { "RenderType" = "Transparent" "Queue" = "Transparent" }

		Pass
		{
			Tags{ "LightMode" = "ForwardBase" }

			Name "FluidFrenzyParticle"
			// No culling or depth
			Cull Off ZWrite [_ZWrite] ZTest Lequal
			Blend [_SrcBlend] [_DstBlend]
			AlphaToMask [_AlphaToMask]

			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 5.0
			#pragma shader_feature_local _ _NORMALMAP
			#pragma shader_feature_local _ _BILLBOARDMODE_CAMERA _BILLBOARDMODE_CAMERA_NORMAL_UP _BILLBOARDMODE_UP _BILLBOARDMODE_NORMAL
			#pragma shader_feature_local _ _ALPHABLEND_ON _ALPHATEST_ON _ALPHAPREMULTIPLY_ON _BLENDADDITIVE_ON
			
			#pragma exclude_renderers gles

			#pragma multi_compile_fog

			#include "UnityCG.cginc"
			#include "UnityStandardUtils.cginc"
			#include "UnityShadowLibrary.cginc"
			#include "UnityStandardCore.cginc"
			#include "UnityStandardInput.cginc"
			#include "UnityStandardBRDF.cginc"

			#define CURVEDWORLD_BEND_ID_1
			#define FLUIDFRENZY_BIRP

			#include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Rendering/Shaders/Library/FluidPipelineCompatibility.hlsl"
			#include_with_pragmas "Packages/com.frenzybyte.fluidfrenzy/Runtime/Rendering/ExternalCompatibility/FluidThirdPartyHeadersBRP.hlsl"
			#include_with_pragmas "Packages/com.frenzybyte.fluidfrenzy/Runtime/Rendering/Shaders/Library/FluidFogHelpers.hlsl"

			#include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Rendering/Shaders/Particles/ProceduralParticleCommon.hlsl"

			#if defined(FLUIDFRENZY_EXTERNALCOMPATIBILITY_CURVEDWORLD)
				#pragma shader_feature_local CURVEDWORLD_BEND_TYPE_CLASSICRUNNER_X_POSITIVE
				#pragma shader_feature_local CURVEDWORLD_DISABLED_ON
				#pragma shader_feature_local CURVEDWORLD_NORMAL_TRANSFORMATION_ON
			#endif

			struct VaryingParticle
			{
				float2 uv : TEXCOORD0;
				float4 color : TEXCOORD1;
				float3 positionWS : TEXCOORD2;

				#if !defined(_BILLBOARDMODE_CAMERA_NORMAL_UP) && !defined(_BILLBOARDMODE_UP)
					#if _NORMALMAP
						float3 tangentWS           : TEXCOORD3;
						float3 bitangentWS           : TEXCOORD4;
					#endif
					float3 normalWS           : TEXCOORD5;
				#endif
				half3 viewDirWS        : TEXCOORD6;

				UNITY_FOG_COORDS(7)
				float4 positionCS : SV_POSITION;
			};

			sampler2D _NormalMap;
			float _Smoothness;
			float4x4 _ObjectToWorld;

			VaryingParticle vert(uint vid : SV_VertexID, uint svInstanceID : SV_InstanceID)
			{
				VaryingParticle output;

				float2 size;
				float3 position; // Position of the particle in sim space.
				float4 color;
				float rotation, angularVelocity, life, maxlife;
				SampleParticleData(svInstanceID, position, size, rotation, angularVelocity, color, life, maxlife);

				float3 vertex, normalOS;
				float4 tangentOS;
				float2 uv;
				SampleParticleVertexData(vid, UNITY_MATRIX_V, vertex, uv, normalOS, tangentOS);

				#if defined(CURVEDWORLD_IS_INSTALLED) && !defined(CURVEDWORLD_DISABLED_ON)
				   #ifdef CURVEDWORLD_NORMAL_TRANSFORMATION_ON
					  CURVEDWORLD_TRANSFORM_VERTEX_AND_NORMAL(position, normalOS, tangentOS)
				   #else
					  CURVEDWORLD_TRANSFORM_VERTEX(position)
				   #endif
				#endif

				float3 positionOS;
				TransformParticleToBillboard(position, UNITY_MATRIX_I_V, vertex, size, rotation, angularVelocity, positionOS);

				output.positionWS = mul(_ParticleSystemObjectToWorld, float4(positionOS, 1));
				output.viewDirWS = normalize(output.positionWS - _WorldSpaceCameraPos);
				output.positionCS = UnityObjectToClipPos(float4(positionOS,1));

				#if !defined(_BILLBOARDMODE_CAMERA_NORMAL_UP) && !defined(_BILLBOARDMODE_UP)
					#if defined(_NORMALMAP)
						output.tangentWS = tangentOS;
						output.bitangentWS = UNITY_MATRIX_V[1];
					#endif
					output.normalWS = normalOS;
				#endif

				UNITY_TRANSFER_FOG(output, output.positionCS);
				output.color = color * _Color;

				float progress = life / max(0.01f,maxlife);
				output.color.a *= smoothstep(0.0, 0.2,PingPong(1 - progress, 0.5f));
				output.uv.xy = uv;
				return output;
			}

			float4 frag(VaryingParticle input) : SV_Target
			{
				float4 diffuse = tex2D(_MainTex, input.uv);
				half2 metallicGloss = half2(_Metallic, _Smoothness);
				#if _NORMALMAP
					float3 normalTS = UnpackNormalWithScale(tex2D(_NormalMap, input.uv),1);
				#endif

				half3 tangentWS = half3(1,0,0);
				half3 bitangentWS = half3(0,0,1);
				half3 normalWS = half3(0,1,0);

				#if !defined(_BILLBOARDMODE_CAMERA_NORMAL_UP) && !defined(_BILLBOARDMODE_UP)
					#if defined(_NORMALMAP)
						tangentWS = input.tangentWS.xyz;
						bitangentWS = input.bitangentWS.xyz;
					#endif
					normalWS = input.normalWS.xyz;
				#endif
				#if defined(_NORMALMAP)
					half3x3 tangentToWorld = half3x3(tangentWS.xyz, bitangentWS.xyz, normalWS.xyz);
					normalWS = normalTS.x * tangentWS + normalTS.y * bitangentWS + normalTS.z * normalWS;
					normalWS.xyz = normalize(normalWS.xyz);
				#else
					normalWS = normalize(normalWS);
				#endif
				//return float4(normalWS, 1);
				UnityGI gi = (UnityGI)(0);
				half3 ambient = ShadeSH9(half4(normalWS, 1.0));
				float3 viewDir = input.viewDirWS;

				Unity_GlossyEnvironmentData g;
				g.roughness = 1 - metallicGloss.g;
				g.reflUVW = reflect(-viewDir, normalWS);;

				gi.indirect.diffuse = ShadeSHPerPixel(normalWS, ambient, input.positionWS.xyz);
				gi.indirect.specular = Unity_GlossyEnvironment(UNITY_PASS_TEXCUBE(unity_SpecCube0), unity_SpecCube0_HDR, g);

				half alpha = diffuse.a * input.color.a;
				half3 particleColor = diffuse.rgb * input.color.rgb;
				half3 particleSpecular = 0;
				half oneMinusReflectivity = 0;
				half3 albedo = DiffuseAndSpecularFromMetallic(particleColor, metallicGloss.r, particleSpecular, oneMinusReflectivity);
				UnityLight light;
				light.color = _LightColor0;
				light.dir = _WorldSpaceLightPos0.xyz;
				particleColor.rgb = BRDF1_Unity_PBS(particleColor, particleSpecular, oneMinusReflectivity, metallicGloss.g, normalWS, viewDir, light, gi.indirect);

				float4 color = float4(particleColor, alpha);
				#if defined(_ALPHATEST_ON)
					clip(alpha - _Cutoff);
				#endif

				ApplyFog(color, input.positionWS.xyz, input.positionCS.xy * rcp(_ScreenParams.xy), UNITY_GET_FOGCOORD(input));

				#if defined(_ALPHABLEND_ON) || defined(_ALPHAPREMULTIPLY_ON)
					color.a = alpha;
					#if defined(_ALPHAPREMULTIPLY_ON)
						color.rgb *= alpha;
					#endif
				#else
					color.a = 1;
				#endif

				return color;
			}

			ENDHLSL
		}
    }
	CustomEditor "FluidFrenzy.Editor.FluidParticleShaderGUI"
}
