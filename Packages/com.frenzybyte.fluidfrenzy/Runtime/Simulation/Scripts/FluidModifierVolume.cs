using System;
using UnityEngine;
using UnityEngine.Serialization;
using FluidModifierBlendMode = FluidFrenzy.FluidSimulation.FluidModifierBlendMode;
using FluidModifierSpace = FluidFrenzy.FluidSimulation.FluidModifierSpace;

namespace FluidFrenzy
{
	/// <summary>
	/// <see cref="FluidModifierVolume"/> is a FluidModifier that interacts with any <see cref="FluidSimulation"/>.
	/// There are several modes that can be used to interact with fluid simulations by setting the <see cref="FluidModifierType"/> enum value <see cref="type"/>.
	/// </summary>
	[HelpURL("https://frenzybyte.github.io/fluidfrenzy/docs/fluid_modifiers/#fluid-modifier-volume")]
	public class FluidModifierVolume : FluidModifier
	{
		[Flags]
		public enum FluidModifierType
		{
			/// <summary>
			/// When enabled, the modifier acts as a fluid source, directly adding or removing fluid from the simulation's height field.
			/// </summary>
			Source = 1 << 0,
			/// <summary>
			/// When enabled, the modifier interacts with the simulation by creating flows or currents in the velocity field.
			/// </summary>
			Flow = 1 << 1,
			/// <summary>
			/// When enabled, the modifier adds force or energy to the fluid simulation, useful for creating waves.
			/// </summary>
			/// <remarks>
			/// **Warning**: This mode may have no effect or the same effect as the Flow mode on certain simulation types (e.g., <c>FlowFluidSimulation</c>).
			/// </remarks>
			Force = 1 << 2,
		}

		/// <summary>
		/// Defines the settings when <see cref="FluidModifierType.Source"/> is enabled on the <see cref="FluidModifierVolume"/>.
		/// </summary>
		[Serializable]
		public class FluidSourceSettings
		{
			public enum FluidSourceMode
			{
				/// <summary>
				/// The modifier inputs fluid in a circular shape.
				/// </summary>
				Circle,
				/// <summary>
				/// The modifier inputs fluid in a rectangular shape.
				/// </summary>
				Box,
				/// <summary>
				/// The modifier uses a source texture to define the shape and intensity of the fluid input.
				/// </summary>
				Texture
			}
			/// <summary>
			/// Sets the input mode of the modifier, defining the shape or source of the fluid input.
			/// </summary>
			/// <remarks>
			/// Fluid input modes include:
			/// <list type="bullet">
			/// 	<item>
			/// 		<term><see cref="FluidSourceMode.Circle"/></term>
			/// 		<description>Fluid input in a circular shape.</description>
			/// 	</item>
			/// 	<item>
			/// 		<term><see cref="FluidSourceMode.Box"/></term>
			/// 		<description>Fluid input in a rectangular shape.</description>
			/// 	</item>
			/// 	<item>
			/// 		<term><see cref="FluidSourceMode.Texture"/></term>
			/// 		<description>Fluid input defined by a source texture.</description>
			/// 	</item>
			/// </list>
			/// </remarks>
			public FluidSourceMode mode = FluidSourceMode.Circle;

			/// <summary>
			/// Enables or disables movement for this modifier.
			/// </summary>
			/// <remarks>
			/// When disabled, the modifier is treated as static and its contribution is calculated once at the start of the simulation. 
			/// This improves performance for multiple stationary fluid sources.
			/// </remarks>
			public bool dynamic = true;

			[SerializeField]
			public bool additive = true; 

			/// <summary>
			/// Defines the blending operation used to apply the fluid source to the simulation's height field.
			/// </summary>
			/// <remarks>
			/// This determines how the fluid is applied to the simulation's current height. Options include:<br/>
			/// - <see cref="FluidModifierBlendMode.Additive"/>: Adds or subtracts the fluid amount.<br/>
			/// - <see cref="FluidModifierBlendMode.Set"/>: Sets the height to a specific value.<br/>
			/// - <see cref="FluidModifierBlendMode.Minimum"/>/<see cref="FluidModifierBlendMode.Maximum"/>: Clamps the height to the target value.
			/// </remarks>
			public FluidModifierBlendMode blendMode = FluidModifierBlendMode.Additive;

