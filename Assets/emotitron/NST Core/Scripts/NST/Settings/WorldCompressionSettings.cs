////Copyright 2018, Davin Carten, All rights reserved

//using UnityEngine;
//using emotitron.Utilities.GUIUtilities;
//using emotitron.Compression;
//using emotitron.Debugging;


//#if UNITY_EDITOR
//using UnityEditor;
//#endif

//namespace emotitron.NST
//{

//#if UNITY_EDITOR
//	[HelpURL(HELP_URL)]
//#endif

//	[System.Obsolete("WorldBoundsSO.single how is home to WorldBounds crushers.")]
//	public class WorldCompressionSettings : SettingsScriptableObject<WorldCompressionSettings>
//	{

//		[Range(10, 1000)]
//		[Tooltip("Indicate the minimum resolution of any axis of compressed root positions (Subdivisions per 1 Unit). Increasing this needlessly will increase your network traffic. Decreasing it too much will result in objects moving in visible rounded increments.")]
//		[HideInInspector]
//		public int minPosResolution = 100;

//		//[Tooltip("If no NSTMapBounds are found in the scene, this is the size of the world that will be used by the root position compression engine.")]
//		//public Bounds defaultWorldBounds = new Bounds(new Vector3(0, 0, 0), new Vector3(640, 40, 640));

//		//public override string SettingsName { get { return "World Compression Settings"; } }
//		//public FloatCrusher[] worldCrusher = new FloatCrusher[3];
//		[HideInInspector]
//		public static ElementCrusher globalPosCrusher;

//		//public /*readonly*/ ElementCrusher boundsPosCrusher;
//		public /*readonly*/ ElementCrusher defaultPosCrusher;
//		//public ElementCrusher mapBoundsCrusher;

//		public WorldCompressionSettings()
//		{
//			//boundsPosCrusher = new ElementCrusher(TRSType.Position, false)
//			//{
//			//	enableLocalSelector = false,
//			//	xcrusher = new FloatCrusher(-100f, 100f, 100, Axis.X, TRSType.Position),
//			//	ycrusher = new FloatCrusher(-20f, 20f, 100, Axis.Y, TRSType.Position),
//			//	zcrusher = new FloatCrusher(-100f, 100f, 100, Axis.Z, TRSType.Position)
//			//};
//			defaultPosCrusher = new ElementCrusher(TRSType.Position, false)
//			{
//				enableLocalSelector = false,
//				XCrusher = new FloatCrusher(-100f, 100f, 100, Axis.X, TRSType.Position),
//				YCrusher = new FloatCrusher(-20f, 20f, 100, Axis.Y, TRSType.Position),
//				ZCrusher = new FloatCrusher(-100f, 100f, 100, Axis.Z, TRSType.Position)
//			};
//			globalPosCrusher = defaultPosCrusher;

//		}

		
//		/// <summary>
//		/// Change the axisranges for the world bounds to a new bounds.
//		/// </summary>
//		public static void SetWorldRanges(Bounds bounds, bool silent = false)
//		{
//			/// TODO: MOVE THIS
//			if (NSTMapBounds.ActiveBoundsObjCount > 0)
//				globalPosCrusher = NSTMapBounds.boundsPosCrusher;
//			else
//				globalPosCrusher = Single.defaultPosCrusher;
			
//			//var worldCompSettings = WorldCompressionSettings.Single;
//			var worldCrusher = NSTMapBounds.boundsPosCrusher;
//			//NSTSettings nstSettings = NSTSettings.EnsureExistsInScene(NSTSettings.DEFAULT_GO_NAME);
//			XDebug.LogWarning(!XDebug.logWarnings ? null :
//				("<b>Scene is missing map bounds</b>, defaulting to a map size of Center:" + NSTMapBounds.combinedWorldBounds.center + " Size:" + NSTMapBounds.combinedWorldBounds.size +
//				". Be sure to add NSTMapBounds components to your scene to define its bounds, or be sure the default bounds in NSTSettings are what you want."),
//				(!silent && Application.isPlaying && NSTMapBounds.ActiveBoundsObjCount == 0 && Time.time > 1)
//				);

