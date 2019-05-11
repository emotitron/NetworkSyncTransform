//Copyright 2018, Davin Carten, All rights reserved

#if PUN_2_OR_NEWER || MIRROR || !UNITY_2019_1_OR_NEWER

using UnityEngine;
using emotitron.Utilities;
using emotitron.Utilities.GUIUtilities;
using System.Collections.Generic;
using System.Linq;
using emotitron.Debugging;

#if MIRROR
using Mirror;
#elif !UNITY_2019_1_OR_NEWER
using UnityEngine.Networking;
#endif

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

#pragma warning disable CS0618 // UNET obsolete

namespace emotitron.NST
{
	/// <summary>
	/// Utilities for automatically adding the required components to scene and networked objects.
	/// </summary>
	public static class NetAdapterTools
	{
		/// <summary>
		/// Add NSTRewindEngine to the NST if Rewind Addon is present, and authority model calls for rewind.
		/// </summary>
		/// <param name="nst"></param>
		public static void AddRewindEngine(this NetworkSyncTransform nst)
		{
			// Try to add/remove the rewind engine by name (to avoid errors if they aren't installed)
			System.Type t = System.Type.GetType("emotitron.NST.NSTRewindEngine");

			if (t != null)
			{
				if (NetLibrarySettings.Single.defaultAuthority == DefaultAuthority.ServerAuthority)
				{
					if (!nst.GetComponent(t))
					{
						nst.gameObject.AddComponent(t);
#if UNITY_EDITOR
						if (!Application.isPlaying)
							EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
#endif
					}
				}
			}
		}

		//public static void EnsureNSTMasterExistsInScene()
		//{
		//	if (NSTMaster.Single == null)
		//		NSTMaster.EnsureExistsInScene(NSTMaster.DEFAULT_GO_NAME);
		//}

#if UNITY_EDITOR

		/// <summary>
		/// Ensures NSTSettings as well as (NSTMaster/MasterAdapter/NetworkIdentity) exist in the scene.
		/// </summary>
		/// <returns></returns>
		public static void EnsureSceneNetLibDependencies(bool immediate = true)
		{
			if (Application.isPlaying)
				return;

			// If a post-recompile rebuild of dependencies is pending... do it now.
			TryToAddDependenciesEverywhere();

#if MIRROR || !UNITY_2019_1_OR_NEWER

			if (MasterNetAdapter.NetLib == NetworkLibrary.UNET)
			{
				GetNetworkManager(true);
				CopyPlayerPrefab();
			}
#endif

			//if (NetLibrarySettings.Single.AutoAddNSTMaster)
			//	NSTMaster.EnsureExistsInScene(NSTMaster.DEFAULT_GO_NAME);
			if (NetLibrarySettings.Single.AutoAddSettings)
				NSTSettings.EnsureExistsInScene(NSTSettings.DEFAULT_GO_NAME);
		}

		/// <summary>
		/// Ensure all required dependencies are added for this NST to work. Can be called often in edit mode, and should be. 
		/// </summary>
		/// <param name="nst"></param>
		/// <param name="silence"></param>
		public static void EnsureAllNSTDependencies(this NetworkSyncTransform nst, SerializedObject serializedObject, bool silence = false)
		{
			EnsureSceneNetLibDependencies();

			if (Application.isPlaying)
				return;

			// If user tried to put NST where it shouldn't be... remove it and all of the required components it added.
			if (nst.transform.parent != null)
			{
				XDebug.LogError("NetworkSyncTransform must be on the root of an prefab object.");
				nst.nstElementsEngine = nst.transform.GetComponent<NSTElementsEngine>();

				NSTNetAdapter.RemoveAdapter(nst);

				Object.DestroyImmediate(nst);

				if (nst.nstElementsEngine != null)
				{
					Object.DestroyImmediate(nst.nstElementsEngine);
					EditorUtility.SetDirty(nst.gameObject);
				}
				return;
			}

			nst.nstElementsEngine = NSTElementsEngine.EnsureExistsOnRoot(nst.transform, false);

			nst.na = EditorUtils.EnsureRootComponentExists<NSTNetAdapter>(nst.gameObject, false);

			//AddRewindEngine(nst);

			//// Add this NST to the prefab spawn list (and as player prefab if none exists yet) as an idiot prevention
			NSTNetAdapter.AddAsRegisteredPrefab(nst.gameObject, true, silence);
			return;
		}

