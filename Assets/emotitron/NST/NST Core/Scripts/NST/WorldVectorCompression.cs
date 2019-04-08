// Deprecated. All methods now exist in WorldCompressionSettings and in the ElementCrusher


////Copyright 2018, Davin Carten, All rights reserved

//using UnityEngine;
//using emotitron.Compression;
//using emotitron.Debugging;

//namespace emotitron.NST
//{
//	/// <summary>
//	/// Compress vector3 to the scale of the map.
//	/// </summary>
//	public static class WorldVectorCompression
//	{
//		// constructor (not entirely sure if this is needed)
//		static WorldVectorCompression()
//		{
//			//Bounds bounds = NSTMapBounds.combinedWorldBounds;
//			//SetWorldRanges(bounds, true);
//		}

		

//		//public static void WriteWorldCompPosToBitstream(this CompressedElement compressedpos, ref UdpBitStream bitstream, IncludedAxes ia, BitCullingLevel bcl)
//		//{

//		//	if (((int)ia & 1) != 0) bitstream.WriteUInt(compressedpos[0], WorldCompressionSettings.Single.globalPosCrusher.xcrusher.GetBitsAtCullLevel(bcl));
//		//	for (int axis = 0; axis < 3; axis++)
//		//	{
//		//		if (ia.IsXYZ(axis)) bitstream.WriteUInt(compressedpos[axis], WorldCompressionSettings.Single.globalPosCrusher[axis].GetBitsAtCullLevel(bcl));
//		//	}
//		//}

//		//public static void WriteCompressedAxisToBitstream(this uint val, int axis, ref UdpBitStream bitstream, BitCullingLevel bcl)
//		//{
//		//	bitstream.WriteUInt(val, WorldCompressionSettings.Single.globalPosCrusher[axis].GetBitsAtCullLevel(bcl));

//		//}

//		//public static uint WriteAxisToBitstream(this float val, int axis, ref UdpBitStream bitstream, BitCullingLevel bcl)
//		//{
//		//	uint compressedAxis = val.CompressAxis(axis);
//		//	bitstream.WriteUInt(val.CompressAxis(axis), WorldCompressionSettings.Single.globalPosCrusher[axis].GetBitsAtCullLevel(bcl));
		
//		//	return compressedAxis;
//		//}

//		//public static uint CompressAxis(this float val, int axis)
//		//{
//		//	return WorldCompressionSettings.Single.globalPosCrusher[axis].Compress(val);
//		//}

//		//public static CompressedElement CompressToWorld(this Vector3 pos)
//		//{
//		//	WorldCompressionSettings.Single.globalPositionCrusher.Compress(pos);
//		//	CompressedValue cx = WorldCompressionSettings.Single.globalPositionCrusher[0].Compress(pos.x);
//		//	CompressedValue cy = WorldCompressionSettings.Single.globalPositionCrusher[1].Compress(pos.y);
//		//	CompressedValue cz = WorldCompressionSettings.Single.globalPositionCrusher[2].Compress(pos.z);
//		//	CompressedElement ce = new CompressedElement(null, cx, cy, cz);

//		//	return ce;
//		//}

//		//public static int ReadCompressedAxisFromBitstream(ref UdpBitStream bitstream, int axis, BitCullingLevel bcl)
//		//{
//		//	return (bitstream.ReadInt(WorldCompressionSettings.Single.globalPosCrusher[axis].GetBitsAtCullLevel(bcl)));
//		//}

//		//public static float ReadAxisFromBitstream(ref UdpBitStream bitstream, int axis, BitCullingLevel bcl)
//		//{
//		//	uint compressedAxis = bitstream.ReadUInt(WorldCompressionSettings.Single.globalPosCrusher[axis].GetBitsAtCullLevel(bcl));

//		//	return compressedAxis.DecompressAxis(axis);
//		//}

//		//private static float DecompressAxis(this uint val, int axis)
//		//{
//		//	return WorldCompressionSettings.Single.globalPosCrusher[axis].Decompress(val);
//		//}

//		//public static CompressedElement ReadCompressedPosFromBitstream(CompressedElement target, ref UdpBitStream bitstream, IncludedAxes ia, BitCullingLevel bcl)
//		//{
//		//	return new CompressedElement(
//		//		null,
//		//		(ia.IsXYZ(0)) ? (bitstream.ReadUInt(WorldCompressionSettings.Single.glblPosCrusher[0].GetBitsAtCullLevel(bcl))) : 0,
//		//		(ia.IsXYZ(1)) ? (bitstream.ReadUInt(WorldCompressionSettings.Single.glblPosCrusher[1].GetBitsAtCullLevel(bcl))) : 0,
//		//		(ia.IsXYZ(2)) ? (bitstream.ReadUInt(WorldCompressionSettings.Single.glblPosCrusher[2].GetBitsAtCullLevel(bcl))) : 0);
//		//}