//			XDebug.LogWarning(!XDebug.logWarnings ? null :
//				("<b>Scene map bounds are very small</b>. Current world bounds are " + bounds.center + " Size:" + bounds.size + ", is this intentional?" +
//				"If not check that your NSTMapBounds fully encompass your world as intended, or if using the Default bounds set in NSTSettings, that it is correct."),
//				(!silent && Application.isPlaying && NSTMapBounds.ActiveBoundsObjCount > 0 && (bounds.size.x <= 1 || bounds.size.y <= 1 || bounds.size.z <= 1))
//				);

//			//for (int axis = 0; axis < 3; axis++)
//			//{
//			//	worldCrusher[axis].SetRange((float)bounds.min[axis], (float)bounds.max[axis]); //, (uint)worldCompSettings.minPosResolution);
//			//}

//			XDebug.Log(
//				("Notice: Change in Map Bounds (Due to an NSTBounds being added or removed from the scene) to \n" +
//				"Center:" + bounds.center + " Size:" + bounds.size + ". Be sure this map change is happening to all networked clients or things will break badly. \n" +
//				"Position keyframes will use x:" + worldCrusher[0].Bits + " bits, y:" + worldCrusher[1].Bits + "bits, and z:" + worldCrusher[2].Bits +
//				" bits at the current minimum resolutons settings (in NST Settings)."), !silent && Application.isPlaying, true);
//		}

//		///// TODO: Add Clamp to ElementCrusher
//		//public static Vector3 ClampAxes(Vector3 value)
//		//{
//		//	globalPosCrusher.Clamp(value);
//		//	return new Vector3(
//		//		WorldCompressionSettings.globalPosCrusher[0].Clamp(value[0]),
//		//		WorldCompressionSettings.globalPosCrusher[1].Clamp(value[1]),
//		//		WorldCompressionSettings.globalPosCrusher[2].Clamp(value[2])
//		//		);
//		//}

//		//public ElementCrusher globalRotCrusher = new ElementCrusher(TRSType.Quaternion, false)
//		//{
//		//	enableLocalSelector = false,
//		//	qcrusher = new QuatCrusher(CompressLevel.uint32Med, false)
//		//	//ycrusher = new FloatCrusher(-20f, 20f, 100, Axis.Y, TRSType.Quaternion),
//		//	//zcrusher = new FloatCrusher(-100f, 100f, 100, Axis.Z, TRSType.Quaternion)
//		//};

//		//public ElementCrusher globalSclCrusher = new ElementCrusher(TRSType.Scale, false)
//		//{
//		//	uniformAxes = ElementCrusher.UniformAxes.XYZ,
//		//	enableLocalSelector = false,
//		//	ucrusher = new FloatCrusher(0f, 2f, 127, Axis.X, TRSType.Scale) { AccurateCenter = true }
//		//};

//#if UNITY_EDITOR

//		public override string AssetPath { get { return @"Assets/emotitron/NST Core 5/Resources/"; } }

//		public const string HELP_URL = "https://docs.google.com/document/d/1nPWGC_2xa6t4f9P0sI7wAe4osrg4UP0n_9BVYnr5dkQ/edit#bookmark=kix.59wxxn84tocg";
//		public override string HelpURL { get { return HELP_URL; } }
//		private static GUIContent sliderGuiContent = new GUIContent("Map Bounds res:", "The min resolution setting that will be used by NSTMapBounds");

//		public override void DrawGuiPre(bool asWindow)
//		{
//			NSTMapBounds.boundsPosCrusher.XCrusher.Resolution = (ulong)minPosResolution;
//			NSTMapBounds.boundsPosCrusher.YCrusher.Resolution = (ulong)minPosResolution;
//			NSTMapBounds.boundsPosCrusher.ZCrusher.Resolution = (ulong)minPosResolution;

