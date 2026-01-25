using System;
using UnityEngine;

namespace FluidFrenzy
{
	/// <summary>
	/// Describes the configuration parameters for a particle emitter, defining 
	/// the minimum and maximum bounds used to randomize particle initialization.
	/// </summary>
	[Serializable]
	public struct ParticleEmitterDesc
	{
		public static ParticleEmitterDesc Default = new ParticleEmitterDesc(
			minColor: Color.white,
			maxColor: Color.white,
			minVelocity: Vector4.zero,
			maxVelocity: Vector4.zero,
			minAcceleration: Vector4.zero,
			maxAcceleration: Vector4.zero,
			minOffset: Vector4.zero,
			maxOffset: Vector4.zero,
			minAngularVelocity: 0,
			maxAngularVelocity: 0,
			minSize: 5,
			maxSize: 5,
			minLife: 1,
			maxLife: 2
		);

		public ParticleEmitterDesc(
			Color minColor, Color maxColor,
			Vector4 minVelocity, Vector4 maxVelocity,
			Vector4 minAcceleration, Vector4 maxAcceleration,
			Vector4 minOffset, Vector4 maxOffset,
			float minAngularVelocity, float maxAngularVelocity,
			float minSize, float maxSize,
			float minLife, float maxLife
		)
		{
			this.minColor = minColor;
			this.maxColor = maxColor;
			this.minVelocity = minVelocity;
			this.maxVelocity = maxVelocity;
			this.minAcceleration = minAcceleration;
			this.maxAcceleration = maxAcceleration;
			this.minOffset = minOffset;
			this.maxOffset = maxOffset;
			this.minAngularVelocity = minAngularVelocity;
			this.maxAngularVelocity = maxAngularVelocity;
			this.minSize = minSize;
			this.maxSize = maxSize;
			this.minLife = minLife;
			this.maxLife = maxLife;
		}

		/// <summary>
		/// The minimum color bound for the particle's random selection.
		/// </summary>
		public Color minColor;

		/// <summary>
		/// The maximum color bound for the particle's random selection.
		/// </summary>
		public Color maxColor;

		/// <summary>
		/// The minimum velocity bound for the particle's random selection.
		/// </summary>			
		public Vector4 minVelocity;

		/// <summary>
		/// The maximum velocity bound for the particle's random selection.
		/// </summary>			
		public Vector4 maxVelocity;

		/// <summary>
		/// The minimum acceleration bound for the particle's random selection.
		/// </summary>
		public Vector4 minAcceleration;

		/// <summary>
		/// The maximum acceleration bound for the particle's random selection.
		/// </summary>
		public Vector4 maxAcceleration;

		/// <summary>
		/// The minimum position offset bound for the particle's random selection.
		/// </summary>
		public Vector4 minOffset;

		/// <summary>
		/// The maximum position offset bound for the particle's random selection.
		/// </summary>
		public Vector4 maxOffset;

		/// <summary>
		/// The minimum angular velocity of the particles.
		/// </summary>
		public float minAngularVelocity;

		/// <summary>
		/// The maximum angular velocity of the particles.
		/// </summary>
		public float maxAngularVelocity;

		/// <summary>
		/// The minimum size (scale) bound for the particle's random selection.
		/// </summary>
		public float minSize;

		/// <summary>
		/// The maximum size (scale) bound for the particle's random selection.
		/// </summary>
		public float maxSize;

		/// <summary>
		/// The minimum lifetime bound for the particle's random selection.
		/// </summary>
		public float minLife;

		/// <summary>
		/// The maximum lifetime bound for the particle's random selection.
		/// </summary>
		public float maxLife;
	};
}