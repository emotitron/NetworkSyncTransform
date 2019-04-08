using UnityEngine;
using UnityEngine.SceneManagement;
using emotitron.Utilities;

#if UNITY_2018_3_OR_NEWER && UNITY_EDITOR
using UnityEditor.Build.Reporting;
#endif

#if MIRROR
using Mirror;
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
#if MIRROR && UNITY_2018_3_OR_NEWER && UNITY_EDITOR

	public class MirrorCheck : MonoBehaviour, UnityEditor.Build.IPreprocessBuildWithReport
	{
		public int callbackOrder { get { return 0; } }

		public void OnPreprocessBuild(BuildReport report)
		{
			RunCheck();
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
		public static void RunCheck()
		{
			var scene = SceneManager.GetActiveScene();

			//var objs = Resources.FindObjectsOfTypeAll<MirrorCheck>();
			var objs = Object.FindObjectsOfType<MirrorCheck>();

			// Find objects in scene - this will generally just be the NetworkManager.
			for (int i = 0; i < objs.Length; i++)
			{
				objs[i].StripNetworkManager();
			}

			//// Option for stripping all prefabs in all resource folders of NI
			//objs = Resources.FindObjectsOfTypeAll<MirrorCheck>();

			//// MirrorCheck on objects in resource folders should be prefabs. Replace NI on all of those.
			//for (int i = 0; i < objs.Length; i++)
			//{
			//	GiveMirrorNetworkIdentity(objs[i].gameObject);
			//}
		}

		public static Mirror.NetworkManager ConvertNetworkManager(UnityEngine.Networking.NetworkManager unetNM, UnityEngine.Networking.NetworkManagerHUD unetHUD)
		{
			if (unetNM == null)
				return null;

			var go = unetNM.gameObject;
			Mirror.NetworkManager mirrorNM = null;

			/// If this has a UNET NM on it, replace it with Mirror, and copy the playerprefab over
			if (unetNM)
			{
				var transport = go.GetComponent<Transport>();
				if (!transport)
					transport = go.AddComponent<TelepathyTransport>();

				mirrorNM = go.GetComponent<Mirror.NetworkManager>();
				if (mirrorNM == null)
				{
					mirrorNM = go.AddComponent<Mirror.NetworkManager>();
					NetworkManager.singleton = mirrorNM;
				}

				var mirrorHUD = go.GetComponent<Mirror.NetworkManagerHUD>();
				if (mirrorHUD == null)
					mirrorHUD = go.AddComponent<Mirror.NetworkManagerHUD>();

#if MIRROR_1726_OR_NEWER

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

				/// Destroy the Unet HUD

				if (unetHUD)
					Object.DestroyImmediate(unetHUD, true);

				/// Copy values from UNET NM to Mirror NM
				if (unetNM)
				{
					CopyPlayerPrefab(unetNM, mirrorNM);
					CopySpawnablePrefabs(unetNM, mirrorNM);
					Object.DestroyImmediate(unetNM, true);

				}

			}

			Object.DestroyImmediate(unetHUD, true);
			Object.DestroyImmediate(unetNM, true);

			UnityEditor.SceneManagement.EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
			return mirrorNM;
		}

		private void StripNetworkManager()
		{

			var unetNM = GetComponent<UnityEngine.Networking.NetworkManager>();
			var unetHUD = GetComponent<UnityEngine.Networking.NetworkManagerHUD>();

			ConvertNetworkManager(unetNM, unetHUD);
		}

		public static void GiveMirrorNetworkIdentity(GameObject prefab)
		{
			var unetNI = prefab.GetComponent<UnityEngine.Networking.NetworkIdentity>();

			var mirrorNI = prefab.GetComponent<Mirror.NetworkIdentity>();

			if (!mirrorNI)
			{
				mirrorNI = prefab.AddComponentToPrefab<Mirror.NetworkIdentity>();
			}

			if (unetNI)
			{
				mirrorNI.localPlayerAuthority = unetNI.localPlayerAuthority;
				mirrorNI.serverOnly = unetNI.serverOnly;
				DestroyImmediate(unetNI, true);
			}
		}

		public static void CopyPlayerPrefab(UnityEngine.Networking.NetworkManager src, Mirror.NetworkManager targ)
		{
			/// Make sure the player object is using mirror components
			if (src.playerPrefab)
			{
				GiveMirrorNetworkIdentity(src.playerPrefab);

				targ.autoCreatePlayer = src.autoCreatePlayer;
				targ.playerPrefab = src.playerPrefab;
				var ppNI = targ.playerPrefab.GetComponent<NetworkIdentity>();
				ppNI.localPlayerAuthority = true;
			}
		}

		public static void CopySpawnablePrefabs(UnityEngine.Networking.NetworkManager src, Mirror.NetworkManager targ)
		{
			foreach (var obj in src.spawnPrefabs)
			{
				GiveMirrorNetworkIdentity(obj);

				if (!targ.spawnPrefabs.Contains(obj))
					targ.spawnPrefabs.Add(obj);
			}
		}
	}

#elif MIRROR

	public class MirrorCheck : MonoBehaviour 
	{
		private void Awake()
		{
			/// Remove the UNET NetIdentity if it exists on this object.
			var netidentity = GetComponent<UnityEngine.Networking.NetworkIdentity>();

			if (netidentity)
				DestroyImmediate(netidentity);
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


