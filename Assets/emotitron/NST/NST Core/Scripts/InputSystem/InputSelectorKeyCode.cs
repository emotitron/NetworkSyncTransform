using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace emotitron.InputSystem
{
	[System.Serializable]
	public class InputSelectorKeyCode : InputSelectorBase
	{
		public int keyCode;

		public override bool Test()
		{
			return Input.GetKeyDown((KeyCode)keyCode) && TestModKeys();
		}
	}
	
#if UNITY_EDITOR
	[CustomPropertyDrawer(typeof(InputSelectorKeyCode))]
	[CanEditMultipleObjects]
	public class CustomKeyCodeSelection : InputSelectionBaseDrawer
	{

		public override void OnGUI(Rect r, SerializedProperty property, GUIContent label)
		{
			base.OnGUI(r, property, label);
			//InputSelectorKeyCode target = (InputSelectorKeyCode)_target;
			SerializedProperty keyCode = property.FindPropertyRelative("keyCode");

			//EditorGUI.LabelField(r, "On Keycode");

			keyCode.intValue = (int)(KeyCode)EditorGUI.EnumPopup(new Rect(secondFieldLeft, r.yMin, halfFieldWidth, 16), GUIContent.none, (KeyCode)keyCode.intValue);

		}
	}
#endif
}

