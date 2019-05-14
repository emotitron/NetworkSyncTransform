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
#elif !UNITY_2019_1_OR_NEWER
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
		public static byte[] reusableIncomingBuffer = new byte[4000];
		public static byte[] reusableOutgoingBuffer = new byte[4000];

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

		public static void Send(this byte[] buffer, int bitposition, GameObject refObj, ReceiveGroup rcvGrp)
		{
			int bytecount = (bitposition + 7) >> 3;
			//TODO replace this GC generating mess with something prealloc
			System.ArraySegment<byte> byteseg = new System.ArraySegment<byte>(buffer, 0, bytecount);

			PhotonNetwork.NetworkingClient.OpRaiseEvent(NetMsgCallbacks.DEF_MSG_ID, byteseg, opts[(int)rcvGrp], sendOpts);
			PhotonNetwork.NetworkingClient.Service();
		}

		//public static void Send(this byte[] buffer, ushort bytecount, byte msgId, ReceiveGroup rcvGrp)
		//{
		//	//TODO replace this GC generating mess with something prealloc
		//	System.ArraySegment<byte> byteseg = new System.ArraySegment<byte>(buffer, 0, bytecount);

		//	PhotonNetwork.NetworkingClient.OpRaiseEvent(msgId, byteseg, opts[(int)rcvGrp], sendOpts);
		//	PhotonNetwork.NetworkingClient.Service();
		//}

		//public static void Send(this byte[] buffer, int bitposition, byte msgId, ReceiveGroup rcvGrp)
		//{
		//	int bytecount = (bitposition + 7) >> 3;

		//	//TODO replace this GC generating mess with something prealloc
		//	System.ArraySegment<byte> byteseg = new System.ArraySegment<byte>(buffer, 0, bytecount);

		//	PhotonNetwork.NetworkingClient.OpRaiseEvent(msgId, byteseg, opts[(int)rcvGrp], sendOpts);
		//	PhotonNetwork.NetworkingClient.Service();
		//}

		//public static void Send(this byte[] buffer, byte msgId, ReceiveGroup rcvGrp)
		//{
		//	//TODO replace this GC generating mess with something prealloc
		//	int bytecount = buffer.Length;
		//	byte[] streambytes = new byte[bytecount];
		//	Array.Copy(buffer, streambytes, bytecount);
		//	PhotonNetwork.NetworkingClient.OpRaiseEvent(msgId, streambytes, opts[(int)rcvGrp], sendOpts);
		//	PhotonNetwork.NetworkingClient.Service();
		//}
