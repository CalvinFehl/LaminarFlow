using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static FluidFrenzy.ErosionLayer;
using static FluidFrenzy.FluidSimulation;

namespace FluidFrenzy
{
	/// <summary>
	/// <see cref="TerraformModifier"/> is a component used to interactively modify both the terrain and fluid layers within the <see cref="FluidSimulation"/>. 
	/// </summary>
	/// <remarks>
	/// <para>
	/// This component acts as a "brush" to simulate the transformation of material layers between solid ground and liquid fluid. It enables real-time interactive effects, such as melting snow/ice (<c>liquify</c>) into water or the water into snow (<c>solidify</c>), making it ideal for "God games" or complex world interaction scenarios.
	/// </para>
	/// <para>
	/// **Note:** This component requires and works in conjunction with the <c>TerraformLayer</c> (or a similar erosion/terrain modification system) on the <see cref="FluidSimulation"/> to enable the material transformation process.
	/// </para>
	/// <para>
	/// The modification effect is defined by the <see cref="TerraformModifierSettings"/> and applied within a localized area determined by the selected <see cref="TerraformInputMode"/> and <see cref="size"/>.
	/// </para>
	/// </remarks>
	public class TerraformModifier : MonoBehaviour
	{
		public enum TerraformInputMode
		{
			/// <summary>
			/// The modifier inputs the fluid in a circular shape.
			/// </summary>
			Circle,
			/// <summary>
			/// The modifier inputs fluid in a rectangular shape.
			/// </summary>
			Box,

			/// <summary>
			/// The modifier inputs fluid in a Sphere shape.
			/// </summary>
			Sphere,

			/// <summary>
			/// The modifier inputs fluid in a Cube shape.
			/// </summary>
			Cube,

			/// <summary>
			/// The modifier inputs fluid in a Cylinder shape.
			/// </summary>
			Cylinder,

			/// <summary>
			/// The modifier inputs fluid in a Capsule shape.
			/// </summary>
			Capsule
		}

		[Serializable]
		public class TerraformModifierSettings
		{
			/// <summary>
			/// Set the shape of the modification brush (Circle, Box, Sphere, Cube, Cylinder, Capsule).
			/// </summary>
			public TerraformInputMode mode = TerraformInputMode.Circle;

			/// <summary>
			/// Adjust the dimensions of the modification area in world units. Interpretation varies by mode (e.g., Circle uses X for radius, Box uses X/Z for width/depth).
			/// </summary>
			public Vector3 size = new Vector3(10, 10, 10);

			/// <summary>
			/// Adjust the sharpness of the brush edge. Higher values create a softer edge.
			/// </summary>
			[Range(0.001f, 5)]
			public float falloff = 0.001f;

			/// <summary>
			/// If enabled, the modifier will attempt to dissolve terrain into fluid (liquify).
			/// </summary>
			public bool liquify = true;

			/// <summary>
			/// Set the terrain layer (e.g. Layer 1, Layer 2) that will be dissolved.
			/// </summary>
			public ErosionLayer.TerrainLayer liquifyTerrainLayer = ErosionLayer.TerrainLayer.Layer1;


			/// <summary>
			/// Set the fluid layer (e.g., Layer 1, Layer 2) that this terrain will dissolve into.
			/// </summary>
			public FluidSimulation.FluidLayerIndex liquifyFluidLayer = FluidLayerIndex.Layer1;

			/// <summary>
			/// Set the speed at which the terrain dissolves into fluid, in units of height per second. Higher values mean faster melting or dissolving..
			/// </summary>
			[Range(0, 20)]
			public float liquifyRate = 1;

			/// <summary>
			/// Set the conversion ratio of terrain height to fluid depth. A value of 1 means 1 unit of terrain
			/// height becomes 1 unit of fluid depth. A value of 2 means 1 unit of terrain becomes 2 units of fluid.
			/// </summary>
			[Range(0, 10)]
			public float liquifyAmount = 1;


			/// <summary>
			/// If enabled, the modifier will attempt to solidify fluid into terrain.
			/// </summary>
			public bool solidify = true;

