using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace FluidFrenzy.Editor
{
	using static FluidSimulationSettingsEditor;
	public class FluidSimulationMenu
	{

#if UNITY_2021_1_OR_NEWER
		private const string kWaterShaderName = "FluidFrenzy/Water";
#else
		private const string kWaterShaderName = "FluidFrenzy/Legacy/2020/Water";
#endif

#if UNITY_2021_1_OR_NEWER
#if FLUIDFRENZY_EDITOR_HDRP_SUPPORT
		private const string kLavaShaderName = "FluidFrenzy/HDRP/Lava";
#else
		private const string kLavaShaderName = "FluidFrenzy/Lava";
#endif
#else
		private const string kLavaShaderName = "FluidFrenzy/Legacy/2020/Lava";
#endif

#if FLUIDFRENZY_EDITOR_HDRP_SUPPORT
		private const string kTerraformTerrainShaderName = "FluidFrenzy/HDRP/TerraformTerrain";
#else
		private const string kTerraformTerrainShaderName = "FluidFrenzy/TerraformTerrain";
#endif

		[MenuItem("GameObject/FluidFrenzy/Flux/Water Simulation", false, 12)]
		private static void CreateFluxWaterSimulation(MenuCommand menuCommand)
		{
			CreateWaterSimulation<FluxFluidSimulation, FluxFluidSimulationSettings>(menuCommand);
		}		
		
		[MenuItem("GameObject/FluidFrenzy/Flow/Water Simulation", false, 12)]
		private static void CreateFlowWaterSimulation(MenuCommand menuCommand)
		{
			CreateWaterSimulation<FlowFluidSimulation, FlowFluidSimulationSettings>(menuCommand);
		}

		private static void CreateWaterSimulation<T,R>(MenuCommand menuCommand) where T : FluidSimulation where R : FluidSimulationSettings
		{
			GameObject selectedObject = menuCommand.context as GameObject;
			selectedObject ??= Selection.activeGameObject; // If no object was selected through the context menu, try the selection.
			GameObject customObject = new GameObject("WaterSimulation");

			if (selectedObject)
			{
				customObject.transform.SetParent(selectedObject.transform.parent, false);
				customObject.transform.position = selectedObject.transform.position;
				customObject.transform.rotation = Quaternion.identity;
				customObject.transform.localScale = Vector3.one;
			}

			T fluidSim = customObject.AddComponent<T>();
			if (fluidSim)
			{
				Vector2Int fluidSurfaceResolution = Vector2Int.one * 1024;

				ISurfaceRenderer.RenderProperties surfaceProperties = new ISurfaceRenderer.RenderProperties();
				if (selectedObject)
				{
					if (selectedObject.TryGetComponent(out Terrain terrain))
					{
						fluidSim.SetTerrain(terrain);
						fluidSurfaceResolution = Vector2Int.one * terrain.terrainData.heightmapResolution - Vector2Int.one;
						surfaceProperties.meshResolution = fluidSurfaceResolution;
						surfaceProperties.meshBlocks = new Vector2Int(8, 8);
					}
					else if(selectedObject.TryGetComponent(out SimpleTerrain simpleTerrain))
					{
						fluidSim.SetTerrain(simpleTerrain);
						surfaceProperties = simpleTerrain.surfaceProperties;

						if(surfaceProperties.renderMode != ISurfaceRenderer.RenderMode.GPULOD)
							fluidSurfaceResolution = surfaceProperties.meshResolution;
					}
					else if (selectedObject.TryGetComponent(out MeshCollider collider))
					{
						fluidSim.SetTerrain(collider);
						int numVertices = collider.sharedMesh.vertexCount;
						int gridSize = Mathf.ClosestPowerOfTwo((int)(Mathf.Sqrt((float)numVertices)));
						fluidSurfaceResolution = new Vector2Int(gridSize, gridSize);
						surfaceProperties.meshResolution = fluidSurfaceResolution;
						surfaceProperties.meshBlocks = new Vector2Int(8, 8);
					}
				}

				surfaceProperties.dimension = fluidSim.dimension;

				void simSettingsInit(R settings)
				{
					if (settings is FlowFluidSimulationSettings flowSettings)
					{
						settings.cellSize = 1;
					}
					settings.numberOfCells = fluidSurfaceResolution;
				}
				fluidSim.settings = CreateFluidSimulationSettings(customObject.scene, customObject.name + "_Fluid", (initializeCallback<R>)simSettingsInit);

				FoamLayer foamLayer = fluidSim.AddFluidLayer<FoamLayer>() as FoamLayer;
				if (foamLayer)
				{
					void foamSettingsInit(FoamLayerSettings settings)
					{
						if (typeof(T) == typeof(FlowFluidSimulation))
						{
							settings.applyShallowFoam = true;
							settings.applyTurbulenceFoam = true;
						}
					}
					foamLayer.settings = CreateFluidSimulationSettings(customObject.scene, customObject.name + "_Foam", (initializeCallback<FoamLayerSettings>)foamSettingsInit);
				}

				FluidFlowMapping flowMapping = fluidSim.AddFluidLayer<FluidFlowMapping>() as FluidFlowMapping;
				if (flowMapping)
				{
					flowMapping.settings = CreateFluidSimulationSettings<FluidFlowMappingSettings>(customObject.scene, customObject.name + "_Flow");
				}

				WaterSurface waterSurface = customObject.AddComponent<WaterSurface>();
				if (waterSurface)
				{
					waterSurface.surfaceProperties = surfaceProperties;
					waterSurface.fluidMaterial = CreateWaterSuraceMaterial(customObject.scene, customObject.name);
					waterSurface.simulation = fluidSim;
					waterSurface.foamLayer = foamLayer;
					waterSurface.flowMapping = flowMapping;
				}
			}

			Undo.RegisterCreatedObjectUndo(customObject, "Create Custom Object");
			Selection.activeObject = customObject;
		}

		[MenuItem("GameObject/FluidFrenzy/Flux/Lava Simulation", false, 13)]
		private static void CreateFluxLavaSimulation(MenuCommand menuCommand)
		{
			CreateLavaSimulation<FluxFluidSimulation, FluxFluidSimulationSettings>(menuCommand);
		}

		[MenuItem("GameObject/FluidFrenzy/Flow/Lava Simulation", false, 13)]
		private static void CreateFlowLavaSimulation(MenuCommand menuCommand)
		{
			CreateLavaSimulation<FlowFluidSimulation, FlowFluidSimulationSettings>(menuCommand);
		}

		private static void CreateLavaSimulation<T, R>(MenuCommand menuCommand) where T : FluidSimulation where R : FluidSimulationSettings
		{
			GameObject selectedObject = menuCommand.context as GameObject;
			selectedObject ??= Selection.activeGameObject; // If no object was selected through the context menu, try the selection.
			GameObject customObject = new GameObject("LavaSimulation");

			if (selectedObject)
			{
				customObject.transform.SetParent(selectedObject.transform.parent, false);
				customObject.transform.position = selectedObject.transform.position;
				customObject.transform.rotation = Quaternion.identity;
				customObject.transform.localScale = Vector3.one;
			}

			T fluidSim = customObject.AddComponent<T>();
			if (fluidSim)
			{
				Vector2Int fluidSurfaceResolution = Vector2Int.one * 1024;

				ISurfaceRenderer.RenderProperties surfaceProperties = new ISurfaceRenderer.RenderProperties();
				if (selectedObject)
				{
					if (selectedObject.TryGetComponent(out Terrain terrain))
					{
						fluidSim.SetTerrain(terrain);
						fluidSurfaceResolution = Vector2Int.one * terrain.terrainData.heightmapResolution - Vector2Int.one;
						surfaceProperties.meshResolution = fluidSurfaceResolution;
						surfaceProperties.meshBlocks = new Vector2Int(8, 8);
					}
					else if (selectedObject.TryGetComponent(out SimpleTerrain simpleTerrain))
					{
						fluidSim.SetTerrain(simpleTerrain);
						surfaceProperties = simpleTerrain.surfaceProperties;

						if (surfaceProperties.renderMode != ISurfaceRenderer.RenderMode.GPULOD)
							fluidSurfaceResolution = surfaceProperties.meshResolution;
					}
					else if (selectedObject.TryGetComponent(out MeshCollider collider))
					{
						fluidSim.SetTerrain(collider);
						int numVertices = collider.sharedMesh.vertexCount;
						int gridSize = Mathf.ClosestPowerOfTwo((int)(Mathf.Sqrt((float)numVertices)));
						fluidSurfaceResolution = new Vector2Int(gridSize, gridSize);
						surfaceProperties.meshResolution = fluidSurfaceResolution;
						surfaceProperties.meshBlocks = new Vector2Int(8, 8);
					}
				}

				surfaceProperties.dimension = fluidSim.dimension;

				void simSettingsInit(R settings)
				{
					settings.numberOfCells = fluidSurfaceResolution;
					settings.waveDamping = 0.25f;

					if(settings is FluxFluidSimulationSettings fluxSettings)
					{
						settings.cellSize = 0.15f;
						fluxSettings.velocityScale = 0.25f;
						fluxSettings.velocityDamping = 0.2f;
						fluxSettings.pressure = 0.15f;
						fluxSettings.velocityMax = 10;
						fluxSettings.advectionScale = 2;
					}
					else if (settings is FlowFluidSimulationSettings flowSettings)
					{
						settings.cellSize = 1;
					}
				}
				fluidSim.settings = CreateFluidSimulationSettings(customObject.scene, customObject.name + "_Fluid", (initializeCallback<R>)simSettingsInit);

				FluidFlowMapping flowMapping = fluidSim.AddFluidLayer<FluidFlowMapping>() as FluidFlowMapping;
				if (flowMapping)
				{
					flowMapping.settings = CreateFluidSimulationSettings<FluidFlowMappingSettings>(customObject.scene, customObject.name + "_Flow");
				}

				LavaSurface lavaSurface = customObject.AddComponent<LavaSurface>();
				if (lavaSurface)
				{
					lavaSurface.surfaceProperties = surfaceProperties;
					lavaSurface.fluidMaterial = CreateLavaSuraceMaterial(customObject.scene, customObject.name, 0);
					lavaSurface.simulation = fluidSim;
					lavaSurface.flowMapping = flowMapping;
					lavaSurface.generateHeatLut = true;
					lavaSurface.heat = LavaSurface.DefaultHeatLUT();
				}
			}

			Undo.RegisterCreatedObjectUndo(customObject, "Create Custom Object");
			Selection.activeObject = customObject;
		}

		[MenuItem("GameObject/FluidFrenzy/Terraform Terrain", false, 30)]
		private static void CreateTerraformTerrain(MenuCommand menuCommand)
		{
			GameObject selectedObject = menuCommand.context as GameObject;
			selectedObject = selectedObject ?? Selection.activeGameObject;
			GameObject customObject = new GameObject("Terraform Terrain");
			customObject.SetActive(false);
			if (selectedObject)
			{
				customObject.transform.SetParent(selectedObject.transform, false);
				customObject.transform.position = selectedObject.transform.position;
				customObject.transform.rotation = Quaternion.identity;
				customObject.transform.localScale = Vector3.one;
			}

			TerraformTerrain terraformTerrain = customObject.AddComponent<TerraformTerrain>();
			if (terraformTerrain)
			{
				Vector2 fluidSimDimension = Vector2.one;
				Vector2Int fluidSurfaceResolution = Vector2Int.one * 512;

				terraformTerrain.surfaceProperties.dimension = fluidSimDimension;
				terraformTerrain.surfaceProperties.meshResolution = fluidSurfaceResolution;
				terraformTerrain.terrainMaterial = CreateTerraformTerrainMaterial(customObject.scene, customObject.name);
				terraformTerrain.heightScale = 0;
			}
			customObject.SetActive(true);
			Undo.RegisterCreatedObjectUndo(customObject, "Create Custom Object");
			Selection.activeObject = customObject;
		}

		[MenuItem("GameObject/FluidFrenzy/Flux/Terraform Simulation", false, 31)]
		private static void CreateFluxTerraformSimulation(MenuCommand menuCommand)
		{
			CreateTerraformSimulation<FluxFluidSimulation, FluxFluidSimulationSettings>(menuCommand);
		}

		[MenuItem("GameObject/FluidFrenzy/Flow/Terraform Simulation", false, 31)]
		private static void CreateFlowTerraformSimulation(MenuCommand menuCommand)
		{
			CreateTerraformSimulation<FlowFluidSimulation, FlowFluidSimulationSettings>(menuCommand);
		}

		private static void CreateTerraformSimulation<T, R>(MenuCommand menuCommand) where T : FluidSimulation where R : FluidSimulationSettings
		{
			GameObject selectedObject = menuCommand.context as GameObject;
			selectedObject = selectedObject ?? Selection.activeGameObject;
			GameObject customObject = new GameObject("FluidSimulation");

			if (selectedObject)
			{
				customObject.transform.SetParent(selectedObject.transform.parent, false);
				customObject.transform.position = selectedObject.transform.position;
				customObject.transform.rotation = Quaternion.identity;
				customObject.transform.localScale = Vector3.one;
			}

			T fluidSim = customObject.AddComponent<T>();
			if (fluidSim)
			{
				Vector2 fluidSimDimension = Vector2.one;
				Vector2Int fluidSurfaceResolution = Vector2Int.one * 1024;
				if (selectedObject && selectedObject.TryGetComponent(out TerraformTerrain terrain))
				{
					fluidSim.SetTerrain(terrain);
					fluidSimDimension = terrain.surfaceProperties.dimension;
					fluidSurfaceResolution = terrain.surfaceProperties.meshResolution;
				}

				void simSettingsInit(R settings)
				{
					settings.numberOfCells = fluidSurfaceResolution;
					settings.secondLayer = true;
					if (settings is FlowFluidSimulationSettings flowSettings)
					{
						settings.cellSize = 1;
						flowSettings.accelerationMax = 0.005f;
						flowSettings.velocityMax = 2;
					}
					else if (settings is FluxFluidSimulationSettings fluxSettings)
					{
						fluxSettings.secondLayerAdditiveVelocity = false;
					}
				}
				fluidSim.settings = CreateFluidSimulationSettings<R>(customObject.scene, customObject.name + "_Fluid", simSettingsInit);
				fluidSim.dimension = fluidSimDimension;
				FoamLayer foamLayer = fluidSim.AddFluidLayer<FoamLayer>() as FoamLayer;
				if (foamLayer)
				{
					void foamSettingsInit(FoamLayerSettings settings)
					{
						if (typeof(T) == typeof(FlowFluidSimulation))
						{
							settings.applyShallowFoam = true;
							settings.applyTurbulenceFoam = true;
						}
					}
					foamLayer.settings = CreateFluidSimulationSettings<FoamLayerSettings>(customObject.scene, customObject.name + "_Foam", foamSettingsInit);
				}

				FluidFlowMapping flowMapping = fluidSim.AddFluidLayer<FluidFlowMapping>() as FluidFlowMapping;
				if (flowMapping)
				{
					void flowSettingsInit(FluidFlowMappingSettings settings)
					{
						settings.flowMappingMode = FluidFlowMapping.FlowMappingMode.Static;
					}
					flowMapping.settings = CreateFluidSimulationSettings<FluidFlowMappingSettings>(customObject.scene, customObject.name + "_Flow", flowSettingsInit);
				}

				fluidSim.AddFluidLayer<TerraformLayer>();
				GameObject waterSurfaceObject = new GameObject("WaterSurface", typeof(WaterSurface));
				if (waterSurfaceObject)
				{
					waterSurfaceObject.transform.SetParent(customObject.transform, false);
					waterSurfaceObject.transform.position = customObject.transform.position;
					waterSurfaceObject.transform.rotation = Quaternion.identity;
					waterSurfaceObject.transform.localScale = Vector3.one;

					if (waterSurfaceObject.TryGetComponent(out WaterSurface waterSurface))
					{
						waterSurface.surfaceProperties.meshResolution = fluidSurfaceResolution;
						waterSurface.surfaceProperties.meshBlocks = new Vector2Int(8, 8);
						waterSurface.fluidMaterial = CreateWaterSuraceMaterial(customObject.scene, customObject.name);
						waterSurface.simulation = fluidSim;
						waterSurface.foamLayer = foamLayer;
						waterSurface.flowMapping = flowMapping;
					}
				}

				GameObject lavaSurfaceObject = new GameObject("LavaSurface", typeof(LavaSurface));
				if (lavaSurfaceObject)
				{
					lavaSurfaceObject.transform.SetParent(customObject.transform, false);
					lavaSurfaceObject.transform.position = customObject.transform.position;
					lavaSurfaceObject.transform.rotation = Quaternion.identity;
					lavaSurfaceObject.transform.localScale = Vector3.one;


					if (lavaSurfaceObject.TryGetComponent(out LavaSurface lavaSurface))
					{
						lavaSurface.surfaceProperties.meshResolution = fluidSurfaceResolution;
						lavaSurface.surfaceProperties.meshBlocks = new Vector2Int(8, 8);
						lavaSurface.fluidMaterial = CreateLavaSuraceMaterial(customObject.scene, customObject.name, 1);
						lavaSurface.simulation = fluidSim;
						lavaSurface.flowMapping = flowMapping;
						lavaSurface.generateHeatLut = true;
						lavaSurface.heat = LavaSurface.DefaultHeatLUT();
					}
				}
			}

			Undo.RegisterCreatedObjectUndo(customObject, "Create Custom Object");
			Selection.activeObject = customObject;
		}

		[MenuItem("GameObject/FluidFrenzy/Modifiers/Fluid Source (Layer 1)", false, 50)]
		private static void CreateFluidModifierSourceLayer1(MenuCommand menuCommand)
		{
			FluidEditorUtils.CreateFluidModifierSource(Vector3.zero, Quaternion.identity, Vector2.one * 10, 5, 0);
		}

		[MenuItem("GameObject/FluidFrenzy/Modifiers/Fluid Source (Layer 2)", false, 51)]
		private static void CreateFluidModifierSourceLayer2(MenuCommand menuCommand)
		{
			FluidEditorUtils.CreateFluidModifierSource(Vector3.zero, Quaternion.identity, Vector2.one * 10, 5, 1);
		}

		[MenuItem("GameObject/FluidFrenzy/Modifiers/Fluid Vortex", false, 52)]
		private static void CreateFluidModifierVortex(MenuCommand menuCommand)
		{
			FluidEditorUtils.CreateFluidModifierVortex(Vector3.zero, Quaternion.identity, Vector2.one * 10);
		}

		[MenuItem("GameObject/FluidFrenzy/Obstacles/Sphere", false, 52)]
		private static void CreateObstacleSphere(MenuCommand menuCommand)
		{
			FluidEditorUtils.CreateProceduralObstacle(FluidSimulationObstacle.ObstacleShape.Sphere, Vector3.zero, Quaternion.identity, Vector3.one * 10);
		}

		[MenuItem("GameObject/FluidFrenzy/Obstacles/Box", false, 52)]
		private static void CreateObstacleBox(MenuCommand menuCommand)
		{
			FluidEditorUtils.CreateProceduralObstacle(FluidSimulationObstacle.ObstacleShape.Box, Vector3.zero, Quaternion.identity, Vector3.one * 10);
		}

		[MenuItem("GameObject/FluidFrenzy/Obstacles/Cylinder", false, 52)]
		private static void CreateObstacleCylinder(MenuCommand menuCommand)
		{
			FluidEditorUtils.CreateProceduralObstacle(FluidSimulationObstacle.ObstacleShape.Cylinder, Vector3.zero, Quaternion.identity, Vector3.one * 10);
		}

		public static Material CreateWaterSuraceMaterial(Scene scene, string targetName)
		{
			string path = GetAssetPath(scene, "_FluidMaterials");

			path += scene.name + "_water.mat";
			path = AssetDatabase.GenerateUniqueAssetPath(path);

			Material waterMaterial = new Material(Shader.Find(kWaterShaderName));
			waterMaterial.renderQueue = 2510;
			waterMaterial.EnableKeyword("_NORMALMAP");
			waterMaterial.EnableKeyword("_FOAM_NORMALMAP");
			waterMaterial.EnableKeyword("_SCREENSPACE_REFRACTION_ON");
			waterMaterial.EnableKeyword("_FOAMMODE_ALBEDO");
			waterMaterial.SetTextureScale("_FoamTexture", Vector2.one * 30);
			waterMaterial.SetTextureScale("_WaveNormals", Vector2.one * 30);
			AssetDatabase.CreateAsset(waterMaterial, path);
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
			return waterMaterial;
		}

		public static Material CreateLavaSuraceMaterial(Scene scene, string targetName, int layer)
		{
			string path = GetAssetPath(scene, "_FluidMaterials");

			path += scene.name + "_lava.mat";
			path = AssetDatabase.GenerateUniqueAssetPath(path);

			Material lavaMaterial = new Material(Shader.Find(kLavaShaderName));
	
			lavaMaterial.renderQueue = 2510;
			lavaMaterial.SetFloat("_Layer", layer);
			lavaMaterial.SetTextureScale("_MainTex", Vector2.one * 30);
			AssetDatabase.CreateAsset(lavaMaterial, path);
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
			return lavaMaterial;
		}

		public static Material CreateTerraformTerrainMaterial(Scene scene, string targetName)
		{
			string path = GetAssetPath(scene, "_TerrainMaterials");

			path += scene.name + "_terraformTerrain.mat";
			path = AssetDatabase.GenerateUniqueAssetPath(path);

			Material waterMaterial = new Material(Shader.Find(kTerraformTerrainShaderName));
			waterMaterial.renderQueue = 2000;

			AssetDatabase.CreateAsset(waterMaterial, path);
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
			return waterMaterial;
		}

		public static string GetAssetPath(Scene scene, string folder)
		{
			var path = string.Empty;

			if (string.IsNullOrEmpty(scene.path))
			{
				path = "Assets/";
			}
			else
			{
				var scenePath = Path.GetDirectoryName(scene.path);
				var extPath = scene.name + folder;
#if UNITY_2021_1_OR_NEWER
				var profilePath = Path.Join(scenePath, extPath);
#else
				var profilePath = scenePath + "/" + extPath;
#endif

				if (!AssetDatabase.IsValidFolder(profilePath))
					AssetDatabase.CreateFolder(scenePath, extPath);

				path = profilePath + "/";
			}

			return path;
		}
	}
}