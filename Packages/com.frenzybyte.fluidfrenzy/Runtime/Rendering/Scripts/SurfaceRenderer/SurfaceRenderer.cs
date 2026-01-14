using System;
using UnityEngine;
using UnityEngine.Rendering;
#if FLUIDFRENZY_RUNTIME_HDRP_SUPPORT
using HDRPWaterSurface = UnityEngine.Rendering.HighDefinition.WaterSurface;
#endif
namespace FluidFrenzy
{
	/// <summary>
	/// <see cref="ISurfaceRenderer"/> defines a interface for rendering techniques aimed at height field surfaces.
	/// Implementing classes should provide specific algorithms and methods to visualize height maps and related surface data
	/// in different graphical contexts, such as terrain or fluid fields. This interface is designed to promote extensibility,
	/// allowing developers to introduce new rendering methods as needed while adhering to a standard approach for rendering
	/// surfaces.
	/// Currently there are three classes that extend this interface.
	/// <list type="bullet">
	/// 	<item>
	/// 		<term><c>MeshRenderer</c></term>
	/// 		<description>The implementation using standard <see cref="MeshRendererSurface"/> components.</description>
	/// 	</item>
	/// 	<item>
	/// 		<term><c>Mesh</c></term>
	/// 		<description>A simpler implementation using <see cref="MeshSurface"/>.</description>
	/// 	</item>
	/// 	<item>
	/// 		<term><c>GPULOD</c></term>
	/// 		<description>An implementation using a GPU-accelerated LOD system: <see cref="GPULODSurface"/>.</description>
	/// 	</item>
	/// </list>
	/// </summary>
	/// <remarks>
	/// All classes implementing this interface must provide functionality to clean up resources by overriding the
	/// <see cref="IDisposable.Dispose"/> method, ensuring that any graphics resources are properly disposed of.
	/// </remarks>
	public interface ISurfaceRenderer : IDisposable
	{
		/// <summary>
		/// Defines the method used to generate and render the visual surface of the surface.
		/// </summary>
		public enum RenderMode
		{
			/// <summary>
			/// Uses standard GameObjects with <see cref="MeshRenderer"/> components to draw the surface.
			/// </summary>
			/// <remarks>
			/// The surface is an evenly distributed, fixed-size grid. The grid can be subdivided into multiple blocks to improve performance via frustum culling. 
			/// GPU instancing is supported if enabled on the assigned material.
			/// </remarks>
			MeshRenderer,
			/// <summary>
			/// Uses <see cref="Graphics.RenderMesh"/> or <see cref="Graphics.RenderMeshInstanced"/> to render directly to the camera without creating GameObjects.
			/// </summary>
			/// <remarks>
			/// Functionally similar to <see cref="MeshRenderer"/> but avoids the overhead of managing GameObjects. 
			/// This method renders manually and natively supports GPU Instancing on the water shader.
			/// </remarks>
			DrawMesh,
			/// <summary>
			/// Uses a fully GPU-accelerated, distance-based Level of Detail (LOD) system.
			/// </summary>
			/// <remarks>
			/// This mode offers maximum performance for large surfaces. The geometry is drawn with high detail near the camera and lower detail in the distance.
			/// </remarks>
			GPULOD,
			/// <summary>
			/// Offloads the rendering to a Unity <see cref="HDRPWaterSurface"/> component.
			/// </summary>
			/// <remarks>
			/// The fluid simulation data is passed to the HDRP Water System rather than drawing a mesh directly.
			/// </remarks>
			HDRPWaterSurface
		}

		public static class Extensions
		{
			/// <summary>
			/// Tests if the current rendermode is supported on the current running platform.
			/// </summary>
			/// <param name="mode">The mode that is being requested</param>
			/// <param name="supportedMode">The fallback mode that will be used if the requested mode is not supported.</param>
			/// <returns>Whether the requested mode was supported or not.</returns>
			public static bool IsRenderModeSupported(in RenderMode mode, out RenderMode supportedMode)
			{
				if (mode == RenderMode.GPULOD)
				{
					if (!SystemInfo.supportsComputeShaders)
					{
						supportedMode = RenderMode.MeshRenderer;
						Debug.LogWarningFormat("Surface Render mode {0} is not supported on this platform. Falling back to {1}", mode, supportedMode);
						return false;
					}
				}
				supportedMode = mode;
				return true;
			}
		}

