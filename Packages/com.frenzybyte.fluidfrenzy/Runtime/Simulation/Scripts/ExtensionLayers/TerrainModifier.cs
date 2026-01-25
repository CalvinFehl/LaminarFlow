using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;
using FluidModifierSpace = FluidFrenzy.FluidSimulation.FluidModifierSpace;

namespace FluidFrenzy
{
	/// <summary>
	/// <see cref="TerrainModifier"/> is a component used to interactively modify the underlying terrain heightfield within the <see cref="FluidSimulation"/>. 
	/// </summary>
	/// <remarks>
	/// <para>
	/// This component acts as a "brush" for precise terrain editing, allowing you to raise, lower, or set the height of the solid ground layer. It is ideal for dynamic world sculpting or in-game level editing.
	/// </para>
	/// <para>
	/// **Note:** This component requires and works in conjunction with a specialized terrain system on the <see cref="FluidSimulation"/>, such as the <c>TerraformLayer</c> or <c>ErosionLayer</c> and the <c>Simple/TerraformTerrain</c> types, to enable terrain height modifications.
	/// </para>
	/// <para>
	/// The modification effect is defined by the <see cref="TerrainModifierSettings"/> and applied within a localized area determined by the selected <see cref="TerrainInputMode"/> and <see cref="size"/>.
	/// </para>
	/// </remarks>
	public class TerrainModifier : MonoBehaviour
	{
		public enum TerrainModifierBlendMode
		{
			/// <summary>
			/// The modifier will directly set the terrain's height to the target value (derived from the component's position and/or <see cref="strength"/>).
			/// </summary>
			Set,
			/// <summary>
			/// The modifier will add or subtract the <see cref="strength"/> value to/from the current terrain height **per second**.
			/// </summary>
			Additive,
			/// <summary>
			/// The modifier will set the terrain height to the minimum value between its current height and the target value.
			/// </summary>
			Minimum,
			/// <summary>
			/// The modifier will set the terrain height to the maximum value between its current height and the target value.
			/// </summary>
			Maximum,
		}

		public enum TerrainInputMode
		{
			/// <summary>
			/// The modifier applies the effect within a circular area.
			/// </summary>
			Circle,
			/// <summary>
			/// The modifier applies the effect within a rectangular area.
			/// </summary>
			Box,
			/// <summary>
			/// The modifier uses a source texture to define the shape and intensity of the effect.
			/// </summary>
			Texture
		}

		[Serializable]
		public class TerrainModifierSettings
		{
			/// <summary>
			/// Defines the shape or source of the modification brush.
			/// </summary>
			/// <remarks>
			/// Input modes include:
			/// <list type="bullet">
			/// 	<item>
			/// 		<term><see cref="TerrainInputMode.Circle"/></term>
			/// 		<description>The brush applies the modification within a circular area.</description>
			/// 	</item>
			/// 	<item>
			/// 		<term><see cref="TerrainInputMode.Box"/></term>
			/// 		<description>The brush applies the modification within a rectangular area.</description>
			/// 	</item>
			/// 	<item>
			/// 		<term><see cref="TerrainInputMode.Texture"/></term>
			/// 		<description>The brush uses a source texture to define the shape and intensity.</description>
			/// 	</item>
			/// </list>
			/// </remarks>
			public TerrainInputMode mode = TerrainInputMode.Circle;

			/// <summary>
			/// Defines the mathematical operation used to apply the modification to the terrain.
			/// </summary>
			/// <remarks>
			/// This determines how the modification is applied to the terrain. Options include:
			/// <list type="bullet">
			/// 	<item>
			/// 		<term><see cref="TerrainModifierBlendMode.Additive"/></term>
			/// 		<description>Raising or lowering the height over time.</description>
			/// 	</item>
			/// 	<item>
			/// 		<term><see cref="TerrainModifierBlendMode.Set"/></term>
			/// 		<description>Setting the height to a specific value.</description>
			/// 	</item>
			/// 	<item>
			/// 		<term><see cref="TerrainModifierBlendMode.Minimum"/>/<see cref="TerrainModifierBlendMode.Maximum"/></term>
			/// 		<description>Clamping the height to the target value.</description>
			/// 	</item>
			/// </list>
			/// </remarks>
			public TerrainModifierBlendMode blendMode = TerrainModifierBlendMode.Additive;

			/// <summary>
			/// Specifies the coordinate space used for height modifications.
			/// </summary>
			/// <remarks>
			/// <list type="bullet">
			/// 	<item>
			/// 		<term><see cref="FluidSimulation.FluidModifierSpace.WorldHeight"/></term>
			/// 		<description>The height is interpreted as a specific world Y-coordinate.</description>
			/// 	</item>
			/// 	<item>
			/// 		<term><see cref="FluidSimulation.FluidModifierSpace.LocalHeight"/></term>
			/// 		<description>The height is interpreted relative to the base terrain height.</description>
			/// 	</item>
			/// </list>
			/// </remarks>
			public FluidModifierSpace space = FluidModifierSpace.WorldHeight;

