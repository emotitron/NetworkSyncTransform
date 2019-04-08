//Copyright 2018, Davin Carten, All rights reserved

using System.Diagnostics;
using UnityEngine;
using emotitron.Compression;

namespace emotitron.NST
{
	public enum BandwidthLogType { MasterIn, MasterOut, UpdateSend, UpdateRcv, UpdateMirror }

	/// <summary>
	/// Only used in Editor mode - this class collects and reports the size of NST Updates. May later change this from conditional
	/// to a #if UNITY_EDITOR, but for now this is much cleaner.
	/// </summary>
	public static class BandwidthUsage
	{
		static bool enabled;
		static BandwidthLogType logType;
		static uint nstid;
		static string summary;
		static string objectName;
		static int lastPtr;
		static int startPtr;

		static float masterInTotalBits, masterOutTotalBits, masterInStartTimer, masterOutStartTimer;

		// construct
		static BandwidthUsage()
		{
			enabled = (DebuggingSettings.Single.logDataUse);
		}
		
		[Conditional("UNITY_EDITOR")]
		public static void AddUsage(ref UdpBitStream bitstream, string _itemname)
		{
			if (!enabled)
				return;

			int size = bitstream.ptr - lastPtr;
			summary += _itemname.PadRight(16) + "\t" + size + "\n";
			lastPtr = bitstream.ptr;
		}

		[Conditional("UNITY_EDITOR")]
		public static void PrintSummary()
		{
			if (!enabled)
				return;
			string color =
				(logType == BandwidthLogType.UpdateRcv) ? "<color=navy>" :
				(logType == BandwidthLogType.UpdateSend) ? "<color=olive>" :
				"<color=purple>"; // mirror

			UnityEngine.Debug.Log(Time.time + " <b>" + color  + logType + "</color></b> for NstId:" + nstid + " " + objectName + "\nTotal: " + (lastPtr - startPtr) + " bits / " + Mathf.CeilToInt((lastPtr - startPtr) / 8) + " bytes. \n" + summary);
		}

		[Conditional("UNITY_EDITOR")]
		public static void SetName(NetworkSyncTransform nst)
		{
			if (!enabled)
				return;

			objectName = nst.name;
			nstid = nst.NstId;
		}

		[Conditional("UNITY_EDITOR")]
		public static void Start(ref UdpBitStream bitStream, BandwidthLogType _logType)
		{
			if (!enabled)
				return;

			logType = _logType;
			lastPtr = bitStream.ptr;
			startPtr = lastPtr;
			summary = "";
		}

		[Conditional("UNITY_EDITOR")]
		public static void ReportMasterBits(ref UdpBitStream bitstream, BandwidthLogType logType)
		{
			if (!enabled)
				return;

			// log start time if this is the first call.
			if (logType == BandwidthLogType.MasterIn)
				if (masterInStartTimer == 0)
					masterInStartTimer = Time.time;
				else
				if (masterOutStartTimer == 0)
					masterOutStartTimer = Time.time;

			if (logType == BandwidthLogType.MasterIn)
				masterInTotalBits += bitstream.ptr;
			else
				masterOutTotalBits += bitstream.ptr;


			float elapstedTime = Time.time - ((logType == BandwidthLogType.MasterIn) ? masterInStartTimer : masterOutStartTimer);
			string color = (logType == BandwidthLogType.MasterIn) ? "<color=blue>" : "<color=green>";
			string avg = ( masterInTotalBits / elapstedTime).ToString();

			UnityEngine.Debug.Log(Time.time + " " + color + "<b>" + logType + " Summary:</b></color> " + bitstream.ptr + " bits / " + bitstream.BytesUsed + " Bytes / " + avg + " b/s ");
		}
	}
}


