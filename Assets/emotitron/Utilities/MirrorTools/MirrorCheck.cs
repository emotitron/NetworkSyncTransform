using UnityEngine;
using UnityEngine.SceneManagement;
using emotitron.Utilities;

#if UNITY_2018_3_OR_NEWER && UNITY_EDITOR
using UnityEditor.Build.Reporting;
#endif

#if MIRROR
using Mirror;
using System.Collections.Generic;
#else
using UnityEngine.Networking;
#endif

#if UNITY_EDITOR
using UnityEditor;
#endif

#pragma warning disable CS0618 // UNET obsolete

namespace emotitron.Utilities.Networking
{
	/// <summary>
	/// Component that will replace NetworkManager, NetworkManagerHUD and NetworkIdentity with mirror version at runtime.
	/// </summary>
#if MIRROR && UNITY_2018_3_OR_NEWER && UNITY_EDITOR && !UNITY_2019_1_OR_NEWER

	//[ExecuteInEditMode]
	public class MirrorCheck : MonoBehaviour/*, UnityEditor.Build.IPreprocessBuildWithReport*/
	{

		private void Awake()
		{
			/// Remove the UNET NetIdentity if it exists on this object.
			var ni = GetComponent<UnityEngine.Networking.NetworkIdentity>();
			if (ni)
			{
				AddMirrorNetManager();
				DestroyImmediate(ni);
				return;
			}

			/// If this wasn't a NI, the likely is the NM

			/// Remove the UNET NetManagerHUD if it exists on this object.
			var nmhud = GetComponent<UnityEngine.Networking.NetworkManagerHUD>();
			if (nmhud)
				DestroyImmediate(nmhud);

			/// Remove the UNET NetManager if it exists on this object.
			var nm = GetComponent<UnityEngine.Networking.NetworkManager>();

			if (nm)
				DestroyImmediate(nm);

		}


		[InitializeOnLoadMethod]
		public static void SubscribeToSceneChange()
		{
			//UnityEditor.SceneManagement.EditorSceneManager.sceneOpening += SceneOpeningCallback;
			UnityEditor.SceneManagement.EditorSceneManager.sceneOpened += SceneOpenedCallback;
		}

		static bool mirrorNetIdAlreadyAddedToResources;

		//static void SceneOpeningCallback(string path, UnityEditor.SceneManagement.OpenSceneMode mode)
		static void SceneOpenedCallback(Scene scene, UnityEditor.SceneManagement.OpenSceneMode mode)
		{
			// Only even attempt mirror check if we are looking at a sample scene.
			if (!scene.name.Contains("Example") && !scene.name.Contains("Sample"))
				return;

			//if (_mode == UnityEditor.SceneManagement.OpenSceneMode.)
			// get root objects in scene
			List<GameObject> rootObjects = new List<GameObject>();
			//Scene scene = SceneManager.GetActiveScene();
			scene.GetRootGameObjects(rootObjects);

			/// Need to make sure all NetIds have been converted before converting NM
			if (!mirrorNetIdAlreadyAddedToResources)
			{
				Debug.Log("Mirror NetId being added to all resources that have UNET NetId");
				AddMirrorNetIdToResources();
				mirrorNetIdAlreadyAddedToResources = true;
			}

			// iterate root objects and do something
			for (int i = 0; i < rootObjects.Count; ++i)
			{
				GameObject go = rootObjects[i];
				if (go)
				{
					var mc = go.GetComponent<MirrorCheck>();
					{
						if (mc)
						{
							mc.AddMirrorNetManager();
						}
					}
				}
			}
		}

		public static void AddMirrorNetIdToResources()
		{
			// Option for stripping all prefabs in all resource folders of NI
			var objs = Resources.LoadAll<MirrorCheck>("");

			// MirrorCheck on objects in resource folders should be prefabs. Replace NI on all of those.
			for (int i = 0; i < objs.Length; i++)
			{
				AddMirrorNetowrkIdentity(objs[i].gameObject);
			}

			AssetDatabase.SaveAssets();
		}

