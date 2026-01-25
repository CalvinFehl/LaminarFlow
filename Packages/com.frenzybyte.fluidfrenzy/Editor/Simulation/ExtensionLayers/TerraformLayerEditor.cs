using System;
using UnityEditor;
using UnityEngine;

namespace FluidFrenzy.Editor
{
#if UNITY_EDITOR
	[CustomEditor(typeof(TerraformLayer))]
	public class TerraformLayerEditor : ErosionLayerEditor
	{
		internal class Styles
		{
			public static GUIContent fluidMixingText = new GUIContent(
				"Fluid Mixing",
				@"Toggles the interaction logic between overlapping fluid types.

When enabled, if two distinct fluids (e.g., Water and Lava) occupy the same grid cell, they will trigger a mixing event. In standard configurations, this results in the fluid volume being consumed and converted into solid terrain geometry."
			);
			public static GUIContent fluidMixRateText = new GUIContent(
				"Fluid Mix Rate",
				@"Controls the speed of the reaction between interacting fluids.

Higher values cause the fluids to consume each other and generate terrain more rapidly."
			);
			public static GUIContent fluidMixScaleText = new GUIContent(
				"Fluid Mix Scale",
				@"The volumetric conversion ratio between consumed fluid and generated terrain.

This value determines how much solid ground is created for every unit of fluid lost during mixing. 
•  1.0: One unit of fluid volume converts exactly to one unit of terrain volume. 
•  Greater than 1.0: The reaction expands, creating more terrain than the fluid consumed. 
•  Less than 1.0: The reaction contracts, creating less terrain than the fluid consumed."
			);
			public static GUIContent depositRateText = new GUIContent(
				"Deposit Rate",
				@"The rate at which the newly solidified material is integrated into the terrain's heightmap.

While fluidMixRate controls the fluid consumption, this controls the visual rise of the ground. Lower values can smooth out the generation process, preventing abrupt spikes in the terrain mesh."
			);
			public static GUIContent depositTerrainLayersText = new GUIContent(
				"Deposit Terrain Layers",
				"Specifies which vertical terrain layer (e.g., Bedrock or Sediment) receives the newly generated geometry."
			);
			public static GUIContent fluidParticlesText = new GUIContent(
				"Particles",
				@"Configuration for the particle system emitted during fluid mixing events.

This is commonly used to create steam or smoke effects when hot fluids interact with cool fluids (e.g., Lava meeting Water)."
			);
			public static GUIContent emissionRateText = new GUIContent(
				"Emission Rate",
				@"The time interval, in seconds, between consecutive particle spawn events at a mixing location.

A lower value results in a higher frequency of particle emission (more particles), while a higher value results in sparse emission."
			);


			public static readonly GUIContent liquifyLabel = new GUIContent(
		"Liquify",
		"If enabled, this terrain layer will dissolve into a fluid layer over time. This can be used to simulate effects like melting snow."
	);

			public static readonly GUIContent liquifyLayerLabel = new GUIContent(
				"Target Fluid Layer",
				"The fluid layer (e.g., Layer 1, Layer 2) that this terrain will dissolve into."
			);

			public static readonly GUIContent liquifyRateLabel = new GUIContent(
				"Liquify Rate",
				"The speed at which the terrain dissolves into fluid, in units of height per second. Higher values mean faster melting or dissolving."
			);

			public static readonly GUIContent liquifyAmountLabel = new GUIContent(
				"Liquify Amount",
				"The conversion ratio of terrain height to fluid depth. A value of 1 means 1 unit of terrain " +
				"height becomes 1 unit of fluid depth. A value of 2 means 1 unit of terrain becomes 2 units of fluid."
			);

			public static readonly GUIContent fluidLayer1ContactHeaderLabel = new GUIContent(
				"Fluid Layer 1 Contact Reaction",
				"Settings to control how this terrain layer reacts to contact with fluid layer 1."
			);
			public static readonly GUIContent fluidLayer2ContactHeaderLabel = new GUIContent(
				"Fluid Layer 2 Contact Reaction",
				"Settings to control how this terrain layer reacts to contact with fluid layer 2."
			);

