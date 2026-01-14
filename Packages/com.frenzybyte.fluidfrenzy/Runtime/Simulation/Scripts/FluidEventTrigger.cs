using UnityEngine;
using UnityEngine.Events;

namespace FluidFrenzy
{
	/// <summary>
	/// A component that detects interactions with the fluid simulation at a specific world position.
	/// </summary>
	/// <remarks>
	/// This component continually samples the simulation data at its transform position. 
	/// It determines if the object is submerged and reports the properties of the fluid at that location. 
	/// If multiple fluid layers overlap at this point, the trigger reports data for the most dominant (highest) fluid layer.
	/// </remarks>
	[HelpURL("https://frenzybyte.github.io/fluidfrenzy/docs/fluid_simulation_components/#fluid-simulation-obstacle")]
	public class FluidEventTrigger : MonoBehaviour
	{
		/// <summary>
		/// Invoked the moment the object transitions from being outside fluid to being submerged.
		/// </summary>
		public UnityEvent<FluidEventTrigger> onFluidEnter = new UnityEvent<FluidEventTrigger>();

		/// <summary>
		/// Invoked the moment the object transitions from being submerged to being outside fluid.
		/// </summary>
		public UnityEvent<FluidEventTrigger> onFluidExit = new UnityEvent<FluidEventTrigger>();

		/// <summary>
		/// The index of the dominant fluid layer currently occupying the trigger's position.
		/// </summary>
		/// <remarks>
		/// If the trigger is not in fluid, this value may default to 0 or the last known valid layer.
		/// </remarks>
		public int fluidLayer { get; private set; } = 0;

		/// <summary>
		/// The world-space Y-coordinate of the fluid surface at the trigger's location.
		/// </summary>
		public float fluidHeight { get; private set; } = 0;

		/// <summary>
		/// The vertical distance from the fluid surface to the ground (terrain) at the trigger's location.
		/// </summary>
		/// <remarks>
		/// This represents the total depth of the water column at this specific coordinate, not the object's submersion depth.
		/// </remarks>
		public float fluidDepth { get; private set; } = 0;

		/// <summary>
		/// Indicates whether the trigger is currently intersecting with any fluid volume.
		/// </summary>
		public bool isInFluid { get; private set; } = false;

		// Update is called once per frame
		void Update()
		{
			bool wasInFluid = isInFluid;
			if (FluidSimulationManager.GetHeightLayer(transform.position, out Vector2 sampledHeight, out int sampledLayer))
			{
				if (transform.position.y < sampledHeight.x && sampledHeight.y > 0)
				{
					isInFluid = true;
					fluidHeight = sampledHeight.x;
					fluidDepth = sampledHeight.y;
					fluidLayer = sampledLayer;
					if (!wasInFluid)
						onFluidEnter.Invoke(this);
				}
				else
				{
					isInFluid = false;
					if (wasInFluid)
						onFluidExit.Invoke(this);
					fluidDepth = 0;
					fluidLayer = 0;
					fluidHeight = 0;
				}
			}
			else
			{
				isInFluid = false;
				fluidDepth = 0;
				fluidLayer = 0;
				fluidHeight = 0; 
				if (wasInFluid)
					onFluidExit.Invoke(this);
			}
		}
	}
}