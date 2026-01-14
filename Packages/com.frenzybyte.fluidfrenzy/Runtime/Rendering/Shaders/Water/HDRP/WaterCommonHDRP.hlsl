#ifndef FLUIDFRENZY_WATER_COMMON_HDRP_INCLUDED
#define FLUIDFRENZY_WATER_COMMON_HDRP_INCLUDED


struct SurfaceDescriptionInputs
{
	float3 Albedo;
	float3 SpecularColor;
	float Smoothness;
	float3 TangentSpaceNormal;
};
   
// Graph Pixel
struct SurfaceDescription
{
	float3 BaseColor;
	float3 Emission;
	float Alpha;
	float3 BentNormal;
	float Smoothness;
	float Occlusion;
	float3 NormalTS;
	float Metallic;
	float3 SpecularColor;
	float4 VTPackedFeedback;
};
        
SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
{
	SurfaceDescription surface = (SurfaceDescription)0;
	surface.BaseColor = IN.Albedo;
	surface.Emission = float3(0, 0, 0);
	surface.Alpha = float(1);
	surface.BentNormal = IN.TangentSpaceNormal;
	surface.Smoothness = IN.Smoothness;
	surface.Occlusion = float(1);
	surface.NormalTS = IN.TangentSpaceNormal;
	surface.Metallic = float(1);
	surface.SpecularColor = IN.SpecularColor;
	{
		surface.VTPackedFeedback = float4(1.0f,1.0f,1.0f,1.0f);
	}
	return surface;
}


SurfaceDescriptionInputs FragInputsToSurfaceDescriptionInputs(FragInputs input, float3 viewWS, in FluidInputData fluidInput, in WaterSurfaceData waterSurfaceData, in WaterInputData waterInput)
{
	SurfaceDescriptionInputs output;
	ZERO_INITIALIZE(SurfaceDescriptionInputs, output);

	half foamAmount = WaterFoamAmount(waterInput.foamMask, waterSurfaceData.foamAlbedo, _FoamColor.a, waterInput.screenspaceParticles);

	float3 waveTS = waterSurfaceData.waveNormalTS;
	float3 foamTS = waterSurfaceData.foamNormalTS;

	// Foam Exception: Force foam to point Skyward when underwater
	if (!input.isFrontFace)
	{
		foamTS.z *= -1.0;
	}

	output.TangentSpaceNormal = lerp(waveTS, foamTS, foamAmount);
	output.Albedo = lerp(0, _FoamColor.rgb, foamAmount);
	output.SpecularColor = lerp(0.04f, _FoamColor.rgb * 0.01f, foamAmount);;
	output.Smoothness = lerp(0.95f, 0.1f, foamAmount);
        
	return output;
}
        
// --------------------------------------------------
// Build Surface Data (Specific Material)
        
void ApplyDecalToSurfaceDataNoNormal(DecalSurfaceData decalSurfaceData, inout SurfaceData surfaceData);
        