			/// <summary>
			/// Specifies the coordinate space to which the fluid height source should be set relative to.
			/// </summary>
			/// <remarks>
			/// <list type="bullet">
			/// 	<item>
			/// 		<term><see cref="FluidSimulation.FluidModifierSpace.WorldHeight"/></term>
			/// 		<description>The height is interpreted as a specific world Y-coordinate.</description>
			/// 	</item>
			/// 	<item>
			/// 		<term><see cref="FluidSimulation.FluidModifierSpace.LocalHeight"/></term>
			/// 		<description>The height is interpreted relative to the fluid surface's base height.</description>
			/// 	</item>
			/// </list>
			/// </remarks>
			public FluidModifierSpace space = FluidModifierSpace.WorldHeight;

			/// <summary>
			/// Adjusts the amount of fluid added or set by the volume.
			/// </summary>
			/// <remarks>
			/// <list type="bullet">
			/// 	<item>
			/// 		<term>For Additive blending</term>
			/// 		<description>This is the rate per second of fluid to add/subtract.</description>
			/// 	</item>
			/// 	<item>
			/// 		<term>For Set blending</term>
			/// 		<description>This value contributes to the target height.</description>
			/// 	</item>
			/// </list>
			/// </remarks>
			public float strength = 5;

			/// <summary>
			/// Adjusts the curve of the distance-based strength, controlling how quickly the influence falls off from the center.
			/// </summary>
			/// <remarks>
			/// Higher values create a faster falloff, resulting in a more focused fluid source.
			/// </remarks>
			[FormerlySerializedAs("exponent")]
			[Range(0.001f, 5)]
			public float falloff = 1;

			/// <summary>
			/// Specifies the target fluid layer to which the fluid will be added.
			/// </summary>
			public int layer = 0;

			/// <summary>
			/// Adjust the size (width and height) of the modification area in world units.
			/// </summary>
			public Vector2 size = new Vector2(10, 10);

			/// <summary>
			/// The source texture used to determine the shape and intensity of the fluid input when <see cref="mode"/> is <see cref="FluidSourceMode.Texture"/>. 
			/// Only the red channel of the texture is used.
			/// </summary>
			public Texture2D texture = null;
		}

		/// <summary>
		/// Defines the settings when <see cref="FluidModifierType.Flow"/> is enabled on the <see cref="FluidModifierVolume"/>.
		/// </summary>
		[Serializable]
		public class FluidFlowSettings
		{
			public enum FluidFlowMode
			{
				/// <summary>
				/// The modifier inputs flow in a constant direction within a circular shape.
				/// </summary>
				Circle,
				/// <summary>
				/// The modifier inputs the flow of a vortex. The flow is circular with control over radial (inward) and tangential flow.
				/// </summary>
				Vortex,
				/// <summary>
				/// The modifier inputs the flow direction from a dedicated flow map texture.
				/// </summary>
				Texture
			}

			/// <summary>
			/// Sets the input mode of the modifier, defining how flow is applied.
			/// </summary>
			/// <remarks>
			/// Flow application modes include:
			/// <list type="bullet">
			/// 	<item>
			/// 		<term><see cref="FluidFlowMode.Circle"/></term>
			/// 		<description>A constant directional flow within a circular shape.</description>
			/// 	</item>
			/// 	<item>
			/// 		<term><see cref="FluidFlowMode.Vortex"/></term>
			/// 		<description>A circular flow with radial and tangential control.</description>
			/// 	</item>
			/// 	<item>
			/// 		<term><see cref="FluidFlowMode.Texture"/></term>
			/// 		<description>Flow direction supplied from a dedicated flow map texture.</description>
			/// 	</item>
			/// </list>
			/// </remarks>
			public FluidFlowMode mode = FluidFlowMode.Circle;

			/// <summary>
			/// Sets the 2D direction in which the flow force will be applied for <see cref="FluidFlowMode.Circle"/>. 
			/// </summary>
			/// <remarks>
			/// The <c>x</c> component maps to world X, and the <c>y</c> component maps to world Z (assuming a flat surface).
			/// </remarks>
			public Vector2 direction = Vector2.right;

