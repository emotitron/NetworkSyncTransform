//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using UnityEditor;
//using emotitron.NST;
//using emotitron.Compression;
//using emotitron.Utilities.GUIUtilities;


//namespace emotitron.Network.Compression
//{


//#if UNITY_EDITOR

//	[CustomPropertyDrawer(typeof(ScaleElement))]
//	[CanEditMultipleObjects]

//	public class ScaleElementEditor : TransformElementDrawer
//	{
//		SerializedProperty uniformScale;
//		SerializedProperty compression;
//		ScaleElement se;

//		public override void OnGUI(Rect r, SerializedProperty property, GUIContent label)
//		{
//			base.OnGUI(r, property, label);

//			SerializedProperty drawerHeight = property.FindPropertyRelative("drawerHeight");
//			se = PropertyDrawerUtility.GetActualObjectForSerializedProperty<ScaleElement>(fieldInfo, property);

//			if (!noUpdates)
//			{
//				compression = property.FindPropertyRelative("compression");
//				uniformScale = property.FindPropertyRelative("uniformScale");
//				//SerializedProperty includedAxes = property.FindPropertyRelative("includedAxes");
//				SerializedProperty axisRanges = property.FindPropertyRelative("axisRanges");
//				SerializedProperty xrange = axisRanges.GetArrayElementAtIndex(0);
//				SerializedProperty yrange = axisRanges.GetArrayElementAtIndex(1);
//				SerializedProperty zrange = axisRanges.GetArrayElementAtIndex(2);

//				EditorGUI.PropertyField(new Rect(left, currentLine, r.width, LINEHEIGHT), compression);
//				currentLine += LINEHEIGHT + 2;
//				EditorGUI.PropertyField(new Rect(left, currentLine, r.width, LINEHEIGHT), uniformScale);

//				if (se.uniformScale)
//				{
//					DrawAxis(se, 0, xrange, se.axisRanges[0], se.uniformScale, "Uniform Scale", gray);
//				}
//				else
//				{
//					bool x = DrawAxis(se, 0, xrange, se.axisRanges[0], se.uniformScale, "Use X", red);
//					bool y = DrawAxis(se, 1, yrange, se.axisRanges[1], se.uniformScale, "Use Y", green);
//					bool z = DrawAxis(se, 2, zrange, se.axisRanges[2], se.uniformScale, "Use Z", blue);

//					se.includedAxes = (IncludedAxes)((x ? 1 : 0) | (y ? 2 : 0) | (z ? 4 : 0));
//				}

//			}

//			// revert to original indent level.
//			EditorGUI.indentLevel = savedIndentLevel;

//			// Record the height of this instance of drawer
//			currentLine += LINEHEIGHT + margin * 2 + margin;
//			drawerHeight.floatValue = currentLine - r.yMin;

//			EditorGUI.EndProperty();

//		}

//		private bool DrawAxis(ScaleElement se, int axis, SerializedProperty range, FloatRange axisRange, bool isUniformScale, string name, Color color)
//		{
//			currentLine += LINEHEIGHT + 8;

//			bool showrange = se.includedAxes.IsXYZ(axis) && ((Compression)compression.intValue == Compression.LocalRange);

//			float headerheight = 18;

//			float rangeheight = showrange ? LINEHEIGHT + 8 : 0;
//			EditorGUI.DrawRect(new Rect(left - 1, currentLine - 3, realwidth - 3 + 2, headerheight + rangeheight + 2), Color.black);
//			EditorGUI.DrawRect(new Rect(left, currentLine - 2, realwidth - 3, headerheight + rangeheight), color);
//			EditorGUI.DrawRect(new Rect(left, currentLine - 2, realwidth - 3, headerheight), color * .9f);

//			EditorGUI.LabelField(new Rect(left + margin + 18, currentLine, 100, LINEHEIGHT), new GUIContent(name), lefttextstyle);
//			bool includeAxis = EditorGUI.Toggle(new Rect(left + margin + 0, currentLine - 1, 32, LINEHEIGHT), GUIContent.none, se.includedAxes.IsXYZ(axis));

//			int bits = (includeAxis) ?
//				compression.enumValueIndex == 1 ? 16 :
//				compression.enumValueIndex == 2 ? FloatRange.GetBitsForRangeAndRez(axisRange.Min, axisRange.Max, axisRange.Resolution) :
//				32 :
//				0;

//			GUI.Label(new Rect(left, currentLine - 1, realwidth - 16, LINEHEIGHT), "Bits used: " + bits, "MiniLabelRight");