void ApplyDecalAndGetNormal(FragInputs fragInputs, PositionInputs posInput, SurfaceDescription surfaceDescription,
	inout SurfaceData surfaceData)
{
	float3 doubleSidedConstants = GetDoubleSidedConstants();
        
#ifdef DECAL_NORMAL_BLENDING
	// SG nodes don't ouptut surface gradients, so if decals require surf grad blending, we have to convert
	// the normal to gradient before applying the decal. We then have to resolve the gradient back to world space
	float3 normalTS;
        
	normalTS = SurfaceGradientFromTangentSpaceNormalAndFromTBN(surfaceDescription.NormalTS,
	fragInputs.tangentToWorld[0], fragInputs.tangentToWorld[1]);
        
        
	#if HAVE_DECALS
	if (_EnableDecals)
	{
		float alpha = 1.0;
		alpha = surfaceDescription.Alpha;
        
		DecalSurfaceData decalSurfaceData = GetDecalSurfaceData(posInput, fragInputs, alpha);
		ApplyDecalToSurfaceNormal(decalSurfaceData, fragInputs.tangentToWorld[2], normalTS);
		ApplyDecalToSurfaceDataNoNormal(decalSurfaceData, surfaceData);
	}
	#endif
        
	GetNormalWS_SG(fragInputs, normalTS, surfaceData.normalWS, doubleSidedConstants);
#else
	// normal delivered to master node
	GetNormalWS(fragInputs, surfaceDescription.NormalTS, surfaceData.normalWS, doubleSidedConstants);
        
	#if HAVE_DECALS
	if (_EnableDecals)
	{
		float alpha = 1.0;
		alpha = surfaceDescription.Alpha;
        
		// Both uses and modifies 'surfaceData.normalWS'.
		DecalSurfaceData decalSurfaceData = GetDecalSurfaceData(posInput, fragInputs, alpha);
		ApplyDecalToSurfaceNormal(decalSurfaceData, surfaceData.normalWS.xyz);
		ApplyDecalToSurfaceDataNoNormal(decalSurfaceData, surfaceData);
	}
	#endif
#endif
}
void BuildSurfaceData(FragInputs fragInputs, inout SurfaceDescription surfaceDescription, float3 V, PositionInputs posInput, out SurfaceData surfaceData, out float3 bentNormalWS)
{
	ZERO_INITIALIZE(SurfaceData, surfaceData);
        
	surfaceData.specularOcclusion = 1.0;
	surfaceData.thickness = 0.0;
        
	surfaceData.baseColor =                 surfaceDescription.BaseColor;
	surfaceData.perceptualSmoothness =      surfaceDescription.Smoothness;
	surfaceData.ambientOcclusion =          surfaceDescription.Occlusion;
	surfaceData.metallic =                  surfaceDescription.Metallic;
	surfaceData.specularColor =                  surfaceDescription.SpecularColor;

	surfaceData.ior = 1.333;
    
	surfaceData.transmittanceColor = float3(1.0, 1.0, 1.0);
	surfaceData.atDistance = 1.0;
	surfaceData.transmittanceMask = 0.0;
        
	// These static material feature allow compile time optimization
	surfaceData.materialFeatures = MATERIALFEATUREFLAGS_LIT_STANDARD;
        
	#ifdef _MATERIAL_FEATURE_ANISOTROPY
		surfaceData.materialFeatures |= MATERIALFEATUREFLAGS_LIT_ANISOTROPY;
		// Initialize the normal to something non-zero to avoid a div-zero warning for anisotropy.
		surfaceData.normalWS = float3(0, 1, 0);
	#endif

        
	#ifdef _MATERIAL_FEATURE_SPECULAR_COLOR
		surfaceData.materialFeatures |= MATERIALFEATUREFLAGS_LIT_SPECULAR_COLOR;
	#endif
        
	#if defined (_MATERIAL_FEATURE_SPECULAR_COLOR) && defined (_ENERGY_CONSERVING_SPECULAR)
		// Require to have setup baseColor
		// Reproduce the energy conservation done in legacy Unity. Not ideal but better for compatibility and users can unchek it
		surfaceData.baseColor *= (1.0 - Max3(surfaceData.specularColor.r, surfaceData.specularColor.g, surfaceData.specularColor.b));
	#endif
        
	float3 doubleSidedConstants = GetDoubleSidedConstants();
        
	ApplyDecalAndGetNormal(fragInputs, posInput, surfaceDescription, surfaceData);
        
	surfaceData.geomNormalWS = fragInputs.tangentToWorld[2];
        
	surfaceData.tangentWS = normalize(fragInputs.tangentToWorld[0].xyz);    // The tangent is not normalize in tangentToWorld for mikkt. TODO: Check if it expected that we normalize with Morten. Tag: SURFACE_GRADIENT
        
	bentNormalWS = surfaceData.normalWS;
        
	surfaceData.tangentWS = Orthonormalize(surfaceData.tangentWS, surfaceData.normalWS);
        
	// By default we use the ambient occlusion with Tri-ace trick (apply outside) for specular occlusion.
	// If user provide bent normal then we process a better term
	#if defined(_SPECULAR_OCCLUSION_CUSTOM)
		// Just use the value passed through via the slot (not active otherwise)
	#elif defined(_SPECULAR_OCCLUSION_FROM_AO_BENT_NORMAL)
		// If we have bent normal and ambient occlusion, process a specular occlusion
		surfaceData.specularOcclusion = GetSpecularOcclusionFromBentAO(V, bentNormalWS, surfaceData.normalWS, surfaceData.ambientOcclusion, PerceptualSmoothnessToPerceptualRoughness(surfaceData.perceptualSmoothness));
	#elif defined(_AMBIENT_OCCLUSION) && defined(_SPECULAR_OCCLUSION_FROM_AO)
		surfaceData.specularOcclusion = GetSpecularOcclusionFromAmbientOcclusion(ClampNdotV(dot(surfaceData.normalWS, V)), surfaceData.ambientOcclusion, PerceptualSmoothnessToRoughness(surfaceData.perceptualSmoothness));
	#endif
        
	#if defined(_ENABLE_GEOMETRIC_SPECULAR_AA) && !defined(SHADER_STAGE_RAY_TRACING)
		surfaceData.perceptualSmoothness = GeometricNormalFiltering(surfaceData.perceptualSmoothness, fragInputs.tangentToWorld[2], surfaceDescription.SpecularAAScreenSpaceVariance, surfaceDescription.SpecularAAThreshold);
	#endif
}
        
