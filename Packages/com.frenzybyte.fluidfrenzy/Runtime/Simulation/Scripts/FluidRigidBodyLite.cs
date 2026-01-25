using UnityEngine;
using System;
using System.Collections.Generic;

namespace FluidFrenzy
{
	/// <summary>
	/// <see cref="FluidRigidBodyLite"/> is a lightweight component for simplified interaction between a <see cref="Rigidbody"/> and the <see cref="FluidSimulation"/>. 
	/// </summary>
	/// <remarks>
	/// <para>
	/// This component applies physics effects such as buoyancy, drag, and advection (movement by the current), and allows the object to generate visual wave and splash effects on the fluid surface. It is designed for performance with minimal setup.
	/// </para>
	/// <para>
	/// **Requirement:** This component requires both a <see cref="Rigidbody"/> and a <see cref="Collider"/> to be attached to the <see cref="GameObject"/>.
	/// </para>
	/// <para>
	/// **Prerequisite:** To allow the CPU to access the fluid height for buoyancy calculations, the <see cref="FluidSimulationSettings.readBackHeight"/> (<c>CPU Height Read</c>) setting must be enabled on the <see cref="FluidSimulation"/>.
	/// </para>
	/// </remarks>
	[RequireComponent(typeof(Rigidbody))]
	[RequireComponent(typeof(Collider))]
	[HelpURL("https://frenzybyte.github.io/fluidfrenzy/docs/fluid_simulation_components/#fluid-rigidbody")]
	public class FluidRigidBodyLite : MonoBehaviour
	{
		[Serializable]
		public struct SplashParticleSystem
		{
			/// <summary>
			/// The <see cref="ParticleSystem"/> to be emitted when the rigidbody hits the fluid.
			/// </summary>
			public ParticleSystem system;

			/// <summary>
			/// If enabled, the particle system's start velocity and emission rate will be overwritten based on the rigidbody's impact speed with the fluid.
			/// </summary>
			public bool overrideSplashParticles;

			/// <summary>
			/// The static or overridden number of particles to emit when the rigidbody hits the fluid.
			/// </summary>
			public int splashEmissionRate;

			/// <summary>
			/// Adjusts the starting speed multiplier of the emitted particles.
			/// </summary>
			public float splashParticleSpeedScale;
		}
		private readonly float s_waterDensity = 1024;

		/// <summary>
		/// If enabled, the object will interact with the fluid simulation by creating waves in its direction of movement (e.g., wakes).
		/// </summary>
		public bool createWaves = false;

		/// <summary>
		/// Adjusts the radius (size) of the generated wave/wake on the fluid surface.
		/// </summary>
		public float waveRadius = 5.0f;

		/// <summary>
		/// Adjusts the height (amplitude) or intensity of the wave.
		/// </summary>
		public float waveStrength = 5.0f;

		/// <summary>
		/// Adjusts the falloff curve of the wave's strength. Higher values mean a faster falloff, which can be used to create sharper or flatter wave/vortex shapes.
		/// </summary>
		public float waveExponent = 0.1f;

		/// <summary>
		/// If enabled, the object will interact with the fluid simulation by generating splashes when falling into the fluid.
		/// </summary>
		public bool createSplashes = true;

		/// <summary>
		/// Adjusts the force applied to the fluid simulation when the object lands in the fluid. Faster falling objects create bigger splashes.
		/// </summary>
		public float splashForce = 45;

		/// <summary>
		/// Adjusts the size of the splash area on the fluid surface.
		/// </summary>
		public float splashRadius = 5.0f;

		/// <summary>
		/// Adjusts the time duration over which the splash force is applied while surface contact is made.
		/// </summary>
		public float splashTime = 0.1f;

		/// <summary>
		/// A list of <see cref="SplashParticleSystem"/> settings that will be spawned when the rigid body makes contact with the fluid.
		/// </summary>
		public List<SplashParticleSystem> splashParticles;

		/// <summary>
		/// Adjusts the influence the <see cref="FluidSimulation"/> velocity field (current) has on the object. Higher values will move the object through the fluid at faster speeds.
		/// </summary>
		public float advectionSpeed = 2.0f;

		/// <summary>
		/// Adjusts the amount of linear drag applied to the object when it is in contact with the fluid.
		/// </summary>
		public float drag = 1;

		/// <summary>
		/// Adjusts the amount of angular (rotational) drag applied to the object when it is in contact with the fluid.
		/// </summary>
		public float angularDrag = 1;

