using UnityEngine;
using UnityEngine.Rendering;

#if FLUIDFRENZY_RUNTIME_HDRP_SUPPORT
using UnityEngine.Rendering.HighDefinition;
using HDRPWaterSurface = UnityEngine.Rendering.HighDefinition.WaterSurface;
namespace FluidFrenzy
{
	using HDRPProperties = ISurfaceRenderer.RenderProperties.HDRPWaterSurfaceProperties;
	/// <summary>
	/// Represents a surface renderer that can be instantiated to render heightfields using Unity's <see cref="MeshRenderer"/> components.
	/// The surfaces can be split into multiple blocks to optimize rendering performance as the blocks can be more easily culled by the camera.
	/// When splitting the surface into chunks there is support for rendering using Unity's GPU Instancing functionality available on materials.
	/// </summary>
	public class WaterSurfaceHDRP : ISurfaceRenderer
    {
        public bool customRender { get { return false; } }

		private HDRPWaterSurface m_targetWaterSurface;
		private WaterDecal m_waterDecal;
		private Material m_decalMaterial;
		private FluidSimulation m_simulation;
        public WaterSurfaceHDRP(HDRPProperties hdrpProperties, FluidSimulation simulation)
        {
			m_simulation = simulation;
			hdrpProperties.targetWaterSurface.decalRegionAnchor = hdrpProperties.targetWaterSurface.transform;

			m_targetWaterSurface = hdrpProperties.targetWaterSurface;
			GameObject decalGO = new GameObject("FluidFrenzy-Decal", typeof(WaterDecal));
			decalGO.transform.position = simulation.transform.position;
			m_waterDecal = decalGO.GetComponent<WaterDecal>();
			m_waterDecal.updateMode = CustomRenderTextureUpdateMode.Realtime;
			m_waterDecal.resolution = simulation.settings.numberOfCells;
			m_waterDecal.regionSize = simulation.dimension;
			m_waterDecal.amplitude = hdrpProperties.amplitude;
			m_decalMaterial = Material.Instantiate(Resources.Load<Material>("FluidSampleDecal"));
			m_waterDecal.material = m_decalMaterial;

			m_decalMaterial.SetFloat("_Amplitude", hdrpProperties.amplitude);
			m_decalMaterial.SetFloat("_LargeCurrentWeight", hdrpProperties.largeCurrent);
			m_decalMaterial.SetFloat("_RipplesWeight", hdrpProperties.ripples);
			m_decalMaterial.SetFloat("_BaseFluidHeight", m_targetWaterSurface.transform.position.y);

			m_simulation = simulation;
		}

	
        public void Dispose() 
        {
			Material.Destroy(m_decalMaterial);
			if (m_waterDecal)
			{
				GameObject.Destroy(m_waterDecal.gameObject);
			}
		}

		public void SetupMaterial(Material material)
		{
			m_decalMaterial.SetFloat(FluidShaderProperties._TerrainHeightScale, m_simulation.terrainScale);
			m_decalMaterial.SetVector(FluidShaderProperties._TerrainHeightField_ST, m_simulation.terrainTextureST);
			m_decalMaterial.SetTexture(FluidShaderProperties._TerrainHeightField, m_simulation.terrainHeight);
			m_decalMaterial.SetTexture(FluidShaderProperties._FluidHeightVelocityField, m_simulation.fluidRenderData);
			m_decalMaterial.SetTexture(FluidShaderProperties._FluidNormalField, m_simulation.normalTexture);
			m_decalMaterial.SetFloat(FluidShaderProperties._FluidClipHeight, m_simulation.clipHeight);
			m_decalMaterial.SetVector(FluidShaderProperties._TextureUVOffsetScale, Vector2.one);
			m_decalMaterial.SetVector(FluidShaderProperties._MeshUVOffsetScale, Vector2.one);
			m_decalMaterial.SetFloat("_BaseFluidHeight", m_targetWaterSurface.transform.position.y);

			FoamLayer foamLayer = m_simulation.GetFluidLayer<FoamLayer>();
			if(foamLayer)
			{
				m_decalMaterial.SetTexture(FluidShaderProperties._FluidFoamField, foamLayer.activeLayer);
				m_decalMaterial.SetVector(FluidShaderProperties._FluidFoamField_ST, foamLayer.textureST);
			}
		}

		public void UpdateSurfaceDescriptor(ISurfaceRenderer.SurfaceDescriptor desc)
		{

		}

		public void OnEnable() { }
        public void OnDisable()
        {
            Dispose();
        }
		public void AddCommandBuffers(Camera camera) { }

		public void RemoveCommandBuffers(Camera camera) { }

		public void PreRender(ScriptableRenderContext context, Transform transform, Camera camera, Texture heightmap, int traverseIterations = 1, bool renderShadows = false)
        {
        }		
		
		public void PreRenderCamera(Transform transform, Camera camera, Texture heightmap, int traverseIterations = 1)
        {
        }

        public void CullShadowLight(CommandBuffer commandBuffer, Transform transform, Camera camera, Light light)
        {
        }

        public void Render(Transform transform, Material material, MaterialPropertyBlock properties, Camera camera = null)
        {

        }

		public void Render(CommandBuffer cmd, Matrix4x4 localToWorld, Material material, MaterialPropertyBlock properties, int pass = 0, Camera camera = null)
		{
		}
	}
}
#endif
