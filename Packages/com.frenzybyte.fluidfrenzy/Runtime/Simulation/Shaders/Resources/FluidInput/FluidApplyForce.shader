Shader "Hidden/FluidFrenzy/ApplyForce"
{
    SubShader
    {
		HLSLINCLUDE
		#include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Simulation/Shaders/Resources/FluidSimulationCommon.hlsl"

		float2 _ForceDir;

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
			o.vertex.xy += (_ForceDir.xy) * 0.25f * _BlitScaleBiasRt.xy;
		#if UNITY_UV_STARTS_AT_TOP
			o.vertex.xy = o.vertex.xy * float2(2.0f, -2.0f) + float2(-1.0f, 1.0f); //convert to -1..1
		#else
			o.vertex.xy = o.vertex.xy * float2(2.0f, 2.0f) + float2(-1.0f, -1.0f); //convert to -1..1
		#endif
			o.uv.xy = GetQuadTexCoord(vid) * 2 - 1;
			return o;
		}

		float _IncreaseStrength;
		float _IncreaseExponent;

		ENDHLSL
		Pass
		{
			Name "ApplyForceDirection"
			// No culling or depth
			Cull Off ZWrite Off ZTest Always
			Blend One One, One One

			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment fragDirection

			float4 fragDirection(v2f i) : SV_Target
			{
				float2 pos = i.uv;
				float2 pxDist = pos;
				float dist = length(pxDist);
				float scale = max(0,1 - dist);
				scale = pow(scale, _IncreaseExponent);


				float d = max(0,dot((_ForceDir.xy), (pxDist))) * 0.5 + 0.5;
				return float4(smoothstep(0, 1, scale) * _IncreaseStrength * d,0,0,0);
			}

			ENDHLSL
		}

		Pass
		{
			Name "ApplyForceSplash"
			// No culling or depth
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
				scale = (pow(scale, _IncreaseExponent))  * _IncreaseStrength;
				return float4(scale, 0, 0, 0);
			}

			ENDHLSL
		}

		Pass
		{
			Name "ApplyForceVortex"
			// No culling or depth
			Cull Off ZWrite Off ZTest Always
			Blend One One, One One

			HLSLPROGRAM
			#pragma vertex vertWhirlpool
			#pragma fragment fragWhirlPool

			v2f vertWhirlpool(uint vid : SV_VertexID)
			{
				v2f o;
				o.vertex = GetQuadVertexPosition(vid);
				o.vertex.xy = o.vertex.xy * _BlitScaleBiasRt.xy + _BlitScaleBiasRt.zw - _BlitScaleBiasRt.xy * 0.5f;
			#if UNITY_UV_STARTS_AT_TOP
				o.vertex.xy = o.vertex.xy * float2(2.0f, -2.0f) + float2(-1.0f, 1.0f); //convert to -1..1
			#else
				o.vertex.xy = o.vertex.xy * float2(2.0f, 2.0f) + float2(-1.0f, -1.0f); //convert to -1..1
			#endif
				o.uv.xy = GetQuadTexCoord(vid) * 2 - 1;
				return o;
			}

			float4 fragWhirlPool(v2f i) : SV_Target
			{
				float4 outflow = float4(0,0,0,0);
				float2 pos = i.uv;
				float dist = length(pos);
				dist = pow(dist, _IncreaseExponent);
				float scale = max(0,1 - dist);
				scale = scale * 1000.0f * _IncreaseStrength;
				return float4(scale,0,0,0);
			}

			ENDHLSL
		}

		Pass
		{
			Name "ApplyForceTexture"
			Cull Off ZWrite Off ZTest Always
			Blend One One, One One
			HLSLPROGRAM
			#pragma vertex vert_texture
			#pragma fragment frag

			sampler2D _MainTex;


			v2f vert_texture(uint vid : SV_VertexID)
			{
				v2f o;
				o.vertex = GetQuadVertexPosition(vid);
				o.vertex.xy = o.vertex.xy * _BlitScaleBiasRt.xy + _BlitScaleBiasRt.zw - _BlitScaleBiasRt.xy * 0.5f;
			#if UNITY_UV_STARTS_AT_TOP
				o.vertex.xy = o.vertex.xy * float2(2.0f, -2.0f) + float2(-1.0f, 1.0f); //convert to -1..1
			#else
				o.vertex.xy = o.vertex.xy * float2(2.0f, 2.0f) + float2(-1.0f, -1.0f); //convert to -1..1
			#endif
				o.uv.xy = GetQuadTexCoord(vid);
				return o;
			}
			
			float4 frag(v2f i) : SV_Target
			{
				float4 force = tex2D(_MainTex, i.uv);
				return float4(force.x * _IncreaseStrength,0,0,0);
			}
			ENDHLSL
		}


			Pass
		{
			Name "DampenForceCircle"
			// No culling or depth
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

				float scale = pow(max(0,1 - dist), _IncreaseExponent);

				return saturate(1 - _IncreaseStrength * scale);
			}

			ENDHLSL
		}

		Pass
		{
			Name "DampenForceTexture"
			Cull Off ZWrite Off ZTest Always
			Blend DstColor Zero, DstColor Zero

			HLSLPROGRAM
			#pragma vertex vert_texture
			#pragma fragment frag_dampen_texture

			sampler2D _MainTex;

			v2f vert_texture(uint vid : SV_VertexID)
			{
				v2f o;
				o.vertex = GetQuadVertexPosition(vid);
				o.vertex.xy = o.vertex.xy * _BlitScaleBiasRt.xy + _BlitScaleBiasRt.zw - _BlitScaleBiasRt.xy * 0.5f;
			#if UNITY_UV_STARTS_AT_TOP
				o.vertex.xy = o.vertex.xy * float2(2.0f, -2.0f) + float2(-1.0f, 1.0f); //convert to -1..1
			#else
				o.vertex.xy = o.vertex.xy * float2(2.0f, 2.0f) + float2(-1.0f, -1.0f); //convert to -1..1
			#endif
				o.uv.xy = GetQuadTexCoord(vid);
				return o;
			}
			
			float4 frag_dampen_texture(v2f i) : SV_Target
			{
				float4 force = tex2D(_MainTex, i.uv);
				return saturate(1 - (force.xxxx * _IncreaseStrength));

			}
			ENDHLSL
		}
    }
}