//		//private static Vector3 Decompress(uint x, uint y, uint z)
//		//{
//		//	return new Vector3
//		//		(
//		//			WorldCompressionSettings.Single.glblPosCrusher[0].Decompress(x),
//		//			WorldCompressionSettings.Single.glblPosCrusher[1].Decompress(y),
//		//			WorldCompressionSettings.Single.glblPosCrusher[2].Decompress(z)
//		//		);
//		//}
//		//public static Vector3 DecompressFromWorld(this CompressedElement compos)
//		//{
//		//	return new Vector3
//		//		(
//		//			WorldCompressionSettings.Single.globalPositionCrusher[0].Decompress(compos.cx),
//		//			WorldCompressionSettings.Single.globalPositionCrusher[1].Decompress(compos.cy),
//		//			WorldCompressionSettings.Single.globalPositionCrusher[2].Decompress(compos.cz)
//		//		);
//		//}

//		///// TODO: Add Clamp to ElementCrusher
//		//public static Vector3 ClampAxes(Vector3 value)
//		//{
//		//	return new Vector3(
//		//		WorldCompressionSettings.globalPosCrusher[0].Clamp(value[0]),
//		//		WorldCompressionSettings.globalPosCrusher[1].Clamp(value[1]),
//		//		WorldCompressionSettings.globalPosCrusher[2].Clamp(value[2])
//		//		);
//		//}


//		//private static bool TestMatchingUpper(uint a, uint b, int lowerbits)
//		//{
//		//	return (((a >> lowerbits) << lowerbits) == ((b >> lowerbits) << lowerbits));
//		//}

//		//public static bool TestMatchingUpper(CompressedElement prevPos, CompressedElement b, FloatRange[] ar, BitCullingLevel bcl)
//		//{
//		//	return
//		//		(
//		//		TestMatchingUpper(prevPos.x, b.x, ar[0].BitsAtCullLevel(bcl)) &&
//		//		TestMatchingUpper(prevPos.y, b.y, ar[1].BitsAtCullLevel(bcl)) &&
//		//		TestMatchingUpper(prevPos.z, b.z, ar[2].BitsAtCullLevel(bcl))
//		//		);
//		//}


//		///// <summary>
//		///// Attempts to guess the most likely upperbits state by seeing if each axis of the new position would be
//		///// closer to the old one if the upper bit is incremented by one, two, three etc. Stops trying when it fails to get a better result than the last increment.
//		///// </summary>
//		///// <param name="oldcpos">Last best position test against.</param>
//		///// <returns>Returns a corrected CompressPos</returns>
//		//public static CompressedElement GuessUpperBitsWorld(this CompressedElement newcpos, CompressedElement oldcpos, BitCullingLevel bcl)
//		//{
//		//	return newcpos.GuessUpperBits(oldcpos, axisRanges, bcl);
//		//}

//		//public static CompressedElement ZeroLowerBits(this CompressedElement fullpos, BitCullingLevel bcl)
//		//{
//		//	return new CompressedElement(
//		//		null,
//		//		axisRanges[0].ZeroLowerBits(fullpos.cx, bcl),
//		//		axisRanges[1].ZeroLowerBits(fullpos.cy, bcl),
//		//		axisRanges[2].ZeroLowerBits(fullpos.cz, bcl)
//		//		);
//		//}

//		//public static CompressedElement ZeroUpperBits(this CompressedElement fullpos, BitCullingLevel bcl)
//		//{
//		//	return new CompressedElement(
//		//		null,
//		//		axisRanges[0].ZeroUpperBits(fullpos.cx, bcl),
//		//		axisRanges[1].ZeroUpperBits(fullpos.cy, bcl),
//		//		axisRanges[2].ZeroUpperBits(fullpos.cz, bcl)
//		//		);
//		//}

//		//public static CompressedElement OverwriteLowerBits(CompressedElement upperbits, CompressedElement lowerbits, BitCullingLevel bcl)
//		//{
//		//	return new CompressedElement
//		//	(
//		//		null,
//		//		WorldCompressionSettings.globalPosCrusher[0].ZeroLowerBits(upperbits[0], bcl) | lowerbits[0],
//		//		WorldCompressionSettings.globalPosCrusher[1].ZeroLowerBits(upperbits[1], bcl) | lowerbits[1],
//		//		WorldCompressionSettings.globalPosCrusher[2].ZeroLowerBits(upperbits[2], bcl) | lowerbits[2]
//		//	);
//		//}

//	}
//}