		/// <summary>
		/// Adjusts the buoyancy of the object. Higher values increase the upward force, causing the object to float higher. Lower values make the object float lower or sink.
		/// </summary>
		public float buoyancy = 1;

		private float m_volume = 1;
		protected Rigidbody m_rigidBody = null;
		protected Collider m_collider = null;
		protected Transform m_cachedTransform = null;
		protected Bounds m_localSpaceBounds;
		private Vector3[] m_boundsCorners = null;
		private Vector3[] m_planeCorners = null;

		protected bool m_isInWater = false;
		private float m_splashTimer = 0;
		private Vector3 m_smoothPlaneNormal;
		private FluidModifierVolume m_waveModifier;
		private FluidModifierVolume m_splashModifier;

		protected virtual void Awake()
		{
			m_cachedTransform = transform;
			m_splashModifier = gameObject.AddComponent<FluidModifierVolume>();
			m_splashModifier.type = FluidModifierVolume.FluidModifierType.Force;
			m_splashModifier.forceSettings.mode = FluidModifierVolume.FluidForceSettings.FluidForceMode.Splash;

			GameObject waveModifierGO = new GameObject("Wave");
			waveModifierGO.transform.SetParent(transform, false);
			m_waveModifier = waveModifierGO.AddComponent<FluidModifierVolume>();
			m_waveModifier.type = FluidModifierVolume.FluidModifierType.Force;
			m_waveModifier.forceSettings.mode = FluidModifierVolume.FluidForceSettings.FluidForceMode.Circle;
			m_waveModifier.enabled = false;
		}

		// Start is called before the first frame update
		protected virtual void Start()
		{
			m_rigidBody = GetComponent<Rigidbody>();
			m_collider = GetComponent<Collider>();

			Vector3 size = Vector3.one;
			Vector3 center = Vector3.zero;
			if (m_collider.GetType() == typeof(BoxCollider))
			{
				BoxCollider boxCollider = m_collider as BoxCollider;
				size = boxCollider.size;
				center = boxCollider.center;
				m_localSpaceBounds.size = size;
			}
			else if (m_collider.GetType() == typeof(SphereCollider))
			{
				float radius = (m_collider as SphereCollider).radius;
				size = new Vector3(radius, radius, radius);
				center = (m_collider as SphereCollider).center;
				m_localSpaceBounds.size = size * 2;
			}
			else if (m_collider.GetType() == typeof(MeshCollider))
			{
				Bounds bounds = (m_collider as MeshCollider).bounds;

				// Convert the center from world space to local space
				center = m_cachedTransform.InverseTransformPoint(bounds.center);

				// To convert size, you can do the following:
				// Note: size is not affected by position, only by scale
				// So, scale the size by the object's lossyScale
				size = Vector3.Scale(bounds.size, new Vector3(
					1f / Mathf.Abs(m_cachedTransform.lossyScale.x),
					1f / Mathf.Abs(m_cachedTransform.lossyScale.y),
					1f / Mathf.Abs(m_cachedTransform.lossyScale.z)));
				
				m_localSpaceBounds.size = size;
			}

			m_localSpaceBounds.center = center;

			size *= 0.5f;

			Vector3 boundsMin = -size + m_localSpaceBounds.center;
			Vector3 boundsMax = size + m_localSpaceBounds.center;
			m_boundsCorners = new Vector3[8];
			m_boundsCorners[0] = boundsMin;
			m_boundsCorners[1] = boundsMax;
			m_boundsCorners[2] = new Vector3(boundsMin.x, boundsMin.y, boundsMax.z);
			m_boundsCorners[3] = new Vector3(boundsMin.x, boundsMax.y, boundsMin.z);
			m_boundsCorners[4] = new Vector3(boundsMax.x, boundsMin.y, boundsMin.z);
			m_boundsCorners[5] = new Vector3(boundsMin.x, boundsMax.y, boundsMax.z);
			m_boundsCorners[6] = new Vector3(boundsMax.x, boundsMin.y, boundsMax.z);
			m_boundsCorners[7] = new Vector3(boundsMax.x, boundsMax.y, boundsMin.z);

			m_planeCorners = new Vector3[4];
			m_planeCorners[0] = m_boundsCorners[0];
			m_planeCorners[1] = m_boundsCorners[2];
			m_planeCorners[2] = m_boundsCorners[4];
			m_planeCorners[3] = m_boundsCorners[6];

			Vector3 worldSpaceBoundsSize = m_localSpaceBounds.size;
			worldSpaceBoundsSize.Scale(m_cachedTransform.lossyScale);
			m_volume = worldSpaceBoundsSize.x * worldSpaceBoundsSize.y * worldSpaceBoundsSize.z;
		}

