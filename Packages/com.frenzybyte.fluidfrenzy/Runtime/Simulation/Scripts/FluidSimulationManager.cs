using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.LowLevel;

namespace FluidFrenzy
{
	public class FluidSimulationLoop
	{
#if UNITY_EDITOR
		static FluidSimulationLoop()
		{
			EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
		}

		private static void OnPlayModeStateChanged(PlayModeStateChange state)
		{
			if (state == PlayModeStateChange.ExitingPlayMode)
			{
				RemoveSystems();
			}
		}
#endif

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
		private static void Initialize()
		{
			PlayerLoopSystem currentLoop = PlayerLoop.GetCurrentPlayerLoop();
#if FLUIDFRENZY_RUN_UPDATE
			AddSimulationToPlayerLoop<UnityEngine.PlayerLoop.Update>(currentLoop, Update);
#else
			AddSimulationToPlayerLoop<UnityEngine.PlayerLoop.FixedUpdate>(currentLoop, FixedUpdate);
#endif

			Application.quitting += RemoveSystems;
		}

		private static void RemoveSystems()
		{
			PlayerLoopSystem currentLoop = PlayerLoop.GetCurrentPlayerLoop();
#if FLUIDFRENZY_RUN_UPDATE
			RemoveSimulationFromPlayerLoop<UnityEngine.PlayerLoop.Update>(currentLoop);
#else
			RemoveSimulationFromPlayerLoop<UnityEngine.PlayerLoop.FixedUpdate>(currentLoop);
#endif
		}

		private static void AddSimulationToPlayerLoop<T>(PlayerLoopSystem modifiedLoop, PlayerLoopSystem.UpdateFunction updateFunction)
		{
			int systemIndex = FindSystemIndex(modifiedLoop.subSystemList, typeof(T));

			if (systemIndex != -1)
			{
				var fixedUpdateSubSystems = modifiedLoop.subSystemList[systemIndex].subSystemList.ToList();

				var postFixedUpdateSystem = new PlayerLoopSystem
				{
					type = typeof(FluidSimulationManager),
					updateDelegate = updateFunction
				};

				fixedUpdateSubSystems.Add(postFixedUpdateSystem);
				modifiedLoop.subSystemList[systemIndex].subSystemList = fixedUpdateSubSystems.ToArray();
				PlayerLoop.SetPlayerLoop(modifiedLoop);
			}
		}

		private static void RemoveSimulationFromPlayerLoop<T>(PlayerLoopSystem currentLoop)
		{
			int systemIndex = FindSystemIndex(currentLoop.subSystemList, typeof(T));

			if (systemIndex != -1)
			{
				var fixedUpdateSubSystems = currentLoop.subSystemList[systemIndex].subSystemList.ToList();

				// Directly remove our system by its type.
				fixedUpdateSubSystems.RemoveAll(system => system.type == typeof(FluidSimulationManager));

				currentLoop.subSystemList[systemIndex].subSystemList = fixedUpdateSubSystems.ToArray();
				PlayerLoop.SetPlayerLoop(currentLoop);
			}
		}

		private static int FindSystemIndex(PlayerLoopSystem[] systems, Type systemType)
		{
			for (int i = 0; i < systems.Length; i++)
			{
				if (systems[i].type == systemType)
				{
					return i;
				}
			}
			return -1;
		}

		private static void Update()
		{
			FluidSimulationManager.Step(Time.deltaTime, 2);
		}

		private static void FixedUpdate()
		{
			FluidSimulationManager.Step(Time.fixedDeltaTime, 2);
		}
	}

	public class FluidSimulationManager
	{
		// This will hold a reference to the main simulation for editor scripts.
		private static FluidSimulation s_editorMainSimulationCache;

		private static List<FluidSimulation> s_simulations = new List<FluidSimulation>();
		/// <summary>
		/// Returns a list containing all<see cref="FluidSimulation">FluidSimulations</see> in the scene.
		/// </summary>
		public static List<FluidSimulation> simulations { get { return s_simulations; } private set { } }

		internal static float s_simulationTime = 0;

