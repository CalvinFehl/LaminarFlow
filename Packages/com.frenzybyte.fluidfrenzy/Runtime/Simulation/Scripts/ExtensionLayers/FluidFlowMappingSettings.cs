using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Serialization;

namespace FluidFrenzy
{
	/// <summary>
	/// A <see cref="ScriptableObject"/> asset containing configuration parameters for <see cref="FluidFlowMapping"/>.
	/// </summary>
	/// <remarks>
	/// Using a settings asset allows for easy reuse and centralized modification of flow mapping behaviors across multiple simulation instances.
	/// <para>
	/// To create a new settings asset, navigate to: <c>Assets > Create > Fluid Frenzy > Flow Mapping Settings</c>.
	/// </para>
	/// </remarks>
	[CreateAssetMenu(fileName = "Flow Mapping Settings", menuName = "FluidSimulation/Flow Mapping Settings", order = 51)]
	[HelpURL("https://frenzybyte.github.io/fluidfrenzy/docs/fluid_simulation_components/#fluid-flow-mapping-settings")]
	public class FluidFlowMappingSettings : ScriptableObject
	{
		[SerializeField]
		internal int version = 1;

		/// <summary>
		/// Defines the technique used to render the fluid's surface flow and texture advection.
		/// </summary>
		/// <remarks>
		/// <list type="bullet">
		/// 	<item>
		/// 		<term><c>Off</c></term>
		/// 		<description>No flow mapping is applied.</description>
		/// 	</item>
		/// 	<item>
		/// 		<term><c>Static</c></term>
		/// 		<description>Flow mapping is performed directly in the shader by offsetting UV coordinates based on the instantaneous velocity field.</description>
		/// 	</item>
		/// 	<item>
		/// 		<term><c>Dynamic</c></term>
		/// 		<description>Utilizes a separate simulation buffer to calculate UV offsets. The UVs are advected over time, similar to the velocity field and foam mask. This allows for complex swirling effects but may accumulate distortion over longer periods.</description>
		/// 	</item>
		/// </list>
		/// </remarks>
		public FluidFlowMapping.FlowMappingMode flowMappingMode = FluidFlowMapping.FlowMappingMode.Static;

		/// <summary>
		/// A multiplier applied to the velocity vectors when calculating UV offsets.
		/// </summary>
		/// <remarks>
		/// Higher values create the appearance of faster-moving fluid but increase the visual distortion (stretching) of the surface texture.
		/// </remarks>
		public float flowSpeed = 1;

		/// <summary>
		/// Controls the frequency at which the flow map cycle resets to its original UV coordinates.
		/// </summary>
		/// <remarks>
		/// Continuous advection eventually distorts textures beyond recognition. To prevent this, the system resets the UVs periodically.
		/// <para>
		/// Increasing this value makes the reset occur more frequently, reducing maximum distortion. To hide the visual "pop" during a reset, the texture is sampled multiple times with offset phases and blended based on this cycle speed.
		/// </para>
		/// </remarks>
		public float flowPhaseSpeed = 0.5f;
	}
}