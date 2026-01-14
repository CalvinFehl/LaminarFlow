using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace FluidFrenzy.Editor
{
#if UNITY_EDITOR
	using SurfaceRenderMode = ISurfaceRenderer.RenderMode;

	[CustomEditor(typeof(SimpleTerrain), editorForChildClasses: true)]
	public class SimpleTerrainEditor : UnityEditor.Editor
	{
		protected class Styles
		{
			public static GUIContent meshRenderingText = new GUIContent(
				"Terrain Settings", 
				"Terrain settings the visual look of the terrain.");
			public static GUIContent shadowLightText = new GUIContent(
				"Shadow Light",
				@"The primary directional light used to calculate shadows on the terrain.

This is primarily used when rendering in the Built-in Render Pipeline (BiRP) to manually handle shadow projection on the custom terrain mesh."
			);
			public static GUIContent terrainMaterialText = new GUIContent(
				"Terrain Material",
				@"The material used to render the terrain surface.

The assigned shader must support vertex displacement based on the renderHeightmap to correctly visualize the terrain shape."
			);
			public static GUIContent sourceHeightmapText = new GUIContent(
				"Terrain Heightmap",
				@"The input texture that defines the initial shape and composition of the terrain.

For best results, use a 16-bit (16bpp) texture to prevent stepping artifacts. 

 The texture channels represent distinct material layers stacked sequentially from bottom to top. All layers are erodible. 
•  Red Channel: Layer 1 (Bottom). Defines the base height. In TerraformTerrain, a separate splatmap texture is used to apply visual variation to this specific layer. 
•  Green Channel: Layer 2. Stacked on top of the Red channel. 
•  Blue Channel: Layer 3. Stacked on top of the Green channel. 
•  Alpha Channel: Layer 4 (Top). Stacked on top of the Blue channel."
			);
			
			public static GUIContent heightScaleText = new GUIContent(
				"Height Scale",
				@"A global multiplier applied to the height values sampled from the sourceHeightmap.

This converts the normalized (0 to 1) texture data into world-space height units."
			);
			public static GUIContent upsampleText = new GUIContent(
				"Upsample",
				@"Toggles bilinear interpolation for the heightmap sampling.

Enabling this increases the number of samples taken to smooth out the terrain. This is particularly useful for reducing ""stair-stepping"" artifacts when using lower bit-depth source textures."
			);
			
		
			public static GUIContent splatmapText = new GUIContent(
				"Splatmap",
				@"The texture defining the initial material distribution (splatmap) across the terrain surface.

This texture controls which sub-textures or material layers from the assigned terrainMaterial are rendered at specific coordinates. 

 It is used to apply visual variation to the physical layers defined by the sourceHeightmap. For instance, when using TerraformTerrain, this map dictates how different surface textures (like grass, rock, or sand) are blended onto the base bedrock layer to create diversity."
			);
		}

		protected SerializedProperty m_terrainMaterialProperty;
		protected SerializedProperty m_sourceHeightmapProperty;
		protected SerializedProperty m_upsampleProperty;
		protected SerializedProperty m_heightScaleProperty;
		// m_splatmapProperty is handled by derived class
		protected SerializedProperty m_shadowLightProperty;
		protected SerializedProperty m_surfaceProperties;

		protected SerializedProperty m_colliderProperties;


		protected virtual void OnEnable()
		{
			m_terrainMaterialProperty = serializedObject.FindProperty("terrainMaterial");
			m_sourceHeightmapProperty = serializedObject.FindProperty("sourceHeightmap");
			m_heightScaleProperty = serializedObject.FindProperty("heightScale");
			m_upsampleProperty = serializedObject.FindProperty("upsample");
			m_shadowLightProperty = serializedObject.FindProperty("shadowLight");
			m_surfaceProperties = serializedObject.FindProperty("surfaceProperties");
			m_colliderProperties = serializedObject.FindProperty("colliderProperties");
			Undo.undoRedoPerformed += OnUndoRedoPerformed;
		}

		protected virtual void OnDisable()
		{
			Undo.undoRedoPerformed -= OnUndoRedoPerformed;
		}

		protected void OnUndoRedoPerformed()
		{
			SimpleTerrain simpleTerrain = target as SimpleTerrain;
			if (simpleTerrain != null)
			{
				simpleTerrain.OnRenderModeChanged();
				simpleTerrain.OnTerrainChanged();
				simpleTerrain.OnColliderChanged();
				Repaint();
			}
		}

		// Virtual method for drawing properties unique to or common in the terrain material block.
		protected virtual void DrawTerrainProperties()
		{
			EditorGUILayout.PropertyField(m_terrainMaterialProperty, Styles.terrainMaterialText);
			EditorGUILayout.PropertyField(m_heightScaleProperty, Styles.heightScaleText);
			EditorGUILayout.PropertyField(m_sourceHeightmapProperty, Styles.sourceHeightmapText);
			EditorGUILayout.PropertyField(m_upsampleProperty, Styles.upsampleText);
			// Derived classes (like TerraformTerrainEditor) will override this 
			// to add their unique properties (e.g., m_splatmapProperty)
		}

		public override void OnInspectorGUI()
		{
			using (new EditorGUILayout.HorizontalScope())
			{
				if (GUILayout.Button("Save"))
				{
					(target as SimpleTerrain).SaveTerrain(Application.dataPath, target.name + ".png", SimulationIO.FileFormat.PNG);
				}
				if(GUILayout.Button("Load"))
				{
					(target as SimpleTerrain).LoadTerrain(Application.dataPath, target.name + ".png", SimulationIO.FileFormat.PNG);
				}
			}

			EditorGUI.BeginChangeCheck();
			EditorGUILayout.PropertyField(m_surfaceProperties);
			bool changedRenderMode = EditorGUI.EndChangeCheck();

			bool terrainChanged = false;
			if (EditorExtensions.DrawFoldoutHeader(m_terrainMaterialProperty, new GUIContent("Terrain Settings")))
			{
				EditorGUI.BeginChangeCheck();
				DrawTerrainProperties(); // Calls the virtual method
				terrainChanged = EditorGUI.EndChangeCheck();
				EditorGUILayout.PropertyField(m_shadowLightProperty, Styles.shadowLightText);
			}

			GUILayout.Space(2);

			EditorGUI.BeginChangeCheck();
			EditorGUILayout.PropertyField(m_colliderProperties);
			bool colliderChanged = EditorGUI.EndChangeCheck();

			serializedObject.ApplyModifiedProperties();

			if (changedRenderMode)
			{
				(target as SimpleTerrain).OnRenderModeChanged();
			}			
			if (terrainChanged)
			{
				(target as SimpleTerrain).OnTerrainChanged();
			}			
			
			if (colliderChanged)
			{
				(target as SimpleTerrain).OnColliderChanged();
			}
		}

		public virtual void OnSceneGUI()
		{
			SimpleTerrain targetTerrain = target as SimpleTerrain;
			if (targetTerrain == null) return;
			Handles.color = new Color(0.7990195f, 0.8313726f, 0.0843138f, 1).linear;
			Vector3 cubeSize = new Vector3(targetTerrain.surfaceProperties.dimension.x, targetTerrain.heightScale, targetTerrain.surfaceProperties.dimension.y);
			Handles.DrawWireCube(targetTerrain.transform.position + Vector3.up * cubeSize.y * 0.5f, cubeSize);
		}
	}
}
#endif