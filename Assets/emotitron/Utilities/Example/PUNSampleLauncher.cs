using UnityEngine;
using System.Collections.Generic;
using emotitron.Utilities;

#if UNITY_2018_3_OR_NEWER && UNITY_EDITOR
using UnityEditor.Build.Reporting;
#endif

#if PUN_2_OR_NEWER
using Photon;
using Photon.Pun;
using Photon.Realtime;
#endif

#if MIRROR
using Mirror;
#else
using UnityEngine.Networking;

#endif

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
#endif

#pragma warning disable CS0618 // UNET obsolete

namespace emotitron.Utilities.Example
{

	/// <summary>
	/// This is a very basic PUN implementation I have supplied to make it easy to quicky get started.
	/// It doesn't make use of a lobby so it only uses one scene, which eliminates the need to add any
	/// scenes to the build. Your actual game using PUN likely will want to have multiple scenes and you
	/// will want to replace all of this code with your own.
	/// </summary>
	public class PUNSampleLauncher :

#if PUN_2_OR_NEWER
		MonoBehaviourPunCallbacks
#else
		MonoBehaviour
#endif


#if UNITY_2018_3_OR_NEWER && UNITY_EDITOR && PUN_2_OR_NEWER

		, UnityEditor.Build.IPreprocessBuildWithReport
	{
		public int callbackOrder { get { return 0; } }

		public void OnPreprocessBuild(BuildReport report)
		{
			if (playerPrefab)
			{
				PhotonView pv = playerPrefab.GetComponent<PhotonView>();
				if (pv == null)
					AddPhotonView(playerPrefab);
			}

			/// Option for stripping all prefabs in all resource folders of NI
			var objs = Resources.LoadAll<Networking.MirrorCheck>("");
			
			// MirrorCheck on objects in resource folders should be prefabs. Replace NI on all of those.
			for (int i = 0; i < objs.Length; i++)
				AddPhotonView(objs[i].gameObject);

		}

#elif UNITY_EDITOR && PUN_2_OR_NEWER

		, UnityEditor.Build.IPreprocessBuild
	{
		public int callbackOrder { get { return 0; } }

		public void OnPreprocessBuild(BuildTarget buildTarget, string str)
		{
			if (playerPrefab)
			{
				PhotonView pv = playerPrefab.GetComponent<PhotonView>();
				if (pv == null)
					AddPhotonView(playerPrefab);
			}

			/// Option for stripping all prefabs in all resource folders of NI
			var objs = Resources.FindObjectsOfTypeAll<Networking.MirrorCheck>();

			// MirrorCheck on objects in resource folders should be prefabs. Replace NI on all of those.
			for (int i = 0; i < objs.Length; i++)
				AddPhotonView(objs[i].gameObject);
		}
#else
	{
#endif



#if PUN_2_OR_NEWER
		public static void AddPhotonView(GameObject prefab)
		{
			//var unetNI = prefab.GetComponent<UnityEngine.Networking.NetworkIdentity>();

			var pv = prefab.GetComponent<PhotonView>();

			if (!pv)
				PrefabUtils.AddComponentToPrefab<PhotonView>(prefab);


			//if (unetNI)
			//	DestroyImmediate(unetNI, true);

			//return pv;
		}
#endif

		[Tooltip("The prefab to use for representing the player")]
		public GameObject playerPrefab;
		public bool autoSpawnPlayer = true;
		public KeyCode spawnPlayerKey = KeyCode.P;
		public KeyCode unspawnPlayerKey = KeyCode.O;

#if PUN_2_OR_NEWER

		public static GameObject localPlayer;
		
		private void Awake()
		{
			/// this makes sure we can use PhotonNetwork.LoadLevel() on the master client and all clients in the same room sync their level automatically
			PhotonNetwork.AutomaticallySyncScene = true;

			if (playerPrefab)
			{
				PhotonView pv = playerPrefab.GetComponent<PhotonView>();
				if (pv == null)
					AddPhotonView(playerPrefab);

			}
		}

		void Start()
		{
			Connect();
		}

		public override void OnConnectedToMaster()
		{
			JoinOrCreateRoom();
		}

		public override void OnJoinedLobby()
		{
			base.OnJoinedLobby();
		}

		public override void OnJoinedRoom()
		{
			Debug.Log("Joined room");
			if (autoSpawnPlayer)
				SpawnLocalPlayer();
			else
				Debug.Log("<b>Auto-Create for player is disabled on component '" + this.GetType().Name + "'</b>. Press '" + spawnPlayerKey + "' to spawn a player. '" + unspawnPlayerKey + "' to unspawn.");
		}

		public void Update()
		{
			if (Input.GetKeyDown(spawnPlayerKey))
				SpawnLocalPlayer();

			if (Input.GetKeyDown(unspawnPlayerKey))
				UnspawnLocalPlayer();
		}

		/// <summary>
		/// Start the connection process. 
		/// - If already connected, we attempt joining a random room
		/// - if not yet connected, Connect this application instance to Photon Cloud Network
		/// </summary>
		public void Connect()
		{
			// we check if we are connected or not, we join if we are , else we initiate the connection to the server.
			if (PhotonNetwork.IsConnected)
			{
				JoinOrCreateRoom();
			}
			else
			{
				PhotonNetwork.ConnectUsingSettings();
			}
		}

		private void JoinOrCreateRoom()
		{
			PhotonNetwork.JoinOrCreateRoom("TestRoom", new RoomOptions() { MaxPlayers = 8 }, null);
		}

		private void SpawnLocalPlayer()
		{
			// we're in a room. spawn a character for the local player. it gets synced by using PhotonNetwork.Instantiate
			if (playerPrefab/* && !localPlayer*/)
			{
				//Transform tr = spawnPoints.Count > 0 ? spawnPoints[Random.Range(0, spawnPoints.Count)] : null;
				Transform tr = emotitron.Utilities.Networking.GenericSpawnPoint.GetSpawnPointFromValue(PhotonNetwork.LocalPlayer.ActorNumber);
				Vector3 pos = (tr) ? tr.position : new Vector3(PhotonNetwork.LocalPlayer.ActorNumber, 0f, 0f);
				Quaternion rot = (tr) ? tr.rotation : Quaternion.identity;

				localPlayer = PhotonNetwork.Instantiate(playerPrefab.name, pos, rot, 0);
				localPlayer.transform.parent = null;
			}
			else
				Debug.LogError("No PlayerPrefab defined in " + this.GetType().Name);
		}

		private void UnspawnLocalPlayer()
		{
			if (localPlayer.GetComponent<PhotonView>().IsMine && PhotonNetwork.IsConnected)
				PhotonNetwork.Destroy(localPlayer);
		}


#endif
	}

#if UNITY_EDITOR

	[CustomEditor(typeof(PUNSampleLauncher))]
	[CanEditMultipleObjects]
	public class PUNSampleLauncherEditor : Editor
	{
		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();
			EditorGUILayout.HelpBox("Convenience handler for launching a very basic Photon PUN2 room. Self-Destroys at runtime if PUN2 is not present.", MessageType.None);

#if PUN_2_OR_NEWER
			EditorGUILayout.HelpBox("Sample PUN launcher code that creates a PUN room and spawns players.", MessageType.None);
#else
			EditorGUILayout.HelpBox("PUN2 not installed.", MessageType.Warning);
#endif

		}
	}

#endif

}
#pragma warning disable CS0618 // UNET obsolete
