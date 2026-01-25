using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Serialization;

namespace FluidFrenzy
{
	/// <summary>
	/// Represents the specialized settings for the <see cref="FluxFluidSimulation"/> solver.
	/// This class extends <see cref="FluidSimulationSettings"/> to include specific properties and parameters unique to the Flux simulation algorithm.
	/// </summary>
	/// <remarks>
	/// <para>
	/// As a <see cref="ScriptableObject"/> asset, <c>FluxFluidSimulationSettings</c> allows for the reuse and quick modification of a specific Flux simulation profile across multiple <see cref="FluxFluidSimulation"/> components. This ensures consistency and simplifies rapid iteration on simulation characteristics.
	/// </para>
	/// 
	/// <h4>Creation</h4>
	/// <para>
	/// To create a new Flux-specific simulation settings asset, navigate to: <c>Assets > Create > Fluid Frenzy > Flux > Simulation Settings</c>.
	/// </para>
	/// 
	/// <h4>Specialized Flux Properties</h4>
	/// <para>
	/// This asset contains all settings that are specific to the "Flux" implementation of the fluid solver, inheriting all universal properties from the base <see cref="FluidSimulationSettings"/>. These properties are used to control the unique behaviors, stability, and optimizations of the Flux algorithm.
	/// </para>
	/// </remarks>
	[CreateAssetMenu(fileName = "FluidSimulationSettings", menuName = "FluidSimulation/Flux/Simulation Settings", order = 51)]
	[HelpURL("https://frenzybyte.github.io/fluidfrenzy/docs/fluid_simulation_components/#flux-fluid-simulation-settings")]
	public class FluxFluidSimulationSettings : FluidSimulationSettings
	{
		/// <summary>
		/// Controls whether the newly calculated velocity for the current frame is added to or overwrites the previous frame's velocity.
		/// </summary>
		/// <remarks>
		/// <list type="bullet">
		/// 	<item>
		/// 		<term>Enabled (Additive)</term>
		/// 		<description>The current frame's velocity is accumulated onto the existing velocity map. This is essential for simulating persistent effects like continuous flow, pressure buildup, and rotational momentum (swirls/eddies).</description>
		/// 	</item>
		/// 	<item>
		/// 		<term>Disabled (Overwrite)</term>
		/// 		<description>The velocity map is reset each frame to only contain the velocity calculated from the fluid's movement during that single frame. This typically results in a less continuous, more reactive flow.</description>
		/// 	</item>
		/// </list>
		/// </remarks>
		public bool additiveVelocity = true;

		/// <summary>
		/// Controls the resolution (width and height) of the internal Velocity Field texture.
		/// </summary>
		/// <remarks>
		/// This texture stores the flow direction and magnitude used for advection. The resolution is often lower than the main fluid grid to save memory and processing time.
		/// </remarks>
		public Vector2Int velocityTextureSize = new Vector2Int(512, 512);

		/// <summary>
		/// A percentage of padding added to the borders of the velocity flow map.
		/// </summary>
		/// <remarks>
		/// This padding is specifically designed for use in tiled fluid simulations (currently in **beta**) to ensure smooth flow continuity between adjacent tiles. It should typically be set to 0 if tiling is not used.
		/// </remarks>
		public float paddingScale = 0;

		/// <summary>
		/// Scales the distance the velocity field <see href="https://en.wikipedia.org/wiki/Advection">advects</see> (carries) itself and other data maps like the <see cref="FoamLayer"/>. 
		/// </summary>
		/// <remarks>
		/// A larger value causes the flow patterns, foam, and dynamic flow mapping data to be carried further by the fluid movement each frame, effectively increasing the visual influence of the velocity field.
		/// </remarks>
		public float advectionScale = 1;
		/// <summary>
		/// Determines the number of Jacobi iterations used to solve for the fluid's pressure field (the incompressible part of the velocity).
		/// </summary>
		/// <remarks>
		/// This step is crucial for fluid incompressibility. Increasing the iterations improves the accuracy of the pressure solution and reduces volume loss but increases the computational cost. The default value of 5 is generally recommended for visual quality.
		/// </remarks>
		public int advectionIterations = 5;
		/// <summary>
		/// Scales down the accumulated velocity of the fluid each frame to slow down movement when no new acceleration is applied.
		/// </summary>
		/// <remarks>
		/// This damping acts as a friction or viscosity factor. Higher values dampen the velocity faster, causing the fluid to come to rest more quickly.
		/// </remarks>
		public float velocityDamping = 0.1f;
		/// <summary>
		/// The factor by which the newly generated fluid velocity (outflow) is applied to the final velocity map texture.
		/// </summary>
		/// <remarks>
		/// This value controls the responsiveness and maximum speed of the flow. A higher scale means the fluid accelerates faster, resulting in more pronounced and quickly-appearing flow patterns.
		/// </remarks>
		public float velocityScale = 1.0f;
		/// <summary>
		/// Clamps the magnitude of the velocity field vector to a maximum value.
		/// </summary>
		/// <remarks>
		/// This prevents the fluid from accelerating past a defined maximum speed, which helps maintain numerical stability and controls the intensity of the flow.
		/// </remarks>
		public float velocityMax = 10.0f;
		/// <summary>
		/// Scales the perceived incompressibility of the fluid's velocity field when solving for pressure.
		/// </summary>
		/// <remarks>
		/// A higher value forces the fluid to "push out" more aggressively to neighboring cells. Tweaking this value significantly influences the size and intensity of swirls/eddies and the pressure buildup around obstacles.
		/// </remarks>
		public float pressure = 0.9f;

		/// <summary>
		/// Secondary Layer: Controls whether the newly calculated velocity is added to or overwrites the previous frame's velocity.
		/// </summary>
		/// <remarks>
		/// <inheritdoc cref="additiveVelocity" path="/remarks"/>
		/// </remarks>
		public bool secondLayerAdditiveVelocity = false;
		/// <summary>
		/// Secondary Layer: The factor by which the newly generated fluid velocity is applied to the final velocity map texture.
		/// </summary>
		/// <remarks>
		/// <inheritdoc cref="velocityScale" path="/remarks"/>
		/// </remarks>
		public float secondLayerVelocityScale = 0.125f;

		/// <summary>
		/// Enables a custom viscosity control model for the second fluid layer.
		/// </summary>
		/// <remarks>
		/// When enabled, this feature allows the second fluid to flow more slowly on shallow slopes and stack up to a certain height before flowing, which is useful for simulating highly viscous fluids like lava.
		/// </remarks>
		public bool secondLayerCustomViscosity = true;
		/// <summary>
		/// Scales the flow speed of the second layer when <see cref="secondLayerCustomViscosity"/> is enabled.
		/// </summary>
		/// <remarks>
		/// This factor determines the fluid's viscosity. The fluid volume leaves the cell at a slower rate than its calculated velocity, simulating thicker fluid. A higher value results in more viscous, slower flow.
		/// </remarks>
		public float secondLayerViscosity = 0.75f;
		/// <summary>
		/// Indicates the minimum height (thickness) the second layer fluid must achieve before it begins to flow significantly on flat or near-flat surfaces.
		/// </summary>
		/// <remarks>
		/// This simulates the non-Newtonian behavior of highly viscous fluids like lava, where an initial minimum head height is required to overcome internal friction before flow commences.
		/// </remarks>
		public float secondLayerFlowHeight = 0.1f;
	}
}