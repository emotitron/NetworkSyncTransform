////Copyright 2018, Davin Carten, All rights reserved

//using UnityEngine;
//using System.Collections.Generic;
//using emotitron.Networking;
//using emotitron.Debugging;

//#if UNITY_EDITOR
//using UnityEditor;
//#endif

//namespace emotitron.Compression.Sample
//{
//	/// <summary>
//	/// A VERY basic compressed sync example using UNET. There is no interpolation, buffering or extrapoltion in this example - 
//	/// this is NOT an example of good networking. This is only to demonstrate the usage of the crushers.
//	/// </summary>
//	public class Singleton_Example :
//#if PUN_2_OR_NEWER
//		Photon.Pun.MonoBehaviourPunCallbacks,
//#else
//#pragma warning disable CS0618 // UNET is obsolete
//		UnityEngine.Networking.NetworkBehaviour,
//#pragma warning restore CS0618 // UNET is obsolete
//#endif
//		IHasTransformCrusher

//	{
//		/// The lookup table for finding which net object incoming updates belong to.
//		public static Dictionary<uint, Singleton_Example> players = new Dictionary<uint, Singleton_Example>();

//		public const byte CLIENT_SND_ID = 222;
//		public const byte SERVER_SND_ID = 223;

//		/// Reference to the crusher we are using for the IHasTransformCrusher interface.
//		/// This is used by the BasicController to get the bounds of our crusher to clamp movement.
//		public TransformCrusher TC { get { return SampleCrusherSO.Singleton.globalTransformCrusher; } }

//		/// A reusable bitstream struct (its 48 bytes - so best to recycle when possible
//		static Bitstream bitstream = new Bitstream();

//		void Awake()
//		{
//			/// If you would like to get warnings from the various compression DLLs we need to send Assert fails to the Debug.Log - 
//			/// This does that.
//			XDebug.ForwardTraceListener(true);
//			XDebug.RedirectConsoleErrorToDebug(true);
//		}

//#if !PUN_2_OR_NEWER
//		public override void OnStartServer() { NetMsgCallbacks.RegisterHandler(CLIENT_SND_ID, OnServerRcv, true); }
//		public override void OnStartClient() { NetMsgCallbacks.RegisterHandler(SERVER_SND_ID, OnClientRcv, false); }
//#endif

//		private void Start()
//		{
//			/// Register our methods as Unet Msg Receivers
//			/// Register our methods as Unet Msg Receivers
//			NetMsgCallbacks.RegisterHandler(CLIENT_SND_ID, OnServerRcv, true);
//			NetMsgCallbacks.RegisterHandler(SERVER_SND_ID, OnClientRcv, false);

//			/// Add this component to the dictionary of netobjects. Netid is used as the key.
//#if PUN_2_OR_NEWER
//			players.Add((uint)photonView.ViewID, this);
//#else
//			players.Add(NetId, this);
//#endif
//		}

//		private void OnDestroy()
//		{
//#if PUN_2_OR_NEWER
//			if (photonView)
//				players.Remove((uint)photonView.ViewID);
//#else
//			players.Remove((uint)netId.Value);
//#endif


//			if (UsingPUN)
//			{
//				NetMsgCallbacks.UnregisterHandler(CLIENT_SND_ID, OnServerRcv, true);
//				NetMsgCallbacks.UnregisterHandler(SERVER_SND_ID, OnClientRcv, false);
//			}
//		}

//		/// ------------------------------------------------------------------------------------------------------
//		/// 1. Owner sends compressed transform updates to the server every physics tick
//		///	The transform is being moved by the BasicController component.
//		/// ------------------------------------------------------------------------------------------------------

//		private void FixedUpdate()
//		{
//			if (!IsMine)
//				return;

//			SendUpdate(AsServer);
//		}

//		/// ------------------------------------------------------------------------------------------------------
//		/// 2. The Server receives the packet (If we are running Server/Client which is the case with UNET, but not PUN)
//		/// unpacks it, and applies the values. Then mirrors it off to all clients.
//		/// ------------------------------------------------------------------------------------------------------
//		private static void OnServerRcv(ref Bitstream bitstream)
//		{
//			/// Process the incoming messages - returns which instance Singleton_Example this belongs to
//			Singleton_Example player = ReceiveUpdate(ref bitstream);

