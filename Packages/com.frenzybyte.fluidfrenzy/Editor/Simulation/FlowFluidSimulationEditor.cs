using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using static FluidFrenzy.Editor.EditorExtensions;

namespace FluidFrenzy.Editor
{
#if UNITY_EDITOR
	[CustomEditor(typeof(FlowFluidSimulation), editorForChildClasses:true)]
	public class FlowFluidSimulationEditor : FluidSimulationEditor
	{
		internal class Styles
		{
			public static GUIContent iterationsLabel = new GUIContent(
				"Iterations",
				@"The number of internal sub-steps (iterations) the simulation performs per frame to increase numerical stability and accuracy.

This value represents a trade-off between stability and performance. 
•  Stability: A smaller cellWorldSize (higher spatial detail) or faster effective fluid movement requires more iterations to prevent the simulation from becoming unstable. 
•  Performance: Each iteration executes the core simulation logic, meaning increasing this value directly and linearly increases the GPU computation cost."
			);
		}
	}
}
#endif