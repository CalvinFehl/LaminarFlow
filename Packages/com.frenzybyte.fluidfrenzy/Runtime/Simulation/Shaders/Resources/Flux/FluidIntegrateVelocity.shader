Shader "Hidden/FluidFrenzy/IntegrateVelocity"
{
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

			#pragma multi_compile_local _ ADDITIVE_VELOCITY
			#pragma multi_compile_local _ SECOND_ADDITIVE_VELOCITY
			#pragma multi_compile_local _ FLUID_MULTILAYER
			#pragma multi_compile_local _ OPEN_BORDER
	
			#include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Simulation/Shaders/Resources/FluidSimulationCommon.hlsl"

			CBUFFER_START(WaterSimFlowToVelocity)
			float4 _VelocityBlitScaleBias;
			float4 _VelocityFieldBoundary;
			CBUFFER_END

            struct v2f
            {
                float4 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

			v2f vert(uint vid : SV_VertexID)
			{
				v2f o;
				o.vertex = GetQuadVertexPosition(vid);
				o.vertex.xy = o.vertex.xy * float2(2.0f, -2.0f) + float2(-1.0f, 1.0f); //convert to -1..1
				o.uv.xy = GetQuadTexCoord(vid) * 2 - 1;
				o.uv.xy = (o.uv.xy) * _VelocityBlitScaleBias.xy;
				o.uv.xy = (o.uv.xy * 0.5 + 0.5);
				o.uv.zw = GetQuadTexCoord(vid);
				return o;
			}

			float4 frag(v2f i) : SV_Target
			{
				float4 velocityData = tex2D(_VelocityField, i.uv.xy);
				float2 velocity = float2(0,0);

				float2 prevVelocity = tex2D(_PreviousVelocityField, i.uv.zw).xy * _VelocityDamping;

#if !FLUID_MULTILAYER
				#if ADDITIVE_VELOCITY
					velocity.xy = prevVelocity + velocityData.xy * _VelocityDeltaTime;
				#else
					velocity.xy = velocityData.xy;
				#endif
#else
				float2 additiveVelocity = prevVelocity + velocityData.xy * _VelocityDeltaTime;
				#if ADDITIVE_VELOCITY && SECOND_ADDITIVE_VELOCITY
					velocity.xy = additiveVelocity;
				#elif ADDITIVE_VELOCITY
					velocity.xy = lerp(additiveVelocity, velocityData.xy, velocityData.w);
				#elif SECOND_ADDITIVE_VELOCITY
					velocity.xy = lerp(velocityData.xy, additiveVelocity, velocityData.w);
				#else
					velocity.xy = velocityData.xy;
				#endif
#endif

				velocity = ClampVector(velocity, _VelocityMax.x);
#if !OPEN_BORDER
				if (i.uv.z < _VelocityFieldBoundary.x || i.uv.z >= _VelocityFieldBoundary.z)
					velocity.x = 0;

				if (i.uv.w < _VelocityFieldBoundary.y || i.uv.w >= _VelocityFieldBoundary.w)
					velocity.y = 0;
#endif

				return float4(velocity.xy * velocityData.z,0,0);
            }
            ENDHLSL
        }
    }
}
