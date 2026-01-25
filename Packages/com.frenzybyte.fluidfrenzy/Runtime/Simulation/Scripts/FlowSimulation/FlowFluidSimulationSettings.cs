using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Serialization;

namespace FluidFrenzy
{

	/// <summary>
	/// Represents the specialized settings for the <see cref="FlowFluidSimulation"/> solver.
	/// This class extends <see cref="FluidSimulationSettings"/> to include specific properties and parameters unique to the Flow simulation algorithm.
	/// </summary>
	/// <remarks>
	/// <para>
	/// As a <see cref="ScriptableObject"/> asset, <c>FlowFluidSimulationSettings</c> allows for the reuse and quick modification of a specific Flow simulation profile across multiple <see cref="FlowFluidSimulation"/> components. This ensures consistency and simplifies rapid iteration on simulation characteristics.
	/// </para>
	/// 
	/// <h4>Creation</h4>
	/// <para>
	/// To create a new Flow-specific simulation settings asset, navigate to: <c>Assets > Create > Fluid Frenzy > Flow > Simulation Settings</c>.
	/// </para>
	/// 
	/// <h4>Specialized Flow Properties</h4>
	/// <para>
	/// This asset contains all settings that are specific to the "Flow" implementation of the fluid solver, building upon the universal properties inherited from the base <see cref="FluidSimulationSettings"/>. These properties control the unique behaviors and optimizations of the Flow algorithm.
	/// </para> 
	/// </remarks>
	[CreateAssetMenu(fileName = "FluidSimulationSettings", menuName = "FluidSimulation/Flow/Simulation Settings", order = 51)]
	[HelpURL("https://frenzybyte.github.io/fluidfrenzy/docs/fluid_simulation_components/#flow-fluid-simulation-settings")]
	public class FlowFluidSimulationSettings : FluidSimulationSettings
	{
		/// <summary>
		/// Clamps the magnitude of the fluid's acceleration to a maximum value per frame.
		/// </summary>
		/// <remarks>
		/// Limiting acceleration is a stability measure, as it prevents sudden, large forces from being applied to the fluid. This helps control the rate at which fluid speed changes and improves the overall stability of the simulation.
		/// </remarks>
		public float accelerationMax = 10.0f;
		/// <summary>
		/// Clamps the magnitude of the velocity field vector to a maximum value.
		/// </summary>
		/// <remarks>
		/// This prevents the fluid from accelerating past a defined maximum speed, which helps maintain numerical stability and controls the intensity of the flow.
		/// </remarks>
		public float velocityMax = 10.0f;

		/// <summary>
		/// Enables a technique to mitigate the amplification of wave heights that can occur when waves transition from deep to shallow water.
		/// </summary>
		/// <remarks>
		/// This feature prevents "spiking" artifacts from appearing at the edges of waves, particularly in areas of rapid depth change, by applying a necessary correction factor.
		/// </remarks>
		public bool overshootingReduction = false;
		/// <summary>
		/// A threshold that determines the sensitivity for detecting a significant change in wave height (a "wave edge") as the fluid transitions into shallow areas.
		/// </summary>
		/// <remarks>
		/// A lower value makes the reduction system more sensitive, applying the correction to smaller wave changes.
		/// </remarks>
		public float overshootingEdge = 1.0f;
		/// <summary>
		/// A scaling factor that adjusts the magnitude of the correction applied to reduce overshooting at detected wave edges.
		/// </summary>
		/// <remarks>
		/// This controls how aggressively the "spiking" artifacts are dampened. A higher value results in a stronger smoothing effect.
		/// </remarks>
		public float overshootingScale = 0.1f;

		/// <summary>
		/// Secondary Layer: Clamps the magnitude of the second fluid layer's acceleration to a maximum value per frame.
		/// </summary>
		/// <remarks>
		/// <inheritdoc cref="accelerationMax" path="/remarks"/>
		/// </remarks>
		public float secondLayerAccelerationMax = 10.0f;
		/// <summary>
		/// Secondary Layer: Clamps the magnitude of the second fluid layer's velocity vector to a maximum value.
		/// </summary>
		/// <remarks>
		/// <inheritdoc cref="velocityMax" path="/remarks"/>
		/// </remarks>
		public float secondLayerVelocityMax = 10.0f;
	}
}