		public static void Register(FluidSimulation sim)
		{
			if (!s_simulations.Contains(sim))
			{
				s_simulations.Add(sim);
			}
		}

		public static void Deregister(FluidSimulation sim)
		{
			s_simulations.Remove(sim);
		}

		/// <summary>
		/// <inheritdoc cref="FluidSimulation.Step" path="/summary"/>
		/// </summary>
		/// <param name="deltaTime"><inheritdoc cref="FluidSimulation.Step" path="/param[@name='deltaTime']"/></param>
		/// <param name="maxSteps"><inheritdoc cref="FluidSimulation.Step" path="/param[@name='maxSteps']"/></param>
		public static void Step(float deltaTime, int maxSteps)
		{
			s_simulationTime += deltaTime;
			if (s_simulationTime < FluidSimulation.kMaxTimestep)
			{
				return;
			}

			int numSteps = Math.Min(maxSteps, Mathf.RoundToInt(s_simulationTime / FluidSimulation.kMaxTimestep));

			foreach (FluidSimulation sim in s_simulations)
			{
				if (sim == null || !sim.isActiveAndEnabled)
				{
					continue;
				}
				sim.PreUpdate(deltaTime, numSteps);
			}

			foreach (FluidSimulation sim in s_simulations)
			{
				if (sim == null || !sim.isActiveAndEnabled)
				{
					continue;
				}
				sim.Step(deltaTime, numSteps);
			}

			foreach (FluidRigidBody solid in FluidRigidBody.fluidRigidBodies)
			{
				if (solid == null || !solid.isActiveAndEnabled)
				{
					continue;
				}
				solid.PostUpdate();
			}

			s_simulationTime -= FluidSimulation.kMaxTimestep * numSteps;
		}


		/// <summary>
		/// Notifies all <see cref="FluidSimulation">FluidSimulations</see> that the their settings have changed.
		/// </summary>
		public static void MarkSettingsChanged(bool changed)
		{
			if (!Application.isPlaying)
				return;

			foreach (FluidSimulation sim in s_simulations)
				sim.MarkSettingsChanged(changed);
		}

		/// <summary>
		/// Notifies all <see cref="FluidSimulation">FluidSimulations</see> to regenerated their obstacle mask.
		/// </summary>
		public static void RequestObstacleUpdate(bool changed)
		{
			if (!Application.isPlaying)
				return;

			foreach (FluidSimulation sim in s_simulations)
				sim.RequestObstacleUpdate(changed);
		}

		/// <summary>
		/// Applies a flow effect to all <see cref="FluidSimulation">FluidSimulations</see> at a specified world position.
		/// </summary>
		/// <param name="worldPos">The world position where the flow is applied.</param>
		/// <param name="direction">The direction of the flow.</param>
		/// <param name="size">The size of the flow area.</param>
		/// <param name="strength">The strength of the flow effect.</param>
		/// <param name="falloff">The falloff used to control the gradient of the flow effect.</param>
		/// <param name="timestep">The time delta since the last update.</param>
		public static void ApplyFlow(Vector3 worldPos, Vector2 direction, Vector2 size, float strength, float falloff, float timestep)
		{
			foreach (FluidSimulation sim in s_simulations)
				sim.ApplyFlow(worldPos, direction, size, strength, falloff, timestep);
		}

		/// <summary>
		/// Applies a vortex flow effect to all <see cref="FluidSimulation">FluidSimulations</see> at a specified world position.
		/// </summary>
		/// <param name="worldPos">The world position where the vortex is centered.</param>
		/// <param name="size">The size of the vortex area.</param>
		/// <param name="innerStrength">The strength of the flow at the center of the vortex.</param>
		/// <param name="outerStrength">The strength of the flow at the outer edge of the vortex.</param>
		/// <param name="additive">If true, the vortex effect is added to existing flows; otherwise, it replaces them.</param>
		/// <param name="timestep">The time delta since the last update.</param>
		public static void ApplyFlowVortex(Vector3 worldPos, Vector2 size, float innerStrength, float outerStrength, float timestep)
		{
			foreach (FluidSimulation sim in s_simulations)
				sim.ApplyFlowVortex(worldPos, size, innerStrength, outerStrength, timestep);
		}

