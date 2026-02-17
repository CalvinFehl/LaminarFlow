Shader "Hidden/FluidFrenzy/ProceduralObstacle"
{
	SubShader
	{
				HLSLINCLUDE

		#include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Simulation/Shaders/Resources/FluidSimulationCommon.hlsl"

		float4x4 unity_ObjectToWorld;

		struct v2f
		{
			float2 uv : TEXCOORD0;
			float3 localPos : TEXCOORD1;
			float4 vertex : SV_POSITION;
		};

		#define SHAPE_SPHERE 0
		#define SHAPE_BOX 1
		#define SHAPE_CYLINDER 2
		#define SHAPE_CAPSULE 3
		#define SHAPE_ELLIPSOID 4
		#define SHAPE_CONE 5
		#define SHAPE_HEX 6
		#define SHAPE_WEDGE 7

		float3 _Size; 
		float2 _TexelSize; 
		float4x4 _Transform; // Sim Local Space to Obstacle Local Space
		float4 _SimParams;   // x = Width, y = Height
		float4 _ProjectionParams;
		v2f vert(uint vid : SV_VertexID)
		{
			v2f o;
			float4 rawVert = GetQuadVertexPosition(vid); 
			
			// Standard Quad is 0..1, center it to -0.5..0.5
			rawVert.xy -= 0.5f; 
			rawVert.z = 0; 
			rawVert.w = 1;
			
			float4 localPos = mul(unity_ObjectToWorld, rawVert);
			float2 clipPos;
			clipPos.x = localPos.x / (_SimParams.x * 0.5);
			clipPos.y = localPos.z / (_SimParams.y * 0.5);
			
			#if UNITY_UV_STARTS_AT_TOP
			if (_ProjectionParams.x < 0) clipPos.y *= -1;
			#endif

			o.vertex = float4(clipPos.x, clipPos.y, 0, 1);
			o.localPos = localPos.xyz;
			o.uv = rawVert.xy + 0.5f;
			return o;
		}

		// --- SPHERE ---
		float sphIntersect(float3 ro, float3 rd, float r) {
			float b = dot(ro, rd);
			float c = dot(ro, ro) - r*r;
			float h = b*b - c;
			if(h<0.0) return -1.0;
			return -b - sqrt(h);
		}

		// --- BOX ---
		float boxIntersect(float3 ro, float3 rd, float3 rad) {
			float3 d = rd + 1e-6; 
			float3 m = 1.0/d; float3 n = m*ro; float3 k = abs(m)*rad;
			float3 t1 = -n - k; float3 t2 = -n + k;
			float tN = max(max(t1.x, t1.y), t1.z);
			float tF = min(min(t2.x, t2.y), t2.z);
			if(tN > tF || tF < 0.0) return -1.0;
			return tN;
		}

		float cylIntersect(float3 ro, float3 rd, float r, float h) {
			float3 d = rd; float3 p = ro;
			float a = dot(d.xy, d.xy);
			float b = 2.0*dot(p.xy, d.xy);
			float c = dot(p.xy, p.xy) - r*r;
			float t = -1.0;
			if(a > 1e-6) {
				float k = b*b - 4.0*a*c;
				if(k>=0) {
					float t0 = (-b - sqrt(k)) / (2.0*a);
					if(abs(p.z + t0*d.z) <= h) t = t0;
				}
			}
			if(abs(d.z) > 1e-6) {
				float t1 = (h - p.z)/d.z;
				float t2 = (-h - p.z)/d.z;
				if(t1>0 && dot((p+t1*d).xy, (p+t1*d).xy) <= r*r) t = (t<0||t1<t)?t1:t;
				if(t2>0 && dot((p+t2*d).xy, (p+t2*d).xy) <= r*r) t = (t<0||t2<t)?t2:t;
			}
			return t;
		}

		float capIntersect(float3 ro, float3 rd, float r, float h) {
			float t = cylIntersect(ro, rd, r, h);
			float t1 = sphIntersect(ro - float3(0,0, h), rd, r);
			float t2 = sphIntersect(ro - float3(0,0,-h), rd, r);
			if(t1>0) t = (t<0||t1<t)?t1:t;
			if(t2>0) t = (t<0||t2<t)?t2:t;
			return t;
		}

		float ellIntersect(float3 ro, float3 rd, float3 r) {
			float3 o = ro/r; float3 d = rd/r;
			float a = dot(d,d); float b = dot(o,d); float c = dot(o,o)-1.0;
			float h = b*b - a*c;
			if(h<0) return -1.0;
			return (-b - sqrt(h))/a;
		}

		float hexIntersect(float3 ro, float3 rd, float ra, float he)
		{
			const float ks3 = 0.866025;

			const float3 n1 = float3( 1.0, 0.0, 0.0);
			const float3 n2 = float3( 0.5, ks3, 0.0);
			const float3 n3 = float3(-0.5, ks3, 0.0);
			const float3 n4 = float3( 0.0, 0.0, 1.0);

			float3 t1 = float3((float2(ra,-ra)-dot(ro,n1))/dot(rd,n1), 1.0);
			float3 t2 = float3((float2(ra,-ra)-dot(ro,n2))/dot(rd,n2), 1.0);
			float3 t3 = float3((float2(ra,-ra)-dot(ro,n3))/dot(rd,n3), 1.0);
			float3 t4 = float3((float2(he,-he)-dot(ro,n4))/dot(rd,n4), 1.0);

			if( t1.y<t1.x ) t1=float3(t1.yx,-1.0);
			if( t2.y<t2.x ) t2=float3(t2.yx,-1.0);
			if( t3.y<t3.x ) t3=float3(t3.yx,-1.0);
			if( t4.y<t4.x ) t4=float3(t4.yx,-1.0);

			float tN = t1.x;
			if( t2.x > tN ) tN = t2.x;
			if( t3.x > tN ) tN = t3.x;
			if( t4.x > tN ) tN = t4.x;

			float tF = min(min(t1.y,t2.y),min(t3.y,t4.y));

			if( tN > tF || tF < 0.0) return -1.0;

			return tN;
		}

		float coneIntersect(float3 ro, float3 rd, float r1, float r2, float h)
		{
			float3 pa = float3(0.0, 0.0, -h);
			float3 pb = float3(0.0, 0.0,  h);
			float ra = r1;
			float rb = r2;

			float3 ba = pb - pa;
			float3 oa = ro - pa;
			float3 ob = ro - pb;

			float m0 = dot(ba, ba);
			float m1 = dot(oa, ba);
			float m2 = dot(ob, ba);
			float m3 = dot(rd, ba);

			// Caps
			if (m1 < 0.0) 
			{ 
				float3 temp = oa * m3 - rd * m1;
				if (dot(temp, temp) < (ra * ra * m3 * m3)) 
					return -m1 / m3; 
			}
			else if (m2 > 0.0) 
			{ 
				float3 temp = ob * m3 - rd * m2;
				if (dot(temp, temp) < (rb * rb * m3 * m3)) 
					return -m2 / m3; 
			}

			// Body
			float m4 = dot(rd, oa);
			float m5 = dot(oa, oa);
			float rr = ra - rb;
			float hy = m0 + rr * rr;

			float k2 = m0 * m0 - m3 * m3 * hy;
			float k1 = m0 * m0 * m4 - m1 * m3 * hy + m0 * ra * (rr * m3 * 1.0);
			float k0 = m0 * m0 * m5 - m1 * m1 * hy + m0 * ra * (rr * m1 * 2.0 - m0 * ra);

			float disc = k1 * k1 - k2 * k0;
			if (disc < 0.0) return -1.0;

			float t = (-k1 - sqrt(disc)) / k2;

			float y = m1 + t * m3;
			if (y > 0.0 && y < m0)
			{
				return t;
			}

			return -1.0;
		}

		float wedgeIntersect(float3 ro, float3 rd, float3 s)
		{
			float3 m = 1.0 / (rd + 1e-6); 
			float3 z = float3(rd.x >= 0.0 ? 1.0 : -1.0, rd.y >= 0.0 ? 1.0 : -1.0, rd.z >= 0.0 ? 1.0 : -1.0);
			float3 k = s * z;
			
			float3 t1 = (-ro - k) * m;
			float3 t2 = (-ro + k) * m;
			
			float tn = max(max(t1.x, t1.y), t1.z);
			float tf = min(min(t2.x, t2.y), t2.z);
			
			if (tn > tf || tf < 0.0) return -1.0;

			float k1 = s.y * ro.x - s.x * ro.y;
			float k2 = s.x * rd.y - s.y * rd.x;
			float tp = k1 / (k2 + 1e-6);

			if (k1 > tn * k2) return tn;
			if (tp > tn && tp < tf) return tp;

			return -1.0;
		}

		float4 frag_core(v2f i, int shape, int range)
		{
			const float offset = 1000.0;
			float h = 0;
			int samples = 0;

			// Ray Direction in Simulation Space is always "Down" (0, -1, 0)
			// because we are rendering the heightmap top-down relative to the Sim Grid.
			float3 simRd = float3(0, -1, 0);

			for (int y = -range; y <= range; y++) {
				for (int x = -range; x <= range; x++) {
					
					// Calculate Sample Point on Sim Grid (Sim Local Space)
					float2 sampleOffset = float2(x, y) * _TexelSize;
					float3 simPosOnPlane = i.localPos;
					simPosOnPlane.x += sampleOffset.x;
					simPosOnPlane.z += sampleOffset.y; // Z is Y in texture/grid terms

					// Set Ray Origin above the plane
					float3 simRo = simPosOnPlane;
					simRo.y += offset;

					// Transform Ray to Obstacle Local Space
					// This effectively handles the relative position/rotation/scale of the obstacle
					float3 obsRo = mul(_Transform, float4(simRo, 1)).xyz;
					float3 obsRd = mul(_Transform, float4(simRd, 0)).xyz;

					// Normalize Direction for standard SDF
					// The length of obsRd encodes the scale difference (if sim is scaled differently)
					float scaleFactor = length(obsRd);
					float3 d = obsRd / scaleFactor; 

					float t = -1.0;

					if (shape == SHAPE_SPHERE) t = sphIntersect(obsRo, d, _Size.x);
					else if (shape == SHAPE_BOX) t = boxIntersect(obsRo, d, _Size);
					else if (shape == SHAPE_CYLINDER) t = cylIntersect(obsRo, d, _Size.x, _Size.y);
					else if (shape == SHAPE_CAPSULE) t = capIntersect(obsRo, d, _Size.x, _Size.y);
					else if (shape == SHAPE_ELLIPSOID) t = ellIntersect(obsRo, d, _Size);
					else if (shape == SHAPE_HEX) t = hexIntersect(obsRo, d, _Size.x, _Size.y);
					else if (shape == SHAPE_CONE) t = coneIntersect(obsRo, d, _Size.x, _Size.y, _Size.z);
					else if (shape == SHAPE_WEDGE) t = wedgeIntersect(obsRo, d, _Size);
					
					float simHeight = 0;
					if (t > 0.0) {
						// 't' is distance in Obstacle Space.
						// Convert back to Sim Space Distance: t / scaleFactor
						float dist = t / scaleFactor;
						simHeight = offset - dist;
					}
					h += max(0, simHeight);
					samples++;
				}
			}
			return h / samples;
		}
		ENDHLSL

		Cull Off ZWrite Off ZTest Always
		ColorMask R
		BlendOp Max

		// SHARP Passes 0 to 7

		Pass
		{
			Name "Sphere_Sharp"
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			float4 frag(v2f i) : SV_Target
			{
				return frag_core(i, SHAPE_SPHERE, 0);
			}
			ENDHLSL
		}

		Pass
		{
			Name "Box_Sharp"
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			float4 frag(v2f i) : SV_Target
			{
				return frag_core(i, SHAPE_BOX, 0);
			}
			ENDHLSL
		}

		Pass
		{
			Name "Cylinder_Sharp"
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			float4 frag(v2f i) : SV_Target
			{
				return frag_core(i, SHAPE_CYLINDER, 0);
			}
			ENDHLSL
		}

		Pass
		{
			Name "Capsule_Sharp"
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			float4 frag(v2f i) : SV_Target
			{
				return frag_core(i, SHAPE_CAPSULE, 0);
			}
			ENDHLSL
		}

		Pass
		{
			Name "Ellipsoid_Sharp"
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			float4 frag(v2f i) : SV_Target
			{
				return frag_core(i, SHAPE_ELLIPSOID, 0);
			}
			ENDHLSL
		}

		Pass
		{
			Name "Cone_Sharp"
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			float4 frag(v2f i) : SV_Target
			{
				return frag_core(i, SHAPE_CONE, 0);
			}
			ENDHLSL
		}

		Pass
		{
			Name "Hex_Sharp"
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			float4 frag(v2f i) : SV_Target
			{
				return frag_core(i, SHAPE_HEX, 0);
			}
			ENDHLSL
		}

		Pass
		{
			Name "Wedge_Sharp"
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			float4 frag(v2f i) : SV_Target
			{
				return frag_core(i, SHAPE_WEDGE, 0);
			}
			ENDHLSL
		}

		// SMOOTH PASSES 8 to 15
		Pass
		{
			Name "Sphere_Smooth"
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			float4 frag(v2f i) : SV_Target
			{
				return frag_core(i, SHAPE_SPHERE, 2);
			}
			ENDHLSL
		}

		Pass
		{
			Name "Box_Smooth"
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			float4 frag(v2f i) : SV_Target
			{
				return frag_core(i, SHAPE_BOX, 2);
			}
			ENDHLSL
		}

		Pass
		{
			Name "Cylinder_Smooth"
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			float4 frag(v2f i) : SV_Target
			{
				return frag_core(i, SHAPE_CYLINDER, 2);
			}
			ENDHLSL
		}

		Pass
		{
			Name "Capsule_Smooth"
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			float4 frag(v2f i) : SV_Target
			{
				return frag_core(i, SHAPE_CAPSULE, 2);
			}
			ENDHLSL
		}

		Pass
		{
			Name "Ellipsoid_Smooth"
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			float4 frag(v2f i) : SV_Target
			{
				return frag_core(i, SHAPE_ELLIPSOID, 2);
			}
			ENDHLSL
		}

		Pass
		{
			Name "Cone_Smooth"
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			float4 frag(v2f i) : SV_Target
			{
				return frag_core(i, SHAPE_CONE, 2);
			}
			ENDHLSL
		}

		Pass
		{
			Name "Hex_Smooth"
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			float4 frag(v2f i) : SV_Target
			{
				return frag_core(i, SHAPE_HEX, 2);
			}
			ENDHLSL
		}

		Pass
		{
			Name "Wedge_Smooth"
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			float4 frag(v2f i) : SV_Target
			{
				return frag_core(i, SHAPE_WEDGE, 2);
			}
			ENDHLSL
		}

	}
}