// --------------------------------------------------
// Get Surface And BuiltinData
        
void GetSurfaceAndBuiltinData(FragInputs fragInputs, float3 V, inout PositionInputs posInput, out SurfaceData surfaceData, out BuiltinData builtinData, in FluidInputData fluidInput, in WaterSurfaceData waterSurfaceData, in WaterInputData waterInput )
{

        
	SurfaceDescriptionInputs surfaceDescriptionInputs = FragInputsToSurfaceDescriptionInputs(fragInputs, V, fluidInput, waterSurfaceData, waterInput);
	SurfaceDescription surfaceDescription = SurfaceDescriptionFunction(surfaceDescriptionInputs);
        
	float3 bentNormalWS;
	BuildSurfaceData(fragInputs, surfaceDescription, V, posInput, surfaceData, bentNormalWS);

	float4 lightmapTexCoord1 = float4(0,0,0,0);
	float4 lightmapTexCoord2 = float4(0,0,0,0);
	float alpha = surfaceDescription.Alpha;
        
	InitBuiltinData(posInput, alpha, bentNormalWS, -fragInputs.tangentToWorld[2], lightmapTexCoord1, lightmapTexCoord2, builtinData);

	builtinData.renderingLayers = GetMeshRenderingLayerMask(); 
	builtinData.emissiveColor = surfaceDescription.Emission;
        
	PostInitBuiltinData(V, posInput, surfaceData, builtinData);
}

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Debug/DebugDisplayMaterial.hlsl"

#if defined(_TRANSPARENT_REFRACTIVE_SORT) || defined(_ENABLE_FOG_ON_TRANSPARENT)
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Water/Shaders/UnderWaterUtilities.hlsl"
#endif

float RealLinearEyeDepth(float rawDepth, float4 zBufferParam)
{
	float persp = LinearEyeDepth(rawDepth, zBufferParam);
	float ortho = (_ProjectionParams.z-_ProjectionParams.y)*(1-rawDepth)+_ProjectionParams.y;
	return lerp(persp,ortho,unity_OrthoParams.w);
}


void InitializeFluidInputData(FragInputs input, out FluidInputData outFluidInputData)
{
	outFluidInputData = (FluidInputData)(0);
	outFluidInputData.flowUV = input.texCoord1;
	outFluidInputData.fluidUV = input.texCoord0;
	outFluidInputData.velocity = SampleFluidVelocity(outFluidInputData.fluidUV.xy);
	outFluidInputData.fluidMask = SampleFluidNormal(outFluidInputData.fluidUV.xy).w;
}