		/// <summary>
		/// Applies a force to all <see cref="FluidSimulation">FluidSimulations</see> at a specified world position.
		/// </summary>
		/// <param name="worldPos">The world position where the force is applied.</param>
		/// <param name="direction">The direction of the force.</param>
		/// <param name="size">The size of the area affected by the force.</param>
		/// <param name="strength">The strength of the force effect.</param>
		/// <param name="falloff">The falloff used to control the gradient of the force effect.</param>
		/// <param name="timestep">The time delta since the last update.</param>
		/// <param name="splash">If true, applies the force as a splash(outward) effect; otherwise, applies as a directional force.</param>
		public static void ApplyForce(Vector3 worldPos, Vector2 direction, Vector2 size, float strength, float falloff, float timestep, bool splash)
		{
			foreach (FluidSimulation sim in s_simulations)
				sim.ApplyForce(worldPos, direction, size, strength, falloff, timestep, splash);
		}

		/// <summary>
		/// Applies a vortex force to all <see cref="FluidSimulation">FluidSimulations</see> at a specified world position.
		/// </summary>
		/// <param name="worldPos">The world position where the vortex force is centered.</param>
		/// <param name="size">The size of the vortex area.</param>
		/// <param name="strength">The strength of the vortex effect.</param>
		/// <param name="falloff">The falloff used to control the gradient of the vortex effect.</param>
		/// <param name="timestep">The time delta since the last update.</param>
		public static void ApplyForceVortex(Vector3 worldPos, Vector2 size, float strength, float falloff, float timestep)
		{
			foreach (FluidSimulation sim in s_simulations)
				sim.ApplyForceVortex(worldPos, size, strength, falloff, timestep);
		}

		/// <summary>
		/// Applies a force effect based on a texture to all <see cref="FluidSimulation">FluidSimulations</see>.
		/// </summary>
		/// <param name="texture">The texture that defines the force effect.</param>
		/// <param name="strength">The strength of the force effect.</param>
		/// <param name="timestep">The time delta since the last update.</param>
		public static void ApplyForce(Texture texture, float strength, float timestep)
		{
			foreach (FluidSimulation sim in s_simulations)
				sim.ApplyForce(texture, strength, timestep);
		}

		/// <summary>
		/// Adds fluid to all <see cref="FluidSimulation">FluidSimulations</see> at a specified world position.
		/// </summary>
		/// <param name="worldPos">The world position where the fluid is added.</param>
		/// <param name="size">The size of the fluid area.</param>
		/// <param name="amount">The strength of the added fluid.</param>
		/// <param name="falloff">The falloff used to control the gradient/shape of the added fluid.</param>
		/// <param name="layer">The layer in which the fluid will be added.</param>
		/// <param name="timestep">The time delta since the last update.</param>
		public static void AddFluid(Vector3 worldPos, Vector2 size, float amount, float falloff, int layer, float timestep)
		{
			foreach (FluidSimulation sim in s_simulations)
				sim.AddFluidCircle(worldPos, size, amount, falloff, layer, timestep);
		}

		/// <summary>
		/// Samples the height data of all <see cref="FluidSimulation">FluidSimulations</see> as the specified world position when inside the bounds of the <see cref="FluidSimulation">FluidSimulations</see>.
		/// </summary>
		/// <param name="worldPos">
		/// The world space position where the height of the <see cref="FluidSimulation">FluidSimulations</see> should be sampled.
		/// </param>
		/// <param name="heightData">
		/// The result of the sample's height. 
		/// heightData.x contains the total height in worldspace, including the height of the underlying terrain.
		/// heightData.y contains depth of the fluid in relation to the underlying terrain.
		/// This paramter will returns Vector2.zero if there is no valid height for this simulation or if <paramref name="worldPos"/> is outside of the simulation's bounds.
		/// </param>
		/// <returns>Returns true when valid data has been sampled.
		/// Returns false if there was no valid data or if if <paramref name="worldPos"/> is outside of the simulation's bounds.
		/// </returns>
		public static bool GetHeight(Vector3 worldPos, out Vector2 heightData)
		{
			bool result = false;
			heightData = Vector2.zero;
			foreach (FluidSimulation sim in s_simulations)
			{
				if (sim.GetHeight(worldPos, out Vector2 height) == true)
				{
					result = true;
					heightData = height;
				}
			}
			return result;
		}

