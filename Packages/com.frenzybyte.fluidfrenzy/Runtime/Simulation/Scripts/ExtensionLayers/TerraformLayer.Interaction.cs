using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;
using static FluidFrenzy.FluidSimulation;

namespace FluidFrenzy
{
	public partial class TerraformLayer : ErosionLayer
	{
		/// <summary>
		/// Defines the changes that occur when a specific terrain layer comes into contact with fluid.
		/// </summary>
		/// <remarks>
		/// This class acts as a "recipe" for interactions, such as Lava turning Grass into Rock (scorching) or Water turning Soil into Mud.
		/// </remarks>
		[Serializable]
		public class FluidContactReaction
		{
			/// <summary>
			/// Toggles this specific contact reaction.
			/// </summary>
			public bool enabled = false;

			/// <summary>
			/// The global speed multiplier for this reaction.
			/// </summary>
			/// <remarks>
			/// Defines how fast the transition happens in units per second. Higher values result in near-instant transformations.
			/// </remarks>
			[Range(0, 100)]
			public float conversionRate = 1;

			/// <summary>
			/// The amount of solid terrain removed per second while in contact with the fluid.
			/// </summary>
			/// <remarks>
			/// Use this to simulate the terrain being "eaten away" or dissolved by the fluid.
			/// </remarks>
			[Range(0, 4)]
			public float terrainDissolveAmount = 1;

			/// <summary>
			/// The amount of fluid volume consumed per second during the reaction.
			/// </summary>
			/// <remarks>
			/// Use this to simulate fluid evaporating (e.g., lava cooling on rock) or soaking into the ground.
			/// </remarks>
			[Range(0, 4)]
			public float fluidConsumptionAmount = 1;

			/// <summary>
			/// The target terrain layer that the original terrain will transform into.
			/// </summary>
			/// <remarks>
			/// For example, if Water touches a "Dirt" layer, you might set this to a "Mud" layer.
			/// </remarks>
			public TerrainLayerMask convertToTerrainLayer = TerrainLayerMask.None;

			/// <summary>
			/// The visual texture (splat channel) applied to the transformed terrain.
			/// </summary>
			/// <remarks>
			/// This updates the terrain's appearance to match its new physical properties (e.g., turning green grass texture into grey rock).
			/// </remarks>
			public SplatChannel convertToSplatChannel = SplatChannel.None;

			/// <summary>
			/// The volumetric conversion ratio for generating new terrain.
			/// </summary>
			/// <remarks>
			/// Determines how much new solid ground is created relative to the amount consumed.
			/// <list type="bullet">
			/// 	<item>
			/// 		<term>1.0</term>
			/// 		<description>One unit of consumed terrain becomes exactly one unit of new terrain.</description>
			/// 	</item>
			/// 	<item>
			/// 		<term>Greater than 1.0</term>
			/// 		<description>The reaction expands, creating more terrain volume than was consumed.</description>
			/// 	</item>
			/// </list>
			/// </remarks>
			[Range(0, 4)]
			public float convertToTerrainVolume = 1;

			/// <summary>
			/// The target fluid layer to generate when the terrain dissolves.
			/// </summary>
			/// <remarks>
			/// Use this for reactions where solid ground turns into liquid, such as Lava melting Ice into Water.
			/// </remarks>
			public FluidSimulation.FluidLayerIndex convertToFluidLayer;

			/// <summary>
			/// The volumetric conversion ratio for generating new fluid.
			/// </summary>
			/// <remarks>
			/// Determines how much liquid is produced relative to the amount of terrain dissolved.
			/// <list type="bullet">
			/// 	<item>
			/// 		<term>1.0</term>
			/// 		<description>One unit of terrain height becomes one unit of fluid depth.</description>
			/// 	</item>
			/// 	<item>
			/// 		<term>Greater than 1.0</term>
			/// 		<description>The reaction expands, creating more fluid volume than the terrain that was dissolved.</description>
			/// 	</item>
			/// </list>
			/// </remarks>
			[Range(0, 4)]
			public float convertToFluidVolume = 1;

			internal float ConvertsToTerrain(TerrainLayerMask mask)
			{
				return (convertToTerrainLayer & mask) != 0 ? 1 : 0;
			}

			internal Vector4 ConvertsToSplatMask()
			{
				return SplatChannelToMask(convertToSplatChannel);
			}
		}

