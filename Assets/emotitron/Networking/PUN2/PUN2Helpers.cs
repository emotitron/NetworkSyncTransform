////Copyright 2018, Davin Carten, All rights reserved

//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using Photon;
//using Photon.Pun;
//using Photon.Realtime;
//using ExitGames.Client.Photon;
//using emotitron.Debugging;
//using System;

//namespace emotitron.Networking
//{
//	public static class PUN2Helpers
//	{


//		//public static Dictionary<byte, List<Action<byte[]>>> onEventCallbacks = new Dictionary<byte, List<Action<byte[]>>>();

//		//[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
//		//public static void RegisterOnEventListener()
//		//{
//		//	Debug.Log("Registered generic PUN2 event listener");
//		//	PhotonNetwork.NetworkingClient.EventReceived += OnEvent;
//		//}

//		//public static void RegisterOnEventCallbacks(byte msgid, Action<byte[]> onEvent)
//		//{
//		//	if (!onEventCallbacks.ContainsKey(msgid))
//		//		onEventCallbacks.Add(msgid, new List<Action<byte[]>>());

//		//	onEventCallbacks[msgid].Add(onEvent);
//		//}
//		//public static void UnregisterOnEventCallback(byte msgid, Action<byte[]> onEvent)
//		//{
//		//	if (onEventCallbacks.ContainsKey(msgid))
//		//		onEventCallbacks[msgid].Remove(onEvent);
//		//}

//		///// <summary>
//		///// Capture incoming Photon messages here.
//		///// </summary>
//		//public static void OnEvent(EventData photonEvent)
//		//{
			
//		//	if (!onEventCallbacks.ContainsKey(photonEvent.Code))
//		//		return;

//		//	// ignore messages from self.
//		//	if (PhotonNetwork.IsMasterClient && PhotonNetwork.MasterClient.ActorNumber == photonEvent.Sender)
//		//	{
//		//		XDebug.Log("Master Client talking to self? Normal occurance for a few seconds after Master leaves the game and a new master is selected.");
//		//		return;
//		//	}


//		//	byte[] buffer = (photonEvent.CustomData as byte[]);

//		//	var cbs = onEventCallbacks[photonEvent.Code];
//		//	foreach (var cb in cbs)
//		//		cb.Invoke(buffer);
//		//}

//		//public enum ReceiveGroup {  Others, All, Master }
		
//		//private static RaiseEventOptions[] opts = new RaiseEventOptions[3]
//		//{
//		//	new RaiseEventOptions() { Receivers = ReceiverGroup.Others },
//		//	new RaiseEventOptions() { Receivers = ReceiverGroup.All },
//		//	new RaiseEventOptions() { Receivers = ReceiverGroup.MasterClient }
//		//};
		
//		//private static SendOptions sendOpts = new SendOptions();

//		//public static void SendUpdate(byte[] buffer, int bytecount, byte msgId, ReceiveGroup rcvGrp)
//		//{
//		//	//TODO replace this GC generating mess with something prealloc
//		//	byte[] streambytes = new byte[bytecount];
//		//	Array.Copy(buffer, streambytes, bytecount);

//		//	//byte msgid = PhotonNetwork.IsMasterClient ? MASTER_SND_ID : CLIENT_SND_ID;
//		//	PhotonNetwork.NetworkingClient.OpRaiseEvent(msgId, streambytes, opts[(int)rcvGrp], sendOpts);
//		//	PhotonNetwork.NetworkingClient.Service();

//		//	//// MasterClient send to self - may are may not need this in the future.
//		//	//if (PhotonNetwork.IsMasterClient)
//		//	//	NSTMaster.ReceiveUpdate(ref bitstream, ref outstream, false, PhotonNetwork.MasterClient.ActorNumber);
//		//}

//		//public static void SendUpdate(byte[] buffer, byte msgId, ReceiverGroup rcvGrp)
//		//{
//		//	//TODO replace this GC generating mess with something prealloc
//		//	int bytecount = buffer.Length;
//		//	byte[] streambytes = new byte[bytecount];
//		//	Array.Copy(buffer, streambytes, bytecount);
//		//	//byte msgid = PhotonNetwork.IsMasterClient ? MASTER_SND_ID : CLIENT_SND_ID;
//		//	PhotonNetwork.NetworkingClient.OpRaiseEvent(msgId, streambytes, opts[(int)rcvGrp], sendOpts);
//		//	PhotonNetwork.NetworkingClient.Service();


//		//	//// MasterClient send to self - may are may not need this in the future.
//		//	//if (PhotonNetwork.IsMasterClient)
//		//	//	NSTMaster.ReceiveUpdate(ref bitstream, ref outstream, false, PhotonNetwork.MasterClient.ActorNumber);
//		//}

//	}
//}