		/// <summary>
		/// Samples the height and layer data of the <see cref="FluidSimulation"/> at the specified world position when inside the bounds of the <see cref="FluidSimulation"/>.
		/// </summary>
		/// <param name="worldPos">
		/// The world space position where the height of the <see cref="FluidSimulation"/> should be sampled.
		/// </param>
		/// <param name="heightData">
		/// The result of the sample's height. 
		/// heightData.x contains the total height in worldspace, including the height of the underlying terrain.
		/// heightData.y contains depth of the fluid in relation to the underlying terrain.
		/// This paramter will returns Vector2.zero if there is no valid height for this simulation or if <paramref name="worldPos"/> is outside of the simulation's bounds.
		/// </param>
		/// <param name="layer">
		/// The most dominant/heighest layer index in the fluid field.
		/// </param> 
		/// <returns>Returns true when valid data has been sampled.
		/// Returns false if there was no valid data or if if <paramref name="worldPos"/> is outside of the simulation's bounds.
		/// </returns>
		public static bool GetHeightLayer(Vector3 worldPos, out Vector2 heightData, out int layer)
		{
			bool result = false;
			heightData = Vector2.zero;
			layer = 0;
			foreach (FluidSimulation sim in s_simulations)
			{
				if (sim.GetHeightLayer(worldPos, out Vector2 sampledHeight, out int sampledLayer) == true)
				{
					result = true;
					heightData = sampledHeight;
					layer = sampledLayer;
				}
			}
			return result;
		}

		/// <summary>
		/// Samples the height and velocity data of all <see cref="FluidSimulation">FluidSimulations</see> at the specified world position when inside the bounds of the <see cref="FluidSimulation">FluidSimulations</see>.
		/// </summary>
		/// <param name="worldPos">
		/// The world space position where the height and velocity of the <see cref="FluidSimulation">FluidSimulations</see> should be sampled.
		/// </param>
		/// <param name="heightData">
		/// The result of the sample's height. 
		/// heightData.x contains the total height in worldspace, including the height of the underlying terrain.
		/// heightData.y contains depth of the fluid in relation to the underlying terrain.
		/// This paramter will returns Vector2.zero if there is no valid height data for this simulation or if <paramref name="worldPos"/> is outside of the simulation's bounds.
		/// </param>
		/// <param name="velocity">
		/// The result of the sample's velocity in world space. 
		/// This paramter will returns Vector3.zero if there is no valid data for this simulation or if <paramref name="worldPos"/> is outside of the simulation's bounds.
		/// </param>
		/// <returns>Returns true when valid data has been sampled.
		/// Returns false if there was no valid data or if if <paramref name="worldPos"/> is outside of the simulation's bounds.
		/// </returns>
		public static bool GetHeightVelocity(Vector3 worldPos, out Vector2 heightData, out Vector3 velocity)
		{
			bool result = false;
			heightData = Vector2.zero;
			velocity = Vector3.zero;
			foreach (FluidSimulation sim in s_simulations)
			{
				if (sim.GetHeightVelocity(worldPos, out Vector2 height, out Vector3 vel) == true)
				{
					result = true;
					heightData = height;
					velocity = vel;
				}
			}
			return result;
		}

		/// <summary>
		/// Samples the and calculates a world space surface normal of all <see cref="FluidSimulation">FluidSimulations</see> at the specified world position when inside the bounds of the <see cref="FluidSimulation">FluidSimulations</see>.
		/// </summary>
		/// <param name="worldPos">
		/// The world space position where the height of the <see cref="FluidSimulation">FluidSimulations</see> should be sampled.
		/// </param>
		/// <param name="normal">
		/// The resulting normal that is sampled and calculated for the specified <param name="worldPos">. 
		/// </param>
		/// <returns>Returns true when valid data has been sampled.
		/// Returns false if there was no valid data or if if <paramref name="worldPos"/> is outside of the simulation's bounds.
		/// </returns>
		public static bool GetNormal(Vector3 worldPos, out Vector3 normal)
		{
			bool result = false;
			normal = Vector3.zero;
			foreach (FluidSimulation sim in s_simulations)
			{
				if (sim.GetNormal(worldPos, out Vector3 resultNormal) == true)
				{
					result = true;
					normal = resultNormal;
				}
			}
			return result;
		}

