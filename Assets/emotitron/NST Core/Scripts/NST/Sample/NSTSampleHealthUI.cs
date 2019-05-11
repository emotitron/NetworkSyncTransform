//Copyright 2018, Davin Carten, All rights reserved

#if PUN_2_OR_NEWER || MIRROR || !UNITY_2019_1_OR_NEWER

using UnityEngine;
using UnityEngine.UI;
using emotitron.NST.HealthSystem;
using emotitron.Debugging;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace emotitron.NST.Sample
{
	[AddComponentMenu("NST/Sample Code/NST Sample Health UI")]
	/// <summary>
	/// Link a graphics text, fill amount or X scale to an object vitals stat. If used on a NST object with a IVitals interface - it will link to that
	/// objects vitals, otherwise it will link to the Local Players vitals.
	/// </summary>
	public class NSTSampleHealthUI : MonoBehaviour, IMonitorVitals
	{
		[HideInInspector] public NetworkSyncTransform nst;

		public enum Monitor { Auto, Self, LocalPlayer }

		[HideInInspector]
		public Monitor monitor = Monitor.Auto;

		[HideInInspector]
		public GameObject vitalsSrcGO;

		//[Help("Apply this to UI Text or UI Image (with Image Type set to Filled) to bind it to selected health stat of the local player.")]
		[HideInInspector] public int monitoredVitalId;

		public IVitals iVitals;
		[Tooltip("If a Canvas is supplied in the inspector, this will attempt to find the first Text / Image in its children and use that.")]
		public Canvas canvas;

		//Graphic graphic;
		public Text UIText;
		public Image UIImage;
		public bool searchChildren = true;

		private const string PLACEHOLDER_CANVAS_NAME = "PLACEHOLDER_VITALS_CANVAS";

		bool isFilled;

		private void Awake()
		{
			// Find the source for our Vitals updates
			if (monitor == Monitor.Auto || monitor == Monitor.Self)
				iVitals = transform.root.GetComponent<IVitals>();

			nst = transform.root.GetComponent<NetworkSyncTransform>();

		}

		/// <summary>
		/// Finds UI Elements. If none found will create them and return false to indicate that placeholders are being used.
		/// </summary>
		/// <returns></returns>
		public bool FindUIElement()
		{
			// First see if this is on a canvas, if so note it
			if (!canvas)
				canvas = GetComponent<Canvas>();

			// Is this a placeholder canvas we just created for another bar?
			if (canvas && canvas.gameObject.name == PLACEHOLDER_CANVAS_NAME)
			{
				// This means another HealthUI created a placeholder, so we will use that canvas.
			}
			// If we have a canvas then search for the text/images
			else if (canvas)
			{
				if (canvas.gameObject.name != PLACEHOLDER_CANVAS_NAME)
				{
					if (!UIText)
						UIText = (searchChildren) ? canvas.GetComponentInChildren<Text>() : canvas.GetComponent<Text>();

					if (!UIImage)
						UIImage = (searchChildren) ? canvas.GetComponentInChildren<Image>() : canvas.GetComponent<Image>();

				}
			}
			// No canvas, so this may be the UI Element itself this is attached to
			else
			{
				if (!UIText)
					UIText = (searchChildren) ? GetComponentInChildren<Text>() : GetComponent<Text>();

				if (!UIImage)
					UIImage = (searchChildren) ? GetComponentInChildren<Image>() : GetComponent<Image>();
			}

			// If nothing was found after all of that - we need to make a canvas and UI elements.
			if (!UIText && !UIImage)
			{
				XDebug.LogWarning(!XDebug.logWarnings ? null : ("NSTSampleHealthUI on gameobject '" + name + "' cannot find any UI Canvas, Text or Image. Will create some placeholders until you supply them."), nst);
				XDebug.LogError(!XDebug.logErrors ? null : ("NSTSampleHealthUI on gameobject '" + name + "' cannot find a NetworkSyncTransform, UI Text, or UI Image component. Be sure the object we are attaching to conains one of those."), !nst);

				// Put some bars of this things head if it is an NST - Otherwise they won't make much sense on other objects.
				if (nst)
					CreatePlaceholderVitalBar(monitoredVitalId);

				return false;
			}
			return true;
		}

		private void Start()
		{
			FindUIElement();

			// If this UI element is not attached to a nst object with IVitals then register local player static iMonitor 
			// has to be static because no Vitals will exist until players join.
			if (iVitals == null)
			{
				NSTSampleHealth.lclPlayerMonitors.Add(this);
			}
		}

		private void OnDestroy()
		{
			if (iVitals == null)
			{
				NSTSampleHealth.lclPlayerMonitors.Remove(this);
			}
		}

		public void OnVitalsChange(IVitals vitals)
		{
			UpdateGraphics(vitals);
		}

		public void UpdateGraphics(IVitals vitals)
		{
			Vital vital = vitals[monitoredVitalId];

			if (vital == null)
				return;

			if (UIText != null)
			{
				UIText.text = ((int)vital.Value).ToString();
			}

			// Resize the healthbar
			if (UIImage != null)
			{
				if (UIImage.type == Image.Type.Filled && UIImage.sprite != null)
					UIImage.fillAmount = vital.Value / vital.maxValue;
				else
					UIImage.rectTransform.localScale = new Vector3(
						vital.Value / vital.maxValue,
						UIImage.rectTransform.localScale.y,
						UIImage.rectTransform.localScale.z);
			}
		}

		/// <summary>
		/// Bunch of thrown together stuff to make a placeholder UI that hovers over an object
		/// </summary>
		/// <param name="vitalId"></param>
		private void CreatePlaceholderVitalBar(int vitalId)
		{
			if (!canvas)
				CreatePlaceholderCanvas();

			GameObject barGO = new GameObject("VitalBar " + vitalId);
			barGO.transform.parent = canvas.transform;

			UIImage = barGO.AddComponent<Image>();
			UIImage.rectTransform.sizeDelta = new Vector2(2f, .2f);
			UIImage.color = (vitalId == 0) ? Color.red : (vitalId == 1) ? Color.yellow : Color.blue;

			GameObject textGO = new GameObject("Vitaltext " + vitalId);
			textGO.transform.parent = canvas.transform;

			UIText = textGO.AddComponent<Text>();
			UIText.resizeTextForBestFit = true;
			UIText.rectTransform.sizeDelta = new Vector2(2, .25f);
			UIText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
			UIText.alignment = TextAnchor.MiddleCenter;
			UIText.resizeTextMinSize = 0;
			UIText.text = "100";
			UIText.color = Color.black;

			// Stagger vitals bars based on their index
			float heightOffset = vitalId * .21f;
			barGO.transform.localPosition = new Vector3(0, heightOffset, 0);
			textGO.transform.localPosition = new Vector3(0, heightOffset, 0);
			barGO.transform.localRotation = Quaternion.identity;
			textGO.transform.localRotation = Quaternion.identity;
		}

		private void CreatePlaceholderCanvas()
		{
			// Make sure we haven't already created a placeholder canvas for some other vitalsUi
			Transform canvasT = gameObject.transform.Find(PLACEHOLDER_CANVAS_NAME);

			// We seem to have already made a canvas for some other UI element, we will use that.
			if (canvasT)
			{
				canvas = canvasT.GetComponent<Canvas>();
				return;
			}

			// Either create a new canvas, or use our already made placeholder canvas.
			GameObject canvasGO = new GameObject(PLACEHOLDER_CANVAS_NAME);
			canvasGO.transform.parent = transform;
			canvasGO.transform.localRotation = Quaternion.identity;
			canvasGO.transform.localPosition = Vector3.zero;

			canvas = canvasGO.AddComponent<Canvas>();
			canvas.GetComponent<RectTransform>().sizeDelta = new Vector2( 3f, 3f);
			CanvasScaler cscaler = canvasGO.AddComponent<CanvasScaler>();
			cscaler.dynamicPixelsPerUnit = 100f;
			canvas.renderMode = RenderMode.WorldSpace;

			Bounds parBounds = Utilities.BoundsTools.CollectMyBounds(transform.root.gameObject, Utilities.BoundsTools.BoundsType.Both);
			float heightOffset = ((parBounds.center.y -gameObject.transform.root.position.y) + parBounds.extents.y) * 1.1f;

			canvas.transform.localPosition = new Vector3(0 ,heightOffset, 0);
		}
	}


#if UNITY_EDITOR

	[CustomEditor(typeof(NSTSampleHealthUI))]
	[CanEditMultipleObjects]
	public class NSTSampleHealthUIEditor : NSTSampleHeader
	{
		// reusable array for building out list of vital names
		private static GUIContent[] vitalnames = new GUIContent[16];

		public override void OnInspectorGUI()
		{
			NSTSampleHealthUI _target = (NSTSampleHealthUI)target;
			IVitals ivitals = null;
			
			base.OnInspectorGUI();

			EditorGUILayout.HelpBox("This component should be placed on a UI Text or UI Image (with Image Type set to Filled ideally). " +
				"It can monitor IVitals on the root of this object, or of the local player gameobject.", MessageType.None);

			string monitorTooltip = "Which IVital should be be monitoring? Auto will try to find an IVital interface on the root of this object first, and if it can't find one will subscribe to the local player IVital.";
			_target.monitor = (NSTSampleHealthUI.Monitor)EditorGUILayout.EnumPopup(new GUIContent("IVitals Source", monitorTooltip), _target.monitor );

			//bool monitorSelf =_target.monitor == NSTSampleHealthUI.Monitor.Self;
			//bool monitorLclPlayer = _target.monitor == NSTSampleHealthUI.Monitor.LocalPlayer;

			//// if monitoring self is an option, try to take it - see if we have vitals
			//if (!monitorLclPlayer)
			//	ivitals = _target.transform.root.GetComponent<IVitals>();

			//// failed to find vitals or we are only set to monitor the local player
			//if (ivitals == null && !monitorSelf)
			//{
			//	// make the GO selector available if no vitals were found on this object
			//	string gotooltip = "Used by the editor only. Vital names in the list below are pulled from the IVitals on this gameobject.";
			//	_target.vitalsSrcGO = (GameObject)EditorGUILayout.ObjectField(new GUIContent("Player GameObj", gotooltip), _target.vitalsSrcGO, typeof(GameObject), true);

			//	if (_target.vitalsSrcGO == null)
			//	{
			//		if (MasterNetAdapter.NetLib == NetworkLibrary.UNET)
			//			_target.vitalsSrcGO = MasterNetAdapter.UNET_GetRegisteredPlayerPrefab();

			//		else if (MasterNetAdapter.NetLib == NetworkLibrary.PUN || MasterNetAdapter.NetLib == NetworkLibrary.PUN2)
			//			_target.vitalsSrcGO = PUNSampleLauncher.Single.playerPrefab;
			//	}

			//	if (_target.vitalsSrcGO != null)
			//		ivitals = _target.vitalsSrcGO.GetComponent<IVitals>();
			//}

			int i = 0;

			// if we found an iVitals... scrape out the names
			if (ivitals != null)
				for  (i = 0; i < ivitals.Vitals.Count; i++)
					vitalnames[i] = (new GUIContent(i.ToString() + " " + ivitals.Vitals[i].name));

			// if not, just make a list of numbers
			for (int j =i ; j < 16; j++)
				vitalnames[j] = (new GUIContent(j.ToString()));

			
			string vitalIdTooltip = "Which vital to monitor.";
			GUIContent label = new GUIContent("Monitored Vital", vitalIdTooltip);
			_target.monitoredVitalId = EditorGUILayout.Popup(label, _target.monitoredVitalId, vitalnames);

			serializedObject.ApplyModifiedProperties();

		}
	}
#endif

}

#endif