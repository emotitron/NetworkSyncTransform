//Copyright 2018, Davin Carten, All rights reserved

#if PUN_2_OR_NEWER || MIRROR || !UNITY_2019_1_OR_NEWER

using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace emotitron.NST
{
	public class NSTHitGroupsSettings : Singleton<NSTHitGroupsSettings>
	{
		
	}

#if UNITY_EDITOR

	[CustomEditor(typeof(NSTHitGroupsSettings))]
	[CanEditMultipleObjects]
	public class NSTHitGroupsSettingsEditor : NSTHeaderEditorBase
	{
		public override void OnEnable()
		{
			headerName = HeaderSettingsName;
			headerColor = HeaderSettingsColor;
			base.OnEnable();
		}

		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();
			
			HitGroupSettings.Single.DrawGui(target, false, true, true);
		}
	}

#endif
}

#endif
