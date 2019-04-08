//Copyright 2018, Davin Carten, All rights reserved

using UnityEngine;
using System.Collections.Generic;
using emotitron.Utilities.GUIUtilities;

//#if UNITY_EDITOR
//using UnityEditor;
//#endif

namespace emotitron.NST
{

#if UNITY_EDITOR

	/// <summary>
	/// Variation of my SO Settings Singleton with the NST branding on top.
	/// </summary>
	public abstract class SettingsSOBaseEditor<T> : NSTHeaderEditorBase where T : SettingsScriptableObject<T>, new()
	{
		public override void OnEnable()
		{
			headerName = HeaderSettingsName;
			headerColor = HeaderSettingsColor;
			base.OnEnable();

			//if (!ScriptableObjectGUITools.foldoutStates.ContainsKey(target))
			//	ScriptableObjectGUITools.foldoutStates.Add(target, true);
		}

		public override void OnInspectorGUI()
		{
			OverlayHeader();
		}
	}
#endif
}
