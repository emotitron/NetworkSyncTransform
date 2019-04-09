//Copyright 2018, Davin Carten, All rights reserved

using System.Collections.Generic;
using UnityEngine;
using emotitron.Debugging;
using emotitron.Compression;

#if PUN_2_OR_NEWER
using Photon.Pun;
using ExitGames.Client.Photon;
#endif

//#if MIRROR
//using Mirror;
//#else
using UnityEngine.Networking;
//#endif

#pragma warning disable CS0618 // UNET obsolete

/// <summary>
/// Generic handelers for routing incoming network messages as byte[] arrays to registered handlers. This abstracts the various netlibs into a
/// standard byte[] format.
/// </summary>
namespace emotitron.Utilities.Networking
{
	/// <summary>
	///  Nonalloc message for Mirror, since we can't directly send writers with Mirror.
	/// </summary>
	public class BytesMessageNonalloc : MessageBase
	{
		public byte[] buffer;
		public ushort length;

		public BytesMessageNonalloc() { }

		public BytesMessageNonalloc(byte[] nonalloc)
		{
			this.buffer = nonalloc;
		}

		public BytesMessageNonalloc(byte[] nonalloc, ushort length)
		{
			this.buffer = nonalloc;
			this.length = length;
		}

		public override void Serialize(NetworkWriter writer)
		{
			writer.Write(length);
			for (int i = 0; i < length; i++)
				writer.Write(buffer[i]);
		}

		public override void Deserialize(NetworkReader reader)
		{
			length = reader.ReadUInt16();
			for (int i = 0; i < length; i++)
				buffer[i] = reader.ReadByte();
		}
	}

	public static class NetMsgCallbacks
	{
		public delegate void ByteBufferCallback(byte[] buffer);
		//public delegate void BitstreamCallback(ref Bitstream bitstream);

		private static Dictionary<int, CallbackLists> callbacks = new Dictionary<int, CallbackLists>();

		private class CallbackLists
		{
			public List<ByteBufferCallback> bufferCallbacks;
			//public List<BitstreamCallback> bitstreamCallbacks;
		}

		//private static Bitstream reusableBitstream = new Bitstream();

#if PUN_2_OR_NEWER

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
		private static void RegisterOnEventListener()
		{
			PhotonNetwork.NetworkingClient.EventReceived += OnEvent;
		}

		/// <summary>
		/// Capture incoming Photon messages here.
		/// </summary>
		public static void OnEvent(EventData photonEvent)
		{
			byte msgId = photonEvent.Code;

			if (!callbacks.ContainsKey(msgId))
				return;

			// ignore messages from self.
			if (PhotonNetwork.IsMasterClient && PhotonNetwork.MasterClient.ActorNumber == photonEvent.Sender)
			{
				XDebug.Log("Master Client talking to self? Normal occurance for a few seconds after Master leaves the game and a new master is selected.");
				return;
			}

			byte[] buffer = (photonEvent.CustomData as byte[]);

			var cbs = callbacks[msgId];
			if (cbs.bufferCallbacks != null && cbs.bufferCallbacks.Count > 0)
			{
				foreach (var cb in cbs.bufferCallbacks)
					cb(buffer);
			}

		}

		#region Handler Registration

		public static void RegisterHandler(byte msgid, ByteBufferCallback callback, bool asServer)
		{
			if (!callbacks.ContainsKey(msgid))
				callbacks.Add(msgid, new CallbackLists());

			if (callbacks[msgid].bufferCallbacks == null)
				callbacks[msgid].bufferCallbacks = new List<ByteBufferCallback>();

			var cbs = callbacks[msgid].bufferCallbacks;

			if (!cbs.Contains(callback))
				cbs.Add(callback);
		}
		//public static void RegisterHandler(byte msgid, BitstreamCallback callback, bool asServer)
		//{
		//	if (!callbacks.ContainsKey(msgid))
		//		callbacks.Add(msgid, new CallbackLists());

		//	if (callbacks[msgid].bitstreamCallbacks == null)
		//		callbacks[msgid].bitstreamCallbacks = new List<BitstreamCallback>();

		//	var cbs = callbacks[msgid].bitstreamCallbacks;

		//	if (!cbs.Contains(callback))
		//		cbs.Add(callback);
		//}

		public static void UnregisterHandler(byte msgid, ByteBufferCallback callback, bool asServer)
		{
			if (callbacks.ContainsKey(msgid))
			{
				var cbs = callbacks[msgid];
				cbs.bufferCallbacks.Remove(callback);

				if (cbs.bufferCallbacks.Count == 0 )
					callbacks.Remove(msgid);
			}
		}

		#endregion

#else


		private static readonly byte[] reusablebuffer = new byte[1024];

