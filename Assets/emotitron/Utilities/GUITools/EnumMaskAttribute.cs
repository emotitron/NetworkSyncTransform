using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace emotitron.Utilities.GUIUtilities
{
	
	// Attribute that lets me flag SendCull to use the custom drawer and be a multiselect enum
	public class EnumMaskAttribute : PropertyAttribute
	{
		public EnumMaskAttribute() { }
	}

#if UNITY_EDITOR
	[CustomPropertyDrawer(typeof(EnumMaskAttribute))]
	public class EnumMaskAttributeDrawer : PropertyDrawer
	{
		public override void OnGUI(Rect _position, SerializedProperty _property, GUIContent _label)
		{
			//_property.intValue = System.Convert.ToInt32(EditorGUI.EnumMaskPopup(_position, _label, (SendCullMask)_property.intValue));
			_property.intValue = EditorGUI.MaskField(_position, _label, _property.intValue, _property.enumNames);
		}
	}
#endif
}

