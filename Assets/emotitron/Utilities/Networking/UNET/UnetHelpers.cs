////Copyright 2018, Davin Carten, All rights reserved

//using emotitron.Compression;

//#if MIRROR
//using Mirror;
//#elif !UNITY_2019_1_OR_NEWER
//using UnityEngine.Networking;
//#endif

//namespace emotitron.Utilities.Networking.UNET
//{
//#pragma warning disable CS0618 // UNET is obsolete

//	/// <summary>
//	/// Unity Unet specific stuff tucked away, to not litter up the sample code with UNET specific stuff.
//	/// </summary>
//	public static class UnetHelpers
//	{
//		/// The arbitary message ids used by UNET
//		private const short CLIENT_TO_SVR = 193;
//		private const short SVR_TO_CLIENT = 194;


//		/// <summary>
//		/// Register the Server and Client message handlers
//		/// </summary>
//		/// <param name="OnSvrRcv"></param>
//		/// <param name="OnClientsRcv"></param>
//		public static void RegisterHandlers(NetworkMessageDelegate OnSvrRcv, NetworkMessageDelegate OnClientsRcv)
//		{
//			/// Register the Static handler method for incoming UNET messages.
//			{
//				if (OnSvrRcv != null && NetworkServer.active && !NetworkServer.handlers.ContainsKey(CLIENT_TO_SVR))
//				{
//					NetworkServer.RegisterHandler(CLIENT_TO_SVR, OnSvrRcv);
//				}

//#if MIRROR
//				else if (OnClientsRcv != null && NetworkClient.active && !NetworkClient.handlers.ContainsKey(SVR_TO_CLIENT))
//				{
//					NetworkClient.RegisterHandler(SVR_TO_CLIENT, OnClientsRcv);
//				}
//#elif !UNITY_2019_1_OR_NEWER
//				else if (OnClientsRcv != null && NetworkClient.active && !NetworkManager.singleton.client.handlers.ContainsKey(SVR_TO_CLIENT))
//				{
//					NetworkManager.singleton.client.RegisterHandler(SVR_TO_CLIENT, OnClientsRcv);
//				}
//#endif
//			}
//		}

//		//#if !MIRROR && !PUN_2_OR_NEWER

//		//		static readonly NetworkWriter unetwriter = new NetworkWriter();

//		//		/// <summary>
//		//		/// Write the contents of a bitstream out to a UNET NetworkWriter and send.
//		//		/// </summary>
//		//		public static void SendWithUnetWriter(ref Bitstream bitstream)
//		//		{
//		//			/// Create the UNET message - you would use the serializer for your network library here instead
//		//			unetwriter.StartMessage(NetworkServer.active ? SVR_TO_CLIENT : CLIENT_TO_SVR);

//		//			/// There is a built in overload for working with UNET
//		//			unetwriter.Write(ref bitstream);

//		//			/// For other libraries, you can just do a byte by byte copy from our bitstream to the outgoing byte[]
//		//			//for (int i = 0; i < bitstream.BytesUsed; ++i)
//		//			//	unetwriter.Write(bitstream.ReadByte());

//		//			unetwriter.FinishMessage();

//		//			/// Server send to all. Owner client send to server.
//		//			if (NetworkServer.active)
//		//				NetworkServer.SendWriterToReady(null, unetwriter, Channels.DefaultUnreliable);
//		//			else
//		//				ClientScene.readyConnection.SendWriter(unetwriter, Channels.DefaultUnreliable);
//		//			//NetworkManager.singleton.client.SendWriter(unetwriter, Channels.DefaultUnreliable);
//		//		}

//		//		/// <summary>
//		//		/// Write the contents of a byte[] out to a UNET NetworkWriter and send.
//		//		/// </summary>
//		//		public static void SendWithUnetWriter(byte[] buffer, int bitcount)
//		//		{
//		//			/// Create the UNET message - you would use the serializer for your network library here instead
//		//			unetwriter.StartMessage(NetworkServer.active ? SVR_TO_CLIENT : CLIENT_TO_SVR);

//		//			int bytecount = (bitcount >> 3) + ((bitcount % 8 != 0) ? 1 : 0);
//		//			/// There is a built in overload for working with UNET
//		//			unetwriter.Write(buffer, bytecount);

//		//			/// For other libraries, you can just do a byte by byte copy from our bitstream to the outgoing byte[]
//		//			//for (int i = 0; i < bitstream.BytesUsed; ++i)
//		//			//	unetwriter.Write(bitstream.ReadByte());

//		//			unetwriter.FinishMessage();

//		//			/// Server send to all. Owner client send to server.
//		//			if (NetworkServer.active)
//		//				NetworkServer.SendWriterToReady(null, unetwriter, Channels.DefaultUnreliable);
//		//			else
//		//				ClientScene.readyConnection.SendWriter(unetwriter, Channels.DefaultUnreliable);
//		//			//NetworkManager.singleton.client.SendWriter(unetwriter, Channels.DefaultUnreliable);
//		//		}

//		//		/// <summary>
//		//		/// Copy the byte[] of NetworkMessage into your bitstream.
//		//		/// </summary>
//		//		/// <param name="netMsg"></param>
//		//		/// <param name="bitstream"></param>
//		//		public static void NetworkMessageToBitstream(NetworkMessage netMsg, ref Bitstream bitstream)
//		//		{
//		//			bitstream.Reset();

//		//			for (uint i = netMsg.reader.Position; i < netMsg.reader.Length; ++i)
//		//				bitstream.WriteByte(netMsg.reader.ReadByte());
//		//		}

//		//#endif

//	}
//#pragma warning restore CS0618 // UNET is obsolete

//}
