#ifndef FLUIDFRENZY_TERRAFORM_TERRAIN_DEFERRED_BRP_INCLUDED
#define FLUIDFRENZY_TERRAFORM_TERRAIN_DEFERRED_BRP_INCLUDED

void fragDeferred(
	Varyings i,
	out half4 outGBuffer0 : SV_Target0,
	out half4 outGBuffer1 : SV_Target1,
	out half4 outGBuffer2 : SV_Target2,
	out half4 outEmission : SV_Target3          // RT3: emission (rgb), --unused-- (a)
#if defined(SHADOWS_SHADOWMASK) && (UNITY_ALLOWED_MRT_COUNT > 4)
	, out half4 outShadowMask : SV_Target4       // RT4: shadowmask (rgba)
#endif
)
{
#if (SHADER_TARGET < 30)
	outGBuffer0 = 1;
	outGBuffer1 = 1;
	outGBuffer2 = 0;
	outEmission = 0;
#if defined(SHADOWS_SHADOWMASK) && (UNITY_ALLOWED_MRT_COUNT > 4)
	outShadowMask = 1;
#endif
	return;
#endif

	// no analytic lights in this pass
	UnityLight dummyLight = DummyLight();
	half atten = 1;

	half occlusion = 0;
	FragmentCommonData s = FragmentSetup(i, occlusion);
#if UNITY_ENABLE_REFLECTION_BUFFERS
	bool sampleReflectionsInDeferred = false;
#else
	bool sampleReflectionsInDeferred = true;
#endif

	UnityGI gi = FragmentGI(s, occlusion, float4(0, 0, 0, 0), atten, dummyLight, sampleReflectionsInDeferred);

	half3 emissiveColor = UNITY_BRDF_PBS(s.diffColor, s.specColor, s.oneMinusReflectivity, s.smoothness, s.normalWorld, -s.eyeVec, gi.light, gi.indirect).rgb;

#ifdef _EMISSION
	emissiveColor += Emission(i.tex.xy);
#endif

#ifndef UNITY_HDR_ON
	emissiveColor.rgb = exp2(-emissiveColor.rgb);
#endif

	UnityStandardData data;
	data.diffuseColor = s.diffColor;
	data.occlusion = occlusion;
	data.specularColor = s.specColor;
	data.smoothness = s.smoothness;
	data.normalWorld = s.normalWorld;

	UnityStandardDataToGbuffer(data, outGBuffer0, outGBuffer1, outGBuffer2);

	// Emissive lighting buffer
	outEmission = half4(emissiveColor, 1);

	// Baked direct lighting occlusion if any
#if defined(SHADOWS_SHADOWMASK) && (UNITY_ALLOWED_MRT_COUNT > 4)
	outShadowMask = UnityGetRawBakedOcclusions(i.ambientOrLightmapUV.xy, IN_WORLDPOS(i));
#endif
}

#endif // FLUIDFRENZY_TERRAFORM_TERRAIN_DEFERRED_BRP_INCLUDED