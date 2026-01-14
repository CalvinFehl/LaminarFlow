using System.Runtime.InteropServices;
using Unity.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace FluidFrenzy
{
	/// <summary>
	/// <see cref="MeshSurface"/> uses <see cref="Graphics.RenderMesh"/> or <see cref="Graphics.RenderMeshInstanced"/> to render a height field to each camera.
	/// It has the same functionality as the <see cref="MeshRendererSurface"/> method except it doesn't create any GameObjects,
	/// it is rendered manually and can therefore support GPU Instancing on the <see cref="WaterSurface"/>.
	/// </summary>
	public class MeshSurface : ISurfaceRenderer
    {
		public Mesh mesh { get { return m_surfaceMesh; } set { } }

        private Mesh m_surfaceMesh = null;
        public bool customRender { get { return true; } }

		struct InstanceConstants
		{
			public Vector4 meshUV;
			public Vector4 textureUV;
		}

        NativeArray<Matrix4x4> m_InstanceMatrices;
        NativeArray<Matrix4x4> m_instanceTransforms;
        NativeArray<InstanceConstants> m_uvScaleOffets;
        GraphicsBuffer m_instancedUVConstants;
        private Vector3 m_boundsSize;

        public MeshSurface(ISurfaceRenderer.SurfaceDescriptor desc, Vector2Int meshBlocks)
        {
            int numVertsX = desc.meshResolution.x / meshBlocks.x;
            int numVertsY = desc.meshResolution.y / meshBlocks.y;

            m_boundsSize = new Vector3(desc.dimension.x, desc.maxHeight, desc.dimension.y);
            m_surfaceMesh ??= PrimitiveGenerator.GenerateTerrainMesh(numVertsX, numVertsY, desc.dimension / meshBlocks, Vector2.zero, Vector2.one);

            if (m_surfaceMesh.isReadable)
                m_surfaceMesh.RecalculateBounds();

            Bounds bounds = m_surfaceMesh.bounds;
            bounds.Encapsulate(Vector3.up * desc.maxHeight);
            m_surfaceMesh.bounds = bounds;

            m_instanceTransforms = new NativeArray<Matrix4x4> (meshBlocks.x * meshBlocks.y, Allocator.Persistent,  NativeArrayOptions.UninitializedMemory);
			m_InstanceMatrices = new NativeArray<Matrix4x4> (meshBlocks.x * meshBlocks.y, Allocator.Persistent,  NativeArrayOptions.UninitializedMemory);
            m_uvScaleOffets = new NativeArray<InstanceConstants> (meshBlocks.x * meshBlocks.y, Allocator.Persistent,  NativeArrayOptions.UninitializedMemory);

            Vector2 gridSize = desc.meshResolution + Vector2.one;
			Vector2 chunkScale = Vector2.one - (Vector2.one / meshBlocks);
			Vector2 uvChunkScale = Vector2.one / gridSize * chunkScale;
			Vector2 uvChunkOffset = Vector2.one - (Vector2.one / gridSize);
            for (int y = 0; y < meshBlocks.y; y++)
            {
                for (int x = 0; x < meshBlocks.x; x++)
                {
                    Vector3 pos = new Vector3()
                    {
                        x = (-desc.dimension.x / 2) + desc.dimension.x / 2 / meshBlocks.x,
                        z = (-desc.dimension.y / 2) + desc.dimension.y / 2 / meshBlocks.y
                    };
                    pos.x += (desc.dimension.x / meshBlocks.x) * x;
                    pos.z += (desc.dimension.y / meshBlocks.y) * y;
                    int index = x + y * meshBlocks.x;

					Vector2 textureUVStart = new Vector2((float)x / meshBlocks.x, (float)y / meshBlocks.y);
					Vector2 textureUVScale = new Vector2(1.0f / meshBlocks.x, 1.0f / meshBlocks.y);
					Vector2 meshUVStart = textureUVStart * uvChunkOffset;
					Vector2 meshUVScale = textureUVScale + Vector2.one * uvChunkScale;
					m_InstanceMatrices[index] = Matrix4x4.Translate(pos);
					m_uvScaleOffets[index] = new InstanceConstants()
					{
						meshUV = new Vector4(meshUVStart.x, meshUVStart.y, meshUVScale.x, meshUVScale.y),
						textureUV = new Vector4(textureUVStart.x, textureUVStart.y, textureUVScale.x, textureUVScale.y)
					};
                }
            }
        }

        private void CreateConstantBuffer(Material mat)
		{
            if (!mat || !mat.enableInstancing)
                return;

            if (m_instancedUVConstants == null || !m_instancedUVConstants.IsValid())
            {
                m_instancedUVConstants = new GraphicsBuffer(GraphicsBuffer.Target.Constant, m_uvScaleOffets.Length, Marshal.SizeOf(typeof(InstanceConstants)));
                m_instancedUVConstants.SetData(m_uvScaleOffets);
            }
        }

        private void DisposeData()
        {
			m_InstanceMatrices.Dispose();
            m_instanceTransforms.Dispose();
            m_uvScaleOffets.Dispose();
            if (m_instancedUVConstants != null)
            {
                m_instancedUVConstants.Dispose();
                m_instancedUVConstants = null;
            }
        }

        public void Dispose()
        {
#if UNITY_EDITOR
            AssemblyReloadEvents.beforeAssemblyReload -= DisposeData;
#endif
            GraphicsHelpers.SafeDestroy(m_surfaceMesh);
            DisposeData();
        }
		public void SetupMaterial(Material material)
		{
			material?.DisableKeyword("_FLUIDFRENZY_INSTANCING");
		}

		public void UpdateSurfaceDescriptor(ISurfaceRenderer.SurfaceDescriptor desc)
		{
			m_boundsSize = new Vector3(desc.dimension.x, desc.maxHeight, desc.dimension.y);
		}

		public void OnEnable()
        {
#if UNITY_EDITOR
            AssemblyReloadEvents.beforeAssemblyReload += DisposeData;
#endif
        }

        public void OnDisable()
        {
#if UNITY_EDITOR
            AssemblyReloadEvents.beforeAssemblyReload -= DisposeData;
#endif
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
#if UNITY_2021_1_OR_NEWER
			RenderParams rp = new RenderParams()
            {
                receiveShadows = true,
                shadowCastingMode = ShadowCastingMode.On,
                worldBounds = new Bounds(transform.position, m_boundsSize),
                lightProbeUsage = LightProbeUsage.Off,
                material = material,
                reflectionProbeUsage = ReflectionProbeUsage.BlendProbes,
                layer = transform.gameObject.layer,
                matProps = properties,
                camera = camera,
				renderingLayerMask = uint.MaxValue
            };
            CreateConstantBuffer(material);

            if (material && material.enableInstancing)
            {
                rp.worldBounds = new Bounds(transform.position, m_boundsSize);
                properties.SetConstantBuffer(FluidShaderProperties._UnityInstancing_InstanceProperties, m_instancedUVConstants, 0, m_instancedUVConstants.stride * m_instancedUVConstants.count);

				for (int i = 0; i < m_InstanceMatrices.Length; i++)
				{
					m_instanceTransforms[i] = transform.localToWorldMatrix * m_InstanceMatrices[i];
				}
				Graphics.RenderMeshInstanced(rp, m_surfaceMesh, 0, m_instanceTransforms, m_InstanceMatrices.Length);
            }
            else
            {
                properties.SetConstantBuffer(FluidShaderProperties._UnityInstancing_InstanceProperties, null as GraphicsBuffer, 0, 0);
                for(int i = 0; i < m_InstanceMatrices.Length; i++)
                {
                    properties.SetVector(FluidShaderProperties._MeshUVOffsetScale, m_uvScaleOffets[i].meshUV);
                    properties.SetVector(FluidShaderProperties._TextureUVOffsetScale, m_uvScaleOffets[i].textureUV);
                    rp.worldBounds = new Bounds(m_InstanceMatrices[i].GetPosition(), m_surfaceMesh.bounds.size);
                    Graphics.RenderMesh(rp, m_surfaceMesh, 0, transform.localToWorldMatrix * m_InstanceMatrices[i]);
                }
            }
#else
			CreateConstantBuffer(material);

			if (material.enableInstancing)
			{
				material.SetConstantBuffer(FluidShaderProperties._UnityInstancing_InstanceProperties, m_instancedUVConstants, 0, m_instancedUVConstants.stride * m_instancedUVConstants.count);

				for (int i = 0; i < m_InstanceMatrices.Length; i++)
				{
					m_instanceTransforms[i] = transform.localToWorldMatrix * m_InstanceMatrices[i];
				}
				Graphics.DrawMeshInstanced(m_surfaceMesh, 0, material,
											m_instanceTransforms.ToArray(), m_InstanceMatrices.Length, properties,
											ShadowCastingMode.On, true, transform.gameObject.layer, null);
			}
			else
			{
				properties.SetConstantBuffer(FluidShaderProperties._UnityInstancing_InstanceProperties, null as GraphicsBuffer, 0, 0);
				for (int i = 0; i < m_InstanceMatrices.Length; i++)
				{
					properties.SetVector(FluidShaderProperties._MeshUVOffsetScale, m_uvScaleOffets[i].meshUV);
					properties.SetVector(FluidShaderProperties._TextureUVOffsetScale, m_uvScaleOffets[i].textureUV);
					//rp.worldBounds = new Bounds(m_InstanceMatrices[i].GetPosition(), m_surfaceMesh.bounds.size);
					//Graphics.RenderMesh(rp, m_surfaceMesh, 0, transform.localToWorldMatrix * m_InstanceMatrices[i]);
					Graphics.DrawMesh(m_surfaceMesh, transform.localToWorldMatrix * m_InstanceMatrices[i], material,
						transform.gameObject.layer, null, 0, properties, true, true, true);
				}
			}
#endif
		}

		public void Render(CommandBuffer cmd, Matrix4x4 localToWorld, Material material, MaterialPropertyBlock properties, int pass = 0, Camera camera = null)
		{
			SetupMaterial(material);

			CreateConstantBuffer(material);

			if (material && material.enableInstancing)
			{
				properties.SetConstantBuffer(FluidShaderProperties._UnityInstancing_InstanceProperties, m_instancedUVConstants, 0, m_instancedUVConstants.stride * m_instancedUVConstants.count);

				for (int i = 0; i < m_InstanceMatrices.Length; i++)
				{
					m_instanceTransforms[i] = localToWorld * m_InstanceMatrices[i];
				}
				cmd.DrawMeshInstanced(m_surfaceMesh, 0, material, pass, m_instanceTransforms.ToArray(), m_InstanceMatrices.Length, properties);
			}
			else
			{
				properties.SetConstantBuffer(FluidShaderProperties._UnityInstancing_InstanceProperties, null as GraphicsBuffer, 0, 0);
				for (int i = 0; i < m_InstanceMatrices.Length; i++)
				{
					properties.SetVector(FluidShaderProperties._MeshUVOffsetScale, m_uvScaleOffets[i].meshUV);
					properties.SetVector(FluidShaderProperties._TextureUVOffsetScale, m_uvScaleOffets[i].textureUV);
					cmd.DrawMesh(m_surfaceMesh, localToWorld * m_InstanceMatrices[i], material, 0, pass, properties);
				}
			}
		}
	}
}
