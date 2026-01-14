using System;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace FluidFrenzy.Editor
{
#if UNITY_EDITOR


	[CustomEditor(typeof(ErosionLayer))]
	public class ErosionLayerEditor : UnityEditor.Editor
	{
		class Styles
		{
			public static GUIContent slippageText = new GUIContent(
				"Slipage",
				@"Toggles material slippage.

When enabled, material on steep slopes will naturally slide down to lower areas, smoothing the terrain over time."
			);
			public static GUIContent angleText = new GUIContent(
				"Angle", 
				"Adjust the angle at which the slippage should happen. Any terrain angle higher than this value will smooth due to slippage."
			);
			public static GUIContent slopeSmoothnessText = new GUIContent(
				"Slope Smoothness",
				@"Controls the intensity of the slippage effect.

Higher values result in more aggressive smoothing of slopes that exceed the slippageAngle."
			);
			
			public static GUIContent hydraulicErsoionText = new GUIContent(
				"Hydraulic Erosion", 
				"Enable/Disable hydraulic erosion caused by fluids flowing over the top layer of the terrain. Faster-flowing fluids erode the terrain faster."
			);
			public static GUIContent maxSedimentText = new GUIContent(
				"Max Sedediment",
				@"The maximum amount of sediment a fluid cell can carry.

Once the sediment carried by the fluid reaches this limit, no further erosion will occur in that cell until material is deposited elsewhere."
			);
			public static GUIContent sedimentDissolveRateText = new GUIContent(
				"Sediment Dissolve Rate",
				@"The rate at which solid terrain is picked up by the fluid.

Higher values cause the terrain to erode faster, provided the fluid has not reached its maxSediment capacity."
			);
			public static GUIContent sedimentDepositRateText = new GUIContent(
				"Sediment Deposit Rate",
				@"The rate at which carried sediment settles back onto the terrain.

Deposition occurs when the fluid slows down or when the carried material exceeds the capacity defined by maxSediment."
			);
			public static GUIContent sedimentAdvectionSpeedText = new GUIContent(
				"Sediment Advection Speed",
				@"The speed at which sediment moves with the fluid flow.

Higher values transport material further across the world before it deposits. 

 Note: Because the erosion simulation is not strictly mass-conserving, very high speeds may cause sediment to ""vanish"" if it moves into cells with no fluid or off the edge of the simulation grid."
			);

			public static GUIContent minTiltAngleText = new GUIContent(
				"Min Tilt Angle",
				@"Defines the minimum slope angle required for full hydraulic erosion efficiency.

This value modulates erosion based on the terrain's tilt. 
•  0 Degrees: No restriction. Flat surfaces erode at the same rate as slopes. 
•  High Degrees: Limits erosion primarily to steeper slopes, preserving flat areas."
			);

			public static readonly GUIContent highPrecisionAdvectionLabel = new GUIContent(
				"High Precision Advection",
				@"Toggles a higher-fidelity movement calculation for sediment.

When enabled, the simulation uses a more accurate method to move sediment. This prevents sediment from artificially fading away due to calculation errors but increases the GPU performance cost."
			);
		}


		[CustomPropertyDrawer(typeof(ErosionLayer.ErosionSettings), true)]
		public class ErosionSettingsDrawer : PropertyDrawer
		{
			// This is the main function that draws the UI in the Inspector.
			public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
			{
				EditorGUI.BeginProperty(position, label, property);

				Rect currentPos = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
				SerializedProperty nameProperty = property.FindPropertyRelative("name");

				Rect headerRect = new Rect(currentPos.x, currentPos.y - 2, currentPos.width, EditorGUIUtility.singleLineHeight);

				Rect foldoutRect = headerRect;
				foldoutRect.x += 12;
				foldoutRect.width = 15f;

				Rect nameFieldRect = headerRect;
				nameFieldRect.x += foldoutRect.width;
				nameFieldRect.width = currentPos.width / 2;

				property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, GUIContent.none, true);
				nameProperty.stringValue = EditorGUI.DelayedTextField(nameFieldRect, nameProperty.stringValue, EditorStyles.boldLabel);
				currentPos.y += EditorGUIUtility.singleLineHeight + 2;

				if (!property.isExpanded)
					return;

				SerializedProperty slippage = property.FindPropertyRelative("slippage");
				SerializedProperty slippageAngle = property.FindPropertyRelative("slippageAngle");
				SerializedProperty slopeSmoothness = property.FindPropertyRelative("slopeSmoothness");

				SerializedProperty hydraulicErosion = property.FindPropertyRelative("hydraulicErosion");
				SerializedProperty maxSediment = property.FindPropertyRelative("maxSediment");
				SerializedProperty sedimentDissolveRate = property.FindPropertyRelative("sedimentDissolveRate");
				SerializedProperty sedimentDepositRate = property.FindPropertyRelative("sedimentDepositRate");

				if (EditorExtensions.DrawFoldoutHeaderToggle(slippage, Styles.slippageText, ref currentPos, false))
				{
					using (new EditorGUI.DisabledGroupScope(!slippage.boolValue))
					using (new EditorGUI.IndentLevelScope(1))
					{
						EditorGUI.PropertyField(currentPos, slippageAngle, Styles.angleText);
						currentPos.y += EditorGUIUtility.singleLineHeight + 2;
						EditorGUI.PropertyField(currentPos, slopeSmoothness, Styles.slopeSmoothnessText);
						currentPos.y += EditorGUIUtility.singleLineHeight + 2;
					}
				}

				if (EditorExtensions.DrawFoldoutHeaderToggle(hydraulicErosion, Styles.hydraulicErsoionText, ref currentPos, false))
				{
					using (new EditorGUI.DisabledGroupScope(!hydraulicErosion.boolValue))
					using (new EditorGUI.IndentLevelScope(1))
					{
						EditorGUI.PropertyField(currentPos, maxSediment, Styles.maxSedimentText);
						currentPos.y += EditorGUIUtility.singleLineHeight + 2;
						EditorGUI.PropertyField(currentPos, sedimentDissolveRate, Styles.sedimentDissolveRateText);
						currentPos.y += EditorGUIUtility.singleLineHeight + 2;
						EditorGUI.PropertyField(currentPos, sedimentDepositRate, Styles.sedimentDepositRateText);
						currentPos.y += EditorGUIUtility.singleLineHeight + 2;
					}
				}

				if (property.managedReferenceValue is TerraformLayer.TerraformSettings)
				{
					SerializedProperty liquify = property.FindPropertyRelative("liquify");
					if (EditorExtensions.DrawFoldoutHeaderToggle(liquify, TerraformLayerEditor.Styles.liquifyLabel, ref currentPos, false))
					{
						using (new EditorGUI.DisabledGroupScope(!liquify.boolValue))
						using (new EditorGUI.IndentLevelScope(1))
						{
							SerializedProperty liquifyLayer = property.FindPropertyRelative("liquifyLayer");
							SerializedProperty liquifyRate = property.FindPropertyRelative("liquifyRate");
							SerializedProperty liquifyAmount = property.FindPropertyRelative("liquifyAmount");

							EditorGUI.PropertyField(currentPos, liquifyLayer, TerraformLayerEditor.Styles.liquifyLayerLabel);
							currentPos.y += EditorGUIUtility.singleLineHeight + 2;
							EditorGUI.PropertyField(currentPos, liquifyRate, TerraformLayerEditor.Styles.liquifyRateLabel);
							currentPos.y += EditorGUIUtility.singleLineHeight + 2;
							EditorGUI.PropertyField(currentPos, liquifyAmount, TerraformLayerEditor.Styles.liquifyAmountLabel);
							currentPos.y += EditorGUIUtility.singleLineHeight + 2;
						}
					}

					SerializedProperty[] fluidLayerContacts = { property.FindPropertyRelative("fluidLayer1Contact"), property.FindPropertyRelative("fluidLayer2Contact") };
					GUIContent[] fluidLayerLabels = { TerraformLayerEditor.Styles.fluidLayer1ContactHeaderLabel, TerraformLayerEditor.Styles.fluidLayer2ContactHeaderLabel };

					for(int i = 0; i < fluidLayerContacts.Length; i++)
					{
						SerializedProperty fluidLayerContact = fluidLayerContacts[i];
						GUIContent fluidLayerLabel = fluidLayerLabels[i];
						SerializedProperty fluidLayerContact_enabled = fluidLayerContact.FindPropertyRelative("enabled");
						if (EditorExtensions.DrawFoldoutHeaderToggle(fluidLayerContact_enabled, fluidLayerLabel, ref currentPos, false))
						{
							using (new EditorGUI.DisabledGroupScope(!fluidLayerContact_enabled.boolValue))
							using (new EditorGUI.IndentLevelScope(1))
							{
								SerializedProperty conversionRate = fluidLayerContact.FindPropertyRelative("conversionRate");
								SerializedProperty terrainDissolveAmount = fluidLayerContact.FindPropertyRelative("terrainDissolveAmount");
								SerializedProperty fluidConsumptionAmount = fluidLayerContact.FindPropertyRelative("fluidConsumptionAmount");
								SerializedProperty convertToTerrainLayer = fluidLayerContact.FindPropertyRelative("convertToTerrainLayer");
								SerializedProperty convertToSplatChannel = fluidLayerContact.FindPropertyRelative("convertToSplatChannel");
								SerializedProperty convertToTerrainVolume = fluidLayerContact.FindPropertyRelative("convertToTerrainVolume");
								SerializedProperty convertToFluidLayer = fluidLayerContact.FindPropertyRelative("convertToFluidLayer");
								SerializedProperty convertToFluidVolume = fluidLayerContact.FindPropertyRelative("convertToFluidVolume");

								EditorGUI.PropertyField(currentPos, conversionRate, TerraformLayerEditor.Styles.conversionRateLabel);
								currentPos.y += EditorGUIUtility.singleLineHeight + 2;
								EditorGUI.PropertyField(currentPos, terrainDissolveAmount, TerraformLayerEditor.Styles.terrainDissolveAmountLabel);
								currentPos.y += EditorGUIUtility.singleLineHeight + 2;
								EditorGUI.PropertyField(currentPos, fluidConsumptionAmount, TerraformLayerEditor.Styles.fluidConsumptionAmountLabel);
								currentPos.y += EditorGUIUtility.singleLineHeight + 2;

								EditorGUI.LabelField(currentPos, "Terrain Generation", EditorStyles.boldLabel);
								currentPos.y += EditorGUIUtility.singleLineHeight + 2;

								Rect lineRect = currentPos;
								lineRect.height = EditorGUIUtility.singleLineHeight;

								Rect fieldsRect = EditorGUI.PrefixLabel(lineRect, TerraformLayerEditor.Styles.convertToTerrainLabel);
								Rect field1Rect = fieldsRect; field1Rect.width *= 0.6f;
								Rect field2Rect = fieldsRect; field2Rect.width *= 0.4f; field2Rect.x += field1Rect.width + 5;

								using (new EditorGUI.IndentLevelScope(-1))
								{
									EditorGUI.PropertyField(field1Rect, convertToTerrainLayer, GUIContent.none);
									EditorGUI.PropertyField(field2Rect, convertToSplatChannel, GUIContent.none);
								}
								currentPos.y += EditorGUIUtility.singleLineHeight + 2;

								EditorGUI.PropertyField(currentPos, convertToTerrainVolume, TerraformLayerEditor.Styles.convertToTerrainVolumeLabel);
								currentPos.y += EditorGUIUtility.singleLineHeight + 2;

								EditorGUI.LabelField(currentPos, "Fluid Generation", EditorStyles.boldLabel);
								currentPos.y += EditorGUIUtility.singleLineHeight + 2;
								EditorGUI.PropertyField(currentPos, convertToFluidLayer, TerraformLayerEditor.Styles.targetFluidLayerLabel);
								currentPos.y += EditorGUIUtility.singleLineHeight + 2;
								EditorGUI.PropertyField(currentPos, convertToFluidVolume, TerraformLayerEditor.Styles.convertToFluidVolumeLabel);
								currentPos.y += EditorGUIUtility.singleLineHeight + 2;
							}
						}
					}
				}


				EditorGUI.EndProperty();
			}

			// This function tells the Inspector how much vertical space our custom drawer needs.
			public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
			{
				// Start with a small base height for padding
				float height = 0;

				// Label header
				height += EditorGUIUtility.singleLineHeight;

				if (!property.isExpanded)
					return height;

				SerializedProperty slippage = property.FindPropertyRelative("slippage");
				SerializedProperty hydraulicErosion = property.FindPropertyRelative("hydraulicErosion");

				height += EditorGUIUtility.singleLineHeight + 2; // header
				if (slippage.isExpanded)
				{
					height += (EditorGUIUtility.singleLineHeight + 2) * 2; // properties
				}

				height += EditorGUIUtility.singleLineHeight + 2; // header
				if (hydraulicErosion.isExpanded)
				{
					height += (EditorGUIUtility.singleLineHeight + 2) * 3; // properties
				}
				
				if (property.managedReferenceValue is TerraformLayer.TerraformSettings)
				{
					SerializedProperty liquify = property.FindPropertyRelative("liquify");
					height += EditorGUIUtility.singleLineHeight + 2;

					if (liquify.isExpanded)
					{
						height += (EditorGUIUtility.singleLineHeight + 2) * 3;
					}

					SerializedProperty fluidLayer1Contact = property.FindPropertyRelative("fluidLayer1Contact").FindPropertyRelative("enabled");
					height += EditorGUIUtility.singleLineHeight + 2;
					if(fluidLayer1Contact.isExpanded)
					{
						height += (EditorGUIUtility.singleLineHeight + 2) * 7; // properties
						height += (EditorGUIUtility.singleLineHeight + 2) * 2; // label
					}

					SerializedProperty fluidLayer2Contact = property.FindPropertyRelative("fluidLayer2Contact").FindPropertyRelative("enabled");
					height += EditorGUIUtility.singleLineHeight + 2;
					if (fluidLayer2Contact.isExpanded)
					{
						height += (EditorGUIUtility.singleLineHeight + 2) * 7; // properties
						height += (EditorGUIUtility.singleLineHeight + 2) * 2; // label
					}
				}

				return height;
			}
		}

		SerializedProperty m_layerSettingsProperty;
		SerializedProperty m_minTiltAngleProperty;
		SerializedProperty m_sedimentAdvectionSpeedProperty;
		SerializedProperty m_highPrecisionAdvectionProperty;

		private ReorderableList m_reorderableList;

		protected virtual void OnEnable()
		{
			// Find the list property
			m_layerSettingsProperty = serializedObject.FindProperty("layerSettings");
			m_minTiltAngleProperty = serializedObject.FindProperty("minTiltAngle");
			m_sedimentAdvectionSpeedProperty = serializedObject.FindProperty("sedimentAdvectionSpeed");
			m_highPrecisionAdvectionProperty = serializedObject.FindProperty("highPrecisionAdvection");

			m_reorderableList = new ReorderableList(serializedObject, m_layerSettingsProperty, true, true, true, true);

			// Customize the header
			m_reorderableList.drawHeaderCallback = (Rect rect) =>
			{
				EditorGUI.LabelField(rect, m_layerSettingsProperty.displayName);
			};

			// Customize how each element is drawn. 
			m_reorderableList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
			{
				SerializedProperty element = m_reorderableList.serializedProperty.GetArrayElementAtIndex(index);

				// Add some padding
				rect.y += 2;
				EditorGUI.PropertyField(
					new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight),
					element,
					new GUIContent("Layer " + index),
					true
				);
			};

			// Customize the height of each element. 
			m_reorderableList.elementHeightCallback = (int index) =>
			{
				// Get the specific element property from the list.
				SerializedProperty element = m_reorderableList.serializedProperty.GetArrayElementAtIndex(index);
				float propertyHeight = EditorGUI.GetPropertyHeight(element, true);

				float padding = 4;

				return propertyHeight + padding;
			};

			m_reorderableList.onAddCallback = (ReorderableList list) =>
			{
				int index = list.serializedProperty.arraySize;
				if (list.serializedProperty.arraySize > 3)
					return;

				list.serializedProperty.arraySize++;
				list.index = index;
				SerializedProperty element = list.serializedProperty.GetArrayElementAtIndex(index);

				if (target is TerraformLayer)
				{
					element.managedReferenceValue = new TerraformLayer.TerraformSettings()
					{
						name = "Layer " + index
					};
				}
				else
				{
					element.managedReferenceValue = new ErosionLayer.ErosionSettings()
					{
						name = "Layer " + index
					};
				}
			};
		}


		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			m_reorderableList.DoLayoutList();

			EditorGUILayout.PropertyField(m_minTiltAngleProperty, Styles.minTiltAngleText);
			EditorGUILayout.PropertyField(m_sedimentAdvectionSpeedProperty, Styles.sedimentAdvectionSpeedText);
			EditorGUILayout.PropertyField(m_highPrecisionAdvectionProperty, Styles.highPrecisionAdvectionLabel);

			serializedObject.ApplyModifiedProperties();
		}
	}
}
#endif