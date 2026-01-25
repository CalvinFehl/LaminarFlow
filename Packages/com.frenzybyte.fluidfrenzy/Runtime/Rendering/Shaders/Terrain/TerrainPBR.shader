Shader "Hidden/FluidFrenzy/Legacy/TerrainPBR"
{
	Properties
	{
		_HeightTex("Height Tex", 2D) = "black" {}
		_Color("Color", Color) = (1,1,1,1)
		_MainTex("Albedo", 2D) = "white" {}

		_Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5

		_Glossiness("Smoothness", Range(0.0, 1.0)) = 0.5
		_GlossMapScale("Smoothness Scale", Range(0.0, 1.0)) = 1.0
		[Enum(Metallic Alpha,0,Albedo Alpha,1)] _SmoothnessTextureChannel("Smoothness texture channel", Float) = 0

		[Gamma] _Metallic("Metallic", Range(0.0, 1.0)) = 0.0
		_MetallicGlossMap("Metallic", 2D) = "white" {}

		[ToggleOff] _SpecularHighlights("Specular Highlights", Float) = 1.0
		[ToggleOff] _GlossyReflections("Glossy Reflections", Float) = 1.0

		_BumpScale("Scale", Float) = 1.0
		[Normal] _BumpMap("Normal Map", 2D) = "bump" {}

		_Parallax("Height Scale", Range(0.005, 0.08)) = 0.02
		_ParallaxMap("Height Map", 2D) = "black" {}

		_OcclusionStrength("Strength", Range(0.0, 1.0)) = 1.0
		_OcclusionMap("Occlusion", 2D) = "white" {}

		_EmissionColor("Color", Color) = (0,0,0)
		_EmissionMap("Emission", 2D) = "white" {}

		_DetailMask("Detail Mask", 2D) = "white" {}

		_DetailAlbedoMap("Detail Albedo x2", 2D) = "grey" {}
		_DetailNormalMapScale("Scale", Float) = 1.0
		[Normal] _DetailNormalMap("Normal Map", 2D) = "bump" {}

		[Enum(UV0,0,UV1,1)] _UVSec("UV Set for secondary textures", Float) = 0


		// Blending state
		[HideInInspector] _Mode("__mode", Float) = 0.0
		[HideInInspector] _SrcBlend("__src", Float) = 1.0
		[HideInInspector] _DstBlend("__dst", Float) = 0.0
		[HideInInspector] _ZWrite("__zw", Float) = 1.0
	}

		SubShader
		{
			Tags{ "RenderType" = "Opaque" }
			LOD 300

			CGINCLUDE
			#include "UnityCG.cginc"
			#include "UnityStandardCore.cginc"
			#include "UnityStandardInput.cginc"

			sampler2D _HeightTex;
			float4 _HeightTex_TexelSize;

			float4 _TexelWorldSize;

			float3 _MousePos;
			float _MouseRadius;


			#ifdef _ALPHA_TEST
			float _AlphaThreshold;
			#endif


			float3 GetTerrainNormal(float2 uv)
			{
				float texelwss = _TexelWorldSize.x;
				float2 du = float2(_HeightTex_TexelSize.x, 0);
				float2 dv = float2(0, _HeightTex_TexelSize.y);

				float state_l = tex2Dlod(_HeightTex, float4(uv.xy + du, 0, 0)).x * 50;
				float state_r = tex2Dlod(_HeightTex, float4(uv.xy - du, 0, 0)).x * 50;
				float state_t = tex2Dlod(_HeightTex, float4(uv.xy + dv, 0, 0)).x * 50;
				float state_b = tex2Dlod(_HeightTex, float4(uv.xy - dv, 0, 0)).x * 50;

				half dhdu = ((state_r)-(state_l));
				half dhdv = ((state_b)-(state_t));
				float3 normal = (float3(dhdu, texelwss.x, dhdv));
				return normal;
			}

			VertexOutputForwardBase vert(VertexInput v)
			{
				UNITY_SETUP_INSTANCE_ID(v);
				VertexOutputForwardBase o;
				UNITY_INITIALIZE_OUTPUT(VertexOutputForwardBase, o);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

				float4 posWorld = mul(unity_ObjectToWorld, v.vertex);
				#if UNITY_REQUIRE_FRAG_WORLDPOS
					#if UNITY_PACK_WORLDPOS_WITH_TANGENT
						o.tangentToWorldAndPackedData[0].w = posWorld.x;
						o.tangentToWorldAndPackedData[1].w = posWorld.y;
						o.tangentToWorldAndPackedData[2].w = posWorld.z;
					#else
						o.posWorld = posWorld.xyz;
					#endif
				#endif

						o.tex = TexCoords(v);

						float2 waveSample = tex2Dlod(_HeightTex, float4(o.tex.xy, 0, 0)).xy;
						v.vertex.y += waveSample.x;

				o.pos = UnityObjectToClipPos(v.vertex);

				o.eyeVec.xyz = NormalizePerVertexNormal(posWorld.xyz - _WorldSpaceCameraPos);

				float3 normalWorld = GetTerrainNormal(o.tex);//float3(0,1,0);//UnityObjectToWorldNormal(v.normal.xyz);
				#ifdef _TANGENT_TO_WORLD
					float4 tangentWorld = float4(UnityObjectToWorldDir(v.tangent.xyz), v.tangent.w);

					float3x3 tangentToWorld = CreateTangentToWorldPerVertex(normalWorld, tangentWorld.xyz, tangentWorld.w);
					o.tangentToWorldAndPackedData[0].xyz = normalize(tangentToWorld[0]);
					o.tangentToWorldAndPackedData[1].xyz = normalize(tangentToWorld[1]);
					o.tangentToWorldAndPackedData[2].xyz = normalize(tangentToWorld[2]);
				#else
					o.tangentToWorldAndPackedData[0].xyz = 0;
					o.tangentToWorldAndPackedData[1].xyz = 0;
					o.tangentToWorldAndPackedData[2].xyz = normalWorld;
				#endif

					//We need this for shadow receving
					UNITY_TRANSFER_LIGHTING(o, v.uv1);

					o.ambientOrLightmapUV = VertexGIForward(v, posWorld, normalWorld);

					#ifdef _PARALLAXMAP
						TANGENT_SPACE_ROTATION;
						half3 viewDirForParallax = mul(rotation, ObjSpaceViewDir(v.vertex));
						o.tangentToWorldAndPackedData[0].w = viewDirForParallax.x;
						o.tangentToWorldAndPackedData[1].w = viewDirForParallax.y;
						o.tangentToWorldAndPackedData[2].w = viewDirForParallax.z;
					#endif

					UNITY_TRANSFER_FOG_COMBINED_WITH_EYE_VEC(o,o.pos);
					return o;
				}


				float4 frag(VertexOutputForwardBase In) : SV_Target
				{

					UnityGI gi;
					ResetUnityGI(gi);

					//Inputs//
					fixed3 albedo = Albedo(In.tex);											// base (diffuse or specular) color
					float2 metallicGloss = MetallicGloss(In.tex.xy);

					float3 normalWorld = PerPixelWorldNormal(In.tex, In.tangentToWorldAndPackedData);

					half Metallic = metallicGloss.r;														// 0=non-metal, 1=metal
					half Smoothness = metallicGloss.g;														// 0=rough, 1=smooth
					fixed Alpha = 1.0f;																		// alpha for transparencies
					float3 viewDir = normalize(half3(-In.eyeVec.xyz));
					float3 worldPos = IN_WORLDPOS(In);
					////////

					Unity_GlossyEnvironmentData g;
					g.roughness /* perceptualRoughness */ = SmoothnessToPerceptualRoughness(Smoothness);
					g.reflUVW = reflect(viewDir, normalWorld);

					gi.light = MainLight();//no need for light, halfs the number of instructions
					gi.indirect.diffuse = ShadeSHPerPixel(normalWorld, In.ambientOrLightmapUV, worldPos);
					gi.indirect.specular = Unity_GlossyEnvironment(UNITY_PASS_TEXCUBE(unity_SpecCube0), unity_SpecCube0_HDR, g);


					half oneMinusReflectivity;
					half3 specColor;
					albedo = DiffuseAndSpecularFromMetallic(albedo, Metallic, /*out*/ specColor, /*out*/ oneMinusReflectivity);

					// shader relies on pre-multiply alpha-blend (_SrcBlend = One, _DstBlend = OneMinusSrcAlpha)
					// this is necessary to handle transparency in physically correct way - only diffuse component gets affected by alpha
					half outputAlpha;
					albedo = PreMultiplyAlpha(albedo, Alpha, oneMinusReflectivity, /*out*/ outputAlpha);

					half4 c = UNITY_BRDF_PBS(albedo, specColor, oneMinusReflectivity, Smoothness, normalWorld, viewDir, gi.light, gi.indirect);
					c.a = outputAlpha;

					float2 pos = In.tex.zw;
					float l = length(pos);


					float2 pxDist = (pos - _MousePos.xy);
					float dist = length(pxDist);

					float smoothRing = step(dist, _MouseRadius) * step(_MouseRadius - 0.001f, dist);
					c = lerp(c, float4(0,1,0,1), smoothRing);

					return c;
				}


				float4 fragShadow(VertexOutputForwardBase In) : SV_Target
				{
					return 0;
				}
			ENDCG

			Pass
			{
				Name "FORWARD"
				Tags{ "LightMode" = "ForwardBase" }

				ColorMask RGBA

				Blend[_SrcBlend][_DstBlend]
				ZWrite[_ZWrite]

				CGPROGRAM
				#pragma target 5.0
				#pragma only_renderers d3d11
				#pragma fragmentoption ARB_precision_hint_fastest

				#pragma shader_feature _ALBEDOMAP
				#pragma shader_feature _NORMALMAP
				#pragma shader_feature _METALLICGLOSSMAP

				#pragma shader_feature _ _ALPHA_TEST _ALPHABLEND_ON
				#pragma shader_feature _VERTEXALPHA
				#pragma shader_feature _AMBIENT_LIGHT
				#pragma shader_feature _DIRECTIONAL_LIGHT
				#pragma shader_feature _SPECULARHIGHLIGHTS_OFF

				#pragma vertex vert
				#pragma fragment frag

				ENDCG
			}

				// ------------------------------------------------------------------
				//  Shadow rendering pass
				Pass{
					Name "ShadowCaster"
					Tags { "LightMode" = "ShadowCaster" }

					ZWrite On ZTest LEqual

					CGPROGRAM
					#pragma target 2.0

					#pragma skip_variants SHADOWS_SOFT
					#pragma multi_compile_shadowcaster

					#pragma vertex vert
					#pragma fragment fragShadow


					ENDCG
				}
		}
			FallBack "VertexLit"
			CustomEditor "StandardShaderGUI"
}