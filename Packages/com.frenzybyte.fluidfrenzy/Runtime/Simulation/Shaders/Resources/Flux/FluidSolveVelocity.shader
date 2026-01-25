Shader "Hidden/FluidFrenzy/SolveVelocity"
{

    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

		HLSLINCLUDE
		#define USETEXTURE2D
		#include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Simulation/Shaders/Resources/FluidSimulationCommon.hlsl"

		struct v2f
		{
			float2 uv : TEXCOORD0;
#if SHADER_TARGET < 40
			float4 uv0 : TEXCOORD1;
			float4 uv1 : TEXCOORD2;
#endif
			float4 vertex : SV_POSITION;
		};


		float4 jacobian(v2f i, float pressure)
		{

	#if SHADER_TARGET < 40
			float pE = _PressureField.Sample(sampler_PressureField, i.uv0.xy).x;
			float pW = _PressureField.Sample(sampler_PressureField, i.uv0.zw).x;

			float pN = _PressureField.Sample(sampler_PressureField, i.uv1.xy).x;
			float pS = _PressureField.Sample(sampler_PressureField, i.uv1.zw).x;
	#else
			float pE, pW, pN, pS;
			GatherNeighbourBilinear(_PressureField, sampler_PressureField, i.uv, 1, pE, pW, pN, pS);
	#endif

			float4 col = float4(0.25 * (
				_DivergenceField.Sample(sampler_DivergenceField, i.uv).x
				+ (pE
				+ pW
				+ pN
				+ pS) * pressure
				), 0.0, 0.0, 1.0);
			return col;
		}

		ENDHLSL

		Pass
		{
			Name "AdvectVelocity"
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Simulation/Shaders/Resources/FluidSimulationCommon.hlsl"

			struct v2f_advection
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			v2f_advection vert(uint vid : SV_VertexID)
			{
				v2f_advection o;
				o.vertex = GetQuadVertexPosition(vid);
				o.vertex.xy = o.vertex.xy * float2(2.0f, -2.0f) + float2(-1.0f, 1.0f); //convert to -1..1
				o.uv = GetQuadTexCoord(vid);
				return o;
			}

			CBUFFER_START(Advect)
			float2 _AdvectScale;
			CBUFFER_END

			float4 frag(v2f_advection i) : SV_Target
			{
				float4 vel = _VelocityField.Sample(sampler_VelocityField, i.uv);
				float2 pos = (i.uv - vel.xy * _AdvectScale * _VelocityDeltaTime);
				return _VelocityField.Sample(sampler_VelocityField, pos);
			}
			ENDHLSL
		}

		Pass
		{
			Name "Divergence"
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Simulation/Shaders/Resources/FluidSimulationCommon.hlsl"

			struct v2f_divergence
			{
				float4 uv0 : TEXCOORD0;
				float4 uv1 : TEXCOORD1;
				float4 vertex : SV_POSITION;
			};

			CBUFFER_START(Diverge)
			float4 _Epsilon; //eps, epsdt
			CBUFFER_END

			v2f_divergence vert(uint vid : SV_VertexID)
			{
				v2f_divergence o;
				o.vertex = GetQuadVertexPosition(vid);
				o.vertex.xy = o.vertex.xy * float2(2.0f, -2.0f) + float2(-1.0f, 1.0f); //convert to -1..1
				float2 uv = GetQuadTexCoord(vid);

				o.uv0.xy = uv + float2(_Epsilon.x, 0.0);
				o.uv0.zw = uv - float2(_Epsilon.x, 0.0);

				o.uv1.xy = uv + float2(0.0, _Epsilon.y);
				o.uv1.zw = uv - float2(0.0, _Epsilon.y);

				return o;
			}


			float4 frag(v2f_divergence i) : SV_Target
			{
				float2 uE = _VelocityField.Sample(sampler_VelocityField, i.uv0.xy).xy;
				float2 uW = _VelocityField.Sample(sampler_VelocityField, i.uv0.zw).xy;

				float2 uN = _VelocityField.Sample(sampler_VelocityField, i.uv1.xy).xy;
				float2 uS = _VelocityField.Sample(sampler_VelocityField, i.uv1.zw).xy;

				float x =  ((_Epsilon.z * _VelocityDeltaTimeRcp ) *(uE.x - uW.x) + (_Epsilon.w * _VelocityDeltaTimeRcp ) *(uN.y - uS.y));
				float4 col = float4(x, 0.0f, 0.0f, 1.0f);
				return col;
			}
			ENDHLSL
		}

		Pass
		{
			Name "JacobianReducePressure"
			HLSLPROGRAM
			#pragma target 5.0
			#pragma vertex vert
			#pragma fragment frag

			CBUFFER_START(Jacobi)
			float4 _Epsilon; //eps, epsdt
			float _Pressure;
			CBUFFER_END

			v2f vert(uint vid : SV_VertexID)
			{
				v2f o;
				o.vertex = GetQuadVertexPosition(vid);
				o.vertex.xy = o.vertex.xy * float2(2.0f, -2.0f) + float2(-1.0f, 1.0f); //convert to -1..1
				o.uv = GetQuadTexCoord(vid);

		#if SHADER_TARGET < 40
				o.uv0.xy = o.uv + float2(_Epsilon.x, 0.0);
				o.uv0.zw = o.uv - float2(_Epsilon.x, 0.0);

				o.uv1.xy = o.uv + float2(0.0, _Epsilon.y);
				o.uv1.zw = o.uv - float2(0.0, _Epsilon.y);
		#endif
				return o;
			}

			float4 frag(v2f i) : SV_Target
			{
				return jacobian(i, _Pressure);
			}

			ENDHLSL
		}

		Pass
		{
			Name "Jacobian"
			HLSLPROGRAM
			#pragma target 5.0
			#pragma vertex vert
			#pragma fragment frag

			CBUFFER_START(Jacobi)
			float4 _Epsilon; //eps, epsdt
			CBUFFER_END

			v2f vert(uint vid : SV_VertexID)
			{
				v2f o;
				o.vertex = GetQuadVertexPosition(vid);
				o.vertex.xy = o.vertex.xy * float2(2.0f, -2.0f) + float2(-1.0f, 1.0f); //convert to -1..1
				o.uv = GetQuadTexCoord(vid);

		#if SHADER_TARGET < 40
				o.uv0.xy = o.uv + float2(_Epsilon.x, 0.0);
				o.uv0.zw = o.uv - float2(_Epsilon.x, 0.0);

				o.uv1.xy = o.uv + float2(0.0, _Epsilon.y);
				o.uv1.zw = o.uv - float2(0.0, _Epsilon.y);
		#endif
				return o;
			}

			float4 frag(v2f i) : SV_Target
			{
				return jacobian(i, 1);
			}


			ENDHLSL
		}

		Pass
		{
			Name "ApplyPressure"
			HLSLPROGRAM
			#pragma target 5.0
			#pragma vertex vert
			#pragma fragment frag

			CBUFFER_START(Pressure)
			float4 _Epsilon; //eps, epsdt, lerp
			CBUFFER_END

			v2f vert(uint vid : SV_VertexID)
			{
				v2f o;
				o.vertex = GetQuadVertexPosition(vid);
				o.vertex.xy = o.vertex.xy * float2(2.0f, -2.0f) + float2(-1.0f, 1.0f); //convert to -1..1
				o.uv = GetQuadTexCoord(vid);

#if SHADER_TARGET < 40
				o.uv0.xy = o.uv + float2(_Epsilon.x, 0.0);
				o.uv0.zw = o.uv - float2(_Epsilon.x, 0.0);

				o.uv1.xy = o.uv + float2(0.0, _Epsilon.y);
				o.uv1.zw = o.uv - float2(0.0, _Epsilon.y);
#endif
				return o;
			}

			float4 frag(v2f i) : SV_Target
			{
#if SHADER_TARGET < 40
				float pE = _PressureField.Sample(sampler_PressureField, i.uv0.xy);
				float pW = _PressureField.Sample(sampler_PressureField, i.uv0.zw);

				float pN = _PressureField.Sample(sampler_PressureField, i.uv1.xy);
				float pS = _PressureField.Sample(sampler_PressureField, i.uv1.zw);
#else
				float pE, pW, pN, pS;
				GatherNeighbourBilinear(_PressureField, sampler_PressureField, i.uv, 1, pW, pE, pN, pS);
#endif

				float rho = 1;
				float2 u_a = _VelocityField.Sample(sampler_VelocityField, i.uv.xy).xy;
				float diff_p_x = (pE - pW);
				float u_x = u_a.x - _VelocityDeltaTime * _Epsilon.z * diff_p_x;
				float diff_p_y = (pN - pS);
				float u_y = u_a.y - _VelocityDeltaTime * _Epsilon.w * diff_p_y;
				float4 col = float4(u_x, u_y, 0.0, 0.0);

				return col;
			}
			ENDHLSL
		}
    }

    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

		Pass
		{
			Name "AdvectVelocity"
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Simulation/Shaders/Resources/FluidSimulationCommon.hlsl"

			struct v2f_advection
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			v2f_advection vert(uint vid : SV_VertexID)
			{
				v2f_advection o;
				o.vertex = GetQuadVertexPosition(vid);
				o.vertex.xy = o.vertex.xy * float2(2.0f, -2.0f) + float2(-1.0f, 1.0f); //convert to -1..1
				o.uv = GetQuadTexCoord(vid);
				return o;
			}

			CBUFFER_START(Advect)
			float2 _AdvectScale;
			CBUFFER_END

			float4 frag(v2f_advection i) : SV_Target
			{
				float4 vel = _VelocityField.Sample(sampler_VelocityField, i.uv);
				float2 pos = (i.uv - vel.xy * _AdvectScale * _VelocityDeltaTime);
				return _VelocityField.Sample(sampler_VelocityField, pos);
			}
			ENDHLSL
		}

		Pass
		{
			Name "Divergence"
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Simulation/Shaders/Resources/FluidSimulationCommon.hlsl"

			struct v2f_divergence
			{
				float4 uv0 : TEXCOORD0;
				float4 uv1 : TEXCOORD1;
				float4 vertex : SV_POSITION;
			};

			CBUFFER_START(Diverge)
			float4 _Epsilon; //eps, epsdt
			CBUFFER_END

			v2f_divergence vert(uint vid : SV_VertexID)
			{
				v2f_divergence o;
				o.vertex = GetQuadVertexPosition(vid);
				o.vertex.xy = o.vertex.xy * float2(2.0f, -2.0f) + float2(-1.0f, 1.0f); //convert to -1..1
				float2 uv = GetQuadTexCoord(vid);

				o.uv0.xy = uv + float2(_Epsilon.x, 0.0);
				o.uv0.zw = uv - float2(_Epsilon.x, 0.0);

				o.uv1.xy = uv + float2(0.0, _Epsilon.y);
				o.uv1.zw = uv - float2(0.0, _Epsilon.y);

				return o;
			}


			float4 frag(v2f_divergence i) : SV_Target
			{
				float2 uE = _VelocityField.Sample(sampler_VelocityField, i.uv0.xy).xy;
				float2 uW = _VelocityField.Sample(sampler_VelocityField, i.uv0.zw).xy;

				float2 uN = _VelocityField.Sample(sampler_VelocityField, i.uv1.xy).xy;
				float2 uS = _VelocityField.Sample(sampler_VelocityField, i.uv1.zw).xy;

				float x = (_Epsilon.z * _VelocityDeltaTimeRcp ) * ((uE.x - uW.x) + (uN.y - uS.y));
				float4 col = float4(x, 0.0f, 0.0f, 1.0f);
				return col;
			}
			ENDHLSL
		}

		Pass
		{
			Name "JacobianReducePressure"
			HLSLPROGRAM
			#pragma target 3.0
			#pragma vertex vert
			#pragma fragment frag

			CBUFFER_START(Jacobi)
			float4 _Epsilon; //eps, epsdt
			float _Pressure;
			CBUFFER_END

			v2f vert(uint vid : SV_VertexID)
			{
				v2f o;
				o.vertex = GetQuadVertexPosition(vid);
				o.vertex.xy = o.vertex.xy * float2(2.0f, -2.0f) + float2(-1.0f, 1.0f); //convert to -1..1
				o.uv = GetQuadTexCoord(vid);

		#if SHADER_TARGET < 40
				o.uv0.xy = o.uv + float2(_Epsilon.x, 0.0);
				o.uv0.zw = o.uv - float2(_Epsilon.x, 0.0);

				o.uv1.xy = o.uv + float2(0.0, _Epsilon.y);
				o.uv1.zw = o.uv - float2(0.0, _Epsilon.y);
		#endif
				return o;
			}

			float4 frag(v2f i) : SV_Target
			{
				return jacobian(i, _Pressure);
			}

			ENDHLSL
		}

		Pass
		{
			Name "Jacobian"
			HLSLPROGRAM
			#pragma target 3.0
			#pragma vertex vert
			#pragma fragment frag

			CBUFFER_START(Jacobi)
			float4 _Epsilon; //eps, epsdt
			CBUFFER_END

			v2f vert(uint vid : SV_VertexID)
			{
				v2f o;
				o.vertex = GetQuadVertexPosition(vid);
				o.vertex.xy = o.vertex.xy * float2(2.0f, -2.0f) + float2(-1.0f, 1.0f); //convert to -1..1
				o.uv = GetQuadTexCoord(vid);

		#if SHADER_TARGET < 40
				o.uv0.xy = o.uv + float2(_Epsilon.x, 0.0);
				o.uv0.zw = o.uv - float2(_Epsilon.x, 0.0);

				o.uv1.xy = o.uv + float2(0.0, _Epsilon.y);
				o.uv1.zw = o.uv - float2(0.0, _Epsilon.y);
		#endif
				return o;
			}

			float4 frag(v2f i) : SV_Target
			{
				return jacobian(i, 1);
			}


			ENDHLSL
		}

		Pass
		{
			Name "ApplyPressure"
			HLSLPROGRAM
			#pragma target 3.0
			#pragma vertex vert
			#pragma fragment frag

			CBUFFER_START(Pressure)
			float4 _Epsilon; //eps, epsdt, lerp
			CBUFFER_END

			v2f vert(uint vid : SV_VertexID)
			{
				v2f o;
				o.vertex = GetQuadVertexPosition(vid);
				o.vertex.xy = o.vertex.xy * float2(2.0f, -2.0f) + float2(-1.0f, 1.0f); //convert to -1..1
				o.uv = GetQuadTexCoord(vid);

#if SHADER_TARGET < 40
				o.uv0.xy = o.uv + float2(_Epsilon.x, 0.0);
				o.uv0.zw = o.uv - float2(_Epsilon.x, 0.0);

				o.uv1.xy = o.uv + float2(0.0, _Epsilon.y);
				o.uv1.zw = o.uv - float2(0.0, _Epsilon.y);
#endif
				return o;
			}

			float4 frag(v2f i) : SV_Target
			{
#if SHADER_TARGET < 40
				float pE = _PressureField.Sample(sampler_PressureField, i.uv0.xy).r;
				float pW = _PressureField.Sample(sampler_PressureField, i.uv0.zw).r;

				float pN = _PressureField.Sample(sampler_PressureField, i.uv1.xy).r;
				float pS = _PressureField.Sample(sampler_PressureField, i.uv1.zw).r;
#else
				float pE, pW, pN, pS;
				GatherNeighbourBilinear(_PressureField, sampler_PressureField, i.uv, 1, pW, pE, pN, pS);
#endif

				float rho = 1;
				float2 u_a = _VelocityField.Sample(sampler_VelocityField, i.uv.xy).xy;
				float diff_p_x = (pE - pW);
				float u_x = u_a.x - _VelocityDeltaTime * _Epsilon.z * diff_p_x;
				float diff_p_y = (pN - pS);
				float u_y = u_a.y - _VelocityDeltaTime * _Epsilon.w * diff_p_y;
				float4 col = float4(u_x, u_y, 0.0, 0.0);

				return col;
			}
			ENDHLSL
		}
    }
}
