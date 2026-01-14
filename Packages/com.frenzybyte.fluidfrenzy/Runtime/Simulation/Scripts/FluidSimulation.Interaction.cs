using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace FluidFrenzy
{
	public partial class FluidSimulation : MonoBehaviour
	{
		public struct FluidObstacleData
		{
			public Vector3 position;
			public Quaternion rotation;
			public FluidSimulationObstacle.ObstacleShape shape;
			public bool smooth;

			public Vector3 boxSize; // Box, Wedge, Ellipsoid
			public float radius;    // Primary Radius
			public float radius2;   // Secondary Radius
			public float height;    // Height
		}

		public enum FluidLayerIndex
		{
			Layer1 = 1,
			Layer2 = 2,
			None = 0
		}

		static private int[] s_LayerToColorMask = { (int)ColorWriteMask.Red, (int)ColorWriteMask.Green, (int)ColorWriteMask.Blue, (int)ColorWriteMask.Alpha };
		static private Vector4[] s_LayerToLayerMask = { new Vector4(1, 0, 0, 0), new Vector4(0, 1, 0, 0), new Vector4(0, 0, 1, 0), new Vector4(0, 0, 0, 1) };

		private static readonly Vector4[] s_BottomLayerMask = new Vector4[]
		{
				new Vector4(0, 0, 0, 0),
				new Vector4(1, 0, 0, 0),
				new Vector4(1, 1, 0, 0),
				new Vector4(1, 1, 1, 0)
		};

		private static readonly Vector4[] s_TotalHeightLayerMask = new Vector4[]
		{
				new Vector4(1, 0, 0, 0),
				new Vector4(1, 1, 0, 0),
				new Vector4(1, 1, 1, 0),
				new Vector4(1, 1, 1, 1)
		};

		public static int LayerToColorMask(int layer)
		{
			return s_LayerToColorMask[layer];
		}

		public static Vector4 LayerToLayerMask(int layer)
		{
			return s_LayerToLayerMask[layer];
		}		
		
		public static Vector4 LayerToBottomLayersMask(int layer)
		{
			return s_BottomLayerMask[layer];
		}		
		public static Vector4 LayerToTotalHeightLayerMask(int layer)
		{
			return s_TotalHeightLayerMask[layer];
		}

		internal static Vector4 LayersToTopLayerMask(int layers)
		{
			Vector4 addVec = (new Vector4(Mathf.Abs(layers - 1), Mathf.Abs(layers - 2), Mathf.Abs(layers - 3), Mathf.Abs(layers - 4)));
			return Vector4.one - Vector4.Min(Vector4.one, addVec);
		}

		public enum FluidModifierBlendMode
		{
			Set,
			Additive,
			Minimum,
			Maximum,
			Dampen
		}

		public enum FluidModifierSpace
		{
			/// <summary>
			/// Apply the fluidmodifier height in local space relative to the height of the terrain, starting from the terrain. This can be seen as a fluid Depth. 
			/// </summary>
			LocalHeight,

			/// <summary>
			/// Apply the fluidmodifier height in world space relative to the height in the world, starting 0. This can be seen as a total height of the fluid (Terrain Height + Fluid Depth/Amount). 
			/// </summary>
			WorldHeight,
		}
		public Vector2 WorldSpaceToUVSpace(Vector3 worldPos)
		{
			return WorldSpaceToUVSpace(worldPos, bounds, dimension);
		}

		public Vector2 WorldSpaceToPaddedUVSpace(Vector3 worldPos)
		{
			Vector2 uvPos = WorldSpaceToUVSpace(worldPos, bounds, dimension);
			uvPos.x *= m_paddingST.x; uvPos.y *= m_paddingST.y;
			uvPos.x += m_paddingST.z; uvPos.y += m_paddingST.w;
			return uvPos;
		}

		public Vector2 WorldSpaceToPaddedVelocityUVSpace(Vector3 worldPos)
		{
			Vector2 uvPos = WorldSpaceToUVSpace(worldPos, bounds, dimension);
			uvPos.x *= m_velocityTextureST.x; uvPos.y *= m_velocityTextureST.y;
			uvPos.x += m_velocityTextureST.z; uvPos.y += m_velocityTextureST.w;
			return uvPos;
		}

		public static Vector2 WorldSpaceToUVSpace(Vector3 worldPos, Bounds bounds, Vector2 dimension)
		{
			float u = (worldPos.x - bounds.center.x) / dimension.x + 0.5f;
			float v = (worldPos.z - bounds.center.z) / dimension.y + 0.5f;

			return new Vector2(u, v);
		}		
		
		public static Vector2 WorldSizeToUVSize(Vector2 size, Vector2 dimension)
		{
			return size / dimension;
		}

		private float WorldRadiusToUVRadius(float radius)
		{
			return radius / dimension.x;
		}

		public Vector2 WorldSizeToUVSize(Vector2 size)
		{
			return WorldSizeToUVSize(size, dimension);
		}


		public void BlitQuadExternal(Texture src, RenderTexture dst, Material mat, MaterialPropertyBlock properties, int pass = 0)
		{
			if (src != null)
				properties.SetTexture(FluidShaderProperties._MainTex, src);
			m_dynamicCommandBuffer.SetRenderTarget(dst);
			m_dynamicCommandBuffer.DrawProcedural(Matrix4x4.identity, mat, pass, MeshTopology.Quads, 4, 1, properties);
		}

		public void BlitQuadExternal(Texture src, RenderTargetIdentifier dst, Material mat, MaterialPropertyBlock properties, int pass = 0)
		{
			if (src != null)
				properties.SetTexture(FluidShaderProperties._MainTex, src);
			m_dynamicCommandBuffer.SetRenderTarget(dst);
			m_dynamicCommandBuffer.DrawProcedural(Matrix4x4.identity, mat, pass, MeshTopology.Quads, 4, 1, properties);
		}

		/// <summary>
		/// Applies a velocity to the <see cref="FluidSimulation"/> at a specified world position.
		/// </summary>
		/// <param name="worldPos">The world position where the flow is applied.</param>
		/// <param name="direction">The direction of the flow.</param>
		/// <param name="size">The size of the flow area.</param>
		/// <param name="strength">The strength of the flow effect.</param>
		/// <param name="falloff">The falloff used to control the gradient of the flow effect.</param>
		/// <param name="timestep">The time delta since the last update.</param>

		public void ApplyFlow(Vector3 worldPos, Vector2 direction, Vector2 size, float strength, float falloff, float timestep)
		{
			ApplyFlow(worldPos, direction, size, strength, falloff, timestep, m_applyVelocityDirection);
		}

		/// <summary>
		/// Set a velocity to the <see cref="FluidSimulation"/> at a specified world position.
		/// </summary>
		/// <param name="worldPos">The world position where the flow is applied.</param>
		/// <param name="direction">The direction of the flow.</param>
		/// <param name="size">The size of the flow area.</param>
		/// <param name="strength">The strength of the flow effect.</param>
		/// <param name="falloff">The falloff used to control the gradient of the flow effect.</param>

		public void SetFlow(Vector3 worldPos, Vector2 direction, Vector2 size, float strength, float falloff)
		{
			ApplyFlow(worldPos, direction, size, strength, falloff, 1, m_setVelocityDirection);
		}

		protected virtual void BindSolidToFluidShared(int kernel, Matrix4x4 localToWorld, Matrix4x4 prevLocalToWorld, FluidRigidBody.FluidDisplacementProfile displacementProfile)
		{
			m_dynamicCommandBuffer.SetComputeTextureParam(m_solidToFluidCS, kernel, FluidShaderProperties._TerrainHeightField, m_terrainHeight);
			m_dynamicCommandBuffer.SetComputeTextureParam(m_solidToFluidCS, kernel, FluidShaderProperties._FluidHeightField, m_activeWaterHeight);

			m_dynamicCommandBuffer.SetComputeBufferParam(m_solidToFluidCS, kernel, FluidShaderProperties._HeightAccumulator, m_solidToFluidHeightDelta);
			m_dynamicCommandBuffer.SetComputeBufferParam(m_solidToFluidCS, kernel, FluidShaderProperties._VelocityAccumulator, m_solidToFluidVelocityDelta);

			m_dynamicCommandBuffer.SetComputeMatrixParam(m_solidToFluidCS, FluidShaderProperties._LocalToWorld, transform.worldToLocalMatrix * localToWorld);
			m_dynamicCommandBuffer.SetComputeMatrixParam(m_solidToFluidCS, FluidShaderProperties._PrevLocalToWorld, transform.worldToLocalMatrix * prevLocalToWorld);

			m_dynamicCommandBuffer.SetComputeFloatParam(m_solidToFluidCS, FluidShaderProperties._HeightInfluence, displacementProfile.heightInfluence);
			m_dynamicCommandBuffer.SetComputeFloatParam(m_solidToFluidCS, FluidShaderProperties._VelocityInfluence, displacementProfile.velocityInfluence);
			m_dynamicCommandBuffer.SetComputeFloatParam(m_solidToFluidCS, FluidShaderProperties._SolidToFluidScale, displacementProfile.velocityScale);
			m_dynamicCommandBuffer.SetComputeIntParam(m_solidToFluidCS, FluidShaderProperties._BufferWidth, m_activeWaterHeight.width);
		}

		protected virtual void UpdateSolidToFluid() { }

		internal virtual void ApplySolidToFluid_Mesh(GraphicsBuffer vertexBuffer, Matrix4x4 localToWorld, Matrix4x4 prevLocalToWorld, FluidRigidBody.FluidDisplacementProfile displacementProfile)
		{
			if (localToWorld == prevLocalToWorld)
			{
				return;
			}
			BindSolidToFluidShared(m_solidToFluidMeshKernel, localToWorld, prevLocalToWorld, displacementProfile);

			int count = vertexBuffer.count / 3;
			m_dynamicCommandBuffer.SetComputeBufferParam(m_solidToFluidCS, m_solidToFluidMeshKernel, FluidShaderProperties._Vertices, vertexBuffer);
			m_dynamicCommandBuffer.SetComputeIntParam(m_solidToFluidCS, FluidShaderProperties._NumTriangles, count);

			m_solidToFluidCS.GetKernelThreadGroupSizes(m_solidToFluidMeshKernel, out uint x, out _, out _);
			m_dynamicCommandBuffer.DispatchCompute(m_solidToFluidCS, m_solidToFluidMeshKernel, GraphicsHelpers.DCS(count, x), 1, 1);
		}


		internal virtual void ApplySolidToFluid_Sphere(float radius, float area, int numberOfPoints, Matrix4x4 localToWorld, Matrix4x4 prevLocalToWorld, FluidRigidBody.FluidDisplacementProfile displacementProfile)
		{
			if (localToWorld == prevLocalToWorld)
			{
				return;
			}

			BindSolidToFluidShared(m_solidToFluidSphereKernel, localToWorld, prevLocalToWorld, displacementProfile);

			m_dynamicCommandBuffer.SetComputeFloatParam(m_solidToFluidCS, FluidShaderProperties._SphereRadius, radius);
			m_dynamicCommandBuffer.SetComputeFloatParam(m_solidToFluidCS, FluidShaderProperties._PrimitiveArea, area);
			m_dynamicCommandBuffer.SetComputeIntParam(m_solidToFluidCS, FluidShaderProperties._PointCount, numberOfPoints);

			m_solidToFluidCS.GetKernelThreadGroupSizes(m_solidToFluidSphereKernel, out uint x, out _, out _);
			m_dynamicCommandBuffer.DispatchCompute(m_solidToFluidCS, m_solidToFluidSphereKernel, GraphicsHelpers.DCS(numberOfPoints, x), 1, 1);
		}

		internal virtual void ApplySolidToFluid_Box(Vector3 size, int numberOfPoints, Matrix4x4 localToWorld, Matrix4x4 prevLocalToWorld, FluidRigidBody.FluidDisplacementProfile displacementProfile)
		{
			if (localToWorld == prevLocalToWorld)
			{
				return;
			}
			BindSolidToFluidShared(m_solidToFluidBoxKernel, localToWorld, prevLocalToWorld, displacementProfile);

			int samplesPerAxis = Mathf.RoundToInt(Mathf.Sqrt(numberOfPoints / 6.0f));
			Vector3 scaledSize = Vector3.Scale(size, localToWorld.lossyScale);
			Vector3 boxFaceArea = new Vector3(scaledSize.y * scaledSize.z, scaledSize.x * scaledSize.z, scaledSize.x * scaledSize.y);

			m_dynamicCommandBuffer.SetComputeVectorParam(m_solidToFluidCS, FluidShaderProperties._BoxSize, size);
			m_dynamicCommandBuffer.SetComputeVectorParam(m_solidToFluidCS, FluidShaderProperties._BoxArea, boxFaceArea);
			m_dynamicCommandBuffer.SetComputeIntParam(m_solidToFluidCS, FluidShaderProperties._PointCount, numberOfPoints);
			m_dynamicCommandBuffer.SetComputeIntParam(m_solidToFluidCS, FluidShaderProperties._BoxSamplesPerAxis, samplesPerAxis);

			m_solidToFluidCS.GetKernelThreadGroupSizes(m_solidToFluidBoxKernel, out uint x, out _, out _);
			m_dynamicCommandBuffer.DispatchCompute(m_solidToFluidCS, m_solidToFluidBoxKernel, GraphicsHelpers.DCS(numberOfPoints, x), 1, 1);
		}

		internal virtual void ApplySolidToFluid_Capsule(float radius, float height, int direction, float area, int numberOfPoints, Matrix4x4 localToWorld, Matrix4x4 prevLocalToWorld, FluidRigidBody.FluidDisplacementProfile displacementProfile)
		{
			if (localToWorld == prevLocalToWorld)
			{
				return;
			}
			BindSolidToFluidShared(m_solidToFluidCapsuleKernel, localToWorld, prevLocalToWorld, displacementProfile);

			m_dynamicCommandBuffer.SetComputeFloatParam(m_solidToFluidCS, FluidShaderProperties._PrimitiveArea, area);
			m_dynamicCommandBuffer.SetComputeFloatParam(m_solidToFluidCS, FluidShaderProperties._CapsuleRadius, radius);
			m_dynamicCommandBuffer.SetComputeFloatParam(m_solidToFluidCS, FluidShaderProperties._CapsuleHeight, height);
			m_dynamicCommandBuffer.SetComputeIntParam(m_solidToFluidCS, FluidShaderProperties._CapsuleDirection, direction);
			m_dynamicCommandBuffer.SetComputeIntParam(m_solidToFluidCS, FluidShaderProperties._PointCount, numberOfPoints);

			m_solidToFluidCS.GetKernelThreadGroupSizes(m_solidToFluidCapsuleKernel, out uint x, out _, out _);
			m_dynamicCommandBuffer.DispatchCompute(m_solidToFluidCS, m_solidToFluidCapsuleKernel, GraphicsHelpers.DCS(numberOfPoints, x), 1, 1);
		}

		private void ApplyFlow(Vector3 worldPos, Vector2 direction, Vector2 size, float strength, float falloff, float timestep, int pass)
		{
			//We do normalize in the shader so we dont want / 0 
			if (direction.sqrMagnitude < Mathf.Epsilon || falloff < Mathf.Epsilon)
				return;
			using (new ProfilingScope(m_dynamicCommandBuffer, ProfilingSampler.Get(WaterSimProfileID.Velocity)))
			{
				Vector2 position = WorldSpaceToPaddedVelocityUVSpace(worldPos);
				Vector2 uvSize = WorldSizeToUVSize(size);

				m_externalPropertyBlock.Clear();
				m_externalPropertyBlock.SetVector(FluidShaderProperties._BlitScaleBiasRt, new Vector4(uvSize.x, uvSize.y, position.x, position.y));
				m_externalPropertyBlock.SetVector(FluidShaderProperties._VelocityDir, direction);
				m_externalPropertyBlock.SetFloat(FluidShaderProperties._IncreaseStrength, strength * timestep);
				m_externalPropertyBlock.SetFloat(FluidShaderProperties._IncreaseExponent, falloff);
				BlitQuad(m_dynamicCommandBuffer, null, m_activeVelocity, m_applyVelocityMaterial, m_externalPropertyBlock, pass);
			}
		}

		/// <summary>
		/// Applies a vortex flow effect to the <see cref="FluidSimulation"/> at a specified world position.
		/// </summary>
		/// <param name="worldPos">The world position where the vortex is centered.</param>
		/// <param name="size">The size of the vortex area.</param>
		/// <param name="radialFlowStrength">The strength of the flow at the center of the vortex.</param>
		/// <param name="inwardFlowStrength">The strength of the flow at the outer edge of the vortex.</param>
		/// <param name="timestep">The time delta since the last update.</param>
		public void ApplyFlowVortex(Vector3 worldPos, Vector2 size, float radialFlowStrength, float inwardFlowStrength, float timestep)
		{
			ApplyFlowVortex(worldPos, size, radialFlowStrength, inwardFlowStrength, timestep, m_applyVelocityVortexAdditive);

		}
		/// <summary>
		/// Applies a vortex flow effect to the <see cref="FluidSimulation"/> at a specified world position.
		/// </summary>
		/// <param name="worldPos">The world position where the vortex is centered.</param>
		/// <param name="size">The size of the vortex area.</param>
		/// <param name="radialFlowStrength">The strength of the flow at the center of the vortex.</param>
		/// <param name="inwardFlowStrength">The strength of the flow at the outer edge of the vortex.</param>
		public void SetFlowVortex(Vector3 worldPos, Vector2 size, float radialFlowStrength, float inwardFlowStrength)
		{
			ApplyFlowVortex(worldPos, size, radialFlowStrength, inwardFlowStrength, 1, m_setVelocityVortexBlend);
		}
		/// <summary>
		/// Applies a vortex flow effect to the <see cref="FluidSimulation"/> at a specified world position.
		/// </summary>
		/// <param name="worldPos">The world position where the vortex is centered.</param>
		/// <param name="size">The size of the vortex area.</param>
		/// <param name="radialFlowStrength">The strength of the flow at the center of the vortex.</param>
		/// <param name="inwardFlowStrength">The strength of the flow at the outer edge of the vortex.</param>
		/// <param name="timestep">The time delta since the last update.</param>
		private void ApplyFlowVortex(Vector3 worldPos, Vector2 size, float radialFlowStrength, float inwardFlowStrength, float timestep, int pass)
		{
			using (new ProfilingScope(m_dynamicCommandBuffer, ProfilingSampler.Get(WaterSimProfileID.Velocity)))
			{
				Vector2 position = WorldSpaceToPaddedVelocityUVSpace(worldPos);
				Vector2 uvSize = WorldSizeToUVSize(size);

				m_externalPropertyBlock.Clear();
				m_externalPropertyBlock.SetVector(FluidShaderProperties._BlitScaleBiasRt, new Vector4(uvSize.x, uvSize.y, position.x, position.y));
				m_externalPropertyBlock.SetFloat(FluidShaderProperties._IncreaseStrength, radialFlowStrength * timestep);
				m_externalPropertyBlock.SetFloat(FluidShaderProperties._IncreaseStrengthOuter, inwardFlowStrength * timestep);

				BlitQuad(m_dynamicCommandBuffer, null, m_activeVelocity, m_applyVelocityMaterial, m_externalPropertyBlock, pass);
			}
		}


		/// <summary>
		/// Applies a flow/velocity effect based on a texture to the <see cref="FluidSimulation"/>. The texture will be remapped from 0 to 1 o -1 to 1.
		/// </summary>
		/// <param name="worldPos">The world position where the velocity is applied.</param>
		/// <param name="size">The size of the area affected by the velocity.</param>
		/// <param name="texture">The texture that defines the velocity effect.</param>
		/// <param name="strength">The strength of the velocity effect.</param>
		/// <param name="timestep">The time delta since the last update.</param>
		public virtual void ApplyVelocity(Vector3 worldPos, Vector2 size, Texture texture, float strength, float timestep)
		{
			ApplyVelocity(worldPos, size, texture, strength, timestep, m_applyVelocityTextureRemapped);
		}

		/// <summary>
		/// Applies a flow/velocity effect based on a texture to the <see cref="FluidSimulation"/>. The texture will be remapped from 0 to 1 o -1 to 1.
		/// </summary>
		/// <param name="worldPos">The world position where the velocity is applied.</param>
		/// <param name="size">The size of the area affected by the velocity.</param>
		/// <param name="texture">The texture that defines the velocity effect.</param>
		/// <param name="strength">The strength of the velocity effect.</param>
		public virtual void SetVelocity(Vector3 worldPos, Vector2 size, Texture texture, float strength) 
		{
			ApplyVelocity(worldPos, size, texture, strength, 1, m_setVelocityTextureRemapped);
		}

		/// <summary>
		/// Dampens a flow/velocity effect based on a texture to the <see cref="FluidSimulation"/>.
		/// </summary>
		/// <param name="worldPos">The world position where the velocity damping is applied.</param>
		/// <param name="size">The size of the area affected by the velocity.</param>
		/// <param name="texture">The texture that defines the velocity effect.</param>
		/// <param name="strength">The strength of the velocity effect.</param>
		/// <param name="timestep">The time delta since the last update.</param>
		public void DampenVelocity(Vector3 worldPos, Vector2 size, Texture texture, float strength, float timestep)
		{
			ApplyVelocity(worldPos, size, texture, strength, timestep, m_dampenVelocityTexture);
		}

		/// <summary>
		/// Applies a flow/velocity effect based on a texture to the <see cref="FluidSimulation"/>. The texture will be remapped from 0 to 1 o -1 to 1.
		/// </summary>
		/// <param name="worldPos">The world position where the velocity is applied.</param>
		/// <param name="size">The size of the area affected by the velocity.</param>
		/// <param name="texture">The texture that defines the velocity effect.</param>
		/// <param name="strength">The strength of the velocity effect.</param>
		/// <param name="timestep">The time delta since the last update.</param>
		private void ApplyVelocity(Vector3 worldPos, Vector2 size, Texture texture, float strength, float timestep, int pass) 
		{
			using (new ProfilingScope(m_dynamicCommandBuffer, ProfilingSampler.Get(WaterSimProfileID.ExternalForceTexture)))
			{
				Vector2 position = WorldSpaceToPaddedVelocityUVSpace(worldPos);
				Vector2 uvSize = WorldSizeToUVSize(size);

				m_externalPropertyBlock.Clear();
				m_externalPropertyBlock.SetVector(FluidShaderProperties._BlitScaleBiasRt, new Vector4(uvSize.x, uvSize.y, position.x, position.y));
				m_externalPropertyBlock.SetFloat(FluidShaderProperties._IncreaseStrength, strength * timestep);
				m_externalPropertyBlock.SetVector(FluidShaderProperties._VelocityDir, new Vector2(2, -1));
				BlitQuad(m_dynamicCommandBuffer, texture, m_activeVelocity, m_applyVelocityMaterial, m_externalPropertyBlock, pass);
			}
		}

		public void DampenVelocityCircle(Vector3 worldPos, Vector2 size, float strength, float falloff, float timestep)
		{
			DampenVelocity(worldPos, size, strength, falloff, timestep, m_dampenVelocityCircle);
		}

		private void DampenVelocity(Vector3 worldPos, Vector2 size, float strength, float falloff, float timestep, int pass)
		{
			using (new ProfilingScope(m_dynamicCommandBuffer, ProfilingSampler.Get(WaterSimProfileID.ExternalForceTexture)))
			{
				Vector2 position = WorldSpaceToPaddedVelocityUVSpace(worldPos);
				Vector2 uvSize = WorldSizeToUVSize(size);

				m_externalPropertyBlock.Clear();
				m_externalPropertyBlock.SetVector(FluidShaderProperties._BlitScaleBiasRt, new Vector4(uvSize.x, uvSize.y, position.x, position.y));
				m_externalPropertyBlock.SetFloat(FluidShaderProperties._IncreaseStrength, (strength * timestep));
				m_externalPropertyBlock.SetFloat(FluidShaderProperties._IncreaseExponent, falloff);
				BlitQuad(m_dynamicCommandBuffer, null, m_activeVelocity, m_applyVelocityMaterial, m_externalPropertyBlock, pass);
			}
		}



		/// <summary>
		/// Applies a force to the <see cref="FluidSimulation"/> at a specified world position.
		/// </summary>
		/// <param name="worldPos">The world position where the force is applied.</param>
		/// <param name="direction">The direction of the force.</param>
		/// <param name="size">The size of the area affected by the force.</param>
		/// <param name="strength">The strength of the force effect.</param>
		/// <param name="falloff">The falloff used to control the gradient of the force effect.</param>
		/// <param name="timestep">The time delta since the last update.</param>
		/// <param name="splash">If true, applies the force as a splash(outward) effect; otherwise, applies as a directional force.</param>
		public virtual void ApplyForce(Vector3 worldPos, Vector2 direction, Vector2 size, float strength, float falloff, float timestep, bool splash) {}

		/// <summary>
		/// Applies a force effect based on a texture to the <see cref="FluidSimulation"/>.
		/// </summary>
		/// <param name="texture">The texture that defines the force effect.</param>
		/// <param name="strength">The strength of the force effect.</param>
		/// <param name="timestep">The time delta since the last update.</param>
		public virtual void ApplyForce(Texture texture, float strength, float timestep) {}

		/// <summary>
		/// Applies a force effect based on a texture to the <see cref="FluidSimulation"/>.
		/// </summary>
		/// <param name="texture">The texture that defines the force effect.</param>
		/// <param name="strength">The strength of the force effect.</param>
		/// <param name="timestep">The time delta since the last update.</param>
		public virtual void ApplyForce(int texture, float strength, float timestep) { }


		/// <summary>
		/// Applies a force effect based on a texture to the <see cref="FluidSimulation"/>.
		/// </summary>
		/// <param name="texture">The texture that defines the force effect.</param>
		/// <param name="strength">The strength of the force effect.</param>
		/// <param name="timestep">The time delta since the last update.</param>
		public virtual void ApplyForce(Vector3 worldPos, Vector2 size, Texture texture, float strength, float timestep) { }

		/// <summary>
		/// Applies a vortex force to the <see cref="FluidSimulation"/> at a specified world position.
		/// </summary>
		/// <param name="worldPos">The world position where the vortex force is centered.</param>
		/// <param name="size">The size of the vortex area.</param>
		/// <param name="strength">The strength of the vortex effect.</param>
		/// <param name="falloff">The falloff used to control the gradient of the vortex effect.</param>
		/// <param name="timestep">The time delta since the last update.</param>
		public virtual void ApplyForceVortex(Vector3 worldPos, Vector2 size, float strength, float falloff, float timestep) {}


		/// <summary>
		/// Applies a force to the <see cref="FluidSimulation"/> at a specified world position.
		/// </summary>
		/// <param name="worldPos">The world position where the force is applied.</param>
		/// <param name="size">The size of the area affected by the force.</param>
		/// <param name="strength">The strength of the force effect.</param>
		/// <param name="falloff">The falloff used to control the gradient of the force effect.</param>
		/// <param name="timestep">The time delta since the last update.</param>
		public virtual void DampenForce(Vector3 worldPos, Vector2 size, float strength, float falloff, float timestep) { }

		/// <summary>
		/// Applies a force effect based on a texture to the <see cref="FluidSimulation"/>.
		/// </summary>
		/// <param name="texture">The texture that defines the force effect.</param>
		/// <param name="strength">The strength of the force effect.</param>
		/// <param name="timestep">The time delta since the last update.</param>
		public virtual void DampenForce(Vector3 worldPos, Vector2 size, Texture texture, float strength, float timestep) { }


		/// <summary>
		/// Adds fluid to the <see cref="FluidSimulation"/> at a specified world position in a circular shape.
		/// </summary>
		/// <param name="worldPos">The world position where the fluid is added.</param>
		/// <param name="size">The size of the fluid area.</param>
		/// <param name="amount">The strength of the added fluid.</param>
		/// <param name="falloff">The falloff used to control the gradient/shape of the added fluid.</param>
		/// <param name="layer">The layer in which the fluid will be added.</param>
		/// <param name="timestep">The time delta since the last update.</param>
		public void AddFluidCircle(Vector3 worldPos, Vector2 size, float amount, float falloff, int layer, float timestep)
		{
			using (new ProfilingScope(m_dynamicCommandBuffer, ProfilingSampler.Get(WaterSimProfileID.ExternalWater)))
			{
				AddFluid(m_dynamicCommandBuffer, platformSupportsFloat32Blend ? m_activeWaterHeight : m_dynamicInput, worldPos, size, amount, falloff, layer, timestep, m_addFluidCirclePass);
			}
		}

		/// <summary>
		/// Adds fluid to the <see cref="FluidSimulation"/> at a specified world position in a square shape.
		/// </summary>
		/// <param name="worldPos">The world position where the fluid is added.</param>
		/// <param name="size">The size of the fluid area.</param>
		/// <param name="amount">The strength of the added fluid.</param>
		/// <param name="falloff">The falloff used to control the gradient/shape of the added fluid.</param>
		/// <param name="layer">The layer in which the fluid will be added.</param>
		/// <param name="timestep">The time delta since the last update.</param>
		public void AddFluidSquare(Vector3 worldPos, Vector2 size, float amount, float falloff, int layer, float timestep)
		{
			using (new ProfilingScope(m_dynamicCommandBuffer, ProfilingSampler.Get(WaterSimProfileID.ExternalWater)))
			{
				AddFluid(m_dynamicCommandBuffer, platformSupportsFloat32Blend ? m_activeWaterHeight : m_dynamicInput, worldPos, size, amount, falloff, layer, timestep, m_addFluidSquarePass);
			}
		}

		protected void AddFluid(CommandBuffer commandBuffer, RenderTexture dest, Vector3 worldPos, Vector2 size, float strength, float exponent, int layer, float dt, int pass)
		{
			Vector2 position = WorldSpaceToPaddedUVSpace(worldPos);
			Vector2 uvSize = WorldSizeToUVSize(size);

			m_externalPropertyBlock.Clear();
			m_externalPropertyBlock.SetVector(FluidShaderProperties._BlitScaleBiasRt, new Vector4(uvSize.x, uvSize.y, position.x, position.y));
			m_externalPropertyBlock.SetVector(FluidShaderProperties._BlitRotation, Vector2.up);
			m_externalPropertyBlock.SetVector(FluidShaderProperties._LayerMask, layer == 0 ? Vector3.right : Vector3.up);
			m_externalPropertyBlock.SetFloat(FluidShaderProperties._IncreaseStrength, strength * dt);
			m_externalPropertyBlock.SetFloat(FluidShaderProperties._IncreaseExponent, exponent);
			BlitQuad(commandBuffer, null, dest, m_addFluidMaterial, m_externalPropertyBlock, pass);
		}

		/// <summary>
		/// Set fluid the <see cref="FluidSimulation"/> to a height at a specified world position in a circular shape.
		/// </summary>
		/// <param name="worldPos">The world position where the fluid is added.</param>
		/// <param name="size">The size of the fluid area.</param>
		/// <param name="height">The height of the added fluid.</param>
		/// <param name="falloff">The falloff used to control the gradient/shape of the added fluid.</param>
		/// <param name="layer">The layer in which the fluid will be added.</param>
		public void SetFluidHeightCircle(Vector3 worldPos, Vector2 size, float height, float falloff, int layer, FluidModifierSpace space)
		{
			using (new ProfilingScope(m_dynamicCommandBuffer, ProfilingSampler.Get(WaterSimProfileID.ExternalWater)))
			{
				SetFluid(m_dynamicCommandBuffer, m_activeWaterHeight, worldPos, size, height, falloff, layer, space, BlendOp.Add, m_mixFluidHeightCirclePass);
			}
		}

		/// <summary>
		/// Set fluid the <see cref="FluidSimulation"/> to a height at a specified world position in a square shape.
		/// </summary>
		/// <param name="worldPos">The world position where the fluid is added.</param>
		/// <param name="size">The size of the fluid area.</param>
		/// <param name="height">The height of the added fluid.</param>
		/// <param name="falloff">The falloff used to control the gradient/shape of the added fluid.</param>
		/// <param name="layer">The layer in which the fluid will be added.</param>
		public void SetFluidHeightSquare(Vector3 worldPos, Vector2 size, float height, float falloff, int layer, FluidModifierSpace space)
		{
			using (new ProfilingScope(m_dynamicCommandBuffer, ProfilingSampler.Get(WaterSimProfileID.ExternalWater)))
			{
				SetFluid(m_dynamicCommandBuffer, m_activeWaterHeight, worldPos, size, height, falloff, layer, space, BlendOp.Add, m_mixFluidHeightSquarePass);
			}
		}
		/// <summary>
		/// Set fluid the <see cref="FluidSimulation"/> to a height at a specified world position using a texture.
		/// </summary>
		/// <param name="worldPos">The world position where the fluid is added.</param>
		/// <param name="size">The size of the fluid area.</param>
		/// <param name="height">The height of the added fluid.</param>
		/// <param name="falloff">The falloff used to control the gradient/shape of the added fluid.</param>
		/// <param name="layer">The layer in which the fluid will be added.</param>
		public void SetFluidHeightTexture(Vector3 worldPos, Vector2 size, Texture source, float height, int layer, FluidModifierSpace space)
		{
			using (new ProfilingScope(m_dynamicCommandBuffer, ProfilingSampler.Get(WaterSimProfileID.ExternalWater)))
			{
				SetFluid(m_dynamicCommandBuffer, m_activeWaterHeight, worldPos, size, source, height, layer, space, BlendOp.Add, m_mixFluidHeightTexturePass);
			}
		}

		/// <summary>
		/// Set fluid the <see cref="FluidSimulation"/> to a depth at a specified world position in a circular shape.
		/// </summary>
		/// <param name="worldPos">The world position where the fluid is added.</param>
		/// <param name="size">The size of the fluid area.</param>
		/// <param name="height">The height of the added fluid.</param>
		/// <param name="falloff">The falloff used to control the gradient/shape of the added fluid.</param>
		/// <param name="layer">The layer in which the fluid will be added.</param>
		public void SetFluidDepthCircle(Vector3 worldPos, Vector2 size, float height, float falloff, int layer, FluidModifierSpace space)
		{
			using (new ProfilingScope(m_dynamicCommandBuffer, ProfilingSampler.Get(WaterSimProfileID.ExternalWater)))
			{
				SetFluid(m_dynamicCommandBuffer, m_activeWaterHeight, worldPos, size, height, falloff, layer, space, BlendOp.Add, m_mixFluidDepthCirclePass);
			}
		}

		/// <summary>
		/// Set fluid the <see cref="FluidSimulation"/> to a height at a specified world position in a square shape.
		/// </summary>
		/// <param name="worldPos">The world position where the fluid is added.</param>
		/// <param name="size">The size of the fluid area.</param>
		/// <param name="height">The height of the added fluid.</param>
		/// <param name="falloff">The falloff used to control the gradient/shape of the added fluid.</param>
		/// <param name="layer">The layer in which the fluid will be added.</param>
		public void SetFluidDepthSquare(Vector3 worldPos, Vector2 size, float height, float falloff, int layer, FluidModifierSpace space)
		{
			using (new ProfilingScope(m_dynamicCommandBuffer, ProfilingSampler.Get(WaterSimProfileID.ExternalWater)))
			{
				SetFluid(m_dynamicCommandBuffer, m_activeWaterHeight, worldPos, size, height, falloff, layer, space, BlendOp.Add, m_mixFluidDepthSquarePass);
			}
		}
		/// <summary>
		/// Set fluid the <see cref="FluidSimulation"/> to a height at a specified world position using a texture.
		/// </summary>
		/// <param name="worldPos">The world position where the fluid is added.</param>
		/// <param name="size">The size of the fluid area.</param>
		/// <param name="height">The height of the added fluid.</param>
		/// <param name="falloff">The falloff used to control the gradient/shape of the added fluid.</param>
		/// <param name="layer">The layer in which the fluid will be added.</param>
		public void SetFluidDepthTexture(Vector3 worldPos, Vector2 size, Texture source, float height, int layer, FluidModifierSpace space)
		{
			using (new ProfilingScope(m_dynamicCommandBuffer, ProfilingSampler.Get(WaterSimProfileID.ExternalWater)))
			{
				SetFluid(m_dynamicCommandBuffer, m_activeWaterHeight, worldPos, size, source, height, layer, space, BlendOp.Add, m_mixFluidDepthTexturePass);
			}
		}

		/// <summary>
		/// Stop fluid from going higher than a the set amount in world space.
		/// </summary>
		/// <param name="worldPos">The world position where the fluid is added.</param>
		/// <param name="size">The size of the fluid area.</param>
		/// <param name="height">The height of the added fluid.</param>
		/// <param name="falloff">The falloff used to control the gradient/shape of the added fluid.</param>
		/// <param name="layer">The layer in which the fluid will be added.</param>
		public void SetMinFluidHeightCircle(Vector3 worldPos, Vector2 size, float height, float falloff, int layer, FluidModifierSpace space)
		{
			using (new ProfilingScope(m_dynamicCommandBuffer, ProfilingSampler.Get(WaterSimProfileID.ExternalWater)))
			{
				SetFluid(m_dynamicCommandBuffer, m_activeWaterHeight, worldPos, size, height, falloff, layer, space, BlendOp.Min, m_mixFluidHeightCirclePass);
			}
		}

		/// <summary>
		/// Stop fluid from going higher than a the set amount in world space.
		/// </summary>
		/// <param name="worldPos">The world position where the fluid is added.</param>
		/// <param name="size">The size of the fluid area.</param>
		/// <param name="height">The height of the added fluid.</param>
		/// <param name="falloff">The falloff used to control the gradient/shape of the added fluid.</param>
		/// <param name="layer">The layer in which the fluid will be added.</param>
		public void SetMinFluidHeightSquare(Vector3 worldPos, Vector2 size, float height, float falloff, int layer, FluidModifierSpace space)
		{
			using (new ProfilingScope(m_dynamicCommandBuffer, ProfilingSampler.Get(WaterSimProfileID.ExternalWater)))
			{
				SetFluid(m_dynamicCommandBuffer, m_activeWaterHeight, worldPos, size, height, falloff, layer, space, BlendOp.Min, m_mixFluidHeightSquarePass);
			}
		}
		/// <summary>
		/// Stop fluid from going higher than a the set amount in world space.
		/// </summary>
		/// <param name="worldPos">The world position where the fluid is added.</param>
		/// <param name="size">The size of the fluid area.</param>
		/// <param name="height">The height of the added fluid.</param>
		/// <param name="falloff">The falloff used to control the gradient/shape of the added fluid.</param>
		/// <param name="layer">The layer in which the fluid will be added.</param>
		public void SetMinFluidHeightTexture(Vector3 worldPos, Vector2 size, Texture source, float height, int layer, FluidModifierSpace space)
		{
			using (new ProfilingScope(m_dynamicCommandBuffer, ProfilingSampler.Get(WaterSimProfileID.ExternalWater)))
			{
				SetFluid(m_dynamicCommandBuffer, m_activeWaterHeight, worldPos, size, source, height, layer, space, BlendOp.Min, m_mixFluidHeightTexturePass);
			}
		}

		/// <summary>
		/// Stop fluid from going lower than a the set amount in world space.
		/// </summary>
		/// <param name="worldPos">The world position where the fluid is added.</param>
		/// <param name="size">The size of the fluid area.</param>
		/// <param name="height">The height of the added fluid.</param>
		/// <param name="falloff">The falloff used to control the gradient/shape of the added fluid.</param>
		/// <param name="layer">The layer in which the fluid will be added.</param>
		public void SetMaxFluidHeightCircle(Vector3 worldPos, Vector2 size, float height, float falloff, int layer, FluidModifierSpace space)
		{
			using (new ProfilingScope(m_dynamicCommandBuffer, ProfilingSampler.Get(WaterSimProfileID.ExternalWater)))
			{
				SetFluid(m_dynamicCommandBuffer, m_activeWaterHeight, worldPos, size, height, falloff, layer, space, BlendOp.Max, m_mixFluidHeightCirclePass);
			}
		}

		/// <summary>
		/// Stop fluid from going lower than a the set amount in world space.
		/// </summary>
		/// <param name="worldPos">The world position where the fluid is added.</param>
		/// <param name="size">The size of the fluid area.</param>
		/// <param name="height">The height of the added fluid.</param>
		/// <param name="falloff">The falloff used to control the gradient/shape of the added fluid.</param>
		/// <param name="layer">The layer in which the fluid will be added.</param>
		public void SetMaxFluidHeightSquare(Vector3 worldPos, Vector2 size, float height, float falloff, int layer, FluidModifierSpace space)
		{
			using (new ProfilingScope(m_dynamicCommandBuffer, ProfilingSampler.Get(WaterSimProfileID.ExternalWater)))
			{
				SetFluid(m_dynamicCommandBuffer, m_activeWaterHeight, worldPos, size, height, falloff, layer, space, BlendOp.Max, m_mixFluidHeightSquarePass);
			}
		}

		/// <summary>
		/// Stop fluid from going lower than a the set amount in world space.
		/// </summary>
		/// <param name="worldPos">The world position where the fluid is added.</param>
		/// <param name="size">The size of the fluid area.</param>
		/// <param name="height">The height of the added fluid.</param>
		/// <param name="falloff">The falloff used to control the gradient/shape of the added fluid.</param>
		/// <param name="layer">The layer in which the fluid will be added.</param>
		public void SetMaxFluidHeightTexture(Vector3 worldPos, Vector2 size, Texture source, float height, int layer, FluidModifierSpace space)
		{
			using (new ProfilingScope(m_dynamicCommandBuffer, ProfilingSampler.Get(WaterSimProfileID.ExternalWater)))
			{
				SetFluid(m_dynamicCommandBuffer, m_activeWaterHeight, worldPos, size, source, height, layer, space, BlendOp.Max, m_mixFluidHeightTexturePass);
			}
		}

		/// <summary>
		/// Stop fluid from going higher than a the set amount in world space.
		/// </summary>
		/// <param name="worldPos">The world position where the fluid is added.</param>
		/// <param name="size">The size of the fluid area.</param>
		/// <param name="height">The height of the added fluid.</param>
		/// <param name="falloff">The falloff used to control the gradient/shape of the added fluid.</param>
		/// <param name="layer">The layer in which the fluid will be added.</param>
		public void SetMinFluidDepthCircle(Vector3 worldPos, Vector2 size, float height, float falloff, int layer, FluidModifierSpace space)
		{
			using (new ProfilingScope(m_dynamicCommandBuffer, ProfilingSampler.Get(WaterSimProfileID.ExternalWater)))
			{
				SetFluid(m_dynamicCommandBuffer, m_activeWaterHeight, worldPos, size, height, falloff, layer, space, BlendOp.Min, m_mixFluidDepthCirclePass);
			}
		}

		/// <summary>
		/// Stop fluid from going higher than a the set amount in world space.
		/// </summary>
		/// <param name="worldPos">The world position where the fluid is added.</param>
		/// <param name="size">The size of the fluid area.</param>
		/// <param name="height">The height of the added fluid.</param>
		/// <param name="falloff">The falloff used to control the gradient/shape of the added fluid.</param>
		/// <param name="layer">The layer in which the fluid will be added.</param>
		public void SetMinFluidDepthSquare(Vector3 worldPos, Vector2 size, float height, float falloff, int layer, FluidModifierSpace space)
		{
			using (new ProfilingScope(m_dynamicCommandBuffer, ProfilingSampler.Get(WaterSimProfileID.ExternalWater)))
			{
				SetFluid(m_dynamicCommandBuffer, m_activeWaterHeight, worldPos, size, height, falloff, layer, space, BlendOp.Min, m_mixFluidDepthSquarePass);
			}
		}

		/// <summary>
		/// Stop fluid from going higher than a the set amount in world space.
		/// </summary>
		/// <param name="worldPos">The world position where the fluid is added.</param>
		/// <param name="size">The size of the fluid area.</param>
		/// <param name="height">The height of the added fluid.</param>
		/// <param name="falloff">The falloff used to control the gradient/shape of the added fluid.</param>
		/// <param name="layer">The layer in which the fluid will be added.</param>
		public void SetMinFluidDepthTexture(Vector3 worldPos, Vector2 size, Texture source, float height, int layer, FluidModifierSpace space)
		{
			using (new ProfilingScope(m_dynamicCommandBuffer, ProfilingSampler.Get(WaterSimProfileID.ExternalWater)))
			{
				SetFluid(m_dynamicCommandBuffer, m_activeWaterHeight, worldPos, size, source, height, layer, space, BlendOp.Min, m_mixFluidDepthTexturePass);
			}
		}

		/// <summary>
		/// Stop fluid from going lower than a the set amount in world space.
		/// </summary>
		/// <param name="worldPos">The world position where the fluid is added.</param>
		/// <param name="size">The size of the fluid area.</param>
		/// <param name="height">The height of the added fluid.</param>
		/// <param name="falloff">The falloff used to control the gradient/shape of the added fluid.</param>
		/// <param name="layer">The layer in which the fluid will be added.</param>
		public void SetMaxFluidDepthCircle(Vector3 worldPos, Vector2 size, float height, float falloff, int layer, FluidModifierSpace space)
		{
			using (new ProfilingScope(m_dynamicCommandBuffer, ProfilingSampler.Get(WaterSimProfileID.ExternalWater)))
			{
				SetFluid(m_dynamicCommandBuffer, m_activeWaterHeight, worldPos, size, height, falloff, layer, space, BlendOp.Max, m_mixFluidDepthCirclePass);
			}
		}

		/// <summary>
		/// Stop fluid from going lower than a the set amount in world space.
		/// </summary>
		/// <param name="worldPos">The world position where the fluid is added.</param>
		/// <param name="size">The size of the fluid area.</param>
		/// <param name="height">The height of the added fluid.</param>
		/// <param name="falloff">The falloff used to control the gradient/shape of the added fluid.</param>
		/// <param name="layer">The layer in which the fluid will be added.</param>
		public void SetMaxFluidDepthSquare(Vector3 worldPos, Vector2 size, float height, float falloff, int layer, FluidModifierSpace space)
		{
			using (new ProfilingScope(m_dynamicCommandBuffer, ProfilingSampler.Get(WaterSimProfileID.ExternalWater)))
			{
				SetFluid(m_dynamicCommandBuffer, m_activeWaterHeight, worldPos, size, height, falloff, layer, space, BlendOp.Max, m_mixFluidDepthSquarePass);
			}
		}
		/// <summary>
		/// Stop fluid from going lower than a the set amount in world space.
		/// </summary>
		/// <param name="worldPos">The world position where the fluid is added.</param>
		/// <param name="size">The size of the fluid area.</param>
		/// <param name="height">The height of the added fluid.</param>
		/// <param name="falloff">The falloff used to control the gradient/shape of the added fluid.</param>
		/// <param name="layer">The layer in which the fluid will be added.</param>
		public void SetMaxFluidDepthTexture(Vector3 worldPos, Vector2 size, Texture source, float height, int layer, FluidModifierSpace space)
		{
			using (new ProfilingScope(m_dynamicCommandBuffer, ProfilingSampler.Get(WaterSimProfileID.ExternalWater)))
			{
				SetFluid(m_dynamicCommandBuffer, m_activeWaterHeight, worldPos, size, source, height, layer, space, BlendOp.Max, m_mixFluidDepthTexturePass);
			}
		}

		private void SetFluid(CommandBuffer commandBuffer, RenderTexture dest, Vector3 worldPos, Vector2 size, float height, float falloff, int layer, FluidModifierSpace space, BlendOp blendOp, int pass)
		{
			Vector2 position = WorldSpaceToPaddedUVSpace(worldPos);
			Vector2 uvSize = WorldSizeToUVSize(size);

			float yOffset = space == FluidModifierSpace.WorldHeight ? transform.position.y : 0;
			float amount = height;
			m_externalPropertyBlock.Clear();
			m_externalPropertyBlock.SetVector(FluidShaderProperties._BlitScaleBiasRt, new Vector4(uvSize.x, uvSize.y, position.x, position.y));
			m_externalPropertyBlock.SetVector(FluidShaderProperties._BlitRotation, Vector2.up);
			m_externalPropertyBlock.SetVector(FluidShaderProperties._LayerMask, layer == 0 ? Vector3.right : Vector3.up);
			m_externalPropertyBlock.SetFloat(FluidShaderProperties._IncreaseStrength, amount - yOffset);
			m_externalPropertyBlock.SetFloat(FluidShaderProperties._IncreaseExponent, falloff);
			//m_externalPropertyBlock.SetTexture(FluidShaderProperties._FluidHeightField, dest);
			m_externalPropertyBlock.SetTexture(FluidShaderProperties._TerrainHeightField, m_terrainHeight);
			commandBuffer.SetGlobalInt(FluidShaderProperties._ColorMaskFluidInteraction, LayerToColorMask(layer));
			commandBuffer.SetGlobalInt(FluidShaderProperties._BlendOpFluidInteraction, (int)blendOp); 
			BlitQuad(commandBuffer, null, dest, m_addFluidMaterial, m_externalPropertyBlock, pass);
		}

		private void SetFluid(CommandBuffer commandBuffer, RenderTexture dest, Vector3 worldPos, Vector2 size, Texture source, float height, int layer, FluidModifierSpace space, BlendOp blendOp, int pass)
		{
			Vector2 position = WorldSpaceToPaddedUVSpace(worldPos);
			Vector2 uvSize = WorldSizeToUVSize(size);

			float yOffset = space == FluidModifierSpace.WorldHeight ? transform.position.y : 0;
			float amount = height;
			m_externalPropertyBlock.Clear();
			m_externalPropertyBlock.SetVector(FluidShaderProperties._BlitScaleBiasRt, new Vector4(uvSize.x, uvSize.y, position.x, position.y));
			m_externalPropertyBlock.SetVector(FluidShaderProperties._BlitRotation, Vector2.up);
			m_externalPropertyBlock.SetVector(FluidShaderProperties._LayerMask, layer == 0 ? Vector3.right : Vector3.up);
			m_externalPropertyBlock.SetFloat(FluidShaderProperties._IncreaseStrength, amount - yOffset);
			m_externalPropertyBlock.SetTexture(FluidShaderProperties._TerrainHeightField, m_terrainHeight);
			commandBuffer.SetGlobalInt(FluidShaderProperties._ColorMaskFluidInteraction, LayerToColorMask(layer));
			commandBuffer.SetGlobalInt(FluidShaderProperties._BlendOpFluidInteraction, (int)blendOp);
			BlitQuad(commandBuffer, source, dest, m_addFluidMaterial, m_externalPropertyBlock, pass);
		}

		protected void AddFluidStatic(CommandBuffer commandBuffer, Texture source, Texture current, RenderTexture dest)
		{
			using (new ProfilingScope(commandBuffer, ProfilingSampler.Get(WaterSimProfileID.AddWater)))
			{
				m_externalPropertyBlock.Clear();
				m_externalPropertyBlock.SetVector(FluidShaderProperties._BlitScaleBiasRt, new Vector4(1.0f, 1.0f, 0.5f, 0.5f)); //Focus around the center of the volume. So we need to offset same for global static texture
				m_externalPropertyBlock.SetVector(FluidShaderProperties._BlitRotation, Vector2.up); 
				m_externalPropertyBlock.SetTexture(FluidShaderProperties._FluidHeightField, current); //Focus around the center of the volume. So we need to offset same for global static texture
				BlitQuad(commandBuffer, source, dest, m_addFluidMaterial, m_externalPropertyBlock, m_addFluidTextureStaticPass);
			}
		}

		protected void AddFluidDynamic(CommandBuffer commandBuffer, Texture source, Texture current, RenderTexture dest)
		{
			using (new ProfilingScope(commandBuffer, ProfilingSampler.Get(WaterSimProfileID.AddWater)))
			{
				m_externalPropertyBlock.Clear();
				m_externalPropertyBlock.SetVector(FluidShaderProperties._BlitScaleBiasRt, new Vector4(1.0f, 1.0f, 0.5f, 0.5f)); //Focus around the center of the volume. So we need to offset same for global static texture
				m_externalPropertyBlock.SetVector(FluidShaderProperties._BlitRotation, Vector2.up); 
				m_externalPropertyBlock.SetTexture(FluidShaderProperties._FluidHeightField, current); //Focus around the center of the volume. So we need to offset same for global static texture
				BlitQuad(commandBuffer, source, dest, m_addFluidMaterial, m_externalPropertyBlock, m_addFluidTextureDynamicPass);
			}
		}

		/// <summary>
		/// Applies a force effect based on a texture to the <see cref="FluidSimulation"/>.
		/// </summary>
		/// <param name="worldPos">The world position where the fluid is added.</param>
		/// <param name="worldSize">The size of the fluid area.</param> 
		/// <param name="source">The texture that defines the force effect.</param>
		/// <param name="amount">The strength of the force effect.</param>
		/// <param name="timestep">The time delta since the last update.</param>
		public void AddFluid(Vector3 worldPos, Vector2 worldSize, Texture source, float amount, int layer, float timestep)
		{
			AddFluid(m_dynamicCommandBuffer, worldPos, worldSize, source, m_activeWaterHeight, amount, layer, timestep);
		}

		private void AddFluid(CommandBuffer commandBuffer, Vector3 worldPos, Vector2 worldSize, Texture source, RenderTexture dest, float strength, int layer, float dt)
		{
			using (new ProfilingScope(commandBuffer, ProfilingSampler.Get(WaterSimProfileID.AddWater)))
			{
				Vector2 position = WorldSpaceToPaddedUVSpace(worldPos);
				Vector2 uvSize = WorldSizeToUVSize(worldSize);

				m_externalPropertyBlock.Clear();
				m_externalPropertyBlock.SetVector(FluidShaderProperties._BlitScaleBiasRt, new Vector4(uvSize.x, uvSize.y, position.x, position.y));
				m_externalPropertyBlock.SetVector(FluidShaderProperties._BlitRotation, Vector2.up);
				m_externalPropertyBlock.SetVector(FluidShaderProperties._LayerMask, layer == 0 ? Vector3.right : Vector3.up);
				m_externalPropertyBlock.SetFloat(FluidShaderProperties._IncreaseStrength, strength * dt);
				BlitQuad(commandBuffer, source, dest, m_addFluidMaterial, m_externalPropertyBlock, m_addFluidTexturePass);
			}
		}

		private float GetProjectedQuadSize(FluidObstacleData data)
		{
			if (data.shape == FluidSimulationObstacle.ObstacleShape.Sphere) return data.radius * 2.0f;
			if (data.shape == FluidSimulationObstacle.ObstacleShape.Ellipsoid)
				return Mathf.Max(data.boxSize.x, Mathf.Max(data.boxSize.y, data.boxSize.z)) * 2.0f;

			Vector3 right = data.rotation * Vector3.right;
			Vector3 up = data.rotation * Vector3.up;
			Vector3 fwd = data.rotation * Vector3.forward;

			float widthX = 0f;
			float depthZ = 0f;

			switch (data.shape)
			{
				case FluidSimulationObstacle.ObstacleShape.Box:
				case FluidSimulationObstacle.ObstacleShape.Wedge:
					widthX = Mathf.Abs(right.x) * data.boxSize.x + Mathf.Abs(up.x) * data.boxSize.y + Mathf.Abs(fwd.x) * data.boxSize.z;
					depthZ = Mathf.Abs(right.z) * data.boxSize.x + Mathf.Abs(up.z) * data.boxSize.y + Mathf.Abs(fwd.z) * data.boxSize.z;
					break;

				default:
					float maxR = data.radius;
					if (data.shape == FluidSimulationObstacle.ObstacleShape.CappedCone) maxR = Mathf.Max(data.radius, data.radius2);
					if (data.shape == FluidSimulationObstacle.ObstacleShape.HexPrism) maxR *= 1.155f;

					float spineX = Mathf.Abs(fwd.x) * data.height;
					float spineZ = Mathf.Abs(fwd.z) * data.height;

					widthX = spineX + (maxR * 2.0f);
					depthZ = spineZ + (maxR * 2.0f);
					break;
			}

			return Mathf.Max(widthX, depthZ);
		}

		internal void AddObstacle(CommandBuffer cmd, FluidObstacleData data)
		{
			float quadSize = GetProjectedQuadSize(data);

			// Generic Shader Parameters
			// _Size.x = Radius 1 / Box Half-X
			// _Size.y = Radius 2 / Box Half-Y / Cylinder Half-Height
			// _Size.z = Box Half-Z / Cone Half-Height
			Vector3 shaderParam = Vector3.zero;

			switch (data.shape)
			{
				case FluidSimulationObstacle.ObstacleShape.Box:
				case FluidSimulationObstacle.ObstacleShape.Wedge:
				case FluidSimulationObstacle.ObstacleShape.Ellipsoid:
					shaderParam = data.boxSize * 0.5f; // Half Extents
					break;
				case FluidSimulationObstacle.ObstacleShape.Sphere:
					shaderParam.x = data.radius;
					break;
				case FluidSimulationObstacle.ObstacleShape.CappedCone:
					shaderParam.x = data.radius;  // Bot
					shaderParam.y = data.radius2; // Top
					shaderParam.z = data.height * 0.5f; // Half-Height
					break;
				default:
					shaderParam.x = data.radius;
					shaderParam.y = data.height * 0.5f; // Half-Height
					break;
			}

			Matrix4x4 quadMatrix = Matrix4x4.TRS(data.position, Quaternion.Euler(90, 0, 0), new Vector3(quadSize, quadSize, 1f));
			Matrix4x4 worldToLocal = Matrix4x4.TRS(data.position, data.rotation, Vector3.one).inverse;

			Vector2 halfSize = dimension * 0.5f * (Vector2.one + (ghostCells2 + Vector2Int.one) / new Vector2(m_obstacleHeight.width, m_obstacleHeight.height));
			Vector2 totalWorldSize = halfSize * 2.0f;
			Vector2 worldTexelSize = new Vector2(totalWorldSize.x / m_obstacleHeight.width, totalWorldSize.y / m_obstacleHeight.height);

			int finalPass = (int)data.shape + (data.smooth ? 8 : 0);

			m_externalPropertyBlock.Clear();
			m_externalPropertyBlock.SetMatrix(FluidShaderProperties._Transform, worldToLocal);
			m_externalPropertyBlock.SetVector(FluidShaderProperties._Center, data.position);
			m_externalPropertyBlock.SetVector(FluidShaderProperties._Size, shaderParam);
			m_externalPropertyBlock.SetVector(FluidShaderProperties._TexelSize, worldTexelSize);

			cmd.DrawProcedural(quadMatrix, m_obstacleProceduralMaterial, finalPass, MeshTopology.Quads, 4, 1, m_externalPropertyBlock);
		}

		internal void GetTemporaryRT(int id, int width, int height, GraphicsFormat format)
		{
			m_dynamicCommandBuffer.GetTemporaryRT(id, width, height, 0, FilterMode.Bilinear, format);
		}

		internal void ReleaseTemporaryRT(int id)
		{
			m_dynamicCommandBuffer.ReleaseTemporaryRT(id);
		}



		/// <summary>
		/// A helper function to easily blit a quad with the specified source material data to a <see cref="RenderTexture"/>.
		/// </summary>
		/// <param name="commandBuffer">The <see cref="CommandBuffer"/> to which the drawing commands will be added.</param>
		/// <param name="src">The source texture to be used on the quad's surface. This can be null, 
		/// in which case the shader will not use a texture.</param>
		/// <param name="dst">The <see cref="RenderTexture"/> where the quad will be drawn. This is the target for the blit operation.</param>
		/// <param name="mat">The <see cref="Material"/> used for rendering the quad. It defines how the quad will be shaded.</param>
		/// <param name="properties">A <see cref="MaterialPropertyBlock"/> containing properties to be sent to the shader.
		/// This allows for setting additional shader properties without modifying the original Material.</param>
		/// <param name="pass">An optional integer specifying which pass to use on the Material. 
		/// Default is 0, which refers to the first pass defined in the shader.</param>
		/// </summary>
		public static void BlitQuad(CommandBuffer commandBuffer, Texture src, RenderTexture dst, Material mat, MaterialPropertyBlock properties, int pass = 0)
		{
			if (src != null)
				properties.SetTexture(FluidShaderProperties._MainTex, src);
			commandBuffer.SetRenderTarget(dst);
			commandBuffer.DrawProcedural(Matrix4x4.identity, mat, pass, MeshTopology.Quads, 4, 1, properties);
		}

		internal static void BlitQuad(CommandBuffer commandBuffer, RenderTargetIdentifier dst, Material mat, MaterialPropertyBlock properties, int pass = 0)
		{
			commandBuffer.SetRenderTarget(dst);
			commandBuffer.DrawProcedural(Matrix4x4.identity, mat, pass, MeshTopology.Quads, 4, 1, properties);
		}

		/// <summary>
		/// A helper function to easily blit a quad with the specified source material data to a <see cref="RenderTexture"/>.
		/// </summary>
		/// <param name="commandBuffer">The <see cref="CommandBuffer"/> to which the drawing commands will be added.</param>
		/// <param name="src">The source texture to be used on the quad's surface. This can be null, 
		/// in which case the shader will not use a texture.</param>
		/// <param name="dst">The <see cref="RenderTexture"/> where the quad will be drawn. This is the target for the blit operation.</param>
		/// <param name="mat">The <see cref="Material"/> used for rendering the quad. It defines how the quad will be shaded.</param>
		/// <param name="properties">A <see cref="MaterialPropertyBlock"/> containing properties to be sent to the shader.
		/// This allows for setting additional shader properties without modifying the original Material.</param>
		/// <param name="pass">An optional integer specifying which pass to use on the Material. 
		/// Default is 0, which refers to the first pass defined in the shader.</param>
		/// </summary>
		public static void BlitQuad(CommandBuffer commandBuffer, int src, RenderTexture dst, Material mat, MaterialPropertyBlock properties, int pass = 0)
		{
			commandBuffer.SetGlobalTexture(FluidShaderProperties._MainTex, src);
			commandBuffer.SetRenderTarget(dst);
			commandBuffer.DrawProcedural(Matrix4x4.identity, mat, pass, MeshTopology.Quads, 4, 1, properties);
		}

		/// <summary>
		/// A helper function to easily blit a quad with the specified source material data to multiple <see cref="RenderTexture">RenderTextures</see>.
		/// </summary>
		/// <param name="commandBuffer">The <see cref="CommandBuffer"/> to which the drawing commands will be added.</param>
		/// <param name="src">The source texture to be used on the quad's surface. This can be null, 
		/// in which case the shader will not use a texture.</param>
		/// <param name="dst">The <see cref="RenderTargetIdentifier">RenderTextures</see> where the quad will be drawn. These are the targets for the blit operation.</param>
		/// <param name="depth">The <see cref="RenderTargetIdentifier">depth texture</see> where the quad will be drawn. 
		/// This can be <see cref="RenderTexture.depthBuffer"/> of a <see cref="RenderTexture"/> that has been created without a depth.</param>
		/// <param name="mat">The <see cref="Material"/> used for rendering the quad. It defines how the quad will be shaded.</param>
		/// <param name="properties">A <see cref="MaterialPropertyBlock"/> containing properties to be sent to the shader.
		/// This allows for setting additional shader properties without modifying the original Material.</param>
		/// <param name="pass">An optional integer specifying which pass to use on the Material. 
		/// Default is 0, which refers to the first pass defined in the shader.</param>
		public static void BlitQuad(CommandBuffer commandBuffer, Texture src, RenderTargetIdentifier[] dst, RenderTargetIdentifier depth, Material mat, MaterialPropertyBlock properties, int pass = 0)
		{
			if (src != null)
				properties.SetTexture(FluidShaderProperties._MainTex, src);
			commandBuffer.SetRenderTarget(dst, depth);
			commandBuffer.DrawProcedural(Matrix4x4.identity, mat, pass, MeshTopology.Quads, 4, 1, properties);
		}

	}
}