using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Collections.LowLevel.Unsafe;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace FluidFrenzy
{
	public static class GraphicsHelpers
	{
		const int kCornerCount = 8;

		public static bool supportsCompute = true;
		static GraphicsHelpers()
        {
			supportsCompute = SystemInfo.supportsComputeShaders;
#if UNITY_2021_1_OR_NEWER
			supportsCompute &= SystemInfo.SupportsRandomWriteOnRenderTextureFormat(RenderTextureFormat.RFloat);
			supportsCompute &= SystemInfo.SupportsRandomWriteOnRenderTextureFormat(RenderTextureFormat.RGFloat);
			supportsCompute &= SystemInfo.SupportsRandomWriteOnRenderTextureFormat(RenderTextureFormat.RHalf);
			supportsCompute &= SystemInfo.SupportsRandomWriteOnRenderTextureFormat(RenderTextureFormat.RGHalf);
#else
			supportsCompute = false;
#endif
		}
		
		public static void CheckMass(RenderTexture rt)
		{
			RenderTexture.active = rt;
			Texture2D massTexture = new Texture2D(rt.width, rt.height, TextureFormat.RGBAFloat, false);

			massTexture.ReadPixels(new Rect(0, 0, massTexture.width, massTexture.height), 0, 0);
			massTexture.Apply();

			Color[] pixels = massTexture.GetPixels();

			float mass = 0;
			for (int y = 1; y < rt.height - 1; y++)
			{
				for (int x = 1; x < rt.width - 1; x++)
				{
					mass += pixels[y * rt.width + x].r;
				}
			}
			Object.Destroy(massTexture);
			Debug.Log(mass);
		}

		public static int DCS(int total, uint threadGroupCnt)
		{
			return (total + (int)threadGroupCnt - 1) / (int)threadGroupCnt;
		}

		public static RenderTexture CreateSimulationRT(int width, int height, RenderTextureFormat format, bool randomReadWrite = false, FilterMode filterMode = FilterMode.Bilinear, TextureWrapMode wrapMode = TextureWrapMode.Clamp, int depth = 0, string name = "")
        {
			RenderTexture rt = new RenderTexture(width, height, depth, format);
			rt.filterMode = filterMode;
			rt.wrapMode = wrapMode;
			rt.name = AutoRenderTextureName(name);
			rt.enableRandomWrite = randomReadWrite & supportsCompute;
			rt.Create();
			return rt;
		}
		public static RenderTexture CreateSimulationRT(int width, int height, GraphicsFormat format, bool randomReadWrite = false, FilterMode filterMode = FilterMode.Bilinear, TextureWrapMode wrapMode = TextureWrapMode.Clamp, int depth = 0, string name = "")
        {
			RenderTexture rt = new RenderTexture(width, height, depth, format);
			rt.filterMode = filterMode;
			rt.wrapMode = wrapMode;
			rt.name = AutoRenderTextureName(name);
			rt.enableRandomWrite = randomReadWrite & supportsCompute;
			rt.Create();
			return rt;
		}

		public static void SafeDestroy(Object obj)
		{
#if UNITY_EDITOR
			if (!EditorApplication.isPlaying)
			{
				Object.DestroyImmediate(obj);
				return;
			}
#endif
			Object.Destroy(obj);
		}

		public static void ReleaseSimulationRT(RenderTexture renderTexture)
		{
			if (renderTexture != null)
			{
				renderTexture.Release();
				SafeDestroy(renderTexture);
			}
		}
		public static string AutoRenderTextureName(string rtName)
		{
			return string.Format("FluidSim-{0}", rtName);
		}


		/// <summary>
		/// Calculates the necessary world XZ size for the blit quad after the modifier has been rotated.
		/// </summary>
		/// <param name="modifierRotation">The rotation of the modifier.</param>
		/// <param name="unrotatedSize">The size of the modifier's unrotated bounding box (X, Y, Z).</param>
		/// <returns>The required XZ dimensions for the axis-aligned bounding box in world space.</returns>
		public static Vector3 CalculateRotatedXZBoundingBoxSize(Quaternion modifierRotation, Vector3 unrotatedSize)
		{
			// Convert the Quaternion to a rotation Matrix
			Matrix4x4 rotationMatrix = Matrix4x4.Rotate(modifierRotation);

			Vector3 h = unrotatedSize * 0.5f;

			float maxWorldX = 0f;
			float maxWorldZ = 0f;

			for (int i = 0; i < 8; i++)
			{
				Vector3 corner = new Vector3(
					((i & 1) == 0) ? h.x : -h.x,
					((i & 2) == 0) ? h.y : -h.y,
					((i & 4) == 0) ? h.z : -h.z
				);

				// Rotate the corner using the Matrix
				Vector3 rotatedCorner = rotationMatrix.MultiplyPoint(corner); // Use MultiplyPoint to be safe

				maxWorldX = Mathf.Max(maxWorldX, Mathf.Abs(rotatedCorner.x));
				maxWorldZ = Mathf.Max(maxWorldZ, Mathf.Abs(rotatedCorner.z));
			}

			return new Vector3(maxWorldX * 2f, 0f, maxWorldZ * 2f);
		}

		public static void CalculateFrustumCornersWorldSpace(Matrix4x4 proj, Matrix4x4 view, Vector4[] corners)
		{
			// Calculate frustum corners in world space.
			Matrix4x4 inv = (proj * view).inverse;
			for (int x = 0; x <= 1; x++)
			{
				for (int y = 0; y <= 1; y++)
				{
					for (int z = 0; z <= 1; z++)
					{
						Vector4 pt = inv * new Vector4(
							2.0f * x - 1.0f,
							2.0f * y - 1.0f,
							2.0f * z - 1.0f,
							1.0f);
						corners[x * 4 + y * 2 + z] = pt / pt.w; // Directly store in array
					}
				}
			}
		}

		public static void CalculateFrustumBoundsWorldSpace(Matrix4x4 proj, Matrix4x4 view, out Vector3 frustumMinBounds, out Vector3 frustumMaxBounds)
		{
			frustumMinBounds = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
			frustumMaxBounds = new Vector3(float.MinValue, float.MinValue, float.MinValue);
			Matrix4x4 inv = (proj * view).inverse;
			for (int x = 0; x <= 1; x++)
			{
				for (int y = 0; y <= 1; y++)
				{
					for (int z = 0; z <= 1; z++)
					{
						Vector4 pt = inv * new Vector4(
							2.0f * x - 1.0f,
							2.0f * y - 1.0f,
							2.0f * z - 1.0f,
							1.0f);

						pt /= pt.w;
						for (int i = 0; i < kCornerCount; i++)
						{
							frustumMinBounds = Vector3.Min(frustumMinBounds, pt);
							frustumMaxBounds = Vector3.Max(frustumMaxBounds, pt);
						}
					}
				}
			}
		}

		private static void GetMinMax(Vector3 point, ref Vector3 min, ref Vector3 max)
        {
			min = Vector3.Min(min, point);
			max = Vector3.Max(max, point);
		}

		public static void CalculateShadowFrustumPlanes(Camera camera, Light light, Vector4[] frustumPlanes)
		{
			// Find min and max bounds for the frustum corners.
			Matrix4x4 shadowDistProjectionMatrix = Matrix4x4.Perspective(camera.fieldOfView, camera.aspect, camera.nearClipPlane, QualitySettings.shadowDistance);
			CalculateFrustumBoundsWorldSpace(shadowDistProjectionMatrix, camera.worldToCameraMatrix, out Vector3 frustumMinBounds, out Vector3 frustumMaxBounds);

			Vector3 minLightSpacePoint = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
			Vector3 maxLightSpacePoint = new Vector3(float.MinValue, float.MinValue, float.MinValue);
			Quaternion inverseLightRotation = Quaternion.Inverse(light.transform.rotation);

			// Calculate light space bounds, center and size.
			GetMinMax(inverseLightRotation * new Vector3(frustumMinBounds.x, frustumMinBounds.y, frustumMinBounds.z), ref minLightSpacePoint, ref maxLightSpacePoint);
			GetMinMax(inverseLightRotation * new Vector3(frustumMinBounds.x, frustumMinBounds.y, frustumMaxBounds.z), ref minLightSpacePoint, ref maxLightSpacePoint);
			GetMinMax(inverseLightRotation * new Vector3(frustumMinBounds.x, frustumMaxBounds.y, frustumMinBounds.z), ref minLightSpacePoint, ref maxLightSpacePoint);
			GetMinMax(inverseLightRotation * new Vector3(frustumMinBounds.x, frustumMaxBounds.y, frustumMaxBounds.z), ref minLightSpacePoint, ref maxLightSpacePoint);
			GetMinMax(inverseLightRotation * new Vector3(frustumMaxBounds.x, frustumMinBounds.y, frustumMinBounds.z), ref minLightSpacePoint, ref maxLightSpacePoint);
			GetMinMax(inverseLightRotation * new Vector3(frustumMaxBounds.x, frustumMinBounds.y, frustumMaxBounds.z), ref minLightSpacePoint, ref maxLightSpacePoint);
			GetMinMax(inverseLightRotation * new Vector3(frustumMaxBounds.x, frustumMaxBounds.y, frustumMinBounds.z), ref minLightSpacePoint, ref maxLightSpacePoint);
			GetMinMax(inverseLightRotation * new Vector3(frustumMaxBounds.x, frustumMaxBounds.y, frustumMaxBounds.z), ref minLightSpacePoint, ref maxLightSpacePoint);
			Vector3 lightSpaceCenter = (maxLightSpacePoint + minLightSpacePoint) * 0.5f;
			Vector3 lightSpaceSize = maxLightSpacePoint - minLightSpacePoint;
			float maxSize = Mathf.Max(lightSpaceSize.x, lightSpaceSize.y);
			float farClipDistance = lightSpaceSize.z * 2;

			// Move the light back from the center of the light bounds.
			Vector3 lightOffset = light.transform.forward * farClipDistance * 0.5f;
			Matrix4x4 lightViewMatrix = Matrix4x4.Inverse(Matrix4x4.TRS(
				(light.transform.rotation * lightSpaceCenter) - lightOffset,
				light.transform.rotation,
				new Vector3(1, 1, -1)
			));

			// Calculate the light's orthographics projection matrix.
			Matrix4x4 projectionMatrix = Matrix4x4.Ortho(-maxSize, maxSize, -maxSize, maxSize, 0.01f, farClipDistance);

			// Calclate light's frustum planes.
			CalculateFrustumPlanes(projectionMatrix * lightViewMatrix, frustumPlanes);
		}

		public static void CalculateFrustumPlanes(Matrix4x4 viewProj, Vector4[] planes, bool normalize = true)
		{
			// Compute planes using the view-projection matrix
			planes[0] = viewProj.GetRow(3) + viewProj.GetRow(0);
			planes[1] = viewProj.GetRow(3) - viewProj.GetRow(0);
			planes[2] = viewProj.GetRow(3) + viewProj.GetRow(1);
			planes[3] = viewProj.GetRow(3) - viewProj.GetRow(1);
			planes[4] = viewProj.GetRow(3) + viewProj.GetRow(2);
			planes[5] = viewProj.GetRow(3) - viewProj.GetRow(2);

			// Normalize planes if required
			if (normalize)
			{
				for (int i = 0; i < planes.Length; i++)
				{
					float rcpMag = 1.0f / Vector3.Magnitude(planes[i]);
					planes[i] *= rcpMag;
				}
			}
		}



		/// <summary>
		/// Recursively subdivides a triangle until its area falls below kappa * deltaX^2.
		/// </summary>
		/// <param name="v0">World position of vertex 0.</param>
		/// <param name="v1">World position of vertex 1.</param>
		/// <param name="v2">World position of vertex 2.</param>
		/// <param name="n">World normal of the original triangle (assumed flat for sub-triangles).</param>
		public static void SubdivideTriangle(List<Vector3> result, Vector3 v0, Vector3 v1, Vector3 v2, float maxAreaThreshold = 0.01f)
		{
			SubdivideTriangle(result, v0, v1, v2, Vector3.one, maxAreaThreshold);
		}
		public static void SubdivideTriangle(List<Vector3> result, Vector3 v0, Vector3 v1, Vector3 v2, Vector3 scale, float maxAreaThreshold = 0.01f)
		{
			// Calculate the current area of the triangle.
			float currentArea = TriangleArea(Vector3.Scale(v0, scale), Vector3.Scale(v1, scale), Vector3.Scale(v2, scale));

			// Base Case: If the triangle's area is within the acceptable threshold,
			// add its vertices to the result list as a final, non-subdivided triangle.
			if (currentArea <= maxAreaThreshold)
			{
				result.Add(v0);
				result.Add(v1);
				result.Add(v2);
			}
			else
			{
				// Recursive Step: The triangle is too large, so subdivide it into 4 smaller triangles.
				// New vertices are placed at the midpoints of the original triangle's edges.
				Vector3 mid01 = (v0 + v1) / 2.0f; // Midpoint between v0 and v1
				Vector3 mid12 = (v1 + v2) / 2.0f; // Midpoint between v1 and v2
				Vector3 mid20 = (v2 + v0) / 2.0f; // Midpoint between v2 and v0

				// Recursively call SubdivideTriangle for each of the four new triangles:

				// 1. The triangle formed by (v0, mid01, mid20)
				SubdivideTriangle(result, v0, mid01, mid20, scale, maxAreaThreshold);

				// 2. The triangle formed by (v1, mid12, mid01)
				SubdivideTriangle(result, v1, mid12, mid01, scale, maxAreaThreshold);

				// 3. The triangle formed by (v2, mid20, mid12)
				SubdivideTriangle(result, v2, mid20, mid12, scale, maxAreaThreshold);

				// 4. The central triangle formed by the three midpoints (mid01, mid12, mid20)
				SubdivideTriangle(result, mid01, mid12, mid20, scale, maxAreaThreshold);
			}
		}

		/// <summary>
		/// Calculates the area of a triangle in 3D space.
		/// </summary>
		public static float TriangleArea(Vector3 v0, Vector3 v1, Vector3 v2)
		{
			Vector3 side1 = v1 - v0;
			Vector3 side2 = v2 - v0;
			return 0.5f * Vector3.Cross(side1, side2).magnitude;
		}


		// Helper function to get a orthogonal vector
		internal static Vector3 GetOrthogonalVector(Vector3 normal)
		{
			if (Mathf.Abs(normal.y) > 0.999f) return new Vector3(1, 0, 0);
			return new Vector3(0, 1, 0);
		}

		/// <summary>
		/// Transforms the angle in degrees to a vector2.
		/// </summary>
		/// <param name="angleInDegrees">The rotation angle in degrees. 0 is no rotation.</param>
		public static Vector2 DegreesToVec2(float angleInDegrees)
		{
			float angleInRadians = angleInDegrees * Mathf.Deg2Rad;
			return new Vector2(Mathf.Sin(angleInRadians), Mathf.Cos(angleInRadians));
		}


		// A fast, inlineable method to convert a 16-bit half float to a 32-bit float.
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float HalfToFloat(ushort half)
		{
			// Reconstruct the 32-bit float's bits into a uint
			uint resultUint = ((uint)(half & 0x8000) << 16) |
							  ((uint)((half & 0x7C00) + 0x1C000) << 13) |
							  ((uint)(half & 0x03FF) << 13);

			// Reinterpret the bits of the uint as a float. Zero cost, Burst-safe.
			return UnsafeUtility.As<uint, float>(ref resultUint);
		}


		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float HalfToAbsFloat(ushort half)
		{
			// Convert s1 to its 32-bit representation
			uint s1AsUint = ((uint)(half & 0x8000) << 16) |
							((uint)((half & 0x7C00) + 0x1C000) << 13) |
							((uint)(half & 0x03FF) << 13);

			// Clear the sign bit to perform a fast Abs()
			uint absS1Uint = s1AsUint & 0x7FFFFFFF;

			// Reinterpret the result as a float
			return UnsafeUtility.As<uint, float>(ref absS1Uint);
		}
	}

	public static class SphericalHarmonicsUtil
	{

		static int[] _idSHA = {
				Shader.PropertyToID("unity_SHAr"),
				Shader.PropertyToID("unity_SHAg"),
				Shader.PropertyToID("unity_SHAb")
			};

		static int[] _idSHB = {
				Shader.PropertyToID("unity_SHBr"),
				Shader.PropertyToID("unity_SHBg"),
				Shader.PropertyToID("unity_SHBb")
			};

		static int _idSHC =
			Shader.PropertyToID("unity_SHC");

		// Set SH coefficients to MaterialPropertyBlock
		public static void SetSHCoefficients(
			Vector3 position, MaterialPropertyBlock properties
		)
		{
			SphericalHarmonicsL2 aSample;
			LightProbes.GetInterpolatedProbe(position + new Vector3(200, 0, 200), null, out aSample);

			for (int iC = 0; iC < 3; iC++)
			{
				properties.SetVector(_idSHA[iC], new Vector4(aSample[iC, 3], aSample[iC, 1], aSample[iC, 2], aSample[iC, 0] - aSample[iC, 6]));
			}

			for (int iC = 0; iC < 3; iC++)
			{
				properties.SetVector(_idSHB[iC], new Vector4(aSample[iC, 4], aSample[iC, 5], 3.0f * aSample[iC, 6], aSample[iC, 7]));
			}

			// Final quadratic polynomial
			properties.SetVector(_idSHC, new Vector4(aSample[0, 8], aSample[1, 8], aSample[2, 8], 1));
		}

		// Set SH coefficients to Material
		public static void SetSHCoefficients(
			Vector3 position, Material material
		)
		{
			SphericalHarmonicsL2 aSample;
			LightProbes.GetInterpolatedProbe(position + new Vector3(200, 0, 200), null, out aSample);

			for (int iC = 0; iC < 3; iC++)
			{
				material.SetVector(_idSHA[iC], new Vector4(aSample[iC, 3], aSample[iC, 1], aSample[iC, 2], aSample[iC, 0] - aSample[iC, 6]));
			}

			for (int iC = 0; iC < 3; iC++)
			{
				material.SetVector(_idSHB[iC], new Vector4(aSample[iC, 4], aSample[iC, 5], 3.0f * aSample[iC, 6], aSample[iC, 7]));
			}

			// Final quadratic polynomial
			material.SetVector(_idSHC, new Vector4(aSample[0, 8], aSample[1, 8], aSample[2, 8], 1));
		}


		/// <summary>
		/// Calculates the Ambient Color (Up direction) at a specific position.
		/// Handles Pipeline differences (HDRP vs URP/BiRP) and Light Probe fallbacks.
		/// </summary>
		public static Color GetAmbientColorUp(Vector3 position)
		{
			Vector3 resultRGB = Vector3.zero;

#if FLUIDFRENZY_RUNTIME_HDRP_SUPPORT
			// HDRP
			// RenderSettings are unreliable in HDRP. We rely entirely on Light Probes / Volume data.
			SphericalHarmonicsL2 sh;
			LightProbes.GetInterpolatedProbe(position, null, out sh);
			resultRGB = EvaluateSHUp(sh);
#else
			// URP / BiRP
			SphericalHarmonicsL2 sh;
			LightProbes.GetInterpolatedProbe(position, null, out sh);

			// Check if the probe is empty (common in scenes without baked lighting)
			bool isProbeEmpty = (sh[0, 0] == 0 && sh[1, 0] == 0 && sh[2, 0] == 0);

			if (isProbeEmpty)
			{
				// Fallback to global settings (Skybox/Gradient/Color)
				Color env = RenderSettings.ambientSkyColor;
				resultRGB = new Vector3(env.r, env.g, env.b);
			}
			else
			{
				resultRGB = EvaluateSHUp(sh);
			}
#endif

			// If there is absolutely no light (black), return a default grey to prevent the effect from vanishing.
			if (resultRGB.sqrMagnitude < 0.0001f)
			{
				return new Color(0.25f, 0.25f, 0.25f, 1.0f);
			}

			return new Color(resultRGB.x, resultRGB.y, resultRGB.z, 1.0f);
		}

		// Helper math to evaluate SH in the UP direction (0,1,0)
		private static Vector3 EvaluateSHUp(SphericalHarmonicsL2 sh)
		{
			float r = sh[0, 0] + sh[0, 1];
			float g = sh[1, 0] + sh[1, 1];
			float b = sh[2, 0] + sh[2, 1];
			return new Vector3(Mathf.Max(0, r), Mathf.Max(0, g), Mathf.Max(0, b));
		}
	}

	public struct RenderTextureScope : System.IDisposable
	{
		private readonly RenderTexture m_previousRT;

		public RenderTextureScope(RenderTexture newTarget)
		{
			m_previousRT = RenderTexture.active;
			RenderTexture.active = newTarget;
		}

		public void Dispose()
		{
			RenderTexture.active = m_previousRT;
		}
	}

}