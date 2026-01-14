using UnityEditor;
using UnityEngine;

namespace FluidFrenzy.Editor
{
#if UNITY_EDITOR
	[CustomEditor(typeof(FoamLayer), editorForChildClasses:true)]
	public class FoamLayerEditor : UnityEditor.Editor
	{
		class Styles
		{
			public static GUIContent settingsText = new GUIContent(
				"Settings",
				"The FoamLayerSettings that this FoamLayer will use to generate it's foam mask."
			);
		}

		SerializedProperty m_settingsProperty;

		FoamLayerSettingsEditor m_settingsEditor;

		void OnEnable()
		{
			m_settingsProperty = serializedObject.FindProperty("settings");

			m_settingsEditor = CreateEditor(m_settingsProperty.objectReferenceValue) as FoamLayerSettingsEditor;
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
					m_settingsEditor = CreateEditor(m_settingsProperty.objectReferenceValue) as FoamLayerSettingsEditor;
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