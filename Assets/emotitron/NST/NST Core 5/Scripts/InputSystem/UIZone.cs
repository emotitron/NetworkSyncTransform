//Copyright 2018, Davin Carten, All rights reserved

using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace emotitron.InputSystem
{
	[ExecuteInEditMode] 
	public class UIZone : UniqueNamesList<UIZone>
	{

	}

#if UNITY_EDITOR

	[CustomEditor(typeof(UIZone))]
	[CanEditMultipleObjects]
	public class UIZoneEditor : NST.NSTHeaderEditorBase
	{
		public override void OnEnable()
		{
			headerName = HeaderTagName;
			headerColor = HeaderHelperColor;
			base.OnEnable();

		}
		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();
			EditorGUILayout.HelpBox("This component tags a Graphic UI element as a touch/click area. Tagged UI Zones appear in the " +
				"inspector wherever 'InputSelector' is serialized. (For example the NSTSampleController)", MessageType.None);
		}
	}

#endif
}