		protected virtual void OnDestroy()
        {

        }

		protected virtual void FixedUpdate()
		{
			bool wasInWater = m_isInWater;
			m_isInWater = false;
			Vector3 s = Vector3.Scale(m_localSpaceBounds.size, m_cachedTransform.lossyScale);
			float cornerWeight = 1.0f / m_boundsCorners.Length;
			Vector3 buoyancyForce = s_waterDensity * -Physics.gravity * m_volume * buoyancy * cornerWeight;
			Vector3 advectionVelocity = Vector3.zero;

			Vector2 velocityScale2D = FluidSimulationManager.simulations[0].GetWorldVelocityScale();
			Vector3 velocityScale3D = new Vector3(velocityScale2D.x, 0, velocityScale2D.y);

			Matrix4x4 localToWorld = m_cachedTransform.localToWorldMatrix;
			Vector3 transformPosition = m_cachedTransform.position;
			Quaternion transformRotation = m_cachedTransform.rotation;

			Vector3 n = m_cachedTransform.up;
			if (FluidSimulationManager.GetNormal(transformPosition, out Vector3 oN))
			{
				n = oN;
			}

			m_smoothPlaneNormal = Vector3.Lerp(m_smoothPlaneNormal, n, 0.5f);
			Vector3 g = Physics.gravity;
			Vector3 p = Vector3.Project(g, n);
			Vector3 f = g - p;
			Vector3 a = g.normalized * (g.magnitude - f.magnitude);
			float planarForce = new Vector2(f.x, f.z).magnitude;

			Vector3 surfaceGravity = (f + a) / m_boundsCorners.Length;
			Vector3 gravity = g / m_boundsCorners.Length;


			foreach (Vector3 offset in m_boundsCorners)
			{
				Vector3 transformedOffset = localToWorld.MultiplyVector(offset);
				Vector3 objPos = transformPosition + transformedOffset;
				if (FluidSimulationManager.GetHeightVelocity(objPos, out Vector2 pos, out Vector3 vel))
				{
					float objHeight = s.y;
					float waterSpaceHeight = (objPos.y);
					if (waterSpaceHeight < pos.x && pos.y > 0)
					{
						vel.Scale(velocityScale3D);
						Vector3 waterVelocity = vel * advectionSpeed * cornerWeight;
						m_rigidBody.AddForceAtPosition(waterVelocity * m_rigidBody.mass, objPos, ForceMode.Force);
						m_rigidBody.AddForceAtPosition(surfaceGravity * m_rigidBody.mass, objPos, ForceMode.Force);
						float underWaterPct = Mathf.Clamp01((pos.x - objPos.y) / objHeight);
						Vector3 force = Mathf.Sqrt(underWaterPct) * buoyancyForce;
						m_rigidBody.AddForceAtPosition(force, objPos, ForceMode.Force);

						m_isInWater = true;
						advectionVelocity += waterVelocity;
					}
					else
					{
						m_rigidBody.AddForceAtPosition(gravity * m_rigidBody.mass, objPos, ForceMode.Force);
					}
				}
			}

#if UNITY_6000_0_OR_NEWER
			Vector3 rigidBodyVelocity = m_rigidBody.linearVelocity;
#else
			Vector3 rigidBodyVelocity = m_rigidBody.velocity;
#endif
			if (m_isInWater)
			{
				m_rigidBody.AddForce(-rigidBodyVelocity * drag * Time.fixedDeltaTime, ForceMode.VelocityChange);
				m_rigidBody.AddTorque(-m_rigidBody.angularVelocity * angularDrag * Time.fixedDeltaTime, ForceMode.VelocityChange);
				if (wasInWater == false && m_splashTimer <= 0 && rigidBodyVelocity.y < 0)
				{
					m_splashTimer = splashTime;
				}
			}


			bool belowSurface = false;
			if (FluidSimulationManager.GetHeight(transformPosition, out Vector2 pos2))
			{
				float objHeight = s.y;
				belowSurface = (transformPosition.y + objHeight * 0.5f) < pos2.x;
			}

			if (createWaves)
			{

				Vector2 direction = new Vector2(rigidBodyVelocity.x, rigidBodyVelocity.z);
				bool doWave = direction.magnitude > 0.01f && m_isInWater && !belowSurface;
				m_waveModifier.enabled = doWave;
				if (doWave)
				{
					float cosTheta = Mathf.Abs(Vector3.Dot(rigidBodyVelocity.normalized, m_cachedTransform.forward));
					float offsetForce = Mathf.Lerp(s.x, s.z, cosTheta) * 0.33f;
					advectionVelocity /= Time.fixedDeltaTime;
					float speed = Mathf.Max(0, direction.magnitude - planarForce);
					m_waveModifier.transform.position = transformPosition + rigidBodyVelocity.normalized * offsetForce;
					m_waveModifier.forceSettings.direction = direction.normalized;
					m_waveModifier.forceSettings.strength = waveStrength * speed;
					m_waveModifier.forceSettings.falloff = waveExponent;
					m_waveModifier.forceSettings.size = Vector2.one * waveRadius;
				}
			}
			else
			{
				m_waveModifier.enabled = false;
			}

			if (createSplashes)
			{ 
				bool doSplash = m_splashTimer > 0;
				m_splashModifier.enabled = doSplash && !belowSurface;
				if (doSplash)
				{
					
					float force = rigidBodyVelocity.y;
					float absForce = Mathf.Abs(force);
					m_splashModifier.forceSettings.size = Vector2.one * splashRadius;
					m_splashModifier.forceSettings.strength = splashForce * absForce;
					m_splashModifier.forceSettings.falloff = 1;
					m_splashTimer -= Time.fixedDeltaTime;

					foreach (SplashParticleSystem particles in splashParticles)
					{
						ParticleSystem ps = particles.system;
						if (ps)
						{
							ps.transform.localRotation = Quaternion.Inverse(transformRotation);
							Vector3 pos = transformPosition;
							pos.y = pos2.x;
							ps.transform.position = pos;
							if (particles.overrideSplashParticles)
							{
								ParticleSystem.MainModule currentModule = ps.main;
								ParticleSystem.MinMaxCurve currentSpeed = currentModule.startSpeed;
								ParticleSystem.MinMaxCurve speedCurve = currentSpeed;
								float speedScale = absForce * particles.splashParticleSpeedScale / 100;
								speedCurve.curveMultiplier = speedScale;
								speedCurve.constant *= speedScale;
								speedCurve.constantMin *= speedScale;
								speedCurve.constantMax *= speedScale;
								currentModule.startSpeed = speedCurve;
								ps.Emit(particles.splashEmissionRate);
								currentModule.startSpeed = currentSpeed;
							}
							else
							{
								ps.Emit(particles.splashEmissionRate);
							}
						}
					}
				}
			}
			else
			{
				m_splashModifier.enabled = false;
			}
		}

