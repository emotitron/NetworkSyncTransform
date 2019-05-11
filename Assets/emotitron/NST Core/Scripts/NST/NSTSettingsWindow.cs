//Copyright 2018, Davin Carten, All rights reserved

#if PUN_2_OR_NEWER || MIRROR || !UNITY_2019_1_OR_NEWER

using UnityEngine;
using System;
using System.Reflection;
using emotitron.Debugging;
using emotitron.Utilities.GUIUtilities;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace emotitron.NST
{

#if UNITY_EDITOR

	//[InitializeOnLoad]
	public class NSTSettingsWindow : EditorWindow
	{
		private SerializedObject so;
		private Vector2 scroll;
		public bool PlaceholderTest;

		[MenuItem("Window/NST/Settings")]
		public static void ShowWindow()
		{
			NSTSettingsWindow window = GetWindow<NSTSettingsWindow>("NST Settings");
			window.minSize = new Vector2(300f, 300f);
		}

		void OnGUI()
		{

			if (so == null)
				so = new SerializedObject(this);

			scroll = EditorGUILayout.BeginScrollView(scroll);

			NSTSettings.DrawAllSettingGuis(this, so, true);
			EditorGUILayout.EndScrollView();
		}
	}
#endif
}

#endif