		public static void RemoveUNetNetIdFromResources()
		{
			// Option for stripping all prefabs in all resource folders of NI
			var objs = Resources.FindObjectsOfTypeAll<MirrorCheck>();

			// MirrorCheck on objects in resource folders should be prefabs. Replace NI on all of those.
			for (int i = 0; i < objs.Length; i++)
			{
				Debug.Log("<b>Replacing NI on </b>" + objs[i].gameObject.name);
				RemoveUNetNetowrkIdentity(objs[i].gameObject);
			}
			AssetDatabase.SaveAssets();
		}

		/// <summary>
		/// Add Mirror copies of Unet NM and NMHud. Will leave both and just delete the UNET NetworkManager at runtime.
		/// </summary>
		/// <returns></returns>
		public Mirror.NetworkManager AddMirrorNetManager()
		{
			var unetNM = GetComponent<UnityEngine.Networking.NetworkManager>();

			if (unetNM == null)
				return null;

			var mirrorNM = GetComponent<Mirror.NetworkManager>();

			if (mirrorNM)
				return mirrorNM;

			var unetHUD = GetComponent<UnityEngine.Networking.NetworkManagerHUD>();
			var mirrorHUD = GetComponent<Mirror.NetworkManagerHUD>();

			/// If this has a UNET NM on it, replace it with Mirror, and copy the playerprefab over
			if (unetNM)
			{
				var transport = GetComponent<Transport>();
				if (!transport)
					transport = gameObject.AddComponent<TelepathyTransport>();

				mirrorNM = gameObject.AddComponent<Mirror.NetworkManager>();
				NetworkManager.singleton = mirrorNM;

				if (mirrorHUD == null)
					mirrorHUD = gameObject.AddComponent<Mirror.NetworkManagerHUD>();

#if MIRROR_3_0_OR_NEWER
				Transport.activeTransport = transport;
#elif MIRROR_1726_OR_NEWER

				/// Initialize some stuff Mirror doesn't on its own (at least when this was written)
				Transport.activeTransport = transport;
				Transport.activeTransport.OnServerDisconnected = new UnityEventInt();
				Transport.activeTransport.OnServerConnected = new UnityEventInt();
				Transport.activeTransport.OnServerDataReceived = new UnityEventIntByteArray();
				Transport.activeTransport.OnServerError = new UnityEventIntException();
				Transport.activeTransport.OnClientConnected = new UnityEngine.Events.UnityEvent();
				Transport.activeTransport.OnClientDataReceived = new UnityEventByteArray();
				Transport.activeTransport.OnClientError = new UnityEventException();
				Transport.activeTransport.OnClientDisconnected = new UnityEngine.Events.UnityEvent();

#else
				/// Initialize some stuff Mirror doesn't on its own (Fix this Mirror team)
				NetworkManager.singleton.transport = transport;
				NetworkManager.singleton.transport.OnServerDisconnected = new UnityEventInt();
				NetworkManager.singleton.transport.OnServerConnected = new UnityEventInt();
				NetworkManager.singleton.transport.OnServerDataReceived = new UnityEventIntByteArray();
				NetworkManager.singleton.transport.OnServerError = new UnityEventIntException();
				NetworkManager.singleton.transport.OnClientConnected = new UnityEngine.Events.UnityEvent();
				NetworkManager.singleton.transport.OnClientDataReceived = new UnityEventByteArray();
				NetworkManager.singleton.transport.OnClientError = new UnityEventException();
				NetworkManager.singleton.transport.OnClientDisconnected = new UnityEngine.Events.UnityEvent();

#endif // End Mirror_1726

				///// Destroy the Unet HUD

				//if (unetHUD)
				//	Object.DestroyImmediate(unetHUD, true);

				/// Copy values from UNET NM to Mirror NM
				if (unetNM)
				{
					CopyPlayerPrefab(unetNM, mirrorNM);
					CopySpawnablePrefabs(unetNM, mirrorNM);
					//Object.DestroyImmediate(unetNM, true);

				}
			}

			//Object.DestroyImmediate(unetHUD, true);
			//Object.DestroyImmediate(unetNM, true);

			//if (!Application.isPlaying)
			//	UnityEditor.SceneManagement.EditorSceneManager.SaveScene(SceneManager.GetActiveScene());

			return mirrorNM;
		}

		private void DestroyUNetNetManager()
		{

		}

		private void StripNetworkManager()
		{

			var unetNM = GetComponent<UnityEngine.Networking.NetworkManager>();
			var unetHUD = GetComponent<UnityEngine.Networking.NetworkManagerHUD>();

			AddMirrorNetManager();
		}

