Shader "Hidden/FluidFrenzy/TerraForm"
{
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

		Pass
		{
			Name "FluidMix"
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment fragMix
			#pragma target 5.0
			#pragma enable_cbuffer
			#pragma exclude_renderers webgpu
			#pragma multi_compile_local _ _FLUID_UNITY_TERRAIN
			#pragma multi_compile_local _ _FLUID_MIX
			#pragma multi_compile_local _ _FLUID_LIQUIFY
			#pragma multi_compile_local _ _FLUID_CONTACT

			#define SUPPORT_GPUPARTICLES
			#include  "Packages/com.frenzybyte.fluidfrenzy/Runtime/Simulation/Shaders/Resources/TerraForm/FluidTerraformCommon.hlsl"

			ENDHLSL
		}

		Pass
		{
			Name "FluidMixApplyLayer"
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment fragApplyLayer
			#pragma target 5.0
			#pragma exclude_renderers webgpu
			#pragma multi_compile_local _ _FLUID_UNITY_TERRAIN
			#pragma multi_compile_local _ _FLUID_MIX
			#pragma multi_compile_local _ _FLUID_LIQUIFY
			#pragma multi_compile_local _ _FLUID_CONTACT
			
			#include  "Packages/com.frenzybyte.fluidfrenzy/Runtime/Simulation/Shaders/Resources/TerraForm/FluidTerraformCommon.hlsl"

			ENDHLSL
		}
    }

    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

		Pass
		{
			Name "FluidMix"
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment fragMix
			#pragma target 3.0
			#pragma multi_compile_local _ _FLUID_UNITY_TERRAIN
			#pragma multi_compile_local _ _FLUID_MIX
			#pragma multi_compile_local _ _FLUID_LIQUIFY
			#pragma multi_compile_local _ _FLUID_CONTACT
			
			#include  "Packages/com.frenzybyte.fluidfrenzy/Runtime/Simulation/Shaders/Resources/TerraForm/FluidTerraformCommon.hlsl"

			ENDHLSL
		}

		Pass
		{
			Name "FluidMixApplyLayer"
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment fragApplyLayer
			#pragma target 3.0
			#pragma multi_compile_local _ _FLUID_UNITY_TERRAIN
			#pragma multi_compile_local _ _FLUID_MIX
			#pragma multi_compile_local _ _FLUID_LIQUIFY
			#pragma multi_compile_local _ _FLUID_CONTACT

			#include  "Packages/com.frenzybyte.fluidfrenzy/Runtime/Simulation/Shaders/Resources/TerraForm/FluidTerraformCommon.hlsl"

			ENDHLSL
		}
    }
}
