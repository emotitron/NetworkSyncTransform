using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace emotitron.InputSystem
{
	[System.Serializable]
	public class InputSelectorAxis : InputSelectorBase
	{
		//public string[] axisNames;
		public string selectedAxis;
		public int selectedIndex;

		public override bool Test()
		{
			return UnityEngine.Input.GetAxis(selectedAxis) > 0 && TestModKeys();
		}
	}

#if UNITY_EDITOR
	[CustomPropertyDrawer(typeof(InputSelectorAxis))]
	[CanEditMultipleObjects]
	public class CustomAxisSelection : InputSelectionBaseDrawer
	{
		private static string[] axisNames = new string[0];

		public override void OnGUI(Rect r, SerializedProperty property, GUIContent label)
		{
			base.OnGUI(r, property, label);

			SerializedProperty selectedAxis = property.FindPropertyRelative("selectedAxis");
			SerializedProperty selectedIndex = property.FindPropertyRelative("selectedIndex");

			GetInputNames();

			int val = EditorGUI.Popup(new Rect(secondFieldLeft, r.yMin, halfFieldWidth, 16), selectedIndex.intValue, axisNames);
			selectedIndex.intValue = val;
			selectedAxis.stringValue = axisNames[val];
		}

		private static void GetInputNames()
		{
			var inputManager = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/InputManager.asset")[0];
			SerializedObject obj = new SerializedObject(inputManager);
			SerializedProperty axisArray = obj.FindProperty("m_Axes");

			// resize the nonalloc array if the input manager count has changed (user added/removed def?)
			if (axisNames.Length != axisArray.arraySize)
				axisNames = new string[axisArray.arraySize];

			// get all of the axis names from the array
			for (int i = 0; i < axisArray.arraySize; ++i)
			{
				var axis = axisArray.GetArrayElementAtIndex(i);
				axisNames[i] = axis.FindPropertyRelative("m_Name").stringValue;
			}
		}
	}
#endif
}