			/// <summary>
			/// Controls the magnitude or intensity of the terrain deformation.
			/// </summary>
			/// <remarks>
			/// - For Additive blending, this is the amount of height to add/subtract *per second*.<br/>
			/// - For Set, Minimum, or Maximum blending, this value contributes to the target height.
			/// </remarks>
			public float strength = 5;

			/// <summary>
			/// Adjust the range used to remap the normalized input value (e.g., from a texture) to the final output strength.
			/// </summary>
			/// <remarks>
			/// A normalized input of 0 is mapped to <c>remap.x</c>, and an input of 1 is mapped to <c>remap.y</c>.
			/// </remarks>
			public Vector2 remap = new Vector2(0, 1);

			/// <summary>
			/// Adjust the softness of the brush edge (falloff) for the Circle and Box input modes.
			/// </summary>
			/// <remarks>
			/// Higher values create a wider and softer transition at the modification boundary.
			/// </remarks>
			[FormerlySerializedAs("exponent")]
			[Range(0.001f, 5)]
			public float falloff = 1;

			/// <summary>
			/// Adjust the size (width and height) of the modification area in world units.
			/// </summary>
			public Vector2 size = new Vector2(10, 10);

			/// <summary>
			/// Specifies the target terrain layer (e.g., a specific heightmap channel) to modify.
			/// </summary>
			/// <remarks>
			/// Typically used to select between the channels of a multi-channel heightmap, such as channel 0 for Red and 1 for Green.
			/// </remarks>
			public ErosionLayer.TerrainLayer layer = 0;

			/// <summary>
			/// Specifies the target splatmap channel to use when the blend mode involves a terrain splatmap.
			/// </summary>
			/// <remarks>
			/// Used to select a specific texture layer for blending (e.g., channel 0 for Red, 1 for Green, etc.).
			/// </remarks>
			public ErosionLayer.SplatChannel splat = 0;

			/// <summary>
			/// The source texture used to define the modification shape and intensity when <see cref="mode"/> is set to <see cref="TerrainInputMode.Texture"/>.
			/// </summary>
			public Texture2D texture = null;
		}

		static HashSet<TerrainModifier> unqiueTerrainModifiers = new HashSet<TerrainModifier>();
		public static TerrainModifier[] terrainModifiers { get; private set; } = new TerrainModifier[0];

		public TerrainModifierSettings settings = new TerrainModifierSettings();

		static void RegisterTerrainModifier(TerrainModifier obj)
		{
			unqiueTerrainModifiers.Add(obj);
			if (unqiueTerrainModifiers.Count == 0)
			{
				terrainModifiers = new TerrainModifier[0];
				return;
			}
			terrainModifiers = unqiueTerrainModifiers.ToArray();
		}

		static void DeregisterTerrainModifier(TerrainModifier obj)
		{
			unqiueTerrainModifiers.Remove(obj);
			if (unqiueTerrainModifiers.Count == 0)
			{
				terrainModifiers = new TerrainModifier[0];
				return;
			}
			terrainModifiers = unqiueTerrainModifiers.ToArray();
		}

		void Awake()
		{
			RegisterTerrainModifier(this);
		}

		private void OnDestroy()
		{
			DeregisterTerrainModifier(this);
		}

