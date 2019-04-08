//// Copyright (c) 2018 emotitron (Davin Carten)

//using UnityEngine;

//namespace emotitron.Network.Compression
//{
//	/// <summary>
//	/// This compression is based on the smallest three method of compressing Quaternions. Supply the Quaterion and the number of /bits you want it reduced to.
//	/// 2 bits are used to identify which channel of xyzw was left out and needs to be reconstructed.
//	/// </summary>
//	public static class QuatCompress
//	{
//		private const int MIN_ALLOWED_BITS = 16;
//		private const int DEF_BYTE_COUNT = 5;

//		/// <summary>
//		/// Cache's the inverse of all of the max values, used by decompress to avoid division and just generally minimize the math in the hotpath.
//		/// </summary>
//		private static float[] invMaxValue;

//		/// <summary>
//		/// Cache's the bit used per channel breakdown for each total bits level.
//		/// </summary>
//		private static BitsPer[] BitsPerChanForTotalBits;

//		// internally reuses this array to reduce GC when non-alloc is not used.
//		public static byte[] reusableByteArray = new byte[8];  


//		// Static Constructor
//		static QuatCompress()
//		{
//			// Do the math for each bit levels up front
//			BitsPerChanForTotalBits = new BitsPer[65];
//			for (int i = MIN_ALLOWED_BITS; i < 65; i++)
//				BitsPerChanForTotalBits[i] = DivideBitsAmongChannels(i);

//			//Create the invMaxValues (at the max bit size of 64, it comes out to 21 bits per channel, so 22 bits per channel is more than we will ever use.
//			invMaxValue = new float[23];

//			// precache the inverse of the max value for the number of bits so we never have to divide
//			for (int i = 0; i < 23; i++)
//			{
//				invMaxValue[i] = 1f / ((1 << i) - 1);
//			}
//		}
		
//		// Premade bits per channel for each bit total compression size
//		private struct BitsPer
//		{
//			public int bitsA, bitsB, bitsC, shiftB, shiftC, shiftExtra;

//			public BitsPer(int _bitsA, int _bitsB, int _bitsC)
//			{
//				// number of bits used for each channel
//				bitsA = _bitsA;
//				bitsB = _bitsB;
//				bitsC = _bitsC;

//				// how much to bitshift B, C and the 2 byte missing axis id
//				shiftB = _bitsA;
//				shiftC = _bitsA + _bitsB;
//				shiftExtra = _bitsA + _bitsB + _bitsC;
//			}
//		}

//		// --------------------------------------------------------------------------
//		// ------------------------------- COMPRESS  --------------------------------
//		// --------------------------------------------------------------------------

//		/// <summary>
//		/// Primary Compression Method. Converts a quaternion into a ulong buffer. Depending on size most of the top bits will be 0. 
//		/// </summary>
//		/// <param name="rot">The quaternion to be compressed</param>
//		/// <param name="bytecount">Number of bytes the quat will be compressed down to.</param>
//		/// <returns>A ulong buffer of the compressed quat.</returns>
//		private static ulong Compress(this Quaternion rot, int bitsA, int bitsB, int bitsC)
//		{
//			ulong biggestAxis = 0;
//			var maxValue = float.MinValue; // initialize to the lowest possible number a float can be - so first compare is sure to be greater.
//			bool isPositive = true;

//			// Determine the index of the largest (absolute value) element in the Quaternion to leave out.

//			for (int i = 0; i < 4; i++)
//			{
//				var axis = rot[i];
//				var abs = Mathf.Abs(rot[i]);

//				// Find the axis with the greatest magnitude
//				if (abs > maxValue)
//				{
//					// Get the sign of the axis not being sent.
//					isPositive = (axis >= 0);

//					// Record which index is not being sent
//					biggestAxis = (ulong)i;
//					maxValue = abs;
//				}
//			}

//			// Create the buffer used elements
//			return
//				(biggestAxis == 0) ? BuildBuffer(biggestAxis, rot.y, rot.z, rot.w, isPositive, bitsA, bitsB, bitsC) :
//				(biggestAxis == 1) ? BuildBuffer(biggestAxis, rot.x, rot.z, rot.w, isPositive, bitsA, bitsB, bitsC) :
//				(biggestAxis == 2) ? BuildBuffer(biggestAxis, rot.x, rot.y, rot.w, isPositive, bitsA, bitsB, bitsC) :
//								  BuildBuffer(biggestAxis, rot.x, rot.y, rot.z, isPositive, bitsA, bitsB, bitsC);
//		}

//		/// <summary>
//		/// Chain together the buffer from the axes and biggestAxis id
//		/// </summary>
//		/// <returns></returns>
//		private static ulong BuildBuffer(ulong biggestAxis, float a, float b, float c, bool isPositive, int bitsA, int bitsB, int bitsC)
//		{
//			// Range is half the value of Max
//			int rangeA = (1 << (bitsA - 1)) - 1;
//			int rangeB = (1 << (bitsB - 1)) - 1;
//			int rangeC = (1 << (bitsC - 1)) - 1;

