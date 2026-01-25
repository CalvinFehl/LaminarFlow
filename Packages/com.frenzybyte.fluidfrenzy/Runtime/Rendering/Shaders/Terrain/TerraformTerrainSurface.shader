Shader "FluidFrenzy/Legacy/TerraformTerrainSurface"
{
    Properties
    {
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
		
		[Header(Top layer)]
		_TopLayerAlbedo("Top Layer Albedo", 2D) = "white"{}
		_TopLayerMaskMap("Top Layer Mask Map", 2D) = "white"{}
		[Normal] _TopLayerBumpMap("Top Layer Normal Map", 2D) = "bump" {}
		_TopLayerBumpScale("Top Layer Scale", Float) = 1.0

		_Splatmap("Splatmap", 2D) = "black"{}
    }
    SubShader
    {
        LOD 100
		Cull Off

		CGPROGRAM
		#pragma surface surf Standard vertex:SplatmapVert addshadow
		#pragma target 3.0
		#include "UnityPBSLighting.cginc"

		sampler2D _Splatmap;
		sampler2D _HeightField;
		float4 _HeightField_TexelSize;
		float4 _TexelWorldSize;


		float4 _Layer0Color;
		sampler2D _Layer0Albedo;
		float4 _Layer0Albedo_ST;
		sampler2D _Layer0MaskMap;
		sampler2D _Layer0BumpMap;
		float _Layer0BumpScale;

		float4 _Layer1Color;
		sampler2D _Layer1Albedo;
		float4 _Layer1Albedo_ST;
		sampler2D _Layer1MaskMap;
		sampler2D _Layer1BumpMap;
		float _Layer1BumpScale;


		sampler2D _TopLayerAlbedo;
		float4 _TopLayerAlbedo_ST;
		sampler2D _TopLayerMaskMap;
		sampler2D _TopLayerBumpMap;
		float _TopLayerBumpScale;

		struct Input
		{
			float4 tc01;
			UNITY_FOG_COORDS(0)
		};

		float3 GetTerrainNormal(float2 uv)
		{
			float texelwss = _TexelWorldSize.x * 2;
			float2 du = float2(_HeightField_TexelSize.x, 0);
			float2 dv = float2(0, _HeightField_TexelSize.y);

			float state_l = dot(tex2Dlod(_HeightField, float4(uv.xy + du, 0, 0)).xy, (1.0f).xx);
			float state_r = dot(tex2Dlod(_HeightField, float4(uv.xy - du, 0, 0)).xy, (1.0f).xx);
			float state_t = dot(tex2Dlod(_HeightField, float4(uv.xy + dv, 0, 0)).xy, (1.0f).xx);
			float state_b = dot(tex2Dlod(_HeightField, float4(uv.xy - dv, 0, 0)).xy, (1.0f).xx);

			float dhdu = ((state_r)-(state_l));
			float dhdv = ((state_b)-(state_t));
			float3 normal = normalize(float3(dhdu, texelwss.x, dhdv));
			return normal;
		}


		void SplatmapVert(inout appdata_full v, out Input data)
		{
			UNITY_INITIALIZE_OUTPUT(Input, data);

			float3 vertex = v.vertex;
			float2 uv = v.texcoord;
			float4 heightSample = dot(tex2Dlod(_HeightField, float4(uv, 0, 0)).xy, (1.0f).xx);
			v.vertex.y += heightSample.x;

			v.normal = GetTerrainNormal(uv);
			v.tangent.xyz = cross(v.normal, float3(0, 0, 1));
			v.tangent.w = -1 * unity_WorldTransformParams.w;

			data.tc01.xy = uv;
			data.tc01.zw = uv;
			float4 pos = UnityObjectToClipPos(v.vertex);
			UNITY_TRANSFER_FOG(data, pos);
		}

		void surf(Input IN, inout SurfaceOutputStandard o) {
			
			float4 heightSample = tex2Dlod(_HeightField, float4(IN.tc01.zw, 0, 0));
			float4 spat = tex2Dlod(_Splatmap, float4(IN.tc01.zw, 0, 0));


			float4 maskMapLayer0 = tex2D(_Layer0MaskMap, IN.tc01.xy * _Layer0Albedo_ST.xy) * spat.r;
			float4 albedoLayer0 = tex2D(_Layer0Albedo, IN.tc01.xy * _Layer0Albedo_ST.xy) * spat.r * _Layer0Color;
			float smoothnessLayer0 = maskMapLayer0.a;
			float metallicLayer0 = maskMapLayer0.r;
			float occlusionLayer0 = maskMapLayer0.g;
			float3 normalLayer0 = UnpackNormalWithScale(tex2D(_Layer0BumpMap, IN.tc01.xy * _Layer0Albedo_ST.xy), _Layer0BumpScale) * spat.r;

			float4 maskMapLayer1 = tex2D(_Layer1MaskMap, IN.tc01.xy * _Layer1Albedo_ST.xy) * spat.g;
			float4 albedoLayer1 = tex2D(_Layer1Albedo, IN.tc01.xy * _Layer1Albedo_ST.xy) * spat.g * _Layer1Color;
			float smoothnessLayer1 = maskMapLayer1.a;
			float metallicLayer1 = maskMapLayer1.r;
			float occlusionLayer1 = maskMapLayer1.g;
			float3 normalLayer1 = UnpackNormalWithScale(tex2D(_Layer1BumpMap, IN.tc01.xy * _Layer1Albedo_ST.xy), _Layer1BumpScale) * spat.g;


			float4 bottomMaskMap = maskMapLayer0 + maskMapLayer1;
			float4 bottomAlbedo = albedoLayer0 + albedoLayer1;
			float bottomSmoothness = smoothnessLayer0 + smoothnessLayer1;
			float bottomMetallic = metallicLayer0 + metallicLayer1;
			float bottomOcclusion = occlusionLayer0 + occlusionLayer1;
			float3 bottomNormal = normalLayer0 + normalLayer1;

			float4 topMaskMap = tex2D(_TopLayerMaskMap, IN.tc01.xy * _TopLayerAlbedo_ST.xy);
			float4 topAlbedo = tex2D(_TopLayerAlbedo, IN.tc01.xy * _TopLayerAlbedo_ST.xy);
			float topSmoothness = topMaskMap.a;
			float topMetallic = topMaskMap.r;
			float topOcclusion = topMaskMap.g;
			float3 topNormal = UnpackNormalWithScale(tex2D(_TopLayerBumpMap, IN.tc01.xy * _TopLayerAlbedo_ST.xy), _TopLayerBumpScale);

			float4 albedo = 0;
			float smoothness = 0;
			float occlusion = 0;
			float metallic = 0;
			float3 normal = 0;
			float remainingThickness = 1.0;
			float materialThickness = 1;
			float thickness = min(materialThickness, heightSample.y);
			float thicknessFactor = thickness / materialThickness;

			albedo += topAlbedo * thicknessFactor;
			smoothness += topSmoothness * thicknessFactor;
			occlusion += topOcclusion * thicknessFactor;
			metallic += topMetallic * thicknessFactor;
			normal += topNormal * thicknessFactor;
			remainingThickness -= thicknessFactor;

			albedo += bottomAlbedo * remainingThickness ;
			smoothness += bottomSmoothness * remainingThickness;
			occlusion += bottomOcclusion * remainingThickness;
			metallic += bottomMetallic * remainingThickness;
			normal += bottomNormal * remainingThickness;

			o.Albedo = albedo;
			o.Alpha = 1;
			o.Smoothness = smoothness;
			o.Metallic = metallic;
			o.Occlusion = occlusion;
			o.Emission = 0;
			o.Normal = normal;
		}
		ENDCG
		
    }
	CustomEditor "FluidFrenzy.Editor.TerraformTerrainShaderGUI"
}
