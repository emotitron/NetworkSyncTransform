//Copyright 2018, Davin Carten, All rights reserved

using System.Diagnostics;
using emotitron.Compression;

namespace emotitron.NST
{
	public static class IntegrityCheck
	{
		private const int TEST_BITS_SIZE = 8;
		private const ushort TEST_VAL = 24; // 0001 1000
		private static byte val;

		static IntegrityCheck()
		{
			UnityEngine.Debug.LogWarning("WARNING!! IntegrityCheck is enabled (INTEGRITY_CHECK is added to the Scripting Define Symbols in Build Settings > Other Settings).  This will add a considerable amount of network data to non-release builds.");
		}

		[Conditional("INTEGRITY_CHECK")]
		public static void WriteCheck(ref UdpBitStream bitStream)
		{
			bitStream.WriteUShort(TEST_VAL, TEST_BITS_SIZE);
		}

		[Conditional("INTEGRITY_CHECK")]
		public static void ReadCheck(ref UdpBitStream bitStream, string tag)
		{
			val = bitStream.ReadByte(TEST_BITS_SIZE);
			if (val != TEST_VAL)
				UnityEngine.Debug.LogError("INTEGRITY FAIL " + tag + " " + Utilities.BitUtilities.BitTools.PrintBitMask(val));
		}

		[Conditional("INTEGRITY_CHECK")]
		public static void ReadCheck(ref UdpBitStream bitStream, ref UdpBitStream outstream, string tag, bool mirror = false)
		{
			ReadCheck(ref bitStream, tag);
			if (mirror)
				WriteCheck(ref outstream);
		}

		[Conditional("INTEGRITY_CHECK")]
		public static void WritePosition(ref UdpBitStream bitStream, UnityEngine.Vector3 pos)
		{
			bitStream.WriteFloat(pos.x);
			bitStream.WriteFloat(pos.y);
			bitStream.WriteFloat(pos.z);
		}

		[Conditional("INTEGRITY_CHECK")]
		public static void ReadPosition(ref UdpBitStream bitStream, ref UdpBitStream outstream, bool print = true, bool mirror = false, string pre = "")
		{
			UnityEngine.Vector3 pos = new UnityEngine.Vector3(bitStream.ReadFloat(), bitStream.ReadFloat(), bitStream.ReadFloat());
			if (print)
				UnityEngine.Debug.Log(pre + pos);

			if (mirror)
				WritePosition(ref outstream, pos);
		}
	}
}