			/// <summary>
			/// Set the terrain layer (e.g. Layer 1, Layer 2) that will be built up.
			/// </summary>
			public ErosionLayer.TerrainLayer solidifyTerrainLayer = ErosionLayer.TerrainLayer.Layer1;

			/// <summary>
			/// Set the splat channel (e.g., R, G, B, A) that will be used to paint the built-up terrain.
			/// </summary>
			public ErosionLayer.SplatChannel solidifySplatChannel = SplatChannel.R;

			/// <summary>
			/// Set the fluid layer (e.g., Layer 1, Layer 2) that will be consumed to create terrain.
			/// </summary>
			public FluidSimulation.FluidLayerIndex solidifyFluidLayer = FluidLayerIndex.Layer1;

			/// <summary>
			/// Set the speed at which the fluid solidifies into terrain, in units of height per second. Higher values mean faster build-up of terrain.
			/// </summary>
			[Range(0, 20)]
			public float solidifyRate = 1;

			/// <summary>
			/// Set the conversion ratio of fluid depth to terrain height. A value of 1 means 1 unit of fluid
			/// depth becomes 1 unit of terrain height. A value of 2 means 2 units of fluid become 1 unit of terrain.
			/// </summary>
			[Range(0, 10)]
			public float solidifyAmount = 1;
		}

		static HashSet<TerraformModifier> unqiueTerraformModifiers = new HashSet<TerraformModifier>();
		public static TerraformModifier[] terraformModifiers { get; private set; } = new TerraformModifier[0];

		public TerraformModifierSettings settings = new TerraformModifierSettings();

		static void RegisterTerrainModifier(TerraformModifier obj)
		{
			unqiueTerraformModifiers.Add(obj);
			if (unqiueTerraformModifiers.Count == 0)
			{
				terraformModifiers = new TerraformModifier[0];
				return;
			}
			terraformModifiers = unqiueTerraformModifiers.ToArray();
		}

		static void DeregisterTerrainModifier(TerraformModifier obj)
		{
			unqiueTerraformModifiers.Remove(obj);
			if (unqiueTerraformModifiers.Count == 0)
			{
				terraformModifiers = new TerraformModifier[0];
				return;
			}
			terraformModifiers = unqiueTerraformModifiers.ToArray();
		}

		void Awake()
		{
			RegisterTerrainModifier(this);
		}

		private void OnDestroy()
		{
			DeregisterTerrainModifier(this);
		}

		public void Process(TerraformLayer terrainLayer, float dt)
		{
			terrainLayer.ApplyTerraform(this, dt);
		}

		private Vector3 GetUnrotatedWorldSize()
		{
			if (settings.mode == TerraformInputMode.Sphere)
			{
				// Sphere diameter is settings.size.x. All dimensions are the same.
				float diameter = settings.size.x;
				return new Vector3(diameter, diameter, diameter);
			}
			else if (settings.mode == TerraformInputMode.Cylinder)
			{
				// Cylinder: X/Z = Diameter (size.x), Y = Height (size.y)
				return new Vector3(settings.size.x, settings.size.y, settings.size.x);
			}
			else if (settings.mode == TerraformInputMode.Capsule)
			{
				// Capsule: X/Z = Diameter (size.x), Y = Segment Length (size.y) + Diameter (size.x)
				float diameter = settings.size.x;
				float height = settings.size.y + settings.size.x;
				return new Vector3(diameter, height, diameter);
			}
			else if (settings.mode == TerraformInputMode.Cube || settings.mode == TerraformInputMode.Box)
			{
				return settings.size;
			}

			return settings.size;
		}

