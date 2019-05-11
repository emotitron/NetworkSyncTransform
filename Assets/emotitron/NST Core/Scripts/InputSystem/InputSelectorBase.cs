//Copyright 2018, Davin Carten, All rights reserved

#if PUN_2_OR_NEWER || MIRROR || !UNITY_2019_1_OR_NEWER

using System.Collections.Generic;
using UnityEngine;
using System;
using emotitron.Utilities.BitUtilities;
using emotitron.Utilities.GUIUtilities;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace emotitron.InputSystem
{

	public enum ModKeys
	{
		RightShift = 1,
		LeftShift = 2,
		RightAlt = 4,
		LeftAlt = 8,
		RightControl = 16,
		LeftControl = 32,
		RightCommand = 64,
		LeftCommand = 128,
		RightApple = 256,
		LeftApple = 512,
		UnModded = 1024
		
	}

	public enum ClickTypes { ClickTap, Down, Release, DblClickTap, Hold }

	public enum InputType { Keys, KeyCode, Axis, TouchArea, UIZone }
	public static class CastInputTypeExt
	{
		public static Type GetTypeOf(this InputType type)
		{
			return 
				(type == InputType.Keys) ? typeof(InputSelectorKeys) :
				(type == InputType.KeyCode) ? typeof(InputSelectorKeyCode) :
				(type == InputType.Axis) ? typeof(InputSelectorAxis) :
				(type == InputType.TouchArea) ? typeof(InputSelectorTouchArea) :
				(type == InputType.UIZone) ? typeof(InputSelectorUIZone) :
				null;
		}
	}

	/// <summary>
	/// Base class for the various input selection drawer types.
	/// </summary>
	[System.Serializable]
	public abstract class InputSelectorBase
	{
		[System.NonSerialized] public InputSelector parent;
		//public bool enabled =  true;
		public int index;
		//public InputType inputType;
		//public bool isTouch;

		public int modKeys = 0;
		private int lastCachedModKeys;

		[SerializeField]
		private List<KeyCode> modKeysList = new List<KeyCode>(); // broken down list of mod keys to actually test for at runtime

		public virtual void Initialize(InputSelector _parent)
		{
			parent = _parent;
			CacheModKeys();
		}

		KeyCode[] mods = new KeyCode[10] 
		{
			KeyCode.RightShift , KeyCode.LeftShift ,
			KeyCode.RightAlt, KeyCode.LeftAlt,
			KeyCode.RightControl, KeyCode.LeftControl,
			KeyCode.RightCommand, KeyCode.LeftCommand,
			KeyCode.RightApple, KeyCode.LeftApple
		};


		public void CacheModKeys()
		{
			// don't update the cached modkey list if nothing has changed.
			if (lastCachedModKeys == modKeys)
				return;

			int enumCount = BitTools.BitsNeededForMaxValue((int)ModKeys.UnModded) - 1;

			modKeysList.Clear();

			for (int i = 1; i < enumCount; i++)
				// create a list of the flagged mod key enums, so we only test those specified.
				if ((modKeys & (1 << i)) > 0)
					modKeysList.Add(mods[i]);
			
			lastCachedModKeys = modKeys;

		}

		// TODOL right now this is only an OR test, might want an AND variant? Will be messy with apple/pc
		public bool TestModKeys()
		{
			// If this was looking for no mod key (Nothing) - then return true for nothing found
			if (modKeys == 0)
				return true;

			// If this specifically calls for true only if no mod keys are pressed...
			if (modKeys == (int)ModKeys.UnModded)
				for (int i = 0; i < mods.Length; i++)
					if (Input.GetKey(mods[i]))
						return false;

			// Fail if any of the required mod keys fails to be pressed
			for (int i = 0; i <  modKeysList.Count; i++)
			{
				if (Input.GetKey(modKeysList[i]))
					return true;
			}

			// If this was looking for no mod key (Nothing) - then return true for nothing found
			if (modKeysList.Count == 0)
				return true;


			return false;
		}

		public abstract bool Test();
	}

#if UNITY_EDITOR
	[CustomPropertyDrawer(typeof(InputSelectorBase))]
	[CanEditMultipleObjects]
	public class InputSelectionBaseDrawer : PropertyDrawer
	{
		protected float halfFieldWidth;
		protected float firstFieldLeft;
		protected float secondFieldLeft;
		protected bool isTouch = false;

		protected SerializedProperty index;
		protected SerializedProperty modKeys;
		protected SerializedProperty castInputType;

		public override void OnGUI(Rect r, SerializedProperty property, GUIContent label)
		{
			EditorGUI.BeginProperty(r, label, property);

			InputSelector par = (InputSelector)PropertyDrawerUtility.GetParent(property);
			
			// Initialize constantly to apply changes
			par.inputSelection.Initialize(par);
			
			index = property.FindPropertyRelative("index");
			modKeys = property.FindPropertyRelative("modKeys");
			castInputType = property.FindPropertyRelative("inputType");

			halfFieldWidth = r.width * .5f;
			firstFieldLeft = r.xMin;
			secondFieldLeft = r.xMin + halfFieldWidth;

			if (!isTouch)
			{
				int oldVal = modKeys.intValue;

#if UNITY_2017_3_OR_NEWER
				int newVal = (int)(ModKeys)EditorGUI.EnumFlagsField(new Rect(r.xMin, r.yMin, halfFieldWidth, r.height), GUIContent.none, (ModKeys)modKeys.intValue);
#else
				int newVal = (int)(ModKeys)EditorGUI.EnumMaskField(new Rect(r.xMin, r.yMin, halfFieldWidth, r.height), GUIContent.none, (ModKeys)modKeys.intValue);
#endif
				// if UnModded has just been selected - clamp to clear all other mods
				if (newVal - oldVal >= (int)ModKeys.UnModded)
					newVal = (int)ModKeys.UnModded;

				// else if a mod has been selected, clear unmodded
				else if (newVal > (int)ModKeys.UnModded)
					newVal = newVal & ~((int)ModKeys.UnModded);

				modKeys.intValue = newVal;
			
			}

			EditorGUI.EndProperty();

		}

	}
#endif
			}


#endif