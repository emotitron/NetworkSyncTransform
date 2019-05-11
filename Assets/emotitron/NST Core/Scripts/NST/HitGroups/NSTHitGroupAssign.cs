//Copyright 2018, Davin Carten, All rights reserved

#if PUN_2_OR_NEWER || MIRROR || !UNITY_2019_1_OR_NEWER

using UnityEngine;
using emotitron.NST.Rewind;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace emotitron.NST
{
	/// <summary>
	/// Attach this to objects with colliders. This tells the NST Rewind engine which rewind layer colliers on this GO (and its children if checked) belong to.
	/// </summary>
	public class NSTHitGroupAssign : NSTComponent, IIncludeOnGhost
	{
		public HitGroupSelector hitGroupSelector;
		
		public bool applyToChildren;

		public override void OnNstPostAwake()
		{
			base.OnNstPostAwake();
			hitGroupSelector.ValidateSelection(this);

			// Copy this hitgroup to all relevant children GOs with colliders
			if (applyToChildren)
				CloneToAllChildrenWithColliders(cachedTransform, this);

		}

		// if applyToChildren is checked, this HitGroup component needs to be copied to all applicable gameobjects with colliders
		private void CloneToAllChildrenWithColliders(Transform par, NSTHitGroupAssign parNstHitGroup)
		{
			if (!applyToChildren)
				return;

			for (int i = 0; i < par.childCount; i++)
			{
				Transform child = par.GetChild(i);

				// if this child has its own NSTHitGroup with applyToChildren = true then stop recursing this branch, that hg will handle that branch.
				NSTHitGroupAssign hg = child.GetComponent<NSTHitGroupAssign>();
				if (hg != null && hg.applyToChildren)
					continue;

				// Copy the parent NSTHitGroup to this child if it has a collider and no NSTHitGroup of its own
				if (hg == null && child.GetComponent<Collider>() != null)
					parNstHitGroup.ComponentCopy(child.gameObject);

				// recurse this on its children
				CloneToAllChildrenWithColliders(child, parNstHitGroup);
			}
		}

		public override string ToString()
		{
			return base.ToString() + hitGroupSelector;
		}
	}

#if UNITY_EDITOR

	[CustomEditor(typeof(NSTHitGroupAssign))]
	[CanEditMultipleObjects]
	public class NSTHitGroupEditor : NSTHeaderEditorBase
	{
		NSTHitGroupAssign nstSetTag;
		bool masterSettingsToggle = true;

		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();

			//masterSettingsToggle = EditorGUILayout.Foldout(masterSettingsToggle, GUIContent.none);//  new GUIContent("Hit Group Master Settings"));
			if (masterSettingsToggle)
				HitGroupSettings.Single.DrawGui(target, true, false, true);

		}
	}
#endif
}




#endif