		/// <summary>
		/// Properties to be used to configure components that use <see cref="ISurfaceRenderer"/>. These properties determine the mesh quality and rendering mode of the surface.
		/// </summary>
		[Serializable]
		public struct RenderProperties
		{
			/// <summary>
			/// Contains settings used to bridge the fluid simulation data to the Unity HDRP Water System.
			/// </summary>
			[Serializable]
			public struct HDRPWaterSurfaceProperties
			{
#if FLUIDFRENZY_RUNTIME_HDRP_SUPPORT
				/// <summary>
				/// The target <see cref="HDRPWaterSurface"/> component that will receive the simulation data.
				/// </summary>
				public HDRPWaterSurface targetWaterSurface;
#else
				/// <summary>
				/// The target HDRP Water System component the simulation is to be applied. (Requires HDRP package).
				/// </summary>
				public MonoBehaviour targetWaterSurface;
#endif
				/// <summary>
				/// Controls the maximum amplitude of the Fluid Simulation used to encode/decode the height to/from 0-1 range
				/// </summary>
				public float amplitude;
				/// <summary>
				/// Controls the weight that the Fluid Simulation's velocity should be applied to the Large Current waves of the HDRP Water System.
				/// </summary>
				public float largeCurrent;
				/// <summary>
				/// Controls the weight that the Fluid Simulation's velocity should be applied to the Rupples of the HDRP Water System.
				/// </summary>
				public float ripples;
			}

			/// <summary> 
			/// The current version of the <see cref="RenderProperties"/> structure, used for compatibility and serialization.
			/// </summary>
			public static int Version = 1;
			/// <summary> 
			/// Default configuration for <see cref="RenderProperties"/>, including predefined resolution and block settings.
			/// </summary>
			public static RenderProperties Default = new RenderProperties(RenderMode.DrawMesh, new Vector2(500, 500), new Vector2Int(512, 512), new Vector2Int(8, 8), new Vector2Int(8, 8), 1, new Vector2Int(0, 15));

			/// <summary>
			/// The method used for generating and rendering the fluid surface geometry.
			/// </summary>
			/// <remarks>
			/// <list type="bullet">
			/// 	<item>
			/// 		<term><c>MeshRenderer</c></term>
			/// 		<description>Uses standard GameObjects with <see cref="MeshRenderer"/> components. Best for simple setups where standard object culling is sufficient.</description>
			/// 	</item>
			/// 	<item>
			/// 		<term><c>DrawMesh</c></term>
			/// 		<description>Uses <see cref="Graphics.RenderMesh"/> to avoid GameObject overhead. Supports GPU Instancing.</description>
			/// 	</item>
			/// 	<item>
			/// 		<term><c>GPULOD</c></term>
			/// 		<description>Draws the surface using a GPU-accelerated LOD system. Best for large-scale oceans or lakes.</description>
			/// 	</item>
			/// 	<item>
			/// 		<term><c>HDRPWaterSurface</c></term>
			/// 		<description>Bridges the simulation data to a Unity <see cref="HDRPWaterSurface"/> component (Requires HDRP).</description>
			/// 	</item>
			/// </list>
			/// </remarks>
			public RenderMode renderMode;

			/// <summary>
			/// The total world-space size (X and Z) of the rendered surface.
			/// </summary>
			public Vector2 dimension;

			/// <summary>
			/// The vertex resolution of the surface's base grid mesh.
			/// </summary>
			/// <remarks>
			/// For the most accurate visualization, it is recommended to match this value to the source heightmap resolution.
			/// </remarks>
			public Vector2Int meshResolution;

			/// <summary>
			/// The number of subdivisions (blocks) to split the rendering mesh into along the X and Z axes.
			/// </summary>
			/// <remarks>
			/// Subdividing the mesh improves GPU performance by allowing the camera to cull blocks that are outside the view frustum.
			/// </remarks>
			public Vector2Int meshBlocks;

			/// <summary>
			/// The vertex resolution of individual LOD patches when using <see cref="RenderMode.GPULOD"/>.
			/// </summary>
			public Vector2Int lodResolution;

			/// <summary>
			/// The number of iterations the Quadtree traversal algorithm performs per frame when using <see cref="RenderMode.GPULOD"/>.
			/// </summary>
			/// <remarks>
			/// Higher values resolve the surface quality faster during camera movement but may reduce performance.
			/// </remarks>
			[Range(1, 8)]
			public int traverseIterations;

			/// <summary>
			/// The range of allowable LOD levels, where X is the minimum level and Y is the maximum level.
			/// </summary>
			public Vector2Int lodMinMax;

			/// <summary>
			/// Configuration settings for bridging this simulation's data to an external <see cref="HDRPWaterSurface"/>.
			/// </summary>
			public HDRPWaterSurfaceProperties hdrpWaterSurface;

			public int version;

