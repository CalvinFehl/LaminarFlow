Shader "Hidden/FluidFrenzy/Underwater"
{
    Properties 
    { 
        _WaterColor ("Water Color", Color) = (0.882, 0.902, 0.906, 1)
        _AbsorptionDepthScale ("Absorption Scale", Float) = 0.1
        _AbsorptionLimits ("Absorption Limits (Min, Max)", Vector) = (0.0, 1.0, 0, 0)
        
        _MeniscusThickness ("Meniscus Thickness", Float) = 50.0
        _MeniscusBlur ("Meniscus Blur", Float) = 5.0
        _MeniscusDarkness ("Meniscus Darkness", Float) = 0.5

        _ScatterColor ("Scattering Color", Color) = (0.0784, 0.3255, 0.5098, 1)
        _ScatterAmbient ("Scattering Ambient", Range(0, 1)) = 0.5
        _ScatterIntensity ("Scattering Intensity", Float) = 1.0
        _ScatterLightIntensity ("Scatter Light Intensity", Float) = 1.0
    }

    // =========================================================================
    // HDRP SubShader
    // =========================================================================
    SubShader
    {
        PackageRequirements { "com.unity.render-pipelines.high-definition" }
        
        Tags{"RenderPipeline" = "HDRenderPipeline" "RenderType" = "Transparent"}

        // Pass 0: Fluid Mask
        Pass
        {
            Name "FluidMask"
            Cull Off ZWrite On ZTest LEqual
            
            HLSLPROGRAM
            #pragma target 4.5
            #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch
            
            #pragma vertex vertMask
            #pragma fragment fragMask
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _FLUIDFRENZY_INSTANCING
            #pragma multi_compile_local_vertex _ _FLUID_UNITY_TERRAIN

            #define FLUID_PIPELINE_HDRP
            #include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Rendering/Shaders/Library/FluidRenderingPipeline.hlsl"
            #include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Rendering/Shaders/Library/FluidRenderingCommon.hlsl"
            
            struct Attributes
            {
                float4 vertex : POSITION;
                float2 fluidUV : TEXCOORD0;
                float2 textureUV : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                FLUID_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f {
                float4 pos : SV_POSITION;
                float4 screenPos : TEXCOORD0;
                float2 uv : TEXCOORD1;      
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f vertMask(Attributes v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                FluidData fluidData;
                SampleFluidSimulationData(v.vertex, v.fluidUV, v.textureUV, FLUID_GET_INSTANCE_ID(v), fluidData);
                
                float3 worldPos = mul(UNITY_MATRIX_M, float4(fluidData.positionOS, 1)).xyz;
                
                o.pos = TransformWorldToHClip(worldPos);
                o.screenPos = FluidComputeScreenPos(o.pos); 
                o.uv = fluidData.uv;
                return o;
            }

            half4 fragMask(v2f i, bool isFrontFace : SV_IsFrontFace) : SV_Target { return isFrontFace ? 0.25 : 0.75; }
            ENDHLSL
        }

        // Pass 1: Volume Fallback
        Pass 
        {
            Name "VolumeFallback"
            ZTest Always ZWrite Off Cull Off
            
            HLSLPROGRAM
            #pragma target 4.5
            #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch
            #pragma vertex vertSimple
            #pragma fragment fragVolume
            #define FLUID_PIPELINE_HDRP
            #include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Rendering/Shaders/Library/UnderwaterEffectPasses.hlsl"
            ENDHLSL
        }

        // Pass 2: Meniscus Blur
        Pass 
        {
            Name "MeniscusBlur"
            ZTest Always ZWrite Off Cull Off
            
            HLSLPROGRAM
            #pragma target 4.5
            #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch
            #pragma vertex vertSimple
            #pragma fragment fragBlur
            #define FLUID_PIPELINE_HDRP
            #include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Rendering/Shaders/Library/UnderwaterEffectPasses.hlsl"
            ENDHLSL
        }

        // Pass 3: Underwater Effect
        Pass 
        {
            Name "UnderwaterEffect"
            ZTest Always ZWrite Off Cull Off
            Blend One Zero
            
            HLSLPROGRAM
            #pragma target 4.5
            #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch
            #pragma vertex vertSimple
            #pragma fragment fragEffect
            #define FLUID_PIPELINE_HDRP
            #include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Rendering/Shaders/Library/UnderwaterEffectPasses.hlsl"
            ENDHLSL
        }

        // Pass 4: Debug
        Pass 
        {
            Name "DebugOverlay"
            ZTest Always ZWrite Off Cull Off
            Blend SrcAlpha OneMinusSrcAlpha
            
            HLSLPROGRAM
            #pragma target 4.5
            #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch
            #pragma vertex vertSimple
            #pragma fragment fragDebug
            #define FLUID_PIPELINE_HDRP
            #include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Rendering/Shaders/Library/UnderwaterEffectPasses.hlsl"
            ENDHLSL
        }
    }

    // =========================================================================
    // URP SubShader
    // =========================================================================
    SubShader
    {
        PackageRequirements { "com.unity.render-pipelines.universal" }
        Tags{"RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True"}
        
        Pass
        {
            Name "FluidMask"
            Cull Off ZWrite On ZTest LEqual
            
            HLSLPROGRAM
            #pragma vertex vertMask
            #pragma fragment fragMask
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _FLUIDFRENZY_INSTANCING
            #pragma multi_compile_local_vertex _ _FLUID_UNITY_TERRAIN

            #define FLUID_PIPELINE_URP
            #include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Rendering/Shaders/Library/FluidRenderingPipeline.hlsl"
            #include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Rendering/Shaders/Library/FluidRenderingCommon.hlsl"
            
            struct Attributes
            {
                float4 vertex : POSITION;
                float2 fluidUV : TEXCOORD0;
                float2 textureUV : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                FLUID_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f {
                float4 pos : SV_POSITION;
                float4 screenPos : TEXCOORD0;
                float2 uv : TEXCOORD1;      
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f vertMask(Attributes v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                FluidData fluidData;
                SampleFluidSimulationData(v.vertex, v.fluidUV, v.textureUV, FLUID_GET_INSTANCE_ID(v), fluidData);
                
                float3 worldPos = mul(UNITY_MATRIX_M, float4(fluidData.positionOS, 1)).xyz;
                
                o.pos = TransformWorldToHClip(worldPos);
                o.screenPos = FluidComputeScreenPos(o.pos);
                o.uv = fluidData.uv;
                return o;
            }

            half4 fragMask(v2f i, bool isFrontFace : SV_IsFrontFace) : SV_Target { return isFrontFace ? 0.25 : 0.75; }
            ENDHLSL
        }

        Pass 
        { 
            Name "VolumeFallback" 
            ZTest Always ZWrite Off Cull Off 
            
            HLSLPROGRAM 
            #pragma vertex vertSimple 
            #pragma fragment fragVolume 
            #define FLUID_PIPELINE_URP 
            #include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Rendering/Shaders/Library/UnderwaterEffectPasses.hlsl" 
            ENDHLSL 
        }

        Pass 
        { 
            Name "MeniscusBlur" 
            ZTest Always ZWrite Off Cull Off 
            
            HLSLPROGRAM 
            #pragma vertex vertSimple 
            #pragma fragment fragBlur 
            #define FLUID_PIPELINE_URP 
            #include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Rendering/Shaders/Library/UnderwaterEffectPasses.hlsl" 
            ENDHLSL 
        }

        Pass 
        { 
            Name "UnderwaterEffect" 
            ZTest Always ZWrite Off Cull Off 
            Blend One Zero 
            
            HLSLPROGRAM 
            #pragma vertex vertSimple 
            #pragma fragment fragEffect 
            #define FLUID_PIPELINE_URP 
            #include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Rendering/Shaders/Library/UnderwaterEffectPasses.hlsl" 
            ENDHLSL 
        }

        Pass 
        { 
            Name "DebugOverlay" 
            ZTest Always ZWrite Off Cull Off 
            Blend SrcAlpha OneMinusSrcAlpha 
            
            HLSLPROGRAM 
            #pragma vertex vertSimple 
            #pragma fragment fragDebug 
            #define FLUID_PIPELINE_URP 
            #include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Rendering/Shaders/Library/UnderwaterEffectPasses.hlsl" 
            ENDHLSL 
        }
    }

    // =========================================================================
    // BiRP SubShader
    // =========================================================================
    SubShader
    {
        Tags { "RenderType"="Transparent" }
        
        Pass
        {
            Name "FluidMask"
            Cull Off ZWrite On ZTest LEqual
            
            HLSLPROGRAM
            #pragma vertex vertMask
            #pragma fragment fragMask
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _FLUIDFRENZY_INSTANCING
            #pragma multi_compile_local_vertex _ _FLUID_UNITY_TERRAIN

            #include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Rendering/Shaders/Library/FluidRenderingPipeline.hlsl"
            #include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Rendering/Shaders/Library/FluidRenderingCommon.hlsl"

            struct Attributes {
                float4 vertex : POSITION;
                float2 fluidUV : TEXCOORD0;
                float2 textureUV : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                FLUID_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f {
                float4 pos : SV_POSITION;
                float4 screenPos : TEXCOORD0;
                float2 uv : TEXCOORD1;      
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f vertMask(Attributes v) {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                FluidData fluidData;
                SampleFluidSimulationData(v.vertex, v.fluidUV, v.textureUV, FLUID_GET_INSTANCE_ID(v), fluidData);
                
                float3 worldPos = mul(unity_ObjectToWorld, float4(fluidData.positionOS, 1)).xyz;
                
                o.pos = UnityWorldToClipPos(worldPos);
                o.screenPos = FluidComputeScreenPos(o.pos);
                o.uv = fluidData.uv;
                return o;
            }

            fixed4 fragMask(v2f i, bool isFrontFace : SV_IsFrontFace) : SV_Target { return isFrontFace ? 0.25 : 0.75; }
            ENDHLSL
        }

        Pass 
        { 
            Name "VolumeFallback" 
            ZTest Always ZWrite Off Cull Off 
            
            HLSLPROGRAM 
            #pragma vertex vertSimple 
            #pragma fragment fragVolume 
            #include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Rendering/Shaders/Library/UnderwaterEffectPasses.hlsl" 
            ENDHLSL 
        }

        Pass 
        { 
            Name "MeniscusBlur" 
            ZTest Always ZWrite Off Cull Off 
            
            HLSLPROGRAM 
            #pragma vertex vertSimple 
            #pragma fragment fragBlur 
            #include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Rendering/Shaders/Library/UnderwaterEffectPasses.hlsl" 
            ENDHLSL 
        }

        Pass 
        { 
            Name "UnderwaterEffect" 
            ZTest Always ZWrite Off Cull Off 
            Blend One Zero 
            
            HLSLPROGRAM 
            #pragma vertex vertSimple 
            #pragma fragment fragEffect 
            #include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Rendering/Shaders/Library/UnderwaterEffectPasses.hlsl" 
            ENDHLSL 
        }

        Pass 
        { 
            Name "DebugOverlay" 
            ZTest Always ZWrite Off Cull Off 
            Blend SrcAlpha OneMinusSrcAlpha 
            
            HLSLPROGRAM 
            #pragma vertex vertSimple 
            #pragma fragment fragDebug 
            #include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Rendering/Shaders/Library/UnderwaterEffectPasses.hlsl" 
            ENDHLSL 
        }
    }
}