using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

namespace FluidFrenzy
{
	/// <summary>
	/// <see cref="FoamModifier"/> is a modifier that interacts with any <see cref="FoamLayer"/>.
	/// This component can be used to add or remove foam from a <see cref="FoamLayer"/>.
	/// </summary>
	[HelpURL("https://frenzybyte.github.io/fluidfrenzy/docs/fluid_modifiers/#foam-modifier")]
	public class FoamModifier : MonoBehaviour
	{
		/// <summary>
		/// Defines a collection of settings to be used on a <see cref="FluidModifier"/>.
		/// </summary>
		[Serializable]
		public struct FluidFoamSettings
		{
			/// <summary> The amount of foam to add or remove </summary>
			public float strength;
			/// <summary> The falloff/shape of the foam added. </summary>
			public float exponent;
			/// <summary> The size/area that is covered by the modifier to add foam in that region. </summary>
			public Vector2 size;
		}

		static HashSet<FoamModifier> unqiueFoamModifiers = new HashSet<FoamModifier>();
		public static FoamModifier[] foamModifiers { get; private set; } = new FoamModifier[0];

		/// <summary>
		/// The settings used for this <see cref="FluidModifier"/>.
		/// </summary>
		public FluidFoamSettings foamSettings;

		static void RegisterFoamModifier(FoamModifier obj)
		{
			unqiueFoamModifiers.Add(obj);
			if (unqiueFoamModifiers.Count == 0)
			{
				foamModifiers = new FoamModifier[0];
				return;
			}
			foamModifiers = unqiueFoamModifiers.ToArray();
		}

		static void DeregisterFoamModifier(FoamModifier obj)
		{
			unqiueFoamModifiers.Remove(obj);
			if (unqiueFoamModifiers.Count == 0)
			{
				foamModifiers = new FoamModifier[0];
				return;
			}
			foamModifiers = unqiueFoamModifiers.ToArray();
		}

		void Awake()
		{
			RegisterFoamModifier(this);
		}

		private void OnDestroy()
		{
			DeregisterFoamModifier(this);
		}

		public void Process(FoamLayer foamSimulation, float dt)
		{
			foamSimulation.AddFoam(transform.position, foamSettings.size, foamSettings.strength, foamSettings.exponent, dt);
		}


		public Vector3 GetSize()
		{
			Vector2 size = new Vector2(0, 0);
			size = Vector3.Max(size, Vector2.one * foamSettings.size);

			return new Vector3(size.x, 10, size.y);
		}

		public void Scale(Vector2 scale)
		{
			foamSettings.size.Scale(scale);
		}

		private Vector3 SizeToGizmoSize(Vector2 size)
		{
			return new Vector3(size.x, 10, size.y);
		}

		public void OnDrawGizmosSelected()
		{
			Gizmos.color = Color.yellow;
			Gizmos.DrawWireCube(transform.position, SizeToGizmoSize(foamSettings.size));
		}
	}
}