#elif MIRROR || !UNITY_2019_1_OR_NEWER

		public static bool ReadyToSend { get { return NetworkServer.active || ClientScene.readyConnection != null; } }
		public static bool AmActiveServer { get { return NetworkServer.active; } }

		//static readonly NetworkWriter reusableunetwriter = new NetworkWriter();
		public static readonly BytesMessageNonalloc bytesmsg = new BytesMessageNonalloc();

		//[System.Obsolete]
		//public static void Send(ref Bitstream buffer, short msgId, ReceiveGroup rcvGrp)
		//{
		//	BitstreamExtensions.ReadOut(ref buffer, reusableOutgoingBuffer);
		//	BytesMessageNonalloc.buffer = reusableOutgoingBuffer;
		//	BytesMessageNonalloc.length = (ushort)buffer.BytesUsed;
		//	Send(null, bytesmsg, msgId, rcvGrp);
		//}

		public static void Send(this byte[] buffer, int bitcount, GameObject refObj, ReceiveGroup rcvGrp)
		{
			BytesMessageNonalloc.outgoingbuffer = buffer;
			BytesMessageNonalloc.length = (ushort)((bitcount + 7) >> 3);
			Send(refObj, bytesmsg, NetMsgCallbacks.DEF_MSG_ID, rcvGrp);
		}

		//[System.Obsolete()]
		//public static void Send(this byte[] buffer, ReceiveGroup rcvGrp)
		//{
		//	BytesMessageNonalloc.buffer = buffer;
		//	BytesMessageNonalloc.length = (ushort)buffer.Length;
		//	Send(null, bytesmsg, NetMsgCallbacks.DEF_MSG_ID, rcvGrp);
		//}

		//[System.Obsolete()]
		//public static void Send(this byte[] buffer, int bitcount, short msgId, ReceiveGroup rcvGrp)
		//{
		//	BytesMessageNonalloc.buffer = buffer;
		//	BytesMessageNonalloc.length = (ushort)((bitcount + 7) >> 3);
		//	Send(null, bytesmsg, msgId, rcvGrp);
		//}

		//[System.Obsolete()]
		//public static void Send(this byte[] buffer, ushort bytecount, short msgId, ReceiveGroup rcvGrp)
		//{
		//	BytesMessageNonalloc.buffer = buffer;
		//	BytesMessageNonalloc.length = bytecount;
		//	Send(null, bytesmsg, msgId, rcvGrp);
		//}



		//[System.Obsolete()]
		//public static void Send(this byte[] buffer, short msgId, ReceiveGroup rcvGrp)
		//{
		//	BytesMessageNonalloc.buffer = buffer;
		//	BytesMessageNonalloc.length = (ushort)buffer.Length;
		//	Send(null, bytesmsg, msgId, rcvGrp);
		//}

		/// <summary>
		/// Sends byte[] to each client, making any needed per client alterations, such as changing the frame offset value in the first byte.
		/// </summary>
		public static void Send(GameObject refObj, BytesMessageNonalloc msg, short msgId, ReceiveGroup rcvGrp, int channel = Channels.DefaultUnreliable)
		{
			/// Server send to all. Owner client send to server.
			if (rcvGrp == ReceiveGroup.All)
			{
				if (NetworkServer.active)
#if MIRROR
					NetworkServer.SendToAll<BytesMessageNonalloc>(msg, channel);
#else
					NetworkServer.SendByChannelToReady(refObj, msgId, msg, channel);
#endif
			}
			else if (rcvGrp == ReceiveGroup.Master)
			{
				var conn = ClientScene.readyConnection;
				if (conn != null)
				{
#if MIRROR
					conn.Send<BytesMessageNonalloc>(msg, channel);
#else
					conn.SendByChannel(msgId, msg, channel);
					conn.FlushChannels();
#endif
				}

			}
			/// Send To Others
			else
			{
				if (NetworkServer.active)
				{
#if MIRROR
					foreach (NetworkConnection conn in NetworkServer.connections.Values)
#else
					foreach (NetworkConnection conn in NetworkServer.connections)
#endif
					{
						if (conn == null)
							continue;

						/// Don't send to self if Host
						if (conn.connectionId == 0)
							continue;


#if MIRROR
						if (conn.isReady && (!refObj || refObj.GetComponent<NetworkIdentity>().observers.ContainsKey(conn.connectionId)))
						{
							conn.Send<BytesMessageNonalloc>(msg, channel);
						}
#else
						if (conn.isReady && (!refObj || refObj.GetComponent<NetworkIdentity>().observers.Contains(conn)))
						{
							conn.SendByChannel(msgId, msg, channel);
							conn.FlushChannels();
						}
#endif
					}
				}

				/// Client's cant send to all, so we will just send to server to make 'others' always work.
				else if (ClientScene.readyConnection != null)
				{
#if MIRROR
					ClientScene.readyConnection.Send<BytesMessageNonalloc>(msg, channel);
#else
					ClientScene.readyConnection.SendByChannel(msgId, msg, channel);
					ClientScene.readyConnection.FlushChannels();

#endif
				}
			}

		}

		//		public static void SendToConn(this byte[] buffer, int bitcount, short msgId, object targConn, int targConnId, ReceiveGroup rcvGrp, int channel = Channels.DefaultUnreliable)
		//		{
		//			bytesmsg.buffer = buffer;
		//			bytesmsg.length = (ushort)((bitcount + 7) >> 3);
		//			SendToConn(bytesmsg, msgId, targConn, targConnId, rcvGrp, channel);
		//		}

		//		public static void SendToConn(BytesMessageNonalloc msg, short msgId, object targConn, int targConnId, ReceiveGroup rcvGrp, int channel = Channels.DefaultUnreliable)
		//		{
		//			{
		//				if (NetworkServer.active)
		//				{
		//					NetworkConnection conn = (targConn as NetworkConnection);

		//					if (conn == null)
		//						return;

		//					if (conn.isReady)
		//					{
		//#if MIRROR
		//						conn.Send<BytesMessageNonalloc>(msg, channel);
		//#else
		//						conn.SendByChannel(msgId, msg, channel);
		//						conn.FlushChannels();
		//#endif
		//					}
		//				}
		//			}
		//		}


#endif
	}

}
#pragma warning restore CS0618 // UNET obsolete
