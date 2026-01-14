using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Serialization;

namespace FluidFrenzy
{

	/// <summary>
	/// This is the abstract base class for all simulation settings, serving as a <see cref="ScriptableObject"/> asset assigned to a <see cref="FluidSimulation"/>. 
	/// It defines the essential, global properties shared across all fluid simulation types.
	/// </summary>
	/// <remarks>
	/// <para>
	/// By utilizing a <see cref="ScriptableObject"/> asset, <c>FluidSimulationSettings</c> simplifies the configuration workflow. This approach allows a single settings profile to be reused and instantly modified across multiple <see cref="FluidSimulation"/> components within a project, ensuring consistent simulation behavior.
	/// </para>
	/// 
	/// <h4>Creation</h4>
	/// <para>
	/// To create a new simulation settings asset, navigate to: <c>Assets > Create > Fluid Frenzy > Simulation Settings</c>.
	/// </para>	
	/// 
	/// <h4>Properties</h4>
	/// <para>
	/// This base class contains all parameters and properties that are universal to the core fluid heightfield simulation system, independent of the specific solver (e.g., Flux, Flow). Any specialized settings for a particular solver will be defined in classes that derive from <c>FluidSimulationSettings</c>.
	/// </para>
	/// </remarks>
	[CreateAssetMenu(fileName = "FluidSimulationSettings", menuName = "FluidSimulation/Simulation Settings", order = 51)]
	public class FluidSimulationSettings : ScriptableObject
	{
		[SerializeField]
		internal int version = 1;

		/// <summary>
		/// Controls the resolution (width and height) of the simulation's 2D grid.
		/// </summary>
		/// <remarks>
		/// It is highly recommended to use power-of-two dimensions (e.g., 512x512 or 1024x1024) for optimal GPU performance. 
		/// A higher resolution increases the spatial accuracy of the fluid simulation but linearly increases both GPU memory usage and processing cost (frame time).
		/// </remarks>
		public Vector2Int numberOfCells = new Vector2Int(1024, 1024);
		/// <summary>
		/// A minimum fluid height threshold below which a cell is considered to have no fluid.
		/// </summary>
		/// <remarks>
		/// This value is primarily used to prevent minor visual artifacts or "clipping" issues that can arise from floating-point imprecision when a cell's fluid height is extremely close to zero. Any cell below this height is treated as empty.
		/// </remarks>
		public float clipHeight = 0.001f;
		/// <summary>
		/// Adjusts the internal scale factor of the fluid volume within each cell to control the effective flow speed.
		/// </summary>
		/// <remarks>
		/// A smaller `cellSize` implies less fluid volume per cell, which results in faster-flowing fluid and more energetic wave behavior for a given acceleration.
		/// </remarks>
		public float cellSize = 0.0125f;
		/// <summary>
		/// Adjusts the rate at which wave energy is dissipated (dampened) over time.
		/// </summary>
		/// <remarks>
		/// A higher value causes waves and ripples to fade away quickly, leading to a calmer surface, while a lower value allows waves to persist longer.
		/// </remarks>
		public float waveDamping = 0.1f;
		/// <summary>
		/// The force of gravity or acceleration applied to the fluid, which directly controls the speed of wave propagation.
		/// </summary>
		/// <remarks>
		/// This value simulates the effect of gravity (9.8 m/s² is typical) on the fluid and is the primary factor determining how quickly waves travel across the simulation domain.
		/// </remarks>
		public float acceleration = 9.8f;

		/// <summary>
		/// Determines whether fluid is allowed to leave the simulation domain at the boundaries.
		/// </summary>
		/// <remarks>
		/// When disabled, the boundaries act as solid walls, causing fluid to reflect and accumulate over time. When enabled, fluid passing over the border is removed, which maintains fluid consistency but causes a net loss of volume.
		/// </remarks>
		public bool openBorders = false;

		/// <summary>
		/// Enables asynchronous readback of the simulation's height and velocity data from the GPU to the CPU.
		/// </summary>
		/// <remarks>
		/// This is necessary for CPU-side interactions, such as buoyancy, floating objects, or gameplay logic that requires current fluid data. Since the readback is asynchronous to prevent performance stalls, the CPU data will lag behind the GPU simulation by a few frames.
		/// </remarks>
		public bool readBackHeight = true;
		/// <summary>
		/// The number of frames over which the CPU readback of the simulation data is sliced to spread the performance cost.
		/// </summary>
		/// <remarks>
		/// Time slicing divides the data transfer into smaller chunks over multiple frames. A higher value reduces the cost per frame but increases the total latency before the full simulation data is available on the CPU. The readback processes the simulation data vertically (from top to bottom).
		/// </remarks>
		public int readBackTimeSliceFrames = 4;

