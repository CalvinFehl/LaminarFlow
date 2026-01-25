using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using FluidModifierSpace = FluidFrenzy.FluidSimulation.FluidModifierSpace;

namespace FluidFrenzy
{
	public partial class ErosionLayer : FluidLayer
	{
		private void UpdateTerrainModifiers(FluidSimulation simulation, float dt)
		{
			if (!m_platformSupportsFloat32Blend)
			{
				m_commandBuffer.SetRenderTarget(m_dynamicInput);
				m_commandBuffer.ClearRenderTarget(false, true, Color.clear);
			}
			foreach (TerrainModifier input in TerrainModifier.terrainModifiers)
			{
				if (input.isActiveAndEnabled)
				{
					input.Process(this, dt);
				}
			}
		}

		public virtual void AddTerrainCircle(Vector3 worldPos, Vector2 worldSize, float rotation, float strength, float falloff, TerrainLayer layer, SplatChannel splatchannel, float dt)
		{
			AddTerrain(worldPos, worldSize, rotation, strength, falloff, layer, splatchannel, dt, m_addTerrainCirclePass);
		}

		public virtual void AddTerrainSquare(Vector3 worldPos, Vector2 worldSize, float rotation, float strength, float falloff, TerrainLayer layer, SplatChannel splatchannel, float dt)
		{
			AddTerrain(worldPos, worldSize, rotation, strength, falloff, layer, splatchannel, dt, m_addTerrainSquarePass);
		}

		protected virtual void AddTerrain(Vector3 worldPos, Vector2 worldSize, float rotation, float strength, float falloff, TerrainLayer layer, SplatChannel splatchannel, float dt, int pass)
		{
			if (!m_terrainSupportsTerraform)
			{
				return;
			}
			Vector2 position = m_parentSimulation.WorldSpaceToPaddedUVSpace(worldPos);
			Vector2 uvSize = m_parentSimulation.WorldSizeToUVSize(worldSize);

			m_externalPropertyBlock.Clear();
			m_externalPropertyBlock.SetVector(FluidShaderProperties._BlitScaleBiasRt, new Vector4(uvSize.x, uvSize.y, position.x, position.y));
			m_externalPropertyBlock.SetVector(FluidShaderProperties._BlitRotation, GraphicsHelpers.DegreesToVec2(rotation));
			m_externalPropertyBlock.SetFloat(FluidShaderProperties._IncreaseStrength, strength * dt);
			m_externalPropertyBlock.SetFloat(FluidShaderProperties._IncreaseExponent, falloff);
			m_externalPropertyBlock.SetVector(FluidShaderProperties._LayerMask, FluidSimulation.LayerToLayerMask((int)layer));

			if (!m_platformSupportsFloat32Blend)
				m_externalPropertyBlock.SetTexture(FluidShaderProperties._FluidHeightField, m_dynamicInput);
			FluidSimulation.BlitQuad(m_commandBuffer, null, m_platformSupportsFloat32Blend ? m_activeTerrainheight : m_dynamicInput, m_addTerrainMaterial, m_externalPropertyBlock, pass);
		}

		public virtual void AddTerrainTexture(Vector3 worldPos, Vector2 worldSize, float rotation, Texture source, float strength, Vector2 remap, TerrainLayer layer, SplatChannel splatchannel, float dt)
		{
			AddTerrain(m_commandBuffer, m_platformSupportsFloat32Blend ? m_activeTerrainheight : m_dynamicInput, worldPos, worldSize, rotation, source, strength, remap, layer, dt);
		}

