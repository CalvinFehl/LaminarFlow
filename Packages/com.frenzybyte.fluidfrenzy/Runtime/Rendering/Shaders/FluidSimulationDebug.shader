Shader "FluidFrenzy/Debug/SimulationData"
{
    Properties
    {
		[Enum(Layer1,0,Layer2,1)] _Layer("Layer", Float) = 0

		_WireThickness("Wire Thickness", RANGE(0, 10)) = 0.1
		_WireSmoothness("Wire Smoothness", RANGE(0, 10)) = 1
		_WireColor ("Wire Color", Color) = (0.0, 0.0, 0.0, 0.0)
    }
    SubShader
    {
        LOD 100

		CGINCLUDE
			#include "UnityCG.cginc"
			#include "UnityStandardUtils.cginc"
			
			#include "UnityShadowLibrary.cginc"
			#include "UnityStandardCore.cginc"
			#include "UnityStandardInput.cginc"
			#include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Rendering/Shaders/Library/ShadowSampling.cginc"
			#include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Rendering/Shaders/Library/LOD/FluidInstancingCommon.hlsl"
			#include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Rendering/Shaders/Library/FluidRenderingCommon.hlsl"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 fluidUV : TEXCOORD0;
				float2 textureUV : TEXCOORD1;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				FLUID_VERTEX_INPUT_INSTANCE_ID
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float2 uv01 : TEXCOORD0;
				float4 uv23				: TEXCOORD1;
				float4 worldPos : TEXCOORD2;
				float3 normalWS	: TEXCOORD3;
				float4 color : TEXCOORD4;
				UNITY_VERTEX_OUTPUT_STEREO
			};

			struct v2g
			{
				float4 vertex : SV_POSITION;
				float4 uv01 : TEXCOORD0;
				float4 uv23 : TEXCOORD1;
				float4 worldPos : TEXCOORD2;
				float3 normalWS	: TEXCOORD3;
				float4 color : TEXCOORD4;
				UNITY_VERTEX_OUTPUT_STEREO
			};

			struct g2f
			{
				float4 vertex : SV_POSITION;
				float4 uv01 : TEXCOORD0;
				float4 uv23 : TEXCOORD1;
				float4 worldPos : TEXCOORD2;
				float3 normalWS	: TEXCOORD3;
				float4 color : TEXCOORD4;
				float2 bary : TEXCOORD5;
				UNITY_VERTEX_OUTPUT_STEREO
			};

			sampler2D _DivergenceField;
			sampler2D _PressureField;
			sampler2D _VelocityField;
			float4 _VelocityField_ST;
			sampler2D _OutflowField;

			float _VelocityScale;
			float _Layer;

			uniform float _WireThickness;
			uniform float _WireSmoothness;
			uniform float4 _WireColor; 

			float2 GetUV(float2 iTexcoord, float2 iTexcoord0, float2 iTexcoord1)
			{
				float2 uv1 = iTexcoord0.xy * _FlowBlend.x;
				float2 uv2 = iTexcoord1.xy * _FlowBlend.y;
				float2 uv = (uv1 + uv2);
				return uv;
			}

			v2g vert(appdata v)
			{
				v2g o = (v2g)(0);
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

				float4 vertex = v.vertex;
				FluidData fluidData;
				SampleFluidSimulationData(vertex, v.fluidUV, v.textureUV, FLUID_GET_INSTANCE_ID(v), fluidData);
				o.normalWS.xyz = fluidData.normalOS;
				o.uv01 = fluidData.uv;
				o.uv23 = fluidData.flowUV;
				o.worldPos.xyz = mul(unity_ObjectToWorld,vertex);
				o.worldPos.w = fluidData.layerHeight;
				o.vertex = UnityObjectToClipPos(fluidData.positionOS);

#if defined(_FLUIDFRENZY_INSTANCING) && !defined(UNITY_INSTANCING_ENABLED)
				int lodLevel = LODLevel(FLUID_GET_INSTANCE_ID(v));
				o.color = kDebugColors[lodLevel];
#else
				o.color = kDebugColors[0];
#endif

				ApplyClipSpaceOffset(o.vertex, 0, 0);
				UNITY_TRANSFER_FOG(o,o.vertex);
				return o;
			}

			[maxvertexcount(3)]
			void geom(triangle v2g i[3], inout TriangleStream<g2f> triangleStream)
			{
				float2 p0 = i[0].vertex.xy / i[0].vertex.w;
				float2 p1 = i[1].vertex.xy / i[1].vertex.w;
				float2 p2 = i[2].vertex.xy / i[2].vertex.w;

				float2 edge0 = p2 - p1;
				float2 edge1 = p2 - p0;
				float2 edge2 = p1 - p0;

				// To find the distance to the opposite edge, we take the
				// formula for finding the area of a triangle Area = Base/2 * Height, 
				// and solve for the Height = (Area * 2)/Base.
				// We can get the area of a triangle by taking its cross product
				// divided by 2.  However we can avoid dividing our area/base by 2
				// since our cross product will already be double our area.
				float area = abs(edge1.x * edge2.y - edge1.y * edge2.x);
				float wireThickness = 800 - _WireThickness;

				g2f o;
				
				o.uv01 = i[0].uv01;
				o.uv23 = i[0].uv23;
				o.worldPos = i[0].worldPos;
				o.vertex = i[0].vertex;
				o.color = i[0].color;
				o.normalWS = i[0].normalWS;
				o.bary = float2(1, 0);
				UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(i[0], o);
				triangleStream.Append(o);

				o.uv01 = i[1].uv01;
				o.uv23 = i[1].uv23;
				o.worldPos = i[1].worldPos;
				o.vertex = i[1].vertex;
				o.color = i[1].color;
				o.normalWS = i[1].normalWS;
				o.bary = float2(0, 1);
				UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(i[1], o);
				triangleStream.Append(o);

				o.uv01 = i[2].uv01;
				o.uv23 = i[2].uv23;
				o.worldPos = i[2].worldPos;
				o.vertex = i[2].vertex;
				o.color = i[2].color;
				o.normalWS = i[2].normalWS;
				o.bary = float2(0, 0);
				UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(i[2], o);
				triangleStream.Append(o);
			}

			float4 frag(g2f i) : SV_Target
			{
				//ClipFluid(i.worldPos.w, _Layer);
#if FLUID_LOD
				float3 barys;
				barys.xy = i.bary;
				barys.z = 1 - barys.x - barys.y;
				float3 deltas = fwidth(barys);
				float3 smoothing = deltas * _WireSmoothness;
				float3 thickness = deltas * _WireThickness;
				barys = smoothstep(thickness, thickness + smoothing, barys);
				float minBary = min(barys.x, min(barys.y, barys.z));

				return lerp(_WireColor, i.color , minBary);
#endif

				float4 col = 0;
				float2 fluidUV = i.uv01.xy;
				float2 fluidHeight = 0, velocity = 0;
				SampleFluidHeightVelocity(fluidUV, fluidHeight, velocity);
				fluidHeight = i.worldPos.w * FluidLayerToMask(_Layer);
#if WATER_FLOW
				col = abs(tex2D(_OutflowField, i.uv01.xy)) * 100;
#elif WATER_DIVERGENCE
				col = abs(tex2D(_DivergenceField, i.uv01.xy * _VelocityField_ST.xy + _VelocityField_ST.zw).rrrr) * 700;
#elif WATER_PRESSURE
				float pressureSample = saturate(abs(tex2D(_PressureField, i.uv01.xy * _VelocityField_ST.xy + _VelocityField_ST.zw).r) * _VelocityScale);
				float pressure = pressureSample * pressureSample * 150;
				col = pressure;
#elif WATER_VELOCITY
				col = float4(velocity,0,0);//abs(tex2D(_VelocityField, i.uv01.xy));
#elif WATER_HEIGHT
				col = fluidHeight.yyyy;
#elif WATER_UV && _FLUID_FLOWMAPPING_DYNAMIC
				col = float4(GetUV(i.uv01.xy, i.uv23.xy, i.uv23.zw),0,1);
#elif WATER_NORMALS
				col = float4(normalize(i.normalWS), 1);
#elif WATER_FOAM
				col = float4(SampleFluidFoamField(i.uv01.zw),0,0,1);
#endif

				return col;
			}

			float4 fragShadow(VertexOutputForwardBase In) : SV_Target
			{
				return 1;
			}
			ENDCG

		Pass
        {
			Tags{ "RenderType" = "Opaque" "Queue" = "Geometry+510" }
			Tags{ "LightMode" = "ForwardBase" }

            CGPROGRAM
			#pragma target 3.0
			#pragma require geometry

            #pragma vertex vert
            #pragma geometry geom
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog
			#pragma multi_compile_instancing
			#pragma multi_compile _ _FLUIDFRENZY_INSTANCING
			#pragma multi_compile_local _ WATER_NORMALS WATER_HEIGHT WATER_VELOCITY WATER_FLOW WATER_DIVERGENCE WATER_FOAM WATER_PRESSURE WATER_UV FLUID_LOD
			#pragma multi_compile_local _ _FLUID_FLOWMAPPING_STATIC _FLUID_FLOWMAPPING_DYNAMIC
			#pragma multi_compile_local _ _FLUID_UNITY_TERRAIN

			ENDCG

		}
    }
}
