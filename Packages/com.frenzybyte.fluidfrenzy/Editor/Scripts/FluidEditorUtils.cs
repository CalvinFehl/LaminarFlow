using UnityEditor;
using UnityEngine;

namespace FluidFrenzy.Editor
{
	public class FluidEditorUtils
	{
		/// <summary>
		/// Creates a new GameObject with a FluidModifierVolume component set up as a Source.
		/// </summary>
		/// <param name="position">The world position of the new object.</param>
		/// <param name="rotation">The rotation of the new object.</param>
		/// <param name="size">The size of the fluid source.</param>
		/// <param name="strength">The strength of the fluid source.</param>
		/// <param name="layer">The fluid layer index to affect.</param>
		/// <param name="name">Optional name for the GameObject.</param>
		public static GameObject CreateFluidModifierSource(Vector3 position, Quaternion rotation, Vector2 size, float strength, int layer, string name = "Fluid Volume (Source) ")
		{
			GameObject fluidSource = new GameObject(name, typeof(FluidModifierVolume));
			fluidSource.transform.position = position;
			fluidSource.transform.rotation = rotation;

			if (fluidSource.TryGetComponent(out FluidModifierVolume volume))
			{
				volume.type = FluidModifierVolume.FluidModifierType.Source;
				volume.sourceSettings.layer = layer;
				volume.sourceSettings.strength = strength;
				volume.sourceSettings.size = size;
				volume.sourceSettings.mode = FluidModifierVolume.FluidSourceSettings.FluidSourceMode.Circle;
				volume.sourceSettings.falloff = 1;
			}
			Undo.RegisterCreatedObjectUndo(fluidSource, "CreateFluidModifierVolume");
			return fluidSource;
		}

		/// <summary>
		/// Creates a new GameObject with a FluidModifierVolume component set up to apply Flow (Velocity).
		/// </summary>
		/// <param name="position">The world position of the new object.</param>
		/// <param name="rotation">The rotation of the new object (determines flow direction).</param>
		/// <param name="size">The size of the flow area.</param>
		/// <param name="strength">The speed/strength of the flow.</param>
		/// <param name="name">Optional name for the GameObject.</param>
		public static GameObject CreateFluidModifierFlow(Vector3 position, Quaternion rotation, Vector2 size, float strength, string name = "Fluid Volume (Flow)")
		{
			GameObject fluidFlow = new GameObject(name, typeof(FluidModifierVolume));
			fluidFlow.transform.position = position;
			fluidFlow.transform.rotation = Quaternion.identity;

			if (fluidFlow.TryGetComponent(out FluidModifierVolume volume))
			{
				float rad = rotation.eulerAngles.y * Mathf.Deg2Rad;
				volume.type = FluidModifierVolume.FluidModifierType.Flow;
				volume.flowSettings.strength = strength;
				volume.flowSettings.direction = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
				volume.flowSettings.size = size;
				volume.flowSettings.mode = FluidModifierVolume.FluidFlowSettings.FluidFlowMode.Circle;
				volume.flowSettings.falloff = 1;
			}
			Undo.RegisterCreatedObjectUndo(fluidFlow, "CreateFluidModifierFlow");
			return fluidFlow;
		}

		/// <summary>
		/// Creates a new GameObject with a FluidModifierVolume component set up to apply Force.
		/// </summary>
		/// <param name="position">The world position of the new object.</param>
		/// <param name="rotation">The rotation of the new object (determines force direction).</param>
		/// <param name="size">The size of the force area.</param>
		/// <param name="strength">The magnitude of the force.</param>
		/// <param name="name">Optional name for the GameObject.</param>
		public static GameObject CreateFluidModifierForce(Vector3 position, Quaternion rotation, Vector2 size, float strength, string name = "Fluid Volume (Force)")
		{
			GameObject fluidForce = new GameObject(name, typeof(FluidModifierVolume));
			fluidForce.transform.position = position;
			fluidForce.transform.rotation = Quaternion.identity;

			if (fluidForce.TryGetComponent(out FluidModifierVolume volume))
			{
				float rad = rotation.eulerAngles.y * Mathf.Deg2Rad;
				volume.type = FluidModifierVolume.FluidModifierType.Force;
				volume.forceSettings.strength = strength;
				volume.forceSettings.direction = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
				volume.forceSettings.size = size;
				volume.forceSettings.mode = FluidModifierVolume.FluidForceSettings.FluidForceMode.Circle;
				volume.forceSettings.falloff = 1;
			}
			Undo.RegisterCreatedObjectUndo(fluidForce, "CreateFluidModifierForce");
			return fluidForce;
		}

