#if PUN_2_OR_NEWER || MIRROR || !UNITY_2019_1_OR_NEWER

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace emotitron.InputSystem
{
	public enum TouchZones
	{
		TopLeft = 1,	TopMid = 2, TopRight = 4,
		CenterLeft = 8, CenterMid = 16, CenterRight = 32,
		BottomLeft = 64, BottomMid = 128, BottomRight = 256,
		LeftHalf = 512, RightHalf = 1024
	} 

	[System.Serializable]
	public class InputSelectorTouchArea : InputSelectorBase
	{
		public ClickTypes touchType = ClickTypes.ClickTap;
		public int touchZones = (int)TouchZones.RightHalf;

		int screenWidthAtLastRecalc = 0;
		int colOneThird, colTwoThird, rowOneThird, rowTwoThird;
		int colHalf;

		public InputSelectorTouchArea (ClickTypes _touchType)
		{
			touchType = _touchType;
		}

		private void UpdateZoneRects()
		{
			screenWidthAtLastRecalc = Screen.width;
			colOneThird = Screen.width / 3;
			colTwoThird = colOneThird + colOneThird;
			rowOneThird = Screen.height / 3;
			rowTwoThird = rowOneThird + rowOneThird;
			colHalf = Screen.width / 2;
		}

		public override void Initialize(InputSelector _parent)
		{
			base.Initialize(_parent);
			UpdateZoneRects();
		}
		public override bool Test()
		{
			ClickTypes currentTouchState = 0;

			for (int i = 0; i < Input.touchCount; i++)
			{

				 
				Touch touch = Input.GetTouch(i);

				if (touch.phase == TouchPhase.Began)
				{
					currentTouchState |= ClickTypes.Down;
				}
				if (touch.phase == TouchPhase.Ended)
				{
					currentTouchState |= ClickTypes.Release;
					if (touch.tapCount > 0)
						currentTouchState |= ClickTypes.Release;
				}

				// Don't bother checking for a zone match if there wasn't a touchtype match
				if ((currentTouchState & touchType) == 0)
					continue;

				// Here in case the screen gets resized after initialization.
				if (Screen.width != screenWidthAtLastRecalc)
					UpdateZoneRects();

				// Check for zone match
				int touchThirdCol = (touch.position.x < colOneThird) ? 0 : (touch.position.x < colTwoThird) ? 1 : 2;
				int touchThirdRow = (touch.position.y < rowOneThird) ? 0 : (touch.position.x < rowTwoThird) ? 1 : 2;

				return
					(((TouchZones)touchZones == TouchZones.BottomLeft) && touchThirdCol == 0 && touchThirdRow == 0) ||
					(((TouchZones)touchZones == TouchZones.BottomMid) && touchThirdCol == 1 && touchThirdRow == 0) ||
					(((TouchZones)touchZones == TouchZones.BottomRight) && touchThirdCol == 2 && touchThirdRow == 0) ||

					(((TouchZones)touchZones == TouchZones.CenterLeft) && touchThirdCol == 0 && touchThirdRow == 1) ||
					(((TouchZones)touchZones == TouchZones.CenterMid) && touchThirdCol == 1 && touchThirdRow == 1) ||
					(((TouchZones)touchZones == TouchZones.CenterRight) && touchThirdCol == 2 && touchThirdRow == 1) ||

					(((TouchZones)touchZones == TouchZones.TopLeft) && touchThirdCol == 0 && touchThirdRow == 2) ||
					(((TouchZones)touchZones == TouchZones.TopMid) && touchThirdCol == 1 && touchThirdRow == 2) ||
					(((TouchZones)touchZones == TouchZones.TopRight) && touchThirdCol == 2 && touchThirdRow == 2) ||

					(((TouchZones)touchZones == TouchZones.LeftHalf) && touch.position.x < colHalf) ||
					(((TouchZones)touchZones == TouchZones.RightHalf) && touch.position.x > colHalf);

			}
			return false;
		}
	}

#if UNITY_EDITOR
	[CustomPropertyDrawer(typeof(InputSelectorTouchArea))]
	[CanEditMultipleObjects]
	public class CustomInputTouchZones : InputSelectionBaseDrawer
	{
		public override void OnGUI(Rect r, SerializedProperty property, GUIContent label)
		{
			base.OnGUI(r, property, label);
			isTouch = true;

			SerializedProperty touchTypes = property.FindPropertyRelative("touchType");
			SerializedProperty touchZones = property.FindPropertyRelative("touchZones");

			touchTypes.enumValueIndex = (int)(ClickTypes)EditorGUI.EnumPopup(new Rect(firstFieldLeft, r.yMin, halfFieldWidth, 16), GUIContent.none, (ClickTypes)touchTypes.enumValueIndex);

#if UNITY_2017_3_OR_NEWER
			touchZones.intValue = (int)(TouchZones)EditorGUI.EnumFlagsField(new Rect(secondFieldLeft, r.yMin, halfFieldWidth, 16), GUIContent.none, (TouchZones)touchZones.intValue);
#else
			touchZones.intValue = (int)(TouchZones)EditorGUI.EnumMaskField(new Rect(secondFieldLeft, r.yMin, halfFieldWidth, 16), GUIContent.none, (TouchZones)touchZones.intValue);
#endif
		}
	}
#endif
}


#endif