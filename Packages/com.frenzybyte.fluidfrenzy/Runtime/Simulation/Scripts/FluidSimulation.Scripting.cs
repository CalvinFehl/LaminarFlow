using System;
using System.Collections.Generic;
using UnityEngine;

namespace FluidFrenzy
{
	public partial class FluidSimulation : MonoBehaviour
	{
		public enum FluidSimulationType
		{
			Flux,
			Flow
		}

		/// <summary>
		/// Choose how the dimenions of the fluid simulation and renderer should be calculated.
		/// </summary>
		public enum DimensionMode
		{
			/// <summary>
			/// Allows the user to set custom bounds size which is used to calculate the <see cref="cellWorldSize"/> based on the <see cref="dimension"/> and <see cref="FluidSimulationSettings.numberOfCells"/>. Use this for square simulations to automatically calculate the correct <see cref="cellWorldSize"/>.
			/// </summary>
			Bounds,
			/// <summary>
			/// Allows the user to set the size of each <see cref="FluidSimulationSettings.numberOfCells"/> cell to determine the size of <see cref="dimension"/>, use this method for none square simulations to automatically get the bounds.
			/// </summary>
			CellSize
		}

		/// <summary>
		/// Returns which simulation type this object is.
		/// </summary>
		public FluidSimulationType simulationType;

		/// <summary>
		/// The number of border cells applied to each of the velocity field borders.
		/// Ghost cells are simulated cells that will not be rendered by final data. They are purely used for simulating extra data for neighbouring tiles or border interactions.
		/// </summary>
		public int velocityGhostCells { get; protected set; } = 1;
		/// <summary>
		/// The total number of border cells applied to the velocity field borders.
		/// Ghost cells are simulated cells that will not be rendered by final data. They are purely used for simulating extra data for neighbouring tiles or border interactions.
		/// </summary>
		public int velocityGhostCells2 { get; protected set; } = 2;

		/// <summary>
		/// The number of cells that will border the base simulation. The base simulation uses 1 pixel border on each side to simulation closed/open borders and tiling.
		/// Ghost cells are simulated cells that will not be rendered by final data. They are purely used for simulating extra data for neighbouring tiles or border interactions.
		/// </summary>
		public Vector2Int ghostCells { get; protected set; } = new Vector2Int(1, 1);
		/// <summary>
		/// The number of cells that will border the base simulation. The base simulation uses 1 pixel border on each side to simulation closed/open borders and tiling.
		/// Ghost cells are simulated cells that will not be rendered by final data. They are purely used for simulating extra data for neighbouring tiles or border interactions.
		/// </summary>
		public Vector2Int ghostCells2 { get; protected set; } = new Vector2Int(2, 2);

		/// <summary>
		/// Specifies te number of cells that will be rendered.
		/// To match the Unity Terrain this is <see cref="FluidSimulationSettings.numberOfCells"/> + 1. 
		/// As Unity runs on POT textures + 1 and this needs to be matched to prevent missmatching with the <see cref="Terrain"/>.
		/// </summary>
		public Vector2Int numRenderCells { get; protected set; } = Vector2Int.zero;
		/// <summary>
		/// Specifies the number of cells that the simulation will use to simulate fluids.
		/// This is calculated by taking the <see cref="numRenderCells"/> + <see cref="ghostCells"/> to account for the <see cref="Terrain"/> and ghost cells required for tiling and borders.
		/// </summary>
		public Vector2Int numSimulationCells { get; protected set; } = Vector2Int.zero;

		/// <summary>
		/// The world space bounds of the <see cref="FluidSimulation"/>
		/// </summary>
		public Bounds bounds { get; protected set; } = new Bounds();

		//Render Data accessors
		/// <summary>
		/// The currently active rendering data containing the simulation's height and velocity to be used for rendering a <see cref="FluidRenderer"/>/
		/// </summary>
		public RenderTexture fluidRenderData { get { return m_activeHeightVelocityTexture; } private set { } }
		/// <summary>
		/// The previous <see cref="Step"/> active rendering data containing the simulation's height and velocity to be used for rendering a <see cref="FluidRenderer"/>/
		/// </summary>
		public RenderTexture prevFluidRenderData { get { return m_nextHeightVelocityTexture; } private set { } }
		/// <summary>
		/// A simulation space normal map calculated based on the <see cref="terrainHeight"/> + <see cref="fluidHeight"/>
		/// </summary>
		public RenderTexture normalTexture { get { return m_normalMap; } private set { } }

