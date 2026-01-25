using UnityEditor;
using UnityEngine;

namespace FluidFrenzy.Editor
{
#if UNITY_EDITOR
	[CustomEditor(typeof(FluidFlowMapping), editorForChildClasses:true)]
	public class FluidFlowMappingEditor : UnityEditor.Editor
	{
		class Styles
		{
			public static GUIContent settingsText = new GUIContent(
				"Settings",
				"The FluidFlowMappingSettings that this FluidFlowMapping will use to generate the flow mapping data."
			);
		}

		SerializedProperty m_settingsProperty;

		FluidFlowMappingLayerSettingsEditor m_settingsEditor;

		void OnEnable()
		{
			m_settingsProperty = serializedObject.FindProperty("settings");

			m_settingsEditor = CreateEditor(m_settingsProperty.objectReferenceValue) as FluidFlowMappingLayerSettingsEditor;
		}

		private void OnDisable()
		{
		}

		public override void OnInspectorGUI()
		{
			EditorGUILayout.BeginHorizontal();

			bool expandSettings = EditorExtensions.DrawExpandToggle(m_settingsProperty, Styles.settingsText);
			using (var settingsChanged = new EditorGUI.ChangeCheckScope())
			using (new EditorExtensions.LabelWidthScope(1))
			{
				EditorGUILayout.PropertyField(m_settingsProperty);
				if (settingsChanged.changed)
				{
					m_settingsEditor = CreateEditor(m_settingsProperty.objectReferenceValue) as FluidFlowMappingLayerSettingsEditor;
				}
			}

			EditorGUILayout.EndHorizontal();

			if (expandSettings)
			{
				m_settingsEditor.OnInspectorGUI();
			}

			serializedObject.ApplyModifiedProperties();

		}

	}
}
#endif