		/// <summary>
		/// Creates a new GameObject with a FluidModifierVolume component pre-configured as a Vortex.
		/// </summary>
		/// <param name="position">The world position of the new object.</param>
		/// <param name="rotation">The rotation of the new object.</param>
		/// <param name="size">The size of the vortex effect.</param>
		/// <param name="name">Optional name for the GameObject.</param>
		public static GameObject CreateFluidModifierVortex(Vector3 position, Quaternion rotation, Vector2 size, string name = "Fluid Volume (Vortex)")
		{
			GameObject fluidVortex = new GameObject(name, typeof(FluidModifierVolume));
			fluidVortex.transform.position = position;
			fluidVortex.transform.rotation = rotation;

			if (fluidVortex.TryGetComponent(out FluidModifierVolume volume))
			{
				volume.type = FluidModifierVolume.FluidModifierType.Flow | FluidModifierVolume.FluidModifierType.Force;
				volume.flowSettings.mode = FluidModifierVolume.FluidFlowSettings.FluidFlowMode.Vortex;
				volume.flowSettings.strength = 20;
				volume.flowSettings.size = size;
				volume.flowSettings.radialFlowStrength = 7;
				volume.flowSettings.blendMode = FluidSimulation.FluidModifierBlendMode.Additive;

				volume.forceSettings.mode = FluidModifierVolume.FluidForceSettings.FluidForceMode.Vortex;
				volume.forceSettings.size = size / 1.5f;
				volume.forceSettings.strength = 1;
				volume.forceSettings.falloff = 1.5f;
			}
			Undo.RegisterCreatedObjectUndo(fluidVortex, "CreateFluidModifierVortex");
			return fluidVortex;
		}

		/// <summary>
		/// Creates a new GameObject with a TerrainModifier component for eroding or modifying terrain data.
		/// </summary>
		/// <param name="position">The world position of the new object.</param>
		/// <param name="rotation">The rotation of the new object.</param>
		/// <param name="size">The size of the modification area.</param>
		/// <param name="strength">The intensity of the modification.</param>
		/// <param name="layer">The terrain layer (e.g., Height, Water) to modify.</param>
		/// <param name="splatchannel">The specific splat channel to target (if applicable).</param>
		/// <param name="name">Optional name for the GameObject.</param>
		public static GameObject CreateTerrainModifier(Vector3 position, Quaternion rotation, Vector2 size, float strength, ErosionLayer.TerrainLayer layer, ErosionLayer.SplatChannel splatchannel, string name = "Terrain Modifier")
		{
			GameObject terrainModifier = new GameObject(name, typeof(TerrainModifier));
			terrainModifier.transform.position = position;
			terrainModifier.transform.rotation = rotation;

			if (terrainModifier.TryGetComponent(out TerrainModifier modifier))
			{
				modifier.settings.layer = layer;
				modifier.settings.splat = splatchannel;
				modifier.settings.strength = strength;
				modifier.settings.size = size;
				modifier.settings.mode = TerrainModifier.TerrainInputMode.Circle;
				modifier.settings.falloff = 1;
			}
			Undo.RegisterCreatedObjectUndo(terrainModifier, "CreateTerrainModifierVolume");
			return terrainModifier;
		}