		//Velocity data accessors
		/// <summary>
		/// The currently active velocity field of the simulation.
		/// This velocity field contains extra simulation space around its borders based on <see cref="velocityGhostCells"/>.
		/// To use this as rendering data combine it with <see cref="velocityTextureST"/>
		/// </summary>
		public RenderTexture velocityTexture { get { return m_activeVelocity; } private set { } }
		/// <summary>
		/// A scale and transform parameters for the <see cref="velocityTexture"/> when used as rendering data, multiply the uv by .xy and add the .zw components to get a correcly sampled texture.
		/// </summary>
		public Vector4 velocityTextureST { get { return m_velocityTextureST; } private set { } }
		/// <summary>
		/// A copy of the supplied terrain used as the base height for the <see cref="FluidSimulation"/>. 
		/// This is a more read/write optimized version of the original data for better performance and safer modifications so that the source data will not get corrupted.
		/// </summary>
		public RenderTexture terrainHeight { get { return m_terrainHeight; } private set { } }
		/// <summary>
		/// A scale and transform parameters for the <see cref="terrainHeight"/> when used as rendering data, multiply the uv by .xy and add the .zw components to get a correcly sampled texture.
		/// </summary>
		public Vector4 terrainTextureST { get { return m_terrainTextureST; } private set { } }
		/// <summary>
		/// The current active height of the <see cref="FluidSimulation"/>.
		/// </summary>
		public RenderTexture fluidHeight { get { return m_activeWaterHeight; } private set { } }
		/// <summary>
		/// The state of the next <see cref="FluidSimulation"/> height.
		/// </summary>
		public RenderTexture nextFluidHeight { get { return m_nextWaterHeight; } private set { } }
		/// <summary>
		/// A buffer containing the height of all registered <see cref="FluidSimulationObstacle"/>.
		/// </summary>
		public RenderTexture obstaclesHeight { get { return m_obstacleHeight; } private set { } }
		/// <summary>
		/// Specifies of the <see cref="FluidSimulation"/> is currently using multi layered fluids.
		/// </summary>
		public bool multiLayeredFluid { get; protected set; } = false;

		/// <summary>
		/// A quick accessor to <see cref="FluidSimulation.clipHeight"/> for this <see cref="FluidSimulation"/>.
		/// </summary>
		public float clipHeight { get { return settings.clipHeight; } private set { } }

		/// <summary>
		/// Returns the size that the velocity buffer should be in a <see cref="FluidSimulation"></see>.
		/// </summary>
		protected internal virtual Vector2Int velocityTextureSize { get { return settings.numberOfCells; } protected set { } }

		/// <summary>
		/// Returns the advection scale for this <see cref="FluidSimulation"/></see>.
		/// </summary>
		protected internal virtual float advectionScale { get { return 1; } protected set { } }

		/// <summary>
		/// Returns the velocity scale for this <see cref="FluidSimulation"/></see>.
		/// </summary>
		protected internal virtual float velocityScale { get { return 1; } protected set { } }


		/// <summary>
		/// Adds a new <see cref="FluidLayer"/> component to the <see cref="FluidSimulation"/>.
		/// </summary>
		/// <typeparam name="T">Any type deriving from <see cref="FluidLayer"/></typeparam>
		/// <returns>The newly created <see cref="FluidLayer"/></returns>
		public FluidLayer AddFluidLayer<T>() where T : FluidLayer
		{
			T layer = gameObject.AddComponent<T>();
			extensionLayers.Add(layer);
			return layer;
		}

		/// <summary>
		/// Adds a new <see cref="FluidLayer"/> component to the <see cref="FluidSimulation"/>.
		/// </summary>
		/// <param name="type">Any type deriving from <see cref="FluidLayer"/></param>
		/// <returns>The newly created <see cref="FluidLayer"/></returns>
		public FluidLayer AddFluidLayer(Type type)
		{
			FluidLayer layer = gameObject.AddComponent(type) as FluidLayer;
			extensionLayers.Add(layer);
			return layer;
		}

		/// <summary>
		/// Get a <see cref="FluidLayer"/> of type <typeparamref name="T"/> currently attached to the <see cref="FluidSimulation"/>.
		/// </summary>
		/// <typeparam name="T">Any type deriving from <see cref="FluidLayer"/></typeparam>
		/// <returns>A currently attached to <see cref="FluidLayer"/> of type <typeparamref name="T"/></returns>
		public T GetFluidLayer<T>() where T : FluidLayer
		{
			return (T)GetFluidLayer(typeof(T));
		}

		/// <summary>
		/// Get a <see cref="FluidLayer"/> of type <typeparamref name="Type"/> currently attached to the <see cref="FluidSimulation"/>.
		/// </summary>
		/// <param name="type">Any type deriving from <see cref="FluidLayer"/></param>
		/// <returns>A currently attached to <see cref="FluidLayer"/> of type <typeparamref name="type"/></returns>
		public FluidLayer GetFluidLayer(Type type)
		{
			foreach (FluidLayer l in extensionLayers)
			{
				if (l.GetType() == type)
					return l;
			}
			return null;
		}

		/// <summary>
		/// Get a <see cref="FluidLayer"/> of type <typeparamref name="layer"/> currently attached to the <see cref="FluidSimulation"/>.
		/// </summary>
		/// <param name="type">Any type deriving from <see cref="FluidLayer"/></param>
		/// <returns>A currently attached to <see cref="FluidLayer"/> of type <typeparamref name="type"/></returns>
		public FluidLayer GetFluidLayer(FluidLayer layer)
		{
			return GetFluidLayer(layer.GetType());
		}
		/// <summary>
		/// Get a <see cref="FluidLayer"/> of type <typeparamref name="layer"/> currently attached to the <see cref="FluidSimulation"/>.
		/// </summary>
		/// <param name="type">The fluid layer to be returned <see cref="FluidLayer"/></param>
		/// <returns>If the layer was found</returns>
		public bool TryGetFluidLayer<T>(out T layer) where T : FluidLayer
		{
			layer = GetFluidLayer<T>();
			return layer != null;
		}

