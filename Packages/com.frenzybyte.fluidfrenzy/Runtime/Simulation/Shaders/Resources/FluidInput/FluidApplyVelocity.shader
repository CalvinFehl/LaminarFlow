Shader "Hidden/FluidFrenzy/ApplyVelocity"
{
    SubShader
    {
		HLSLINCLUDE
		#include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Simulation/Shaders/Resources/FluidSimulationCommon.hlsl"

		struct appdata
		{
			float4 vertex : POSITION;
			float2 uv : TEXCOORD0;
		};

		struct v2f
		{
			float2 uv : TEXCOORD0;
			float4 vertex : SV_POSITION;
		};


		v2f vert(uint vid : SV_VertexID)
		{
			v2f o;
			o.vertex = GetQuadVertexPosition(vid);
			o.vertex.xy = o.vertex.xy * _BlitScaleBiasRt.xy + _BlitScaleBiasRt.zw - _BlitScaleBiasRt.xy * 0.5f;
		#if UNITY_UV_STARTS_AT_TOP
			o.vertex.xy = o.vertex.xy * float2(2.0f, -2.0f) + float2(-1.0f, 1.0f); //convert to -1..1
			o.uv.xy = GetQuadTexCoord(vid) * 2 - 1;
		#else
			o.vertex.xy = o.vertex.xy * float2(2.0f, 2.0f) + float2(-1.0f, -1.0f); //convert to -1..1
			o.uv.xy = GetQuadTexCoord(vid) * 2 - 1;
			o.uv.y = -o.uv.y;
		#endif
			return o;
		}

		
		v2f vert_texture(uint vid : SV_VertexID)
		{
			v2f o;
			o.vertex = GetQuadVertexPosition(vid);
			o.vertex.xy = o.vertex.xy * _BlitScaleBiasRt.xy + _BlitScaleBiasRt.zw - _BlitScaleBiasRt.xy * 0.5f;
		#if UNITY_UV_STARTS_AT_TOP
			o.vertex.xy = o.vertex.xy * float2(2.0f, -2.0f) + float2(-1.0f, 1.0f); //convert to -1..1
			o.uv.xy = GetQuadTexCoord(vid);
		#else
			o.vertex.xy = o.vertex.xy * float2(2.0f, 2.0f) + float2(-1.0f, -1.0f); //convert to -1..1
			o.uv.xy = GetQuadTexCoord(vid);
			o.uv.y = -o.uv.y;
		#endif
			return o;
		}

		float2 _VelocityDir;
		float _IncreaseStrength;
		float _IncreaseStrengthOuter;
		float _IncreaseExponent;

		float4 frag(v2f i) : SV_Target
		{
			float4 oldVelocity = 0;

			float2 pos = i.uv;
			float2 pxDist = pos;
			float dist = length(pxDist);

			pxDist = normalize(pxDist);
			float scale = max(0, 1 - dist);
			float MdD = saturate(dot(normalize(_VelocityDir.xy), pxDist));
			scale = (pow(saturate(scale), _IncreaseExponent)) * _IncreaseStrength;

			clip(1 - dist);

			oldVelocity.xy += _VelocityDir * scale* MdD;
			oldVelocity.zw += _VelocityDir * scale* MdD;

			return oldVelocity;
		}

		float4 frag_set_direction(v2f i) : SV_Target
		{
			float4 oldVelocity = 0;

			float2 pos = i.uv;
			float dist = length(pos);

			clip(1 - dist);

			oldVelocity.xy += _VelocityDir * _IncreaseStrength;
			oldVelocity.zw += _VelocityDir * _IncreaseStrength;

			return oldVelocity;
		}

		float4 fragWhirlpool(v2f i) : SV_Target
		{
			float4 oldVelocity = 0;
			float dist = length(i.uv);

			float scale = max(0, 1 - dist);
			if(dist < 1)
			{
				float2 x = _IncreaseStrength * smoothstep(0.0, 0.2, scale);
				float2 y = _IncreaseStrengthOuter * smoothstep(0.0, 0.2, scale);
				float2 tanVel = float2(i.uv.yx) * float2(-1,1);
				oldVelocity.xy += lerp(normalize(tanVel) * x, normalize(-i.uv.xy) * y, 1- scale) ;
			}

			return float4(oldVelocity.xy,0, scale * 0.5f);
		}

		float4 fragWhirlpoolFlowSim(v2f i) : SV_Target
		{
			float4 oldVelocity = 0;
			float dist = length(i.uv);

			float scale = max(0, 1 - dist);
			if(dist < 1)
			{
				float2 x = _IncreaseStrength * smoothstep(0.0, 0.2, scale);
				float2 y = _IncreaseStrengthOuter * smoothstep(0.0, 0.2, scale);
				float2 tanVel = float2(i.uv.yx) * float2(-1,1);
				oldVelocity.xy += lerp((tanVel) * x, (i.uv.xy) * y, scale) ;
			}

			clip(1 - dist);


			return float4(oldVelocity.xy,0, 0);
		}

		ENDHLSL

        Pass
        {
			Name "ApplyVelocityDirection"
			Cull Off ZWrite Off ZTest Always
			Blend One One, One One
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            ENDHLSL
        }

		Pass
        {
			Name "SetVelocityDirection"
			Cull Off ZWrite Off ZTest Always
			Blend One Zero, One Zero
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag_set_direction

            ENDHLSL
        }

		Pass
		{
			Name "ApplyVelocityVortexBlend"
			Cull Off ZWrite Off ZTest Always
			Blend SrcAlpha OneMinusSrcAlpha, One Zero
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment fragWhirlpool
			ENDHLSL
		}

		Pass
		{
			Name "ApplyVelocityVortexAdd"
			Cull Off ZWrite Off ZTest Always
			Blend One One, One Zero
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment fragWhirlpool
			ENDHLSL
		}

		Pass
		{
			Name "ApplyVelocityVortexFlowSim"
			Cull Off ZWrite Off ZTest Always
			Blend One One, One Zero
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment fragWhirlpoolFlowSim

			ENDHLSL
		}		
		
		Pass
		{
			Name "SetVelocityVortexFlowSim"
			Cull Off ZWrite Off ZTest Always
			Blend One Zero, One Zero
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment fragWhirlpoolFlowSim

			ENDHLSL
		}

		Pass
		{
			Name "ApplyVelocityTexture"
			Cull Off ZWrite Off ZTest Always
			Blend One One, One One
			HLSLPROGRAM
			#pragma vertex vert_texture
			#pragma fragment frag_texture

			sampler2D _MainTex;
			
			float4 frag_texture(v2f i) : SV_Target
			{
				float4 force = tex2D(_MainTex, i.uv);
				return float4(force.xy * _IncreaseStrength,0,0);
			}
			ENDHLSL
		}

		Pass
		{
			Name "ApplyVelocityTextureRemapped"
			Cull Off ZWrite Off ZTest Always
			Blend One One, One One
			HLSLPROGRAM
			#pragma vertex vert_texture
			#pragma fragment frag_texture

			sampler2D _MainTex;
			
			float4 frag_texture(v2f i) : SV_Target
			{
				float4 force = tex2D(_MainTex, i.uv)  * 2 - 1;
				return float4(force.xy * _IncreaseStrength,0,0);
			}
			ENDHLSL
		}

		Pass
		{
			Name "SetVelocityTextureRemapped"
			Cull Off ZWrite Off ZTest Always
			Blend One Zero, One Zero
			HLSLPROGRAM
			#pragma vertex vert_texture
			#pragma fragment frag_texture

			sampler2D _MainTex;
			
			float4 frag_texture(v2f i) : SV_Target
			{
				float4 force = tex2D(_MainTex, i.uv) * 2 - 1;
				return float4(force.xy * _IncreaseStrength,0,0);
			}
			ENDHLSL
		}

		Pass
		{

			Name "ApplyVelocityOutward"
			Cull Off ZWrite Off ZTest Always
			Blend One One, One One
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment fragOutward

			float4 fragOutward(v2f i) : SV_Target
			{
				float2 pos = i.uv;
				float dist = length(pos);
				float scale = max(0.0,1 - dist);
				scale = (pow(scale, _IncreaseExponent))  * _IncreaseStrength ;
				return float4(normalize(pos) * scale, 0, 0);
			}
            ENDHLSL
		}

		Pass
		{

			Name "DampenVelocityCircle"
			Cull Off ZWrite Off ZTest Always
			Blend DstColor Zero, DstColor Zero
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag_dampen

			float4 frag_dampen(v2f i) : SV_Target
			{
				float2 pos = i.uv;
				float dist = length(pos);

				clip(1 - dist);
				float scale = max(0, 1 - dist);

				return saturate(1 - (_IncreaseStrength * pow(scale, _IncreaseExponent)));
			}

            ENDHLSL
		}		
		
		Pass
		{

			Name "DampenVelocityTexture"
			Cull Off ZWrite Off ZTest Always
			Blend DstColor Zero, DstColor Zero
            HLSLPROGRAM
            #pragma vertex vert_texture
            #pragma fragment frag_dampen_texture

			sampler2D _MainTex;
			
			float4 frag_dampen_texture(v2f i) : SV_Target
			{
				float4 force = tex2D(_MainTex, i.uv).xxxx;
				return saturate(1 - force.xxxx * _IncreaseStrength);
			}

            ENDHLSL
		}
    }
}