		internal void GetTerraformModifierBlitParams(FluidSimulation simulation, out Vector2 blitPosition, out Vector2 blitRotation, out Vector2 uvSize)
		{
			Transform modifierTransform = transform;
			blitPosition = simulation.WorldSpaceToPaddedUVSpace(modifierTransform.position);
			blitRotation = GraphicsHelpers.DegreesToVec2(modifierTransform.rotation.eulerAngles.y);
			Vector3 worldSizeForUV;
			// Check if we need the complex 3D rotation bounding box calculation
			if (settings.mode == TerraformModifier.TerraformInputMode.Sphere ||
				settings.mode == TerraformModifier.TerraformInputMode.Cube ||
				settings.mode == TerraformModifier.TerraformInputMode.Cylinder ||
				settings.mode == TerraformModifier.TerraformInputMode.Capsule)
			{
				// Get the unrotated 3D bounding box size of the shape
				Vector3 unrotatedSize = GetUnrotatedWorldSize();

				// Calculate the axis-aligned world XZ size after rotation
				// This is required because the rotation is 3D (X, Y, Z rotation)
				worldSizeForUV = GraphicsHelpers.CalculateRotatedXZBoundingBoxSize(modifierTransform.rotation, unrotatedSize);
				blitRotation = GraphicsHelpers.DegreesToVec2(0.0f);
			}
			else // Circle and 2D Box (which rely on the existing 2D Blit rotation)
			{
				// For 2D-only shapes, the size is just the XZ projection of the settings size.
				if (settings.mode == TerraformModifier.TerraformInputMode.Circle)
				{
					// Use a square bounded by the diameter (settings.size.x)
					worldSizeForUV = new Vector3(settings.size.x, 0f, settings.size.x);
				}
				else // TerraformInputMode.Box
				{
					// Use settings.size.x and settings.size.z directly
					worldSizeForUV = new Vector3(settings.size.x, 0f, settings.size.z);
				}
			}

			// Convert the calculated world XZ size to UV size
			// We only care about X and Z components of worldSizeForUV (Y is 0)
			uvSize = simulation.WorldSizeToUVSize(new Vector3(worldSizeForUV.x, worldSizeForUV.z));
		}

		// This method now correctly returns the 3D bounding box size for the modifier
		public Vector3 GetGizmoSize()
		{
			float dynamicYHeight = 10f; 
			// Get the main fluid simulation
			var sim = FluidSimulationManager.GetEditorMainSimulation();
			if (sim != null)
			{
				dynamicYHeight = transform.position.y;
			}

			if (settings.mode == TerraformInputMode.Circle)
			{
				// Circle is a 2D shape, but we need a height for the Gizmo. Using dynamicYHeight.
				float xzSize = settings.size.x; // Use X for diameter
				return new Vector3(xzSize, dynamicYHeight, xzSize);
			}
			else if (settings.mode == TerraformInputMode.Box)
			{
				// Box is a 2D shape, use X for width, Z for depth, Y is dynamicYHeight
				return new Vector3(settings.size.x, dynamicYHeight, settings.size.z);
			}
			else if (settings.mode == TerraformInputMode.Sphere)
			{
				// Sphere diameter is settings.size.x. All dimensions are the same.
				float diameter = settings.size.x;
				return new Vector3(diameter, diameter, diameter);
			}
			else if (settings.mode == TerraformInputMode.Cube)
			{
				// Cube uses settings.size.xyz directly
				return settings.size;
			}
			else if (settings.mode == TerraformInputMode.Cylinder)
			{
				// Cylinder uses settings.size.x for diameter and settings.size.y for height
				float diameter = settings.size.x;
				return new Vector3(diameter, settings.size.y, diameter);
			}
			else if (settings.mode == TerraformInputMode.Capsule)
			{
				// XZ: Diameter (settings.size.x)
				float diameter = settings.size.x;
				// Y: Segment Length (settings.size.y) + Diameter (settings.size.x)
				float height = settings.size.y + settings.size.x;
				return new Vector3(diameter, height, diameter);
			}

			// Default fallback. Changed 10f to dynamicYHeight.
			return new Vector3(1f, dynamicYHeight, 1f);
		}

		private const int CylinderSegments = 16;