		public static void AddMirrorNetowrkIdentity(GameObject prefab)
		{
			var unetNI = prefab.GetComponent<UnityEngine.Networking.NetworkIdentity>();

			if (!unetNI)
				return;

			var mirrorNI = prefab.GetComponent<Mirror.NetworkIdentity>();

			if (mirrorNI)
				return;

			if (PrefabUtility.IsPartOfPrefabAsset(prefab))
			{
				var path = AssetDatabase.GetAssetPath(prefab);
				var prefabRoot = PrefabUtility.LoadPrefabContents(path);

				/// Check if this prefab already has been converted and it just hasnt propogated.
				mirrorNI = prefabRoot.GetComponent<Mirror.NetworkIdentity>();
				if (mirrorNI)
					return;

				try
				{
					mirrorNI = prefabRoot.AddComponent<Mirror.NetworkIdentity>();
					mirrorNI.localPlayerAuthority = unetNI.localPlayerAuthority;
					mirrorNI.serverOnly = unetNI.serverOnly;
					//PrefabUtility.SaveAsPrefabAssetAndConnect(prefabRoot, path, InteractionMode.UserAction);
					PrefabUtility.SaveAsPrefabAsset(prefabRoot, path);
					//EditorUtility.SetDirty(prefab.gameObject);
					//AssetDatabase.SaveAssets();
				}
				finally
				{
					PrefabUtility.UnloadPrefabContents(prefabRoot);
				}
			}

			//	EditorUtility.SetDirty(prefab.gameObject);
			//	AssetDatabase.SaveAssets();

		}

		public static void RemoveUNetNetowrkIdentity(GameObject prefab)
		{
			var unetNI = prefab.GetComponent<UnityEngine.Networking.NetworkIdentity>();

			if (unetNI)
				DestroyImmediate(unetNI, true);

			EditorUtility.SetDirty(prefab);
		}

		public static void CopyPlayerPrefab(UnityEngine.Networking.NetworkManager src, Mirror.NetworkManager targ)
		{
			/// Make sure the player object is using mirror components
			if (src.playerPrefab)
			{
				AddMirrorNetowrkIdentity(src.playerPrefab);

				targ.autoCreatePlayer = src.autoCreatePlayer;
				targ.playerPrefab = src.playerPrefab;
				var ppNI = targ.playerPrefab.GetComponent<NetworkIdentity>();
				if (ppNI)
					ppNI.localPlayerAuthority = true;
			}
		}

		public static void CopySpawnablePrefabs(UnityEngine.Networking.NetworkManager src, Mirror.NetworkManager targ)
		{
			foreach (var obj in src.spawnPrefabs)
			{
				AddMirrorNetowrkIdentity(obj);

				if (!targ.spawnPrefabs.Contains(obj))
					targ.spawnPrefabs.Add(obj);
			}
		}
	}

	/// Build handling for mirror... removes UNET parts
#elif MIRROR && !UNITY_2019_1_OR_NEWER

	public class MirrorCheck : MonoBehaviour 
	{
		private void Awake()
		{
			/// Remove the UNET NetIdentity if it exists on this object.
			var ni = GetComponent<UnityEngine.Networking.NetworkIdentity>();

			if (ni)
			{
				DestroyImmediate(ni);
				return;
			}

			/// If this wasn't a NI, the likely is the NM

			/// Remove the UNET NetManagerHUD if it exists on this object.
			var nmhud = GetComponent<UnityEngine.Networking.NetworkManagerHUD>();
			if (nmhud)
				DestroyImmediate(nmhud);

			/// Remove the UNET NetManager if it exists on this object.
			var nm = GetComponent<UnityEngine.Networking.NetworkManager>();

			if (nm)
				DestroyImmediate(nm);

		}
	}
#else

	public class MirrorCheck : MonoBehaviour { }

#endif

#if UNITY_EDITOR

	[CustomEditor(typeof(MirrorCheck))]
	[CanEditMultipleObjects]
	public class MirrorCheckEditor : Editor
	{
		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();
			EditorGUILayout.HelpBox("If 'MIRROR' exists as a define, this component will replace UNET components with Mirror versions at runtime.",
				MessageType.None);
		}
	}

#endif

}

#pragma warning restore CS0618 // UNET obsolete