		/// <summary>
		/// An extended configuration profile for the <see cref="TerraformLayer"/>, including standard erosion settings and advanced contact interactions.
		/// </summary>
		[Serializable]
		public class TerraformSettings : ErosionSettings
		{
			/// <summary>
			/// Toggles the automatic "liquefaction" of this terrain layer over time, independent of fluid contact.
			/// </summary>
			/// <remarks>
			/// Useful for simulating unstable materials like melting snow or ice that naturally turns into fluid.
			/// </remarks>
			public bool liquify = false;

			/// <summary>
			/// The target fluid layer (e.g., Layer 1, Layer 2) that this terrain layer dissolves into when <see cref="liquify"/> is enabled.
			/// </summary>
			public FluidSimulation.FluidLayerIndex liquifyLayer = FluidSimulation.FluidLayerIndex.Layer1;

			/// <summary>
			/// The speed at which the terrain naturally dissolves into fluid, in units of height per second.
			/// </summary>
			[Range(0, 10)]
			public float liquifyRate = 1;

			/// <summary>
			/// The volumetric conversion ratio of terrain height to fluid depth during liquefaction.
			/// </summary>
			/// <remarks>
			/// <list type="bullet">
			/// 	<item>
			/// 		<term>1.0</term>
			/// 		<description>1 unit of terrain height becomes 1 unit of fluid depth.</description>
			/// 	</item>
			/// 	<item>
			/// 		<term>2.0</term>
			/// 		<description>1 unit of terrain produces 2 units of fluid.</description>
			/// 	</item>
			/// </list>
			/// </remarks>
			[Range(0, 10)]
			public float liquifyAmount = 1;

			/// <summary>
			/// Defines the reaction rules applied when this terrain layer comes into contact with Fluid Layer 1.
			/// </summary>
			public FluidContactReaction fluidLayer1Contact;

			/// <summary>
			/// Defines the reaction rules applied when this terrain layer comes into contact with Fluid Layer 2.
			/// </summary>
			public FluidContactReaction fluidLayer2Contact;
		}

		private void UpdateTerraformModifier(FluidSimulation simulation, float dt)
		{
			foreach (TerraformModifier input in TerraformModifier.terraformModifiers)
			{
				if (input.isActiveAndEnabled)
				{
					input.Process(this, dt);
				}
			}
		}

		public override void AddTerrainCircle(Vector3 worldPos, Vector2 size, float rotation, float strength, float exponent, TerrainLayer layer, SplatChannel splatchannel, float dt)
		{
			base.AddTerrainCircle(worldPos, size, rotation, strength, exponent, layer, splatchannel, dt);
			AddSplatmap(worldPos, size, rotation, strength, exponent, layer, splatchannel, dt, m_addSplatmapCirclePass);
		}

		public override void AddTerrainSquare(Vector3 worldPos, Vector2 size, float rotation, float strength, float exponent, TerrainLayer layer, SplatChannel splatchannel, float dt)
		{
			base.AddTerrainSquare(worldPos, size, rotation, strength, exponent, layer, splatchannel, dt);
			AddSplatmap(worldPos, size, rotation, strength, exponent, layer, splatchannel, dt, m_addSplatmapSquarePass);
		}

		private void AddSplatmap(Vector3 worldPos, Vector2 size, float rotation, float strength, float exponent, TerrainLayer layer, SplatChannel splatchannel, float dt, int pass)
		{
			if (!m_terrainSupportsTerraform || layer > 0 || splatchannel == SplatChannel.None)
			{
				return;
			}

			if (layer > 0) return;
			Vector2 position = m_parentSimulation.WorldSpaceToPaddedUVSpace(worldPos);
			Vector2 uvSize = m_parentSimulation.WorldSizeToUVSize(size);

			m_externalPropertyBlock.Clear();
			m_externalPropertyBlock.SetVector(FluidShaderProperties._BlitScaleBiasRt, new Vector4(uvSize.x, uvSize.y, position.x, position.y));
			m_externalPropertyBlock.SetVector(FluidShaderProperties._BlitRotation, GraphicsHelpers.DegreesToVec2(rotation));
			m_externalPropertyBlock.SetFloat(FluidShaderProperties._IncreaseStrength, strength * dt);
			m_externalPropertyBlock.SetFloat(FluidShaderProperties._IncreaseExponent, exponent);
			m_externalPropertyBlock.SetVector(FluidShaderProperties._LayerMask, Vector4.one);

			Vector4 mask = Vector4.zero;
			mask[(int)splatchannel] = 1;
			m_externalPropertyBlock.SetVector(FluidShaderProperties._SplatmapMask, mask);

			m_externalPropertyBlock.SetTexture(FluidShaderProperties._FluidHeightField, m_activeTerrainheight);
			FluidSimulation.BlitQuad(m_commandBuffer, null, m_splatmap0, m_addTerrainMaterial, m_externalPropertyBlock, pass);
			FluidSimulation.BlitQuad(m_commandBuffer, null, m_splatmap0, m_addTerrainMaterial, m_externalPropertyBlock, pass + 1);
		}


