using System.Linq;
using UnityEditor;
using UnityEngine;

namespace FluidFrenzy.Editor
{
	[CustomPropertyDrawer(typeof(ParticleEmitterDesc))]
	public class ParticleEmitterDescPropertyDrawer : PropertyDrawer
	{
		class Styles
		{
			/// <summary>
			/// The color range that a particle randomly selects it's color from.
			/// </summary>
			public static GUIContent colorRangeText = new GUIContent("Color", "The color range that a particle randomly selects it's color from.");
			/// <summary>
			/// The velocity range that a particle randomly selects it's velocity from.
			/// </summary>			
			public static GUIContent velocityRangeText = new GUIContent("Velocity", "The velocity range that a particle randomly selects it's velocity from.");
			/// <summary>
			/// The acceleration range that a particle randomly selects it's acceleration from.
			/// </summary>
			public static GUIContent accelerationRangeText = new GUIContent("Acceleration", "The acceleration range that a particle randomly selects it's acceleration from.");
			/// <summary>
			/// The position offset range that a particle randomly selects it's offset from.
			/// </summary>
			public static GUIContent offsetRangeText = new GUIContent("Offset", "The position offset range that a particle randomly selects it's offset from.");
			/// <summary>
			/// The angular velocity of the particles.
			/// </summary>
			public static GUIContent angularVelocityText = new GUIContent("Angular Velocity", "The angular velocity of the particles.");
			/// <summary>
			/// The size range of the particles.
			/// </summary>
			public static GUIContent sizeRangeText = new GUIContent("Size", "The size range of the particles.");
			/// <summary>
			/// The life time range that a particle randomly selects it's life time from.
			/// </summary>
			public static GUIContent lifeRangeText = new GUIContent("Life time", "The life time range that a particle randomly selects it's life time from.");
		}


		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{

			float height = EditorGUIUtility.standardVerticalSpacing + EditorGUIUtility.singleLineHeight; //label


			SerializedProperty minColorProperty = property.FindPropertyRelative("minColor");
			SerializedProperty minVelocityProperty = property.FindPropertyRelative("minVelocity");
			SerializedProperty minAccelerationProperty = property.FindPropertyRelative("minAcceleration");
			SerializedProperty minOffsetProperty = property.FindPropertyRelative("minOffset");
			SerializedProperty minAngularVelocityProperty = property.FindPropertyRelative("minAngularVelocity");
			SerializedProperty minLifeProperty = property.FindPropertyRelative("minLife");
			SerializedProperty minSizeProperty = property.FindPropertyRelative("minSize");

			height += (EditorGUI.GetPropertyHeight(minColorProperty) + EditorGUIUtility.standardVerticalSpacing);
			height += (EditorGUI.GetPropertyHeight(minVelocityProperty, false) + EditorGUIUtility.standardVerticalSpacing) * 2;
			height += (EditorGUI.GetPropertyHeight(minAccelerationProperty, false) + EditorGUIUtility.standardVerticalSpacing) * 2;
			height += (EditorGUI.GetPropertyHeight(minOffsetProperty,false) + EditorGUIUtility.standardVerticalSpacing) * 2;
			height += (EditorGUI.GetPropertyHeight(minAngularVelocityProperty) + EditorGUIUtility.standardVerticalSpacing);
			height += (EditorGUI.GetPropertyHeight(minLifeProperty) + EditorGUIUtility.standardVerticalSpacing);
			height += (EditorGUI.GetPropertyHeight(minSizeProperty) + EditorGUIUtility.standardVerticalSpacing);


			return height;
		}

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.BeginProperty(position, label, property);


			SerializedProperty minColorProperty = property.FindPropertyRelative("minColor");
			SerializedProperty maxColorProperty = property.FindPropertyRelative("maxColor");
			SerializedProperty minVelocityProperty = property.FindPropertyRelative("minVelocity");
			SerializedProperty maxVelocityProperty = property.FindPropertyRelative("maxVelocity");
			SerializedProperty minAccelerationProperty = property.FindPropertyRelative("minAcceleration");
			SerializedProperty maxAccelerationProperty = property.FindPropertyRelative("maxAcceleration");
			SerializedProperty minOffsetProperty = property.FindPropertyRelative("minOffset");
			SerializedProperty maxOffsetProperty = property.FindPropertyRelative("maxOffset");
			SerializedProperty minAngularVelocityProperty = property.FindPropertyRelative("minAngularVelocity");
			SerializedProperty maxAngularVelocityProperty = property.FindPropertyRelative("maxAngularVelocity");
			SerializedProperty minLifeProperty = property.FindPropertyRelative("minLife");
			SerializedProperty maxLifeProperty = property.FindPropertyRelative("maxLife");

			SerializedProperty minSizeProperty = property.FindPropertyRelative("minSize");
			SerializedProperty maxSizeProperty = property.FindPropertyRelative("maxSize");



