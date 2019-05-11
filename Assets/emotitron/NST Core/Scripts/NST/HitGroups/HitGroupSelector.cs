//Copyright 2018, Davin Carten, All rights reserved

#if PUN_2_OR_NEWER || MIRROR || !UNITY_2019_1_OR_NEWER

using System.Collections.Generic;
using UnityEngine;
using emotitron.NST;
using emotitron.Debugging;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// This is the inspector control that collects all of the rewind layers from NSTSettings and turns them into a droplist.
/// </summary>
[System.Serializable]
public struct HitGroupSelector
{

	[Tooltip("This tag will be applied to this object (and children if checked), and that tag is used by CastDefinition " +
		"to categorize hits, for things such as critical hits, limb hits, or whatever you want. " +
		"Adding tags does increase the size of NetworkCast() calls so only use what you need.")]

	public string hitGroupTag;
	public int hitGroupTagId; // the selection ID is stored in case layers are renamed, to help guess the intended tag again.

	//[SerializeField] private float drawerheight;

	public static implicit operator string(HitGroupSelector obj)
	{
		return obj.hitGroupTag;
	}
	public static implicit operator int(HitGroupSelector obj)
	{
		return obj.hitGroupTagId;
	}

	public void ValidateSelection(NSTComponent _parent)
	{
		List<string> tags = HitGroupSettings.Single.hitGroupTags;

		if (hitGroupTagId < tags.Count)
		{
			// If tag id matches the tag name... this is valid.
			if (hitGroupTag == tags[hitGroupTagId])
			{
				return;
			}

			// if not valid loop through all of the tags and see if we have a name match at a different index.
			for (int i = 0; i < tags.Count; i++)
			{
				if (hitGroupTag == tags[i])
				{
					hitGroupTagId = i;
					XDebug.LogWarning(!XDebug.logWarnings ? null : (_parent.name + " references hit group named '" + hitGroupTag + "', but that tag is no longer at the same index. "+
						"Likely this means you have renamed, added or removed a rewind tag and not updated any selection boxes referring to it. Will use the tag of the same name."));
					return;
				}
			}

			XDebug.LogWarning(!XDebug.logWarnings ? null : (_parent.name + " references hit group tag named '" + hitGroupTag + "', but that tag does not exist anymore. " +
					"Likely this means you have renamed, added or removed a rewind tag and not updated a selection boxes referring to it. Will use '" + tags[hitGroupTagId] + "' instead."));
			// Name tag couldn't be found, so using the tag that now resides at the index
			hitGroupTag = tags[hitGroupTagId];


			return;
		}
		XDebug.LogWarning(!XDebug.logWarnings ? null : (_parent.name + " references hit group tag named '" + hitGroupTag + "', but that tag does not exist anymore. " +
				"Likely this means you have renamed, added or removed a rewind tag and not updated a selection boxes referring to it. Will use default instead."));

		hitGroupTagId = 0;
		hitGroupTag = tags[0];
	}

	public override string ToString()
	{
		return "hitGroup:" + hitGroupTagId + " '" + hitGroupTag + "'";
	}
}

#if UNITY_EDITOR

[CustomPropertyDrawer(typeof(HitGroupSelector))]
public class HitGroupSelectorDrawer : PropertyDrawer
{
	
	public override void OnGUI(Rect r, SerializedProperty _property, GUIContent _label)
	{
		EditorGUI.BeginProperty(r, GUIContent.none, _property);

		SerializedProperty hitGroupTagId = _property.FindPropertyRelative("hitGroupTagId");
		SerializedProperty hitGroupTag = _property.FindPropertyRelative("hitGroupTag");

		float line = r.yMin + 2;
		// Make sure settings exists so we can pull from the layers
		HitGroupSettings hgSettings = HitGroupSettings.Single; //.EnsureExistsInScene(NSTSettings.DEFAULT_GO_NAME);

		GUIContent[] listitems = new GUIContent[hgSettings.hitGroupTags.Count];

		// Populate list from current rewind layers in NSTRewindSettings
		for (int i = 0; i < listitems.Length; i++)
		{
			listitems[i] = new GUIContent(hgSettings.hitGroupTags[i]);

			// If the selected tag has moved to a new ID (tags have been added/removed/renamed) this will refind it, 
			// otherwise it keeps the ID and will use whatever tag is at that index
			if (hitGroupTag.stringValue == hgSettings.hitGroupTags[i])
				hitGroupTagId.intValue = i;
		}

		hitGroupTagId.intValue = EditorGUI.Popup(new Rect(r.xMin, line, r.width, 16), new GUIContent("Hit Group Tag"), hitGroupTagId.intValue, listitems);
		hitGroupTag.stringValue =  (listitems.Length > hitGroupTagId.intValue) ? listitems[hitGroupTagId.intValue].text : "Untagged";
		
		EditorGUI.EndProperty();
	}
}

#endif








#endif