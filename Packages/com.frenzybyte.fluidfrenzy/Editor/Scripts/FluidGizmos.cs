using UnityEditor;
using UnityEngine;

namespace FluidFrenzy.Editor
{
	public static class FluidGizmos
	{
		[DrawGizmo(GizmoType.NonSelected | GizmoType.Selected | GizmoType.Pickable)]
		private static void DrawGizmoFluidModifierVoluime(FluidModifierVolume target, GizmoType gizmoType)
		{
			Vector3 position = target.transform.position;
			if ((target.type & FluidModifierVolume.FluidModifierType.Source) != 0)
			{
				Gizmos.DrawIcon(position, FluidEditorIcons.path_toolbar_mod_fluid_source);
			}
			else if ((target.type & FluidModifierVolume.FluidModifierType.Flow) != 0)
			{
				if (target.flowSettings.mode == FluidModifierVolume.FluidFlowSettings.FluidFlowMode.Vortex)
				{
					Gizmos.DrawIcon(position, FluidEditorIcons.path_toolbar_mod_fluid_vortex);
				}
				else
				{
					Gizmos.DrawIcon(position, FluidEditorIcons.path_toolbar_mod_fluid_current);
				}
			}
			else if ((target.type & FluidModifierVolume.FluidModifierType.Force) != 0)
			{
				Gizmos.DrawIcon(position, FluidEditorIcons.path_toolbar_mod_fluid_force);
			}
		}		
		
		
		[DrawGizmo(GizmoType.NonSelected | GizmoType.Selected | GizmoType.Pickable)]
		private static void DrawGizmoTerrainModifier(TerrainModifier target, GizmoType gizmoType)
		{
			Vector3 position = target.transform.position;
			Gizmos.DrawIcon(position, FluidEditorIcons.path_toolbar_mod_terrain_source);
		}	
		
		[DrawGizmo(GizmoType.NonSelected | GizmoType.Selected | GizmoType.Pickable)]
		private static void DrawGizmoTerraformModifier(TerraformModifier target, GizmoType gizmoType)
		{
			Vector3 position = target.transform.position;
			Gizmos.DrawIcon(position, FluidEditorIcons.path_toolbar_mod_terraform_transform);
		}

		[DrawGizmo(GizmoType.NonSelected | GizmoType.Selected | GizmoType.Pickable)]
		private static void DrawGizmoObstacle(FluidSimulationObstacle target, GizmoType gizmoType)
		{
			Vector3 position = target.transform.position;

			if (target.mode == FluidSimulationObstacle.ObstacleMode.Shape)
			{
				if (target.shape == FluidSimulationObstacle.ObstacleShape.Sphere)
				{
					Gizmos.DrawIcon(position, FluidEditorIcons.path_toolbar_obstacle_sphere);
				}
				else if (target.shape == FluidSimulationObstacle.ObstacleShape.Box)
				{
					Gizmos.DrawIcon(position, FluidEditorIcons.path_toolbar_obstacle_cube);
				}
				else if (target.shape == FluidSimulationObstacle.ObstacleShape.Cylinder)
				{
					Gizmos.DrawIcon(position, FluidEditorIcons.path_toolbar_obstacle_cylinder);
				}
				else if (target.shape == FluidSimulationObstacle.ObstacleShape.Ellipsoid)
				{
					Gizmos.DrawIcon(position, FluidEditorIcons.path_toolbar_obstacle_elipse);
				}
				else if (target.shape == FluidSimulationObstacle.ObstacleShape.Wedge)
				{
					Gizmos.DrawIcon(position, FluidEditorIcons.path_toolbar_obstacle_wedge);
				}
				else if (target.shape == FluidSimulationObstacle.ObstacleShape.HexPrism)
				{
					Gizmos.DrawIcon(position, FluidEditorIcons.path_toolbar_obstacle_hexprism);
				}
				else if (target.shape == FluidSimulationObstacle.ObstacleShape.CappedCone)
				{
					Gizmos.DrawIcon(position, FluidEditorIcons.path_toolbar_obstacle_cone);
				}
				else if (target.shape == FluidSimulationObstacle.ObstacleShape.Capsule)
				{
					Gizmos.DrawIcon(position, FluidEditorIcons.path_toolbar_obstacle_capsule);
				}
			}
		}
	}
}