		private void AddTerrain(CommandBuffer commandBuffer, RenderTexture dest, Vector3 worldPos, Vector2 worldSize, float rotation, Texture source, float strength, Vector2 remap, TerrainLayer layer, float dt)
		{
			if (!m_terrainSupportsTerraform)
			{
				return;
			}
			using (new ProfilingScope(commandBuffer, ProfilingSampler.Get(WaterSimProfileID.AddWater)))
			{
				Vector2 position = m_parentSimulation.WorldSpaceToPaddedUVSpace(worldPos);
				Vector2 uvSize = m_parentSimulation.WorldSizeToUVSize(worldSize);

				m_externalPropertyBlock.Clear();
				m_externalPropertyBlock.SetVector(FluidShaderProperties._BlitScaleBiasRt, new Vector4(uvSize.x, uvSize.y, position.x, position.y));
				m_externalPropertyBlock.SetVector(FluidShaderProperties._BlitRotation, GraphicsHelpers.DegreesToVec2(rotation));
				m_externalPropertyBlock.SetFloat(FluidShaderProperties._IncreaseStrength, strength * dt);
				m_externalPropertyBlock.SetVector(FluidShaderProperties._LayerMask, FluidSimulation.LayerToLayerMask((int)layer));
				m_externalPropertyBlock.SetVector(FluidShaderProperties._RemapRange, remap);
				FluidSimulation.BlitQuad(commandBuffer, source, dest, m_addTerrainMaterial, m_externalPropertyBlock, m_addTerrainTexturePass);
			}
		}

		public virtual void SetTerrainHeightCircle(Vector3 worldPos, Vector2 worldSize, float rotation, float height, float falloff, TerrainLayer layer, SplatChannel splatchannel, FluidModifierSpace space)
		{
			SetTerrainHeight(m_commandBuffer, worldPos, worldSize, rotation, height, falloff, layer, splatchannel, space, BlendOp.Add, m_mixTerrainHeightCirclePass);
			SetSplatmap(worldPos, worldSize, rotation, layer, splatchannel, m_setSplatmapCirclePass);
		}

		public virtual void SetTerrainDepthCircle(Vector3 worldPos, Vector2 worldSize, float rotation, float height, float falloff, TerrainLayer layer, SplatChannel splatchannel, FluidModifierSpace space)
		{
			SetTerrainHeight(m_commandBuffer, worldPos, worldSize, rotation, height, falloff, layer, splatchannel, space, BlendOp.Add, m_mixTerrainDepthCirclePass);
			SetSplatmap(worldPos, worldSize, rotation, layer, splatchannel, m_setSplatmapCirclePass);
		}

		public virtual void SetTerrainHeightSquare(Vector3 worldPos, Vector2 worldSize, float rotation, float height, float falloff, TerrainLayer layer, SplatChannel splatchannel, FluidModifierSpace space)
		{
			SetTerrainHeight(m_commandBuffer, worldPos, worldSize, rotation, height, falloff, layer, splatchannel, space, BlendOp.Add, m_mixTerrainHeightSquarePass);
			SetSplatmap(worldPos, worldSize, rotation, layer, splatchannel, m_setSplatmapSquarePass);
		}

		public virtual void SetTerrainDepthSquare(Vector3 worldPos, Vector2 worldSize, float rotation, float height, float falloff, TerrainLayer layer, SplatChannel splatchannel, FluidModifierSpace space)
		{
			SetTerrainHeight(m_commandBuffer, worldPos, worldSize, rotation, height, falloff, layer, splatchannel, space, BlendOp.Add, m_mixTerrainDepthSquarePass);
			SetSplatmap(worldPos, worldSize, rotation, layer, splatchannel, m_setSplatmapSquarePass);
		}

		public virtual void MinTerrainHeightCircle(Vector3 worldPos, Vector2 worldSize, float rotation, float height, float falloff, TerrainLayer layer, SplatChannel splatchannel, FluidModifierSpace space)
		{
			SetTerrainHeight(m_commandBuffer, worldPos, worldSize, rotation, height, falloff, layer, splatchannel, space, BlendOp.Min, m_mixTerrainHeightCirclePass);
			SetSplatmap(worldPos, worldSize, rotation, layer, splatchannel, m_setSplatmapCirclePass);
		}

