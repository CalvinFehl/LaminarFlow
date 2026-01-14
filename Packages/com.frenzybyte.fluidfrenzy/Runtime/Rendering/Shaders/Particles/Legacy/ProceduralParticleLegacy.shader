Shader "FluidFrenzy/Particles/Legacy/ProceduralParticle(Legacy)"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
		_Color("Color", Color) = (1,1,1,1)

		_BillboardMode ("Billboard Mode", Float) = 0.0
	}

	SubShader
	{
		PackageRequirements
		{
			"com.unity.render-pipelines.universal"
		}

		// Universal Pipeline tag is required. If Universal render pipeline is not set in the graphics settings
		// this Subshader will fail. One can add a subshader below or fallback to Standard built-in to make this
		// material work with both Universal Render Pipeline and Builtin Unity Pipeline
		Tags{"RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "UniversalMaterialType" = "Lit" "IgnoreProjector" = "True" "ShaderModel" = "3.0"}
		Tags { "Queue" = "Transparent" }

		Pass
		{
			Tags{"LightMode" = "UniversalForward"}

			Name "FluidFrenzyParticle"
			// No culling or depth
			Cull Off ZWrite Off ZTest Lequal
			Blend OneMinusDstColor One
			HLSLPROGRAM
			#pragma vertex vertForward
			#pragma fragment fragForward

			#pragma exclude_renderers gles

			sampler2D _MainTex;

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include  "Packages/com.frenzybyte.fluidfrenzy/Runtime/Rendering/Shaders/Particles/ProceduralParticleCommon.hlsl"

			struct v2f_unlit
			{
				float2 uv : TEXCOORD0;
				float4 color : TEXCOORD1;
				float4 vertex : SV_POSITION;
			};


			v2f_unlit vertForward(uint vid : SV_VertexID, uint svInstanceID : SV_InstanceID)
			{
				v2f_unlit o;

				uint particleID = _DrawIndices[svInstanceID];
				Particle particle = _ParticleBuffer[particleID];

				float2 size = UnpackHalf2x16(asuint(particle.position_size.w));
				float2 rotation_angularvelocity = UnpackHalf2x16(particle.vel_accel_rot_angularvel.w);
				float4 color = UnpackFromR8G8B8A8(particle.life_maxlife_color.y);
				float2 life_maxlife = UnpackHalf2x16(particle.life_maxlife_color.x);

				float life = life_maxlife.x;
				float maxlife = life_maxlife.y;

				float4 vertex = (GetTriangleQuadVertexPosition(vid % 6) * 2 - 1) * float4(size,0,1);
				vertex.xyz = RotatePointZ(vertex, rotation_angularvelocity.x + rotation_angularvelocity * _Time.y);

				float3 positionWS = particle.position_size.xyz + vertex.xzy;
				float3 positionVS = TransformWorldToView(particle.position_size.xyz) + vertex.xyz;

				o.vertex = TransformWViewToHClip(positionVS.xyz);
				
				o.color = color;
				float progress = life / max(0.01f,maxlife);
				o.color.a *= smoothstep(0.0, 0.2,PingPong(1 - progress, 0.5f) * 2);
				o.uv.xy = GetTriangleQuadTexCoord(vid % 6);
				return o;
			}

			float4 fragForward(v2f_unlit i) : SV_Target
			{
				float4 col = tex2D(_MainTex, i.uv);
				col *= i.color * i.color.a;
				return col;
			}

			ENDHLSL
		}
	}

    SubShader
    {
		Tags { "RenderType" = "Transparent" "Queue" = "Transparent" }

		Pass
		{
			Tags{ "LightMode" = "ForwardBase" }

			Name "FluidFrenzyParticle"
			// No culling or depth
			Cull Off ZWrite Off ZTest Lequal
			Blend OneMinusDstColor One
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#pragma exclude_renderers gles
			sampler2D _MainTex;
			float4 _Color;

			#include "UnityCG.cginc"
			#include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Rendering/Shaders/Particles/ProceduralParticleCommon.hlsl"

			struct v2f_unlit
			{
				float2 uv : TEXCOORD0;
				float4 color : TEXCOORD1;
				float4 vertex : SV_POSITION;
			};


			v2f_unlit vert(uint vid : SV_VertexID, uint svInstanceID : SV_InstanceID)
			{
				v2f_unlit o;

				uint particleID = _DrawIndices[svInstanceID];
				Particle particle = _ParticleBuffer[particleID];

				float2 size = UnpackHalf2x16(asuint(particle.position_size.w));
				float2 rotation_angularvelocity = UnpackHalf2x16(particle.vel_accel_rot_angularvel.w);
				float4 color = UnpackFromR8G8B8A8(particle.life_maxlife_color.y);
				float2 life_maxlife = UnpackHalf2x16(particle.life_maxlife_color.x);

				float life = life_maxlife.x;
				float maxlife = life_maxlife.y;

				float4 vertex = (GetTriangleQuadVertexPosition(vid % 6) * 2 - 1) * float4(size,0,1);
				vertex.xyz = RotatePointZ(vertex, rotation_angularvelocity.x + rotation_angularvelocity.y * _Time.y);

				float3 particlePos = particle.position_size.xyz;
				float3 positionWS = particlePos.xyz + vertex.xzy;
				float3 positionVS = mul(UNITY_MATRIX_V, float4(particlePos.xyz, 1)) + vertex.xyz;

				o.vertex = UnityViewToClipPos(float4(positionVS.xyz,1));
				o.color = color;

				float progress = life / max(0.01f,maxlife);
				o.color.a *= smoothstep(0.0, 0.2,PingPong(1 - progress, 0.5f) * 2);
				o.uv.xy = GetTriangleQuadTexCoord(vid % 6);
				return o;
			}

			float4 frag(v2f_unlit i) : SV_Target
			{
				float4 col = tex2D(_MainTex, i.uv) * _Color;
				col *= i.color * i.color.a;
				return col;
			}

			ENDHLSL
		}
    }
}