		/// <summary>
		/// Creates a new GameObject with a TerraformModifier component.
		/// </summary>
		/// <param name="position">The world position of the new object.</param>
		/// <param name="rotation">The rotation of the new object.</param>
		/// <param name="size">The size of the terraforming area.</param>
		/// <param name="layer">The terrain layer to target.</param>
		/// <param name="splatchannel">The splat channel to target.</param>
		/// <param name="name">Optional name for the GameObject.</param>
		public static GameObject CreateTerraformModifier(Vector3 position, Quaternion rotation, Vector3 size, string name = "Terraform Modifier")
		{
			GameObject terraformModifier = new GameObject(name, typeof(TerraformModifier));
			terraformModifier.transform.position = position;
			terraformModifier.transform.rotation = rotation;

			if (terraformModifier.TryGetComponent(out TerraformModifier modifier))
			{
				modifier.settings.size = size;
				modifier.settings.falloff = 1;
			}
			Undo.RegisterCreatedObjectUndo(terraformModifier, "CreateTerraformModifier");
			return terraformModifier;
		}

		/// <summary>
		/// Creates a new procedural obstacle for the fluid simulation.
		/// </summary>
		/// <param name="shape">The shape of the obstacle.</param>
		/// <param name="position">The world position of the new object.</param>
		/// <param name="rotation">The rotation of the new object.</param>
		/// <param name="size">The dimensions of the obstacle.</param>
		/// <param name="name">Optional name for the GameObject. Defaults to "Fluid Obstacle {Shape}" if null.</param>
		public static GameObject CreateProceduralObstacle(FluidSimulationObstacle.ObstacleShape shape, Vector3 position, Quaternion rotation, Vector3 size, string name = null)
		{
			if (string.IsNullOrEmpty(name))
			{
				name = $"Fluid Obstacle {shape}";
			}

			GameObject obstacleObject = new GameObject(name, typeof(FluidSimulationObstacle));
			obstacleObject.transform.position = position;
			obstacleObject.transform.rotation = rotation;

			if (obstacleObject.TryGetComponent(out FluidSimulationObstacle obstacle))
			{
				obstacle.mode = FluidSimulationObstacle.ObstacleMode.Shape;
				obstacle.shape = shape;
				obstacle.size = size;
				obstacle.radius = size.x;
				obstacle.secondaryRadius = obstacle.radius * 0.5f;
				obstacle.height = size.y * 0.5f;
			}
			Undo.RegisterCreatedObjectUndo(obstacleObject, "CreateProceduralObstacle");
			return obstacleObject;
		}

		/// <summary>
		/// Creates a Box obstacle defined by its dimensions.
		/// </summary>
		/// <param name="position">The world position of the obstacle.</param>
		/// <param name="rotation">The rotation of the obstacle.</param>
		/// <param name="size">The Width, Height, and Depth of the box.</param>
		/// <param name="name">Optional name for the GameObject.</param>
		public static GameObject CreateObstacleBox(Vector3 position, Quaternion rotation, Vector3 size, string name = "Fluid Obstacle (Box)")
		{
			return CreateProceduralObstacle(FluidSimulationObstacle.ObstacleShape.Box, position, rotation, size, name);
		}

		/// <summary>
		/// Creates a Circle/Sphere obstacle defined by its radius.
		/// </summary>
		/// <param name="position">The world position of the obstacle.</param>
		/// <param name="rotation">The rotation of the obstacle.</param>
		/// <param name="radius">The radius of the circle.</param>
		/// <param name="name">Optional name for the GameObject.</param>
		public static GameObject CreateObstacleSphere(Vector3 position, Quaternion rotation, float radius, string name = "Fluid Obstacle (Sphere)")
		{
			Vector3 size = Vector3.one * radius;
			return CreateProceduralObstacle(FluidSimulationObstacle.ObstacleShape.Sphere, position, rotation, size, name);
		}