//			return
//				(ulong)((isPositive ? a : -a) * rangeA + rangeA) |
//				(ulong)((isPositive ? b : -b) * rangeB + rangeB) << bitsA |
//				(ulong)((isPositive ? c : -c) * rangeC + rangeC) << (bitsA + bitsB) |
//				biggestAxis << (bitsA + bitsB + bitsC);
//		}

//		/// <summary>
//		/// Compress a Quaternion to single ulong buffer. Set the total number of bits to limit the number of bits used (heavier compression). Limiting the number
//		/// of bits used allows this buffer to be written to bitstreams for networking using very specific lenghts (rather than even bytes as with a byte stream)
//		/// If you are not using a bitstream or bytestream, and you are just compressing your quaternion by turning it into a ULong, leave the totalBits at 64 for
//		/// maximum resolution. While this isn't and ideal amount of compression, it is 50% of the size of an uncompressed quaternion.
//		/// <param name="rotation"></param>
//		/// <param name="totalBits">A range of 24 to 48 is where this wants to be. 40 is very accurate. 48 pinpoint accurate.</param>
//		/// <returns></returns>
//		public static ulong CompressToULong(this Quaternion rotation, int totalBits = 64)
//		{
//			if (totalBits < MIN_ALLOWED_BITS)
//				totalBits = MIN_ALLOWED_BITS;

//			BitsPer bitsPerChannel = BitsPerChanForTotalBits[totalBits];
//			return Compress(rotation, bitsPerChannel.bitsA, bitsPerChannel.bitsB, bitsPerChannel.bitsC);
//		}

//		/// <summary>
//		/// Compress to a 32 bit UInt. NOTE: 16 bits is strong compression (25% of the origina Quaternion), some small errors in rotationn will be visible. 
//		/// For flawless compression, use CompressToULong or compress to a byte[] or the Ulong buffer using a bitcount in the range of 40 to 50 bits.
//		/// </summary>
//		public static uint CompressToUint(this Quaternion rot)
//		{
//			return (uint)Compress(rot, 10, 10, 10);
//		}

//		/// <summary>
//		/// Compress to a 16 bit UInt. NOTE: 16 bits is heavy compression (12.5% of the origina Quaternion), this will not rotate smoothly. 
//		/// For flawless compression, use CompressToULong or compress to a byte[] or the Ulong buffer using a bitcount in the range of 40 to 50 bits.
//		/// </summary>
//		public static ushort CompressToUShort(this Quaternion rot)
//		{
//			return (ushort)Compress(rot, 5, 5, 4);
//		}


//		/// <summary>
//		/// Writes the quat to a byte[] array. WARNING: this byte[] is reused every time QuatCompress is compresses a quaternion to byte[], so be sure to use
//		/// the contents immediately (copy them to your bitstream, byte array or wherever the data is going. If you need to queue byte arrays for later writing
//		/// use the CompressToByteArrayNonAlloc variation and supply a byte[].
//		/// </summary>
//		/// <param name="rot"></param>
//		/// <param name="bytecount"></param>
//		/// <returns>Returns a recycled byte array. The contents of this may change if not used immediately.</returns>
//		public static byte[] CompressToByteArray(this Quaternion rot, int bytecount = DEF_BYTE_COUNT)
//		{
//			return CompressToByteArrayNonAlloc(rot, reusableByteArray, bytecount);
//		}

//		/// <summary>
//		/// Non-Alloc overload version. Will use the length of the array as the compression level if no level is given.
//		/// </summary>
//		/// <param name="arr">Pre-allocated array to fill. Size should be 3-8 bytes.</param>
//		/// <param name="bytecount">Number of bytes to compress to. Leave blank or set to -1 to use the byte count of the supplied array.</param>
//		/// <returns></returns>
//		public static byte[] CompressToByteArrayNonAlloc(this Quaternion rot, byte[] arr, int bytecount = -1)
//		{
//			if (bytecount == -1)
//				bytecount = arr.Length;

//			ulong buffer = CompressToULong(rot, bytecount * 8);

//			// write all 8 bytes just be sure its cleared without doing a clear.
//			for (int i = 0; i < 8; i++)
//			{
//				arr[i] = (byte)(buffer >> (i * 8));
//			}
//			return arr;
//		}

//		// --------------------------------------------------------------------------
//		// ------------------------------- DECOMPRESS  ------------------------------
//		// --------------------------------------------------------------------------