			/// <summary>
			/// The blending operation used to apply the generated velocity to the simulation's velocity field.
			/// </summary>
			/// <remarks>
			/// This determines how the velocity is applied to the simulation's current flow. Options include:
			/// <list type="bullet">
			/// 	<item>
			/// 		<term><see cref="FluidModifierBlendMode.Additive"/></term>
			/// 		<description>Adds or subtracts the flow/velocity amount.</description>
			/// 	</item>
			/// 	<item>
			/// 		<term><see cref="FluidModifierBlendMode.Set"/></term>
			/// 		<description>Sets the flow/velocity to a specific vector.</description>
			/// 	</item>
			/// 	<item>
			/// 		<term><see cref="FluidModifierBlendMode.Minimum"/>/<see cref="FluidModifierBlendMode.Maximum"/></term>
			/// 		<description>Clamps the velocity vector components to the target values.</description>
			/// 	</item>
			/// </list>
			/// </remarks>
			public FluidModifierBlendMode blendMode = FluidModifierBlendMode.Additive;

			/// <summary>
			/// Adjusts the magnitude of the flow applied to the velocity field. 
			/// </summary>
			/// <remarks>
			/// For <see cref="FluidFlowMode.Vortex"/> mode, this specifically controls the *inward* flow to the center.
			/// </remarks>
			public float strength = 5;

			/// <summary>
			/// Adjusts the amount of *tangential* flow applied for <see cref="FluidFlowMode.Vortex"/> mode. 
			/// Higher values create a faster spinning vortex.
			/// </summary>
			public float radialFlowStrength = 10;

			/// <summary>
			/// Adjusts the curve of the distance-based strength, controlling how quickly the influence falls off from the center.
			/// </summary>
			/// <remarks>
			/// Higher values create a sharper shape with faster falloff.
			/// </remarks>
			[FormerlySerializedAs("exponent")]
			public float falloff = 5;

			/// <summary>
			/// Adjust the size (width and height) of the modification area in world units.
			/// </summary>
			public Vector2 size = new Vector2(10, 10);

			/// <summary>
			/// The flow map texture used as input when <see cref="mode"/> is <see cref="FluidFlowMode.Texture"/>. 
			/// </summary>
			/// <remarks>
			/// The texture's Red and Green channels map to the X and Y velocity components. The texture is unpacked from the [0, 1] range to the [-1, 1] velocity range.
			/// </remarks>
			public Texture2D texture = null;
		}

		/// <summary>
		/// Defines the settings when <see cref="FluidModifierType.Force"/> is enabled on the <see cref="FluidModifierVolume"/>.
		/// </summary>
		[Serializable]
		public class FluidForceSettings
		{
			public enum FluidForceMode
			{
				/// <summary>
				/// Creates a directional force within a circular shape, useful for pushing fluids and creating simple waves.
				/// </summary>
				Circle,
				/// <summary>
				/// Creates a downward, distance-based force, simulating a vortex or whirlpool effect.
				/// </summary>
				Vortex,
				/// <summary>
				/// Creates an immediate outward force from the center, designed to simulate a quick splash effect on the fluid surface.
				/// </summary>
				Splash,
				/// <summary>
				/// Creates forces from a texture input, where the red channel is used for height displacement (force).
				/// </summary>
				Texture
			}

			/// <summary>
			/// Sets the input mode of the modifier, defining the type of force applied.
			/// </summary>
			/// <remarks>
			/// Force application modes include:
			/// <list type="bullet">
			/// 	<item>
			/// 		<term><see cref="FluidForceMode.Circle"/></term>
			/// 		<description>A directional force within a circular shape (for waves/pushes).</description>
			/// 	</item>
			/// 	<item>
			/// 		<term><see cref="FluidForceMode.Vortex"/></term>
			/// 		<description>A downward, distance-based force (for whirlpools).</description>
			/// 	</item>
			/// 	<item>
			/// 		<term><see cref="FluidForceMode.Splash"/></term>
			/// 		<description>An immediate outward force (for splash effects).</description>
			/// 	</item>
			/// 	<item>
			/// 		<term><see cref="FluidForceMode.Texture"/></term>
			/// 		<description>Forces created from a texture input.</description>
			/// 	</item>
			/// </list>
			/// </remarks>
			public FluidForceMode mode = FluidForceMode.Circle;

