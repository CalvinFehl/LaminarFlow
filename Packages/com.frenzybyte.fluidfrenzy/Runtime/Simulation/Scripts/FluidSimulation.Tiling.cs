using System.Runtime.CompilerServices;

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FluidFrenzy
{
	internal static class BoundarySidesExtensions
	{
		internal static FluidSimulation.BoundarySides GetOpposite(this FluidSimulation.BoundarySides side)
		{
			// Calculate the opposite side using modular arithmetic
			return (FluidSimulation.BoundarySides)((int)side + (1 - ((int)(side) % 2) * 2));
		}
	}

	public partial class FluidSimulation : MonoBehaviour
	{
		/// <summary>
		/// Defines the sides of the simulation domain used for boundary conditions and tiling neighbors.
		/// </summary>
		public enum BoundarySides
		{
			/// <summary>The left (negative X) boundary side.</summary>
			Left,
			/// <summary>The right (positive X) boundary side.</summary>
			Right,
			/// <summary>The bottom (negative Z/Y) boundary side.</summary>
			Bottom,
			/// <summary>The top (positive Z/Y) boundary side.</summary>
			Top,
			/// <summary>A utility member representing the total count of boundary sides.</summary>
			Max
		}

		/// <summary>
		/// Internal array of 2D grid offsets corresponding to the <see cref="BoundarySides"/> enum order.
		/// </summary>
		private static Vector2Int[] kGridOffset = { Vector2Int.left, Vector2Int.right, Vector2Int.down, Vector2Int.up };

		[SerializeField, HideInInspector]
		/// <summary>
		/// An array storing references to the four neighboring <see cref="FluidSimulation"/> instances in a tiled setup.
		/// </summary>
		/// <remarks>
		/// The array is indexed based on the <see cref="BoundarySides"/> enum order (Left, Right, Bottom, Top). This is automatically populated when using the Tiled Simulation Gizmos or can be set manually in the Inspector.
		/// </remarks>
		protected FluidSimulation[] m_neighbours = new FluidSimulation[4];

		/// <summary>
		/// Creates a new neighboring <see cref="FluidSimulation"/> tile and connects it to the specified side of this <see cref="FluidSimulation"/>'s group.
		/// </summary>
		/// <param name="side">
		/// The <see cref="BoundarySides"/> enum value to which the new <see cref="FluidSimulation"/> tile will be attached. 
		/// This specifies the relative position of the new tile in relation to the current <see cref="FluidSimulation"/>.
		/// </param>
		/// <returns>The instance of the newly created <see cref="FluidSimulation"/> tile, fully configured and connected to the specified boundary side.
		/// </returns>
		public FluidSimulation AddNeighbour(BoundarySides side)
		{
			Dictionary<Vector2Int, FluidSimulation> fluidSimMap = GetSimulationGroupGrid();
			Bounds bounds = CalculateBounds();

			Vector3 offset = Vector3.zero;
			Vector3 size = bounds.size;

			if (side == BoundarySides.Left)
				offset = new Vector3(-size.x, 0, 0);
			else if (side == BoundarySides.Right)
				offset = new Vector3(size.x, 0, 0);			
			else if (side == BoundarySides.Bottom)
				offset = new Vector3(0, 0, -size.z);			
			else if (side == BoundarySides.Top)
				offset = new Vector3(0, 0, size.z);
			Vector3 pos = transform.position + offset;

			return AddNeighbour(fluidSimMap, side, pos);
		}

		/// <summary>
		/// Creates a new neighboring <see cref="FluidSimulation"/> tile and connects it to the specified side of this <see cref="FluidSimulation"/>'s group.
		/// </summary>
		/// <param name="fluidSimMap">
		/// A dictionary mapping coordinates (as Vector2Int) to their corresponding <see cref="FluidSimulation"/> instances within the group. 
		/// This dictionary can be generated using the <see cref="GetSimulationGroupGrid/> method.
		/// </param>
		/// <param name="side">
		/// The <see cref="BoundarySides"/> enum value to which the new <see cref="FluidSimulation"/> tile will be attached. 
		/// This specifies the relative position of the new tile in relation to the current <see cref="FluidSimulation"/>.
		/// </param>
		/// <param name="pos">
		/// The world space position where the new <see cref="FluidSimulation"/> tile will be instantiated. 
		/// This is typically provided in the format of a Vector3 coordinate.
		/// </param>
		/// <returns>The instance of the newly created <see cref="FluidSimulation"/> tile, fully configured and connected to the specified boundary side.
		/// </returns>
		public FluidSimulation AddNeighbour(Dictionary<Vector2Int, FluidSimulation> fluidSimMap, BoundarySides side, Vector3 pos)
		{
			FluidSimulation parentSim = this;
			GameObject newNeighbour = new GameObject($"{parentSim.name}_{pos}", (this.GetType()));
			SceneManager.MoveGameObjectToScene(newNeighbour, parentSim.gameObject.scene);
			newNeighbour.transform.SetParent(parentSim.transform.parent, false);
			newNeighbour.transform.position = pos;

			if (newNeighbour.TryGetComponent(out FluidSimulation neighbourSim))
			{
				neighbourSim.gridPos = parentSim.gridPos + kGridOffset[(int)side];
				fluidSimMap.Add(neighbourSim.gridPos, neighbourSim);
#if UNITY_EDITOR
				List<FluidSimulation> simsToUndo = new List<FluidSimulation>(fluidSimMap.Values);
				simsToUndo.Add(parentSim);
				Undo.RecordObjects(simsToUndo.ToArray(), "FluidSimNeighbours");
#endif
				parentSim.SetNeighbour(side, neighbourSim);

				GatherNeighbours(neighbourSim, fluidSimMap, 0);

				neighbourSim.groupID = parentSim.groupID;
				neighbourSim.settings = parentSim.settings;
				neighbourSim.dimension = parentSim.dimension;
				neighbourSim.initialFluidHeight = parentSim.initialFluidHeight;
				neighbourSim.heightmapScale = parentSim.heightmapScale;

				neighbourSim.terrainType = parentSim.terrainType;
				neighbourSim.simpleTerrain = parentSim.simpleTerrain;
				neighbourSim.textureHeightmap = parentSim.textureHeightmap;

				if (neighbourSim.terrainType == TerrainType.UnityTerrain && parentSim.unityTerrain)
				{
					if (side == BoundarySides.Left)
						neighbourSim.unityTerrain = parentSim.unityTerrain.leftNeighbor;
					else if (side == BoundarySides.Right)
						neighbourSim.unityTerrain = parentSim.unityTerrain.rightNeighbor;
					else if (side == BoundarySides.Top)
						neighbourSim.unityTerrain = parentSim.unityTerrain.topNeighbor;
					else if (side == BoundarySides.Bottom)
						neighbourSim.unityTerrain = parentSim.unityTerrain.bottomNeighbor;
				}

				FluidRenderer parentRenderer = parentSim.GetComponentInChildren<FluidRenderer>();
				FluidRenderer neighbourRenderer = null;
				if (parentRenderer)
				{
					GameObject newFluidRenderer = new GameObject("FluidRenderer");
					newFluidRenderer.transform.SetParent(newNeighbour.transform, false);
					Type derivedType = parentRenderer.GetType();
					neighbourRenderer = newFluidRenderer.AddComponent(derivedType) as FluidRenderer;
					neighbourRenderer.CopyFrom(parentRenderer);
					neighbourRenderer.simulation = neighbourSim;
				}

				foreach (FluidLayer parentLayer in parentSim.extensionLayers)
				{
					Type derivedType = parentLayer.GetType();
					FluidLayer neighbourLayer = neighbourSim.AddFluidLayer(derivedType);
					neighbourLayer.CopyFrom(parentLayer);

					if (neighbourRenderer)
					{
						if (neighbourLayer is FluidFlowMapping flowmapping)
							neighbourRenderer.flowMapping = flowmapping;
						else if (neighbourLayer is FoamLayer foamLayer && neighbourRenderer is WaterSurface waterSurface)
							waterSurface.foamLayer = foamLayer;
					}

				}
				return neighbourSim;
			}
			return null;
		}

		/// <summary>
		/// Disconnect the neighbour link <see cref="FluidSimulation"/>.
		/// </summary>
		/// <param name="side">
		/// The <see cref="BoundarySides"/> enum value to disconnect the <see cref="FluidSimulation"/> neighbour. 
		public void DisconnectNeighbour(BoundarySides side)
		{
			FluidSimulation neighbour = m_neighbours[(int)side];
			neighbour.m_neighbours[(int)side.GetOpposite()] = null;
			m_neighbours[(int)side] = null;
		}

		/// <summary>
		/// Collects and establishes connections between this <see cref="FluidSimulation"/> instance and its direct neighbors. 
		/// This method ensures that the fluid simulation accurately recognizes all adjacent entities 
		/// within its simulation grid, facilitating proper fluid interactions and behaviors.
		/// </summary>
		/// <param name="fluidSim">The <see cref="FluidSimulation"/> instance for which to gather direct neighbors.</param>
		public static void GatherNeighbours(FluidSimulation fluidSim)
		{
			if (!fluidSim)
				return;
			GatherNeighbours(fluidSim, fluidSim.GetSimulationGroupGrid(), 0);
		}

		internal static void GatherNeighbours(FluidSimulation fluidSim, Dictionary<Vector2Int, FluidSimulation> fluidSimMap, int depth)
		{
			if (!fluidSim || depth > 1)
				return;

			fluidSim.GetNeighboursFromMap(fluidSimMap, out FluidSimulation left, out FluidSimulation right, out FluidSimulation top, out FluidSimulation bottom);
			fluidSim.SetNeighbours(left, right, top, bottom);
			GatherNeighbours(left, fluidSimMap, depth + 1);
			GatherNeighbours(right, fluidSimMap, depth + 1);
			GatherNeighbours(top, fluidSimMap, depth + 1);
			GatherNeighbours(bottom, fluidSimMap, depth + 1);
		}

		/// <summary>
		/// Collects and establishes connections between this <see cref="FluidSimulation"/> instance and its direct neighbors. 
		/// This method ensures that the fluid simulation accurately recognizes all adjacent entities 
		/// within its simulation grid, facilitating proper fluid interactions and behaviors.
		/// </summary>
		public void GatherNeighbours()
		{
			GatherNeighbours(this, GetSimulationGroupGrid(), 0);
		}

		/// <summary>
		/// Establishes a relationship between the current <see cref="FluidSimulation"/> and a specified neighboring <see cref="FluidSimulation"/>.
		/// This method sets the specified neighbor for a defined side of the current fluid simulation. 
		/// Additionally, it configures the opposing side of the specified neighbor to point back to the current fluid simulation,
		/// ensuring mutual reference between the two fluid simulations at the specified sides.
		/// </summary>
		/// <param name="side">The <see cref="BoundarySides"/> enum value representing the side of the current <see cref="FluidSimulation"/> 
		/// where the neighbor is being assigned. This defines the direction of the relationship.</param>
		/// <param name="neighbour">The <see cref="FluidSimulation"/> instance that will be set as the neighbor on the specified side.</param>
		public void SetNeighbour(BoundarySides side, FluidSimulation neighbour)
		{
			m_neighbours[(int)side] = neighbour;
			neighbour.m_neighbours[(int)side.GetOpposite()] = this;
		}

		internal void SetNeighbours(FluidSimulation left, FluidSimulation right, FluidSimulation top, FluidSimulation bottom)
		{
			m_neighbours[(int)BoundarySides.Left] = left;
			m_neighbours[(int)BoundarySides.Right] = right;
			m_neighbours[(int)BoundarySides.Top] = top;
			m_neighbours[(int)BoundarySides.Bottom] = bottom;
		}

		/// <summary>
		/// Retrieves the neighboring <see cref="FluidSimulation"/> instances for the current object.
		/// </summary>
		/// <param name="left">Outputs the neighbor to the left of the current instance.</param>
		/// <param name="right">Outputs the neighbor to the right of the current instance.</param>
		/// <param name="top">Outputs the neighbor above the current instance.</param>
		/// <param name="bottom">Outputs the neighbor below the current instance.</param>
		public void GetNeighbours(out FluidSimulation left, out FluidSimulation right, out FluidSimulation top, out FluidSimulation bottom)
		{
			left = m_neighbours[(int)BoundarySides.Left];
			right = m_neighbours[(int)BoundarySides.Right];
			top = m_neighbours[(int)BoundarySides.Top];
			bottom = m_neighbours[(int)BoundarySides.Bottom];
		}

		/// <summary>
		/// Retrieves the neighboring FluidSimulation instance associated with a specified boundary side.
		/// </summary>
		/// <param name="side">The boundary side for which the neighbor is requested.</param>
		/// <returns>A FluidSimulation instance representing the neighbor on the specified boundary side.
		///          Returns null if no neighbor exists on that side.</returns>
		public FluidSimulation GetNeighbour(BoundarySides side)
		{
			return m_neighbours[(int)side];
		}

		/// <summary>
		/// Determines whether the specified boundary side is an external boundary.
		/// </summary>
		/// <param name="side">The boundary side to check.</param>
		/// <returns>True if the boundary side is external (i.e., it has a neighbor), 
		///          otherwise false.</returns>
		protected bool IsExternalBoundary(BoundarySides side)
		{
			return m_neighbours[(int)side] != null;
		}

		/// <summary>
		/// Populates and returns a list of all <see cref="FluidSimulation"/> instances that belong to the same group as this <see cref="FluidSimulation"/>.
		/// <returns>
		/// A list containing all <see cref="FluidSimulation"/> instances associated with the same group.
		/// </returns>
		public List<FluidSimulation> GetSimulationGroup()
		{
			List<FluidSimulation> matches = new List<FluidSimulation>();
			GetSimulationGroup(matches);
			return matches;
		}

		/// <summary>
		/// Populates the specified list with all <see cref="FluidSimulation"/> instances that belong to the same group 
		/// as this <see cref="FluidSimulation"/> instance.
		/// </summary>
		/// <param name="simulations">A list that will be populated with <see cref="FluidSimulation"/> instances from the group.</param>
		public void GetSimulationGroup(List<FluidSimulation> simulations)
		{
			if(simulations == null)
			{
				throw new ArgumentNullException("Parameter simulations cannot be null");
			}

			Scene sceneOfSim = gameObject.scene;
			foreach (GameObject root in sceneOfSim.GetRootGameObjects())
			{
				simulations.AddRange(root.GetComponentsInChildren<FluidSimulation>());
			}
			simulations.RemoveAll(x => x.groupID != groupID);
		}

		/// <summary>
		/// Populates and returns a dictionary mapping coordinates (as Vector2Int) to their corresponding <see cref="FluidSimulation"/> instances within the group that can be samples to find neighbours. 
		/// </summary>
		/// <returns>
		/// A dictionary mapping all <see cref="FluidSimulation"/> instances associated with the same group.
		/// </returns>
		public Dictionary<Vector2Int, FluidSimulation> GetSimulationGroupGrid()
		{
			Dictionary<Vector2Int, FluidSimulation> simulationMap = new Dictionary<Vector2Int, FluidSimulation>();
			GetSimulationGroupGrid(simulationMap);
			return simulationMap;
		}

		/// <summary>
		/// Populates the specified dictionary with all <see cref="FluidSimulation"/> instances that belong to the same group as this <see cref="FluidSimulation"/> instance that can be samples to find neighbours.
		/// </summary>
		/// <param name="simulationMap">A dictionary that will be populated with <see cref="FluidSimulation"/> instances from the group.</param>
		public void GetSimulationGroupGrid(Dictionary<Vector2Int, FluidSimulation> simulationMap)
		{
			List<FluidSimulation> simulationsInGroup = this.GetSimulationGroup();
			foreach (FluidSimulation currentSim in simulationsInGroup)
			{
				simulationMap.Add(currentSim.gridPos, currentSim);
			}
		}

		internal static void GetNeighboursFromMap(Vector2Int gridIndex, Dictionary<Vector2Int, FluidSimulation> fluidSimMap, out FluidSimulation left, out FluidSimulation right, out FluidSimulation top, out FluidSimulation bottom)
		{
			left = fluidSimMap.GetValueOrDefault(gridIndex + Vector2Int.left, null);
			right = fluidSimMap.GetValueOrDefault(gridIndex + Vector2Int.right, null);
			top = fluidSimMap.GetValueOrDefault(gridIndex + Vector2Int.up, null);
			bottom = fluidSimMap.GetValueOrDefault(gridIndex + Vector2Int.down, null);
		}

		internal void GetNeighboursFromMap(Dictionary<Vector2Int, FluidSimulation> fluidSimMap, out FluidSimulation left, out FluidSimulation right, out FluidSimulation top, out FluidSimulation bottom)
		{
			GetNeighboursFromMap(gridPos, fluidSimMap, out left, out right, out top, out bottom);
		}

		/// <summary>
		/// Retrieves the neighboring FluidSimulation instances from the current simulation group.
		/// </summary>
		/// <param name="left">The FluidSimulation object to the left of the current simulation.</param>
		/// <param name="right">The FluidSimulation object to the right of the current simulation.</param>
		/// <param name="top">The FluidSimulation object above the current simulation.</param>
		/// <param name="bottom">The FluidSimulation object below the current simulation.</param>
		public void GetNeighboursFromGroup(out FluidSimulation left, out FluidSimulation right, out FluidSimulation top, out FluidSimulation bottom)
		{
			GetNeighboursFromMap(gridPos, GetSimulationGroupGrid(), out left, out right, out top, out bottom);
		}
	}
}