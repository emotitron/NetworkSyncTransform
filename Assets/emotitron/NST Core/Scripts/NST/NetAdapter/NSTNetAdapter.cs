//Copyright 2018, Davin Carten, All rights reserved
#if PUN_2_OR_NEWER

/// ----------------------------------------   PUN 2    -----------------------------------------------------
/// ----------------------------------------   PUN 2    -----------------------------------------------------
/// ----------------------------------------   PUN 2    -----------------------------------------------------

#region PUN2

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Photon;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;

using emotitron.Compression;
using emotitron.Utilities.GUIUtilities;
using emotitron.Debugging;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace emotitron.NST
{
	public struct PlayerInitData
	{
		public uint nstId;
	}

	/// <summary>
	/// This class contains the abstracted methods for different networking libraries. 
	/// This adapter is for Photon PUN2.
	/// </summary>
	[DisallowMultipleComponent]
	[AddComponentMenu("")]
	[RequireComponent(typeof(PhotonView))]

	public class NSTNetAdapter : MonoBehaviourPunCallbacks  //, INstNetAdapter
	{
		public const string ADAPTER_NAME = "PUN2";
		public static NetworkLibrary NetLibrary { get { return NetworkLibrary.PUN2; } }

		private PhotonView pv;
		//private NetworkSyncTransform nst;
		NSTSettings nstSettings;

		// callback interfaces... collected on awake from all children on this gameobject, and can be subcribed to as well.
		[HideInInspector] public List<INetEvents> iNetEvents = new List<INetEvents>();
		//[HideInInspector] public List<IOnStartServer> iOnStartServer = new List<IOnStartServer>();
		//[HideInInspector] public List<IOnStartClient> iOnStartClient = new List<IOnStartClient>();
		[HideInInspector] public List<IOnConnect> iOnConnect = new List<IOnConnect>();
		[HideInInspector] public List<IOnStartLocalPlayer> iOnStartLocalPlayer = new List<IOnStartLocalPlayer>();
		[HideInInspector] public List<IOnNetworkDestroy> iOnNetworkDestroy = new List<IOnNetworkDestroy>();
		[HideInInspector] public List<IOnStartAuthority> iOnStartAuthority = new List<IOnStartAuthority>();
		[HideInInspector] public List<IOnStopAuthority> iOnStopAuthority = new List<IOnStopAuthority>();
		[HideInInspector] public List<IOnStart> iOnStart = new List<IOnStart>();

		public bool IsServer { get { return MasterNetAdapter.ServerIsActive; } }
		public bool IsLocalPlayer { get { return pv.IsMine; } } // isLocalPlayer; } }
		public bool IsMine { get { return pv.IsMine; } }

		public uint NetId { get { return (uint)pv.ViewID; } }
		//public int ClientId { get { return pv.ownerId; } }
		public int ClientId { get { return pv.Owner.ActorNumber; } }

		private uint _nstIdSyncvar;
		public uint NstIdSyncvar { get { return _nstIdSyncvar; } set { _nstIdSyncvar = value; } }

		// cached values
		public AuthorityModel authorityModel;

		///// <summary> Does this client have authority over all aspects of this networked object (rather than just movement). Determined by the authority model
		///// selected in MasterSettings.</summary>

		public bool IAmActingAuthority
		{
			get
			{

				if (authorityModel == AuthorityModel.ServerAuthority)
					if (PhotonNetwork.IsMasterClient)
						return true;

				if (authorityModel == AuthorityModel.OwnerAuthority)
					if (pv.IsMine)
						return true;

				return false;
			}
		}

		public void CollectCallbackInterfaces()
		{
			GetComponentsInChildren(true, iNetEvents);
		}

		void Awake()
		{
			pv = GetComponent<PhotonView>();
			//nst = GetComponent<NetworkSyncTransform>();

			//XDebug.LogError("You appear to have an 'NetworkSyncTransform' on instantiated object '" + name + "', but that object has NOT been network spawned. " +
			//	"Only use NST on objects you intend to spawn normally from the server using PhotonNetwork.Instantiate(). " +
			//	"(Projectiles for example probably don't need to be networked objects).", (nst.destroyUnspawned && pv.viewID == 0), true);

			authorityModel = (AuthorityModel)NetLibrarySettings.Single.defaultAuthority;

			CollectCallbackInterfaces();

		}

		private void Start()
		{
			/// TODO: This substitute for OnStartLocalPlayer is suspect at best, just not sure of a better way at the moment.
			if (pv.IsMine)// info.photonView.isMine)
			{
				foreach (INetEvents cb in iNetEvents)
					cb.OnStartLocalPlayer();

				foreach (IOnStartLocalPlayer cb in iOnStartLocalPlayer)
					cb.OnStartLocalPlayer();
			}

			foreach (INetEvents cb in iNetEvents)
				cb.OnStart();

			foreach (IOnStart cb in iOnStart)
				cb.OnStart();
		}

		public override void OnConnectedToMaster()
		{
			foreach (INetEvents cb in iNetEvents)
				cb.OnConnect(ServerClient.Master);

			foreach (IOnConnect cb in iOnConnect)
				cb.OnConnect(ServerClient.Master);
		}

		//public override void OnMasterClientSwitched(PhotonPlayer newMasterClient)
		//{

		//}

		// Detect changes in ownership
		//public override void OnOwnershipTransfered(object[] viewAndPlayers)
		public override void OnMasterClientSwitched(Player newMasterClient)
		{
			//PhotonView changedView = viewAndPlayers[0] as PhotonView;

			//if (changedView != pv)
			//	return;

			if (newMasterClient.IsLocal)
			//if (changedView.IsMine)
			{


				if (iNetEvents != null)
					foreach (INetEvents cb in iNetEvents)
						cb.OnStartAuthority();

				if (iOnNetworkDestroy != null)
					foreach (IOnStartAuthority cb in iOnStartAuthority)
						cb.OnStartAuthority();
			}
			//else
			//{
			//	if (iNetEvents != null)
			//		foreach (INetEvents cb in iNetEvents)
			//			cb.OnStopAuthority();

			//	if (iOnNetworkDestroy != null)
			//		foreach (IOnStopAuthority cb in iOnStopAuthority)
			//			cb.OnStopAuthority();
			//}
		}

		

		//// TODO this generates a little garbage
		//public override void OnPhotonInstantiate(PhotonMessageInfo info)
		//{
		//	// If this is the first nst this client has spawned, call it the local player
		//	if (pv.IsMine && !NSTTools.localPlayerNST)
		//		NSTTools.localPlayerNST = nst;

		//	if (pv.IsMine)// info.photonView.isMine)
		//	{

		//		foreach (INetEvents cb in iNetEvents)
		//			cb.OnStartLocalPlayer();

		//		foreach (IOnStartLocalPlayer cb in iOnStartLocalPlayer)
		//			cb.OnStartLocalPlayer();

		//	}
		//}

		public override void OnDisconnected(DisconnectCause cause)
		{
			if (iNetEvents != null)
				foreach (INetEvents cb in iNetEvents)
					cb.OnNetworkDestroy();

			if (iOnNetworkDestroy != null)
				foreach (IOnNetworkDestroy cb in iOnNetworkDestroy)
					cb.OnNetworkDestroy();
		}
		

		/// <summary>
		/// Remove the adapter and the NetworkIdentity/View from an object
		/// </summary>
		/// <param name="nst"></param>
		public static void RemoveAdapter(NetworkSyncTransform nst)
		{
			NSTNetAdapter na = nst.GetComponent<NSTNetAdapter>();
			PhotonView pv = nst.GetComponent<PhotonView>();

			if (na)
				DestroyImmediate(na);

			if (pv)
				DestroyImmediate(pv);
		}


#if UNITY_EDITOR

		/// <summary>
		/// Add a network adapter and the NetworkIdenity/NetworkView as needed. PhotonView needs to be added before runtime.
		/// If added at runtime, it may get added AFTER network events fire.
		/// </summary>
		public static void EnsureHasEntityComponentForNetLib(GameObject go, bool playerPrefabCandidate = true)
		{
			go.transform.root.gameObject.EnsureRootComponentExists<PhotonView>(false);
			AddAsRegisteredPrefab(go, playerPrefabCandidate);
		}
		/// <summary>
		/// Tries to register this NST as the player prefab (if there is none currently set), after doing some checks to make sure it makes sense to.
		/// </summary>
		public static void AddAsRegisteredPrefab(GameObject go, bool playerPrefabCandidate, bool silence = false)
		{
			// Doesn't apply to PUN
			PUNSampleLauncher punl = UnityEngine.Object.FindObjectOfType<PUNSampleLauncher>();

			if (punl && !punl.playerPrefab && playerPrefabCandidate)
			{
				XDebug.LogWarning("Adding " + go.name + " as the player prefab to " + typeof(PUNSampleLauncher).Name);
#if UNITY_2018_3_OR_NEWER
				GameObject parprefab = PrefabUtility.GetCorrespondingObjectFromSource(go);
#else
#pragma warning disable CS0618 // Type or member is obsolete
				GameObject parprefab = (GameObject)PrefabUtility.GetPrefabParent(go);
#pragma warning restore CS0618 // Type or member is obsolete
#endif
				punl.playerPrefab = parprefab ? parprefab : go;
			}
		}

#endif
	}

#if UNITY_EDITOR

	[CustomEditor(typeof(NSTNetAdapter))]
	[CanEditMultipleObjects]
	public class NSTNetAdapterEditor : NSTHeaderEditorBase
	{
		public override void OnEnable()
		{
			headerName = HeaderAnimatorAddonName;
			headerColor = HeaderAnimatorAddonColor;
			base.OnEnable();
		}
	}
#endif
}