			/// <summary>
			/// Parameterized constructor which allows initialization with specific values.
			/// The constructor ensures that traverseIterations is clamped within a manageable range.
			/// </summary>
			/// <param name="renderMode"><inheritdoc cref="renderMode" path="/summary"/></param>
			/// <param name="dimension"><inheritdoc cref="dimension" path="/summary"/></param>
			/// <param name="meshResolution"><inheritdoc cref="meshResolution" path="/summary"/></param>
			/// <param name="meshBlocks"><inheritdoc cref="meshBlocks" path="/summary"/></param>
			/// <param name="lodResolution"><inheritdoc cref="lodResolution" path="/summary"/></param>
			/// <param name="traverseIterations"><inheritdoc cref="traverseIterations" path="/summary"/></param>
			/// <param name="lodMinMax"><inheritdoc cref="lodMinMax" path="/summary"/></param>
			/// <param name="version"><inheritdoc cref="version" path="/summary"/></param>
			public RenderProperties(RenderMode renderMode, Vector2 dimension, Vector2Int meshResolution,
									Vector2Int meshBlocks, Vector2Int lodResolution, int traverseIterations, Vector2Int lodMinMax, int version = 0)
			{
				this.renderMode = renderMode;
				this.dimension = dimension;
				this.meshResolution = meshResolution;
				this.meshBlocks = meshBlocks;
				this.lodResolution = lodResolution;
				this.traverseIterations = Mathf.Clamp(traverseIterations, 1, 8); // Ensures traverseIterations is within range
				this.lodMinMax = lodMinMax;
				this.version = version;
				this.hdrpWaterSurface = new HDRPWaterSurfaceProperties();
			}
		}

		/// <summary>
		/// Encapsulates settings used for configuring and initializing <see cref="ISurfaceRenderers"/>.
		/// </summary>
		public struct SurfaceDescriptor
        {
			/// <summary><inheritdoc cref="RenderProperties.meshResolution" path="/summary"/></summary>
			public Vector2Int meshResolution;
			/// <summary><inheritdoc cref="RenderProperties.dimension" path="/summary"/></summary>
			public Vector2 dimension;
			/// <summary>The scale that will be applied to the height value in the surface's height field.</summary>
			public float heightScale;
			/// <summary>The maximum height the surface will be. This is used for the culling bounds of the meshes.</summary>
			internal float maxHeight;
			/// <summary>
			/// Specifies which channels of the heightmap to read 1 is read, 0 is ignore. 
			/// The result is accumulated with the following formula: dot(heightTexel, heightmapMask)
			/// </summary>
			public Vector4 heightmapMask;
			/// <summary> The minimum and maximum LOD levels that can be selected for the surface. lodMinMax.x(min) lodMinMax.y(max)</summary>
			public Vector2Int lodMinMax;
		}

		/// <summary> Specifies if the currently surface uses a custom rendering method using Unity's Graphics/CommandBuffer API or not.</summary>
		public bool customRender { get; }
		
		/// <summary>
		/// Sets the correct material parameters and keywords required to render this <see cref="ISurfaceRenderer"/> implementation.
		/// </summary>
		/// <param name="material">The material that will be used to render the implemented see cref="ISurfaceRenderer"/>.</param>
		public void SetupMaterial(Material material);

		/// <summary>
		/// Update the <see cref="ISurfaceRenderer"/> with new <see cref="SurfaceDescriptor"/> settings.
		/// </summary>
		/// <param name="surfaceDesc">The new <see cref="SurfaceDescriptor"/> that will be used for rendering.</param>
		public void UpdateSurfaceDescriptor(SurfaceDescriptor surfaceDesc);

		/// <summary>
		/// A function that executes code to be called when the <see cref="ISurfaceRenderer"/> is enabled.
		/// </summary>
		/// <remarks>This function should be called places like <see cref="MonoBehaviour.OnEnable"/> or any other system's OnEnable logic.</remarks>
		public void OnEnable();

		/// <summary>
		/// A function that executes code to be called when the <see cref="ISurfaceRenderer"/> is disabled.
		/// </summary>
		/// <remarks>This function should be called places like <see cref="MonoBehaviour.OnDisable"/> or any other system's OnDisable logic.</remarks>
		public void OnDisable();

		/// <summary>
		/// Implement this function if the <see cref="ISurfaceRenderer"/> implementation uses <see cref="CommandBuffer"/> to implement rendering functionality.
		/// Call this function to register CommandBuffers to the specified <see cref="Camera"/>.
		/// Places where this function can be called are for example <see cref="Camera.onPreCull"/> or <see cref="Camera.onPreRender"/>, or during initialization.
		/// </summary>
		/// <param name="camera">The camera to which the CommandBuffers should be registered.</param>
		public void AddCommandBuffers(Camera camera);