		public void OnDrawGizmosSelected()
		{
			Matrix4x4 prevMatrix = Gizmos.matrix;

			Gizmos.matrix = transform.localToWorldMatrix;
			Gizmos.color = Color.yellow;

			// Get the correct size based on the current mode
			Vector3 gizmoSize = GetGizmoSize();

			// Draw the Gizmo based on the mode
			if (settings.mode == TerraformInputMode.Sphere)
			{
				Gizmos.DrawWireSphere(Vector3.zero, gizmoSize.x * 0.5f);
			}
			else if (settings.mode == TerraformInputMode.Cylinder)
			{
				float radius = gizmoSize.x * 0.5f;
				float halfHeight = gizmoSize.y * 0.5f;

				Vector3 topCenter = Vector3.up * halfHeight;
				Vector3 bottomCenter = Vector3.down * halfHeight;

				Vector3 prevTopPoint = Vector3.zero;
				Vector3 prevBottomPoint = Vector3.zero;

				for (int i = 0; i <= CylinderSegments; i++)
				{
					float angle = ((float)i / CylinderSegments) * 360f;
					float rad = angle * Mathf.Deg2Rad;

					// Calculate point position on the XZ plane
					Vector3 currentPoint = new Vector3(Mathf.Cos(rad) * radius, 0, Mathf.Sin(rad) * radius);

					Vector3 currentTopPoint = topCenter + currentPoint;
					Vector3 currentBottomPoint = bottomCenter + currentPoint;

					if (i > 0)
					{
						// Draw Top Circle Segment
						Gizmos.DrawLine(prevTopPoint, currentTopPoint);

						// Draw Bottom Circle Segment
						Gizmos.DrawLine(prevBottomPoint, currentBottomPoint);
					}

					// Draw Side Line
					// Only draw staves for the first 4 segments for a cleaner look, or all of them.
					// Drawing all staves can look cluttered, so let's draw one every 4 segments (4 staves total).
					if (i % (CylinderSegments / 4) == 0)
					{
						Gizmos.DrawLine(currentTopPoint, currentBottomPoint);
					}
					// Always draw the first and last stave for the seams
					else if (i == 0 || i == CylinderSegments)
					{
						Gizmos.DrawLine(currentTopPoint, currentBottomPoint);
					}

					prevTopPoint = currentTopPoint;
					prevBottomPoint = currentBottomPoint;
				}
			}
			else if (settings.mode == TerraformInputMode.Capsule)
			{
				float diameter = settings.size.x;
				float radius = diameter * 0.5f;
				float segmentLength = settings.size.y;

				// The two hemisphere centers (top and bottom of the segment)
				Vector3 topCapCenter = Vector3.up * (segmentLength * 0.5f);
				Vector3 bottomCapCenter = Vector3.down * (segmentLength * 0.5f);

				// Draw the central line segment
				Gizmos.DrawLine(topCapCenter, bottomCapCenter);

				// Draw the two spheres (hemispheres)
				Gizmos.DrawWireSphere(topCapCenter, radius);
				Gizmos.DrawWireSphere(bottomCapCenter, radius);

				// Draw the central 'belt' connecting the two hemispheres
				Vector3 prevPoint = Vector3.zero;
				for (int i = 0; i <= CylinderSegments; i++)
				{
					float angle = ((float)i / CylinderSegments) * 360f;
					float rad = angle * Mathf.Deg2Rad;

					// Calculate point position on the XZ plane
					Vector3 currentPoint = new Vector3(Mathf.Cos(rad) * radius, 0, Mathf.Sin(rad) * radius);

					// Top connection point (XZ plane)
					Vector3 currentTopBelt = topCapCenter + currentPoint;
					// Bottom connection point (XZ plane)
					Vector3 currentBottomBelt = bottomCapCenter + currentPoint;

					// Draw the vertical side line (stave)
					Gizmos.DrawLine(currentTopBelt, currentBottomBelt);

					if (i > 0)
					{
						// Draw the top and bottom circles (the 'belt' edges)
						Gizmos.DrawLine(prevPoint + topCapCenter, currentTopBelt);
						Gizmos.DrawLine(prevPoint + bottomCapCenter, currentBottomBelt);
					}

					prevPoint = currentPoint;
				}
			}
			else if (settings.mode == TerraformInputMode.Cube)// Circle, Box, Cube
			{
				Gizmos.DrawWireCube(Vector3.zero, gizmoSize);
			}
			else
			{
				// Circle and Box draw an XZ-plane projection with arbitrary height.
				// Cube draws the full 3D shape.
				Gizmos.DrawWireCube(Vector3.down * gizmoSize.y * 0.5f, gizmoSize);
			}

			Gizmos.matrix = prevMatrix;
		}
	}
}