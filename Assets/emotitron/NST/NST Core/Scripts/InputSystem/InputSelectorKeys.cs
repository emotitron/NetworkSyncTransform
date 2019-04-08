using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace emotitron.InputSystem
{
	[System.Serializable]
	public class InputSelectorKeys : InputSelectorBase
	{
		public string[] keys;
		public string keystring;
		public int selectedIndex;

		public void ExtractKeys()
		{
			keys = new string[keystring.Length];
			for (int i = 0; i < keystring.Length; i++)
			{
				string chr = keystring.Substring(i, 1);
				if (chr == " ")
					keys[i] = "space";
				else
					keys[i] = chr;
			}
		}

		public override void Initialize(InputSelector _parent)
		{
			base.Initialize(_parent);
			ExtractKeys();

		}

		public override bool Test()
		{
			for (int i = 0; i < keys.Length; i++)
				if (Input.GetKeyDown(keys[i]) && TestModKeys())
					return true;

			return false;
		}
	}


#if UNITY_EDITOR
	[CustomPropertyDrawer(typeof(InputSelectorKeys))]
	[CanEditMultipleObjects]
	public class CustomInputKeys : InputSelectionBaseDrawer
	{
		public override void OnGUI(Rect r, SerializedProperty property, GUIContent label)
		{
			base.OnGUI(r, property, label);

			SerializedProperty keystring = property.FindPropertyRelative("keystring");

			keystring .stringValue = EditorGUI.TextField(new Rect(secondFieldLeft, r.yMin, halfFieldWidth, 16), GUIContent.none, keystring.stringValue);
		}
	}
#endif
}

