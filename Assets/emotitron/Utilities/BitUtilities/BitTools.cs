
//Copyright 2018, Davin Carten, All rights reserved

namespace emotitron.Utilities.BitUtilities
{
	public static class BitTools
	{

		// byte
		public static void SetBitInMask(this int bit, ref byte mask, bool onoff)
		{
			mask = ((byte)((onoff) ?
				(mask | (byte)(1 << bit)) :
				(mask & (byte)(~(1 << bit)))));
		}

		//public static void SetBitInMask(ref byte mask, int bit, bool onoff)
		//{
		//	mask = ((byte)((onoff) ?
		//		(mask | (byte)(1 << bit)) :
		//		(mask & (byte)(~(1 << bit)))));
		//}

		//ushort
		public static void SetBitInMask(this int bit, ref ushort mask, bool onoff)
		{
			mask = (ushort)((onoff) ?
				(mask | (ushort)(1 << bit)) :
				(mask & (ushort)(~(1 << bit))));
		}

		//public static void SetBitInMask(ref ushort mask, int bit, bool onoff)
		//{
		//	mask = (ushort)((onoff) ?
		//		(mask | (ushort)(1 << bit)) :
		//		(mask & (ushort)(~(1 << bit))));
		//}

		public static void SetBitInMask(this int bit, ref int mask, bool onoff)
		{
			mask = (int)((onoff) ?
				(mask | (int)(1 << bit)) :
				(mask & (int)(~(1 << bit))));
		}

		public static int SetBitInInt(this int mask, int bit, bool onoff)
		{
			mask = (int)((onoff) ?
				(mask | (int)(1 << bit)) :
				(mask & (int)(~(1 << bit))));
			return mask;
		}
		// uint
		public static void SetBitInMask(this int bit, ref uint mask, bool onoff)
		{
			mask = (uint)((onoff) ?
				(mask | (uint)(1 << bit)) :
				(mask & (uint)(~(1 << bit))));
		}
		public static uint SetBitInUInt(uint mask, int bit, bool onoff)
		{
			mask = (uint)((onoff) ?
				(mask | (uint)(1 << bit)) :
				(mask & (uint)(~(1 << bit))));

			return mask;
		}
		//public static void SetBitInMask(ref uint mask, int bit, bool onoff)
		//{
		//	mask = (uint)((onoff) ?
		//		(mask | (uint)(1 << bit)) :
		//		(mask & (uint)(~(1 << bit))));
		//}

		//ulong
		public static void SetBitInMask(this int bit, ref ulong mask, bool onoff)
		{
			mask = (ulong)((onoff) ?
				(mask | (ulong)((ulong)1 << bit)) :
				(mask & (ulong)(~((ulong)1 << bit))));
		}

		//public static void SetBitInMask(ref ulong mask, int bit, bool onoff)
		//{
		//	mask = (ulong)((onoff) ?
		//		(mask | (ulong)((ulong)1 << bit)) :
		//		(mask & (ulong)(~((ulong)1 << bit))));
		//}



		public static bool GetBitInMask(this byte mask, int bit)
		{
			return ((mask & (byte)(1 << bit)) != 0);
		}
		public static bool GetBitInMask(this ushort mask, int bit)
		{
			return ((mask & (ushort)(1 << bit)) != 0);
		}
		public static bool GetBitInMask(this int mask, int bit)
		{
			return ((mask & (int)(1 << bit)) != 0);
		}

		public static bool GetBitInMask(this uint mask, int bit)
		{
			return ((mask & (uint)((uint)1 << bit)) != 0);
		}

		public static bool GetBitInMask(this ulong mask, int bit)
		{
			return ((mask & (ulong)((ulong)1 << bit)) != 0);
		}



		public static bool CompareBit(this int bit, byte a, byte b)
		{
			byte mask = (byte)(1 << bit);
			return ((a & mask) == (b & mask));
		}
		public static bool CompareBit(this int bit, ushort a, ushort b)
		{
			ushort mask = (ushort)(1 << bit);
			return ((a & mask) == (b & mask));
		}
		public static bool CompareBit(this int bit, uint a, uint b)
		{
			uint mask = (uint)1 << bit;
			return ((a & mask) == (b & mask));
		}



		public static int CountTrueBits(this byte mask)
		{
			int count = 0;
			for (int i = 0; i < 8; ++i)
			{
				if (mask.GetBitInMask(i))
					i++;
			}
			return count;
		}

		public static int CountTrueBits(this ushort mask)
		{
			int count = 0;
			for (int i = 0; i < 16; ++i)
			{
				if (mask.GetBitInMask(i))
					count++;
			}
			return count;
		}
		public static int CountTrueBits(this uint mask)
		{
			int count = 0;
			for (int i = 0; i < 32; ++i)
			{
				if (mask.GetBitInMask(i))
					count++;
			}
			return count;
		}

		public static int CountTrueBits(this ulong mask)
		{
			int count = 0;
			for (int i = 0; i < 64; ++i)
			{
				if (mask.GetBitInMask(i))
					count++;
			}
			return count;
		}

