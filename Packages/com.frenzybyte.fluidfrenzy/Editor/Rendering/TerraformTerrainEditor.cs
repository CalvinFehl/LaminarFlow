using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace FluidFrenzy.Editor
{
#if UNITY_EDITOR
	using SurfaceRenderMode = ISurfaceRenderer.RenderMode;

	// This CustomEditor targets TerraformTerrain specifically.
	[CustomEditor(typeof(TerraformTerrain))]
	public class TerraformTerrainEditor : SimpleTerrainEditor
	{
		protected SerializedProperty m_splatmapProperty;

		protected override void OnEnable()
		{
			base.OnEnable();
			// Find the property unique to TerraformTerrain (or its base SimpleTerrain if it has the field, but it's used only here)
			m_splatmapProperty = serializedObject.FindProperty("splatmap");
		}

		protected override void DrawTerrainProperties()
		{
			// Draw all properties handled by the base SimpleTerrainEditor
			base.DrawTerrainProperties();

			// Add the splatmap property which is unique to TerraformTerrain (or only relevant here)
			if (m_splatmapProperty != null)
				EditorGUILayout.PropertyField(m_splatmapProperty, Styles.splatmapText);
		}
	}
}
#endif