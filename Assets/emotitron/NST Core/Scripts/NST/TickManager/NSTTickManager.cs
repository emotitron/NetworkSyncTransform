using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if PUN_2_OR_NEWER || MIRROR || !UNITY_2019_1_OR_NEWER

namespace emotitron.NST
{

	public static class NSTTickManager
	{
		/// Can make this variable later. How many frames of buffer to keep (above the bare min required to work)
		public const int FRAME_BUFFER_CNT = 3;

		public static List<int> activePlayerIds = new List<int>();
		public static Dictionary<int, ulong> playerValidMask = new Dictionary<int, ulong>(); // <playerid, mask>
		public static Dictionary<int, int> playerSrcToLclFrameOffset = new Dictionary<int, int>(); // <playerid, offsetnumber>
		public static Dictionary<int, int[]> clientFrameMap = new Dictionary<int, int[]>();

		public static int localToServerFrameOffset; // <playerid, offsetnumber>

		//public static void SetFrameValid(int playerid, int frameid)
		//{
		//	///// if this player is communicating for the first time, get the delta between its frameid and the servers framecounter to get our counter offset
		//	//if (!playerValidMask.ContainsKey(playerid))
		//	//	AddPlayer(playerid, frameid);

		//	playerValidMask[playerid] |= ((uint)1 << frameid);
		//}

		//public static void SetFrameInvalid(int playerid, int frameid)
		//{
		//	playerValidMask[playerid] &= ~((uint)1 << frameid);
		//}

		public static void SetFrameInvalidForAllPlayers(int frameid)
		{
			Debug.Log("Set frame invalid for all " + frameid);
			ulong mask = ~((ulong)1 << frameid);

			int cnt = activePlayerIds.Count;
			for (int i = 0; i < cnt; ++i)
				playerValidMask[activePlayerIds[i]] &= mask;

			//Debug.Log(frameid + " set false"); // + Utilities.BitUtilities.BitTools.PrintBitMask(NSTTickManager.playerValidMask[senderId]));

		}

		public static bool IsFrameValid(int connIdx, int frameid)
		{
			Debug.Log("Check frame valid for player " + connIdx + " fid: " + frameid);
			///// -1 is self - for now always make this true.
			//if (playerid == MasterNetAdapter.MyClientId)
			//	return true;

			if (!playerValidMask.ContainsKey(connIdx))
				return false;

			return (playerValidMask[connIdx] & ((ulong)1 << frameid)) != 0;
		}

		public static Dictionary<int, float> avgOffset = new Dictionary<int, float>();

		public static int SetClientFrameMap(int clientId, int serverframeid, int sourceframeid, int localizedframeid)
		{
			Debug.Log("<b>TickManager SetClientFrameMap</b> " + clientId);

			/// Store how a clients update frame id maps to the servers frame ids
			if (!clientFrameMap.ContainsKey(clientId))
				clientFrameMap.Add(clientId, new int[60]);

			clientFrameMap[clientId][localizedframeid] = sourceframeid;
			return localizedframeid;
			//Debug.Log("TM " + playerid + "  svrLcl: " + serverframeid + "  src: " + sourceframeid);

			//int currentLocalFrameId = NSTMaster._frameCounter;
			//int mapped = (currentLocalFrameId + FRAME_BUFFER_CNT);

			//if (mapped > 60)
			//	mapped -= 60;

			//clientFrameMap[playerid][mapped] = sourceframeid;
			//return mapped;
		}