		public virtual void MinTerrainDepthCircle(Vector3 worldPos, Vector2 worldSize, float rotation, float height, float falloff, TerrainLayer layer, SplatChannel splatchannel, FluidModifierSpace space)
		{
			SetTerrainHeight(m_commandBuffer, worldPos, worldSize, rotation, height, falloff, layer, splatchannel, space, BlendOp.Min, m_mixTerrainDepthCirclePass);
			SetSplatmap(worldPos, worldSize, rotation, layer, splatchannel, m_setSplatmapCirclePass);
		}

		public virtual void MinTerrainHeightSquare(Vector3 worldPos, Vector2 worldSize, float rotation, float height, float falloff, TerrainLayer layer, SplatChannel splatchannel, FluidModifierSpace space)
		{
			SetTerrainHeight(m_commandBuffer, worldPos, worldSize, rotation, height, falloff, layer, splatchannel, space, BlendOp.Min, m_mixTerrainHeightSquarePass);
			SetSplatmap(worldPos, worldSize, rotation, layer, splatchannel, m_setSplatmapSquarePass);
		}

		public virtual void MinTerrainDepthSquare(Vector3 worldPos, Vector2 worldSize, float rotation, float height, float falloff, TerrainLayer layer, SplatChannel splatchannel, FluidModifierSpace space)
		{
			SetTerrainHeight(m_commandBuffer, worldPos, worldSize, rotation, height, falloff, layer, splatchannel, space, BlendOp.Min, m_mixTerrainDepthSquarePass);
			SetSplatmap(worldPos, worldSize, rotation, layer, splatchannel, m_setSplatmapSquarePass);
		}

		public virtual void MaxTerrainHeightCircle(Vector3 worldPos, Vector2 worldSize, float rotation, float height, float falloff, TerrainLayer layer, SplatChannel splatchannel, FluidModifierSpace space)
		{
			SetTerrainHeight(m_commandBuffer, worldPos, worldSize, rotation, height, falloff, layer, splatchannel, space, BlendOp.Max, m_mixTerrainHeightCirclePass);
			SetSplatmap(worldPos, worldSize, rotation, layer, splatchannel, m_setSplatmapCirclePass);
		}

		public virtual void MaxTerrainDepthCircle(Vector3 worldPos, Vector2 worldSize, float rotation, float height, float falloff, TerrainLayer layer, SplatChannel splatchannel, FluidModifierSpace space)
		{
			SetTerrainHeight(m_commandBuffer, worldPos, worldSize, rotation, height, falloff, layer, splatchannel, space, BlendOp.Max, m_mixTerrainDepthCirclePass);
			SetSplatmap(worldPos, worldSize, rotation, layer, splatchannel, m_setSplatmapCirclePass);
		}

		public virtual void MaxTerrainHeightSquare(Vector3 worldPos, Vector2 worldSize, float rotation, float height, float falloff, TerrainLayer layer, SplatChannel splatchannel, FluidModifierSpace space)
		{
			SetTerrainHeight(m_commandBuffer, worldPos, worldSize, rotation, height, falloff, layer, splatchannel, space, BlendOp.Max, m_mixTerrainHeightSquarePass);
			SetSplatmap(worldPos, worldSize, rotation, layer, splatchannel, m_setSplatmapSquarePass);
		}

		public virtual void MaxTerrainDepthSquare(Vector3 worldPos, Vector2 worldSize, float rotation, float height, float falloff, TerrainLayer layer, SplatChannel splatchannel, FluidModifierSpace space)
		{
			SetTerrainHeight(m_commandBuffer, worldPos, worldSize, rotation, height, falloff, layer, splatchannel, space, BlendOp.Max, m_mixTerrainDepthSquarePass);
			SetSplatmap(worldPos, worldSize, rotation, layer, splatchannel, m_setSplatmapSquarePass);
		}

