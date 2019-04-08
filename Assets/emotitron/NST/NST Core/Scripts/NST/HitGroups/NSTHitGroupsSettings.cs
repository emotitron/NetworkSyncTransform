//Copyright 2018, Davin Carten, All rights reserved

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
