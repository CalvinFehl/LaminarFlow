using System.Linq;
using UnityEditor;
using UnityEngine;

namespace FluidFrenzy.Editor
{
	[CustomPropertyDrawer(typeof(FluidParticleSystem))]
	public class FluidParticleSystemGUI : PropertyDrawer
	{
		class Styles
		{
			public static GUIContent particleSystemText = new GUIContent("Particle System", "The particle systems settings used for this effect.");
			public static GUIContent particleDescText = new GUIContent("Particle Properties", "The properties of the emitted particles. Control the color, velocity, acceleration and life range of the particles.");
			public static GUIContent maxParticleText = new GUIContent("Max Particles", "The amount of particles to be allocated and can be active at one time. Increasing this number allows for more particles, but decreases performance.");
			public static GUIContent materialText = new GUIContent(
				"Material",
				@"The material used to render the particle geometry.

Requirement: The assigned material must use a shader capable of procedural instantiation, such as the included ProceduralParticle or ProceduralParticleUnlit shaders."
			);
			public static GUIContent layerText = new GUIContent(
				"Layer",
				@"The Unity Layer index assigned to the rendered particles.

This is used to control visibility via Camera Culling Masks, allowing specific cameras (e.g., UI or reflection probes) to ignore these particles."
			);
		}


		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			float height = EditorGUIUtility.standardVerticalSpacing + EditorGUIUtility.singleLineHeight; //label

			SerializedProperty emitterDescProperty = property.FindPropertyRelative("emitterDesc");
			SerializedProperty maxParticlesProperty = property.FindPropertyRelative("maxParticles");
			SerializedProperty materialProperty = property.FindPropertyRelative("material");
			SerializedProperty layerProperty = property.FindPropertyRelative("layer");

			if (emitterDescProperty.isExpanded)
			{
				height += EditorGUIUtility.standardVerticalSpacing + EditorGUIUtility.singleLineHeight; //label
				height += (EditorGUI.GetPropertyHeight(emitterDescProperty) + EditorGUIUtility.standardVerticalSpacing);
			}
			else
				height += (EditorGUI.GetPropertyHeight(maxParticlesProperty) + EditorGUIUtility.standardVerticalSpacing);

			height += (EditorGUI.GetPropertyHeight(maxParticlesProperty) + EditorGUIUtility.standardVerticalSpacing);
			height += (EditorGUI.GetPropertyHeight(materialProperty) + EditorGUIUtility.standardVerticalSpacing);
			height += (EditorGUI.GetPropertyHeight(layerProperty) + EditorGUIUtility.standardVerticalSpacing);


			return height;
		}

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.BeginProperty(position, label, property);

			SerializedProperty emitterDescProperty = property.FindPropertyRelative("emitterDesc");
			SerializedProperty maxParticlesProperty = property.FindPropertyRelative("maxParticles");
			SerializedProperty materialProperty = property.FindPropertyRelative("material");
			SerializedProperty layerProperty = property.FindPropertyRelative("layer");

			float identWidth = 14;
			float lineHeight = EditorGUIUtility.singleLineHeight;
			float spacing = EditorGUIUtility.standardVerticalSpacing;
			float labelWidth = EditorGUIUtility.labelWidth;

			Rect labelRect = new Rect(position.x, position.y, position.width, lineHeight);

			//EditorExtensions.DrawHeader(label, ref labelRect);
			EditorGUI.LabelField(labelRect, label, EditorStyles.boldLabel);
			labelRect.y += (lineHeight + spacing * 2);

			layerProperty.intValue = EditorGUI.LayerField(labelRect, Styles.layerText, layerProperty.intValue);
			labelRect.y += (lineHeight + spacing * 2);
			EditorGUI.PropertyField(labelRect, materialProperty, Styles.materialText);
			labelRect.y += (lineHeight + spacing * 2);
			EditorGUI.PropertyField(labelRect, maxParticlesProperty, Styles.maxParticleText);
			labelRect.y += (lineHeight + spacing * 2);
			if (EditorExtensions.DrawFoldoutHeader(emitterDescProperty, Styles.particleDescText, ref labelRect))
			{
				Rect emitterRect = labelRect;
				emitterRect.x += identWidth;
				EditorGUI.PropertyField(emitterRect, emitterDescProperty);
				labelRect.y += (EditorGUI.GetPropertyHeight(emitterDescProperty) + spacing * 2);
			}

			EditorGUI.EndProperty();
		}
	}
}