//Copyright 2018, Davin Carten, All rights reserved

using UnityEngine;
using System;
using emotitron.Utilities.GUIUtilities;

#if UNITY_EDITOR
using UnityEditor;
#endif


namespace emotitron.Controller
{
	public enum NSTControllerPresets { None, FreeController, Secondary, Custom }

	public enum InputAxis
	{
		MoveRight, MoveLeft, MoveUp, MoveDown, MoveForward, MoveBack,
		PitchDown, PitchUp, YawRight, YawLeft, RollLeft, RollRight
	}

	[System.Serializable]
	public class ControllerKeyMap
	{
		public KeyCode[] keyMaps = new KeyCode[12];

		// Define the Presets here
		public static readonly ControllerKeyMap[] presets = new ControllerKeyMap[3] {
			// None
			new ControllerKeyMap() { keyMaps = new KeyCode[12] },
			// Primary
			new ControllerKeyMap()
			{
				keyMaps = new KeyCode[12]
				{

					KeyCode.D,
					KeyCode.A,
					KeyCode.None,
					KeyCode.None,
					KeyCode.W,
					KeyCode.S,

					KeyCode.R,
					KeyCode.C,
					KeyCode.E,
					KeyCode.Q,
					KeyCode.Alpha1,
					KeyCode.Alpha4
				}
			},

			new ControllerKeyMap()
			{
				keyMaps = new KeyCode[12]
				{
					KeyCode.None,
					KeyCode.None,
					KeyCode.None,
					KeyCode.None,
					KeyCode.None,
					KeyCode.None,

					KeyCode.L,
					KeyCode.K,
					KeyCode.Period,
					KeyCode.Comma,
					KeyCode.J,
					KeyCode.Semicolon,
				}
			}
		};

	}

#if UNITY_EDITOR

	[CustomPropertyDrawer(typeof(ControllerKeyMap))]
	public class NSTControllerKeyMapDrawer : PropertyDrawer
	{
		private const float padding = 3f;
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			ControllerKeyMap target = PropertyDrawerUtility.GetActualObjectForSerializedProperty<ControllerKeyMap>(fieldInfo, property);

			// Outline box
			GUI.Box(position, GUIContent.none, "HelpBox");

			for (int i = 0; i < target.keyMaps.Length; i++)
			{
				Rect r = new Rect(position.xMin + padding, position.yMin + 16 * i + padding, position.width - padding * 2, 16);
				target.keyMaps[i] = (KeyCode)EditorGUI.EnumPopup(r, Enum.GetName(typeof(InputAxis), (InputAxis)i), target.keyMaps[i]);
			}
		}
		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			return 16 * 12 + (padding * 2);
		}
	}
#endif

}

