//Copyright 2018, Davin Carten, All rights reserved

#if PUN_2_OR_NEWER || MIRROR || !UNITY_2019_1_OR_NEWER

using System.Collections.Generic;
using emotitron.Utilities.BitUtilities;
using emotitron.Utilities.GUIUtilities;
using emotitron.Debugging;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace emotitron.NST
{

#if UNITY_EDITOR
	[HelpURL(HELP_URL)]
#endif

	public class HitGroupSettings : SettingsScriptableObject<HitGroupSettings>
	{
		public static bool initialized;
		public const string DEF_NAME = "Default";

		[HideInInspector]
		public List<string> hitGroupTags = new List<string>(1) { DEF_NAME };
		public Dictionary<string, int> rewindLayerTagToId = new Dictionary<string, int>();

#if UNITY_EDITOR
		public override string SettingsName { get { return "Hit Group Settings"; } }
#endif

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
		static void Bootstrap()
		{
			var single = Single;
		}

		public override void Initialize()
		{
			single = this;
			base.Initialize();

			if (initialized)
				return;

			initialized = true;

			// populate the lookup dictionary
			for (int i = 0; i < hitGroupTags.Count; i++)
				if (rewindLayerTagToId.ContainsKey(hitGroupTags[i]))
				{
					XDebug.LogError(!XDebug.logErrors ? null : ("The tag '" + hitGroupTags[i] + "' is used more than once in NSTRewindSettings. Repeats will be discarded, which will likely break some parts of rewind until they are removed."));
				}
				else
				{
					rewindLayerTagToId.Add(hitGroupTags[i], i);
				}

			XDebug.Log(!XDebug.logInfo ? null : ("Initialized HitGroupMasterSettings - Total Layer Tags Count: " + hitGroupTags.Count));
		}

		/// <summary>
		/// Supplied a previous index and hitgroup name, and will return the index of the best guess in the current list of layer tags. First checks for name,
		/// then if the previous int still exists, if none of the above returns 0;
		/// </summary>
		/// <returns></returns>
		public static int FindClosestMatch(string n, int id)
		{
			var hgs = Single;

			if (hgs.hitGroupTags.Contains(n))
				return hgs.hitGroupTags.IndexOf(n);
			if (id < hgs.hitGroupTags.Count)
				return id;

			return 0;
		}
#if UNITY_EDITOR
		//private bool expanded = true;

		public override string AssetPath { get { return @"Assets/emotitron/NST Core/Resources/"; } }

		public const string HELP_URL = "https://docs.google.com/document/d/1nPWGC_2xa6t4f9P0sI7wAe4osrg4UP0n_9BVYnr5dkQ/edit#bookmark=kix.hy52qj594m2v";
		public override string HelpURL { get { return HELP_URL; } }

		public override bool DrawGui(Object target, bool asFoldout, bool includeScriptField, bool initializeAsOpen = true, bool asWindow = false)
		{
			bool isExpanded = base.DrawGui(target, asFoldout, includeScriptField, initializeAsOpen, asWindow);
			bool isHierarchyMode = EditorGUIUtility.hierarchyMode;

			if (!asFoldout || isExpanded)
			{
				SerializedObject soTarget = new SerializedObject(Single);
				SerializedProperty tags = soTarget.FindProperty("hitGroupTags");

				//Rect boxrect = EditorGUILayout.BeginVertical();
				//EditorGUI.LabelField(boxrect, GUIContent.none, (GUIStyle)"flow overlay box");

				EditorGUILayout.Space();
				Rect rt = EditorGUILayout.GetControlRect();

				float padding = 5f;
				float xButtonWidth = 16f;
				float fieldLeft = rt.xMin + EditorGUIUtility.labelWidth - padding;// rt.width - EditorGUIUtility.fieldWidth;
				float fieldWidth = rt.width - fieldLeft - padding - (isHierarchyMode ? 0 : xButtonWidth); // EditorGUIUtility.fieldWidth;
																   // Default tag will always be 0 and 'Default'

				EditorGUI.LabelField(new Rect(rt.xMin + padding, rt.yMin, rt.width - padding * 2 - xButtonWidth, rt.height), "Hit Group 0", DEF_NAME);

				EditorGUI.BeginChangeCheck();
				soTarget.Update();

				for (int i = 1; i < tags.arraySize; i++)
				{
					rt = EditorGUILayout.GetControlRect();
					
					EditorGUI.LabelField(new Rect(rt.xMin + padding, rt.yMin, rt.width - padding * 2 - xButtonWidth, rt.height), "Hit Group " + i);

					SerializedProperty tag = tags.GetArrayElementAtIndex(i);

					tag.stringValue = EditorGUI.TextField(new Rect(fieldLeft, rt.yMin, fieldWidth, rt.height), GUIContent.none, tag.stringValue);

					bool isRepeat = IsTagAlreadyUsed(tag.stringValue, i);

					if (isRepeat)
					{
						EditorUtils.CreateErrorIconF(EditorGUIUtility.labelWidth - 2, rt.yMin,
							"Each name can only be used once, repeats will be discarded at build time which cause some unpedictable results when looking up by name.");
					}

					if (GUI.Button(new Rect(rt.xMin + rt.width - xButtonWidth - padding, rt.yMin, xButtonWidth, rt.height), "X"))
					{
						tags.DeleteArrayElementAtIndex(i);
					}
				}

				soTarget.ApplyModifiedProperties();

				rt = EditorGUI.IndentedRect(EditorGUILayout.GetControlRect());
				
				if (hitGroupTags.Count < 32)
					if (GUI.Button(new Rect(rt.xMin + 8, rt.yMin + 3, rt.width - 14, rt.height + 4), "Add Hitbox Group"))
					{
						string newtag = "HitGroup" + hitGroupTags.Count;

						while (IsTagAlreadyUsed(newtag, hitGroupTags.Count))
							newtag += "X";

						Undo.RecordObject(Single, "Add Hit Group");

						hitGroupTags.Add(newtag);

						EditorUtility.SetDirty(this);
						AssetDatabase.SaveAssets();

						soTarget.Update();
					}

				rt = EditorGUILayout.GetControlRect();
				
				//EditorGUILayout.EndVertical();

				EditorGUILayout.HelpBox(
					"These tags are used by NSTHitboxGroupTag to assign colliders to hitbox groups, for things like headshots and critical hits.", MessageType.None);
				//EditorGUILayout.HelpBox(BitTools.BitsNeededForMaxValue((uint)(hitGroupTags.Count - 1)) + " bits per hit used for Rewind Cast hitmasks.", MessageType.None);
				EditorGUILayout.HelpBox(((hitGroupTags.Count > 1) ? hitGroupTags.Count : 0) + " bits per hit used for Rewind Cast hitmasks.", MessageType.None);

				if (EditorGUI.EndChangeCheck())
					AssetDatabase.SaveAssets();
			}
			return isExpanded;
		}

		private static bool IsTagAlreadyUsed(string tag, int countUpTo)
		{
			for (int i = 0; i < countUpTo; i++)
				if (Single.hitGroupTags[i] == tag)
					return true;

			return false;
		}


		public static void DrawLinkToSettings()
		{
			Rect r = EditorGUILayout.GetControlRect(false, 48f);

			GUI.Box(r, GUIContent.none, (GUIStyle)"HelpBox");

			float padding = 4;
			float line = r.yMin + padding;
			float width = r.width - padding * 2;

			GUI.Label(new Rect(r.xMin + padding, line, width, 16), "Add/Remove Hit Box Groups here:", (GUIStyle)"MiniLabel");
			line += 18;

			if (GUI.Button(new Rect(r.xMin + padding, line, width, 23), new GUIContent("Find Hit Group Settings")))
			{
				EditorGUIUtility.PingObject(HitGroupSettings.Single);
			}
		}
#endif
	}


#if UNITY_EDITOR

	[CustomEditor(typeof(HitGroupSettings))]
	[CanEditMultipleObjects]
	public class HitGroupSettingsEditor : SettingsSOBaseEditor<HitGroupSettings>
	{
		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();
			HitGroupSettings.Single.DrawGui(target, false, true, true);
		}
	}
#endif
}

#endif