		/// <summary>
		/// Samples the fluid distance field to find the nearest fluid location.
		/// </summary>
		/// <param name="worldPos">
		/// The world space position where the height of the <see cref="FluidSimulation"/> should be sampled.
		/// </param>
		/// <param name="fluidLocation">
		/// The resulting nearest location to <param name="worldPos"> containing fluid in 2D space. The x and z are the location sampled from the distance field. The Y is the <see cref="worldPos"/>.y 
		/// </param>
		/// <returns>Returns true when valid data has been sampled.
		/// Returns false if there was no valid data or if if <paramref name="worldPos"/> is outside of the simulation's bounds.
		/// </returns>
		public static bool GetNearestFluidLocation2D(Vector3 worldPos, out Vector3 fluidLocation)
		{
			bool result = false;
			fluidLocation = Vector3.zero;
			foreach (FluidSimulation sim in s_simulations)
			{
				if (sim.GetNearestFluidLocation2D(worldPos, out Vector3 resultLocation) == true)
				{
					result = true;
					fluidLocation = resultLocation;
				}
			}
			return result;
		}
		[Obsolete("Replaced by GetNearestFluidLocation2D")]
		public static bool GetNeartestFluidLocation2D(Vector3 worldPos, out Vector3 fluidLocation)
		{
			return GetNearestFluidLocation2D(worldPos, out fluidLocation);
		}
		/// <summary>
		/// Samples the fluid distance field to find the nearest fluid location.
		/// </summary>
		/// <param name="worldPos">
		/// The world space position where the height of the <see cref="FluidSimulation"/> should be sampled.
		/// </param>
		/// <param name="fluidLocation">
		/// The resulting nearest location to <param name="worldPos"> containing fluid.
		/// </param>
		/// <returns>Returns true when valid data has been sampled.
		/// Returns false if there was no valid data or if if <paramref name="worldPos"/> is outside of the simulation's bounds.
		/// </returns>
		public static bool GetNearestFluidLocation3D(Vector3 worldPos, out Vector3 fluidLocation)
		{
			bool result = false;
			fluidLocation = Vector3.zero;
			foreach (FluidSimulation sim in s_simulations)
			{
				if (sim.GetNearestFluidLocation3D(worldPos, out Vector3 resultLocation) == true)
				{
					result = true;
					fluidLocation = resultLocation;
				}
			}
			return result;
		}

		[Obsolete("Replaced by GetNearestFluidLocation3D")]
		public static bool GetNeartestFluidLocation3D(Vector3 worldPos, out Vector3 fluidLocation)
		{
			return GetNearestFluidLocation3D(worldPos, out fluidLocation);
		}


		/// <summary>
		/// Gets the primary FluidSimulation from the scene, caching the result for performance.
		/// This is safe to call from editor scripts like OnDrawGizmos.
		/// </summary>
		public static FluidSimulation GetEditorMainSimulation()
		{
			// If our cached reference is still valid, return it instantly.
			if (s_editorMainSimulationCache != null)
			{
				return s_editorMainSimulationCache;
			}

			// If we are in Play Mode, the registered list is the most reliable source.
			if (Application.isPlaying && s_simulations.Count > 0)
			{
				return s_simulations[0];
			}

#if UNITY_2023_2_OR_NEWER
			s_editorMainSimulationCache = FluidSimulation.FindAnyObjectByType<FluidSimulation>();
#else
			s_editorMainSimulationCache = FluidSimulation.FindObjectOfType<FluidSimulation>();
#endif
			return s_editorMainSimulationCache;
		}
	}
}