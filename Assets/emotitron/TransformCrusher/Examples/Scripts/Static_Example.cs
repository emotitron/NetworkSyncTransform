
////Copyright 2018, Davin Carten, All rights reserved

//using UnityEngine;
//using System.Collections.Generic;
//using emotitron.Networking;

//#if PUN_2_OR_NEWER
//using Photon.Pun;
//#else
//using UnityEngine.Networking;
//#endif

//#if UNITY_EDITOR
//using UnityEditor;
//#endif

//namespace emotitron.Compression.Sample
//{

//	/// <summary>
//	/// A VERY basic compressed sync example. There is no interpolation, buffering or extrapoltion in this example - 
//	/// this is NOT an example of good networking. This is only to demonstrate the usage of the crushers.
//	/// </summary>
//	public class Static_Example :
//#if PUN_2_OR_NEWER
//		MonoBehaviourPunCallbacks,
//#else
//#pragma warning disable CS0618 // UNET is obsolete
//		NetworkBehaviour,
//#pragma warning restore CS0618 // UNET is obsolete
//#endif
//		IHasTransformCrusher
//	{
//		public TransformCrusher tc = new TransformCrusher();

//		public SharedTransformCrusher tcref = new SharedTransformCrusher(ShareByCommon.Prefab);

//		/// The lookup table for finding which net object incoming updates belong to.
//		public static Dictionary<uint, Static_Example> players = new Dictionary<uint, Static_Example>();
//		public const byte CLIENT_SND_ID = 222;
//		public const byte SERVER_SND_ID = 223;

//		public TransformCrusher crusher;
//		public static TransformCrusher sharedCrusher;

//		/// <summary>
//		/// To ensure that we are only using one instance of a crusher for all copies of a prefab, we keep a static crusher in the class,
//		/// and the first instance to construct will have its crusher become the reference all following instances use as well.
//		/// </summary>

//		// Constructor
//		public Static_Example()
//		{
//			if (sharedCrusher != null)
//			{
//				crusher = sharedCrusher;
//				return;
//			}

//			crusher = new TransformCrusher()
//			{
//				PosCrusher = new ElementCrusher(TRSType.Position, false)
//				{
//					xcrusher = new FloatCrusher(12, -5f, 5f, Axis.X, TRSType.Position, true),
//					ycrusher = new FloatCrusher(10, -4f, 4f, Axis.Y, TRSType.Position, true),
//					zcrusher = new FloatCrusher(10, -4f, 4f, Axis.Z, TRSType.Position, true)
//				},
//				RotCrusher = new ElementCrusher(TRSType.Euler, false)
//				{
//					xcrusher = new FloatCrusher(12, -45f, 45f, Axis.X, TRSType.Euler, true),
//					ycrusher = new FloatCrusher(Axis.Y, TRSType.Euler, true) { Bits = 12 },
//					zcrusher = new FloatCrusher(8, -45f, 45f, Axis.Z, TRSType.Euler, true)
//				},
//				SclCrusher = new ElementCrusher(TRSType.Scale)
//			};

//			sharedCrusher = crusher;
//		}


//		/// Reference to the crusher we are using for the IHasTransformCrusher interface.
//		/// This is used by the BasicController to get the bounds of our crusher to clamp movement.
//		public TransformCrusher TC { get { return crusher; } }

//		/// A reusable bitstream struct (its 48 bytes - so best to recycle when possible
//		static Bitstream bitstream = new Bitstream();

//#if !PUN_2_OR_NEWER
//		public override void OnStartServer() { NetMsgCallbacks.RegisterHandler(CLIENT_SND_ID, OnServerRcv, true); }
//		public override void OnStartClient() { NetMsgCallbacks.RegisterHandler(SERVER_SND_ID, OnClientRcv, false); }
//#endif
//		private void Start()
//		{
//			/// Register our methods as Msg Receivers
//			NetMsgCallbacks.RegisterHandler(CLIENT_SND_ID, OnServerRcv, true);
//			NetMsgCallbacks.RegisterHandler(SERVER_SND_ID, OnClientRcv, false);

