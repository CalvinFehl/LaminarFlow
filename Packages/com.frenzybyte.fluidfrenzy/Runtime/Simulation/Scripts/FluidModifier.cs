using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

namespace FluidFrenzy
{
	/// <summary>
	/// <see cref="FluidModifier">FluidModifiers</see> are Components that can be attached to a GameObject. 
	/// This is the base class other <see cref="FluidModifier">FluidModifiers</see> (can) derive from and can be used to write custom interactions with the <see cref="FluidSimulation"/>
	/// They are used to interact with the simulation in multiple ways, ranging from adding/removing fluids and applying forces. 
	/// There are several Fluid Modifier types each with specific behaviors.
	/// </summary>
	public class FluidModifier : MonoBehaviour
	{
		[SerializeField]
		internal int version = 2;

		static HashSet<FluidModifier> unqiueWaterModifiers = new HashSet<FluidModifier>();
		public static FluidModifier[] waterModifiers { get; private set; } = new FluidModifier[0];

		static void RegisterWaterInputObject(FluidModifier obj)
		{
			unqiueWaterModifiers.Add(obj);
			if (unqiueWaterModifiers.Count == 0)
			{
				waterModifiers = new FluidModifier[0];
				return;
			}
			waterModifiers = unqiueWaterModifiers.ToArray();
		}

		static void DeregisterWaterInputObject(FluidModifier obj)
		{
			unqiueWaterModifiers.Remove(obj);
			if (unqiueWaterModifiers.Count == 0)
			{
				waterModifiers = new FluidModifier[0];
				return;
			}
			waterModifiers = unqiueWaterModifiers.ToArray();
		}

		protected virtual void Awake()
		{
			RegisterWaterInputObject(this);
		}

		protected virtual void OnDestroy()
		{
			DeregisterWaterInputObject(this);
		}

		public virtual void Process(FluidSimulation fluidSim, float dt)
		{
		}

		public virtual void PostProcess(FluidSimulation fluidSim, float dt)
		{
		}
	}
}