			/// <summary>
			/// The blending operation used to apply or dampen the force in the simulation.
			/// </summary>
			/// <remarks>
			/// This determines how the force is applied to the simulation. Options include:
			/// <list type="bullet">
			/// 	<item>
			/// 		<term><see cref="FluidModifierBlendMode.Additive"/></term>
			/// 		<description>Adds or subtracts the force amount.</description>
			/// 	</item>
			/// 	<item>
			/// 		<term><see cref="FluidModifierBlendMode.Set"/></term>
			/// 		<description>Sets the force to a specific vector.</description>
			/// 	</item>
			/// 	<item>
			/// 		<term><see cref="FluidModifierBlendMode.Minimum"/>/<see cref="FluidModifierBlendMode.Maximum"/></term>
			/// 		<description>Clamps the force vector components to the target values.</description>
			/// 	</item>
			/// </list>
			/// </remarks>
			public FluidModifierBlendMode blendMode = FluidModifierBlendMode.Additive;

			/// <summary>
			/// Sets the 2D direction of the applied force/wave propagation.
			/// </summary>
			/// <remarks>
			/// The <c>x</c> component maps to world X, and the <c>y</c> component maps to world Z (assuming a flat surface).
			/// </remarks>
			public Vector2 direction = Vector2.right;

			/// <summary>
			/// Controls the magnitude of the force applied.
			/// </summary>
			/// <remarks>
			/// This represents the height of the wave/splash, the depth of the vortex, or the strength to apply the supplied texture.
			/// </remarks>
			public float strength = 5;

			/// <summary>
			/// Adjusts the curve of the distance-based strength, controlling how quickly the influence falls off from the center.
			/// </summary>
			/// <remarks>
			/// Higher values create a sharper shape with faster falloff.
			/// </remarks>
			[FormerlySerializedAs("exponent")]
			[Range(0.001f, 5)]
			public float falloff = 1;

			/// <summary>
			/// Adjust the size (width and height) of the modification area in world units.
			/// </summary>
			public Vector2 size = new Vector2(10, 10);

			/// <summary>
			/// The source texture used as an input when <see cref="mode"/> is <see cref="FluidForceMode.Texture"/>. 
			/// Only the red channel is used for height/force displacement.
			/// </summary>
			public Texture2D texture = null;
		}

		/// <summary>
		/// Specifies the type of fluid modification enabled on this volume, which can be a combination of Source, Flow, and Force.
		/// </summary>
		public FluidModifierType type = FluidModifierType.Source;

		/// <summary>
		/// Contains all configuration settings for the fluid source behavior when <see cref="type"/> includes <see cref="FluidModifierType.Source"/>.
		/// </summary>
		public FluidSourceSettings sourceSettings = new FluidSourceSettings();

		/// <summary>
		/// Contains all configuration settings for the fluid flow behavior when <see cref="type"/> includes <see cref="FluidModifierType.Flow"/>.
		/// </summary>
		public FluidFlowSettings flowSettings = new FluidFlowSettings();

		/// <summary>
		/// Contains all configuration settings for the fluid force/wave behavior when <see cref="type"/> includes <see cref="FluidModifierType.Force"/>.
		/// </summary>
		public FluidForceSettings forceSettings = new FluidForceSettings();

		void OnValidate()
		{
			if(version == 1)
			{
				if (sourceSettings.additive)
					sourceSettings.blendMode = FluidModifierBlendMode.Additive;
				else
					sourceSettings.blendMode = FluidModifierBlendMode.Set;
				version = 2;
			}
		}

		public override void Process(FluidSimulation fluidSim, float dt)
		{
			if ((type & FluidModifierType.Source) != 0)
				ProcessFluidSource(fluidSim, dt);
			if ((type & FluidModifierType.Flow) != 0)
				ProcessFlow(fluidSim, dt);
			if ((type & FluidModifierType.Force) != 0)
				ProcessForce(fluidSim, dt);
		}

		public override void PostProcess(FluidSimulation fluidSim, float dt)
		{
			if ((type & FluidModifierType.Source) != 0 && sourceSettings.blendMode != FluidModifierBlendMode.Additive)
				ProcessFluidSource(fluidSim, dt);
		}