//			/// Add this component to the dictionary of netobjects. Netid is used as the key.
//#if PUN_2_OR_NEWER
//			players.Add((uint)photonView.ViewID, this);
//#else
//			players.Add(NetId, this);
//#endif
//			/// At runtime, we can check our crushers for redundancies using the CheckAgainstStatics() method.
//			/// This is recursives, so testing TransformCrusher, will test each of its component ElementCrushers (pos, rot, scale)
//			/// and will test the FloatCrushers used by those ElementCrushers as well.
//			/// If any crusher already exists in the dictionary with the same settings, the reference will be replaced with the existing crusher,
//			/// allowing garbage collection of passed crusher.
//			crusher = TransformCrusher.CheckAgainstStatics(crusher);

//			Debug.Log("Static Crusher Counts: " +
//				" TransformCrushers = " + TransformCrusher.staticTransformCrushers.Count +
//				" ElementCrushers = " + ElementCrusher.staticElementCrushers.Count +
//				" FloatCrushers = " + FloatCrusher.staticFloatCrushers.Count +
//				". These counts should not increase as more players join, as each instance will use existing crushers.")
//				;
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

//			SerializeValuesToBitstream();
//		}

//		/// ------------------------------------------------------------------------------------------------------
//		/// 2. The Server receives the packet, unpacks it, and applies the values. Then mirrors it off to all clients.
//		/// ------------------------------------------------------------------------------------------------------
//		private static void OnServerRcv(ref Bitstream bitstream)
//		{
//			/// Process the incoming messages - returns which instance Singleton_Example this belongs to
//			Static_Example player = ReceiveUpdate(ref bitstream);

//			/// This instance of Bitstream_Example on the server sends its update to all clients.
//			player.SerializeValuesToBitstream();
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
//		public void SerializeValuesToBitstream()
//		{
//			bitstream.Reset();

//			/// Write the NetId of the player this update belongs to. Cropping it down to 3 bits (0-7 range)
//			bitstream.Write(NetId, 16);

//			/// Compress and Serialize the current transform to the bitstream.
//			crusher.Write(transform, ref bitstream);

//			/// Serialize and send out this bitstream.
//			if (UsingPUN)
//				NetMsgSends.Send(ref bitstream, SERVER_SND_ID, ReceiveGroup.Others);
//			else
//				NetMsgSends.Send(ref bitstream, AsServer ? SERVER_SND_ID : CLIENT_SND_ID, AsServer ? ReceiveGroup.Others : ReceiveGroup.Master);
//		}

//		/// Deserialize, Uppack, and Decompress the values from the network
//		private static Static_Example ReceiveUpdate(ref Bitstream bitstream)
//		{
//			/// Get the ID for which player this belongs to, and use the lookup table we created to get the correct player instance.
//			/// For this example we are only writing/reading 3 bits of the networkId to demonstrate bitpacking.
//			uint id = bitstream.ReadUint32(16);
//			var player = players[id];

//			/// If we aren't the owner of this player, read the position from the bitstream.
//			if (!player.IsMine)
//				player.crusher.ReadAndApply(player.transform, ref bitstream);

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
//				return NetworkServer.active;
//#pragma warning restore CS0618 // UNET is obsolete
//#endif
//			}
//		}

//		#endregion
//	}


//#if UNITY_EDITOR

//	[CustomEditor(typeof(Static_Example))]
//	[CanEditMultipleObjects]
//	public class Static_ExampleEditor : Editor
//	{
//		SerializedProperty sp;

//		public override void OnInspectorGUI()
//		{
//			base.OnInspectorGUI();
//			EditorGUILayout.HelpBox("The TransformCrusher above will construct when player instances are created at runtime, but because we use CheckAgainstStatics() any redundant crushers are immediately released for garbage collection.", MessageType.None);
//		}
//	}

//#endif
//}