		public override void AddTerrainTexture(Vector3 worldPos, Vector2 worldSize, float rotation, Texture source, float strength, Vector2 remap, TerrainLayer layer, SplatChannel splatchannel, float dt)
		{
			base.AddTerrainTexture(worldPos, worldSize, rotation, source, strength, remap, layer, splatchannel, dt);
			AddTextureSplatmap(worldPos, worldSize, rotation, source, strength, layer, splatchannel, dt);
		}

		private void AddTextureSplatmap(Vector3 worldPos, Vector2 worldSize, float rotation, Texture source, float strength, TerrainLayer layer, SplatChannel splatchannel, float dt)
		{
			if (!m_terrainSupportsTerraform || layer > 0 || splatchannel == SplatChannel.None)
			{
				return;
			}

			Vector2 position = m_parentSimulation.WorldSpaceToPaddedUVSpace(worldPos);
			Vector2 uvSize = m_parentSimulation.WorldSizeToUVSize(worldSize);

			m_externalPropertyBlock.Clear();
			m_externalPropertyBlock.SetVector(FluidShaderProperties._BlitScaleBiasRt, new Vector4(uvSize.x, uvSize.y, position.x, position.y));
			m_externalPropertyBlock.SetVector(FluidShaderProperties._BlitRotation, GraphicsHelpers.DegreesToVec2(rotation));
			m_externalPropertyBlock.SetFloat(FluidShaderProperties._IncreaseStrength, strength * dt);
			m_externalPropertyBlock.SetVector(FluidShaderProperties._LayerMask, Vector4.one);

			Vector4 mask = Vector4.zero;
			mask[(int)splatchannel] = 1;
			m_externalPropertyBlock.SetVector(FluidShaderProperties._SplatmapMask, mask);

			m_externalPropertyBlock.SetTexture(FluidShaderProperties._FluidHeightField, m_activeTerrainheight);
			FluidSimulation.BlitQuad(m_commandBuffer, source, m_splatmap0, m_addTerrainMaterial, m_externalPropertyBlock, m_addSplatmapTexturePass);
			FluidSimulation.BlitQuad(m_commandBuffer, source, m_splatmap0, m_addTerrainMaterial, m_externalPropertyBlock, m_subSplatmapTexturePass);
		}

		protected override void SetSplatmap(Vector3 worldPos, Vector2 size, float rotation, TerrainLayer layer, SplatChannel splatchannel, int pass)
		{
			if (!m_terrainSupportsTerraform || layer > 0 || splatchannel == SplatChannel.None)
			{
				return;
			}
			Vector2 position = m_parentSimulation.WorldSpaceToPaddedUVSpace(worldPos);
			Vector2 uvSize = m_parentSimulation.WorldSizeToUVSize(size);

			m_externalPropertyBlock.Clear();
			m_externalPropertyBlock.SetVector(FluidShaderProperties._BlitScaleBiasRt, new Vector4(uvSize.x, uvSize.y, position.x, position.y));
			m_externalPropertyBlock.SetVector(FluidShaderProperties._BlitRotation, GraphicsHelpers.DegreesToVec2(rotation));
			m_externalPropertyBlock.SetVector(FluidShaderProperties._LayerMask, Vector4.one);
			m_externalPropertyBlock.SetFloat(FluidShaderProperties._IncreaseStrength, 1);

			Vector4 mask = Vector4.zero;
			mask[(int)splatchannel] = 1;
			m_externalPropertyBlock.SetVector(FluidShaderProperties._SplatmapMask, mask);

			m_externalPropertyBlock.SetTexture(FluidShaderProperties._FluidHeightField, m_activeTerrainheight);
			FluidSimulation.BlitQuad(m_commandBuffer, null, m_splatmap0, m_addTerrainMaterial, m_externalPropertyBlock, pass);
		}