		protected virtual void SetTerrainHeight(CommandBuffer commandBuffer, Vector3 worldPos, Vector2 worldSize, float rotation, float height, float falloff, TerrainLayer layer, SplatChannel splatchannel, FluidModifierSpace space, BlendOp blendOp, int pass)
		{
			if (!m_terrainSupportsTerraform)
			{
				return;
			}

			Vector2 position = m_parentSimulation.WorldSpaceToPaddedUVSpace(worldPos);
			Vector2 uvSize = m_parentSimulation.WorldSizeToUVSize(worldSize);

			float yOffset = space == FluidModifierSpace.WorldHeight ? m_parentSimulation.transform.position.y : 0;
			float amount = height;
			m_externalPropertyBlock.Clear();
			m_externalPropertyBlock.SetVector(FluidShaderProperties._BlitScaleBiasRt, new Vector4(uvSize.x, uvSize.y, position.x, position.y));
			m_externalPropertyBlock.SetVector(FluidShaderProperties._BlitRotation, GraphicsHelpers.DegreesToVec2(rotation));

			m_externalPropertyBlock.SetVector(FluidShaderProperties._LayerMask, FluidSimulation.LayerToLayerMask((int)layer));
			m_externalPropertyBlock.SetVector(FluidShaderProperties._BottomLayersMask, FluidSimulation.LayerToBottomLayersMask((int)layer));
			m_externalPropertyBlock.SetFloat(FluidShaderProperties._IncreaseStrength, amount - yOffset);
			m_externalPropertyBlock.SetFloat(FluidShaderProperties._IncreaseExponent, falloff);
			m_externalPropertyBlock.SetFloat(FluidShaderProperties._FallbackBlendOp, (float)blendOp);

			// Because we use blendmode, this is flipped, if we dont use blendmode its not flipped
			RenderTexture current = m_nextTerrainheight;
			RenderTexture dest = m_activeTerrainheight;
			if (!m_platformSupportsFloat32Blend)
			{
				current = m_activeTerrainheight;
				dest = m_nextTerrainheight;
				blendOp = BlendOp.Add;
				//This is not too early of  swap. Might as well swap here because the references for current/dest are already correct. Save another branch.
				SwapTerrainRT();
			}

			m_externalPropertyBlock.SetTexture(FluidShaderProperties._TerrainHeightField, current);
			commandBuffer.SetGlobalInt(FluidShaderProperties._ColorMaskTerrain, FluidSimulation.LayerToColorMask((int)layer));
			commandBuffer.SetGlobalInt(FluidShaderProperties._BlendOpTerrain, (int)blendOp);
			FluidSimulation.BlitQuad(commandBuffer, null, dest, m_addTerrainMaterial, m_externalPropertyBlock, pass);
		}

		protected virtual void SetSplatmap(Vector3 worldPos, Vector2 size, float rotation, TerrainLayer layer, SplatChannel splatchannel, int pass)	{ }

		public virtual void SetTerrainHeightTexture(Vector3 worldPos, Vector2 worldSize, float rotation, Texture source, float height, Vector2 remap, TerrainLayer layer, SplatChannel splatchannel, FluidModifierSpace space)
		{
			SetTerrainHeight(m_commandBuffer, source, worldPos, worldSize, rotation, height, remap, layer, splatchannel, space, BlendOp.Add, m_mixTerrainHeightTexturePass);
			SetSplatmap(worldPos, worldSize, rotation, source, layer, splatchannel);
		}

		public virtual void SetTerrainDepthTexture(Vector3 worldPos, Vector2 worldSize, float rotation, Texture source, float height, Vector2 remap, TerrainLayer layer, SplatChannel splatchannel, FluidModifierSpace space)
		{
			SetTerrainHeight(m_commandBuffer, source, worldPos, worldSize, rotation, height, remap, layer, splatchannel, space, BlendOp.Add, m_mixTerrainDepthTexturePass);
			SetSplatmap(worldPos, worldSize, rotation, source, layer, splatchannel);
		}