		public static void SetFrameOffset(int clientId, int serverframeid, int sourceFrameId, bool asServer, bool isServerOwned)
		{
			Debug.Log("<b>TickManager SetFrameOffset</b> " + clientId + "  " + asServer + " " + isServerOwned);
			/// TODO: make this a bit more adaptive over time and get an average or mean delta
			/// TODO: may need to store the offset for each frame so changes in offset don't apply to all frames

			///// TEST
			//if (playerSrcToLclFrameOffset.ContainsKey(playerid))
			//	return;

			int currentLocalFrameId = NSTMaster.FrameCounter;

			int sourceToLocalOffset;

			if (asServer)
			{
				///// Store how a clients update frame id maps to the servers frame ids
				//if (!clientFrameMap.ContainsKey(playerid))
				//	clientFrameMap.Add(playerid, new int[60]);

				//Debug.Log("TM " + playerid + "  svrLcl: " + localizedFrameId + "  src: " + sourceFrameId);
				//clientFrameMap[playerid][localizedFrameId] = sourceFrameId;

				if (isServerOwned)
				{
					sourceToLocalOffset = 0;
				}
				else
				{
					//offset = (ownerFrameId - (currentLocalFrameId + FRAME_BUFFER_CNT));
					sourceToLocalOffset = (currentLocalFrameId + FRAME_BUFFER_CNT) - sourceFrameId;
					//Debug.LogWarning("SVR OFFSET = " + sourceToLocalOffset + " srcFid: " + sourceFrameId + " svrFid " + serverframeid + " currFid: " + currentLocalFrameId);

				}

			}
			else
			{
				if (isServerOwned)
				{
					/// Server processing its own client
					if (MasterNetAdapter.ServerIsActive)
					{
						//localToServerFrameOffset = 0;
						sourceToLocalOffset = 0;
					}
					/// Client processing server owned object
					else
					{
						sourceToLocalOffset = (currentLocalFrameId + FRAME_BUFFER_CNT) - serverframeid;
						//Debug.LogWarning("SVROWNED OFFSET = " + sourceToLocalOffset + " svrFrm " + serverframeid + " currFr: " + currentLocalFrameId);
						//sourceToLocalOffset = (currentLocalFrameId + FRAME_BUFFER_CNT) - sourceFrameId;
						//localToServerFrameOffset = serverframeid currentLocalFrameId
					}

				}
				/// Client processing other client object
				else
				{
					sourceToLocalOffset = (currentLocalFrameId /*+ FRAME_BUFFER_CNT*/) - (serverframeid + FRAME_BUFFER_CNT);
					//Debug.LogWarning("OFFSET = " + sourceToLocalOffset + " svrFrm " + serverframeid + " currFr: " + currentLocalFrameId);
				}
			}

			/// Put offset into range of 0-59
			if (sourceToLocalOffset >= 60)
				sourceToLocalOffset -= 60;
			else
				while (sourceToLocalOffset < 0)
					sourceToLocalOffset += 60;

			//else if (offset > 59)
			//	offset -= 60;

			/// Looks like this client connection hasn't been used yet. Set it up now. NOTE: Once created these
			/// currently never get destroyed due to UNETs lack of networking callbacks outside of NetworkManager.
			if (!playerSrcToLclFrameOffset.ContainsKey(clientId))
			{
				//avgOffset.Add(playerid, sourceToLocalOffset);
				//playerSrcToLclFrameOffset.Add(playerid, sourceToLocalOffset);
				//playerValidMask.Add(playerid, 0);
				AddPlayer(clientId, serverframeid, sourceFrameId, sourceToLocalOffset);
			}
			else
			{
				avgOffset[clientId] = (avgOffset[clientId] * 10f + sourceToLocalOffset) / 11f;

				//if (!isServerOwned)
				//{
				//	Debug.Log(playerid + " Offset average " + avgOffset[playerid] + "   " + sourceToLocalOffset + "  stored: " + playerSrcToLclFrameOffset[playerid]);
				//	Debug.Log(Time.time + " Applied offset? " + ((NSTMaster._frameCounter - (sourceFrameId + playerSrcToLclFrameOffset[playerid])) % 60));
				//}

				//playerSrcToLclFrameOffset[playerid] = sourceToLocalOffset;
			}

			//if (!isServerOwned)
			//	Debug.Log(Time.time + "  TM set - pid: " + playerid + " isSvrOwnd: <b>" + isServerOwned + "</b> sourceFid: " + " " + sourceFrameId + " svrFid: " + serverframeid + "<b> lcFid " + currentLocalFrameId + " off= " + sourceToLocalOffset + "</b> isntServer: " + (!asServer & !MasterNetAdapter.ServerIsActive));

		}


		public static void AddPlayer(int clientId, int serverframeid, int sourceframeid, int sourceToLocalOffset)
		{
			Debug.Log("<b>TickManager AddPlayer </b>" + clientId);
			//int currentLocalFrame = NSTMaster._frameCounter;

			activePlayerIds.Add(clientId);
			playerValidMask.Add(clientId, 0);
			playerSrcToLclFrameOffset.Add(clientId, sourceToLocalOffset);
			avgOffset.Add(clientId, sourceToLocalOffset);

			///// Make sure the offset is positive
			//int adjfirstframeid = (serverframeid < currentLocalFrame) ? serverframeid + 60 : serverframeid;

			//int offset = ((adjfirstframeid - currentLocalFrame) - FRAME_BUFFER_CNT);
			//if (offset < 0)
			//	offset += 60;

			//Debug.Log("<b>Player " + playerid + " added</b> with offset of " + offset + " c: " + serverframeid + " s: " + currentLocalFrame);

			//playerFrameOffset.Add(playerid, offset);
		}

		/// <summary>
		/// This removes a clientId from the TickManager. NOTE: Right now there is no way for me to call this, since
		/// there are no network callbacks in UNET. This belongs in the NetworkManager, but I can't touch that without
		/// interfering with the workflow of developers, so currently clientIds are never purged when players leave.
		/// </summary>
		public static void RemovePlayer(int playerid)
		{
			activePlayerIds.Remove(activePlayerIds.IndexOf(playerid));
			playerValidMask.Remove(playerid);
			playerSrcToLclFrameOffset.Remove(playerid);
		}

		/// <summary>
		/// Counts the number of frames. Any missing frames between the currentframe and currentframe+maxBuffer are included in the count (as they likely just haven't arrived)
		/// </summary>
		/// <param name="playerid"></param>
		/// <param name="currentframe">The frame currently being interpolated/used for this player.</param>
		/// <param name="maxBuffer">Can be as large as 30. The smaller this number the fewer iterations. Set to the greatest realistic number of buffered frames there will be and no more.</param>
		/// <returns></returns>
		public static int GetNumberOfValidFrames(int playerid, int currentframe, int maxBuffer)
		{
			int cnt = 0;
			/// shift to move the next frame to pos 0
			ulong mask = playerValidMask[playerid] >> currentframe;

			for (int i = 0; i < maxBuffer; ++i)
			{
				if (mask << i != 0)
					cnt++;
				else
					return cnt;
			}
			return cnt;
		}
	}


}

#endif