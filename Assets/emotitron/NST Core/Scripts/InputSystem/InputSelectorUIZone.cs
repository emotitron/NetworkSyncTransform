//Copyright 2018, Davin Carten, All rights reserved

#if PUN_2_OR_NEWER || MIRROR || !UNITY_2019_1_OR_NEWER

using emotitron.Utilities.GUIUtilities;
using UnityEngine;
using emotitron.Debugging;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace emotitron.InputSystem
{
	[System.Serializable]
	public class InputSelectorUIZone : InputSelectorBase
	{
		public ClickTypes touchType = ClickTypes.ClickTap;
		public UIZoneSelector zoneSelector;
		private RectTransform rect;
		int screenWidthAtLastRecalc = 0;

		public override void Initialize(InputSelector _parent)
		{
			base.Initialize(_parent);
			UpdateRect();
		}

		public void ValidateZoneIdAndName()
		{
			// If the list is empty, may mean that UIZones haven't initialized
			UIZone.GetAllInScene();

			if (UIZone.list.Count == 0)
			{
				zoneSelector.ZoneId = 0;
				zoneSelector.ZoneName = "";
				return;
			}

			// If zoneId is out of range, use zero
			if (zoneSelector.ZoneId >= UIZone.list.Count)
			{
				zoneSelector.ZoneId = 0;
			}
			// If the selected name and id don't match, try to guess which one is correct
			if (UIZone.list[zoneSelector.ZoneId].itemName != zoneSelector.ZoneName)
			{
				// First try to find the itemName, if it exists we want that
				if (zoneSelector.ZoneName != "" && UIZone.lookup.ContainsKey(zoneSelector.ZoneName))
				{
					XDebug.LogWarning(!XDebug.logWarnings ? null : 
						("Component '" + parent.GetType() + "' references UIZone named '" + zoneSelector.ZoneName + "'. the index doesn't match though. Will use the new index for that zone name."));

					zoneSelector.ZoneId = UIZone.lookup[zoneSelector.ZoneName].index;
				}
				else
				{
					XDebug.LogWarning(!XDebug.logWarnings ? null : 
						("Component '" + parent.GetType() + "' references UIZone named '" + zoneSelector.ZoneName + "', but it no longer exists. '" + UIZone.list[zoneSelector.ZoneId].itemName + "' will be used instead"), zoneSelector.ZoneName != "");

					zoneSelector.ZoneName = UIZone.list[zoneSelector.ZoneId].itemName;
				}
			}
		}

		private void UpdateRect()
		{
			ValidateZoneIdAndName();

			if (UIZone.list.Count > 0)
				rect = UIZone.list[zoneSelector.ZoneId].GetComponent<RectTransform>();

			screenWidthAtLastRecalc = Screen.width;
		}

		float lastMouseDownTime;
		const float doubleClickThreshold = .5f;
		const float clickThreshold = .25f;
		const float holdThreshold = .5f;

		public override bool Test()
		{

			// Lightweight check for screen resize since last run.
			if (Screen.width != screenWidthAtLastRecalc)
				UpdateRect();

			if (Input.GetMouseButtonDown(0)) 
			{
				bool doubletap = (Time.time - lastMouseDownTime < doubleClickThreshold);
				lastMouseDownTime = Time.time;

				// is this a mouse down inside the rect?
				if (touchType == ClickTypes.Down && RectTransformUtility.RectangleContainsScreenPoint(rect, Input.mousePosition, null))
				{
					return true;
				}

				// is a double click
				if (touchType == ClickTypes.DblClickTap &&
					doubletap &&
					RectTransformUtility.RectangleContainsScreenPoint(rect, Input.mousePosition, null))
				{
					return true;
				}

			}

			if (Input.GetMouseButtonUp(0))
			{
				bool isClick = Time.time - lastMouseDownTime < clickThreshold;


				// Is this a mouseup?
				if (touchType == ClickTypes.Release && RectTransformUtility.RectangleContainsScreenPoint(rect, Input.mousePosition, null))
				{
					return true;

				}

				// Is this a Click?
				if (touchType == ClickTypes.ClickTap && isClick)
				{
					// Register the click time for dblclick detection

					if (RectTransformUtility.RectangleContainsScreenPoint(rect, Input.mousePosition, null))
					{
						return true;
					}

				}
			}

			if (Input.GetMouseButton(0) &&
				touchType == ClickTypes.Hold &&
				Time.time - lastMouseDownTime > holdThreshold &&
				RectTransformUtility.RectangleContainsScreenPoint(rect, Input.mousePosition, null))
			{
				{
					return true;
				}
			}


			for (int i = 0; i < Input.touchCount; i++)
			{
				Touch touch = Input.GetTouch(i);

				// First test for a touch type match - continue to next touch if not
				if (!TestTouchTypeMatch(touch))
					continue;
				
				// Then test if it is inside of the defined rect
				if (RectTransformUtility.RectangleContainsScreenPoint(rect, touch.position, null))
					return true;

			}
			return false;
		}

		private bool TestTouchTypeMatch(Touch touch)
		{
			if (touch.phase == TouchPhase.Began && touchType == ClickTypes.Down)
			{
				if (touchType == ClickTypes.Down)
					return true;
			}
			else if (touch.phase == TouchPhase.Ended)
			{
				if (touchType == ClickTypes.Release)
					return true;

				if (touchType == ClickTypes.ClickTap && touch.tapCount == 1)
					return true;

				if (touchType == ClickTypes.DblClickTap && touch.tapCount > 1)
					return true;
			}
			else if (touch.phase == TouchPhase.Stationary)
			{
				if (touchType == ClickTypes.Hold)
					return true;
			}
			return false;
		}

		public static Rect RectTransformToScreenSpace(RectTransform transform)
		{
			Vector2 size = Vector2.Scale(transform.rect.size, transform.lossyScale);
			Rect rect = new Rect(transform.position.x, Screen.height - transform.position.y, size.x, size.y);
			rect.x -= (transform.pivot.x * size.x);
			rect.y -= ((1.0f - transform.pivot.y) * size.y);
			return rect;
		}

	}

#if UNITY_EDITOR
	[CustomPropertyDrawer(typeof(InputSelectorUIZone))]
	[CanEditMultipleObjects]
	public class CustomInputUIZone : InputSelectionBaseDrawer
	{
		public override void OnGUI(Rect r, SerializedProperty property, GUIContent label)
		{
			InputSelector inputSelector = PropertyDrawerUtility.GetParent(property) as InputSelector;
			InputSelectorUIZone _target = inputSelector.inputSelection as InputSelectorUIZone;

			// Tell the base class that this is a touch/click item - so the modkeys are not needed. 
			isTouch = true;

			base.OnGUI(r, property, label);

			SerializedProperty touchTypes = property.FindPropertyRelative("touchType");
			SerializedProperty zoneSelector = property.FindPropertyRelative("zoneSelector");

			{
				if (_target != null)
				{
					touchTypes.enumValueIndex = (int)(ClickTypes)EditorGUI.EnumPopup(new Rect(firstFieldLeft, r.yMin, halfFieldWidth, 16), GUIContent.none, (ClickTypes)touchTypes.enumValueIndex);
					EditorGUI.PropertyField(new Rect(secondFieldLeft, r.yMin, halfFieldWidth, 16), zoneSelector);
				}
				
			}
		}
	}
#endif
}

#endif