		///// <summary>
		///// Check if the Net Library selected in mastersettings doesn't match the library of the adapters. Whill change to the library
		///// in MasterSettings if not.
		///// </summary>
		///// <param name="newLib"></param>
		//public static bool ChangeLibraries()
		//{
		//	return ChangeLibraries(NetLibrarySettings.Single.networkLibrary);
		//}

		/// <summary>
		/// Initiate the Library change process.
		/// </summary>
		/// <param name="newLib"></param>
		public static bool ChangeLibraries(NetworkLibrary newLib)
		{
			// Don't do anything if the adapters already are correct
			if (newLib == MasterNetAdapter.NetworkLibrary && newLib == NSTNetAdapter.NetLibrary)
				return true;

			if (newLib == NetworkLibrary.PUN && !PUN_Exists)
			{
				Debug.LogError("Photon PUN does not appear to be installed (Cannot find the PhotonNetwork assembly). Be sure it is installed from the asset store for this project.");
				return false;
			}

			if (newLib == NetworkLibrary.PUN2 && !PUN2_Exists)
			{
				Debug.LogError("Photon PUN2 does not appear to be installed (Cannot find the PhotonNetwork assembly). Be sure it is installed from the asset store for this project.");
				return false;
			}

			if (!EditorUtility.DisplayDialog("Change Network Library To " + System.Enum.GetName(typeof(NetworkLibrary), newLib) + "?",
				"Changing libraries is a very messy brute force operation (you may see compile errors and may need to restart Unity). " +
				"Did you really want to do this, or are you just poking at things to see what they do?", "Change Library", "Cancel"))
			{
				return false;
			}

			Debug.Log("Removing current adapters from game objects for Network Library change ...");
			PurgeLibraryReferences();

			// Close and reopen the current scene to remove the bugginess of orphaned scripts.
			var curscene = EditorSceneManager.GetActiveScene();
			var curscenepath = curscene.path;

			if (EditorUtility.DisplayDialog("Save Scene Before Reload?",
				"Scene must be reloaded to complete the purging of old library adapters. Would you like to save this scene?", "Save Scene", "Don't Save"))
				EditorSceneManager.SaveScene(curscene);

			// force a scene close to eliminate weirdness
			EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

			if (MasterNetAdapter.NetworkLibrary != newLib)
				CopyUncompiledAdapters(newLib);

			EditorUtility.DisplayDialog("Touch Nothing!",
				"Wait for the compiling animation in the bottom right of Unity to stop before doing anything. Touching NST related assets will result in broken scripts and errors.", "I Won't Touch, Promise.");


			// Flag the need for a deep global find of NSTs and NSTMasters in need of adapters
			XDebug.LogWarning("Add dependencies pending. Clicking on any NST related object in a scene " +
				"will trigger the final steps of the transition to " + newLib + ". You may need to select the " +
				"Player Prefab in the scene or asset folder in order to make it the default player object.", true, true);

			NetLibrarySettings.Single.dependenciesNeedToBeCheckedEverywhere = true;

			AssetDatabase.Refresh();
			EditorUtility.SetDirty(NetLibrarySettings.single);
			AssetDatabase.SaveAssets();

			return true;
		}


		// This should only be run very rarely. Forced to false after a library change.
		//public static bool dependenciesNeedToBeCheckedEverywhere;

		/// <summary>
		/// Deep find and add of adapters to all NSTs objects in assets. Deferred actions that need to happen after a compile following library change.
		/// </summary>
		public static void TryToAddDependenciesEverywhere()
		{
			if (!NetLibrarySettings.Single.dependenciesNeedToBeCheckedEverywhere)
				return;

			Debug.LogWarning("Added NST Entities in all Assets in entire project. Sorry if this took a while, but changing network libraries is a very brute force operation.");
			MasterNetAdapter.AddNstEntityComponentsEverywhere();

			// Now that prefabs in assets have been altered, make sure any scene objects revert to those prefabs
			RevertPrefabsInSceneWithComponentType<NetworkSyncTransform>();

			NetLibrarySettings.Single.dependenciesNeedToBeCheckedEverywhere = false;

			AssetDatabase.Refresh();
			EditorUtility.SetDirty(NetLibrarySettings.single);
			AssetDatabase.SaveAssets();
		}