//			EditorGUILayout.Space();

//			Rect r = EditorGUILayout.GetControlRect();

//			int res = EditorGUI.IntSlider(r, sliderGuiContent, minPosResolution, 0, 500);

//			if (minPosResolution != res)
//			{
//				Undo.RecordObject(this, "MapBounds resolution change");
//				minPosResolution = res;
//				EditorUtility.SetDirty(this);
//				AssetDatabase.SaveAssets();
//			}

//			EditorGUILayout.HelpBox(WorldBoundsSummary(), MessageType.None);

//			EditorGUILayout.HelpBox("Default settings are used when no NSTMapBounds exist in a scene.\n" + NSTMapBounds.ActiveBoundsObjCount + " NSTMapBounds sources currently active.", MessageType.None);

//		}

//		public override bool DrawGui(Object target, bool asFoldout, bool includeScriptField, bool initializeAsOpen = true, bool asWindow = false)
//		{

//			bool isExpanded = base.DrawGui(target, asFoldout, includeScriptField, initializeAsOpen, asWindow);


//			return isExpanded;
//		}

//		/// <summary>
//		/// Completely refinds and inventories ALL NstMapBounds in the scene, rather than dicking around trying to be efficient. 
//		/// This is editor only so just brute force will do... because I don't see an 'efficient' way.
//		/// </summary>
//		/// <returns></returns>
//		public static string WorldBoundsSummary()
//		{
//			NSTMapBounds.ResetActiveBounds();

//			// Find every damn NSTMapBounds in the scene currently and get its bounds
//			NSTMapBounds[] all = Object.FindObjectsOfType<NSTMapBounds>();

//			foreach (NSTMapBounds mb in all)
//				mb.CollectMyBounds();

//			NSTMapBounds.RecalculateWorldCombinedBounds();
//			NSTMapBounds.UpdateWorldBounds(true);

//			string str =
//				"World Bounds in current scene:\n" +
//				((NSTMapBounds.ActiveBoundsObjCount == 0) ?
//					("No Active NSTMapBounds - will use default.\n") :
//					("(" + NSTMapBounds.ActiveBoundsObjCount + " NSTMapBound(s) combined):\n")
//					) +

//				"Center: " + globalPosCrusher.Bounds.center + "\n" +
//				"Size: " + globalPosCrusher.Bounds.size + "\n" +
//				"Target Resolution: " + globalPosCrusher.XCrusher.Resolution + " actual: " + globalPosCrusher.XCrusher.GetResAtBits().ToString("0.00000") +
//				"\nTarget Precision " + globalPosCrusher.XCrusher.Precision.ToString("0.00000") + " actual: " + globalPosCrusher.XCrusher.GetPrecAtBits().ToString("0.00000") + "\n";

//				//"Root position keyframes will use:";

//			for (BitCullingLevel bcl = 0; bcl < BitCullingLevel.DropAll; bcl++)
//				str += 
//					"\n" + System.Enum.GetName(typeof(BitCullingLevel), bcl) + "\n" +
//					"x: " + globalPosCrusher[0].GetBits(bcl) + " bits, " +
//					"y: " + globalPosCrusher[1].GetBits(bcl) + " bits, " +
//					"z: " + globalPosCrusher[2].GetBits(bcl) + " bits, ";

//			return str;

//		}


//#endif
//	}

//#if UNITY_EDITOR

//	[System.Obsolete()]
//	[CustomEditor(typeof(WorldCompressionSettings))]
//	public class WorldCompressionSettingsEditor : SettingsSOBaseEditor<WorldCompressionSettings>
//	{
//		public override void OnInspectorGUI()
//		{
//			base.OnInspectorGUI();
			
//			WorldCompressionSettings.Single.DrawGui(target, false, true, true);
//		}
//	}
//#endif
//}

