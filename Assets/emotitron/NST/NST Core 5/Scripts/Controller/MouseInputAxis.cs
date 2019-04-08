//Copyright 2018, Davin Carten, All rights reserved

using emotitron.Utilities.GUIUtilities;

using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace emotitron.Controller
{
	public enum MouseAxes { None, MouseX, MouseY, ScrollX, ScrollY }

	[System.Serializable]
	public class MouseInputAxis
	{
		public int axisId;
		public MouseAxes mouseAxis;
		[Range(0, 5)]
		public float sensitivity = 1f;
		public bool invert;
	}

#if UNITY_EDITOR

	[CustomPropertyDrawer(typeof(MouseInputAxis))]
	public class MouseInputAxisDrawer : PropertyDrawer
	{
		public const float padding = 5f;
		public override void OnGUI(Rect r, SerializedProperty property, GUIContent label)
		{
			MouseInputAxis _target = PropertyDrawerUtility.GetActualObjectForSerializedProperty<MouseInputAxis>(fieldInfo, property);

			Rect r2 = new Rect(r.xMin, r.yMin, r.width, r.height + 2);
			GUI.Box(r2, GUIContent.none, "HelpBox");

			r2 = new Rect(r.xMin - 4, r.yMin, 4, r.height + 2);
			Color color = (_target.axisId == 0) ? Color.red : (_target.axisId == 1) ? Color.green : Color.blue;
			EditorGUI.DrawRect(r2, color);

			float labelwidth = EditorGUIUtility.labelWidth;
			float fieldwidth = r.width - labelwidth - padding;

			r2 = new Rect(r.xMin + padding, r.yMin + padding, labelwidth, 16);
			string labeltext = (_target.axisId == 0) ? "X - Pitch" : (_target.axisId == 1) ? "Y - Yaw" : "Z - Roll";
			GUI.Label(r2, labeltext);

			r2 = new Rect(r.xMin + labelwidth, r.yMin + padding, fieldwidth - 40, 16);
			_target.mouseAxis = (MouseAxes)EditorGUI.EnumPopup(r2, GUIContent.none, _target.mouseAxis);


			r2 = new Rect(r.width - 18 + padding, r.yMin + padding, 30, 16);
			GUI.Label(r2, "Inv");

			r2 = new Rect(r.width - 32 + padding, r.yMin + padding, 50, 16);
			_target.invert = EditorGUI.Toggle(r2, _target.invert);


			r2 = new Rect(r.xMin + labelwidth, r.yMin + padding + 17, fieldwidth, 16);
			_target.sensitivity = EditorGUI.Slider(r2, GUIContent.none, _target.sensitivity, 0f, (_target.mouseAxis > MouseAxes.MouseY) ? 20f : 5f);

			r2 = new Rect(r.xMin + padding, r.yMin + padding + 17, labelwidth, 16);
			EditorGUI.LabelField(r2, "Sensitivity");
		}
		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			return 42f;
		}
	}
#endif

}


