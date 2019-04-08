//Copyright 2018, Davin Carten, All rights reserved

using System.Collections.Generic;
using emotitron.Utilities.GUIUtilities;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace emotitron.InputSystem
{
	[System.Serializable]
	public class InputSelectors
	{
		public List<InputSelector> selectors;

		// Constructor (only keycode at the moment, can add others)
		public InputSelectors(KeyCode kc)
		{
			selectors = new List<InputSelector>() { new InputSelector(kc) };
		}

		public bool Test()
		{
			for (int i = 0; i < selectors.Count; i++)
				if (selectors[i].Test())
					return true;

			return false;
		}
	}

	[System.Serializable]
	public class InputSelector
	{
		//public NSTComponent parent;
		public InputType inputType;

		// The instance of the class we are actually making use of
		public InputSelectorBase inputSelection;

		// Lame way of dealing with 5 different custom drawers for these derived classes
		[SerializeField] private InputSelectorAxis inputAxis;
		[SerializeField] private InputSelectorKeyCode inputKeyCode;
		[SerializeField] private InputSelectorKeys inputKeys;
		[SerializeField] private InputSelectorUIZone inputUIZone;
		[SerializeField] private InputSelectorTouchArea inputTouchArea;

		// constructor
		public InputSelector()
		{
			inputType = InputType.KeyCode;

		}

		// Constructors
		public InputSelector(KeyCode kc)
		{
			inputKeyCode = new InputSelectorKeyCode();
			inputSelection = inputKeyCode;
			inputType = InputType.KeyCode;
			inputKeyCode.keyCode = (int)kc;
		}

		//public InputSelector(KeyCode kc) { new InputSelector() }

		///// <summary>
		///// There are a few derived variations of InputSelect in this class, but we only instantiate the one that gets used. Initilize does that.
		///// </summary>
		//public void Initialize()
		//{
		//	UpdateToSelectedType();
		//	//if (triggers != null)
		//	//inputSelection.Initialize(this);

		//}

		/// <summary>
		/// When an input type category is selected, only that derived class will get used, null the others to 
		/// </summary>
		public void UpdateToSelectedType()
		{
			if (inputType == InputType.Axis) inputSelection = inputAxis;
			else if (inputType == InputType.KeyCode) inputSelection = inputKeyCode;
			else if (inputType == InputType.Keys) inputSelection = inputKeys;
			else if (inputType == InputType.UIZone) inputSelection = inputUIZone;
			else if (inputType == InputType.TouchArea) inputSelection = inputTouchArea;

			//NullUnusedTypes();

			inputSelection.Initialize(this);

		}

		/// <summary>
		/// Set the unused derived classes of InputSelectorBase to null to not waste memory on runtime startup
		/// </summary>
		private void NullUnusedTypes()
		{
			if (inputType != InputType.Axis)	inputAxis = null;
			if (inputType != InputType.KeyCode) inputKeyCode = null;
			if (inputType != InputType.Keys)	inputKeys = null;
			if (inputType != InputType.UIZone) inputUIZone = null;
			if (inputType != InputType.TouchArea) inputTouchArea = null;
		}

		public bool Test()
		{
			// Less than ideal way to make sure this initialized correctly.
			if (inputSelection == null)
				UpdateToSelectedType();

			if (inputSelection.Test())
				return true;

			return false;
		}
	}


#if UNITY_EDITOR


	[CustomPropertyDrawer(typeof(InputSelectors))]
	[CanEditMultipleObjects]
	public class InputSelectorsDrawer : PropertyDrawer
	{
		private const float padding = 4f;

		public override void OnGUI(Rect r, SerializedProperty property, GUIContent label)
		{
			SerializedProperty selectors = property.FindPropertyRelative("selectors");

			//EditorGUI.PropertyField(position, selector, true);

			GUI.Box(r, GUIContent.none, "HelpBox");

			float currentY = r.yMin + padding;
			float left = r.xMin + padding;
			float width = r.width - padding * 2;

			Rect rect;
			//Rect rect = new Rect(left, currentY, width - 16, 16);
			//GUI.Label(rect, "Inputs");
			//currentY += 17;

			for (int i = 0; i < selectors.arraySize; i++)
			{
				rect = new Rect(left, currentY, width - 16, 16);
				EditorGUI.PropertyField(rect, selectors.GetArrayElementAtIndex(i), true);

				rect = new Rect(left + width - 16, currentY, 16, 16);
				if (GUI.Button(rect, "X"))
					selectors.DeleteArrayElementAtIndex(i);

					currentY += 17;
			}

			rect = new Rect(left, currentY, width, 16);

			if (GUI.Button(rect, "Add New Trigger", (GUIStyle)"minibutton"))
				selectors.InsertArrayElementAtIndex(selectors.arraySize);

		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			SerializedProperty selectors = property.FindPropertyRelative("selectors");

			return 17 * (selectors.arraySize + 1) + padding * 2;
		}
	}

	[CustomPropertyDrawer(typeof(InputSelector))]
	[CanEditMultipleObjects]
	public class InputSelectorDrawer : PropertyDrawer
	{
		protected float labelWidth;
		protected float fieldWidth;
		protected float halfFieldWidth;
		protected float fieldAreaLeft;
		protected float secondFieldLeft;


		public override void OnGUI(Rect r, SerializedProperty property, GUIContent label)
		{
			InputSelector _target = null;

			EditorGUI.BeginProperty(r, label, property);

			// Very roundabout way of getting this target if is part of a list.
			var _parent = PropertyDrawerUtility.GetParent(property);
			int index = PropertyDrawerUtility.GetIndexOfDrawerObject(property);

			if (_parent != null && _parent is InputSelectors && index != -1)
				_target =  (_parent as InputSelectors).selectors [index];

			// This is not part of a list...
			if(_target == null)
				_target = PropertyDrawerUtility.GetActualObjectForSerializedProperty<InputSelector>(fieldInfo, property);

			if(_target != null)
				_target.UpdateToSelectedType();

			// This will need to be an interface in order to be useable on more than just NSTCastDefinition
			//NSTCastDefinition nstCastDef = property.serializedObject.targetObject as NSTCastDefinition; 
			SerializedProperty inputType = property.FindPropertyRelative("inputType");

			//SerializedProperty inputSelection = property.FindPropertyRelative("triggers");

			labelWidth = EditorGUIUtility.labelWidth - 36;
			fieldWidth = r.width - labelWidth;
			halfFieldWidth = fieldWidth * .5f;
			fieldAreaLeft = r.xMin + labelWidth;

			int val = (int)(InputType)EditorGUI.EnumPopup(new Rect(r.xMin, r.yMin, labelWidth - 2, 16), GUIContent.none, (InputType)inputType.enumValueIndex);

			inputType.enumValueIndex = val;

			Rect rightrect = new Rect(fieldAreaLeft, r.yMin, fieldWidth, r.height);
			InputType enumVal = (InputType)val;

			// Only show the derived input class that we are actually using
			if (enumVal == InputType.Axis)
				EditorGUI.PropertyField(rightrect, property.FindPropertyRelative("inputAxis"));

			else if (enumVal == InputType.KeyCode)
				EditorGUI.PropertyField(rightrect, property.FindPropertyRelative("inputKeyCode"));

			else if (enumVal == InputType.Keys)
				EditorGUI.PropertyField(rightrect, property.FindPropertyRelative("inputKeys"));

			else if (enumVal == InputType.UIZone)
				EditorGUI.PropertyField(rightrect, property.FindPropertyRelative("inputUIZone"));

			else if (enumVal == InputType.TouchArea)
				EditorGUI.PropertyField(rightrect, property.FindPropertyRelative("inputTouchArea"));

			EditorGUI.EndProperty();
		}
	}
#endif
}

