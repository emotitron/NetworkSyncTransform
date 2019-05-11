//Copyright 2018, Davin Carten, All rights reserved

#if PUN_2_OR_NEWER || MIRROR || !UNITY_2019_1_OR_NEWER

using emotitron.Utilities.GUIUtilities;

using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace emotitron.NST
{
	public enum NetworkLibrary { UNET, PUN, PUN2 }

	public enum NetworkModel { ServerClient = 1, MasterClient, PeerToPeer }
	public enum DefaultAuthority { ServerAuthority = 1, OwnerAuthority }
	public enum AuthorityModel { GlobalDefault, ServerAuthority, OwnerAuthority }

#if UNITY_EDITOR
	[HelpURL(HELP_URL)]
#endif
	public class NetLibrarySettings : SettingsScriptableObject<NetLibrarySettings>
	{
		
		//public NetworkLibrary networkLibrary = NetworkLibrary.UNET;
		public DefaultAuthority defaultAuthority = DefaultAuthority.ServerAuthority;
		[Tooltip("Automatically add NST Settings to scenes. NST Settings doesn't need to be in scenes. This is just for convenience.")]
		public bool AutoAddSettings = true;
		
		[HideInInspector]
		public bool dependenciesNeedToBeCheckedEverywhere = false;

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
		static void Bootstrap()
		{
			var single = Single;
		}

#if UNITY_EDITOR

		public override string SettingsName { get { return "Network Library Settings"; } }

		public override string AssetPath { get { return @"Assets/emotitron/NST Core/Resources/"; } }

		public const string HELP_URL = "https://docs.google.com/document/d/1SOm5aZHBed0xVlPk8oX2_PsQ50KTJFgcr8dDXmtdwh8/edit#bookmark=id.c0t8i8v9ghji";
		public override string HelpURL { get { return HELP_URL; } }

		public override bool DrawGui(Object target, bool asFoldout, bool includeScriptField, bool initializeAsOpen, bool asWindow = false)
		{
			
			bool isExpanded = base.DrawGui(target, asFoldout, includeScriptField, initializeAsOpen, asWindow);
			if (isExpanded)
			{
				EditorGUILayout.HelpBox("Currently library is '" + MasterNetAdapter.ADAPTER_NAME 
					+ "'\nLibrary is determined by Define Symbols in PlayerSettings.", MessageType.None);
			}
				
			//bool success = NetAdapterTools.ChangeLibraries();

			//// if the change failed, set the enum back to the current lib
			//if (!success)
			//	Single.networkLibrary = MasterNetAdapter.NetworkLibrary;

			return isExpanded;
		}
#endif

	}

#if UNITY_EDITOR

	[CustomEditor(typeof(NetLibrarySettings))]
	public class NetLibrarySettingsEditor : SettingsSOBaseEditor<NetLibrarySettings>
	{
		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();
			NetLibrarySettings.Single.DrawGui(target, false, true, true);
		}
	}
#endif

}

#endif