		/// <summary>
		/// Enables the asynchronous generation and readback of a distance field representing the nearest fluid location.
		/// </summary>
		/// <remarks>
		/// The distance field is generated on the GPU using the Jump Flood algorithm and then transferred to the CPU. This data provides the distance to the nearest fluid cell, which is useful for advanced gameplay logic or visual effects. Due to the asynchronous nature of the readback, the CPU data will lag behind the GPU simulation.
		/// </remarks>
		public bool distanceFieldReadback = false;

		/// <summary>
		/// The downsampling factor applied to the distance field's resolution.
		/// </summary>
		/// <remarks>
		/// Increasing this value improves performance by reducing the GPU generation and CPU transfer time, but it decreases the spatial accuracy of the distance field. A value of 0 means no downsampling.
		/// </remarks>
		public int distanceFieldDownsample = 0;

		/// <summary>
		/// The number of internal steps the Jump Flood algorithm performs to generate the distance field.
		/// </summary>
		/// <remarks>
		/// Lowering this number increases performance but reduces the accuracy, particularly for larger distances within the field. Higher resolution distance fields generally require more iterations for full accuracy.
		/// </remarks>
		public int distanceFieldIterations = 12;

		/// <summary>
		/// The number of frames over which the distance field's CPU readback is sliced to spread the performance cost.
		/// </summary>
		/// <remarks>
		/// Similar to <see cref="readBackTimeSliceFrames"/>, this value balances transfer cost per frame against the overall latency of the data becoming available on the CPU.
		/// </remarks>
		public int distanceFieldTimeSliceFrames = 4;

		/// <summary>
		/// The rate of constant (linear) fluid volume removal from every cell in the simulation.
		/// </summary>
		/// <remarks>
		/// This simulates a constant, external water loss like pumping. The fluid volume is reduced uniformly at this rate: `fluid -= linearEvaporation * dt`.
		/// </remarks>
		public float linearEvaporation = 0;
		/// <summary>
		/// The rate of fluid volume removal proportional to the amount of fluid currently in the cell.
		/// </summary>
		/// <remarks>
		/// This simulates natural evaporation, where the rate is dependent on surface area/volume. More fluid results in a higher removal rate: `fluid -= fluid * proportionalEvaporation * dt`.
		/// </remarks>
		public float proportionalEvaporation = 0;

		/// <summary>
		/// Enables an optional secondary layer for simulating a different type of fluid.
		/// </summary>
		/// <remarks>
		/// The secondary layer runs concurrently with the main fluid layer, increasing VRAM usage and slightly decreasing performance. However, this is generally more efficient than running a separate <c>FluidSimulation</c> component. The second layer is used for features like **Lava** in the Terraform simulation option. The following properties provide independent physics overrides for this layer.
		/// </remarks>
		public bool secondLayer = false;

		/// <summary>
		/// Secondary Layer: Adjusts the internal scale factor of the fluid volume to control the effective flow speed.
		/// </summary>
		/// <remarks>
		/// <inheritdoc cref="cellSize" path="/remarks"/>
		/// </remarks>
		public float secondLayerCellSize = 0.5f;
		/// <summary>
		/// Secondary Layer: Adjusts the rate at which wave energy is dissipated over time.
		/// </summary>
		/// <remarks>
		/// <inheritdoc cref="waveDamping" path="/remarks"/>
		/// </remarks>
		public float secondLayerWaveDamping = 0.25f;
		/// <summary>
		/// Secondary Layer: The force of acceleration applied to the fluid, which directly controls the speed of wave propagation.
		/// </summary>
		/// <remarks>
		/// <inheritdoc cref="acceleration" path="/remarks"/>
		/// </remarks>
		public float secondLayerAcceleration = 9.8f;

		/// <summary>
		/// Secondary Layer: The rate of constant (linear) fluid volume removal.
		/// </summary>
		/// <remarks>
		/// Fluid volume is reduced uniformly at this rate: `fluid -= secondLayerLinearEvaporation * dt`.
		/// </remarks>
		public float secondLayerLinearEvaporation = 0;
		/// <summary>
		/// Secondary Layer: The rate of fluid volume removal proportional to the amount of fluid currently in the cell.
		/// </summary>
		/// <remarks>
		/// More fluid results in a higher removal rate: `fluid -= fluid * secondLayerProportionalEvaporation * dt`.
		/// </remarks>
		public float secondLayerProportionalEvaporation = 0;
	}
}