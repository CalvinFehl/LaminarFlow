using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FluidFrenzy
{
	internal struct PrimitiveData
	{
		public Vector3 center;
		public Vector3 edge1; // v1 - v0
		public Vector3 edge2; // v2 - v0
	}

	public class PrimitiveGenerator
	{
		public static MeshRenderer GeneratePlane(GameObject gameObject, int resolution, Vector2 dimensions)
		{
			MeshRenderer renderer = gameObject.AddComponent<MeshRenderer>();
			MeshCollider collider = gameObject.AddComponent<MeshCollider>();
			MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();
			Mesh mesh = new Mesh();
			meshFilter.sharedMesh = mesh;


			int resX = resolution; // 2 minimum
			int resZ = resolution;

			#region Vertices		
			Vector3[] vertices = new Vector3[resX * resZ];
			for (int z = 0; z < resZ; z++)
			{
				// [ -length / 2, length / 2 ]
				float zPos = ((float)z / (resZ - 1) - .5f) * dimensions.y;
				for (int x = 0; x < resX; x++)
				{
					// [ -width / 2, width / 2 ]
					float xPos = ((float)x / (resX - 1) - .5f) * dimensions.x;
					vertices[x + z * resX] = new Vector3(xPos, 0f, zPos);
				}
			}
			#endregion

			#region Normales
			Vector3[] normales = new Vector3[vertices.Length];
			for (int n = 0; n < normales.Length; n++)
				normales[n] = Vector3.up;
			#endregion

			#region UVs		
			Vector2[] uvs = new Vector2[vertices.Length];
			for (int v = 0; v < resZ; v++)
			{
				for (int u = 0; u < resX; u++)
				{
					uvs[u + v * resX] = new Vector2((float)u / (resX - 1), (float)v / (resZ - 1));
				}
			}
			#endregion

			#region Triangles
			int nbFaces = (resX - 1) * (resZ - 1);
			int[] triangles = new int[nbFaces * 6];
			int t = 0;
			for (int face = 0; face < nbFaces; face++)
			{
				// Retrieve lower left corner from face ind
				int i = face % (resX - 1) + (face / (resZ - 1) * resX);

				triangles[t++] = i + resX;
				triangles[t++] = i + 1;
				triangles[t++] = i;

				triangles[t++] = i + resX;
				triangles[t++] = i + resX + 1;
				triangles[t++] = i + 1;
			}
			#endregion
			mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

			mesh.vertices = vertices;
			mesh.normals = normales;
			mesh.uv = uvs;
			mesh.triangles = triangles;

			mesh.RecalculateBounds();
			mesh.Optimize();
			mesh.UploadMeshData(true);

			collider.sharedMesh = mesh;

			return renderer;
		}

		public static Mesh GenerateTerrainMesh(int resX, int resZ, Vector2 dimension, Vector2 uvStart, Vector2 uvScale)
		{
			Mesh mesh = new Mesh();
			#region Vertices		
			Vector3[] vertices = new Vector3[(resX + 1) * (resZ + 1)];
			Vector2[] uvs = new Vector2[vertices.Length];
			Vector2[] uvs2 = new Vector2[vertices.Length];

			Vector2 gridRcpSize = Vector2.one / new Vector2(resX + 1, resZ + 1);
			Vector2 halfGridRcpSize = (gridRcpSize) * 0.5f;

			int v = 0;
			for (int z = 0; z <= resZ; z++)
			{
				float zPos = (((float)(z) / resZ) - 0.5f) * dimension.y;
				for (int x = 0; x <= resX; x++)
				{
					Vector2 uv = new Vector2(x, z);
					Vector2 uv2 = new Vector2((float)(x) / resX, (float)(z) / resZ);
					float xPos = (((float)(x) / resX) - 0.5f) * dimension.x;
					//Vector2 uv = new Vector2((float)x / (resX), (float)z);
					uv = (uv * gridRcpSize) + halfGridRcpSize;//+ uvParams.xy;

					uv.Scale(uvScale);
					uvs[v] = uv + uvStart;					
					
					uv2.Scale(uvScale);
					uvs2[v] = uv2 + uvStart;

					vertices[v] = new Vector3(xPos, 0, zPos);
					v++;
				}
			}
			#endregion

			#region Triangles
			int nbFaces = (resX) * (resZ);
			int[] triangles = new int[nbFaces * 6];
			int t = 0;

			int vert = 0;
			for (int z = 0; z < resZ; z++)
			{
				for (int x = 0; x < resX; x++)
				{
					triangles[t++] = vert;
					triangles[t++] = vert + resX + 2;
					triangles[t++] = vert + 1;

					triangles[t++] = vert;
					triangles[t++] = vert + resX + 1;
					triangles[t++] = vert + resX + 2;

					vert++;
				}
				vert++;
			}
			#endregion
			//m_Mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
			mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
			mesh.vertices = vertices;
			mesh.uv = uvs;
			mesh.uv2 = uvs2;
			mesh.triangles = triangles;

			return mesh;
		}

		public static Mesh GenerateTerrainLODMesh(int resX, int resZ)
		{
			Mesh mesh = new Mesh();
			#region Vertices		
			Vector3[] vertices = new Vector3[(resX + 1) * (resZ + 1)];


			int v = 0;
			for (int z = 0; z <= resZ; z++)
			{
				float zPos = (((float)(z) / resZ) - 0.5f);
				for (int x = 0; x <= resX; x++)
				{
					float xPos = (((float)(x) / resX) - 0.5f);

					vertices[v] = new Vector3((float)(x) / resZ, 0, (float)(z) / resZ);
					//vertices[v] = new Vector3(xPos, 0, zPos);
					v++;
				}
			}
			#endregion

			#region Triangles
			int nbFaces = (resX) * (resZ);
			int[] triangles = new int[nbFaces * 6];
			int t = 0;

			int vert = 0;
			for (int z = 0; z < resZ; z++)
			{
				for (int x = 0; x < resX; x++)
				{
					triangles[t++] = vert;
					triangles[t++] = vert + resX + 2;
					triangles[t++] = vert + 1;

					triangles[t++] = vert;
					triangles[t++] = vert + resX + 1;
					triangles[t++] = vert + resX + 2;

					vert++;
				}
				vert++;
			}
			#endregion
			//m_Mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
			mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
			mesh.vertices = vertices;
			mesh.triangles = triangles;

			return mesh;
		}

		internal static void GenerateSphereSamples(List<PrimitiveData> points, Vector3 center, float radius, int sampleCount)
		{
			float pointCount = sampleCount;
			float surfaceArea = 4 * Mathf.PI * radius * radius;
			float pointArea = surfaceArea / pointCount;
			float edgeLength = Mathf.Sqrt(2 * pointArea);

			float goldenRatio = (1 + Mathf.Sqrt(5)) / 2;
			for (int i = 0; i < pointCount; i++)
			{
				float y = 1 - (i / (pointCount - 1)) * 2;
				float r = Mathf.Sqrt(1 - y * y);
				float theta = (i * 2 * Mathf.PI) / goldenRatio;

				Vector3 localNormal = new Vector3(Mathf.Cos(theta) * r, y, Mathf.Sin(theta) * r).normalized;

				// Find a perpendicular vector.
				Vector3 u_axis = GraphicsHelpers.GetOrthogonalVector(localNormal);

				// Create the two basis vectors for the tangent plane.
				Vector3 v_axis = Vector3.Cross(localNormal, u_axis).normalized;
				u_axis = Vector3.Cross(v_axis, localNormal).normalized; // Re-cross to guarantee orthogonality and normalize

				PrimitiveData triangleData = new PrimitiveData
				{
					center = center + localNormal * radius,
					// Store the two edges of the virtual triangle
					edge1 = u_axis * edgeLength,
					edge2 = v_axis * edgeLength
				};
				points.Add(triangleData);
			}
		}

		internal static void GenerateBoxSamples(List<PrimitiveData> points, Vector3 center, Vector3 size, int sampleCount)
		{
			Vector3 extents = size / 2.0f;
			int count = sampleCount;
			for (int face = 0; face < 6; face++)
			{
				Vector3 normal = Vector3.zero;
				int axisU = 0, axisV = 0;
				float facePos = 0;

				// Determine face normal and axes
				switch (face)
				{
					case 0: normal = Vector3.right; axisU = 2; axisV = 1; facePos = extents.x; break; // +X
					case 1: normal = Vector3.left; axisU = 2; axisV = 1; facePos = extents.x; break; // -X
					case 2: normal = Vector3.up; axisU = 0; axisV = 2; facePos = extents.y; break; // +Y
					case 3: normal = Vector3.down; axisU = 0; axisV = 2; facePos = extents.y; break; // -Y
					case 4: normal = Vector3.forward; axisU = 0; axisV = 1; facePos = extents.z; break; // +Z
					case 5: normal = Vector3.back; axisU = 0; axisV = 1; facePos = extents.z; break; // -Z
				}

				float faceArea = size[axisU] * size[axisV];
				if (faceArea <= 0) continue;
				float pointArea = faceArea / (count * count);

				// Create the two perpendicular virtual edges
				Vector3 u_axis = Vector3.zero; u_axis[axisU] = 1;
				Vector3 v_axis = Vector3.zero; v_axis[axisV] = 1;

				if (Vector3.Dot(Vector3.Cross(u_axis, v_axis), normal) < 0)
				{
					// Flip one of the axes to correct the normal's direction.
					u_axis = -u_axis;
				}

				float edgeLength = Mathf.Sqrt(2 * pointArea);

				float divisor = (float)(count - 1);
				for (int u = 0; u < count; u++)
				{
					for (int v = 0; v < count; v++)
					{
						Vector3 point = center + normal * facePos;
						point[axisU] += (u / divisor - 0.5f) * size[axisU];
						point[axisV] += (v / divisor - 0.5f) * size[axisV];

						PrimitiveData triangleData = new PrimitiveData
						{
							center = point,
							// Store the two edges of the virtual triangle
							edge1 = u_axis * edgeLength,
							edge2 = v_axis * edgeLength
						};
						points.Add(triangleData);
					}
				}
			}
		}

		internal static void GenerateCapsuleSamples(List<PrimitiveData> points, CapsuleCollider capsule, int sampleCount)
		{
			Vector3 center = capsule.center;
			float radius = capsule.radius;
			float height = capsule.height;
			int direction = capsule.direction; // 0=X, 1=Y, 2=Z

			// Ensure height is at least as tall as the caps
			height = Mathf.Max(height, radius * 2);

			// Calculate the height of the cylindrical part
			float cylinderHeight = height - 2 * radius;

			// Calculate surface areas to distribute points proportionally
			float sphereArea = 4 * Mathf.PI * radius * radius;
			float cylinderArea = 2 * Mathf.PI * radius * cylinderHeight;
			float totalArea = sphereArea + cylinderArea;

			int pointsForCaps = Mathf.RoundToInt(sampleCount * (sphereArea / totalArea));
			int pointsForCylinder = sampleCount - pointsForCaps;

			float pointArea = totalArea / sampleCount;
			float edgeLength = Mathf.Sqrt(2 * pointArea);

			// Generate points for the two hemispherical caps
			Vector3 capAxis = Vector3.zero;
			capAxis[direction] = 1;
			Vector3 topCapCenter = center + capAxis * (cylinderHeight / 2.0f);
			Vector3 bottomCapCenter = center - capAxis * (cylinderHeight / 2.0f);

			float goldenRatio = (1 + Mathf.Sqrt(5)) / 2;
			for (int i = 0; i < pointsForCaps; i++)
			{
				// Generate a point on a unit sphere (Fibonacci sphere)
				float y = 1 - (i / (float)(pointsForCaps - 1)) * 2;
				float r = Mathf.Sqrt(1 - y * y);
				float theta = (i * 2 * Mathf.PI) / goldenRatio;
				Vector3 pointOnUnitSphere = new Vector3(Mathf.Cos(theta) * r, y, Mathf.Sin(theta) * r);

				// Determine which cap this point belongs to and set its final position
				Vector3 capCenter = (y > 0) ? topCapCenter : bottomCapCenter;
				Vector3 localPosition = capCenter + pointOnUnitSphere * radius;
				Vector3 localNormal = pointOnUnitSphere.normalized;

				// Rotate the point if the capsule is not Y-aligned
				if (direction == 0) // X-Axis
				{
					localPosition = new Vector3(localPosition.y, localPosition.x, localPosition.z);
					localNormal = new Vector3(localNormal.y, localNormal.x, localNormal.z);
				}
				else if (direction == 2) // Z-Axis
				{
					localPosition = new Vector3(localPosition.x, localPosition.z, localPosition.y);
					localNormal = new Vector3(localNormal.x, localNormal.z, localNormal.y);
				}

				// Create and add the virtual triangle data
				Vector3 u_axis = GraphicsHelpers.GetOrthogonalVector(localNormal);
				Vector3 v_axis = Vector3.Cross(localNormal, u_axis).normalized;
				u_axis = Vector3.Cross(v_axis, localNormal);

				points.Add(new PrimitiveData
				{
					center = localPosition,
					edge1 = u_axis * edgeLength,
					edge2 = v_axis * edgeLength
				});
			}

			// Generate points for the cylindrical body
			int cylinderRows = Mathf.Max(2, Mathf.RoundToInt(Mathf.Sqrt(pointsForCylinder * (cylinderHeight / (2 * Mathf.PI * radius)))));
			int cylinderCols = Mathf.Max(2, pointsForCylinder / cylinderRows);

			for (int i = 0; i < cylinderRows; i++)
			{
				float t = (float)i / (cylinderRows - 1); // interpolant along height
				float yPos = Mathf.Lerp(-cylinderHeight / 2.0f, cylinderHeight / 2.0f, t);

				for (int j = 0; j < cylinderCols; j++)
				{
					float angle = (float)j / cylinderCols * 2 * Mathf.PI;
					Vector3 localNormal = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
					Vector3 localPosition = center + new Vector3(localNormal.x * radius, yPos, localNormal.z * radius);

					// Rotate the point if the capsule is not Y-aligned
					if (direction == 0) // X-Axis
					{
						localPosition = new Vector3(localPosition.y, localPosition.x, localPosition.z);
						localNormal = new Vector3(localNormal.y, localNormal.x, localNormal.z);
					}
					else if (direction == 2) // Z-Axis
					{
						localPosition = new Vector3(localPosition.x, localPosition.z, localPosition.y);
						localNormal = new Vector3(localNormal.x, localNormal.z, localNormal.y);
					}

					// Create and add the virtual triangle data
					Vector3 u_axis = GraphicsHelpers.GetOrthogonalVector(localNormal);
					Vector3 v_axis = Vector3.Cross(localNormal, u_axis).normalized;
					u_axis = Vector3.Cross(v_axis, localNormal);

					points.Add(new PrimitiveData
					{
						center = localPosition,
						edge1 = u_axis * edgeLength,
						edge2 = v_axis * edgeLength
					});
				}
			}
		}

		/// <summary>
		/// Calculates the world-space surface area of a BoxCollider.
		/// This is accurate for non-uniform scales.
		/// </summary>
		/// <param name="collider">The BoxCollider to measure.</param>
		/// <returns>The total surface area in world units.</returns>
		public static float GetSurfaceArea(BoxCollider collider)
		{
			if (collider == null)
			{
				Debug.LogError("BoxCollider is null. Cannot calculate area.");
				return 0f;
			}

			// Get the local size of the box from the collider
			Vector3 localSize = collider.size;

			// Get the world scale of the object
			Vector3 worldScale = collider.transform.lossyScale;

			// Calculate the world-space dimensions of the box by applying the scale
			float worldWidth = localSize.x * worldScale.x;
			float worldHeight = localSize.y * worldScale.y;
			float worldDepth = localSize.z * worldScale.z;

			// Calculate the area of each pair of faces
			float areaXY = worldWidth * worldHeight; // Front and Back faces
			float areaXZ = worldWidth * worldDepth;  // Top and Bottom faces
			float areaYZ = worldHeight * worldDepth; // Left and Right faces

			// The total surface area is the sum of the areas of all 6 faces
			return 2f * (areaXY + areaXZ + areaYZ);
		}

		/// <summary>
		/// Calculates the approximate world-space surface area of a CapsuleCollider.
		/// This method provides a very good approximation for non-uniformly scaled capsules.
		/// </summary>
		/// <param name="collider">The CapsuleCollider to measure.</param>
		/// <returns>The approximate total surface area in world units.</returns>
		public static float GetSurfaceArea(CapsuleCollider collider)
		{
			if (collider == null)
			{
				Debug.LogError("CapsuleCollider is null. Cannot calculate area.");
				return 0f;
			}

			// Get local properties from the collider
			float localRadius = collider.radius;
			float localHeight = collider.height;
			int direction = collider.direction; // 0=X, 1=Y, 2=Z

			// Get the world scale
			Vector3 worldScale = collider.transform.lossyScale;

			// Approximate World-Space Dimensions
			// For a non-uniformly scaled capsule, the caps become ellipsoids and the body
			// becomes an elliptical cylinder. Calculating the exact surface area is extremely complex.
			// Instead, we use a robust approximation by averaging the scale for the radius.

			float worldRadius;
			float heightScale;

			// Determine the average radius scale and the height scale based on the capsule's alignment
			if (direction == 0) // X-Axis
			{
				worldRadius = localRadius * (worldScale.y + worldScale.z) / 2f;
				heightScale = worldScale.x;
			}
			else if (direction == 1) // Y-Axis
			{
				worldRadius = localRadius * (worldScale.x + worldScale.z) / 2f;
				heightScale = worldScale.y;
			}
			else // Z-Axis
			{
				worldRadius = localRadius * (worldScale.x + worldScale.y) / 2f;
				heightScale = worldScale.z;
			}

			float worldHeight = localHeight * heightScale;
			// Ensure the total height is at least as tall as the two hemisphere caps
			worldHeight = Mathf.Max(worldHeight, worldRadius * 2f);

			// Calculate Area Components

			// The two hemispherical caps combine to form one full sphere
			float sphereArea = 4f * Mathf.PI * worldRadius * worldRadius;

			// The height of the cylindrical part is the total height minus the radius of the two caps
			float cylinderHeight = worldHeight - 2f * worldRadius;

			// The area of the cylinder's wall is its circumference times its height
			float cylinderArea = 2f * Mathf.PI * worldRadius * cylinderHeight;

			return sphereArea + cylinderArea;
		}

		/// <summary>
		/// Calculates the approximate world-space surface area of a SphereCollider.
		/// Note: A non-uniformly scaled sphere becomes an ellipsoid. This provides a robust
		/// approximation of the surface area without complex elliptic integrals.
		/// </summary>
		public static float GetSurfaceArea(SphereCollider collider)
		{
			if (collider == null)
			{
				Debug.LogError("SphereCollider is null. Cannot calculate area.");
				return 0f;
			}

			float localRadius = collider.radius;
			Vector3 worldScale = collider.transform.lossyScale;

			// Approximate the radius of the resulting ellipsoid by averaging the scale on each axis.
			// This is a robust approximation that works well for both uniform and non-uniform scales.
			float averageScale = (worldScale.x + worldScale.y + worldScale.z) / 3f;
			float worldRadius = localRadius * averageScale;

			// Use the standard sphere area formula with our calculated world-space radius.
			return 4f * Mathf.PI * worldRadius * worldRadius;
		}
	}
}