		public static string PrintBitMask(this ushort mask)
		{
			string str = "";
			for (int i = 15; i >= 0; --i)
			{
				str += (GetBitInMask(mask, i)) ? 1 : 0;

				if (i % 4 == 0)
					str += " ";
			}
			return str;
		}

		public static string PrintBitMask(this byte mask, int highliteNum = -1)
		{
			string str = "";
			for (int i = 7; i >= 0; --i)
			{
				if (i == highliteNum)
					str += "<b>";

				str += (GetBitInMask(mask, i)) ? 1 : 0;

				if (i == highliteNum)
					str += "</b>";

				if (i % 4 == 0)
					str += " ";
			}
			return str;
		}

		public static string PrintBitMask(this uint mask, int highliteNum = -1, int numOfBitsToShow = 32)
		{
			string str = "";
			for (int i = numOfBitsToShow - 1; i >= 0; --i)
			{
				if (i == highliteNum)
					str += "<b>";

				str += (GetBitInMask(mask, i)) ? 1 : 0;

				if (i == highliteNum)
					str += "</b>";

				if (i % 4 == 0)
					str += " ";
			}
			return str;
		}
		public static string PrintBitMask(this ulong mask, int highliteNum = -1, int numOfBitsToShow = 64)
		{
			string str = "";
			for (int i = numOfBitsToShow - 1; i >= 0; --i)
			{
				if (i == highliteNum)
					str += "<b>";

				str += (GetBitInMask(mask, i)) ? 1 : 0;

				if (i == highliteNum)
					str += "</b>";

				if (i % 4 == 0)
					str += " ";
			}
			return str;
		}

		public static int GetTrueBitOfLayerMask(int layermask)
		{
			for (int i = 0; i < 32; ++i)
			{
				if (layermask.GetBitInMask(i))
					return i;
			}
			return 0;
		}

		public static string PrintContents(this byte[] b, int count = -1)
		{
			if (count == -1)
				count = b.Length;

			string s = "";
			for (int i = count -1; i >= 0; --i)
				s += b[i].PrintBitMask() + " ";

			return s;
		}

		//public static uint[] maxValue = new uint[33]
		//{
		//	0, 1, 3, 7, 15, 31, 63, 127, 255,
		//	511, 1023, 2047, 4095, 8191, 16383, 32767, 65535,
		//	131071, 262143, 524287, 1048575, 2097151, 4194303, 8388607, 16777215,
		//	33554431, 67108863, 134217727, 268435455, 536870911, 1073741823, 2147483647,
		//	4294967295 //, 8589934591, 17179869183, 34359738367, 68719476735, 137438953471, 274877906943, 549755813887, 1099511627775,
		//	//2199023255551, 4398046511103, 8796093022207, 17592186044415, 35184372088831, 70368744177663, 140737488355327, 281474976710655
		//};

		//UNTESTED
		/// <summary>
		/// Finds the number of bits needed for the max value possible. Returns -1 on error.
		/// </summary>
		public static int BitsNeededForMaxValue(this uint val)
		{
			for (int i = 0; i < 32; ++i)
				if (val >> i == 0)
					return i;

			return -1;
		}

		//UNTESTED
		public static int BitsNeededLargeFirst(this uint val)
		{
			for (int i = 31; i >= 0; --i)
				if (val << i == 1)
					return i + 1;

			return 0;
		}

		public static uint MaxUintValueForBits(this int bitcount)
		{
			return ((uint)1 << bitcount) - 1;
		}


		public static ulong MaxULongValueForBits(this int bitcount)
		{
			return ((ulong)1 << bitcount) - 1;
		}


		//public static int BitsNeededForMaxValue(uint value)
		//{
		//	return
		//		(value == 0) ? 0 :
		//		(value < 2) ? 1 :
		//		(value < 4) ? 2 :
		//		(value < 8) ? 3 :
		//		(value < 16) ? 4 :
		//		(value < 32) ? 5 :
		//		(value < 64) ? 6 :
		//		(value < 128) ? 7 :
		//		(value < 256) ? 8 :

		//		(value < 512) ? 9 :
		//		(value < 1024) ? 10 :
		//		(value < 2048) ? 11 :
		//		(value < 4096) ? 12 :
		//		(value < 8192) ? 13 :
		//		(value < 16384) ? 14 :
		//		(value < 32768) ? 15 :
		//		(value < 65536) ? 16 :

		//		(value < 131072) ? 17 :
		//		(value < 262144) ? 18 :
		//		(value < 524288) ? 19 :
		//		(value < 1048576) ? 20 :
		//		(value < 2097152) ? 21 :
		//		(value < 4194304) ? 22 :
		//		(value < 8388608) ? 23 :
		//		(value < 16777216) ? 24 :

		//		(value < 33554432) ? 25 :
		//		(value < 67108864) ? 26 :
		//		(value < 134217728) ? 27 :
		//		(value < 268435456) ? 28 :
		//		(value < 536870912) ? 29 :
		//		(value < 1073741824) ? 30 :
		//		(value < 2147483648) ? 31 :
		//		32;
		//}