		/// <summary>
		/// Notifies the <see cref="FluidSimulation"/> that the it's settings have changed.
		/// </summary>
		public void MarkSettingsChanged(bool changed)
		{
			m_settingsChanged = changed;
		}

		/// <summary>
		/// Notifies the <see cref="FluidSimulation"/> to regenerated it's obstacle mask.
		/// </summary>
		public void RequestObstacleUpdate(bool update)
		{
			m_updateObstacles = update;
		}

		/// <summary>
		/// Assign a (new) Terrain to the <see cref="FluidSimulation"/> for the fluid to flow over.
		/// </summary>
		/// <param name="terrain">The terrain to use as the base collider for the fluid. 
		/// This can be a <see cref="Terrain"/>, <see cref="SimpleTerrain"/>, <see cref="TerraformTerrain"/> or <see cref="Texture2D"/></param>
		/// <param name="updateDimensions">if true, the dimensions of the <see cref="FluidSimulation"/> will be updated to match the dimensions of the <paramref name="terrain"/></param>
		/// <param name="updateTransform">if true, the transform of the <see cref="FluidSimulation"/> will be updated to match the position of the <paramref name="terrain"/></param>
		public void SetTerrain(object terrain, bool updateDimensions = true, bool updateTransform = true)
		{
			if (terrain == null)
			{
				Debug.LogError("terrain is null. Pass in a valid terrain");
				return;
			}

			if (terrain is Terrain)
			{
				unityTerrain = terrain as Terrain;
				terrainType = TerrainType.UnityTerrain;

				if (updateDimensions)
					dimension = new Vector2(unityTerrain.terrainData.size.x, unityTerrain.terrainData.size.z);
			}
			else if (terrain is SimpleTerrain)
			{
				simpleTerrain = terrain as SimpleTerrain;
				terrainType = TerrainType.SimpleTerrain;
				if (updateDimensions)
					dimension = simpleTerrain.surfaceProperties.dimension;
			}
			else if (terrain is Texture2D)
			{
				textureHeightmap = terrain as Texture2D;
				terrainType = TerrainType.Heightmap;
			}
			else if (terrain is MeshCollider)
			{
				meshCollider = terrain as MeshCollider;
				terrainType = TerrainType.MeshCollider;
			}

			if (updateDimensions)
			{
				SyncDimensions();
			}

			if (updateTransform)
			{
				SyncTransform();
			}
		}

		/// <summary>
		/// Synchronizes the <see cref="FluidSimulation"/> dimensions to the assigned terrain's dimensions.
		/// </summary>
		public void SyncDimensions()
		{
			if (terrainType == TerrainType.UnityTerrain && unityTerrain)
			{
				dimension = new Vector2(unityTerrain.terrainData.size.x, unityTerrain.terrainData.size.z);
			}
			else if (terrainType == TerrainType.SimpleTerrain && simpleTerrain)
			{
				dimension = simpleTerrain.surfaceProperties.dimension;
			}			
			else if (terrainType == TerrainType.MeshCollider && meshCollider)
			{
				dimension = new Vector2(meshCollider.bounds.size.x, meshCollider.bounds.size.z);
			}
			bounds = CalculateBounds();
		}

		/// <summary>
		/// Synchronizes the <see cref="FluidSimulation"/> world space position to the assigned terrain's <see cref="Transform"/> and dimensions.
		/// </summary>
		public void SyncTransform()
		{
			if (terrainType == TerrainType.UnityTerrain && unityTerrain)
			{
				dimension = new Vector2(unityTerrain.terrainData.size.x, unityTerrain.terrainData.size.z);
				transform.position = unityTerrain.transform.position + new Vector3(dimension.x, 0, dimension.y) * 0.5f;
			}
			else if (terrainType == TerrainType.SimpleTerrain && simpleTerrain)
			{
				transform.position = simpleTerrain.transform.position;
			}
			else if (terrainType == TerrainType.MeshCollider && meshCollider)
			{
				Vector3 boundsPosition = meshCollider.bounds.center;
				transform.position = new Vector3(boundsPosition.x, transform.position.y, boundsPosition.z);
			}
			bounds = CalculateBounds();
		}

		/// <summary>
		/// Calculates the bounds of the <see cref="FluidSimulation"./>
		/// </summary>
		/// <returns><see cref="Bounds"/> for based of the <see cref="Transform"/>, <see cref="dimension"/> and, <see cref="heightmapScale"/>.</returns>
		public Bounds CalculateBounds()
		{
			GetBoundsHeight(out float boundsHeight);
			return new Bounds(transform.position + Vector3.up * boundsHeight * 0.5f, new Vector3(dimension.x, boundsHeight, dimension.y));
		}
	}
}