		private static bool RegisterMessageId(short msgId, bool asServer)
		{
			/// Make sure network is active, or registering handlers will fail, or they will just be forgotten
			if (asServer)
			{
				if (NetworkServer.active)
					if (!NetworkServer.handlers.ContainsKey(msgId))
						NetworkServer.RegisterHandler(msgId, OnMessage);

					else
						return false;

			}
			else
			{
				if (NetworkClient.active)
					if (!NetworkManager.singleton.client.handlers.ContainsKey(msgId))
						NetworkManager.singleton.client.RegisterHandler(msgId, OnMessage);

					else
						return false;
			}

			if (!callbacks.ContainsKey(msgId))
				callbacks.Add(msgId, new CallbackLists());

			return true;
		}

		public static void RegisterHandler(short msgId, ByteBufferCallback callback, bool asServer)
		{
			if (callback == null)
				return;

			if (!RegisterMessageId(msgId, asServer))
				return;

			/// make a new list if this is the first item
			if (callbacks[msgId].bufferCallbacks == null)
				callbacks[msgId].bufferCallbacks = new List<ByteBufferCallback>();

			/// don't register the same callback twice
			if (callbacks[msgId].bufferCallbacks.Contains(callback))
				return;

			callbacks[msgId].bufferCallbacks.Add(callback);
		}

		//public static void RegisterHandler(short msgId, BitstreamCallback callback, bool asServer)
		//{
		//	if (callback == null)
		//		return;

		//	if (!RegisterMessageId(msgId, asServer))
		//		return;

		//	/// make a new list if this is the first item
		//	if (callbacks[msgId].bitstreamCallbacks == null)
		//		callbacks[msgId].bitstreamCallbacks = new List<BitstreamCallback>();

		//	/// don't register the same callback twice
		//	if (callbacks[msgId].bitstreamCallbacks.Contains(callback))
		//		return;

		//	callbacks[msgId].bitstreamCallbacks.Add(callback);
		//}

		private static void UnregisterMessageId(int msgId, bool asServer)
		{
			if (asServer)
				NetworkServer.UnregisterHandler((short)msgId);
			else
				if (NetworkManager.singleton.client != null)
				NetworkManager.singleton.client.UnregisterHandler((short)msgId);
		}

		public static void UnregisterHandler(short msgId, ByteBufferCallback callback, bool asServer)
		{
			if (!callbacks.ContainsKey(msgId) || callbacks[msgId].bufferCallbacks == null)
				return;

			var cbs = callbacks[msgId];

			// Remove callback method from list for this msgid
			cbs.bufferCallbacks.Remove(callback);

			/// Remove the dictionary entry entirely if we no longer have any callbacks
			if (cbs.bufferCallbacks.Count == 0/* && (cbs.bitstreamCallbacks == null || cbs.bitstreamCallbacks.Count == 0)*/)
			{
				UnregisterMessageId(msgId, asServer);
				callbacks.Remove(msgId);
			}
		}
		//public static void UnregisterHandler(short msgId, BitstreamCallback callback, bool asServer)
		//{
		//	if (!callbacks.ContainsKey(msgId) || callbacks[msgId].bitstreamCallbacks == null)
		//		return;

		//	var cbs = callbacks[msgId];

		//	// Remove callback method from list for this msgid
		//	cbs.bitstreamCallbacks.Remove(callback);

		//	/// Remove the dictionary entry entirely if we no longer have any callbacks
		//	if (cbs.bitstreamCallbacks.Count == 0 && (cbs.bufferCallbacks == null || cbs.bufferCallbacks.Count == 0))
		//	{
		//		Debug.Log("Unregister " + cbs.bitstreamCallbacks.Count);
		//		UnregisterMessageId(msgId, asServer);
		//		callbacks.Remove(msgId);
		//	}
		//}

		/// <summary>
		/// All of our registered UNET msgId msgs get routed this method, which reads them into a byte[] form before passing them to the callbacks
		/// </summary>
		/// <param name="msg"></param>
		public static void OnMessage(NetworkMessage msg)
		{

			var bmsg = NetMsgSends.bytesmsg;
			bmsg.buffer = reusablebuffer;

			bmsg.Deserialize(msg.reader);

			var msgId = msg.msgType;

			if (!callbacks.ContainsKey(msgId))
				return;

			var cbs = callbacks[msgId];


			/// Send to all byte[] buffer callbacks
			var bufferCBList = cbs.bufferCallbacks;

			if (bufferCBList != null)
			{
				int cnt = bufferCBList.Count;
				for (int i = 0; i < cnt; ++i)
					bufferCBList[i](reusablebuffer);
			}


			///// Send to all Bitstream callbacks
			//var bitstreamCBList = cbs.bitstreamCallbacks;

			//if (bitstreamCBList != null)
			//{
			//	/// copy buffer into bitstream
			//	reusableBitstream.Reset();
			//	//for (int i = 0; i < bmsg.length; ++i)
			//	//	reusableBitstream.WriteByte(reusablebuffer[i]);
			//	reusableBitstream.WriteFromByteBuffer(bmsg.buffer, bmsg.length * 8);

			//	for (int i = 0; i < bitstreamCBList.Count; ++i)
			//		bitstreamCBList[i](ref reusableBitstream);
			//}

		}
#endif

	}
}
#pragma warning restore CS0618 // UNET obsolete
