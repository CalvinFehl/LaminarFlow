using UnityEditor;
using UnityEngine;

namespace FluidFrenzy.Editor
{
	public static class FluidEditorIcons
	{
		private const string ROOT_PATH = "Packages/com.frenzybyte.fluidfrenzy/Editor/Textures/";

		// Custom Icons Paths
		public static string path_toolbar_mod_fluid_source => ROOT_PATH + "icons-toolbar-mod-fluid-source.png";
		public static string path_toolbar_mod_fluid_force => ROOT_PATH + "icons-toolbar-mod-fluid-wave.png";
		public static string path_toolbar_mod_fluid_current => ROOT_PATH + "icons-toolbar-mod-fluid-current.png";
		public static string path_toolbar_mod_fluid_vortex => ROOT_PATH + "icons-toolbar-mod-fluid-vortex.png";
		public static string path_toolbar_mod_terrain_source => ROOT_PATH + "icons-toolbar-mod-terrain-source.png";
		public static string path_toolbar_mod_terraform_transform => ROOT_PATH + "icons-toolbar-mod-terraform.png";
		public static string path_toolbar_obstacle_sphere => ROOT_PATH + "icons-toolbar-obstacle-sphere.png";
		public static string path_toolbar_obstacle_cube => ROOT_PATH + "icons-toolbar-obstacle-cube.png";
		public static string path_toolbar_obstacle_cylinder => ROOT_PATH + "icons-toolbar-obstacle-cylinder.png";
		public static string path_toolbar_obstacle_elipse => ROOT_PATH + "icons-toolbar-obstacle-elipse.png";
		public static string path_toolbar_obstacle_wedge => ROOT_PATH + "icons-toolbar-obstacle-wedge.png";
		public static string path_toolbar_obstacle_hexprism => ROOT_PATH + "icons-toolbar-obstacle-hexprism.png";
		public static string path_toolbar_obstacle_cone => ROOT_PATH + "icons-toolbar-obstacle-cone.png";
		public static string path_toolbar_obstacle_capsule => ROOT_PATH + "icons-toolbar-obstacle-capsule.png";

		// Cached Textures
		private static Texture2D _toolbar_mod_fluid_source;
		private static Texture2D _toolbar_mod_fluid_force;
		private static Texture2D _toolbar_mod_fluid_current;
		private static Texture2D _toolbar_mod_fluid_vortex;
		private static Texture2D _toolbar_mod_terrain_source;
		private static Texture2D _toolbar_mod_terraform_transform;
		private static Texture2D _toolbar_obstacle_sphere;
		private static Texture2D _toolbar_obstacle_cube;
		private static Texture2D _toolbar_obstacle_cylider;
		private static Texture2D _toolbar_obstacle_elipse;
		private static Texture2D _toolbar_obstacle_wedge;
		private static Texture2D _toolbar_obstacle_hexprism;
		private static Texture2D _toolbar_obstacle_cone;
		private static Texture2D _toolbar_obstacle_capsule;

		public static Texture2D toolbar_mod_fluid_source => _toolbar_mod_fluid_source ??= Load(path_toolbar_mod_fluid_source);
		public static Texture2D toolbar_mod_fluid_force => _toolbar_mod_fluid_force ??= Load(path_toolbar_mod_fluid_force);
		public static Texture2D toolbar_mod_fluid_current => _toolbar_mod_fluid_current ??= Load(path_toolbar_mod_fluid_current);
		public static Texture2D toolbar_mod_fluid_vortex => _toolbar_mod_fluid_vortex ??= Load(path_toolbar_mod_fluid_vortex);
		public static Texture2D toolbar_mod_terrain_source => _toolbar_mod_terrain_source ??= Load(path_toolbar_mod_terrain_source);
		public static Texture2D toolbar_mod_terraform_transform => _toolbar_mod_terraform_transform ??= Load(path_toolbar_mod_terraform_transform);
		public static Texture2D toolbar_obstacle_sphere => _toolbar_obstacle_sphere ??= Load(path_toolbar_obstacle_sphere);
		public static Texture2D toolbar_obstacle_cube => _toolbar_obstacle_cube ??= Load(path_toolbar_obstacle_cube);
		public static Texture2D toolbar_obstacle_cylider => _toolbar_obstacle_cylider ??= Load(path_toolbar_obstacle_cylinder);
		public static Texture2D toolbar_obstacle_elipse => _toolbar_obstacle_elipse ??= Load(path_toolbar_obstacle_elipse);
		public static Texture2D toolbar_obstacle_wedge => _toolbar_obstacle_wedge ??= Load(path_toolbar_obstacle_wedge);
		public static Texture2D toolbar_obstacle_hexprism => _toolbar_obstacle_hexprism ??= Load(path_toolbar_obstacle_hexprism);
		public static Texture2D toolbar_obstacle_cone => _toolbar_obstacle_cone ??= Load(path_toolbar_obstacle_cone);
		public static Texture2D toolbar_obstacle_capsule => _toolbar_obstacle_capsule ??= Load(path_toolbar_obstacle_capsule);

		private static Texture2D Load(string path)
		{
			return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
		}
	}
}