void InitializeWaterInputData(inout FragInputs input, in FluidInputData fluidInput, in WaterSurfaceData waterSurfaceData, out WaterInputData outWaterInput)
{
	outWaterInput = (WaterInputData)(0);

	outWaterInput.frontFaceMask = input.isFrontFace ? 1 : -1;
	input.tangentToWorld[2] = input.tangentToWorld[2] * outWaterInput.frontFaceMask;
	outWaterInput.positionWS = input.positionRWS.xyz;

	#if UNITY_UV_STARTS_AT_TOP
	float2 pixelPosition = float2(input.positionSS.x, (_ProjectionParams.x < 0) ? (_ScreenParams.y - input.positionSS.y) : input.positionSS.y);
	#else
	float2 pixelPosition = float2(input.positionSS.x, (_ProjectionParams.x > 0) ? (_ScreenParams.y - input.positionSS.y) : input.positionSS.y);
	#endif

	outWaterInput.normalizedScreenSpaceUV = pixelPosition.xy / _ScreenParams.xy;
	outWaterInput.normalizedScreenSpaceUV.y = 1.0f - outWaterInput.normalizedScreenSpaceUV.y;

#if defined(_SCREENSPACE_REFRACTION_ON) || defined(_SCREENSPACE_REFRACTION_ALPHA) || defined(_SCREENSPACE_REFRACTION_ABSORB)
	outWaterInput.pixelZ = RealLinearEyeDepth(input.positionSS.z, _ZBufferParams);
	outWaterInput.sceneDepth = SampleCameraDepth(outWaterInput.normalizedScreenSpaceUV);//SAMPLE_SCREENSPACE_TEXTURE(_CameraDepthTexture, outWaterInput.normalizedScreenSpaceUV).x;
	outWaterInput.sceneZ = RealLinearEyeDepth(outWaterInput.sceneDepth, _ZBufferParams);
	outWaterInput.waterDepth = outWaterInput.sceneZ - outWaterInput.pixelZ;
#endif

	outWaterInput.normalWS = normalize(input.tangentToWorld[2]);
#if defined(_NORMALMAP) || defined(_FOAM_NORMALMAP)
	outWaterInput.tangentWS = normalize(input.tangentToWorld[0].xyz);
	outWaterInput.bitangentWS = input.tangentToWorld[1];
#endif

	outWaterInput.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionRWS);;//normalize(_WorldSpaceCameraPos.xyz - outWaterInput.positionWS);

	outWaterInput.shadow = 1;

#if defined(_NORMALMAP)
	outWaterInput.waveNormalWS = normalize(waterSurfaceData.waveNormalTS.x * outWaterInput.tangentWS +
		waterSurfaceData.waveNormalTS.y * outWaterInput.bitangentWS +
		waterSurfaceData.waveNormalTS.z * outWaterInput.normalWS);
#else
	outWaterInput.waveNormalWS = outWaterInput.normalWS;
#endif

#if defined(_FOAM_NORMALMAP)
	outWaterInput.foamNormalWS = normalize(waterSurfaceData.foamNormalTS.x * outWaterInput.tangentWS +
		waterSurfaceData.foamNormalTS.y * outWaterInput.bitangentWS +
		waterSurfaceData.foamNormalTS.z * outWaterInput.normalWS);
#else
	outWaterInput.foamNormalWS = outWaterInput.normalWS;
#endif

	outWaterInput.foamNormalWS = outWaterInput.foamNormalWS * outWaterInput.frontFaceMask;
	outWaterInput.fade = smoothstep(0, _FadeHeight, max(0, (fluidInput.fluidMask * FluidLayerToMask(_Layer)) - _FluidClipHeight));

	//Foam
#if defined(_FOAMMASK_ON)
	half foamField = SampleFluidFoamField(fluidInput.fluidUV.zw);
	outWaterInput.foamMask = smoothstep(_FoamVisibility.x, _FoamVisibility.y, foamField);
#else
	outWaterInput.foamMask = 0;
#endif

#if defined(_SCREENSPACE_REFRACTION_ON) || defined(_SCREENSPACE_REFRACTION_ABSORB)
	half2 refractOffset = outWaterInput.waveNormalWS.xz * _RefractionDistortion * min(1, outWaterInput.waterDepth);
	outWaterInput.refractedSceneDepth = SampleCameraDepth(outWaterInput.normalizedScreenSpaceUV + refractOffset).x;
	outWaterInput.refractedSceneZ = RealLinearEyeDepth(outWaterInput.refractedSceneDepth, _ZBufferParams);
	outWaterInput.refractionColor = float4(SampleCameraColor(outWaterInput.normalizedScreenSpaceUV + refractOffset), 1);
	outWaterInput.refractedDistance = max(outWaterInput.refractedSceneZ - outWaterInput.pixelZ, outWaterInput.waterDepth);
#else
	outWaterInput.refractedDistance = outWaterInput.waterDepth;
#endif

#if _SCREENSPACE_FOAMMASK_ON
	outWaterInput.screenspaceParticles = SAMPLE_TEXTURE2D(_FluidScreenSpaceParticles, linear_clamp_sampler, outWaterInput.normalizedScreenSpaceUV);
#endif // _SCREENSPACE_FOAMMASK_ON
}


#endif // FLUIDFRENZY_WATER_COMMON_HDRP_INCLUDED