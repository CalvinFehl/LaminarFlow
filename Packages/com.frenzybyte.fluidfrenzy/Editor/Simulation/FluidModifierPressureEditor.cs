using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR
namespace FluidFrenzy.Editor
{
	[CustomEditor(typeof(FluidModifierPressure)), CanEditMultipleObjects]
	public class FluidModifierPressureEditor : UnityEditor.Editor
	{
		public override void OnInspectorGUI()
		{
			FluidModifierPressure settings = target as FluidModifierPressure;
			Undo.RecordObject(settings, "PressureSettings");

			EditorGUI.BeginChangeCheck();

			EditorGUILayout.BeginHorizontal();
			settings.pressureRange.x = EditorGUILayout.FloatField("Pressure Range", settings.pressureRange.x);
			EditorGUILayout.MinMaxSlider(ref settings.pressureRange.x, ref settings.pressureRange.y, 0, 1);
			settings.pressureRange.y = EditorGUILayout.FloatField(settings.pressureRange.y, GUILayout.Width(50));
			EditorGUILayout.EndHorizontal();
			settings.strength = EditorGUILayout.Slider("Strength", settings.strength, 0, 1000);

			if (EditorGUI.EndChangeCheck())
			{
				EditorUtility.SetDirty(settings);
			}
		}
	}
}
#endif