		public void ProcessFluidSource(FluidSimulation fluidSim, float dt)
		{
			if (!sourceSettings.dynamic) return;

			switch (sourceSettings.mode)
			{
				case FluidSourceSettings.FluidSourceMode.Circle:
					if (sourceSettings.blendMode == FluidModifierBlendMode.Additive)
					{
						fluidSim.AddFluidCircle(transform.position, sourceSettings.size, sourceSettings.strength, sourceSettings.falloff, sourceSettings.layer, dt);
					}
					else if (sourceSettings.blendMode == FluidModifierBlendMode.Set)
					{
						if (sourceSettings.space == FluidModifierSpace.WorldHeight)
						{
							fluidSim.SetFluidHeightCircle(transform.position, sourceSettings.size, transform.position.y, sourceSettings.falloff, sourceSettings.layer, sourceSettings.space);
						}
						else if (sourceSettings.space == FluidModifierSpace.LocalHeight)
						{
							fluidSim.SetFluidDepthCircle(transform.position, sourceSettings.size, sourceSettings.strength, sourceSettings.falloff, sourceSettings.layer, sourceSettings.space);
						}
					}
					else if (sourceSettings.blendMode == FluidModifierBlendMode.Minimum)
					{
						if (sourceSettings.space == FluidModifierSpace.WorldHeight)
						{
							fluidSim.SetMinFluidHeightCircle(transform.position, sourceSettings.size, transform.position.y, sourceSettings.falloff, sourceSettings.layer, sourceSettings.space);
						}
						else if (sourceSettings.space == FluidModifierSpace.LocalHeight)
						{
							fluidSim.SetMinFluidDepthCircle(transform.position, sourceSettings.size, sourceSettings.strength, sourceSettings.falloff, sourceSettings.layer, sourceSettings.space);
						}
					}					
					else if (sourceSettings.blendMode == FluidModifierBlendMode.Maximum)
					{
						if (sourceSettings.space == FluidModifierSpace.WorldHeight)
						{
							fluidSim.SetMaxFluidHeightCircle(transform.position, sourceSettings.size, transform.position.y, sourceSettings.falloff, sourceSettings.layer, sourceSettings.space);
						}
						else if (sourceSettings.space == FluidModifierSpace.LocalHeight)
						{
							fluidSim.SetMaxFluidDepthCircle(transform.position, sourceSettings.size, sourceSettings.strength, sourceSettings.falloff, sourceSettings.layer, sourceSettings.space);
						}
					}
					break;
				case FluidSourceSettings.FluidSourceMode.Box:
					if (sourceSettings.blendMode == FluidModifierBlendMode.Additive)
					{
						fluidSim.AddFluidSquare(transform.position, sourceSettings.size, sourceSettings.strength, sourceSettings.falloff, sourceSettings.layer, dt);
					}
					else if (sourceSettings.blendMode == FluidModifierBlendMode.Set)
					{
						if (sourceSettings.space == FluidModifierSpace.WorldHeight)
						{
							fluidSim.SetFluidHeightSquare(transform.position, sourceSettings.size, transform.position.y, sourceSettings.falloff, sourceSettings.layer, sourceSettings.space);
						}
						else if (sourceSettings.space == FluidModifierSpace.LocalHeight)
						{
							fluidSim.SetFluidDepthSquare(transform.position, sourceSettings.size, sourceSettings.strength, sourceSettings.falloff, sourceSettings.layer, sourceSettings.space);
						}
					}
					else if (sourceSettings.blendMode == FluidModifierBlendMode.Minimum)
					{
						if (sourceSettings.space == FluidModifierSpace.WorldHeight)
						{
							fluidSim.SetMinFluidHeightSquare(transform.position, sourceSettings.size, transform.position.y, sourceSettings.falloff, sourceSettings.layer, sourceSettings.space);
						}
						else if (sourceSettings.space == FluidModifierSpace.LocalHeight)
						{
							fluidSim.SetMinFluidDepthSquare(transform.position, sourceSettings.size, sourceSettings.strength, sourceSettings.falloff, sourceSettings.layer, sourceSettings.space);
						}
					}
					else if (sourceSettings.blendMode == FluidModifierBlendMode.Maximum)
					{
						if (sourceSettings.space == FluidModifierSpace.WorldHeight)
						{
							fluidSim.SetMaxFluidHeightSquare(transform.position, sourceSettings.size, transform.position.y, sourceSettings.falloff, sourceSettings.layer, sourceSettings.space);
						}
						else if (sourceSettings.space == FluidModifierSpace.LocalHeight)
						{
							fluidSim.SetMaxFluidDepthSquare(transform.position, sourceSettings.size, sourceSettings.strength, sourceSettings.falloff, sourceSettings.layer, sourceSettings.space);
						}
					}
					break;
				case FluidSourceSettings.FluidSourceMode.Texture:
					
					if (sourceSettings.blendMode == FluidModifierBlendMode.Additive)
					{
						fluidSim.AddFluid(transform.position, sourceSettings.size, sourceSettings.texture, sourceSettings.strength, sourceSettings.layer, dt);
					}
					else if (sourceSettings.blendMode == FluidModifierBlendMode.Set)
					{
						if (sourceSettings.space == FluidModifierSpace.WorldHeight)
						{
							fluidSim.SetFluidHeightTexture(transform.position, sourceSettings.size, sourceSettings.texture, transform.position.y, sourceSettings.layer, sourceSettings.space);
						}
						else if (sourceSettings.space == FluidModifierSpace.LocalHeight)
						{
							fluidSim.SetFluidDepthTexture(transform.position, sourceSettings.size, sourceSettings.texture, sourceSettings.strength, sourceSettings.layer, sourceSettings.space);
						}
					}
					else if (sourceSettings.blendMode == FluidModifierBlendMode.Minimum)
					{
						if (sourceSettings.space == FluidModifierSpace.WorldHeight)
						{
							fluidSim.SetMinFluidHeightTexture(transform.position, sourceSettings.size, sourceSettings.texture, transform.position.y, sourceSettings.layer, sourceSettings.space);
						}
						else if (sourceSettings.space == FluidModifierSpace.LocalHeight)
						{
							fluidSim.SetMinFluidDepthTexture(transform.position, sourceSettings.size, sourceSettings.texture, sourceSettings.strength, sourceSettings.layer, sourceSettings.space);
						}
					}
					else if (sourceSettings.blendMode == FluidModifierBlendMode.Maximum)
					{
						if (sourceSettings.space == FluidModifierSpace.WorldHeight)
						{
							fluidSim.SetMaxFluidHeightTexture(transform.position, sourceSettings.size, sourceSettings.texture, transform.position.y, sourceSettings.layer, sourceSettings.space);
						}
						else if (sourceSettings.space == FluidModifierSpace.LocalHeight)
						{
							fluidSim.SetMaxFluidDepthTexture(transform.position, sourceSettings.size, sourceSettings.texture, sourceSettings.strength, sourceSettings.layer, sourceSettings.space);
						}
					}
					break;
			}
		}