		//public static void EnsureNSTMasterConforms()
		//{
		//	GameObject nstMasterPrefab = Resources.Load("NST Master", typeof(GameObject)) as GameObject;
		//	nstMasterPrefab.EnsureRootComponentExists<NSTMaster>();
		//	nstMasterPrefab.EnsureRootComponentExists<MasterNetAdapter>();

		//	if (MasterNetAdapter.NetLib == NetworkLibrary.UNET)
		//		nstMasterPrefab.EnsureRootComponentExists<NetworkIdentity>();
		//	else
		//	{
		//		NetworkIdentity ni = nstMasterPrefab.GetComponent<NetworkIdentity>();
		//		if (ni)
		//			Object.DestroyImmediate(ni);
		//	}
		//}

		//public static T EnsureContainsComponent<T>(this GameObject go) where T : Component
		//{
		//	T comp = go.GetComponent<T>();
		//	if (!comp)
		//		comp = go.AddComponent<T>();
		//	return comp;
		//}

		///// <summary>
		///// Return true if that library successfully was activated, or was already activated. False indicates a fail, to notify the enum
		///// selector to not make the change and stay in the current mode.
		///// </summary>
		///// <param name="netlib"></param>
		///// <returns></returns>
		//public static bool OverwriteAdapters(NetworkLibrary newLib)
		//{
		//	// Test to see if this library is already what is active
		//	if (MasterNetAdapter.NetworkLibrary != newLib)

		//	// Don't try to change to PUN if the PUN classes are missing (not installed)
		//	if (newLib == NetworkLibrary.PUN)
		//		if (!PUN_Exists)
		//		{
		//			XDebug.LogError("Photon PUN does not appear to exist in this project. You will need to download Photon PUN from the Unity Asset Store and import it into this project in order to have this option.", true, true);
		//			return false;
		//		}

		//	return CopyUncompiledAdapters(newLib);
		//}

		/// <summary>
		/// Test if PUN library appears to exist in this project.
		/// </summary>
		public static bool PUN_Exists
		{
			get
			{
				System.Type type = System.AppDomain.CurrentDomain.GetAssemblies()
					.SelectMany(x => x.GetTypes())
					.FirstOrDefault(x => x.Name == "PhotonNetwork");

				if (type == null)
					return false;

				if (type.Namespace == "Photon.Pun")
				{
					Debug.LogError("It appears that PUN2 is installed, rather than PUN.");
					return false;
				}
				else
				{
					return true;
				}
			}
		}

		/// <summary>
		/// Test if the PUN2 library appears to exist in this project.
		/// </summary>
		public static bool PUN2_Exists
		{
			get
			{
				System.Type type = System.AppDomain.CurrentDomain.GetAssemblies()
					.SelectMany(x => x.GetTypes())
					.FirstOrDefault(x => x.Name == "PhotonNetwork");

				if (type == null)
					return false;

				if (type.Namespace == "Photon.Pun")
				{
					return true;
				}
				else
				{
					Debug.LogError("It appears that PUN is installed, rather than PUN2.");
					return false;
				}
			}
		}

		/// <summary>
		/// Get the path to the named asset (just the first found, there should be no multiples). Will return empty if not found.
		/// </summary>
		/// <param name="a"></param>
		/// <returns></returns>
		private static string GetPathFromAssetName(string a)
		{
			var id = AssetDatabase.FindAssets(a);
			return (id.Length == 0) ? "" : AssetDatabase.GUIDToAssetPath(id[0]);
		}