		public virtual void MinTerrainHeightTexture(Vector3 worldPos, Vector2 worldSize, float rotation, Texture source, float height, Vector2 remap, TerrainLayer layer, SplatChannel splatchannel, FluidModifierSpace space)
		{
			SetTerrainHeight(m_commandBuffer, source, worldPos, worldSize, rotation, height, remap, layer, splatchannel, space, BlendOp.Min, m_mixTerrainHeightTexturePass);
			SetSplatmap(worldPos, worldSize, rotation, source, layer, splatchannel);
		}

		public virtual void MinTerrainDepthTexture(Vector3 worldPos, Vector2 worldSize, float rotation, Texture source, float height, Vector2 remap, TerrainLayer layer, SplatChannel splatchannel, FluidModifierSpace space)
		{
			SetTerrainHeight(m_commandBuffer, source, worldPos, worldSize, rotation, height, remap, layer, splatchannel, space, BlendOp.Min, m_mixTerrainDepthTexturePass);
			SetSplatmap(worldPos, worldSize, rotation, source, layer, splatchannel);
		}

		public virtual void MaxTerrainHeightTexture(Vector3 worldPos, Vector2 worldSize, float rotation, Texture source, float height, Vector2 remap, TerrainLayer layer, SplatChannel splatchannel, FluidModifierSpace space)
		{
			SetTerrainHeight(m_commandBuffer, source, worldPos, worldSize, rotation, height, remap, layer, splatchannel, space, BlendOp.Max, m_mixTerrainHeightTexturePass);
			SetSplatmap(worldPos, worldSize, rotation, source, layer, splatchannel);
		}

		public virtual void MaxTerrainDepthTexture(Vector3 worldPos, Vector2 worldSize, float rotation, Texture source, float height, Vector2 remap, TerrainLayer layer, SplatChannel splatchannel, FluidModifierSpace space)
		{
			SetTerrainHeight(m_commandBuffer, source, worldPos, worldSize, rotation, height, remap, layer, splatchannel, space, BlendOp.Max, m_mixTerrainDepthTexturePass);
			SetSplatmap(worldPos, worldSize, rotation, source, layer, splatchannel);
		}

		protected void SetTerrainHeight(CommandBuffer commandBuffer, Texture source, Vector3 worldPos, Vector2 worldSize, float rotation, float height, Vector2 remap, TerrainLayer layer, SplatChannel splatchannel, FluidModifierSpace space, BlendOp blendOp, int pass)
		{
			if (!m_terrainSupportsTerraform)
			{
				return;
			}
			using (new ProfilingScope(commandBuffer, ProfilingSampler.Get(WaterSimProfileID.AddWater)))
			{
				Vector2 position = m_parentSimulation.WorldSpaceToPaddedUVSpace(worldPos);
				Vector2 uvSize = m_parentSimulation.WorldSizeToUVSize(worldSize);

				float yOffset = space == FluidModifierSpace.WorldHeight ? m_parentSimulation.transform.position.y : 0;
				float amount = height;
				m_externalPropertyBlock.Clear();
				m_externalPropertyBlock.SetVector(FluidShaderProperties._BlitScaleBiasRt, new Vector4(uvSize.x, uvSize.y, position.x, position.y));
				m_externalPropertyBlock.SetVector(FluidShaderProperties._BlitRotation, GraphicsHelpers.DegreesToVec2(rotation));
				m_externalPropertyBlock.SetVector(FluidShaderProperties._LayerMask, FluidSimulation.LayerToLayerMask((int)layer));
				m_externalPropertyBlock.SetVector(FluidShaderProperties._BottomLayersMask, FluidSimulation.LayerToBottomLayersMask((int)layer));
				m_externalPropertyBlock.SetVector(FluidShaderProperties._RemapRange, remap);
				m_externalPropertyBlock.SetFloat(FluidShaderProperties._IncreaseStrength, amount - yOffset);
				m_externalPropertyBlock.SetFloat(FluidShaderProperties._FallbackBlendOp, (float)blendOp);

				// Because we use blendmode, this is flipped, if we dont use blendmode its not flipped
				RenderTexture current = m_nextTerrainheight;
				RenderTexture dest = m_activeTerrainheight;
				if (!m_platformSupportsFloat32Blend)
				{
					current = m_activeTerrainheight;
					dest = m_nextTerrainheight;
					blendOp = BlendOp.Add;
					//This is not too early of  swap. Might as well swap here because the references for current/dest are already correct. Save another branch.
					SwapTerrainRT();
				}

				m_externalPropertyBlock.SetTexture(FluidShaderProperties._TerrainHeightField, current);
				commandBuffer.SetGlobalInt(FluidShaderProperties._ColorMaskTerrain, FluidSimulation.LayerToColorMask((int)layer));
				commandBuffer.SetGlobalInt(FluidShaderProperties._BlendOpTerrain, (int)blendOp);
				FluidSimulation.BlitQuad(commandBuffer, source, dest, m_addTerrainMaterial, m_externalPropertyBlock, pass);
			}
		}