		public void ProcessFlow(FluidSimulation fluidSim, float dt)
		{
			switch (flowSettings.mode)
			{
				case FluidFlowSettings.FluidFlowMode.Circle:
					if (flowSettings.blendMode == FluidModifierBlendMode.Additive)
					{
						fluidSim.ApplyFlow(transform.position, flowSettings.direction, flowSettings.size, flowSettings.strength, flowSettings.falloff, dt);
					}
					else if (flowSettings.blendMode == FluidModifierBlendMode.Set)
					{
						fluidSim.SetFlow(transform.position, flowSettings.direction, flowSettings.size, flowSettings.strength, flowSettings.falloff);
					}
					else if (flowSettings.blendMode == FluidModifierBlendMode.Dampen)
					{
						fluidSim.DampenVelocityCircle(transform.position, flowSettings.size, flowSettings.strength, flowSettings.falloff, dt);
					}
					break;
				case FluidFlowSettings.FluidFlowMode.Vortex:
					if (flowSettings.blendMode == FluidModifierBlendMode.Additive)
					{
						fluidSim.ApplyFlowVortex(transform.position, flowSettings.size, flowSettings.radialFlowStrength, flowSettings.strength, dt);
					}
					else if (flowSettings.blendMode == FluidModifierBlendMode.Set)
					{
						fluidSim.SetFlowVortex(transform.position, flowSettings.size, flowSettings.radialFlowStrength, flowSettings.strength);
					}
					else if (flowSettings.blendMode == FluidModifierBlendMode.Dampen)
					{
						fluidSim.DampenVelocityCircle(transform.position, flowSettings.size, flowSettings.strength, flowSettings.falloff, dt);
					}
					break;				
				case FluidFlowSettings.FluidFlowMode.Texture:
					if (flowSettings.blendMode == FluidModifierBlendMode.Additive)
					{
						fluidSim.ApplyVelocity(transform.position, flowSettings.size, flowSettings.texture, flowSettings.strength, dt);
					}
					else if (flowSettings.blendMode == FluidModifierBlendMode.Set)
					{
						fluidSim.SetVelocity(transform.position, flowSettings.size, flowSettings.texture, flowSettings.strength);
					}
					else if (flowSettings.blendMode == FluidModifierBlendMode.Dampen)
					{
						fluidSim.DampenVelocity(transform.position, flowSettings.size, flowSettings.texture, flowSettings.strength, dt);
					}
					break;
			}
		}