		/// <summary>
		/// Overwrites the Adapter.CS files with the selected library specific version.
		/// </summary>
		/// <param name="newLib"></param>
		/// <returns>Returns true if successful.</returns>
		private static bool CopyUncompiledAdapters(NetworkLibrary newLib)// string libSuffix)
		{
			bool success = false;

			string libSuffix = (newLib == NetworkLibrary.UNET) ? "UNET" : (newLib == NetworkLibrary.PUN2) ? "PUN2" : "PUN";

			var _MA = GetPathFromAssetName("MasterNetAdapter" + libSuffix);
			var _NA = GetPathFromAssetName("NSTNetAdapter" + libSuffix);
			var MA = GetPathFromAssetName("MasterNetAdapter");
			var NA = GetPathFromAssetName("NSTNetAdapter");

			// fail if any of these files were not found.
			if (_MA == "" || _NA == "" || MA == "" || NA == "")
				return false;

			XDebug.Log("Switching to " + libSuffix + " adapters... recompiling should happen automatically.", true, true);

			if (MasterNetAdapter.NetworkLibrary != newLib)
				success |= AssetDatabase.CopyAsset(_MA, MA);

			if (NSTNetAdapter.NetLibrary != newLib)
				success |= AssetDatabase.CopyAsset(_NA, NA);

			return success;
		}

		/// <summary>
		/// Finds all instances of the network adapters in loaded scenes and the assetdatabse. Used prior to a network library change in order to not
		/// create broken scripts where the old adapters were.
		/// </summary>
		public static void PurgeLibraryReferences()
		{
			PurgeTypeFromEverywhere<NSTNetAdapter>();
			PurgeTypeFromEverywhere<MasterNetAdapter>();
			MasterNetAdapter.PurgeLibSpecificComponents();
		}

		private static void RevertPrefabsInSceneWithComponentType<T>() where T : Component
		{
			T[] found = Object.FindObjectsOfType<T>();

			for (int i = 0; i < found.Length; i++)
			{
				PrefabUtility.RevertPrefabInstance(found[i].gameObject);
			}
		}

		/// <summary>
		/// Find all occurances of SearchT in the assetdatabase, and add Component AddT to those gameobjects. Used for adding Adapters to all instances of NST or
		/// future NS components.
		/// </summary>
		/// <typeparam name="SearchT"></typeparam>
		/// <typeparam name="AddT"></typeparam>
		public static void AddComponentWhereverOtherComponentIsFound<SearchT, AddT>()
			where SearchT : Component where AddT : Component
		{
			string[] guids = AssetDatabase.FindAssets(string.Format("t:GameObject", typeof(SearchT)));
			for (int i = 0; i < guids.Length; i++)
			{
				string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
				GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
				if (asset.GetComponent<SearchT>())
				{
					if (!asset.GetComponent<AddT>())
						asset.AddComponent<AddT>();
				}
				Resources.UnloadUnusedAssets();
			}
		}

		public static void AddComponentsWhereverOtherComponentIsFound<SearchT, AddT, Add2T>()
			where SearchT : Component where AddT : Component where Add2T : Component
		{
			string[] guids = AssetDatabase.FindAssets(string.Format("t:GameObject", typeof(SearchT)));
			for (int i = 0; i < guids.Length; i++)
			{
				string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
				GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
				if (asset.GetComponent<SearchT>())
				{
					if (!asset.GetComponent<AddT>())
						asset.AddComponentToPrefab<AddT>();

					if (!asset.GetComponent<Add2T>())
						asset.AddComponentToPrefab<Add2T>();
				}
				Resources.UnloadUnusedAssets();
			}
		}

		/// <summary>
		/// Destroy Component T in all of the assetsDatabase, and then any instances in the current scene and loaded memory. This is root components only,
		/// and will not find components on scene objects in unloaded scenes.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		public static void PurgeTypeFromEverywhere<T>(bool removeGO = false) where T : Component
		{
			Debug.Log("<b>Purging all " + typeof(T).Name + " components from scene </b>");
			RemoveComponentTypeFromAllAssets<T>(removeGO);
			RemoveComponentTypeFromScene<T>(removeGO);

		}

		/// <summary>
		/// Destroy all instances of Component T in scene/memory.
		/// </summary>
		public static void RemoveComponentTypeFromScene<T>(bool removeGO = false) where T : Component
		{
			T[] found = Object.FindObjectsOfType<T>();

			for (int i = 0; i < found.Length; i++)
			{
				if (removeGO)
					Object.DestroyImmediate(found[i].gameObject);
				else
					Object.DestroyImmediate(found[i]);
			}
		}