//			if (showrange)
//			{
//				currentLine += LINEHEIGHT + 4;
//				EditorGUI.PropertyField(new Rect(left + 10, currentLine, realwidth - 16, LINEHEIGHT), range, GUIContent.none); //new GUIContent(compression.enumValueIndex.ToString()));
//				currentLine += 4;

//			}

//			return includeAxis;
//		}
//	}
//}
//#endif





//	[CustomPropertyDrawer(typeof(FloatRange))]
//	[CanEditMultipleObjects]

//	public class AxisRangeDrawer : PropertyDrawer
//	{
//		protected float rows = 4;

//		protected const int LINEHEIGHT = 16;

//		protected float margin = 4;
//		protected float realwidth;
//		protected float colwidths;
//		protected float currentLine;
//		protected int savedIndentLevel;


//		//SerializedProperty axis, min, max, rez;
//		//private GUIStyle lefttextstyle = new GUIStyle
//		//{
//		//	alignment = TextAnchor.UpperLeft
//		//};
//		//private GUIStyle centertextstyle = new GUIStyle
//		//{
//		//	alignment = TextAnchor.UpperCenter
//		//};
//		private GUIStyle righttextstyle = new GUIStyle
//		{
//			alignment = TextAnchor.UpperRight
//		};

//		public override void OnGUI(Rect r, SerializedProperty property, GUIContent label)
//		{
//			savedIndentLevel = EditorGUI.indentLevel;
//			EditorGUI.indentLevel = 1; //(isRoot.boolValue) ? 1 : 2;

//			//SerializedProperty axis = property.FindPropertyRelative("axis");
//			SerializedProperty min = property.FindPropertyRelative("min");
//			SerializedProperty max = property.FindPropertyRelative("max");
//			SerializedProperty rez = property.FindPropertyRelative("resolution");
//			//SerializedProperty useAxis = property.FindPropertyRelative("useAxis");

//			//GUIContent[] temp = new GUIContent[2] { new GUIContent("x"), new GUIContent("y") };

//			//EditorGUI.BeginProperty(r, label, property);

//			//int realleft = 13;
//			realwidth = r.width;
//			//float halfwidth = r.width / 2f;
//			float thirdwidth = r.width / 2.4f;
//			float minvaluearea = 230f;
//			//float split = Mathf.Max(64f,  Mathf.Max(thirdwidth, 100));// Mathf.Max(130, thirdwidth);
//			float split = realwidth - (Mathf.Max(thirdwidth, minvaluearea));

//			//float itemspace = 60f;
//			float labeloffset = -40;
//			float fieldwidth = 56;
//			//float colstart = split;
//			float colend = realwidth;
//			float colarea = realwidth - split;
//			float col3 = realwidth - colarea * .66f + 13;
//			float col2 = realwidth - colarea * .33f + 13;

//			//Color color = (axis.intValue == 0) ? TransformElementDrawer.red : (axis.intValue == 1) ? TransformElementDrawer.green : TransformElementDrawer.blue;

//			//EditorGUI.DrawRect(new Rect(r.xMin -1, r.yMin -1, r.width + 2, r.height + 2), new Color(.25f, .25f, .25f));
//			//EditorGUI.DrawRect(new Rect(r.xMin, r.yMin, r.width, r.height), color);

//			//string axislabel = (axis.intValue == 0) ? "X" : (axis.intValue == 1) ? "Y" : "Z";
//			//EditorGUI.LabelField(new Rect(r.xMin - 13, r.y, 100, 16), new GUIContent(axislabel));


//			// Because Unity serialization... is well... passing info through the label string.

//			//int compression;
//			//System.Int32.TryParse(label.text, out compression);
//			//bool showRanges = (NST.Internal.Compression)compression == NST.Internal.Compression.LocalRange; // && useAxis.boolValue;

//			//float bottomheight = LINEHEIGHT + 6;

//			//EditorGUI.DrawRect(new Rect(margin + 7, r.yMin - 3, realwidth + 2, LINEHEIGHT + bottomheight + 2), Color.black);
//			//EditorGUI.DrawRect(new Rect(margin + 8, r.yMin - 2, realwidth, LINEHEIGHT + bottomheight), color);
//			//EditorGUI.DrawRect(new Rect(margin + 8, r.yMin - 2, realwidth, LINEHEIGHT + 1), color * .5f);
//			//EditorGUI.DrawRect(new Rect(margin + 7, r.yMin - 3, realwidth + 2, bottomheight + 2), Color.black);
//			//EditorGUI.DrawRect(new Rect(margin + 8, r.yMin - 2, realwidth, bottomheight), color);
//			//EditorGUI.DrawRect(new Rect(margin + 8, r.yMin - 2, realwidth, 1), color * .5f);