		protected override void SetSplatmap(Vector3 worldPos, Vector2 worldSize, float rotation, Texture source, TerrainLayer layer, SplatChannel splatchannel)
		{
			if (!m_terrainSupportsTerraform || layer > 0 || splatchannel == SplatChannel.None)
			{
				return;
			}

			Vector2 position = m_parentSimulation.WorldSpaceToPaddedUVSpace(worldPos);
			Vector2 uvSize = m_parentSimulation.WorldSizeToUVSize(worldSize);

			m_externalPropertyBlock.Clear();
			m_externalPropertyBlock.SetVector(FluidShaderProperties._BlitScaleBiasRt, new Vector4(uvSize.x, uvSize.y, position.x, position.y));
			m_externalPropertyBlock.SetVector(FluidShaderProperties._BlitRotation, GraphicsHelpers.DegreesToVec2(rotation));
			m_externalPropertyBlock.SetVector(FluidShaderProperties._LayerMask, Vector4.one);
			m_externalPropertyBlock.SetFloat(FluidShaderProperties._IncreaseStrength, 1);

			Vector4 mask = Vector4.zero;
			mask[(int)splatchannel] = 1;
			m_externalPropertyBlock.SetVector(FluidShaderProperties._SplatmapMask, mask);

			m_externalPropertyBlock.SetTexture(FluidShaderProperties._FluidHeightField, m_activeTerrainheight);
			FluidSimulation.BlitQuad(m_commandBuffer, source, m_splatmap0, m_addTerrainMaterial, m_externalPropertyBlock, m_setSplatmapTexturePass);
			FluidSimulation.BlitQuad(m_commandBuffer, source, m_splatmap0, m_addTerrainMaterial, m_externalPropertyBlock, m_setSplatmapTexturePass + 1);
		}