		/// <summary>
		/// Find all instances of component T in the entire assetdatabase, and remove it from those objects. This only works for root components.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		private static void RemoveComponentTypeFromAllAssets<T>(bool removeGO = false) where T : Component
		{
			string[] guids = AssetDatabase.FindAssets(string.Format("t:GameObject", typeof(T)));
			//Debug.Log("Found " + guids.Length + " " + typeof(T).Name + " in the asset database.");
			for (int i = 0; i < guids.Length; i++)
			{
				string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
				GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
				SerializedObject so = new SerializedObject(asset);

				if (asset != null)
				{
					T comp = (asset as GameObject).GetComponent<T>();
					if (comp)
					{
						Debug.Log("Destroying component " + typeof(T).Name + " on go " + asset);

						if (removeGO)
							Object.DestroyImmediate(comp.gameObject, true);
						else
							Object.DestroyImmediate(comp, true);
					}
				}
				so.Update();
			}
			Resources.UnloadUnusedAssets();
		}

		//#if MIRROR || !UNITY_2019_1_OR_NEWER

		public static void RemovedUnusedNetworkIdentity(GameObject go)
		{
			// Double check to make sure NI doesn't exist if UNET isn't being used. It disables the object and will break other libs.
			if (MasterNetAdapter.NetLib != NetworkLibrary.UNET)
			{
#if MIRROR || !UNITY_2019_1_OR_NEWER
				NetworkIdentity ni = go.GetComponent<NetworkIdentity>();

				if (ni)
				{
					try { Object.DestroyImmediate(ni); }
					catch { try { Object.Destroy(ni); } catch { } }
				}
#endif
			}

		}

#if MIRROR || !UNITY_2019_1_OR_NEWER

		public static NetworkManager GetNetworkManager(bool createMissing = false)
		{

			if (MasterNetAdapter.NetLib != NetworkLibrary.UNET)
			{
				return null;
			}

			if (NetworkManager.singleton == null)
			{

#if MIRROR
				List<NetworkManager> mirrorNM = FindObjects.FindObjectsOfTypeAllInScene<Mirror.NetworkManager>();
				if (mirrorNM.Count > 0)
				{
					return mirrorNM[0];
				}
#endif

				List<NetworkManager> found = FindObjects.FindObjectsOfTypeAllInScene<NetworkManager>();

				if (found.Count > 0)
					NetworkManager.singleton = found[0];

				else if (createMissing)
				{
					Debug.Log("<b>Adding Network Manager</b>");
					XDebug.LogWarning(!XDebug.logWarnings ? null : ("No NetworkManager in scene. Adding one now."));

					GameObject nmGo = GameObject.Find("Network Manager");

					if (nmGo == null)
						nmGo = new GameObject("Network Manager");

					NetworkManager.singleton = nmGo.AddComponent<NetworkManager>();

					// If we are creating a missing NM, also create a HUD in case user wants that.
					NetworkManagerHUD hud = nmGo.GetComponent<NetworkManagerHUD>();

					if (!hud)
						nmGo.AddComponent<NetworkManagerHUD>();

					// Copy the playerprefab over from pun if it exists.

					//#if MIRROR
					//					var unetHUD = nmGo.GetComponent<UnityEngine.Networking.NetworkManagerHUD>();
					//					if (unetHUD)
					//						Object.DestroyImmediate(unetHUD);

					//					var unetNM = nmGo.GetComponent<UnityEngine.Networking.NetworkManager>();
					//					if (unetNM)
					//						Object.DestroyImmediate(unetNM);

					//#endif
				}
			}

			return NetworkManager.singleton;
		}
#endif

		public static void CopyPlayerPrefab()
		{
			CopyPlayerPrefabFromPUNtoOthers();
			CopyPlayerPrefabFromUNETtoOthers();
		}

		// TODO this is redudant with NSTNetAdapter code for UNET
		public static void CopyPlayerPrefabFromPUNtoOthers()
		{
			PUNSampleLauncher punl = PUNSampleLauncher.Single;
			if (!punl || punl.playerPrefab == null)
				return;

			NSTNetAdapter.AddAsRegisteredPrefab(punl.playerPrefab, true);
		}

#if MIRROR || !UNITY_2019_1_OR_NEWER

