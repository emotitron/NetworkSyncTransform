//Copyright 2018, Davin Carten, All rights reserved

#if PUN_2_OR_NEWER || MIRROR || !UNITY_2019_1_OR_NEWER

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using emotitron.NST;
using emotitron.Compression;
using emotitron.Debugging;

/// <summary>
/// Attaches pings to outgoing Master ticks on a regular interval. Clients respond on their next outgoing tick with a compressed float value
/// that indicates how much time passed locally between getting the ping request, and the sending of their ACK. The MasterRTT calculates the RTT for that client
/// by subtracting the returned time value from the time it took total to get an ACK.
/// </summary>
public class MasterRTT
{
	// These two constants need to add up to 1
	private const float OLD_SAMP_WEIGHT = .8f;
	private const float NEW_SAMP_WEIGHT = .2f;

	private static float svrPingInitiateTime;
	private static float clntPingArriveTime;
	private static bool clientNeedsToRespondToPing;

	private static Dictionary<int, float> RTT = new Dictionary<int, float>();

	public static float GetRTT(int clientId)
	{
		return (RTT.ContainsKey(clientId)) ? RTT[clientId] : 0;
	}
	public static float GetRTT(NetworkSyncTransform nst)
	{
		int id = nst.na.ClientId;
		return RTT.ContainsKey(id) ? RTT[id] : 0;
	}
	public static float GetRTT(NSTNetAdapter na)
	{
		int id = na.ClientId;
		return RTT.ContainsKey(id) ? RTT[id] : 0;
	}

	public static void Send(ref UdpBitStream bitstream, int updateCounter)
	{
		// If this is the server and it is due to ping... ping
		//TODO this hard coded 60 will likely go.
		//TODO this should probably be offset by 1 to keep frame zero from becoming massive with all the keyframes of NST
		if (MasterNetAdapter.ServerIsActive && updateCounter % 20 == 0 && updateCounter != 60)
		{
			svrPingInitiateTime = Time.time;
			bitstream.WriteBool(true);
		}

		// If this is a client, and it has rcvd a server ping, it needs to respond next tick.
		else if (clientNeedsToRespondToPing)
		{
			bitstream.WriteBool(true);
			// write how long the client waited to reply in ms (1023 ms max with 10 bits)
			bitstream.WriteInt((int)((Time.time - clntPingArriveTime) * 1000), 10);
			clientNeedsToRespondToPing = false;
		}

		// nothing happening this pass.
		else
		{
			bitstream.WriteBool(false);
		}
	}

	public static void Rcv(ref UdpBitStream bitstream, ref UdpBitStream outstream, bool mirror, int clientId)
	{
		bool isPing = bitstream.ReadBool();

		// if this is a ping from a client, a time delta should follow (mirror means this is the server, and this pass is the server pass)
		if (isPing && mirror && clientId != MasterNetAdapter.MasterClientId)
		{
			float clientHeldTime = .001f * bitstream.ReadInt(10);
			float rtt = (Time.time - svrPingInitiateTime) - clientHeldTime;

			if (RTT.ContainsKey(clientId))
				RTT[clientId] = RTT[clientId] * OLD_SAMP_WEIGHT + rtt * NEW_SAMP_WEIGHT;
			else
				RTT.Add(clientId, rtt);

			//Debug.Log(
			XDebug.Log(!XDebug.logInfo ? null : 
				"MasterRTT ping result " + clientId + " <b>" + RTT[clientId] + " </b>  plus " + clientHeldTime + " of client waiting for next outgoing tick.");
		}

		// this is a dumb client and it just got a ping from server
		else if (isPing && !MasterNetAdapter.ServerIsActive)
		{
			clientNeedsToRespondToPing = true;
			clntPingArriveTime = Time.time;
		}

		// We don't mirror out pings. The conversation is strictly Server > Player > Server
		if (mirror)
		{
			outstream.WriteBool(false);
		}
	}
}

#endif
