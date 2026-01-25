Shader "Hidden/FluidFrenzy/TerrainModifyPreview"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

		HLSLINCLUDE

		float3 _TerrainPosition;
        float3 _Position;
        float2 _GridSize;
        float2 _GridDim;
        float3 _TerrainScale;
        float2 _Direction;
        float _GradientScale;
        float _Shape;
        float _BlendMode;
        float _Space;
		float _Amount;
		float4x4 _ObjectToWorld;

        sampler2D _HeightmapPreview;
        sampler2D _Heightmap;
		float4 _Heightmap_TexelSize;
			
		ENDHLSL

        Pass
        {
			Name "AmountGrid"
            ZTest Always Cull Back ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha
            HLSLPROGRAM
            #pragma vertex vert2
            #pragma fragment frag
            #pragma target 4.5

			#include "UnityStandardUtils.cginc"
			#define BUILTIN_TARGET_API


            #include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Simulation/Shaders/Resources/FluidSimulationCommon.hlsl"
			#include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Simulation/Shaders/Resources/FluidInputCommon.hlsl"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct VertVarryings
            {
                float4 vertex : SV_POSITION;
                float2 heightUV : TEXCOORD1;
                float2 brushUV : TEXCOORD2;
                float3 worldPos : TEXCOORD3;
                float heightOffset : TEXCOORD4;
            };

			float3 CalculateNormalFromHeightField(sampler2D heightField, float4 heightField_TexelSize, float2 worldSpaceTexelSize, float2 uv, float size, float dim)
			{
				float texelwss = worldSpaceTexelSize.x * 2 * size / dim;
				float2 du = float2(heightField_TexelSize.x, 0) * size / dim;
				float2 dv = float2(0, heightField_TexelSize.y) * size / dim;

				float state_l = dot(tex2Dlod(heightField, float4(uv.xy + du, 0, 0)), (1.0f).xxxx);
				float state_r = dot(tex2Dlod(heightField, float4(uv.xy - du, 0, 0)), (1.0f).xxxx);
				float state_t = dot(tex2Dlod(heightField, float4(uv.xy + dv, 0, 0)), (1.0f).xxxx);
				float state_b = dot(tex2Dlod(heightField, float4(uv.xy - dv, 0, 0)), (1.0f).xxxx);

				float dhdu = ((state_r)-(state_l));
				float dhdv = ((state_b)-(state_t));
				float3 normal = normalize(float3(dhdu, texelwss.x, dhdv));
				return normal;
			}


			VertVarryings vert2(uint vid : SV_VertexID)
            {
                VertVarryings o;

                int vertexID = vid % 4;
                uint topBit = vertexID >> 1;
                uint botBit = (vertexID & 1);
                float x = topBit;
                float y = (topBit + botBit) & 1;
                int gridID = vid / 4;
                uint gridX = gridID % (uint)_GridSize.x;
                uint gridY = gridID / (uint)_GridSize.x;
                float2 normalizedPos = (float2(x + (float)gridX, y + (float)gridY)) / _GridSize;
                float2 centeredPos = (normalizedPos - 0.5) * _GridDim;
                float4 localPos = float4(centeredPos.x, 0, centeredPos.y, 1);
                
                float3 worldPos = mul(_ObjectToWorld, localPos).xyz;
                o.brushUV = (normalizedPos * 2 - 1);

                float distance = lerp(DistanceBox(o.brushUV), DistanceCircle(o.brushUV), _Shape);
                distance = step(saturate(distance), 0.99f);
                float vertexOffset = _BlendMode == 1 ? 0 : _Amount;

                // Height sampling will now be perfectly aligned with the terrain's vertices.
                float2 heightUV = (worldPos.xz - (_TerrainPosition.xz - (_TerrainScale.xz * 0.5))) / _TerrainScale.xz;
				float2 gridRcp = (1.0f / (_Heightmap_TexelSize.zw ));
                heightUV = (heightUV * (_Heightmap_TexelSize.zw - 1)) * gridRcp + gridRcp * 0.5;
                float heightPreview = dot(tex2Dlod(_HeightmapPreview, float4(heightUV,0,0)), float4(1,1,1,1));
                float height = dot(tex2Dlod(_Heightmap, float4(heightUV,0,0)), float4(1,1,1,1));
    
                worldPos.y = heightPreview * _TerrainScale.y  ;

				o.heightOffset = heightPreview - height;
				o.heightUV = heightUV;
                o.worldPos = worldPos;
                o.vertex = UnityWorldToClipPos(worldPos);
             //   o.vertex.z += (o.vertex.z / o.vertex.w) * 1; 
                return o;
            }


            fixed4 frag (VertVarryings i) : SV_Target
            {
                float3 normal = CalculateNormalFromHeightField(_HeightmapPreview, _Heightmap_TexelSize, _TerrainScale.xz / _Heightmap_TexelSize.zw, i.heightUV, 1, 1);

				float4 color = 0;
				color.xyz = saturate(normal.xzy * float3(-0.5f, -0.5f, 0.5f) + 0.5f);
				color = color * (0.1 + 0.9f * dot(normal, _WorldSpaceLightPos0.xyz));
				return float4(color.rgb, 0.85f * saturate(abs(i.heightOffset)));

            }
            ENDHLSL
        }
    }
}