#endregion

#elif PUN

/// ----------------------------------------   PUN 1    -----------------------------------------------------
/// ----------------------------------------   PUN 1    -----------------------------------------------------
/// ----------------------------------------   PUN 1    -----------------------------------------------------
 
#region PUN

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using emotitron.Compression;
using emotitron.Utilities.GUIUtilities;
using emotitron.Debugging;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace emotitron.NST
{
	public struct PlayerInitData
	{
		public uint nstId;
	}

	/// <summary>
	/// This class contains the abstracted methods for different networking libraries. 
	/// This adapter is for Photon PUN.
	/// </summary>
	[DisallowMultipleComponent]
	[AddComponentMenu("")]
	[RequireComponent(typeof(PhotonView))]

	public class NSTNetAdapter : Photon.PunBehaviour //, INstNetAdapter
	{
		public const string ADAPTER_NAME = "PUN";
		public static NetworkLibrary NetLibrary { get { return NetworkLibrary.PUN; } }

		PhotonView pv;
		//NetworkSyncTransform nst;
		NSTSettings nstSettings;

		// callback interfaces... collected on awake from all children on this gameobject, and can be subcribed to as well.
		[HideInInspector] public List<INetEvents> iNetEvents = new List<INetEvents>();
		//[HideInInspector] public List<IOnStartServer> iOnStartServer = new List<IOnStartServer>();
		//[HideInInspector] public List<IOnStartClient> iOnStartClient = new List<IOnStartClient>();
		[HideInInspector] public List<IOnConnect> iOnConnect = new List<IOnConnect>();
		[HideInInspector] public List<IOnStartLocalPlayer> iOnStartLocalPlayer = new List<IOnStartLocalPlayer>();
		[HideInInspector] public List<IOnNetworkDestroy> iOnNetworkDestroy = new List<IOnNetworkDestroy>();
		[HideInInspector] public List<IOnStartAuthority> iOnStartAuthority = new List<IOnStartAuthority>();
		[HideInInspector] public List<IOnStopAuthority> iOnStopAuthority = new List<IOnStopAuthority>();
		[HideInInspector] public List<IOnStart> iOnStart = new List<IOnStart>();

		public bool IsServer { get { return MasterNetAdapter.ServerIsActive; } }
		public bool IsLocalPlayer { get { return pv.isMine; } } // isLocalPlayer; } }
		public bool IsMine { get { return pv.isMine; } }

		public uint NetId { get { return (uint)pv.viewID; } }
		public int ClientId { get { return pv.ownerId; } }

		private uint _nstIdSyncvar;
		public uint NstIdSyncvar { get { return _nstIdSyncvar; } set {  _nstIdSyncvar = value; } }

		// cached values
		public AuthorityModel authorityModel;

		///// <summary> Does this client have authority over all aspects of this networked object (rather than just movement). Determined by the authority model
		///// selected in MasterSettings.</summary>

		public bool IAmActingAuthority
		{
			get {

				if (authorityModel == AuthorityModel.ServerAuthority)
					if (PhotonNetwork.isMasterClient)
						return true;

				if (authorityModel == AuthorityModel.OwnerAuthority)
					if (pv.isMine)
						return true;

				return false;
			}
		}
		
		public void CollectCallbackInterfaces()
		{
			GetComponentsInChildren(true, iNetEvents);
		}

		void Awake()
		{
			pv = GetComponent<PhotonView>();
			//nst = GetComponent<NetworkSyncTransform>();

			//XDebug.LogError("You appear to have an 'NetworkSyncTransform' on instantiated object '" + name + "', but that object has NOT been network spawned. " +
			//	"Only use NST on objects you intend to spawn normally from the server using PhotonNetwork.Instantiate(). " +
			//	"(Projectiles for example probably don't need to be networked objects).", (nst.destroyUnspawned && pv.viewID == 0), true);

			authorityModel = (AuthorityModel)NetLibrarySettings.Single.defaultAuthority;

			CollectCallbackInterfaces();

		}
		private void Start()
		{
			foreach (INetEvents cb in iNetEvents)
				cb.OnStart();

			foreach (IOnStart cb in iOnStart)
				cb.OnStart();
		}

		public override void OnConnectedToMaster()
		{
			foreach (INetEvents cb in iNetEvents)
				cb.OnConnect(ServerClient.Master);

			foreach (IOnConnect cb in iOnConnect)
				cb.OnConnect(ServerClient.Master);
		}

		//public override void OnMasterClientSwitched(PhotonPlayer newMasterClient)
		//{

		//}

		// Detect changes in ownership
		public override void OnOwnershipTransfered(object[] viewAndPlayers)
		{
			Debug.Log(pv.viewID + " <b>OnOwnershipTransfered</b> " + PhotonNetwork.isMasterClient + " " + PhotonNetwork.isNonMasterClientInRoom);

			PhotonView changedView = viewAndPlayers[0] as PhotonView;

			if (changedView != pv)
				return;

			if (changedView.isMine)
			{

				if (iNetEvents != null)
					foreach (INetEvents cb in iNetEvents)
						cb.OnStartAuthority();

				if (iOnNetworkDestroy != null)
					foreach (IOnStartAuthority cb in iOnStartAuthority)
						cb.OnStartAuthority();
			}
			else
			{
				if (iNetEvents != null)
					foreach (INetEvents cb in iNetEvents)
						cb.OnStopAuthority();

				if (iOnNetworkDestroy != null)
					foreach (IOnStopAuthority cb in iOnStopAuthority)
						cb.OnStopAuthority();
			}
		}
		
		// TODO this generates a little garbage
		public override void OnPhotonInstantiate(PhotonMessageInfo info)
		{
			//// If this is the first nst this client has spawned, call it the local player
			//if (pv.isMine && !NSTTools.localPlayerNST)
			//	NSTTools.localPlayerNST = nst;

			if (pv.isMine)// info.photonView.isMine)
			{

				foreach (INetEvents cb in iNetEvents)
					cb.OnStartLocalPlayer();

				foreach (IOnStartLocalPlayer cb in iOnStartLocalPlayer)
					cb.OnStartLocalPlayer();

			}
		}
		
		public override void OnDisconnectedFromPhoton()
		{
			if (iNetEvents != null)
				foreach (INetEvents cb in iNetEvents)
					cb.OnNetworkDestroy();

			if (iOnNetworkDestroy != null)
				foreach (IOnNetworkDestroy cb in iOnNetworkDestroy)
					cb.OnNetworkDestroy();
		}

		public void SendBitstreamToOwner(ref UdpBitStream bitstream)
		{
			Debug.LogError("Not Implemented");
		}

		/// <summary>
		/// Remove the adapter and the NetworkIdentity/View from an object
		/// </summary>
		/// <param name="nst"></param>
		public static void RemoveAdapter(NetworkSyncTransform nst)
		{
			NSTNetAdapter na = nst.GetComponent<NSTNetAdapter>();
			PhotonView pv = nst.GetComponent<PhotonView>();

			if (na)
				DestroyImmediate(na);

			if (pv)
				DestroyImmediate(pv);
		}


#if UNITY_EDITOR

		/// <summary>
		/// Add a network adapter and the NetworkIdenity/NetworkView as needed. PhotonView needs to be added before runtime.
		/// If added at runtime, it may get added AFTER network events fire.
		/// </summary>
		public static void EnsureHasEntityComponentForNetLib(GameObject go, bool playerPrefabCandidate = true)
		{
			go.transform.root.gameObject.EnsureRootComponentExists<PhotonView>(false);
			AddAsRegisteredPrefab(go, playerPrefabCandidate);
		}
		/// <summary>
		/// Tries to register this NST as the player prefab (if there is none currently set), after doing some checks to make sure it makes sense to.
		/// </summary>
		public static void AddAsRegisteredPrefab(GameObject go, bool playerPrefabCandidate, bool silence = false)
		{
			// Doesn't apply to PUN
			PUNSampleLauncher punl = UnityEngine.Object.FindObjectOfType<PUNSampleLauncher>();

			if (punl && !punl.playerPrefab && playerPrefabCandidate)
			{
				XDebug.LogWarning("Adding " + go.name + " as the player prefab to " + typeof(PUNSampleLauncher).Name);
#if UNITY_2018_3_OR_NEWER
				GameObject parprefab = PrefabUtility.GetCorrespondingObjectFromSource(go);
#else
#pragma warning disable CS0618 // Type or member is obsolete
				GameObject parprefab = (GameObject)PrefabUtility.GetPrefabParent(go);
#pragma warning restore CS0618 // Type or member is obsolete
#endif
				punl.playerPrefab = parprefab ? parprefab : go;
			}
		}
#endif
	}

#if UNITY_EDITOR

	[CustomEditor(typeof(NSTNetAdapter))]
	[CanEditMultipleObjects]
	public class NSTNetAdapterEditor : NSTHeaderEditorBase
	{
		public override void OnEnable()
		{
			headerName = HeaderAnimatorAddonName;
			headerColor = HeaderAnimatorAddonColor;
			base.OnEnable();
		}
	}
#endif
}

