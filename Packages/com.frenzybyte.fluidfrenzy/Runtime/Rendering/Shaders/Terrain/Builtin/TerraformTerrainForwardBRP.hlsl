#ifndef FLUIDFRENZY_TERRAFORM_TERRAIN_FORWARD_BRP_INCLUDED
#define FLUIDFRENZY_TERRAFORM_TERRAIN_FORWARD_BRP_INCLUDED

float4 frag(Varyings i) : SV_Target
{
	#if defined(_FLUIDFRENZY_INSTANCING)
		i.normalWS = CalculateNormalFromHeightField(_HeightField, sampler_HeightField, _HeightField_TexelSize, _TexelWorldSize.xy, i.uv0.xy, 1, 1);
		i.normalWS = mul((float3x3)_ObjectToWorldRotationScale, i.normalWS);
		i.tangentWS.xyz = normalize(cross(i.normalWS.xyz, float3(0, 0, 1)));
		i.tangentWS.w = -unity_WorldTransformParams.w;
	#endif
	half occlusion = 0;
	FragmentCommonData s = FragmentSetup(i, occlusion);

	UnityLight mainLight = MainLight();
	UNITY_LIGHT_ATTENUATION(atten, i, s.posWorld);
	UnityGI gi = FragmentGI(s, occlusion, float4(0,0,0,0), atten, mainLight);

	float4 col = UNITY_BRDF_PBS(s.diffColor, s.specColor, s.oneMinusReflectivity, s.smoothness, s.normalWorld, -s.eyeVec, gi.light, gi.indirect);
	UNITY_APPLY_FOG(i.fogCoord, col);
	return col;
}

float4 fragAdd(Varyings i) : SV_Target
{
	#if defined(_FLUIDFRENZY_INSTANCING)
		i.normalWS = CalculateNormalFromHeightField(_HeightField, sampler_HeightField, _HeightField_TexelSize, _TexelWorldSize.xy, i.uv0.xy, 1, 1);
		i.normalWS = mul((float3x3)_ObjectToWorldRotationScale, i.normalWS);
		i.tangentWS.xyz = normalize(cross(i.normalWS.xyz, float3(0, 0, 1)));
		i.tangentWS.w = -unity_WorldTransformParams.w;
	#endif

	float3 posWorld = i.worldPos.xyz;
	float3 lightDir = _WorldSpaceLightPos0.xyz - posWorld.xyz * _WorldSpaceLightPos0.w;
	#ifndef USING_DIRECTIONAL_LIGHT
		lightDir = NormalizePerVertexNormal(lightDir);
	#endif

	half occlusion = 0;
	FragmentCommonData s = FragmentSetup(i, occlusion);

	UNITY_LIGHT_ATTENUATION(atten, i, s.posWorld)
	UnityLight light = AdditiveLight(lightDir, atten);
	UnityIndirect noIndirect = ZeroIndirect();

	float4 col = UNITY_BRDF_PBS(s.diffColor, s.specColor, s.oneMinusReflectivity, s.smoothness, s.normalWorld, -s.eyeVec, light, noIndirect);
	UNITY_APPLY_FOG_COLOR(i.fogCoord, col.rgb, half4(0,0,0,0)); // fog towards black in additive pass
	return col;
}

#endif // FLUIDFRENZY_TERRAFORM_TERRAIN_FORWARD_BRP_INCLUDED