		internal void ApplyTerraform(TerraformModifier modifier, float dt)
		{
			if (!m_terrainSupportsTerraform)
			{
				return;
			}

			m_commandBuffer.Blit(m_splatmap0, m_splatmap1);
			m_commandBuffer.Blit(m_parentSimulation.fluidHeight, m_parentSimulation.nextFluidHeight);
			m_commandBuffer.Blit(m_activeTerrainheight, m_nextTerrainheight);

			TerraformModifier.TerraformModifierSettings settings = modifier.settings;

			Transform modifierTransform = modifier.transform;
			modifier.GetTerraformModifierBlitParams(m_parentSimulation, out Vector2 position, out Vector2 blitRotation, out Vector2 uvSize);

			Vector3 localPosition = modifierTransform.position - m_parentSimulation.transform.position;
			Vector3 localSize = settings.size;
			Quaternion rotation = modifierTransform.rotation;
			Matrix4x4 inverseRotationMatrix = Matrix4x4.Rotate(Quaternion.Inverse(rotation));

			m_externalPropertyBlock.Clear();
			m_externalPropertyBlock.SetVector(FluidShaderProperties._BlitScaleBiasRt, new Vector4(uvSize.x, uvSize.y, position.x, position.y));
			m_externalPropertyBlock.SetVector(FluidShaderProperties._BlitRotation, blitRotation);
			m_externalPropertyBlock.SetVector(FluidShaderProperties._LayerMask, Vector4.one);
			m_externalPropertyBlock.SetFloat(FluidShaderProperties._IncreaseStrength, 1);
			m_externalPropertyBlock.SetFloat(FluidShaderProperties._IncreaseExponent, settings.falloff);

			Vector4 liquidfyFluidMask = new Vector4(settings.liquifyFluidLayer == FluidLayerIndex.Layer1 ? 1.0f : 0.0f,
								settings.liquifyFluidLayer == FluidLayerIndex.Layer2 ? 1.0f : 0.0f,
								settings.liquify ? (settings.liquifyRate * dt) : 0,
								settings.liquifyAmount);

			Vector4 liquidfyTerrainMask = Vector4.zero;
			liquidfyTerrainMask[(int)settings.liquifyTerrainLayer] = 1;

			Vector4 solidifyFluidMask = new Vector4(settings.solidifyFluidLayer == FluidLayerIndex.Layer1 ? 1.0f : 0.0f,
								settings.solidifyFluidLayer == FluidLayerIndex.Layer2 ? 1.0f : 0.0f,
								settings.solidify ? (settings.solidifyRate * dt) : 0,
								settings.solidifyAmount);

			Vector4 solidifyTerrainMask = Vector4.zero;
			solidifyTerrainMask[(int)settings.solidifyTerrainLayer] = 1;

			Vector4 solidifySplatMask = Vector4.zero;
			solidifySplatMask[(int)settings.solidifySplatChannel] = 1;

			m_externalPropertyBlock.SetVector(FluidShaderProperties._LiquifyFluidLayerMask, liquidfyFluidMask);
			m_externalPropertyBlock.SetVector(FluidShaderProperties._LiquifyTerrainLayerMask, liquidfyTerrainMask);

			m_externalPropertyBlock.SetVector(FluidShaderProperties._LiquifyTotalHeightMask, FluidSimulation.LayerToTotalHeightLayerMask((int)settings.liquifyTerrainLayer));
			m_externalPropertyBlock.SetVector(FluidShaderProperties._SolidifyTotalHeightMask, FluidSimulation.LayerToTotalHeightLayerMask((int)settings.solidifyFluidLayer));

			m_externalPropertyBlock.SetVector(FluidShaderProperties._SolidifyFluidLayerMask, solidifyFluidMask);
			m_externalPropertyBlock.SetVector(FluidShaderProperties._SolidifyTerrainLayerMask, solidifyTerrainMask);
			m_externalPropertyBlock.SetVector(FluidShaderProperties._SplatmapMask, solidifySplatMask);
			m_externalPropertyBlock.SetVector(FluidShaderProperties._SplatmapMask, solidifySplatMask);

			// Modifier 3D Properties
			m_externalPropertyBlock.SetVector(FluidShaderProperties._ModifierCenter, localPosition);
			m_externalPropertyBlock.SetVector(FluidShaderProperties._ModifierSize, localSize);
			m_externalPropertyBlock.SetMatrix(FluidShaderProperties._ModifierRotationMatrix, inverseRotationMatrix);
			m_externalPropertyBlock.SetTexture(FluidShaderProperties._FluidHeightField, this.m_parentSimulation.fluidHeight);
			m_externalPropertyBlock.SetTexture(FluidShaderProperties._Splatmap, m_splatmap0);
			m_externalPropertyBlock.SetTexture(FluidShaderProperties._TerrainHeightField, m_activeTerrainheight);


			m_mrt3[0] = this.m_parentSimulation.nextFluidHeight;
			m_mrt3[1] = m_nextTerrainheight;
			m_mrt3[2] = m_splatmap1;
			int pass = GetTerraformModifierPass(settings.mode);
			FluidSimulation.BlitQuad(m_commandBuffer, null, m_mrt3, m_nextTerrainheight.depthBuffer, m_terraformModifierMaterial, m_externalPropertyBlock, pass);

			m_parentSimulation.SwapFluidRT();
			SwapTerrainRT();
			Swap(ref m_splatmap0, ref m_splatmap1);
		}


		private int GetTerraformModifierPass(TerraformModifier.TerraformInputMode mode)
		{
			int pass = 0;
			if (mode == TerraformModifier.TerraformInputMode.Circle)
			{
				pass = m_terraformModifierCirclePass;
			}
			else if (mode == TerraformModifier.TerraformInputMode.Box)
			{
				pass = m_terraformModifierSquarePass;
			}
			else if (mode == TerraformModifier.TerraformInputMode.Sphere)
			{
				pass = m_terraformModifierSpherePass;
			}
			else if (mode == TerraformModifier.TerraformInputMode.Cube)
			{
				pass = m_terraformModifierCubePass;
			}
			else if (mode == TerraformModifier.TerraformInputMode.Cylinder)
			{
				pass = m_terraformModifierCylinderPass;
			}
			else if (mode == TerraformModifier.TerraformInputMode.Capsule)
			{
				pass = m_terraformModifierCapsulePass;
			}

			return pass;
		}
	}
}