#endregion

#elif MIRROR || !UNITY_2019_1_OR_NEWER

/// ------------------------------------   UNET / MIRROR    -------------------------------------------------
/// ------------------------------------   UNET / MIRROR    -------------------------------------------------
/// ------------------------------------   UNET / MIRROR    -------------------------------------------------

#region UNET / MIRROR

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using emotitron.Compression;
using emotitron.Utilities.GUIUtilities;

#if MIRROR
using Mirror;
#else
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
	/// This class contains the abstracted methods for different networking libraries. 
	/// This adapter is for UNET.
	/// </summary>
	[DisallowMultipleComponent]
	[NetworkSettings(sendInterval = 0)]
	[AddComponentMenu("")]

#if MIRROR
	[RequireComponent(typeof(Mirror.NetworkIdentity))]
#else
	[RequireComponent(typeof(UnityEngine.Networking.NetworkIdentity))]
#endif

	public class NSTNetAdapter : NetworkBehaviour
	{
#if MIRROR
		public const string ADAPTER_NAME = "MIRROR";
#else
		public const string ADAPTER_NAME = "UNET";
#endif
		public static NetworkLibrary NetLibrary { get { return NetworkLibrary.UNET; } }

		NetworkIdentity ni;
		NSTSettings nstSettings;

		// callback interfaces... collected on awake from all children on this gameobject, and can be subcribed to as well.
		[HideInInspector] public List<INetEvents> iNetEvents = new List<INetEvents>();
		[HideInInspector] public List<IOnConnect> iOnConnect = new List<IOnConnect>();
		[HideInInspector] public List<IOnStartLocalPlayer> iOnStartLocalPlayer = new List<IOnStartLocalPlayer>();
		[HideInInspector] public List<IOnNetworkDestroy> iOnNetworkDestroy = new List<IOnNetworkDestroy>();
		[HideInInspector] public List<IOnStartAuthority> iOnStartAuthority = new List<IOnStartAuthority>();
		[HideInInspector] public List<IOnStopAuthority> iOnStopAuthority = new List<IOnStopAuthority>();
		[HideInInspector] public List<IOnStart> iOnStart = new List<IOnStart>();

		public bool IsServer { get { return isServer; } }
		public bool IsLocalPlayer { get { return isLocalPlayer; } }
		public bool IsMine { get { return hasAuthority; } }

#if MIRROR
		public uint NetId { get { return ni.netId; } }
#else
		public uint NetId { get { return ni.netId.Value; } }
#endif
		//public int ClientId { get { return (ni.clientAuthorityOwner == null) ? -1 : ni.clientAuthorityOwner.connectionId; } }
		public int ClientId { get { return ni.clientAuthorityOwner.connectionId; } }

		[SyncVar]
		private uint _nstIdSyncvar;
		public uint NstIdSyncvar { get { return _nstIdSyncvar; } set { _nstIdSyncvar = value; } }

		[HideInInspector]
		[System.NonSerialized]
		public AuthorityModel cachedAuthModel;

		public bool IAmActingAuthority
		{
			get
			{
				if (cachedAuthModel == AuthorityModel.ServerAuthority)
					if (NetworkServer.active)
						return true;

				if (cachedAuthModel == AuthorityModel.OwnerAuthority)
					if (hasAuthority)
						return true;

				return false;
			}
		}

		public void CollectCallbackInterfaces()
		{
			GetComponentsInChildren(true, iNetEvents);
		}

		void Awake()
		{
			cachedAuthModel = (AuthorityModel)NetLibrarySettings.Single.defaultAuthority;

			ni = GetComponent<NetworkIdentity>();
			CollectCallbackInterfaces();
		}

		public override void OnStartServer()
		{
			foreach (INetEvents cb in iNetEvents)
				cb.OnConnect(ServerClient.Server);

			foreach (IOnConnect cb in iOnConnect)
				cb.OnConnect(ServerClient.Server);
		}

		public override void OnStartClient()
		{
			foreach (INetEvents cb in iNetEvents)
				cb.OnConnect(ServerClient.Client);

			foreach (IOnConnect cb in iOnConnect)
				cb.OnConnect(ServerClient.Client);

		}

		public override void OnStartLocalPlayer()
		{
			foreach (INetEvents cb in iNetEvents)
				cb.OnStartLocalPlayer();

			foreach (IOnStartLocalPlayer cb in iOnStartLocalPlayer)
				cb.OnStartLocalPlayer();
		}

		public void Start()
		{
			//XDebug.LogError("You appear to have a NetworkIdentity on instantiated object '" + name + "', but that object has NOT been network spawned. " +
			//	"Only use NetworkSyncTransform and NetworkIdentity on objects you intend to spawn normally from the server using NetworkServer.Spawn(). " +
			//		"(Projectiles for example probably don't need to be networked objects).", ni.netId.Value == 0, true);

			//// If this is an invalid NST... abort startup and shut it down.
			//if (ni.netId.Value == 0)
			//{
			//	Destroy(GetComponent<NetworkSyncTransform>());
			//	return;
			//}


			foreach (INetEvents cb in iNetEvents)
				cb.OnStart();

			foreach (IOnStart cb in iOnStart)
				cb.OnStart();
		}

		public override void OnNetworkDestroy()
		{
			if (iNetEvents != null)
				foreach (INetEvents cb in iNetEvents)
					cb.OnNetworkDestroy();

			if (iOnNetworkDestroy != null)
				foreach (IOnNetworkDestroy cb in iOnNetworkDestroy)
					cb.OnNetworkDestroy();
		}

		public override void OnStartAuthority()
		{
			if (iNetEvents != null)
				foreach (INetEvents cb in iNetEvents)
					cb.OnStartAuthority();

			if (iOnNetworkDestroy != null)
				foreach (IOnStartAuthority cb in iOnStartAuthority)
					cb.OnStartAuthority();
		}

		public override void OnStopAuthority()
		{
			if (iNetEvents != null)
				foreach (INetEvents cb in iNetEvents)
					cb.OnStopAuthority();

			if (iOnNetworkDestroy != null)
				foreach (IOnStopAuthority cb in iOnStopAuthority)
					cb.OnStopAuthority();
		}

		/// <summary>
		/// Get the RTT in seconds for the owner of this network object. Only valid on Server.
		/// </summary>
		public float GetRTT()
		{
			return MasterRTT.GetRTT(ni.clientAuthorityOwner.connectionId);

			//NetworkConnection conn = NI.clientAuthorityOwner;
			//byte error = 0;
			//return (conn == null || conn.hostId == -1) ? 0 :
			//	.001f * NetworkTransport.GetCurrentRTT(NI.clientAuthorityOwner.hostId, NI.clientAuthorityOwner.connectionId, out error);
		}

		/// <summary>
		/// Get the RTT to the player who owns this NST
		/// </summary>
		public static float GetRTT(NetworkSyncTransform nstOfOwner)
		{
			return nstOfOwner.na.GetRTT();
		}

		//public void SendBitstreamToOwner(ref UdpBitStream bitstream)
		//{
		//	ni.clientAuthorityOwner.SendBitstreamToThisConn(ref bitstream, Channels.DefaultUnreliable);
		//}

		/// <summary>
		/// Remove the adapter and the NetworkIdentity/View from an object
		/// </summary>
		/// <param name="nst"></param>
		public static void RemoveAdapter(NetworkSyncTransform nst)
		{
			NetworkIdentity ni = nst.GetComponent<NetworkIdentity>();
			NSTNetAdapter na = nst.GetComponent<NSTNetAdapter>();

			if (na)
				DestroyImmediate(na);

			if (ni)
				DestroyImmediate(ni);
		}

#if UNITY_EDITOR

		///// <summary>
		///// Add the NetworkIdenity/PhotonView to an NST gameobject. Must be added before runtime (thus this is editor only script).
		///// If added at runtime, it may get added AFTER network events fire. Also will attempt to add this NST as a registered prefab
		///// and player prefab. Will also attempt to register the supplied go with the NetworkManager and as the PlayerPrefab if there is none
		///// but one is expected.
		///// </summary>
		//public static void EnsureHasEntityComponentForNetLib(GameObject go, bool playerPrefabCandidate = true)
		//{
		//	if (!Application.isPlaying)
		//		AddAsRegisteredPrefab(go, true, !playerPrefabCandidate, true);
		//}
		/// <summary>
		/// Attempts to add a prefab with NST on it to the NetworkManager spawnable prefabs list, after doing some checks to make 
		/// sure it makes sense to. Will then add as the network manager player prefab if it is set to auto spawwn and is still null.
		/// </summary>
		public static bool AddAsRegisteredPrefab(GameObject go, bool playerPrefabCandidate, bool silence = false)
		{
			if (Application.isPlaying)
				return false;

			// Don't replace an existing playerPrefab
			NetworkManager nm = NetAdapterTools.GetNetworkManager();

			if (!nm)
				return false;



#if UNITY_2018_2_OR_NEWER
			GameObject prefabGO = PrefabUtility.GetCorrespondingObjectFromSource(go) as GameObject;

			if (!prefabGO)
			{
				//if (!silence)
				//	Debug.Log("You have a NST component on a gameobject '" + go.name + "', which is not a prefab. Be sure to make '" + go.name + "' a prefab, otherwise it cannot be registered with the NetworkManager for network spawning.");
				return false;
			}

#else
			PrefabType type = PrefabUtility.GetPrefabType(go);
			GameObject prefabGO = (type == PrefabType.Prefab) ? go : PrefabUtility.GetPrefabParent(go) as GameObject;

			if (!prefabGO)
			{
				if (!silence)
					Debug.Log("You have a NST component on a gameobject '" + go.name + "', which is not a prefab. Be sure to make '" + go.name + "' a prefab, otherwise it cannot be registered with the NetworkManager for network spawning.");
				return false;
			}

#endif
			NetworkIdentity ni = prefabGO.GetComponent<NetworkIdentity>();

//#if MIRROR
//			if (!ni)
//				ni = prefabGO.AddComponent<NetworkIdentity>();

//			var unetNI = prefabGO.GetComponent<UnityEngine.Networking.NetworkIdentity>();
//			if (unetNI)
//			{
//				bool lpa = unetNI.localPlayerAuthority;
//				bool svr = unetNI.serverOnly;
//				DestroyImmediate(unetNI, true);

//				ni.localPlayerAuthority = lpa;
//				ni.serverOnly = svr;
//			}
//#else
			if (!ni)
				ni = prefabGO.AddComponent<NetworkIdentity>();
//#endif

			if (!ni)
			{
				if (!silence)
					Debug.Log("There is no NetworkIdentity on '" + go.name + "', so it cannot be registered with the NetworkManager for network spawning.");
				return false;
			}

			// Force the NetworkIdentity to be valid. Bad things happen if we don't do this. UNET suck.
#if PUN_2_OR_NEWER

#elif !MIRROR
			ni.assetId.IsValid();
#endif
			if (!nm.spawnPrefabs.Contains(prefabGO))
			{
				Debug.Log("Automatically adding '<b>" + prefabGO.name + "</b>' to the NetworkManager spawn list for you.");

				nm.spawnPrefabs.Add(prefabGO);
				EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
			}

			// Set this as the player prefab if there is none yet
			if (nm.playerPrefab == null && nm.autoCreatePlayer && playerPrefabCandidate)
			{
				Debug.Log("Automatically adding '<b>" + prefabGO.name + "</b>' to the NetworkManager as the <b>playerPrefab</b>. If this isn't desired, assign your the correct prefab to the Network Manager, or turn off Auto Create Player in the NetworkManager.");
				nm.playerPrefab = prefabGO;
				EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
			}

			NetAdapterTools.EnsureNMPlayerPrefabIsLocalAuthority(nm);
			return true;
		}
#endif
	}

#if UNITY_EDITOR

	[CustomEditor(typeof(NSTNetAdapter))]
	[CanEditMultipleObjects]
	public class NSTNetAdapterEditor : NSTHeaderEditorBase
	{
		public override void OnEnable()
		{
			headerName = HeaderAnimatorAddonName;
			headerColor = HeaderAnimatorAddonColor;
			base.OnEnable();
		}
	}
#endif

}

#pragma warning restore CS0618 // UNET obsolete

#endregion // UNET / MIRROR

#endif // END UNET / MIRROR