using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FluidFrenzy
{
	/// <summary>
	/// The <see cref="TerraformTerrain"/> component is an extension of the <see cref="SimpleTerrain"/> component. It adds an extra splat map that the <see cref="TerraformLayer"/> makes modifications to. 
	/// 
	/// A **splat map** is a graphics technique essential for terrain rendering. It's typically a low-resolution control texture often an RGBA image where each color channel acts as a weight map. The value of a channel dictates the blend strength for a corresponding detail texture (like rock, grass, or dirt), allowing the shader to seamlessly combine multiple material layers across the terrain surface.
	/// 
	/// This splat map is used to represent different terrain layers on the base layer of the terrain. 
	/// It is rendered by the FluidFrenzy/TerraformTerrain shader.
	/// </summary>
	[ExecuteInEditMode]
	public class TerraformTerrain : SimpleTerrain
	{
		/// <summary>
		/// The texture defining the initial material distribution (splatmap) across the terrain surface.
		/// </summary>
		/// <remarks>
		/// The splat map acts as a mask to determine which of the four material layers from the assigned <see cref="terrainMaterial"/> are rendered at any given coordinate.
		/// 
		/// Each channel of the splat map corresponds to a material layer:
		/// <list type="bullet">
		///     <item>Layer 1: Red channel (Primary Material)</item>
		///     <item>Layer 2: Green channel</item>
		///     <item>Layer 3: Blue channel</item>
		///     <item>Layer 4: Alpha channel</item>
		/// </list>
		/// 
		/// **Crucial Constraint:** When using <c>TerraformTerrain</c>, this splat map is only applied to the base (Red channel) physical layer data defined by the <see cref="sourceHeightmap"/>. The other physical layers (G, B, A channels of the heightmap) use their respective material properties directly without being masked by this splat map.
		/// </remarks>
		public Texture2D splatmap;
		[HideInInspector]
		public RenderTexture renderSplatmap = null;

		public override void Start()
		{
			renderSplatmap = new RenderTexture(surfaceProperties.meshResolution.x + 1, surfaceProperties.meshResolution.y + 1, 0, RenderTextureFormat.ARGB32);
			renderSplatmap.filterMode = FilterMode.Bilinear;
			renderSplatmap.wrapMode = TextureWrapMode.Clamp;
			renderSplatmap.name = GraphicsHelpers.AutoRenderTextureName("TerrainTexture");
			renderSplatmap.Create();
			base.Start();
		}

		protected override void InitializeTerrain(Texture source, bool applyOffset = true)
		{
			base.InitializeTerrain(source, applyOffset);
			RenderTexture.active = renderSplatmap;
			GL.Clear(false, true, new Color(1, 0, 0, 0));
			if (splatmap)
			{
				Graphics.Blit(splatmap, renderSplatmap);
			}
		}

		public override void OnDestroy()
        {
			GraphicsHelpers.ReleaseSimulationRT(renderSplatmap);
			base.OnDestroy();
		}

		protected override void LateUpdate()
		{
			base.LateUpdate();
			if (terrainMaterial)
			{
				terrainMaterial.SetTexture(FluidShaderProperties._Splatmap, renderSplatmap);
			}
		}


	}
}