		/// <summary>
		/// Implement this function if the <see cref="ISurfaceRenderer"/> implementation uses <see cref="CommandBuffer"/> to implement rendering functionality.
		/// Call this function to remove CommandBuffers from the specified <see cref="Camera"/>.
		/// Places where this function can be called are for example <see cref="Camera.onPostRender"/>, or during disposing.
		/// </summary>
		/// <param name="camera">The camera to which the CommandBuffers should be registered.</param>
		public void RemoveCommandBuffers(Camera camera);

		/// <summary>
		/// Implement this function if the <see cref="ISurfaceRenderer"/> requires custom culling.
		/// This function can be called from <see cref="Camera.onPreCull"/>to apply culling for the currently active camera.
		/// </summary>
		/// <param name="transform">The <see cref="Transform"/> of the geometry being rendered.</param>
		/// <param name="camera">The current camera that culling should be performance on.</param>
		/// <param name="heightmap">The heightmap the <see cref="ISurfaceRenderer"/> is rendering with.</param>
		/// <param name="traverseIterations">The amount of iterations the <see cref="QuadTreeGPU"/> should apply during this pass.</param>
		public void PreRender(ScriptableRenderContext context, Transform transform, Camera camera, Texture heightmap, int traverseIterations = 1, bool renderShadows = false);


		/// <summary>
		/// Implement this function if the <see cref="ISurfaceRenderer"/> requires custom culling.
		/// This function can be called from <see cref="Camera.onPreCull"/>to apply culling for the currently active camera.
		/// </summary>
		/// <param name="transform">The <see cref="Transform"/> of the geometry being rendered.</param>
		/// <param name="camera">The current camera that culling should be performance on.</param>
		/// <param name="heightmap">The heightmap the <see cref="ISurfaceRenderer"/> is rendering with.</param>
		/// <param name="traverseIterations">The amount of iterations the <see cref="QuadTreeGPU"/> should apply during this pass.</param>
		public void PreRenderCamera(Transform transform, Camera camera, Texture heightmap, int traverseIterations = 1);

		/// <summary>
		/// Implement this function if the <see cref="ISurfaceRenderer"/> requires custom culling for a directional shadow <see cref="Light"/>.
		/// This function can be called from <see cref="Camera.onPreCull"/>to apply culling for the currently active camera and directional shadow <see cref="Light"/>.
		/// </summary>
		/// <param name="commandBuffer">The commandbuffer that is assigned to the <see cref="Light"/> which the GPU Culling should be applied to.</param>
		/// <param name="transform">The <see cref="Transform"/> of the geometry being rendered.</param>
		/// <param name="camera">The current camera that culling should be performance on.</param>
		/// <param name="light">The directional <see cref="Light"/> that the geometry should be culled for.</param>
		public void CullShadowLight(CommandBuffer commandBuffer, Transform transform, Camera camera, Light light);

		/// <summary>
		/// Implement this function if the <see cref="ISurfaceRenderer"/> requires custom rendering like <see cref="Graphics.RenderMesh"/>.
		/// This function can be called from <see cref="Camera.onPreCull"/>to apply culling for the currently active camera and directional shadow <see cref="Light"/>.
		/// </summary>
		/// <param name="transform">The <see cref="Transform"/> of the geometry being rendered.</param>
		/// <param name="material">The <see cref="Material"/> of the geometry should use for shading when rendering.</param>
		/// <param name="properties">A <see cref="MaterialPropertyBlock"/> with extra rendering properties that dynamically set to render the geometry.</param>
		/// <param name="camera">The current camera that culling should be performance on. Pass null to render to all cameras.</param>
		public void Render(Transform transform, Material material, MaterialPropertyBlock properties, Camera camera = null);



		/// <summary>
		/// Implement this function if the <see cref="ISurfaceRenderer"/> requires custom rendering like <see cref="Graphics.RenderMesh"/>.
		/// This function can be called from <see cref="Camera.onPreCull"/>to apply culling for the currently active camera and directional shadow <see cref="Light"/>.
		/// </summary>
		/// <param name="transform">The <see cref="Transform"/> of the geometry being rendered.</param>
		/// <param name="material">The <see cref="Material"/> of the geometry should use for shading when rendering.</param>
		/// <param name="properties">A <see cref="MaterialPropertyBlock"/> with extra rendering properties that dynamically set to render the geometry.</param>
		/// <param name="camera">The current camera that culling should be performance on. Pass null to render to all cameras.</param>
		public void Render(CommandBuffer cmd, Matrix4x4 localToWorld, Material material, MaterialPropertyBlock properties, int pass = 0, Camera camera = null);


	}
}