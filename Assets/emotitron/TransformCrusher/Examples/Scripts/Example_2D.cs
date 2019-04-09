//Copyright 2018, Davin Carten, All rights reserved

using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using emotitron.Debugging;
using emotitron.Utilities.Networking;

#if PUN_2_OR_NEWER
using Photon.Pun;
//#elif MIRROR
//using Mirror;
#else
using UnityEngine.Networking;
#endif

#pragma warning disable CS0618 // UNET is obsolete

namespace emotitron.Compression.Sample
{
	/// <summary>
	/// A VERY basic compressed sync example using UNET. There is no interpolation, buffering or extrapoltion in this example - 
	/// this is NOT an example of good networking. This is only to demonstrate the usage of the crushers.
	/// </summary>
	public class Example_2D :
#if PUN_2_OR_NEWER
		Photon.Pun.MonoBehaviourPunCallbacks,
#else
		NetworkBehaviour,
#endif
		IHasTransformCrusher
	{
		/// The lookup table for finding which net object incoming updates belong to.
		public static Dictionary<int, Example_2D> players = new Dictionary<int, Example_2D>();

		public const byte CLIENT_SND_ID = 222;
		public const byte SERVER_SND_ID = 223;

		/// Reference to the crusher we are using for the IHasTransformCrusher interface.
		/// This is used by the BasicController to get the bounds of our crusher to clamp movement.
		public TransformCrusher TC { get { return sharedCrusher; } }

		/// SharedTransformCrusher is used here instead of TransformCrusher. 
		/// The Shared wrappers (SharedTransformCrusher, SharedElementCrusher and SharedFloatCrusher do a bunch of work 
		/// behind the scenes to share a single crusher instance (which serialized objects tend not to like in Unity).
		/// The Crusher can be initialized once using the Crusher property. It will only construct once with this initializer,
		/// and all future initialization by new instances of this component will use the existing instance 
		/// (which is stored in the SharedCrusherSO scriptable object singleton).
		public SharedTransformCrusher sharedCrusher = new SharedTransformCrusher()
		{
			Crusher = new TransformCrusher()
			{
				PosCrusher = new ElementCrusher(TRSType.Position, false)
				{
					ZCrusher = new FloatCrusher(Axis.Z, true) { Enabled = false }
				},
				RotCrusher = new ElementCrusher(TRSType.Euler, false)
				{
					XCrusher = new FloatCrusher(Axis.X, TRSType.Euler, true) { Enabled = false },
					YCrusher = new FloatCrusher(Axis.Y, TRSType.Euler, true) { Enabled = false },
					ZCrusher = new FloatCrusher(Axis.Z, TRSType.Euler, true) { Enabled = true }
				}
			}
		};

		/// Preallocate the classes used for storing the compressed values returned from TransformCrusher
		private CompressedMatrix compMatrix = new CompressedMatrix();
		private CompressedMatrix sentCompMatrix = new CompressedMatrix();

		/// reusable bitstream.
		public static byte[] bitstream = new byte[48];

		void Awake()
		{
			/// If you would like to get warnings from the various compression DLLs we need to send Assert fails to the Debug.Log - 
			/// This does that.
			XDebug.ForwardTraceListener(true);
			XDebug.RedirectConsoleErrorToDebug(true);
		}

#if !PUN_2_OR_NEWER
		public override void OnStartServer() { NetMsgCallbacks.RegisterHandler(CLIENT_SND_ID, OnServerRcv, true); }
		public override void OnStartClient() { NetMsgCallbacks.RegisterHandler(SERVER_SND_ID, OnClientRcv, false); }
#endif

		private void Start()
		{
			/// Register our methods as Unet Msg Receivers
			NetMsgCallbacks.RegisterHandler(CLIENT_SND_ID, OnServerRcv, true);
			NetMsgCallbacks.RegisterHandler(SERVER_SND_ID, OnClientRcv, false);

			/// Add this component to the dictionary of netobjects. Netid is used as the key.
#if PUN_2_OR_NEWER
			players.Add((int)photonView.ViewID, this);
#else
			players.Add(NetId, this);
#endif
		}

		private void OnDestroy()
		{
			if (players.ContainsKey(NetId))
				players.Remove(NetId);

			if (UsingPUN)
			{
				NetMsgCallbacks.UnregisterHandler(CLIENT_SND_ID, OnServerRcv, true);
				NetMsgCallbacks.UnregisterHandler(SERVER_SND_ID, OnClientRcv, false);
			}
		}

		/// ------------------------------------------------------------------------------------------------------
		/// 1. Owner generates some values for us to network. In this an animated color, and a milliseconds value
		/// ------------------------------------------------------------------------------------------------------

		private void Update()
		{
			if (!IsMine)
				return;

			/// Apply them locally (Since we are the owner)
			ApplyValues();
		}

		/// ------------------------------------------------------------------------------------------------------
		/// 2. Owner sends compressed transform updates to the server every physics tick
		/// ------------------------------------------------------------------------------------------------------

		private void FixedUpdate()
		{
			if (!IsMine)
				return;

			SendUpdate();
		}

		/// ------------------------------------------------------------------------------------------------------
		/// 3. The Server receives the packet, unpacks it, and applies the values. 
		/// ------------------------------------------------------------------------------------------------------
		private static void OnServerRcv(byte[] bitstream)
		{
			var player = ReceiveUpdate(bitstream);

			if (!player || player.IsMine)
				return;

			/// This instance of Example_2D on the server sends its update to all clients.
			player.SendUpdate();
		}

		/// ------------------------------------------------------------------------------------------------------
		/// 4. Clients receive the packet from the server, unpack it, and apply the values. 
		/// ------------------------------------------------------------------------------------------------------
		private static void OnClientRcv(byte[] bitstream)
		{
			ReceiveUpdate(bitstream);
		}


		private void SendUpdate()
		{
			int writepos = 0;
			SerializeValuesToBitstream(bitstream, ref writepos);

			/// Serialize and send out this bitstream.
			if (UsingPUN)
				NetMsgSends.Send(bitstream, writepos, SERVER_SND_ID, ReceiveGroup.Others);
			else
				NetMsgSends.Send(bitstream, writepos, AsServer ? SERVER_SND_ID : CLIENT_SND_ID, AsServer ? ReceiveGroup.Others : ReceiveGroup.Master);
		}

		/// <summary>
		/// Write/Pack all of the values we want to network into a bitstream
		/// </summary>
		/// <param name="bitstream"></param>
		public void SerializeValuesToBitstream(byte[] bitstream, ref int writepos)
		{

			/// Write the NetId of the player this update belongs to.
			bitstream.Write((ulong)NetId, ref writepos, 32);

			/// Compress this objects current transform to the CompressedMatrix.
			sharedCrusher.Crusher.Compress(compMatrix, transform);

			/// For this example we compare the new CompressedMatrix with the previously sent one to see if anything has changed.
			/// If the compressed value has not changed, we will not send a compressed transform this tick. The == operator is overloaded
			/// for CompressedElement and CompressedMatrix, so == actually is running a.Equals(b) behind the scenes letting you compare the
			/// classes as if they were structs.
			if (compMatrix != sentCompMatrix)
			{
				/// Single true bit flag to indicate a compressed transform follows
				bitstream.WriteBool(true, ref writepos);

				/// Pass the bitstream to the TransformCrusher to serialize in the value of compMatrix.
				sharedCrusher.Crusher.Write(compMatrix, bitstream, ref writepos);

				/// Copy the values of of cm for comparison next tick.
				sentCompMatrix.CopyFrom(compMatrix);
			}
			else
			{
				/// Single false bit flag to indicate a compressed transform doesn't follow
				bitstream.WriteBool(false, ref writepos);
			}
		}

		/// Deserialize, Uppack, and Decompress the values
		private static Example_2D ReceiveUpdate(byte[] bitstream)
		{
			int readpos = 0;
			/// Get the ID for which player this belongs to, and use the lookup table we created to get the correct player instance.
			int id = (int)bitstream.Read(ref readpos, 32);

			if (!players.ContainsKey(id))
				return null;

			var player = players[id];

			// Don't apply these values if we own this.
			if (player.IsMine)
				return player;

			/// Pass the bitstream to the player instance, which will read out the compressed values
			if (bitstream.ReadBool(ref readpos))
			{
				/// Read the compressed transform values in from the bitstream.
				/// We pass it our destination CompressedMatrix class to hold the compressed version of the Matrix.
				player.sharedCrusher.Crusher.Read(player.compMatrix, bitstream, ref readpos);
			}


			/// Tell the player to apply those values.
			player.ApplyValues();

			return player;
		}

		/// Apply the received values to our GameObjects
		private void ApplyValues()
		{
			/// The TransformCrusher and ElementCrushers have an apply method that will apply changes without overwriting the values
			/// that were not networked. For example if your object only moves on the x axis, y and z will not change from their current
			/// values when Apply() is used.
			if (!IsMine)
				compMatrix.Apply(transform);
		}

		#region Net Library Abstractions



		//public class AddDefineSymbols : Editor
		//{



		//}

		private bool IsMine
		{
			get
			{
#if PUN_2_OR_NEWER
				return photonView.IsMine;
#else
				return hasAuthority;
#endif
			}
		}

		private int NetId
		{
			get
			{
#if PUN_2_OR_NEWER
				return (photonView) ? (int)photonView.ViewID : 0;
//#elif MIRROR
//				return (int)netId;
#else
				return (int)netId.Value;
#endif
			}
		}

		private static bool UsingPUN
		{
			get
			{
#if PUN_2_OR_NEWER
				return true;
#else
				return false;
#endif
			}
		}

		private bool AsServer
		{
			get
			{
#if PUN_2_OR_NEWER
				return false;
#else
				return NetworkServer.active;
#endif
			}
		}

#endregion
	}


//#if UNITY_EDITOR

//	[CustomEditor(typeof(Example_2D))]
//	[CanEditMultipleObjects]
//	public class Example_2DEditor : Editor
//	{
//		SerializedProperty sp;

		

//		//public void OnEnable()
//		//{

//		//}
//		//public override void OnInspectorGUI()
//		//{

//		//}
//	}

//#endif

}
#pragma warning restore CS0618 // UNET is obsolete
