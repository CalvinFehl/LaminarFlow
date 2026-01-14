using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace FluidFrenzy
{
	/// <summary>
	/// <see cref="FluidSimulationObstacle"/> is a component that can be added to any object with a <see cref="Renderer"/> component attached, or configured to use a procedural shape. 
	/// </summary>
	/// <remarks>
	/// <para>
	/// When this component is attached, its shape and height are <c>orthographically rendered</c> onto the fluid simulation's underlying ground heightfield. This tells the <see cref="FluidSimulation"/> where the obstacle is, allowing the fluid to correctly flow around or over it. The obstacle itself can be <c>moved dynamically</c> during runtime.
	/// </para>
	/// <para>
	/// <c>Important Note on Movement:</c> The simulation's heightfield model means the fluid cannot flow under the obstacle. If the obstacle moves quickly into a location where water is currently present, that water will be instantly forced on top of the obstacle. <c>Rapidly moving a large obstacle can cause extreme visual artifacts</c> (like large, unnatural splashes or wild fluid behavior) due to the sudden volume displacement. It is generally advised to use this component on objects that are mostly rounded (e.g., rocks or islands) or those that do not create highly concave shapes with the surrounding terrain.
	/// </para>
	/// </remarks>
	[HelpURL("https://frenzybyte.github.io/fluidfrenzy/docs/fluid_simulation_components/#fluid-simulation-obstacle")]
	public class FluidSimulationObstacle : MonoBehaviour
	{
		[SerializeField]
		internal int version = 1;

		/// <summary>
		/// Defines the source used to determine the obstacle's height and shape within the fluid simulation grid.
		/// </summary>
		public enum ObstacleMode
		{
			/// <summary>Automatically uses the height and bounds of the <see cref="Renderer"/> component attached to this GameObject.</summary>
			Renderer,
			/// <summary>Renders a procedural shape defined by the <see cref="shape"/> and <see cref="size"/> properties.</summary>
			Shape
		}

		/// <summary>
		/// Defines the type of procedural shape to use when <see cref="mode"/> is set to <see cref="ObstacleMode.Shape"/>.
		/// </summary>
		public enum ObstacleShape
		{
			Sphere,
			Box,
			Cylinder,
			Capsule,
			Ellipsoid,
			CappedCone,
			HexPrism,
			Wedge
		}

		static HashSet<FluidSimulationObstacle> unqiueWaterObstacles = new HashSet<FluidSimulationObstacle>();
		public static FluidSimulationObstacle[] waterObstacles { get; private set; } = new FluidSimulationObstacle[0];

		/// <summary>
		/// The method used to define the obstacle's shape for the heightmap render. Defaults to <see cref="ObstacleMode.Renderer"/>.
		/// </summary>
		public ObstacleMode mode = ObstacleMode.Renderer;

		/// <summary>
		/// The type of procedural primitive to use when <see cref="mode"/> is <see cref="ObstacleMode.Shape"/>.
		/// <list type="bullet">
		/// 	<item><description>Sphere</description></item>
		/// 	<item><description>Box</description></item>
		/// 	<item><description>Cylinder</description></item>
		/// 	<item><description>Capsule</description></item>
		/// 	<item><description>Ellipsoid</description></item>
		/// 	<item><description>CappedCone</description></item>
		/// 	<item><description>HexPrism</description></item>
		/// 	<item><description>Wedge</description></item>
		/// </list>
		/// </summary>
		public ObstacleShape shape = ObstacleShape.Sphere;

		/// <summary>
		/// Local offset from the Transform position.
		/// </summary>
		public Vector3 center = Vector3.zero;

		/// <summary>
		/// The XYZ dimensions for non-uniform procedural shapes.
		/// <list type="bullet">
		/// 	<item><term>Box:</term><description>The full width, height, and depth.</description></item>
		/// 	<item><term>Ellipsoid:</term><description>The diameter of the X, Y, and Z axes.</description></item>
		/// 	<item><term>Wedge:</term><description>The bounding dimensions of the wedge base and height.</description></item>
		/// </list>
		/// </summary>
		public Vector3 size = Vector3.one;

		/// <summary>
		/// The primary radius for rounded procedural shapes.
		/// <list type="bullet">
		///     <item><term>Sphere:</term><description>The radius of the sphere.</description></item>
		///     <item><term>Cylinder:</term><description>The radius of the base.</description></item>
		///     <item><term>HexPrism:</term><description>The radius of the base.</description></item>
		///     <item><term>Capsule:</term><description>The radius of the cylinder body and the hemispherical end-caps.</description></item>
		///     <item><term>Capped Cone:</term><description>The radius of the bottom base.</description></item>
		/// </list>
		/// </summary>
		public float radius = 0.5f;

		/// <summary>
		/// An secondary radius used for complex shapes. 
		/// <list type="bullet">
		/// 	<item><term>Capped Cone:</term><description>The radius of the top cap.</description></item>
		/// </list>
		/// </summary>
		public float secondaryRadius = 0.25f;

		/// <summary>
		/// The total length or height of the procedural shape along its alignment <see cref="direction"/>.
		/// </summary>
		public float height = 2.0f;

		/// <summary>
		/// The local axis that the procedural shape's height or length is aligned with.
		/// <list type="bullet">
		///     <item><term>0:</term><description>X-Axis (Horizontal)</description></item>
		///     <item><term>1:</term><description>Y-Axis (Vertical)</description></item>
		///     <item><term>2:</term><description>Z-Axis (Forward)</description></item>
		/// </list>
		/// </summary>
		public int direction = 1;

		/// <summary>
		/// Ensures that even sub-pixel geometry is captured during the heightfield bake.
		/// </summary>
		/// <remarks>
		/// Standard rasterization only renders a pixel if its center is covered by a triangle. 
		/// Conservative Rasterization renders a pixel if any part of it is touched by a triangle.
		/// 
		/// Enabling this prevents thin obstacles like thin walls from being missed 
		/// if they happen to fall between pixel centers, ensuring more reliable collision data.
		/// This may cause the obstacle to appear slightly larger than its actual mesh.
		/// <para>
		/// <b>Warning:</b> This feature requires hardware-level support. It is <b>not supported</b> on platforms like WebGL, OpenGL ES, 
		/// or older mobile devices. The system will automatically fall back to standard 
		/// rasterization on unsupported hardware.
		/// </para>
		/// </remarks>
		public bool conservativeRasterization = true;

		/// <summary>
		/// Enables multi-sampling to produce smoother edges for procedural shapes.
		/// </summary>
		/// <remarks>
		/// When disabled, procedural shapes are sampled at a single point per grid cell, which can result in jagged edges or
		/// stair stepping in the heightfield. When enabled, the shader performs a multi-sample average 
		/// to create a soft, anti-aliased edge.
		/// <para>
		/// <b>Warning:</b> Because this averages height values within a local neighborhood, perfectly vertical drops 
		/// (like the sides of a box) might be turned into slopes. This can lead to height leakage or cause fluid 
		/// to climb the edges of an obstacle instead of colliding with a sharp wall.
		/// </para>
		/// </remarks>
		public bool smoothRasterization = true;

		/// <summary>
		/// The <see cref="Renderer"/> component automatically detected and used when <see cref="mode"/> is set to <see cref="ObstacleMode.Renderer"/>.
		/// </summary>
		public Renderer obstacleRenderer { get; private set; }

		private bool m_changed = false;

		static void RegisterWaterInputObject(FluidSimulationObstacle obj)
		{
			unqiueWaterObstacles.Add(obj);
			if (unqiueWaterObstacles.Count == 0)
			{
				waterObstacles = new FluidSimulationObstacle[0];
				return;
			}
			waterObstacles = unqiueWaterObstacles.ToArray();
		}

		static void DeregisterWaterInputObject(FluidSimulationObstacle obj)
		{
			unqiueWaterObstacles.Remove(obj);
			if (unqiueWaterObstacles.Count == 0)
			{
				waterObstacles = new FluidSimulationObstacle[0];
				return;
			}
			waterObstacles = unqiueWaterObstacles.ToArray();
		}

		void Awake()
		{
			RegisterWaterInputObject(this);
			obstacleRenderer = GetComponent<Renderer>();
		}

		private void OnDestroy()
		{
			DeregisterWaterInputObject(this);
		}

		private void OnEnable()
		{
			FluidSimulationManager.RequestObstacleUpdate(true);
		}

		private void OnDisable()
		{
			FluidSimulationManager.RequestObstacleUpdate(true);
		}

		void Update()
		{
			if (transform.hasChanged || m_changed)
			{
				m_changed = false;
				transform.hasChanged = false;
				FluidSimulationManager.RequestObstacleUpdate(true);
			}
		}
		public void OnChanged()
		{
			m_changed = true;
		}

		internal void Process(FluidSimulation sim, CommandBuffer cmd, Material obstacleMaterial)
		{
			if (mode == ObstacleMode.Renderer && obstacleRenderer && obstacleRenderer.enabled)
			{
				bool useConservative = conservativeRasterization &&
									   SystemInfo.supportsConservativeRaster &&
									   SystemInfo.supportsGeometryShaders;

				int passIndex = useConservative ? 1 : 0;
				cmd.DrawRenderer(obstacleRenderer, obstacleMaterial, passIndex);
			}
			else
			{
				Vector3 worldPos = transform.TransformPoint(center);
				Vector3 scale = transform.lossyScale;
				Quaternion worldRot = transform.rotation;

				FluidSimulation.FluidObstacleData data = new FluidSimulation.FluidObstacleData
				{
					position = worldPos,
					rotation = worldRot,
					shape = shape,
					smooth = smoothRasterization,
					boxSize = Vector3.zero,
					radius = 0,
					radius2 = 0,
					height = 0
				};

				Vector3 absScale = new Vector3(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
				float maxScale = Mathf.Max(absScale.x, Mathf.Max(absScale.y, absScale.z));

				Quaternion dirOrientation = Quaternion.identity;
				float hScale = 1f;
				float rScale = 1f;

				if (shape == ObstacleShape.Cylinder || shape == ObstacleShape.Capsule ||
					shape == ObstacleShape.CappedCone || shape == ObstacleShape.HexPrism)
				{
					switch (direction)
					{
						case 0: // X-Axis
							dirOrientation = Quaternion.Euler(0, 90, 0);
							if (shape == ObstacleShape.HexPrism) dirOrientation *= Quaternion.Euler(0, 0, 90);

							hScale = absScale.x;
							rScale = Mathf.Max(absScale.y, absScale.z);
							break;
						case 1: // Y-Axis
							dirOrientation = Quaternion.Euler(-90, 0, 0);
							hScale = absScale.y;
							rScale = Mathf.Max(absScale.x, absScale.z);
							break;
						case 2: // Z-Axis
							dirOrientation = Quaternion.identity;
							hScale = absScale.z;
							rScale = Mathf.Max(absScale.x, absScale.y);
							break;
					}
					data.rotation = worldRot * dirOrientation;
				}

				switch (shape)
				{
					case ObstacleShape.Sphere:
						data.radius = radius * maxScale;
						break;

					case ObstacleShape.Box:
					case ObstacleShape.Wedge:
					case ObstacleShape.Ellipsoid:
						data.boxSize = Vector3.Scale(size, absScale);
						break;

					case ObstacleShape.Cylinder:
					case ObstacleShape.HexPrism:
						data.radius = radius * rScale;
						data.height = height * hScale;
						break;

					case ObstacleShape.Capsule:
						data.radius = radius * rScale;
						data.height = height * hScale;
						break;

					case ObstacleShape.CappedCone:
						data.radius = radius * rScale;
						data.radius2 = secondaryRadius * rScale;
						data.height = height * hScale;
						break;
				}

				sim.AddObstacle(cmd, data);
			}
		}

		void OnDrawGizmos()
		{
			if (mode != ObstacleMode.Shape) return;

			Gizmos.color = Color.yellow;

			Vector3 s = transform.lossyScale;
			Vector3 absScale = new Vector3(Mathf.Abs(s.x), Mathf.Abs(s.y), Mathf.Abs(s.z));
			float maxScale = Mathf.Max(absScale.x, Mathf.Max(absScale.y, absScale.z));
			Vector3 worldPos = transform.TransformPoint(center);
			Quaternion worldRot = transform.rotation;

			Quaternion dirRot = Quaternion.identity;
			float hScale = 1f;
			float rScale = 1f;

			switch (direction)
			{
				case 0: // X-Axis
					dirRot = Quaternion.Euler(0, 0, -90);
					hScale = absScale.x;
					rScale = Mathf.Max(absScale.y, absScale.z);
					break;
				case 1: // Y-Axis
					dirRot = Quaternion.identity;
					hScale = absScale.y;
					rScale = Mathf.Max(absScale.x, absScale.z);
					break;
				case 2: // Z-Axis
					dirRot = Quaternion.Euler(90, 0, 0);
					hScale = absScale.z;
					rScale = Mathf.Max(absScale.x, absScale.y);
					break;
			}

			Gizmos.matrix = Matrix4x4.TRS(worldPos, worldRot * dirRot, Vector3.one);

			switch (shape)
			{
				case ObstacleShape.Sphere:
					Gizmos.matrix = Matrix4x4.TRS(worldPos, worldRot, Vector3.one);
					Gizmos.DrawWireSphere(Vector3.zero, radius * maxScale);
					break;

				case ObstacleShape.Box:
					Gizmos.matrix = Matrix4x4.TRS(worldPos, worldRot, Vector3.one);
					Gizmos.DrawWireCube(Vector3.zero, Vector3.Scale(size, absScale));
					break;

				case ObstacleShape.Ellipsoid:
					Gizmos.matrix = Matrix4x4.TRS(worldPos, worldRot, Vector3.Scale(size, absScale));
					Gizmos.DrawWireSphere(Vector3.zero, 0.5f);
					break;

				case ObstacleShape.Wedge:
					Gizmos.matrix = Matrix4x4.TRS(worldPos, worldRot, Vector3.one);
					DrawWedge(Vector3.Scale(size, absScale) * 0.5f);
					break;

				case ObstacleShape.Cylinder:
					DrawCylinder(height * hScale, radius * rScale, radius * rScale);
					break;

				case ObstacleShape.HexPrism:
					DrawHexPrism(height * hScale, radius * rScale);
					break;

				case ObstacleShape.CappedCone:
					DrawCylinder(height * hScale, radius * rScale, secondaryRadius * rScale);
					break;

				case ObstacleShape.Capsule:
					DrawCapsule(height * hScale, radius * rScale);
					break;
			}
		}

		void DrawCylinder(float h, float rBot, float rTop)
		{
			float halfH = h * 0.5f;
			Vector3 top = Vector3.up * halfH;
			Vector3 bot = Vector3.down * halfH;

			DrawCircle(top, rTop);
			DrawCircle(bot, rBot);

			Gizmos.DrawLine(top + Vector3.forward * rTop, bot + Vector3.forward * rBot);
			Gizmos.DrawLine(top - Vector3.forward * rTop, bot - Vector3.forward * rBot);
			Gizmos.DrawLine(top + Vector3.right * rTop, bot + Vector3.right * rBot);
			Gizmos.DrawLine(top - Vector3.right * rTop, bot - Vector3.right * rBot);
		}

		void DrawHexPrism(float h, float r)
		{
			float cornerR = r / 0.866025f;
			float halfH = h * 0.5f;
			Vector3 top = Vector3.up * halfH;
			Vector3 bot = Vector3.down * halfH;

			Vector3[] vertsTop = new Vector3[6];
			Vector3[] vertsBot = new Vector3[6];

			for (int i = 0; i < 6; i++)
			{
				float deg = 30f + i * 60f;
				float rad = deg * Mathf.Deg2Rad;
				float x = Mathf.Cos(rad) * cornerR;
				float z = Mathf.Sin(rad) * cornerR;
				vertsTop[i] = top + new Vector3(x, 0, z);
				vertsBot[i] = bot + new Vector3(x, 0, z);
			}

			for (int i = 0; i < 6; i++)
			{
				Gizmos.DrawLine(vertsTop[i], vertsTop[(i + 1) % 6]);
				Gizmos.DrawLine(vertsBot[i], vertsBot[(i + 1) % 6]);
				Gizmos.DrawLine(vertsTop[i], vertsBot[i]);
			}
		}

		void DrawCapsule(float h, float r)
		{
			float halfH = h * 0.5f;
			Vector3 top = Vector3.up * halfH;
			Vector3 bot = Vector3.down * halfH;

			Gizmos.DrawLine(top + Vector3.forward * r, bot + Vector3.forward * r);
			Gizmos.DrawLine(top - Vector3.forward * r, bot - Vector3.forward * r);
			Gizmos.DrawLine(top + Vector3.right * r, bot + Vector3.right * r);
			Gizmos.DrawLine(top - Vector3.right * r, bot - Vector3.right * r);

			DrawCircle(top, r);
			DrawCircle(bot, r);
			DrawSemiCircle(top, r, Vector3.right, Vector3.up);
			DrawSemiCircle(top, r, Vector3.forward, Vector3.up);
			DrawSemiCircle(bot, r, Vector3.right, Vector3.down);
			DrawSemiCircle(bot, r, Vector3.forward, Vector3.down);
		}

		void DrawWedge(Vector3 s)
		{
			float w = s.x; float h = s.y; float d = s.z;

			Vector3 p1 = new Vector3(-w, -h, -d);
			Vector3 p2 = new Vector3(w, -h, -d); 
			Vector3 p3 = new Vector3(w, h, -d);

			Vector3 p4 = new Vector3(-w, -h, d);
			Vector3 p5 = new Vector3(w, -h, d);
			Vector3 p6 = new Vector3(w, h, d);

			Gizmos.DrawLine(p1, p2); Gizmos.DrawLine(p2, p3); Gizmos.DrawLine(p3, p1);
			Gizmos.DrawLine(p4, p5); Gizmos.DrawLine(p5, p6); Gizmos.DrawLine(p6, p4);
			Gizmos.DrawLine(p1, p4); Gizmos.DrawLine(p2, p5); Gizmos.DrawLine(p3, p6);
		}

		void DrawCircle(Vector3 center, float r) { DrawCircle(center, r, Vector3.up); }
		void DrawCircle(Vector3 center, float r, Vector3 normal)
		{
			Vector3 tangent = (Mathf.Abs(normal.y) > 0.9f) ? Vector3.right : Vector3.up;
			Vector3 bitangent = Vector3.Cross(normal, tangent).normalized;
			tangent = Vector3.Cross(bitangent, normal).normalized;
			Vector3 prev = center + tangent * r;
			for (int i = 1; i <= 24; i++)
			{
				float a = i * 360f / 24 * Mathf.Deg2Rad;
				Vector3 next = center + (tangent * Mathf.Cos(a) + bitangent * Mathf.Sin(a)) * r;
				Gizmos.DrawLine(prev, next); prev = next;
			}
		}
		void DrawSemiCircle(Vector3 center, float r, Vector3 axis, Vector3 up)
		{
			Vector3 prev = center + axis * r;
			for (int i = 1; i <= 12; i++)
			{
				float a = i * 180f / 12 * Mathf.Deg2Rad;
				Vector3 next = center + (axis * Mathf.Cos(a) + up * Mathf.Sin(a)) * r;
				Gizmos.DrawLine(prev, next); prev = next;
			}
		}
	}
}