		public static void EnsureNMPlayerPrefabIsLocalAuthority(NetworkManager nm)
		{
			if (nm && nm.playerPrefab)
			{
				NetworkIdentity ni = nm.playerPrefab.GetComponent<NetworkIdentity>();
				if (ni && !ni.localPlayerAuthority)
				{
					ni.localPlayerAuthority = true;
					Debug.Log("Setting 'NetworkIdentity.localPlayerAuthority = true' on prefab '<b>" + nm.playerPrefab.name + "</b>' for you, now that it is registerd as the Player Prefab with the NetworkManager.");
					EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
				}
			}
		}

#endif

		public static void CopyPlayerPrefabFromUNETtoOthers()
		{
#if MIRROR || !UNITY_2019_1_OR_NEWER

			NetworkManager nm = GetNetworkManager();
			if (!nm || nm.playerPrefab == null)
				return;

			PUNSampleLauncher punl = PUNSampleLauncher.Single;

			if (punl && !punl.playerPrefab)
			{
				Debug.Log("Copying Player Prefab : <b>'" + nm.playerPrefab.name + "'</b> from NetworkManager to " + punl.GetType().Name + " for you.");
				punl.playerPrefab = nm.playerPrefab;
			}
#endif
		}


		public static void RemoveUnusedNetworkManager()
		{
#if MIRROR || !UNITY_2019_1_OR_NEWER

			if (MasterNetAdapter.NetLib != NetworkLibrary.UNET)
			{
				RemoveComponentTypeFromScene<NetworkManager>(true);
			}
#endif
		}

		public static void EnsureNMPlayerPrefabIsLocalAuthority()
		{
#if MIRROR || !UNITY_2019_1_OR_NEWER

			EnsureNMPlayerPrefabIsLocalAuthority(GetNetworkManager());
#endif
		}


		//#endif // END UNET/MIRROR Exist check



		//public static NetworkManager EnsureNetworkManagerExists()
		//{
		//	if (NetworkManager.singleton == null)
		//	{
		//		List<NetworkManager> found = FindObjects.FindObjectsOfTypeAllInScene<NetworkManager>();
		//		if (found.Count > 0)
		//		{
		//			NetworkManager.singleton = found[0];
		//		}
		//		else
		//		{
		//			XDebug.LogWarning(!XDebug.logWarnings ? null : ("No NetworkManager in scene. Adding one now."));

		//			GameObject nmGo = GameObject.Find("Network Manager");

		//			if (nmGo == null)
		//				nmGo = new GameObject("Network Manager");

		//			NetworkManager.singleton = nmGo.AddComponent<NetworkManager>();

		//			// Copy over the player prefab from our PUN launcher if there is one.
		//			PUNSampleLauncher punl = PUNSampleLauncher.Single;
		//			if (punl && punl.playerPrefab)
		//			{
		//				XDebug.Log("Copying Player Prefab : <b>'" +punl.playerPrefab.name + "'</b> from " + typeof(PUNSampleLauncher).Name + " to NetworkManager for you.", true, true);
		//				NetworkManager.singleton.playerPrefab = punl.playerPrefab;
		//			}
		//		}
		//	}

		//	// Add a HUD if that is also missing
		//	if (NetworkManager.singleton.gameObject.GetComponent<NetworkManagerHUD>() == null)
		//		NetworkManager.singleton.gameObject.AddComponent<NetworkManagerHUD>();

		//	return NetworkManager.singleton;
		//}


		///// <summary>
		///// Checks to see if there is a NetworkManager, and if so, checks to make sure the assigned playerprefab has its ni set to localplayerauthority.
		///// </summary>
		//public static void EnsurePlayerPrefabIsSetLocalAuthority()
		//{
		//	if (MasterNetAdapter.NetLib == NetworkLibrary.UNET)
		//	{
		//		NetworkManager nm = GetNetworkManager();
		//		if (nm && nm.playerPrefab)
		//		{
		//			NetworkIdentity ni = nm.playerPrefab.GetComponent<NetworkIdentity>();
		//			if (ni)
		//				ni.localPlayerAuthority = true;
		//		}
		//	}
		//}
#endif
	}
}

#pragma warning restore CS0618 // UNET obsolete


#endif // End NETLIB exist check