//		/// <summary>
//		/// Primary Decompression Method. Decompress the 3 channels and missing channel ID from the serialized ULong buffer.
//		/// </summary>
//		/// <param name="buffer">The ulong that represents the compressed quaternion.</param>
//		/// <param name="bytecount">The number of bytes the quaternion was originally compressed to.</param>
//		/// <returns>The restored Quaternion.</returns>
//		private static Quaternion Decompress(this ulong buffer, int bitsA, int bitsB, int bitsC)
//		{
//			// get the max possible values for the bit counts
//			ulong maxA = ((ulong)1 << bitsA) - 1;
//			ulong maxB = ((ulong)1 << bitsB) - 1;
//			ulong maxC = ((ulong)1 << bitsC) - 1;

//			// the zero check is to avoid creating a -1 index (which of course there can't be)
//			int rangeA = (1 << (bitsA - 1)) - 1;
//			int rangeB = (1 << (bitsB - 1)) - 1;
//			int rangeC = (1 << (bitsC - 1)) - 1;

//			int along = (int)(maxA & buffer);
//			int blong = (int)(maxB & (buffer >> bitsA));
//			int clong = (int)(maxC & (buffer >> bitsA + bitsB));

//			int maxIndex = (int)(buffer >> (bitsA + bitsB + bitsC));

//			// uncompressed value = (compressed value - half of the max value) / half of the max value
//			float a = (along - rangeA) * invMaxValue[bitsA - 1]; 
//			float b = (blong - rangeB) * invMaxValue[bitsB - 1]; 
//			float c = (clong - rangeC) * invMaxValue[bitsC - 1]; 

//			float d = Mathf.Sqrt(1f - (a * a + b * b + c * c));

//			return
//				(maxIndex == 0) ? new Quaternion(d, a, b, c) :
//				(maxIndex == 1) ? new Quaternion(a, d, b, c) :
//				(maxIndex == 2) ? new Quaternion(a, b, d, c) :
//								  new Quaternion(a, b, c, d);
//		}

//		/// <summary>
//		/// Read the compressed quaterion from a Byte[] array.
//		/// </summary>
//		public static Quaternion DecompressToQuat(this byte[] arr, int bytecount = DEF_BYTE_COUNT)
//		{
//			return DecompressToQuat(arr.ConvertByteArrayToULongBuffer(bytecount), bytecount * 8);
//		}

//		/// <summary>
//		/// Decompress Quaternion from ulong buffer.
//		/// </summary>
//		/// <param name="totalBits">Number of bits that were used to compressed this quat.</param>
//		/// <returns></returns>
//		public static Quaternion DecompressToQuat(this ulong buffer, int totalBits = 64)
//		{
//			BitsPer bitsper = BitsPerChanForTotalBits[totalBits];
//			return Decompress(buffer, bitsper.bitsA, bitsper.bitsB, bitsper.bitsC);
//		}

//		/// <summary>
//		/// Decompress Quaternion from uint buffer.
//		/// <param name="totalBits">Number of bits that were used to compressed this quat.</param>
//		/// </summary>
//		public static Quaternion DecompressToQuat(this uint buffer, int totalBits = 32)
//		{
//			BitsPer bitsper = BitsPerChanForTotalBits[totalBits];
//			return Decompress(buffer, bitsper.bitsA, bitsper.bitsB, bitsper.bitsC);
//		}

//		/// <summary>
//		/// Decompress Quaternion from uint buffer.
//		/// <param name="totalBits">Number of bits that were used to compressed this quat.</param>
//		/// </summary>
//		public static Quaternion DecompressToQuat(this short buffer)
//		{
//			return Decompress((ulong)buffer, 5, 5, 4);
//		}

//		/// <summary>
//		/// Utility to convert a byte[] array into a ulong buffer.
//		/// </summary>
//		/// <param name="bytecount">Number of bytes to use (in case the byte[].length is larger than the number of bytes used). 
//		/// -1 means use the full length of the array.</param>
//		/// <returns></returns>
//		public static ulong ConvertByteArrayToULongBuffer(this byte[] arr, int bytecount = -1)
//		{
//			ulong buffer = 0;

//			if (bytecount == -1)
//				bytecount = arr.Length;

//			for (int i = 0; i < bytecount; i++)
//			{
//				buffer = buffer | ((ulong)arr[i]) << (i * 8);
//			}
//			return buffer;
//		}

//		/// <summary>
//		/// Distribute bits among the three channels as evenly as possible (leaving 2 bits for the missing axis ID)
//		/// </summary>
//		private static BitsPer DivideBitsAmongChannels(int totalbits)
//		{
//			// take two off the top to identify the dropped axis
//			totalbits -= 2;

//			int startingBitsPer = totalbits / 3;
//			int remainingBits = totalbits - startingBitsPer * 3;

//			return new BitsPer(
//			startingBitsPer + ((remainingBits > 0) ? 1 : 0),
//			startingBitsPer + ((remainingBits == 2) ? 1 : 0),
//			startingBitsPer
//			);
//		}
//	}
//}