		/// <summary>
		/// Creates a Cylinder obstacle defined by radius and length.
		/// </summary>
		/// <param name="position">The world position of the obstacle.</param>
		/// <param name="rotation">The rotation of the obstacle.</param>
		/// <param name="radius">The radius of the cylinder base.</param>
		/// <param name="length">The total height (length) of the cylinder.</param>
		/// <param name="name">Optional name for the GameObject.</param>
		public static GameObject CreateObstacleCylinder(Vector3 position, Quaternion rotation, float radius, float length, string name = "Fluid Obstacle (Cylinder)")
		{
			// X and Z are radius, Y is height
			Vector3 size = new Vector3(radius, length, radius);
			return CreateProceduralObstacle(FluidSimulationObstacle.ObstacleShape.Cylinder, position, rotation, size, name);
		}

		/// <summary>
		/// Creates an Elipse obstacle defined by its radius per axis.
		/// </summary>
		/// <param name="position">The world position of the obstacle.</param>
		/// <param name="rotation">The rotation of the obstacle.</param>
		/// <param name="radius">The radius of the elipse on each axis.</param>
		/// <param name="name">Optional name for the GameObject.</param>
		public static GameObject CreateObstacleElipse(Vector3 position, Quaternion rotation, Vector3 radius, string name = "Fluid Obstacle (Elipse)")
		{
			Vector3 size = radius;
			return CreateProceduralObstacle(FluidSimulationObstacle.ObstacleShape.Ellipsoid, position, rotation, size, name);
		}

		/// <summary>
		/// Creates a Wedge obstacle defined by its dimensions.
		/// </summary>
		/// <param name="position">The world position of the obstacle.</param>
		/// <param name="rotation">The rotation of the obstacle.</param>
		/// <param name="size">The size dimensions of the wedge.</param>
		/// <param name="name">Optional name for the GameObject.</param>
		public static GameObject CreateObstacleWedge(Vector3 position, Quaternion rotation, Vector3 size, string name = "Fluid Obstacle (Wedge)")
		{
			return CreateProceduralObstacle(FluidSimulationObstacle.ObstacleShape.Wedge, position, rotation, size, name);
		}

		/// <summary>
		/// Creates a Hexagonal Prism obstacle defined by its radius and height.
		/// </summary>
		/// <param name="position">The world position of the obstacle.</param>
		/// <param name="rotation">The rotation of the obstacle.</param>
		/// <param name="radius">The radius of the hexagon.</param>
		/// <param name="height">The height of the prism.</param>
		/// <param name="name">Optional name for the GameObject.</param>
		public static GameObject CreateObstacleHexPrism(Vector3 position, Quaternion rotation, float radius, float height, string name = "Fluid Obstacle (HexPrism)")
		{
			Vector3 size = new Vector3(radius, height, radius);
			return CreateProceduralObstacle(FluidSimulationObstacle.ObstacleShape.HexPrism, position, rotation, size, name);
		}

		/// <summary>
		/// Creates a Cone obstacle defined by its radius and height.
		/// </summary>
		/// <param name="position">The world position of the obstacle.</param>
		/// <param name="rotation">The rotation of the obstacle.</param>
		/// <param name="radius">The radius of the cone base.</param>
		/// <param name="height">The height of the cone.</param>
		/// <param name="name">Optional name for the GameObject.</param>
		public static GameObject CreateObstacleCone(Vector3 position, Quaternion rotation, float radius, float height, string name = "Fluid Obstacle (Cone)")
		{
			Vector3 size = new Vector3(radius, height, radius);
			return CreateProceduralObstacle(FluidSimulationObstacle.ObstacleShape.CappedCone, position, rotation, size, name);
		}

		/// <summary>
		/// Creates a Capsule obstacle defined by its radius and height.
		/// </summary>
		/// <param name="position">The world position of the obstacle.</param>
		/// <param name="rotation">The rotation of the obstacle.</param>
		/// <param name="radius">The radius of the capsule.</param>
		/// <param name="height">The height of the capsule.</param>
		/// <param name="name">Optional name for the GameObject.</param>
		public static GameObject CreateObstacleCapsule(Vector3 position, Quaternion rotation, float radius, float height, string name = "Fluid Obstacle (Capsule)")
		{
			Vector3 size = new Vector3(radius, height, radius);
			return CreateProceduralObstacle(FluidSimulationObstacle.ObstacleShape.Capsule, position, rotation, size, name);
		}
	}
}