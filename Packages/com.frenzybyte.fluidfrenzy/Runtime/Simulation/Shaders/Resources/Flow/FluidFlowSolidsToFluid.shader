Shader "Hidden/FluidFrenzy/Simulation/Flow/SolidsToFluid"
{
    SubShader
    {
		HLSLINCLUDE

		#define USETEXTURE2D
		#include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Simulation/Shaders/Resources/FluidSimulationCommon.hlsl"
		#include "Packages/com.frenzybyte.fluidfrenzy/Runtime/Simulation/Shaders/Resources/Flow/FluidFlowCommon.hlsl"

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


		float3 _Velocity;
		float _IncreaseStrength;
		float _IncreaseExponent;
		float3 _WorldPos;
		float _NumSteps;

		// New structs for sphere intersection
		struct SphereIntersectionPoint
		{
			float3 position; // World position of the intersection
			float3 normal;   // Normal of the sphere at the intersection point
			bool isValid;    // True if a valid intersection occurred
		};

		struct fluidout
		{
			float4 velocity : SV_Target0; // .xy for horizontal velocity components
			float4 height : SV_Target1;   // .x for height, .y (or other channels) could be for other data
			float4 normal : SV_Target2;   // .x for height, .y (or other channels) could be for other data
		};


		/// <summary>
		/// Calculates ray-sphere intersection distances.
		/// </summary>
		/// <param name="ro">Ray origin.</param>
		/// <param name="rd">Normalized ray direction.</param>
		/// <param name="sphereCenter">Sphere center position.</param>
		/// <param name="sphereRadius">Sphere radius.</param>
		/// <returns>A float2 where x is the closer positive distance (t0) and y is the further positive distance (t1).
		///          If no intersection, or both intersections are behind the ray origin, returns float2(0.0, 0.0).
		///          If ray starts inside sphere, t0 will be the exit point, and t1 will be the same as t0 (for consistency).</returns>
		float2 RaySphereIntersectDistances(float3 ro, float3 rd, float3 sphereCenter, float sphereRadius)
		{
			float3 oc = ro - sphereCenter;
			float b = dot(oc, rd);
			float c = dot(oc, oc) - sphereRadius * sphereRadius;
			float h = b * b - c;

			if (h < 0.0) return float2(0.0, 0.0); // No real intersection

			h = sqrt(h);
			float t0 = -b - h;
			float t1 = -b + h;

			// Ensure we only return positive distances.
			// If t0 is negative, it means the ray starts inside the sphere, or the intersection is behind the origin.
			// If t0 is negative but t1 is positive, take t1 as the only valid forward intersection.
			if (t0 <= 0.0 && t1 <= 0.0) return float2(0.0, 0.0); // Both behind ray origin
			if (t0 <= 0.0) t0 = t1; // Ray starts inside sphere, use the exit point as the "entry" for fluid interaction.

			return float2(t0, t1);
		}

		/// <summary>
		/// Calculates the world position and normal of a sphere intersection point.
		/// </summary>
		/// <param name="ro">Ray origin.</param>
		/// <param name="rd">Normalized ray direction.</param>
		/// <param name="t">Intersection distance along the ray.</param>
		/// <param name="sphereCenter">Sphere center position.</param>
		/// <returns>A SphereIntersectionPoint struct. isValid will be false if t is not positive.</returns>
		SphereIntersectionPoint GetSphereIntersectionPoint(float3 ro, float3 rd, float t, float3 sphereCenter)
		{
			SphereIntersectionPoint result;
			result.isValid = false;

			if (t <= 0.0) return result; // Only consider positive distances

			result.position = ro + rd * t;
			result.normal = normalize(result.position - sphereCenter);
			result.isValid = true;
			return result;
		}



		ENDHLSL

        Pass
        {
			Name "ApplyDirection"
			Cull Off ZWrite Off ZTest Always
			Blend One One, One One
			Blend 1 One One, One One
			Blend 2 One One, One One
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

			// This new function encapsulates the logic for handling a single intersection point.
			// It calculates the fluid interaction (height and velocity changes) and accumulates
			// the results into the 'output' struct.
			void ApplyFluidCoupling(
				in SphereIntersectionPoint intersection,
				in float totalFluidHeight,
				in float2 currentFluidVelocityXZ,
				in float3 solidVelocity,
				inout fluidout output)
			{
				// Exit early if the intersection point is not valid.
				if (!intersection.isValid)
				{
					return;
				}

				// Calculate the depth of the intersection point below the fluid surface.
				float depthBelowSurface = totalFluidHeight - intersection.position.y;

				// Apply fluid coupling logic only if the point is submerged.
				if (depthBelowSurface > 0.0f)
				{
				output.normal.xyz = intersection.normal;

					// Define constants based on the paper's recommendations.
					const float lambdaDecayRate = 1.0f; // λ = 1
					const float Cdis = 1.0f;            // C_dis = 1.0 (Controls height effect)
					const float Cadapt = 1.0f;          // C_adapt = 0.2 (Controls velocity effect)
					const float subTriangleArea = CELLSIZE_LAYER1.x * CELLSIZE_LAYER1.y; // Assuming per-pixel contribution.

					// Calculate the decay factor based on submergence depth.
					float decayFactor = exp(lambdaDecayRate * (-depthBelowSurface));
					float cellSizeSquared = CELLSIZE_LAYER1.x * CELLSIZE_LAYER1.y;

					// Get the normal of the sphere at the intersection point.
					float3 solidDirectionNormalized = intersection.normal;

					// Calculate the relative velocity between the solid and the fluid.
					float3 relativeVelocity = solidVelocity - float3(currentFluidVelocityXZ.x, 0.0f, currentFluidVelocityXZ.y);

					// Calculate the volume of fluid displaced by this point.
					float displacedVolume = dot(solidDirectionNormalized, relativeVelocity) * subTriangleArea * _VelocityDeltaTime;
        
					// Accumulate the change in height.
					output.height.x += decayFactor * (displacedVolume / (_NumSteps * cellSizeSquared)) * Cdis;

					// Calculate the coefficient for the velocity update.
					float sgn = (solidDirectionNormalized.y > 0.0f) ? 1.0f : -1.0f;
					float velocityCoeff = min(1.0f, decayFactor * Cadapt * (depthBelowSurface / totalFluidHeight) * sgn * (_VelocityDeltaTime / cellSizeSquared));

					// Accumulate the change in velocity.
					output.velocity.xy += velocityCoeff * (solidVelocity.xz - currentFluidVelocityXZ);

					//output.debug.xyz = float3(output.height.x, -output.height.x, 0); //float3(intersection.normal);
					//output.debug = 1;//float3(output.height.x, -output.height.x, 0); //float3(intersection.normal);
				}
			}

			// The main fragment shader, now cleaned up to use the helper function.
			fluidout frag(v2f i)
			{
				// Convert screen-space coordinates to normalized UV for fluid grid sampling.
				float2 sampleUV = i.vertex.xy * _VelocityField_TexelSize.xy;// * 0.5 + 0.5;
				float2 padding = _VelocityField_TexelSize.xy * 8;
				sampleUV = sampleUV * (1 + padding*2) - padding;
				// Get the world XZ position of the current fluid grid cell.
				float2 fluidCellWorldXZ = SimulationUVToLocalPosition(sampleUV).xz;

				// Sample current fluid height and terrain height at this fragment's position.
				float fluidSurfaceHeight = SampleFluidHeight(sampleUV).x;
				if(fluidSurfaceHeight <= 0)
					discard;

				float terrainBaseHeight = SampleTerrainHeight(sampleUV);
				float totalFluidHeight = fluidSurfaceHeight + terrainBaseHeight;

				// Sample current fluid velocity (u, v) from the fluid grid.
				float2 currentFluidVelocityXZ = SampleVelocity(sampleUV).xy;
				float3 _SphereVelocity = _Velocity * CELLSIZE_SCALE_LAYER1 * 50;
				float3 _SphereCenter = _WorldPos;
				float _SphereRadius = 5;

				// Define a vertical ray for intersection with the sphere.
				float3 rayOrigin = float3(fluidCellWorldXZ.x, _SphereCenter.y + _SphereRadius + 0.1f, fluidCellWorldXZ.y);
				float3 rayDirection = float3(0.0f, -1.0f, 0.0f);

				// Find intersection distances with the sphere.
				float2 intersectionDistances = RaySphereIntersectDistances(rayOrigin, rayDirection, _SphereCenter, _SphereRadius);

				// Initialize total output for fluid changes.
				fluidout output = (fluidout)0;

				// Process the top intersection point.
				SphereIntersectionPoint topIntersection = GetSphereIntersectionPoint(rayOrigin, rayDirection, intersectionDistances.x, _SphereCenter);
				ApplyFluidCoupling(topIntersection, totalFluidHeight, currentFluidVelocityXZ, _SphereVelocity, output);

				// Process the bottom intersection point if it's distinct and valid.
				if (intersectionDistances.y > 0.0f && intersectionDistances.y != intersectionDistances.x)
				{
					SphereIntersectionPoint bottomIntersection = GetSphereIntersectionPoint(rayOrigin, rayDirection, intersectionDistances.y, _SphereCenter);
					ApplyFluidCoupling(bottomIntersection, totalFluidHeight, currentFluidVelocityXZ, _SphereVelocity, output);
				}
				//output.velocity.xy = 1;
				return output;
			}



            ENDHLSL
        }

        Pass
        {
			Name "ApplyDirectionFlux"
			Cull Off ZWrite Off ZTest Always
			Blend One One, One One
			Blend 1 One One, One One
			Blend 2 One One, One One
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

			// This new function encapsulates the logic for handling a single intersection point.
			// It calculates the fluid interaction (height and velocity changes) and accumulates
			// the results into the 'output' struct.
			void ApplyFluidCoupling(
				in SphereIntersectionPoint intersection,
				in float totalFluidHeight,
				in float2 currentFluidVelocityXZ,
				in float3 solidVelocity,
				inout fluidout output)
			{
				// Exit early if the intersection point is not valid.
				if (!intersection.isValid)
				{
					return;
				}

				output.normal.xy = 1;

				// Calculate the depth of the intersection point below the fluid surface.
				float depthBelowSurface = totalFluidHeight - intersection.position.y;

				// Apply fluid coupling logic only if the point is submerged.
				if (depthBelowSurface > 0.0f)
				{
					output.normal.xyz = intersection.normal;

					// Define constants based on the paper's recommendations.
					const float lambdaDecayRate = 1.0f; // λ = 1
					const float Cdis = 1.0f;            // C_dis = 1.0 (Controls height effect)
					const float Cadapt = 0.2f;          // C_adapt = 0.2 (Controls velocity effect)
					const float subTriangleArea = WORLD_CELLSIZE_LAYER1.x * WORLD_CELLSIZE_LAYER1.x; // Assuming per-pixel contribution.

					// Calculate the decay factor based on submergence depth.
					float decayFactor = exp(lambdaDecayRate * (-depthBelowSurface));
					float cellSizeSquared = WORLD_CELLSIZE_LAYER1.x * WORLD_CELLSIZE_LAYER1.x;

					// Get the normal of the sphere at the intersection point.
					float3 solidDirectionNormalized = normalize(intersection.normal);

					// Calculate the relative velocity between the solid and the fluid.
					float3 relativeVelocity = solidVelocity - float3(currentFluidVelocityXZ.x, 0.0f, currentFluidVelocityXZ.y);

					// Calculate the volume of fluid displaced by this point.
					float displacedVolume = dot(solidDirectionNormalized, solidVelocity) * subTriangleArea * _VelocityDeltaTime ;
        
					// Accumulate the change in height.
					output.height.x += max(0,decayFactor * (displacedVolume / (_NumSteps * cellSizeSquared)) * Cdis);

					// Calculate the coefficient for the velocity update.
					float sgn = (solidDirectionNormalized.y > 0.0f) ? 1.0f : -1.0f;
					float velocityCoeff = min(1.0f, decayFactor * Cadapt * (depthBelowSurface / totalFluidHeight) * sgn * (_VelocityDeltaTime / cellSizeSquared));

					// Accumulate the change in velocity.
					output.velocity.x += max(0, velocityCoeff * length(solidVelocity.xz - currentFluidVelocityXZ));

					//output.debug.xyz = float3(output.height.x, -output.height.x, 0); //float3(intersection.normal);
					//output.debug = 1;//float3(output.height.x, -output.height.x, 0); //float3(intersection.normal);
				}
			}

			// The main fragment shader, now cleaned up to use the helper function.
			fluidout frag(v2f i)
			{
				// Convert screen-space coordinates to normalized UV for fluid grid sampling.
				float2 sampleUV = i.vertex.xy * _FluidHeightField_TexelSize.xy;// * 0.5 + 0.5;
				float2 padding = _FluidHeightField_TexelSize.xy * 3;
				sampleUV = sampleUV * (1 + padding*2) - padding;
				// Get the world XZ position of the current fluid grid cell.
				float2 fluidCellWorldXZ = SimulationUVToLocalPosition(sampleUV).xz;

				// Sample current fluid height and terrain height at this fragment's position.
				float fluidSurfaceHeight = SampleFluidHeight(sampleUV).x;
				//if(fluidSurfaceHeight <= 0)
				//	discard;
				float terrainBaseHeight = SampleTerrainHeight(sampleUV);
				float totalFluidHeight = fluidSurfaceHeight + terrainBaseHeight;

				// Sample current fluid velocity (u, v) from the fluid grid.
				float2 currentFluidVelocityXZ = 0;//SampleVelocity(sampleUV);
				float3 _SphereVelocity = _Velocity * 25 * 4;
				float3 _SphereCenter = _WorldPos;
				float _SphereRadius = 5;

				// Define a vertical ray for intersection with the sphere.
				float3 rayOrigin = float3(fluidCellWorldXZ.x, _SphereCenter.y + _SphereRadius + 0.1f, fluidCellWorldXZ.y);
				float3 rayDirection = float3(0.0f, -1.0f, 0.0f);

				// Find intersection distances with the sphere.
				float2 intersectionDistances = RaySphereIntersectDistances(rayOrigin, rayDirection, _SphereCenter, _SphereRadius);

				// Initialize total output for fluid changes.
				fluidout output = (fluidout)0;
				output.normal = 1;

				// Process the top intersection point.
				SphereIntersectionPoint topIntersection = GetSphereIntersectionPoint(rayOrigin, rayDirection, intersectionDistances.x, _SphereCenter);
				ApplyFluidCoupling(topIntersection, totalFluidHeight, currentFluidVelocityXZ, _SphereVelocity, output);

				// Process the bottom intersection point if it's distinct and valid.
				if (intersectionDistances.y > 0.0f && intersectionDistances.y != intersectionDistances.x)
				{
					SphereIntersectionPoint bottomIntersection = GetSphereIntersectionPoint(rayOrigin, rayDirection, intersectionDistances.y, _SphereCenter);
					ApplyFluidCoupling(bottomIntersection, totalFluidHeight, currentFluidVelocityXZ, _SphereVelocity, output);
				}
				return output;
			}



            ENDHLSL
        }
    }
}
