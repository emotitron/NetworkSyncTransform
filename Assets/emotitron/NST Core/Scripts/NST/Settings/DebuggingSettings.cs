//Copyright 2018, Davin Carten, All rights reserved

#if PUN_2_OR_NEWER || MIRROR || !UNITY_2019_1_OR_NEWER

using UnityEngine;
using emotitron.Debugging;
using emotitron.Utilities.GUIUtilities;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace emotitron.NST
{
#if UNITY_EDITOR
	[HelpURL(HELP_URL)]
#endif

	public class DebuggingSettings : SettingsScriptableObject<DebuggingSettings>
	{
		private const string contionalremoval = "All DebugX methods are conditionally removed from release builds, so you do not need to remove them manually from your final code.";

		[Tooltip("Enable/Disable DebgugX.LogError calls. " + contionalremoval)]
		public bool logErrors = true;

		[Tooltip("Enable/Disable DebgugX.LogWarning calls." + contionalremoval)]
		public bool logWarnings = true;
		
		[Tooltip("Enable/Disable DebgugX.Log calls. " + contionalremoval)]
		public bool logTestingInfo = false;

		[Space]
		[Tooltip("Enable/Disable forwarding of Console.WriteLine() calls to Debug.Log(). " + contionalremoval)]
		public bool logConsole = false;
		[Tooltip("Enable/Disable forwarding of Console.Error.WriteLine() calls to Debug.LogError(). " + contionalremoval)]
		public bool logConsoleErrors = true;
		[Tooltip("Enable/Disable forwarding of Console.Error.WriteLine() calls to Debug.LogError(). " + contionalremoval)]
		public bool logAssertFails = true;

		[Tooltip("Put itemized summaries of update bandwidth usage into the Debug.Log. This may affect performance in Debug, however " + contionalremoval)]
		public bool logDataUse = false;

#if UNITY_EDITOR
		public override string SettingsName { get { return "Debugging Settings"; } }
#endif
		private void OnValidate()
		{
			Initialize();
		}

		protected override void OnEnable()
		{
			base.OnEnable();
			Initialize();
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
		static void Bootstrap()
		{
			var single = Single;
		}

		public override void Initialize()
		{
			single = this;
			// Let the conditional debug know what to show
			XDebug.logInfo = Single.logTestingInfo;
			XDebug.logWarnings = Single.logWarnings;
			XDebug.logErrors = Single.logConsoleErrors;

			XDebug.ForwardTraceListener(logAssertFails);
			XDebug.RedirectConsoleErrorToDebug(logConsoleErrors);
		}

#if UNITY_EDITOR

		public override string AssetPath { get { return @"Assets/emotitron/NST Core/Resources/"; } }

		public const string HELP_URL = "https://docs.google.com/document/d/1nPWGC_2xa6t4f9P0sI7wAe4osrg4UP0n_9BVYnr5dkQ/edit#bookmark=kix.10q089trf9ig";
		public override string HelpURL { get { return HELP_URL; } }

		public override bool DrawGui(Object target, bool isAsset, bool includeScriptField, bool initializeAsOpen = true, bool asWindow = false)
		{
			var isExpanded = base.DrawGui(target, isAsset, includeScriptField, initializeAsOpen, asWindow);

			if (isExpanded)
				EditorGUILayout.HelpBox("All log options are for editor/debug only and will be conditionally purged from all builds. No need to disable these for releases.", MessageType.None);

			return isExpanded;
		}
#endif
	}

#if UNITY_EDITOR

	[CustomEditor(typeof(DebuggingSettings))]
	public class DebuggingSettingsEditor : SettingsSOBaseEditor<DebuggingSettings>
	{
		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();
			DebuggingSettings.Single.DrawGui(target, false, true, true);
		}
	}
#endif
}

#endif

