//Copyright 2018, Davin Carten, All rights reserved

#if PUN_2_OR_NEWER || MIRROR || !UNITY_2019_1_OR_NEWER

using System.Collections.Generic;
using UnityEngine;
using emotitron.Utilities.GUIUtilities;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace emotitron.NST.HealthSystem
{
	public enum VitalAttribute { Value, MaxValue, RegenRate, RegenDelay }

	[System.Serializable]
	public class Vital
	{
		public string name;
		[HideInInspector]
		private float value;
		public float Value { get { return value; } set { this.value = Mathf.Clamp(value, 0, maxValue); } }
		public float maxValue;
		public float startValue;
		public float regenDelay;
		public float regenRate;
		//public float overhealthThreshold;

		[Range(0, 1)]
		[Tooltip("How much of the damage this vital absords, the remainder is passed through to the next lower stat. 0 = None (useless), 0.5 = Half, 1 = Full. The root vital (0) likely should always be 1.")]
		public float absorption;
		[BitsPerRange(4, 10, true, true, "Max Network Value:", false)]
		public int bitsForStat = 7;

		public Vital()
		{
			maxValue = 100;
			startValue = 100;
			absorption = 1;
			regenDelay = 1;
			regenRate = 10;
			name = "Unnamed Vital";
			value = startValue;
			bitsForStat = 7;
		}

		public Vital(float maxValue, float startValue, float mitigation, float regenDelay, float regenRate, string name, int bitsForStat)
		{
			this.maxValue = maxValue;
			this.startValue = startValue;
			this.absorption = mitigation;
			this.regenDelay = regenDelay;
			this.regenRate = regenRate;
			this.name = name;
			this.value = startValue;
			this.bitsForStat = bitsForStat;
		}
	}


#if UNITY_EDITOR

	/// <summary>
	/// This vital will draw a vital, and will include add/destroy buttons if it is part of a list and its parent has the IVitals interface.
	/// </summary>
	[CustomPropertyDrawer(typeof(Vital))]
	[CanEditMultipleObjects]
	public class VitalDrawer : PropertyDrawer
	{
		public override void OnGUI(Rect r, SerializedProperty property, GUIContent label)
		{
			EditorGUI.BeginProperty(r, label, property);

			SerializedProperty bitsForStat = property.FindPropertyRelative("bitsForStat");
			SerializedProperty startValue = property.FindPropertyRelative("startValue");
			SerializedProperty maxValue = property.FindPropertyRelative("maxValue");

			IVitals vitals = (PropertyDrawerUtility.GetParent(property) as IVitals);

			int index = PropertyDrawerUtility.GetIndexOfDrawerObject(property);
			bool isAnArray = index >= 0 && vitals != null;

			float margin = 4;
			float padding = 6;
			float width = r.width + r.xMin + 5 ;
			float topbarHeight = 74;

			EditorGUI.indentLevel = 0;

			Rect outer = new Rect(0 + margin, r.yMin, width - margin * 2, r.height - margin - (isAnArray ? 24 : 2));
			//GUI.Box(outer, GUIContent.none, "flow overlay box");
			GUI.Box(outer, GUIContent.none, "HelpBox");
			Color boxcolor = index == 0 ? new Color(.4f, .2f, .2f) : new Color(.3f, .3f, .3f);
			EditorGUI.DrawRect(new Rect(margin + 1, r.yMin + 1, width - margin * 2 - 2, topbarHeight), boxcolor);

			Rect inner = new Rect(outer.xMin + padding, outer.yMin, outer.width - padding * 2, outer.height);

			inner.yMin += padding;
			inner.height = 16;

			string vitalnum = isAnArray ? "[" + index + "]" : "Name";

			EditorGUI.LabelField(inner, "Vital " + vitalnum + ((index == 0) ? " (Root)" : ""), (GUIStyle)"WhiteBoldLabel" );

			Rect namerect = index > 0 ? new Rect(inner.xMin, inner.yMin, inner.width - 17, inner.height) : inner;
			EditorGUI.PropertyField(namerect, property.FindPropertyRelative("name"), new GUIContent(" "));

			if (index > 0 && vitals != null)
				if (GUI.Button(new Rect(inner.xMin + inner.width - 16, inner.yMin, 16, 16), "X"))
					vitals.Vitals.RemoveAt(index);

			inner.yMin += 19;

			inner.height = 64;
			EditorGUI.PropertyField(inner, bitsForStat);
			inner.yMin += EditorGUI.GetPropertyHeight(bitsForStat) + padding;// 64;

			inner.height = 16;
			startValue.floatValue = EditorGUI.IntSlider(inner, "Start Value", (int)startValue.floatValue, 0, (1 << bitsForStat.intValue));

			inner.yMin += 17;
			inner.height = 16;
			maxValue.floatValue = EditorGUI.IntSlider(inner, "Max Value", (int)maxValue.floatValue, 0, (1 << bitsForStat.intValue));

			inner.yMin += 17;
			inner.height = 16;
			EditorGUI.PropertyField(inner, property.FindPropertyRelative("absorption"));

			inner.yMin += 17;
			inner.height = 16;
			EditorGUI.PropertyField(inner, property.FindPropertyRelative("regenDelay"));

			inner.yMin += 17;
			inner.height = 16;
			EditorGUI.PropertyField(inner, property.FindPropertyRelative("regenRate"));

			// Add new vital button (only available if this belongs to a list)
			if (index > -1 && vitals != null)
			{
				inner.yMin += 28;
				inner.height = 16;
				if (GUI.Button(new Rect(inner.xMin + 64, inner.yMin, inner.width - 128, 16), "Add New Vital"))
					vitals.Vitals.Insert(index + 1, new Vital());
			}
			
			EditorGUI.EndProperty();
			
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			IVitals vitals = (PropertyDrawerUtility.GetParent(property) as IVitals);
			bool isAnArray = PropertyDrawerUtility.GetIndexOfDrawerObject(property) != -1;
			return (isAnArray && vitals != null) ? 194 : 172;
		}
	}

#endif

}

#endif