		public void Process(ErosionLayer terrainLayer, float dt)
		{
			float height = transform.position.y;
			float rotation = transform.rotation.eulerAngles.y;
			if (settings.mode == TerrainInputMode.Circle)
			{
				if (settings.blendMode == TerrainModifierBlendMode.Additive)
				{
					terrainLayer.AddTerrainCircle(transform.position, settings.size, rotation, settings.strength, settings.falloff, settings.layer, settings.splat, dt);
				}
				else if (settings.blendMode == TerrainModifierBlendMode.Set)
				{
					if (settings.space == FluidModifierSpace.WorldHeight)
					{
						terrainLayer.SetTerrainHeightCircle(transform.position, settings.size, rotation, height, settings.falloff, settings.layer, settings.splat, settings.space);
					}
					else
					{
						terrainLayer.SetTerrainDepthCircle(transform.position, settings.size, rotation, settings.strength, settings.falloff, settings.layer, settings.splat, settings.space);
					}
				}
				else if (settings.blendMode == TerrainModifierBlendMode.Minimum)
				{
					if (settings.space == FluidModifierSpace.WorldHeight)
					{
						terrainLayer.MinTerrainHeightCircle(transform.position, settings.size, rotation, height, settings.falloff, settings.layer, settings.splat, settings.space);
					}
					else
					{
						terrainLayer.MinTerrainDepthCircle(transform.position, settings.size, rotation, settings.strength, settings.falloff, settings.layer, settings.splat, settings.space);
					}
				}
				else if (settings.blendMode == TerrainModifierBlendMode.Maximum)
				{
					if (settings.space == FluidModifierSpace.WorldHeight)
					{
						terrainLayer.MaxTerrainHeightCircle(transform.position, settings.size, rotation, height, settings.falloff, settings.layer, settings.splat, settings.space);
					}
					else
					{
						terrainLayer.MaxTerrainDepthCircle(transform.position, settings.size, rotation, settings.strength, settings.falloff, settings.layer, settings.splat, settings.space);
					}
				}
			}
			else if (settings.mode == TerrainInputMode.Box)
			{
				if (settings.blendMode == TerrainModifierBlendMode.Additive)
				{
					terrainLayer.AddTerrainSquare(transform.position, settings.size, rotation, settings.strength, settings.falloff, settings.layer, settings.splat, dt);
				}
				else if (settings.blendMode == TerrainModifierBlendMode.Set)
				{
					if (settings.space == FluidModifierSpace.WorldHeight)
					{
						terrainLayer.SetTerrainHeightSquare(transform.position, settings.size, rotation, height, settings.falloff, settings.layer, settings.splat, settings.space);
					}
					else
					{
						terrainLayer.SetTerrainDepthSquare(transform.position, settings.size, rotation, settings.strength, settings.falloff, settings.layer, settings.splat, settings.space);
					}
				}
				else if (settings.blendMode == TerrainModifierBlendMode.Minimum)
				{
					if (settings.space == FluidModifierSpace.WorldHeight)
					{
						terrainLayer.MinTerrainHeightSquare(transform.position, settings.size, rotation, height, settings.falloff, settings.layer, settings.splat, settings.space);
					}
					else
					{
						terrainLayer.MinTerrainDepthSquare(transform.position, settings.size, rotation, settings.strength, settings.falloff, settings.layer, settings.splat, settings.space);
					}
				}
				else if (settings.blendMode == TerrainModifierBlendMode.Maximum)
				{
					if (settings.space == FluidModifierSpace.WorldHeight)
					{
						terrainLayer.MaxTerrainHeightSquare(transform.position, settings.size, rotation, height, settings.falloff, settings.layer, settings.splat, settings.space);
					}
					else
					{
						terrainLayer.MaxTerrainDepthSquare(transform.position, settings.size, rotation, settings.strength, settings.falloff, settings.layer, settings.splat, settings.space);
					}
				}
			}
			else if (settings.mode == TerrainInputMode.Texture)
			{
				if (settings.blendMode == TerrainModifierBlendMode.Additive)
				{
					terrainLayer.AddTerrainTexture(transform.position, settings.size, rotation, settings.texture, settings.strength, settings.remap, settings.layer, settings.splat, dt);
				}
				else if (settings.blendMode == TerrainModifierBlendMode.Set)
				{
					if (settings.space == FluidModifierSpace.WorldHeight)
					{
						terrainLayer.SetTerrainHeightTexture(transform.position, settings.size, rotation, settings.texture, height, settings.remap, settings.layer, settings.splat, settings.space);
					}
					else
					{
						terrainLayer.SetTerrainDepthTexture(transform.position, settings.size, rotation, settings.texture, settings.strength, settings.remap, settings.layer, settings.splat, settings.space);
					}
				}				
				else if (settings.blendMode == TerrainModifierBlendMode.Minimum)
				{
					if (settings.space == FluidModifierSpace.WorldHeight)
					{
						terrainLayer.MinTerrainHeightTexture(transform.position, settings.size, rotation, settings.texture, height, settings.remap, settings.layer, settings.splat, settings.space);
					}
					else
					{
						terrainLayer.MinTerrainDepthTexture(transform.position, settings.size, rotation, settings.texture, settings.strength, settings.remap, settings.layer, settings.splat, settings.space);
					}
				}				
				else if (settings.blendMode == TerrainModifierBlendMode.Maximum)
				{
					if (settings.space == FluidModifierSpace.WorldHeight)
					{
						terrainLayer.MaxTerrainHeightTexture(transform.position, settings.size, rotation, settings.texture, height, settings.remap, settings.layer, settings.splat, settings.space);
					}
					else
					{
						terrainLayer.MaxTerrainDepthTexture(transform.position, settings.size, rotation, settings.texture, settings.strength, settings.remap, settings.layer, settings.splat, settings.space);
					}
				}
			}
		}

		public Vector3 GetSize()
		{
			Vector2 size = new Vector2(0, 0);
			size = Vector3.Max(size, Vector2.one * settings.size);

			return new Vector3(size.x, 10, size.y);
		}

		public void Scale(Vector2 scale)
		{
			settings.size.Scale(scale);
		}

		private Vector3 SizeToGizmoSize(Vector2 size)
		{
			return new Vector3(size.x, 10, size.y);
		}

		public void OnDrawGizmosSelected()
		{
			Matrix4x4 prevMatrix = Gizmos.matrix;
			Gizmos.matrix = transform.localToWorldMatrix;
			Gizmos.color = Color.yellow;
			Gizmos.DrawWireCube(Vector3.zero, SizeToGizmoSize(settings.size));
			Gizmos.matrix = prevMatrix;
		}
	}
}