		protected virtual void SetSplatmap(Vector3 worldPos, Vector2 worldSize, float rotation, Texture source, TerrainLayer layer, SplatChannel splatchannel) { }

		public void MaxTerrain(Vector3 worldPos, Vector2 worldSize, Texture source, float strength, TerrainLayer layer)
		{
			MaxTerrain(m_commandBuffer, worldPos, worldSize, source, strength, layer, BlendOp.Max);
		}

		private void MaxTerrain(CommandBuffer commandBuffer, Vector3 worldPos, Vector2 worldSize, Texture source, float strength, TerrainLayer layer, BlendOp blendOp)
		{
			if (!m_terrainSupportsTerraform)
			{
				return;
			}
			using (new ProfilingScope(commandBuffer, ProfilingSampler.Get(WaterSimProfileID.AddWater)))
			{
				Vector2 position = m_parentSimulation.WorldSpaceToPaddedUVSpace(worldPos);
				Vector2 uvSize = m_parentSimulation.WorldSizeToUVSize(worldSize);

				m_externalPropertyBlock.Clear();
				m_externalPropertyBlock.SetFloat(FluidShaderProperties._IncreaseStrength, strength);
				m_externalPropertyBlock.SetVector(FluidShaderProperties._BlitScaleBiasRt, new Vector4(uvSize.x, uvSize.y, position.x, position.y));
				m_externalPropertyBlock.SetVector(FluidShaderProperties._BlitRotation, Vector2.up);
				m_externalPropertyBlock.SetVector(FluidShaderProperties._LayerMask, FluidSimulation.LayerToLayerMask((int)layer));
				m_externalPropertyBlock.SetVector(FluidShaderProperties._BottomLayersMask, FluidSimulation.LayerToBottomLayersMask((int)layer));
				m_externalPropertyBlock.SetVector(FluidShaderProperties._RemapRange, Vector2.up);
				m_externalPropertyBlock.SetFloat(FluidShaderProperties._FallbackBlendOp, (float)blendOp);

				// Because we use blendmode, this is flipped, if we dont use blendmode its not flipped
				RenderTexture current = m_nextTerrainheight;
				RenderTexture dest = m_activeTerrainheight;
				if (!m_platformSupportsFloat32Blend)
				{
					current = m_activeTerrainheight;
					dest = m_nextTerrainheight;
					blendOp = BlendOp.Add;
					//This is not too early of  swap. Might as well swap here because the references for current/dest are already correct. Save another branch.
					SwapTerrainRT();
				}

				m_externalPropertyBlock.SetTexture(FluidShaderProperties._TerrainHeightField, current);

				commandBuffer.SetGlobalInt(FluidShaderProperties._ColorMaskTerrain, FluidSimulation.LayerToColorMask((int)layer));
				commandBuffer.SetGlobalInt(FluidShaderProperties._BlendOpTerrain, (int)blendOp);
				FluidSimulation.BlitQuad(commandBuffer, source, dest, m_addTerrainMaterial, m_externalPropertyBlock, m_mixTerrainHeightTexturePass);
			}
		}
	}
}