		private void OnDrawGizmosSelected()
		{
			Vector3 position = transform.position;
			if (m_boundsCorners != null && m_collider)
			{
				Vector3 center = transform.worldToLocalMatrix.MultiplyPoint(m_collider.bounds.center);
				center.Scale(transform.lossyScale);
				foreach (Vector3 offset in m_boundsCorners)
				{
					Vector3 transformedOffset = transform.localToWorldMatrix.MultiplyVector(offset);
					//transformedOffset.x /= transform.lossyScale.x;
					//transformedOffset.y /= transform.lossyScale.y;
					//transformedOffset.z /= transform.lossyScale.z;
					Vector3 objPos = position + (transformedOffset);

					Gizmos.DrawCube(objPos, Vector3.one * 0.1f);
				}
			}

			Vector3 g = Physics.gravity;
			Vector3 n = m_smoothPlaneNormal;
			Vector3 p = Vector3.Project(g, n);
			Vector3 f = g - p;
			Vector3 a = g.normalized * (g.magnitude - f.magnitude);
			Gizmos.color = Color.red;
			Gizmos.DrawLine(position, position + g);

			Gizmos.color = Color.green;
			Gizmos.DrawLine(position, position + n);

			Gizmos.color = Color.magenta;
			Gizmos.DrawLine(position, position + p);

			Gizmos.color = Color.yellow;
			Gizmos.DrawLine(position, position + f);

			Gizmos.color = Color.blue;
			Gizmos.DrawLine(position, position + a);
		}
	}
}