//			/// This instance of Bitstream_Example on the server sends its update to all clients.
//			player.SendUpdate(true);
//		}

//		/// ------------------------------------------------------------------------------------------------------
//		/// 3. Clients receive the packet from the server, unpack it, and apply the values. 
//		/// ------------------------------------------------------------------------------------------------------

//		private static void OnClientRcv(ref Bitstream bitstream)
//		{
//			ReceiveUpdate(ref bitstream);
//		}

//		/// <summary>
//		/// Write/Pack all of the values we want to network into a bitstream
//		/// </summary>
//		/// <param name="bitstream"></param>
//		public void SendUpdate(bool asServer)
//		{
//			bitstream.Reset();

//			/// Write the NetId of the player this update belongs to. Cropping it down to 3 bits (0-7 range)
//			bitstream.Write(NetId, 16);

//			/// Compress and Serialize the current transform to the bitstream.
//			SampleCrusherSO.singleton.globalTransformCrusher.Write(transform, ref bitstream);

//			/// Send out this bitstream.
//			if (UsingPUN)
//				NetMsgSends.Send(ref bitstream, SERVER_SND_ID, ReceiveGroup.Others);
//			else
//				NetMsgSends.Send(ref bitstream, asServer ? SERVER_SND_ID : CLIENT_SND_ID, asServer ? ReceiveGroup.Others : ReceiveGroup.Master);

//		}

//		/// Deserialize, Uppack, and Decompress the values from the network
//		private static Singleton_Example ReceiveUpdate(ref Bitstream bitstream)
//		{
//			bitstream.ResetReadPtr();
//			/// Get the ID for which player this belongs to, and use the lookup table we created to get the correct player instance.
//			uint id = bitstream.ReadUint32(16);

//			if (!players.ContainsKey(id))
//				return null;

//			var player = players[id];

//			// Don't apply these values if we own this.
//			if (player.IsMine)
//				return player;

//			/// If we aren't the owner of this player, read the position from the bitstream.
//			if (!player.IsMine)
//				SampleCrusherSO.singleton.globalTransformCrusher.ReadAndApply(player.transform, ref bitstream);

//			return player;

//		}

//		#region Net Library Abstractions

//		private bool IsMine
//		{
//			get
//			{
//#if PUN_2_OR_NEWER
//				return photonView.IsMine;
//#else
//				return hasAuthority;
//#endif
//			}
//		}

//		private uint NetId
//		{
//			get
//			{
//#if PUN_2_OR_NEWER
				
//				return (uint)photonView.ViewID;
//#else
//				return (uint)netId.Value;
//#endif
//			}
//		}

//		private static bool UsingPUN
//		{
//			get
//			{
//#if PUN_2_OR_NEWER
//				return true;
//#else
//				return false;
//#endif
//			}
//		}

//		private bool AsServer
//		{
//			get
//			{
//#if PUN_2_OR_NEWER
//				return false;
//#else
//#pragma warning disable CS0618 // UNET is obsolete
//				return UnityEngine.Networking.NetworkServer.active;
//#pragma warning restore CS0618 // UNET is obsolete
//#endif
//			}
//		}

//		#endregion
//	}


//#if UNITY_EDITOR

//	[CustomEditor(typeof(Singleton_Example))]
//	[CanEditMultipleObjects]
//	public class Singleton_ExampleEditor : Editor
//	{
//		SerializedProperty sp;

//		public void OnEnable()
//		{
//			sp = new SerializedObject(SampleCrusherSO.Singleton).FindProperty("globalTransformCrusher");
//		}
//		public override void OnInspectorGUI()
//		{
//			base.OnInspectorGUI();
//			EditorGUILayout.HelpBox("The TransformCrusher below is actually a reference to a field in the ScriptableObject '" + SampleCrusherSO.singleton.name +
//				"'. This is an example of how a Crusher can be shared rather than instanced with each instances of a prefab.", MessageType.None);

//			if (GUI.Button(EditorGUILayout.GetControlRect(), new GUIContent("Ping Scriptable Object")))
//				EditorGUIUtility.PingObject(SampleCrusherSO.singleton);

//			EditorGUILayout.PropertyField(sp);


//		}
//	}

//#endif
//}