		public void ProcessForce(FluidSimulation fluidSim, float dt)
		{
			switch (forceSettings.mode)
			{
				case FluidForceSettings.FluidForceMode.Circle:
					if (forceSettings.blendMode == FluidModifierBlendMode.Additive)
					{
						fluidSim.ApplyForce(transform.position, forceSettings.direction, forceSettings.size, forceSettings.strength, forceSettings.falloff, dt, false);
					}
					else if (forceSettings.blendMode == FluidModifierBlendMode.Dampen)
					{
						fluidSim.DampenForce(transform.position, forceSettings.size, forceSettings.strength, forceSettings.falloff, dt);
					}
					break;
				case FluidForceSettings.FluidForceMode.Texture:
					if (forceSettings.blendMode == FluidModifierBlendMode.Additive)
					{
						fluidSim.ApplyForce(forceSettings.texture, forceSettings.strength, dt);
					}
					else if (forceSettings.blendMode == FluidModifierBlendMode.Dampen)
					{
						fluidSim.DampenForce(transform.position, forceSettings.size, forceSettings.texture, forceSettings.strength, dt);
					}
					break;
				case FluidForceSettings.FluidForceMode.Vortex:
					if (forceSettings.blendMode == FluidModifierBlendMode.Additive)
					{
						fluidSim.ApplyForceVortex(transform.position, forceSettings.size, forceSettings.strength, forceSettings.falloff, dt);
					}
					else if (forceSettings.blendMode == FluidModifierBlendMode.Dampen)
					{
						fluidSim.DampenForce(transform.position, forceSettings.size, forceSettings.strength, forceSettings.falloff, dt);
					}
					break;
				case FluidForceSettings.FluidForceMode.Splash:
					if (forceSettings.blendMode == FluidModifierBlendMode.Additive)
					{
						fluidSim.ApplyForce(transform.position, Vector2.zero, forceSettings.size, forceSettings.strength, forceSettings.falloff, dt, true);
					}
					else if (forceSettings.blendMode == FluidModifierBlendMode.Dampen)
					{
						fluidSim.DampenForce(transform.position, forceSettings.size, forceSettings.strength, forceSettings.falloff, dt);
					}
					break;
			}
		}

		public Vector3 GetSize()
		{
			Vector2 size = new Vector2(0, 0);
			if ((type & FluidModifierType.Source) != 0)
				size = Vector3.Max(size, sourceSettings.size);
			if ((type & FluidModifierType.Flow) != 0)
				size = Vector3.Max(size, Vector2.one * flowSettings.size);
			if ((type & FluidModifierType.Force) != 0)
				size = Vector3.Max(size, Vector2.one * forceSettings.size);

			return new Vector3(size.x, 10, size.y);
		}

		public float GetAbsMaxStrength()
		{
			float strength = 0;
			if ((type & FluidModifierType.Source) != 0)
				strength = Mathf.Max(strength, Mathf.Abs(sourceSettings.strength));
			if ((type & FluidModifierType.Flow) != 0)
				strength = Mathf.Max(strength, Mathf.Abs(flowSettings.strength));
			if ((type & FluidModifierType.Force) != 0)
				strength = Mathf.Max(strength, Mathf.Abs(forceSettings.strength));

			return strength;
		}

		public void Scale(Vector2 scale)
		{
			sourceSettings.size.Scale(scale);
			flowSettings.size.Scale(scale);
			forceSettings.size.Scale(scale);
		}

		private Vector3 SizeToGizmoSize(Vector2 size)
		{
			return new Vector3(size.x, 10, size.y);
		}

		public void OnDrawGizmosSelected()
		{
			if ((type & FluidModifierType.Source) != 0)
			{
				Gizmos.color = Color.blue;
				Gizmos.DrawWireCube(transform.position, SizeToGizmoSize(sourceSettings.size));
			}

			if ((type & FluidModifierType.Flow) != 0)
			{
				Gizmos.color = Color.red;
				Gizmos.DrawWireCube(transform.position, SizeToGizmoSize(flowSettings.size));
			}

			if ((type & FluidModifierType.Force) != 0)
			{
				Gizmos.color = Color.green;
				Gizmos.DrawWireCube(transform.position, SizeToGizmoSize(forceSettings.size));
			}
		}
	}
}