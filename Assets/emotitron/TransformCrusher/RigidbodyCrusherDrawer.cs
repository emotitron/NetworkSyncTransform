//Copyright 2018, Davin Carten, All rights reserved

#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;

namespace emotitron.Compression
{

	[CustomPropertyDrawer(typeof(RigidbodyCrusher))]
	[CanEditMultipleObjects]

	public class RigidbodyCrusherDrawer : TransformCrusherDrawer
	{
		public override void OnGUI(Rect r, SerializedProperty prop, GUIContent label)
		{
			base.OnGUI(r, prop, label);
			SerializedProperty vel = prop.FindPropertyRelative("velCrusher");
			SerializedProperty ang = prop.FindPropertyRelative("angCrusher");
			float vh = EditorGUI.GetPropertyHeight(vel);
			float ah = EditorGUI.GetPropertyHeight(ang);

			EditorGUI.PropertyField(new Rect(r.xMin, currentline, r.width, vh), vel);
			currentline += vh;
			EditorGUI.PropertyField(new Rect(r.xMin, currentline, r.width, ah), ang);
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			SerializedProperty vel = property.FindPropertyRelative("velCrusher");
			SerializedProperty ang = property.FindPropertyRelative("angCrusher");
			float vh = EditorGUI.GetPropertyHeight(vel);
			float ah = EditorGUI.GetPropertyHeight(ang);

			return base.GetPropertyHeight(property, label) + vh + ah;
		}
		//private const float TITL_HGHT = 18f;
		//private const float SET_PAD = 0;
		//public const float BOUNDING_PADDING = 4f;

		////bool haschanged;
		//private static GUIContent gc = new GUIContent();

		//public override void OnGUI(Rect r, SerializedProperty property, GUIContent label)
		//{
		//	gc.text = label.text;
		//	gc.tooltip = label.tooltip;

		//	EditorGUI.BeginProperty(r, label, property);
		//	base.OnGUI(r, property, label);

		//	//property.serializedObject.ApplyModifiedProperties();
		//	//property.serializedObject.Update();

		//	//haschanged = true;
		//	// Hacky way to get the real object
		//	TransformCrusher target = (TransformCrusher)DrawerUtils.GetParent(property.FindPropertyRelative("posCrusher"));

		//	float currentline = r.yMin;

		//	SerializedProperty pos = property.FindPropertyRelative("posCrusher");
		//	SerializedProperty rot = property.FindPropertyRelative("rotCrusher");
		//	SerializedProperty scl = property.FindPropertyRelative("sclCrusher");
		//	SerializedProperty isExpanded = property.FindPropertyRelative("isExpanded");

		//	float ph = EditorGUI.GetPropertyHeight(pos);
		//	float rh = EditorGUI.GetPropertyHeight(rot);
		//	float sh = EditorGUI.GetPropertyHeight(scl);


		//	/// Header

		//	bool _isExpanded = EditorGUI.Foldout(new Rect(r.xMin, currentline, r.width, TITL_HGHT), isExpanded.boolValue, "");
		//	if (isExpanded.boolValue != _isExpanded)
		//	{
		//		isExpanded.boolValue = _isExpanded;
		//		property.serializedObject.ApplyModifiedProperties();
		//	}

		//	EditorGUI.LabelField(new Rect(r.xMin, currentline, r.width, TITL_HGHT), gc);// property.displayName /*new GUIContent("Transform Crusher " + label)*//*, (GUIStyle)"BoldLabel"*/);

		//	int totalbits = target.TallyBits();
		//	int frag0bits = Mathf.Clamp(totalbits, 0, 64);
		//	int frag1bits = Mathf.Clamp(totalbits - 64, 0, 64);
		//	int frag2bits = Mathf.Clamp(totalbits - 128, 0, 64);
		//	int frag3bits = Mathf.Clamp(totalbits - 192, 0, 64);

		//	string bitstr = frag0bits.ToString();
		//	if (frag1bits > 0)
		//		bitstr += "|" + frag1bits;
		//	if (frag2bits > 0)
		//		bitstr += "|" + frag2bits;
		//	if (frag3bits > 0)
		//		bitstr += "|" + frag3bits;

		//	bitstr = bitstr + " bits";
		//	EditorGUI.LabelField(new Rect(paddedleft, currentline, paddedwidth, 16), bitstr, miniLabelRight);

		//	if (isExpanded.boolValue)
		//	{

		//		Rect ir = r; // EditorGUI.IndentedRect(r);;
		//		ir.yMin = ir.yMin + HEADR_HGHT + 2;
		//		ir.xMin -= BOUNDING_PADDING;
		//		ir.xMax += BOUNDING_PADDING;
		//		ir.yMax -= 6;
		//		currentline += BOUNDING_PADDING;
		//		//EditorGUI.LabelField(ir, GUIContent.none, /*(GUIStyle)"RectangleToolVBar");// */(GUIStyle)"HelpBox");
		//		ir = EditorGUI.IndentedRect(ir);
		//		//ir.xMin += 1; ir.xMax -= 1;
		//		//ir.yMin += 1; ir.yMax -= 1;
		//		SolidTextures.DrawTexture(ir, SolidTextures.darken202D);

		//		/// TRS Element Boxes
		//		currentline += TITL_HGHT;
		//		//float leftConnectorY = currentline;


		//		DrawSet(r, currentline, ph, pos);
		//		currentline += ph + SET_PAD;

		//		DrawSet(r, currentline, rh, rot);
		//		currentline += rh + SET_PAD;

		//		DrawSet(r, currentline, sh, scl);
		//		currentline += sh /*+ SET_PAD*/;

		//		/// Connecting line between TRS Elements
		//		//SolidTextures.DrawTexture(new Rect(4, leftConnectorY + 4, 4, currentline - leftConnectorY), SolidTextures.lowcontrast2D);
		//		//EditorGUI.LabelField(new Rect(0, leftConnectorY + 4, 4, currentline - leftConnectorY - 12), GUIContent.none, (GUIStyle)"MiniSliderVertical");
		//	}


		//	EditorGUI.EndProperty();

		//}


		//private void DrawSet(Rect r, float currentline, float h, SerializedProperty prop)
		//{
		//	//SolidTextures.DrawTexture(new Rect(4, currentline + 4, r.width, h - 12), SolidTextures.lowcontrast2D);
		//	//EditorGUI.LabelField(new Rect(2, currentline + 4, 4, h - 12), GUIContent.none, (GUIStyle)"MiniSliderVertical");

		//	EditorGUI.PropertyField(new Rect(r.xMin, currentline, r.width, h), prop);
		//}

		//public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		//{
		//	float ph = EditorGUI.GetPropertyHeight(property.FindPropertyRelative("posCrusher"));
		//	float rh = EditorGUI.GetPropertyHeight(property.FindPropertyRelative("rotCrusher"));
		//	float sh = EditorGUI.GetPropertyHeight(property.FindPropertyRelative("sclCrusher"));
		//	SerializedProperty isExpanded = property.FindPropertyRelative("isExpanded");

		//	float body = SPACING + (isExpanded.boolValue ? (ph + rh + sh + SET_PAD * 2) + BOUNDING_PADDING * 2 : 0);
		//	return TITL_HGHT + body/* + BTTM_MARGIN*/;
		//}
	}

}
#endif
