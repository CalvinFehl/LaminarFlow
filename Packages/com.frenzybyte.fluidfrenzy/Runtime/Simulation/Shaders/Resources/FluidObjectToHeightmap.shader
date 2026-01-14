Shader "Hidden/FluidFrenzy/ObjectToHeightmap"
{
	SubShader
	{
		ColorMask R
		BlendOp Max
		Cull Off
		ZWrite Off
		ZTest Always

		Pass
		{
			Name "Standard"
			CGPROGRAM
			#pragma vertex vert_standard
			#pragma fragment frag_standard
			#pragma target 3.0 // Geometry shaders require a higher shader model

			#include "UnityCG.cginc"
            
			struct appdata_std
			{
				float4 vertex : POSITION;
			};

			struct v2f_std
			{
				float4 vertex : SV_POSITION;
				float4 worldPos : TEXCOORD0;
			};

			float3 _SimulationPositionWS;

			v2f_std vert_standard (appdata_std v)
			{
				v2f_std o;
				o.worldPos = mul(unity_ObjectToWorld, v.vertex);
				o.vertex = mul(UNITY_MATRIX_VP, o.worldPos);
				return o;
			}

			float4 frag_standard (v2f_std i) : SV_Target
			{
				return i.worldPos.y - _SimulationPositionWS.y;
			}
			ENDCG
		}

		Pass
		{
			Conservative True

			Name "Conservative"
			CGPROGRAM
			#pragma vertex vert_geom
			#pragma geometry geom
			#pragma fragment frag_geom
			#pragma target 4.0 // Geometry shaders require a higher shader model

			#include "UnityCG.cginc"
            
			struct v2g // Vertex to Geometry
			{
				float4 localPos : TEXCOORD0;
			};

			struct g2f // Geometry to Fragment
			{
				float4 pos : SV_POSITION;
				float3 worldPos0 : TEXCOORD0;
				float3 worldPos1 : TEXCOORD1;
				float3 worldPos2 : TEXCOORD2;
				float3 pixelWorldPos : TEXCOORD3; 
			};
            
			float3 _SimulationPositionWS;

			v2g vert_geom (float4 vertex : POSITION)
			{
				v2g o;
				o.localPos = vertex;
				return o;
			}

			[maxvertexcount(3)]
			void geom(triangle v2g input[3], inout TriangleStream<g2f> triStream)
			{
				// Transform all three vertices into world space ONCE.
				float3 worldPos0 = mul(unity_ObjectToWorld, input[0].localPos).xyz;
				float3 worldPos1 = mul(unity_ObjectToWorld, input[1].localPos).xyz;
				float3 worldPos2 = mul(unity_ObjectToWorld, input[2].localPos).xyz;

				g2f o;
				// Assign the constant (non-interpolated) triangle vertex positions.
				o.worldPos0 = worldPos0;
				o.worldPos1 = worldPos1;
				o.worldPos2 = worldPos2;

				// Output the three vertices of the new triangle
				// Each vertex carries the data for the entire triangle.
				o.pixelWorldPos = worldPos0;
				o.pos = mul(UNITY_MATRIX_VP, float4(worldPos0, 1.0));
				triStream.Append(o);

				o.pixelWorldPos = worldPos1;
				o.pos = mul(UNITY_MATRIX_VP, float4(worldPos1, 1.0));
				triStream.Append(o);

				o.pixelWorldPos = worldPos2;
				o.pos = mul(UNITY_MATRIX_VP, float4(worldPos2, 1.0));
				triStream.Append(o);
			}

			// Finds the closest point on the 2D line segment (ab) to the point (p).
			float2 ClosestPointOnLineSegment(float2 p, float2 a, float2 b)
			{
				float2 ab = b - a;
				float t = saturate(dot(p - a, ab) / dot(ab, ab));
				return a + t * ab;
			}

			// Finds the closest point on the 2D triangle (a,b,c) to the point (p).
			float2 ClosestPointOnTriangle(float2 p, float2 a, float2 b, float2 c)
			{
				// Check if p is inside the region of edge ab
				if (dot(p - a, b - a) <= 0.0) return a;
				if (dot(p - b, a - b) <= 0.0) return b;
                
				// Check if p is inside the region of edge ac
				if (dot(p - a, c - a) <= 0.0) return a;
				if (dot(p - c, a - c) <= 0.0) return c;
                
				// Check if p is inside the region of edge bc
				if (dot(p - b, c - b) <= 0.0) return b;
				if (dot(p - c, b - c) <= 0.0) return c;
                
				// If none of the above, the closest point is the projection onto the triangle's plane.
				// Since we are doing a 2D check, the point is inside the triangle.
				return p;
			}

			// Helper function to find the intersection of a vertical ray with a triangle's plane.
			float RayPlaneIntersection(float3 rayOrigin, float3 rayDir, float3 planePoint, float3 planeNormal)
			{
				float denom = dot(planeNormal, rayDir);
				if (abs(denom) > 0.0001f)
				{
					float t = dot(planePoint - rayOrigin, planeNormal) / denom;
					return rayOrigin.y + rayDir.y * t; // Return the world Y-height of the intersection
				}
				return -10000; // Return a very low number if no valid intersection
			}

			float4 frag_geom (g2f i) : SV_Target
			{
				// Project the 3D world points onto the XZ plane for the 2D check.
				float2 p_xz = i.pixelWorldPos.xz;
				float2 a_xz = i.worldPos0.xz;
				float2 b_xz = i.worldPos1.xz;
				float2 c_xz = i.worldPos2.xz;

				// 1. Find the 2D barycentric coordinates of the pixel's center relative to the triangle.
				float3 bary = float3(0,0,0);
				float2 v0 = b_xz - a_xz, v1 = c_xz - a_xz, v2 = p_xz - a_xz;
				float d00 = dot(v0, v0);
				float d01 = dot(v0, v1);
				float d11 = dot(v1, v1);
				float d20 = dot(v2, v0);
				float d21 = dot(v2, v1);
				float denom = d00 * d11 - d01 * d01;
				bary.y = (d11 * d20 - d01 * d21) / denom;
				bary.z = (d00 * d21 - d01 * d20) / denom;
				bary.x = 1.0f - bary.y - bary.z;

				// 2. If the point is outside the triangle, clamp the barycentric coordinates.
				//    This effectively finds the closest point on the triangle.
				if (bary.x < 0 || bary.y < 0 || bary.z < 0)
				{
					float2 closest_ab = ClosestPointOnLineSegment(p_xz, a_xz, b_xz);
					float2 closest_bc = ClosestPointOnLineSegment(p_xz, b_xz, c_xz);
					float2 closest_ca = ClosestPointOnLineSegment(p_xz, c_xz, a_xz);

					float dist_ab = distance(p_xz, closest_ab);
					float dist_bc = distance(p_xz, closest_bc);
					float dist_ca = distance(p_xz, closest_ca);

					if (dist_ab < dist_bc && dist_ab < dist_ca) p_xz = closest_ab;
					else if (dist_bc < dist_ca) p_xz = closest_bc;
					else p_xz = closest_ca;
				}
                
				// 3. We now have a "corrected" XZ coordinate that is guaranteed to be on the triangle's footprint.
				//    Use this corrected coordinate to perform the raycast.
				float3 planeNormal = normalize(cross(i.worldPos1 - i.worldPos0, i.worldPos2 - i.worldPos0));
				float3 rayOrigin = float3(p_xz.x, 10000.0, p_xz.y);
				float3 rayDir = float3(0, -1, 0);

				float trueHeight = RayPlaneIntersection(rayOrigin, rayDir, i.worldPos0, planeNormal);

				return trueHeight - _SimulationPositionWS.y;
			}
			ENDCG
		}
	}

	SubShader
	{
		Tags { "RenderType"="Opaque" }
        
		ColorMask R
		BlendOp Max
		Cull Off
		ZWrite Off
		ZTest Always

		// This pass will be used on platforms that don't support conservative rasterization or geometry shaders.
		Pass
		{
			Name "Standard"
			CGPROGRAM
			#pragma vertex vert_standard
			#pragma fragment frag_standard
			#pragma target 3.0 // Geometry shaders require a higher shader model

			#include "UnityCG.cginc"
            
			struct appdata_std
			{
				float4 vertex : POSITION;
			};

			struct v2f_std
			{
				float4 vertex : SV_POSITION;
				float4 worldPos : TEXCOORD0;
			};

			float3 _SimulationPositionWS;

			v2f_std vert_standard (appdata_std v)
			{
				v2f_std o;
				o.worldPos = mul(unity_ObjectToWorld, v.vertex);
				o.vertex = mul(UNITY_MATRIX_VP, o.worldPos);
				return o;
			}

			float4 frag_standard (v2f_std i) : SV_Target
			{
				return i.worldPos.y - _SimulationPositionWS.y;
			}
			ENDCG
		}
	}

}