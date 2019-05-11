
#if PUN_2_OR_NEWER || MIRROR || !UNITY_2019_1_OR_NEWER

using UnityEngine;
using emotitron.Debugging;

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
#endif

#pragma warning disable CS0618 // UNET obsolete

namespace emotitron.NST
{
	// Future interface for making a standard implementation across Libraries...
	public interface IPlayerPrefabDefinition
	{
		GameObject PlayerPrefab { get; set; }
	}

	/// <summary>
	/// This is a very basic PUN implementation I have supplied to make it easy to quicky get started.
	/// It doesn't make use of a lobby so it only uses one scene, which eliminates the need to add any
	/// scenes to the build. Your actual game using PUN likely will want to have multiple scenes and you
	/// will want to replace all of this code with your own.
	/// </summary>
	public class PUNSampleLauncher : Singleton<PUNSampleLauncher>, IOnConnect, IOnJoinRoom, IOnJoinRoomFailed, IPlayerPrefabDefinition
	{
		[Tooltip("The prefab to use for representing the player")]
		public GameObject playerPrefab;
		public GameObject PlayerPrefab { get { return playerPrefab; } set { playerPrefab = value; } }
		public bool autoSpawnPlayer = true;
		public KeyCode spawnPlayerKey = KeyCode.P;
		public KeyCode unspawnPlayerKey = KeyCode.O;

		public static GameObject localPlayer;
		/// <summary>
		/// This client's version number. Users are separated from each other by gameversion (which allows you to make breaking changes).
		/// </summary>
		string _gameVersion = "1";

#if UNITY_EDITOR
		private void Reset()
		{
			// On creation, see if there is a UNET network manager and copy the playerprefab from that.
			playerPrefab = null;
			NetAdapterTools.CopyPlayerPrefab();
		}

		private void OnValidate()
		{
			if (Application.isPlaying)
				return;

			// Make sure that the prefab link is a prefab and not a scene object.
			if (playerPrefab)
			{
				var parPrefab = PrefabUtility.GetPrefabParent(playerPrefab) as GameObject;
				if (parPrefab && parPrefab != playerPrefab)
				{
					playerPrefab = parPrefab;
				}
			}
		}
#endif

		/// <summary>
		/// MonoBehaviour method called on GameObject by Unity during early initialization phase.
		/// </summary>
		protected override void Awake()
		{
			// This test allows this component to be used in UNET scenes without breaking anything.
			if (MasterNetAdapter.NetLib != NetworkLibrary.PUN && NSTNetAdapter.NetLibrary != NetworkLibrary.PUN2)
			{
				Debug.LogWarning("Not using Photon PUN. Destroying " + typeof(PUNSampleLauncher).Name + " on GameObject " + name);
				Destroy(this);

				return;
			}

#if MIRROR || !UNITY_2019_1_OR_NEWER
#if UNITY_EDITOR || DEVELOPMENT_BUILD
			// Destroy any UNET stuff in the scene if we aren't running unet.
			if (MasterNetAdapter.NetLib != NetworkLibrary.UNET)
			{
				NetworkManagerHUD nmh = FindObjectOfType<NetworkManagerHUD>();
				if (nmh)
					Destroy(nmh);

				NetworkManager nm = FindObjectOfType<NetworkManager>();
				if (nm)
					Destroy(nm);
			}
#endif
#endif
			// we don't join the lobby. There is no need to join a lobby to get the list of rooms.
			//MasterNetAdapter.PUN_AutoJoinLobby = false;

			// this makes sure we can use PhotonNetwork.LoadLevel() on the master client and all clients in the same room sync their level automatically
			MasterNetAdapter.PUN_AutomaticallySyncScene = true;
		}

		/// <summary>
		/// MonoBehaviour method called on GameObject by Unity during initialization phase.
		/// </summary>
		void Start()
		{
			Connect();
		}

		/// <summary>
		/// We use interfaces to get messages from the network adapters, so we need to register this MB to make its interfaces
		/// known to the MasterNetAdapter.
		/// </summary>
		protected void OnEnable()
		{
			MasterNetAdapter.RegisterCallbackInterfaces(this);
		}

		protected void OnDisable()
		{
			MasterNetAdapter.UnregisterCallbackInterfaces(this);
		}

		public void OnConnect(ServerClient svrclnt)
		{
			MasterNetAdapter.PUN_JoinRandomRoom();
		}

		public void OnJoinRoom()
		{
			if (autoSpawnPlayer)
				SpawnLocalPlayer();
			else
				Debug.Log("<b>Auto-Create for player is disabled on component '" + this.GetType().Name + "'</b>. Press '" + spawnPlayerKey + "' to spawn a player. '" + unspawnPlayerKey + "' to unspawn.");
		}

		public void OnJoinRoomFailed()
		{
			XDebug.LogWarning("Launcher:OnPhotonRandomJoinFailed() was called by PUN. No random room available, so we create one.\nCalling: PhotonNetwork.CreateRoom(null, new RoomOptions() {maxPlayers = 4}, null);");
			MasterNetAdapter.PUN_CreateRoom(null, 8);
		}

		public void Update()
		{
			if (Input.GetKeyDown(spawnPlayerKey))
				SpawnLocalPlayer();

			else if (Input.GetKeyDown(unspawnPlayerKey))
				MasterNetAdapter.UnSpawn(localPlayer);
		}


		/// <summary>
		/// Start the connection process. 
		/// - If already connected, we attempt joining a random room
		/// - if not yet connected, Connect this application instance to Photon Cloud Network
		/// </summary>
		public void Connect()
		{
			// we check if we are connected or not, we join if we are , else we initiate the connection to the server.
			if (MasterNetAdapter.PUN_Connected)
			{
				MasterNetAdapter.PUN_JoinRandomRoom();
			}
			else
			{
				MasterNetAdapter.PUN_ConnectUsingSettings(_gameVersion);
			}
		}

		private void SpawnLocalPlayer()
		{
			// we're in a room. spawn a character for the local player. it gets synced by using PhotonNetwork.Instantiate
			if (MasterNetAdapter.NetLib == NetworkLibrary.PUN || NSTNetAdapter.NetLibrary == NetworkLibrary.PUN2)
			{
				if (playerPrefab)
				{
					Transform tr = NSTSamplePlayerSpawn.GetRandomSpawnPoint();
					Vector3 pos = (tr) ? tr.position : Vector3.zero;
					Quaternion rot = (tr) ? tr.rotation : Quaternion.identity;

					localPlayer = MasterNetAdapter.Spawn(playerPrefab, pos, rot, null);
				}
				else
					Debug.LogError("No PlayerPrefab defined in " + this.GetType().Name);

			}
		}


#if UNITY_EDITOR

		[MenuItem("Window/NST/Add PUN Bootstrap", false, 1)]

		public static void AddPUNLauncher()
		{
			if (Single)
				return;

			EnsureExistsInScene("NST PUN Launcher", true);
		}
#endif
	}

#if UNITY_EDITOR

	[CustomEditor(typeof(PUNSampleLauncher))]
	[CanEditMultipleObjects]
	public class PUNSampleLauncherEditor : NSTSampleHeader
	{
		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();
			EditorGUILayout.HelpBox("Sample PUN launcher code that creates a PUN room and spawns players.", MessageType.None);
		}
	}

#endif

}
#pragma warning restore CS0618 // UNET obsolete


#endif