			public static readonly GUIContent conversionRateLabel = new GUIContent(
				"Conversion Rate",
				"The speed of the conversion process, in units of height/depth per second."
			);

			public static readonly GUIContent terrainDissolveAmountLabel = new GUIContent(
				"Terrain Dissolve Amount",
				"The amount of the terrain that is consumed when it comes in contact with the fluid, in units of terrain height per second."
			);

			public static readonly GUIContent fluidConsumptionAmountLabel = new GUIContent(
				"Fluid Consumption Amount",
				"The amount of the fluid that is consumed when it comes in contact with the fluid, in units of fluid height per second."
			);

			public static readonly GUIContent convertToTerrainLabel = new GUIContent(
				"Convert Fluid to Terrain",
				"Select terrain layer and splat channel that the terrain and fluid will turn into on contact."
			);

			public static readonly GUIContent convertToTerrainVolumeLabel = new GUIContent(
				"Terrain Volume",
				"A multiplier for the amount of terrain created when terrain is consumed. A value of 1 means 1 unit of terrain height becomes 1 unit of new terrain. A value greater than 1 simulates expansion."
			);

			public static readonly GUIContent targetFluidLayerLabel = new GUIContent(
				"Target Fluid Layer",
				"The target fluid layer that the terrain will turn into when it dissolves (e.g. Lava on snow will create water)."
			);

			public static readonly GUIContent convertToFluidVolumeLabel = new GUIContent(
				"Fluid Volume",
				"A multiplier for the amount of fluid created when terrain is consumed. A value of 1 means 1 unit of terrain height becomes 1 unit of fluid depth. A value greater than 1 simulates expansion."
			);
		}

		SerializedProperty fluidMixing;
		SerializedProperty fluidMixRate;
		SerializedProperty fluidMixScale;
		SerializedProperty depositRate;
		SerializedProperty depositTerrainLayers;
		SerializedProperty depositTerrainSplat;
		SerializedProperty fluidParticles;
		SerializedProperty emissionRate;

		protected override void OnEnable()
		{
			base.OnEnable();

			fluidMixing = serializedObject.FindProperty("fluidMixing");
			fluidMixRate = serializedObject.FindProperty("fluidMixRate");
			fluidMixScale = serializedObject.FindProperty("fluidMixScale");
			depositRate = serializedObject.FindProperty("depositRate");
			depositTerrainLayers = serializedObject.FindProperty("depositTerrainLayers");
			depositTerrainSplat = serializedObject.FindProperty("depositTerrainSplat");
			fluidParticles = serializedObject.FindProperty("fluidParticles");
			emissionRate = serializedObject.FindProperty("emissionRate");
		}

		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();
			serializedObject.Update();

			EditorGUILayout.Space();
			if (EditorExtensions.DrawFoldoutHeaderToggle(fluidMixing, Styles.fluidMixingText))
			{
				EditorGUILayout.PropertyField(fluidMixRate, Styles.fluidMixRateText);
				EditorGUILayout.PropertyField(fluidMixScale, Styles.fluidMixScaleText);
				EditorGUILayout.PropertyField(depositRate, Styles.depositRateText);
				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.PrefixLabel(Styles.depositTerrainLayersText);
				EditorGUILayout.PropertyField(depositTerrainLayers, GUIContent.none);
				EditorGUILayout.PropertyField(depositTerrainSplat, GUIContent.none);
				EditorGUILayout.EndHorizontal();
				EditorGUILayout.PropertyField(fluidParticles, Styles.fluidParticlesText);
				EditorGUILayout.PropertyField(emissionRate, Styles.emissionRateText);
			}
			serializedObject.ApplyModifiedProperties();
		}
	}
}
#endif