			float identWidth = 14;
			float lineHeight = EditorGUIUtility.singleLineHeight;
			float spacing = EditorGUIUtility.standardVerticalSpacing;
			float labelWidth = EditorGUIUtility.labelWidth;

			Rect labelRect = new Rect(position.x, position.y, labelWidth, lineHeight);
			{
				Rect firstColorRect = new Rect(labelRect.x + labelRect.width - identWidth, position.y, position.width / 3 - 2, lineHeight);
				Rect secondColorRect = new Rect(firstColorRect.x + firstColorRect.width - identWidth, position.y, position.width / 3, lineHeight);
				EditorGUI.LabelField(labelRect, Styles.colorRangeText);
				EditorGUI.PropertyField(firstColorRect, minColorProperty, GUIContent.none);
				EditorGUI.PropertyField(secondColorRect, maxColorProperty, GUIContent.none);
			}
			labelRect.y += (lineHeight + spacing * 2);

			{
				Rect rect1 = new Rect(labelRect.x + labelRect.width - identWidth, labelRect.y, position.width / 3 - 2, lineHeight);
				Rect rect2 = new Rect(rect1.x + rect1.width - identWidth, labelRect.y, position.width / 3, lineHeight);
				EditorGUI.LabelField(labelRect, Styles.sizeRangeText);
				EditorGUI.PropertyField(rect1, minSizeProperty, GUIContent.none);
				EditorGUI.PropertyField(rect2, maxSizeProperty, GUIContent.none);
			}

			labelRect.y += lineHeight + spacing * 2;
			{
				Rect vectorRect = new Rect(labelRect.x + labelRect.width - identWidth, labelRect.y, position.width - labelWidth + identWidth, lineHeight);
				EditorGUI.LabelField(labelRect, Styles.velocityRangeText);
				minVelocityProperty.vector4Value = EditorGUI.Vector3Field(vectorRect, GUIContent.none, minVelocityProperty.vector4Value);
				vectorRect.y += lineHeight + spacing;
				maxVelocityProperty.vector4Value = EditorGUI.Vector3Field(vectorRect, GUIContent.none, maxVelocityProperty.vector4Value);
			}
			labelRect.y += (lineHeight + spacing * 2) * 2;

			{
				Rect vectorRect = new Rect(labelRect.x + labelRect.width - identWidth, labelRect.y, position.width - labelWidth + identWidth, lineHeight);
				EditorGUI.LabelField(labelRect, Styles.accelerationRangeText);
				minAccelerationProperty.vector4Value = EditorGUI.Vector3Field(vectorRect, GUIContent.none, minAccelerationProperty.vector4Value);
				vectorRect.y += lineHeight + spacing;
				maxAccelerationProperty.vector4Value = EditorGUI.Vector3Field(vectorRect, GUIContent.none, maxAccelerationProperty.vector4Value);
			}
			labelRect.y += (lineHeight + spacing * 2) * 2;

			{
				Rect vectorRect = new Rect(labelRect.x + labelRect.width - identWidth, labelRect.y, position.width - labelWidth + identWidth, lineHeight);
				EditorGUI.LabelField(labelRect, Styles.offsetRangeText);
				minOffsetProperty.vector4Value = EditorGUI.Vector3Field(vectorRect, GUIContent.none, minOffsetProperty.vector4Value);
				vectorRect.y += lineHeight + spacing;
				maxOffsetProperty.vector4Value = EditorGUI.Vector3Field(vectorRect, GUIContent.none, maxOffsetProperty.vector4Value);
			}
			labelRect.y += (lineHeight + spacing * 2) * 2;

			{
				Rect rect1 = new Rect(labelRect.x + labelRect.width - identWidth, labelRect.y, position.width / 3 - 2, lineHeight);
				Rect rect2 = new Rect(rect1.x + rect1.width - identWidth, labelRect.y, position.width / 3, lineHeight);
				EditorGUI.LabelField(labelRect, Styles.angularVelocityText);
				EditorGUI.PropertyField(rect1, minAngularVelocityProperty, GUIContent.none);
				EditorGUI.PropertyField(rect2, maxAngularVelocityProperty, GUIContent.none);
			}
			labelRect.y += (lineHeight + spacing * 2);

			{
				Rect rect1 = new Rect(labelRect.x + labelRect.width - identWidth, labelRect.y, position.width / 3 - 2, lineHeight);
				Rect rect2 = new Rect(rect1.x + rect1.width - identWidth, labelRect.y, position.width / 3, lineHeight);
				EditorGUI.LabelField(labelRect, Styles.lifeRangeText);
				EditorGUI.PropertyField(rect1, minLifeProperty, GUIContent.none);
				EditorGUI.PropertyField(rect2, maxLifeProperty, GUIContent.none);
			}
			labelRect.y += (lineHeight + spacing * 2) * 2;

			EditorGUI.EndProperty();
		}
	}
}