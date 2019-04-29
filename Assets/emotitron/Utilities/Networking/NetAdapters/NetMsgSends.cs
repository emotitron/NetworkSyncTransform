using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using emotitron.Debugging;
using emotitron.Compression;


#if PUN_2_OR_NEWER
using Photon;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
#elif MIRROR
using Mirror;
#else
using UnityEngine.Networking;
#endif

#pragma warning disable CS0618 // UNET obsolete

namespace emotitron.Utilities.Networking
{

	public enum ReceiveGroup { Others, All, Master }

	/// <summary>
	/// Unified code for sending network messages across different Network Libraries.
	/// </summary>
	public static class NetMsgSends
	{
		public static byte[] reusableByteArray = new byte[512];

#if PUN_2_OR_NEWER

		public static bool ReadyToSend { get { return PhotonNetwork.NetworkClientState == ClientState.Joined; } }
		public static bool AmActiveServer { get { return false; } }

		private static RaiseEventOptions[] opts = new RaiseEventOptions[3]
		{
			new RaiseEventOptions() { Receivers = ReceiverGroup.Others },
			new RaiseEventOptions() { Receivers = ReceiverGroup.All },
			new RaiseEventOptions() { Receivers = ReceiverGroup.MasterClient }
		};

		private static SendOptions sendOpts = new SendOptions();

		public static void Send(this byte[] buffer, ushort bytecount, byte msgId, ReceiveGroup rcvGrp)
		{
			//TODO replace this GC generating mess with something prealloc
			System.ArraySegment<byte> byteseg = new System.ArraySegment<byte>(buffer, 0, bytecount);

			PhotonNetwork.NetworkingClient.OpRaiseEvent(msgId, byteseg, opts[(int)rcvGrp], sendOpts);
			PhotonNetwork.NetworkingClient.Service();
		}

		public static void Send(this byte[] buffer, int bitposition, byte msgId, ReceiveGroup rcvGrp)
		{
			int bytecount = (bitposition + 7) >> 3;

			//TODO replace this GC generating mess with something prealloc
			System.ArraySegment<byte> byteseg = new System.ArraySegment<byte>(buffer, 0, bytecount);

			PhotonNetwork.NetworkingClient.OpRaiseEvent(msgId, byteseg, opts[(int)rcvGrp], sendOpts);
			PhotonNetwork.NetworkingClient.Service();
		}

		public static void Send(this byte[] buffer, byte msgId, ReceiveGroup rcvGrp)
		{
			//TODO replace this GC generating mess with something prealloc
			int bytecount = buffer.Length;
			byte[] streambytes = new byte[bytecount];
			Array.Copy(buffer, streambytes, bytecount);
			PhotonNetwork.NetworkingClient.OpRaiseEvent(msgId, streambytes, opts[(int)rcvGrp], sendOpts);
			PhotonNetwork.NetworkingClient.Service();
		}
#else

		public static bool ReadyToSend { get { return NetworkServer.active || ClientScene.readyConnection != null; } }
		public static bool AmActiveServer { get { return NetworkServer.active; } }

		//static readonly NetworkWriter reusableunetwriter = new NetworkWriter();
		public static readonly BytesMessageNonalloc bytesmsg = new BytesMessageNonalloc();

		public static void Send(ref Bitstream buffer, short msgId, ReceiveGroup rcvGrp)
		{
			BitstreamExtensions.ReadOut(ref buffer, reusableByteArray);
			bytesmsg.buffer = reusableByteArray;
			bytesmsg.length = (ushort)buffer.BytesUsed;
			Send(bytesmsg, msgId, rcvGrp);
		}

		public static void Send(this byte[] buffer, int bitcount, short msgId, ReceiveGroup rcvGrp)
		{
			bytesmsg.buffer = buffer;
			bytesmsg.length = (ushort)((bitcount + 7) >> 3);
			Send(bytesmsg, msgId, rcvGrp);
		}

		public static void Send(this byte[] buffer, ushort bytecount, short msgId, ReceiveGroup rcvGrp)
		{
			bytesmsg.buffer = buffer;
			bytesmsg.length = bytecount;
			Send(bytesmsg, msgId, rcvGrp);
		}

		public static void Send(this byte[] buffer, short msgId, ReceiveGroup rcvGrp)
		{
			bytesmsg.buffer = buffer;
			bytesmsg.length = (ushort)buffer.Length;
			Send(bytesmsg, msgId, rcvGrp);
		}

		/// <summary>
		/// Sends byte[] to each client, making any needed per client alterations, such as changing the frame offset value in the first byte.
		/// </summary>
		public static void Send(BytesMessageNonalloc msg, short msgId, ReceiveGroup rcvGrp, int channel = Channels.DefaultUnreliable)
		{

			/// Server send to all. Owner client send to server.
			if (rcvGrp == ReceiveGroup.All)
			{
				if (NetworkServer.active)
					NetworkServer.SendToAll(msgId, msg);
			}
			else if (rcvGrp == ReceiveGroup.Master)
			{
				if (ClientScene.readyConnection != null)
#if MIRROR
					NetworkClient.Send(msgId, msg);
#else
					NetworkManager.singleton.client.Send(msgId, msg);
#endif
			}
			else
			{
				if (NetworkServer.active)
				{
#if MIRROR
					foreach (NetworkConnection nc in NetworkServer.connections.Values)
#else
					foreach (NetworkConnection nc in NetworkServer.connections)
#endif
					{
						if (nc == null)
							continue;

						/// Don't send to self if Host
						if (nc.connectionId == 0)
							continue;

						if (nc.isReady)
							nc.Send(msgId, msg);
					}
				}

				/// Client's cant send to all, so we will just send to server to make 'others' always work.
				else if (ClientScene.readyConnection != null)
				{
#if MIRROR
					NetworkClient.Send(msgId, msg);
#else
					NetworkManager.singleton.client.Send(msgId, msg);
#endif
				}
			}

			//nc.FlushChannels();
		}


#endif
	}

}
#pragma warning restore CS0618 // UNET obsolete
