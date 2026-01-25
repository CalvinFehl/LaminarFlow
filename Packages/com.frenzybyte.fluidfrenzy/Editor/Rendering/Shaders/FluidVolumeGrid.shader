Shader "Hidden/FluidFrenzy/FluidVolumeGrid"
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

		Texture2D _Heightmap;
		SamplerState sampler_Heightmap;
			
		float3 HUEtoRGB(in float H)
		{
			float R = abs(H * 6 - 3) - 1;
			float G = 2 - abs(H * 6 - 2);
			float B = 2 - abs(H * 6 - 4);
			return saturate(float3(R,G,B));
		}
		float3 HSVtoRGB(in float3 HSV)
		{
			float3 RGB = HUEtoRGB(HSV.x);
			return ((RGB - 1) * HSV.y + 1) * HSV.z;
		}

		float DistanceCircle(float2 uv)
		{
			return length(uv.xy);
		}

		float DistanceBox(float2 uv)
		{
			return max(abs(uv.x), abs(uv.y));
		}

		ENDHLSL

		Pass
		{
			Name "AmountGrid"
			ZWrite Off
			Cull Off
			Blend SrcAlpha OneMinusSrcAlpha
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma geometry geom
			#pragma target 5.0
			#pragma multi_compile_local _ _FLUID_UNITY_TERRAIN

			#include "UnityCG.cginc"

			struct v2g
			{
				float4 vertex : SV_POSITION;
				float2 uv : TEXCOORD1;
				float3 worldPos : TEXCOORD2;
			};

			struct g2f
			{
				float4 vertex : SV_POSITION;
				float2 uv : TEXCOORD1;
				float3 worldPos : TEXCOORD2;
				float4 dist : TEXCOORD3;
			};

			static const uint quadIndices[6] = { 0, 1, 2, 2, 3, 0 };

			v2g vert(uint vid : SV_VertexID)
			{
				v2g o;

				int gridID = vid / 6; 
				int vertIndex = vid % 6;
				int vertexID = quadIndices[vertIndex]; 
                
				uint topBit = vertexID >> 1;
				uint botBit = (vertexID & 1);
				float x = topBit;
				float y = 1 - (topBit + botBit) & 1; 

				o.vertex = float4(x, y, 0, 1);

				uint gridX = gridID % (uint)_GridSize.x;
				uint gridY = gridID / (uint)_GridSize.x;

				float xPos = (float)(gridX);
				float yPos = (float)(gridY);

				o.vertex.x += xPos;
				o.vertex.y += yPos;

				o.vertex.xy /= _GridSize;
				o.vertex.xy = (o.vertex.xy * 2 - 1) * 0.5f;

				o.uv = o.vertex.xy / 0.5f;

				o.vertex.xy *= _GridDim;

				o.vertex.xyz = o.vertex.xzy;
				o.vertex.xyz += _Position;

				float distance = lerp(DistanceBox(o.uv), DistanceCircle(o.uv), _Shape);
				distance =  step(saturate(distance), 0.9f);

				float vertexOffset = _BlendMode == 1 ? 0 : _Amount;

				#if _FLUID_UNITY_TERRAIN
					float2 heightUV = ((o.vertex.xz - (_TerrainPosition.xz + _TerrainScale.xz * 0.5)) / (_TerrainScale.xz * 0.5)) * 0.5 + 0.5;
					float height = UnpackHeightmap(_Heightmap.SampleLevel(sampler_Heightmap, heightUV, 0));
				#else
					float2 heightUV = ((o.vertex.xz - (_TerrainPosition.xz)) / (_TerrainScale.xz * 0.5)) * 0.5 + 0.5;
					float height = dot(_Heightmap.SampleLevel(sampler_Heightmap, heightUV, 0), float4(1,1,1,1));
				#endif

				if(_Space == 0)
					o.vertex.y = height * _TerrainScale.y + _TerrainPosition.y + distance * vertexOffset;
				else
					o.vertex.y = lerp(height * _TerrainScale.y + _TerrainPosition.y, vertexOffset, distance);

				o.worldPos = o.vertex.xyz;
				o.vertex = UnityObjectToClipPos(float4(o.worldPos, 1.0));
                
				o.vertex.z += (o.vertex.z / o.vertex.w) * 1;
                
				return o;
			}

			[maxvertexcount(3)]
			void geom(triangle v2g i[3], inout TriangleStream<g2f> triangleStream)
			{
				if (i[0].vertex.w <= 0.001 || i[1].vertex.w <= 0.001 || i[2].vertex.w <= 0.001)
					return;

				float2 p0 = i[0].vertex.xy / i[0].vertex.w;
				float2 p1 = i[1].vertex.xy / i[1].vertex.w;
				float2 p2 = i[2].vertex.xy / i[2].vertex.w;

				float2 edge0 = p2 - p1;
				float2 edge1 = p2 - p0;
				float2 edge2 = p1 - p0;

				float area = abs(edge1.x * edge2.y - edge1.y * edge2.x);
				float wireThickness = 500;

				g2f o;
				
				float len0 = length(edge0) + 0.00001;
				float len1 = length(edge1) + 0.00001;
				float len2 = length(edge2) + 0.00001;

				o.vertex = i[0].vertex;
				o.worldPos = i[0].worldPos;
				o.uv = i[0].uv;
				o.dist.xyz = float3( (area / len0), 0.0, 0.0) * o.vertex.w * wireThickness;
				o.dist.w = 1.0 / o.vertex.w;
				UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(i[0], o);
				triangleStream.Append(o);

				o.vertex = i[1].vertex;
				o.worldPos = i[1].worldPos;
				o.uv = i[1].uv;
				o.dist.xyz = float3(0.0, (area / len1), 0.0) * o.vertex.w * wireThickness;
				o.dist.w = 1.0 / o.vertex.w;
				UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(i[1], o);
				triangleStream.Append(o);

				o.vertex = i[2].vertex;
				o.worldPos = i[2].worldPos;
				o.uv = i[2].uv;
				o.dist.xyz = float3(0.0, 0.0, (area / len2)) * o.vertex.w * wireThickness;
				o.dist.w = 1.0 / o.vertex.w;
				UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(i[2], o);
				triangleStream.Append(o);
			}

			fixed4 frag (g2f i) : SV_Target
			{
				float minDistanceToEdge = min(i.dist[0], min(i.dist[1], i.dist[2])) * i.dist[3];

				float distance = lerp(DistanceCircle(i.uv), DistanceBox(i.uv), saturate(_Shape));
				distance = saturate(1 - distance);
				float3 color = HSVtoRGB(float3(distance * _GradientScale,1,1));

				float wire = 1 - saturate(minDistanceToEdge);
				return float4(color, wire * pow(max(distance, 0.0), 0.5));
			}
			ENDHLSL
		}

		Pass
		{
			Name "VectorField"
			ZWrite Off
			Cull Off
			Blend SrcAlpha OneMinusSrcAlpha
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 5.0
			#pragma multi_compile_local _ _FLUID_UNITY_TERRAIN

            #include "UnityCG.cginc"

			sampler2D _MainTex;

			// Styles
			static int ARROW_V_STYLE = 1;
			static int ARROW_LINE_STYLE = 2;

			// Choose arrow style
			static int ARROW_STYLE = ARROW_LINE_STYLE;
			static float ARROW_TILE_SIZE = 64.0;

			// Arrow parameters
			static float ARROW_HEAD_ANGLE = (60.0 * UNITY_PI / 180.0);
			static float ARROW_HEAD_LENGTH = (ARROW_TILE_SIZE / 3.0);
			static float ARROW_SHAFT_THICKNESS = 9.0;

			// Helper functions

			// Computes the center pixel of the tile containing pixel pos
			float2 arrowTileCenterCoord(float2 pos)
			{
				return (floor(pos / ARROW_TILE_SIZE) + 0.5) * ARROW_TILE_SIZE;
			}


			// The vector field function
			float2 field(float2 pos, float2 uv)
			{
				if(_Shape == 1)
				{
					float dist = length(uv);

					float scale = max(0, 1 - dist);
					float2 x = 5 * smoothstep(0.0, 0.2, scale);
					float2 y = 5 * smoothstep(0.0, 0.2, scale);
					float2 tanVel = float2(uv.yx) * float2(-1,1);
					return normalize(lerp((tanVel) * x, -(uv.xy) * y, scale)) * _Amount;
				}
				else if(_Shape == 2)
				{
					return (tex2D(_MainTex, uv * 0.5f + 0.5f).rg * 2 - 1) * _Amount;
				}

				return _Direction * _Amount;
			}

			// Main function to compute arrow pixel intensity
			// The arrow function
			float arrow(float2 p, float2 v, float offset, float headoffset)
			{
				// Make everything relative to the center, which may be fractional
				p -= arrowTileCenterCoord(p);

				float mag_v = length(v);
				float mag_p = length(p);

				if (mag_v > 0.0)
				{
					// Non-zero velocity case
					float2 dir_p = p / mag_p;
					float2 dir_v = v / mag_v;

					// Clamp magnitude
					mag_v = clamp(mag_v, 5.0, ARROW_TILE_SIZE * 0.5);

					// Arrow tip location
					v = dir_v * mag_v;

					float dist;

					if (ARROW_STYLE == ARROW_LINE_STYLE) // ARROW_LINE_STYLE
					{
						// Signed distance from a line segment
						// Based on https://www.shadertoy.com/view/ls2GWG
						dist = max(
							(ARROW_SHAFT_THICKNESS * 0.25 + offset * 10) - 
							max(abs(dot(p, float2(dir_v.y, -dir_v.x))), 
								abs(dot(p, dir_v)) - mag_v + ARROW_HEAD_LENGTH * 0.5),
							// Arrow head
							min(0.0, dot(v - p, dir_v) - cos(ARROW_HEAD_ANGLE * 0.5) * length(v - p)) * (1.0+ headoffset) +
							min(0.0, dot(p, dir_v) + ARROW_HEAD_LENGTH - mag_v - headoffset * 0.2)
						);
					}
					else // V style
					{
						dist = min(0.0, mag_v - mag_p) * 2.0 +
							   min(0.0, dot(normalize(v - p), dir_v) - cos(ARROW_HEAD_ANGLE * 0.5)) * 2.0 * length(v - p) +
							   min(0.0, dot(p, dir_v) + 1.0) +
							   min(0.0, cos(ARROW_HEAD_ANGLE * 0.5) - dot(normalize(v * 0.33 - p), dir_v)) * mag_v * 0.8;
					}

					return saturate(1.0 + dist); // clamp between 0 and 1
				}
				else
				{
					// Center of the pixel is always on the arrow
					return max(0.0, 1.2 - mag_p);
				}
			}

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD1;
                float3 worldPos : TEXCOORD2;

            };

            static const uint quadIndices[6] = { 0, 1, 2, 2, 3, 0 };

		    v2f vert(uint vid : SV_VertexID)
		    {
			    v2f o;

                int gridID = vid / 6; 
                int vertIndex = vid % 6;
                int vertexID = quadIndices[vertIndex]; 
                
                uint topBit = vertexID >> 1;
                uint botBit = (vertexID & 1);
                float x = topBit;
                float y = 1 - (topBit + botBit) & 1; 

			    o.vertex = float4(x, y, 0, 1);

                uint gridX = gridID % (uint)_GridSize.x;
                uint gridY = gridID / (uint)_GridSize.x;

                // Calculate position in the world based on grid size and quad size
                float xPos = (float)(gridX);
				float yPos = (float)(gridY);

				o.vertex.x += xPos;
				o.vertex.y += yPos;

				o.vertex.xy /= _GridSize;
				o.vertex.xy = (o.vertex.xy * 2 - 1) * 0.5f;

				o.uv = o.vertex.xy / 0.5f;

				o.vertex.xy *= _GridDim;

				o.vertex.xyz = o.vertex.xzy;
				o.vertex.xyz += _Position;

				#if _FLUID_UNITY_TERRAIN
					float2 heightUV = ((o.vertex.xz - (_TerrainPosition.xz + _TerrainScale.xz * 0.5)) / (_TerrainScale.xz * 0.5)) * 0.5 + 0.5;
					float height = UnpackHeightmap(_Heightmap.SampleLevel(sampler_Heightmap, heightUV, 0));
				#else
					float2 heightUV = ((o.vertex.xz - (_TerrainPosition.xz)) / (_TerrainScale.xz * 0.5)) * 0.5 + 0.5;
					float height = dot(_Heightmap.SampleLevel(sampler_Heightmap, heightUV, 0), float4(1,1,1,1));
				#endif

				o.vertex.y = height * _TerrainScale.y + _TerrainPosition.y ;

				o.worldPos = o.vertex.xyz;
				o.vertex = UnityObjectToClipPos(float4(o.worldPos, 1.0));
                
				o.vertex.z += (o.vertex.z / o.vertex.w) * 1;
                
				return o;
			}

			fixed4 frag (v2f i) : SV_Target
			{	
				float distance = lerp(DistanceCircle(i.uv), DistanceBox(i.uv), _Shape == 2);
				clip(1-distance);

				float2 fragCoord = i.worldPos.xz * 50;

				float2 tileCenter = arrowTileCenterCoord(fragCoord);
				float2 fieldValue = field(tileCenter, i.uv)  * ARROW_TILE_SIZE * 0.4;

				float arrowAlpha = arrow(fragCoord, fieldValue, 0.05, 0);
				float arrowEdge = arrow(fragCoord, fieldValue, 0.0, 1.0);
				float3 color = float3(field(fragCoord, i.uv)  * 0.5 + 0.5,0);
				color = lerp(1-color,color,  arrowEdge);

				return float4(color, arrowAlpha);
			}
			ENDHLSL
		}
	}
}