		//public static int BitsNeededForMaxValueLargeFirst(uint value)
		//{
		//	return
		//		(value > 2147483647) ? 32 :
		//		(value > 1073741823) ? 31 :
		//		(value > 536870911) ? 30 :
		//		(value > 268435455) ? 29 :
		//		(value > 134217727) ? 28 :
		//		(value > 67108863) ? 27 :
		//		(value > 33554431) ? 26 :

		//		(value > 16777215) ? 25 :
		//		(value > 8388607) ? 24 :
		//		(value > 4194303) ? 23 :
		//		(value > 2097151) ? 22 :
		//		(value > 1048575) ? 21 :
		//		(value > 524287) ? 20 :
		//		(value > 262143) ? 19 :
		//		(value > 131071) ? 18 :

		//		(value > 65535) ? 17 :
		//		(value > 32767) ? 16 :
		//		(value > 16383) ? 15 :
		//		(value > 8191) ? 14 :
		//		(value > 4095) ? 13 :
		//		(value > 2047) ? 12 :
		//		(value > 1023) ? 11 :
		//		(value > 511) ? 10 :
		//		(value > 255) ? 9 :
		//		(value > 127) ? 8 :
		//		(value > 63) ? 7 :
		//		(value > 31) ? 6 :
		//		(value > 15) ? 5 :
		//		(value > 7) ? 4 :
		//		(value > 3) ? 3 :
		//		(value > 1) ? 2 :
		//		(value > 0) ? 1 :
		//		0;
		//}


		// Half-Byte functions


		//public static byte CompressTwoHalfBytesIntoOne(this byte first, byte second, int bitsInFirst = 4)
		//{
		//	return (byte)(first | (second << bitsInFirst));
		//}


		//private static byte[] tempTwoByte = new byte[2];
		//public static byte[] DecompressTwoVarsFromByte(this byte dualByte, int bitsInFirst = 4)
		//{
		//	// Clear the high order bits in case they aren't zero
		//	tempTwoByte[0] = (byte)(dualByte << (8 - bitsInFirst));
		//	tempTwoByte[0] = (byte)(tempTwoByte[0] >> (8 - bitsInFirst));
		//	tempTwoByte[1] = (byte)(dualByte >> bitsInFirst);
		//	return tempTwoByte;
		//}


		//public static void DecompressTwoVarsFromByte(this byte dualByte, out byte first, out byte second, int bitsInFirst = 4)
		//{
		//	// Clear the high order bits in case they aren't zero
		//	first = (byte)(dualByte << (8 - bitsInFirst));
		//	first = (byte)(first >> (8 - bitsInFirst));
		//	second = (byte)(dualByte >> bitsInFirst);
		//}

		//public static byte OverwriteSomeBits(this byte host, byte parasite, int bits)
		//{
		//	host = (byte)((host >> bits) << bits);
		//	return (byte)(host | parasite);
		//}

		//public static ushort OverwriteSomeBits(this ushort host, ushort parasite, int bits)
		//{
		//	host = (ushort)((host >> bits) << bits);
		//	return (ushort)(host | parasite);
		//}

		//public static int OverwriteSomeBits(this int host, int parasite, int bits)
		//{
		//	host = ((host >> bits) << bits);
		//	return (host | parasite);
		//}
		//public static uint OverwriteSomeBits(this uint host, uint parasite, int bits)
		//{
		//	host = ((host >> bits) << bits);
		//	return (host | parasite);
		//}

		//public static ulong OverwriteSomeBits(this ulong host, ulong parasite, int bits)
		//{
		//	host = ((host >> bits) << bits);
		//	return (host | parasite);
		//}

		//private static byte[] tempBytes = new byte[8];
		/// <summary>
		/// A recycled array with a length of 8 will be provided for you to assist in GC reduction.
		/// </summary>
		//public static byte[] BufferToByteArray(this ulong buffer, int numOfBytes)
		//{
		//	for (int offset = 0; offset < numOfBytes; offset++)
		//	{
		//		tempBytes[offset] = (byte)(buffer >> (offset * 8));
		//	}
		//	return tempBytes;
		//}
		/// <summary>
		/// Supply a reusable byte[], or don't. One will be provided for you if one isn't provided.
		/// </summary>
		//public static byte[] BufferToByteArray(this ulong buffer, byte[] bytearray, int numOfBytes)
		//{
		//	for (int offset = 0; offset < numOfBytes; offset++)
		//	{
		//		bytearray[offset] = (byte)(buffer >> (offset * 8));
		//	}
		//	return bytearray;
		//}

		//public static ulong ByteArrayToBuffer(this byte[] byteArray, int numOfBytes)
		//{
		//	ulong buffer = 0;
		//	for (int offset = 0; offset < numOfBytes; offset++)
		//	{
		//		buffer = buffer | (((ulong)byteArray[offset]) << (offset * 8));
		//	}
		//	return buffer;
		//}
	}
}
