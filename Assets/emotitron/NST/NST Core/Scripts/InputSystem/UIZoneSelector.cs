//Copyright 2018, Davin Carten, All rights reserved

using System.Collections.Generic;
using emotitron.Utilities.GUIUtilities;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace emotitron.InputSystem
{
	/// <summary>
	/// Property drawer based selector, creates a droplist of all available UIZone components in the scene.
	/// </summary>
	[System.Serializable]
	public class UIZoneSelector
	{
		public string ZoneName;
		public int ZoneId;
	}

#if UNITY_EDITOR
	[CustomPropertyDrawer(typeof(UIZoneSelector))]
	[CanEditMultipleObjects]
	public class UIZoneSelectorDrawer : InputSelectionBaseDrawer
	{
		private Object[] objs;
		private string[] zonenames = new string[0];

		public override void OnGUI(Rect r, SerializedProperty property, GUIContent label)
		{
			InputSelectorUIZone inputSelector = PropertyDrawerUtility.GetParent(property) as InputSelectorUIZone;
			//int index = PropertyDrawerUtility.GetIndexOfDrawerObject(property);
			UIZoneSelector _target = inputSelector.zoneSelector;

			// Get the array of zoneNames
			List<string> zonesnamelist = UIZone.names; // GetListOfZoneNames();

			// only resize the string array when it doesn't match the number of UIZones in scene
			if (zonesnamelist.Count != zonenames.Length)
				zonenames = new string[zonesnamelist.Count];

			for (int i = 0; i < zonesnamelist.Count; i++)
				zonenames[i] = zonesnamelist[i];

			if (zonesnamelist.Count == 0)
			{
				EditorUtils.CreateErrorIconF(r.xMin, r.yMin, "Add a UIZone component to a UI object to define a touch/mouse area. Any added zones will appear in a list here for you to select from.");

				EditorGUI.LabelField(r, "     No UIZones found in scene.", new GUIStyle("MiniLabel"));
			}
			else
			{
				if (_target != null)
				{
					_target.ZoneId = EditorGUI.Popup(r, _target.ZoneId, zonenames);
					_target.ZoneName = UIZone.list[_target.ZoneId].itemName;
				}

				//selectedZoneName.stringValue = zonenames[selectedZoneId.intValue];
			}
		}
	}

#endif
}