//			//axislabel = (axis.intValue == 0) ? "X" : (axis.intValue == 1) ? "Y" : "Z";

//			//EditorGUI.LabelField(new Rect(r.xMin + 12, r.yMin - 1, 100, 14), new GUIContent(axislabel), TransformElementDrawer.lefttextstyle);

//			//EditorGUI.PropertyField(new Rect(r.xMin - 8, r.yMin - 2, 32, 16), useAxis, GUIContent.none);


//			//if (showRanges)
//			//{
//				float rowoffset = r.yMin;
//				EditorGUI.LabelField(new Rect(col2 + labeloffset + 4, rowoffset, 0, 16), new GUIContent("max"), righttextstyle);
//				EditorGUI.PropertyField(new Rect(col2 - fieldwidth + 8, rowoffset, fieldwidth, 16), max, GUIContent.none);

//				EditorGUI.LabelField(new Rect(col3 + labeloffset + 4, rowoffset, 0, 16), new GUIContent("min"), righttextstyle);
//				EditorGUI.PropertyField(new Rect(col3 - fieldwidth + 8, rowoffset, fieldwidth, 16), min, GUIContent.none);

//				EditorGUI.LabelField(new Rect(colend + labeloffset + 4, rowoffset, 0, 16), new GUIContent("res"), righttextstyle);
//				EditorGUI.PropertyField(new Rect(colend - fieldwidth + 8, rowoffset, fieldwidth, 16), rez, GUIContent.none);

//			//}

//			EditorGUI.indentLevel = savedIndentLevel;

//			//EditorGUI.EndProperty();
//		}

//		public void DrawAxis(string axisStr, Rect r)
//		{
//			//EditorGUI.DrawRect(new Rect(margin + 7, r.yMin - 3, realwidth + 2, LINEHEIGHT * 2 + 12), Color.black);
//			//EditorGUI.DrawRect(new Rect(margin + 8, r.yMin - 2, realwidth, LINEHEIGHT * 2 + 10), color);
//			//EditorGUI.DrawRect(new Rect(margin + 8, r.yMin - 2, realwidth, LINEHEIGHT + 4), color * .5f);

//			string axislabel = axisStr;
//			EditorGUI.LabelField(new Rect(r.xMin + 8, r.yMin, 100, 14), new GUIContent(axislabel));

//			//EditorGUI.LabelField(new Rect(0, currentLine, colwidths, LINEHEIGHT), new GUIContent(axisStr + " bits:"), lefttextstyle);
//			//EditorGUI.PropertyField(new Rect(0 + 56, currentLine, realwidth - 56 - 16, LINEHEIGHT), bits, GUIContent.none); // new GUIContent("x Bits"));
//			//currentLine += LINEHEIGHT + 4;

//			//EditorGUI.PropertyField(new Rect(0, currentLine, 32, LINEHEIGHT), limitRange, GUIContent.none);
//			//if (limitRange.boolValue == true)
//			//{
//			//	EditorGUI.PropertyField(new Rect(30, currentLine, 52, LINEHEIGHT), minVal, GUIContent.none, true); // new GUIContent("x Bits"));
//			//	EditorGUI.PropertyField(new Rect(realwidth - 62, currentLine, 52, LINEHEIGHT), maxVal, GUIContent.none, true); // new GUIContent("x Bits"));

//			//	float tempfloat = minVal.floatValue;
//			//	float tempfloat2 = maxVal.floatValue;
//			//	EditorGUI.MinMaxSlider(new Rect(80, currentLine, realwidth - 80 - 60, LINEHEIGHT), ref tempfloat, ref tempfloat2, -360, 360);
//			//	minVal.floatValue = Mathf.Max((int)tempfloat, -360);
//			//	maxVal.floatValue = Mathf.Min((int)tempfloat2, 360);

//			//}
//			//else
//			//{
//			//	EditorGUI.LabelField(new Rect(margin + 24, currentLine, colwidths, LINEHEIGHT), new GUIContent("Range"), lefttextstyle);
//			//}

//			currentLine += 28;
//		}

//		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
//		{
//			return base.GetPropertyHeight(property, label) * rows;  // assuming original is one row
//		}
//	}
//}


