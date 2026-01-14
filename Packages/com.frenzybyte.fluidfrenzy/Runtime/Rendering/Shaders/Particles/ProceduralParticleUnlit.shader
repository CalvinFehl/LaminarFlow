Shader "FluidFrenzy/ProceduralParticleUnlit"
{
	Properties
	{
        [CurvedWorldBendSettings] _CurvedWorldBendSettings("0|1|1", Vector) = (0, 0, 0, 0)
		_MainTex("Texture", 2D) = "white" {}
		_Color("Color", Color) = (1,1,1,1)

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
			#pragma shader_feature_local _ _BILLBOARDMODE_CAMERA _BILLBOARDMODE_UP _BILLBOARDMODE_NORMAL
			#pragma shader_feature_local _ _ALPHABLEND_ON _ALPHATEST_ON _ALPHAPREMULTIPLY_ON _BLENDADDITIVE_ON

			#define CURVEDWORLD_BEND_ID_1
			#define FLUIDFRENZY_URP

			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

			#include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Rendering/Shaders/Library/FluidPipelineCompatibility.hlsl"
			#include_with_pragmas "Packages/com.frenzybyte.fluidfrenzy/Runtime/Rendering/ExternalCompatibility/FluidThirdPartyHeadersURP.hlsl"
			#include_with_pragmas "Packages/com.frenzybyte.fluidfrenzy/Runtime/Rendering/Shaders/Library/FluidFogHelpers.hlsl"

			#include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Rendering/Shaders/Particles/ProceduralParticleCommon.hlsl"

			#if defined(FLUIDFRENZY_EXTERNALCOMPATIBILITY_CURVEDWORLD)
				#pragma shader_feature_local CURVEDWORLD_BEND_TYPE_CLASSICRUNNER_X_POSITIVE
				#pragma shader_feature_local CURVEDWORLD_DISABLED_ON
				#pragma shader_feature_local CURVEDWORLD_NORMAL_TRANSFORMATION_ON
			#endif

			struct v2f_unlit
			{
				float2 uv : TEXCOORD0;
				float4 color : TEXCOORD1;
				float4 positionWS : TEXCOORD2;
				float4 positionCS : SV_POSITION;
			};


			sampler2D _MainTex;
			float4 _Color;
			float _Cutoff;

			v2f_unlit vertForward(uint vid : SV_VertexID, uint svInstanceID : SV_InstanceID)
			{
				v2f_unlit output;

				float2 size;
				float3 position;
				float4 color;
				float rotation, angularVelocity, life, maxlife;
				SampleParticleData(svInstanceID, position, size, rotation, angularVelocity, color, life, maxlife);

				float3 vertex, normalOS;
				float4 tangentOS;
				float2 uv;
				SampleParticleVertexData(vid, UNITY_MATRIX_V, vertex, uv, normalOS, tangentOS);

				#if defined(CURVEDWORLD_IS_INSTALLED) && !defined(CURVEDWORLD_DISABLED_ON)
					  CURVEDWORLD_TRANSFORM_VERTEX(position)
				#endif

				float3 positionOS;
				TransformParticleToBillboard(position, GetViewToWorldMatrix(), vertex, size, rotation, angularVelocity, positionOS);

				output.positionWS.xyz = mul(_ParticleSystemObjectToWorld, float4(positionOS, 1)).xyz;
				output.positionCS = TransformObjectToHClip(positionOS);

				half fogFactor = 0.0;
				#if !defined(_FOG_FRAGMENT)
					fogFactor = ComputeFogFactor(output.positionCS.z);
				#endif
				output.positionWS.w = fogFactor;
				
				output.color = color * _Color;
				float progress = life / max(0.01f,maxlife);
				output.color.a *= smoothstep(0.0, 0.2,PingPong(1 - progress, 0.5f) * 2);
				output.uv.xy = uv;
				return output;
			}

			float4 fragForward(v2f_unlit input) : SV_Target
			{
				float4 texColor = tex2D(_MainTex, input.uv);
				float4 color = texColor * input.color;

				float alpha = texColor.a * input.color.a;

				#if defined(_ALPHATEST_ON)
					clip(alpha - _Cutoff);
				#endif
				ApplyFog(color, input.positionWS.xyz, GetNormalizedScreenSpaceUV(input.positionCS), input.positionWS.w);

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
			#pragma shader_feature_local _ _BILLBOARDMODE_CAMERA _BILLBOARDMODE_UP _BILLBOARDMODE_NORMAL
			#pragma shader_feature_local _ _ALPHABLEND_ON _ALPHATEST_ON _ALPHAPREMULTIPLY_ON _BLENDADDITIVE_ON

			#pragma exclude_renderers gles
			sampler2D _MainTex;
			float4 _Color;
			float _Cutoff;

			#include "UnityCG.cginc"

			#define CURVEDWORLD_BEND_ID_1
			#define FLUIDFRENZY_BIRP
			
			#include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Rendering/Shaders/Library/FluidPipelineCompatibility.hlsl"
			#include_with_pragmas "Packages/com.frenzybyte.fluidfrenzy/Runtime/Rendering/ExternalCompatibility/FluidThirdPartyHeadersBRP.hlsl"
			#include_with_pragmas "Packages/com.frenzybyte.fluidfrenzy/Runtime/Rendering/Shaders/Library/FluidFogHelpers.hlsl"

			#if defined(FLUIDFRENZY_EXTERNALCOMPATIBILITY_CURVEDWORLD)
				#pragma shader_feature_local CURVEDWORLD_BEND_TYPE_CLASSICRUNNER_X_POSITIVE
				#pragma shader_feature_local CURVEDWORLD_DISABLED_ON
				#pragma shader_feature_local CURVEDWORLD_NORMAL_TRANSFORMATION_ON
			#endif

			#include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Rendering/Shaders/Particles/ProceduralParticleCommon.hlsl"
				
			struct v2f_unlit
			{
				float2 uv : TEXCOORD0;
				float4 color : TEXCOORD1;
				float3 positionWS : TEXCOORD2;
				float4 positionCS : SV_POSITION;
				UNITY_FOG_COORDS(7)
			};

			v2f_unlit vert(uint vid : SV_VertexID, uint svInstanceID : SV_InstanceID)
			{
				v2f_unlit output;

				float2 size;
				float3 position;
				float4 color;
				float rotation, angularVelocity, life, maxlife;
				SampleParticleData(svInstanceID, position, size, rotation, angularVelocity, color, life, maxlife);

				float3 vertex, normalOS;
				float4 tangentOS;
				float2 uv;
				SampleParticleVertexData(vid, UNITY_MATRIX_V, vertex, uv, normalOS, tangentOS);

				#if defined(CURVEDWORLD_IS_INSTALLED) && !defined(CURVEDWORLD_DISABLED_ON)
					  CURVEDWORLD_TRANSFORM_VERTEX(position)
				#endif

				float3 positionOS;
				TransformParticleToBillboard(position, UNITY_MATRIX_I_V, vertex, size, rotation, angularVelocity, positionOS);
				output.positionWS.xyz = mul(_ParticleSystemObjectToWorld, float4(positionOS, 1));
				output.positionCS = UnityObjectToClipPos(float4(positionOS,1));

				output.color = color * _Color;

				float progress = life / max(0.01f,maxlife);
				output.color.a *= smoothstep(0.0, 0.2,PingPong(1 - progress, 0.5f) * 2);
				output.uv.xy = uv;
				return output;
			}

			float4 frag(v2f_unlit input) : SV_Target
			{
				float4 texColor = tex2D(_MainTex, input.uv);
				float4 color = texColor * input.color;

				float alpha =  texColor.a * input.color.a;

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
