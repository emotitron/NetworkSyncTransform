/*
* The MIT License (MIT)
* 
* Copyright (c) 2012-2013 Fredrik Holmstrom (fredrik.johan.holmstrom@gmail.com)
* 
* Permission is hereby granted, free of charge, to any person obtaining a copy
* of this software and associated documentation files (the "Software"), to deal
* in the Software without restriction, including without limitation the rights
* to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
* copies of the Software, and to permit persons to whom the Software is
* furnished to do so, subject to the following conditions:
* 
* The above copyright notice and this permission notice shall be included in
* all copies or substantial portions of the Software.
* 
* THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
* IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
* FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
* AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
* LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
* OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
* THE SOFTWARE.
*/

using System;
using System.Runtime.InteropServices;

namespace emotitron.Compression
{
	[StructLayout(LayoutKind.Explicit)]
	public struct UdpByteConverter
	{
		[FieldOffset(0)]
		public Int16 Signed16;
		[FieldOffset(0)]
		public UInt16 Unsigned16;
		[FieldOffset(0)]
		public Char Char;
		[FieldOffset(0)]
		public Int32 Signed32;
		[FieldOffset(0)]
		public UInt32 Unsigned32;
		[FieldOffset(0)]
		public Int64 Signed64;
		[FieldOffset(0)]
		public UInt64 Unsigned64;
		[FieldOffset(0)]
		public Single Float32;
		[FieldOffset(0)]
		public Double Float64;

		[FieldOffset(0)]
		public Byte Byte0;
		[FieldOffset(1)]
		public Byte Byte1;
		[FieldOffset(2)]
		public Byte Byte2;
		[FieldOffset(3)]
		public Byte Byte3;
		[FieldOffset(4)]
		public Byte Byte4;
		[FieldOffset(5)]
		public Byte Byte5;
		[FieldOffset(6)]
		public Byte Byte6;
		[FieldOffset(7)]
		public Byte Byte7;

		public byte[] Bytes4
		{
			get
			{
				reusableBytes4[0] = Byte0;
				reusableBytes4[1] = Byte1;
				reusableBytes4[2] = Byte2;
				reusableBytes4[3] = Byte3;
				return reusableBytes4;
			}
			set
			{
				Byte0 = value[0];
				Byte1 = value[1];
				Byte2 = value[2];
				Byte3 = value[3];
			}
		}

		public byte[] Bytes8
		{
			get
			{
				reusableBytes8[0] = Byte0;
				reusableBytes8[1] = Byte1;
				reusableBytes8[2] = Byte2;
				reusableBytes8[3] = Byte3;
				reusableBytes8[4] = Byte4;
				reusableBytes8[5] = Byte5;
				reusableBytes8[6] = Byte6;
				reusableBytes8[7] = Byte7;
				return reusableBytes8;
			}
			set
			{
				Byte0 = value[0];
				Byte1 = value[1];
				Byte2 = value[2];
				Byte3 = value[3];
				Byte4 = value[4];
				Byte5 = value[5];
				Byte6 = value[6];
				Byte7 = value[7];
			}
		}

		public static byte[] reusableBytes4 = new byte[4];
		public static byte[] reusableBytes8 = new byte[8];

		//public static UdpByteConverter BytesFromObject(object obj)
		//{
		//	byte[] incAsBytes = obj as byte[];
		//	UdpByteConverter bytes = default(UdpByteConverter);

		//	bytes.Byte0 = incAsBytes[0];
		//	bytes.Byte1 = incAsBytes[1];
		//	bytes.Byte2 = incAsBytes[2];
		//	bytes.Byte3 = incAsBytes[3];

		//	return bytes;
		//}

		public static implicit operator UdpByteConverter(byte[] bytes)
		{
			UdpByteConverter bc = default(UdpByteConverter);

			bc.Byte0 = bytes[0];
			bc.Byte1 = bytes[1];
			bc.Byte2 = bytes[2];
			bc.Byte3 = bytes[3];

			return bc;
		}

		public static implicit operator UdpByteConverter(Int16 val)
		{
			UdpByteConverter bytes = default(UdpByteConverter);
			bytes.Signed16 = val;
			return bytes;
		}

		public static implicit operator UdpByteConverter(UInt16 val)
		{
			UdpByteConverter bytes = default(UdpByteConverter);
			bytes.Unsigned16 = val;
			return bytes;
		}

		public static implicit operator UdpByteConverter(Char val)
		{
			UdpByteConverter bytes = default(UdpByteConverter);
			bytes.Char = val;
			return bytes;
		}

		public static implicit operator UdpByteConverter(UInt32 val)
		{
			UdpByteConverter bytes = default(UdpByteConverter);
			bytes.Unsigned32 = val;
			return bytes;
		}

		public static implicit operator UdpByteConverter(Int32 val)
		{
			UdpByteConverter bytes = default(UdpByteConverter);
			bytes.Signed32 = val;
			return bytes;
		}

		public static implicit operator UdpByteConverter(UInt64 val)
		{
			UdpByteConverter bytes = default(UdpByteConverter);
			bytes.Unsigned64 = val;
			return bytes;
		}

		public static implicit operator UdpByteConverter(Int64 val)
		{
			UdpByteConverter bytes = default(UdpByteConverter);
			bytes.Signed64 = val;
			return bytes;
		}

		public static implicit operator UdpByteConverter(Single val)
		{
			UdpByteConverter bytes = default(UdpByteConverter);
			bytes.Float32 = val;
			return bytes;
		}

		public static implicit operator UdpByteConverter(Double val)
		{
			UdpByteConverter bytes = default(UdpByteConverter);
			bytes.Float64 = val;
			return bytes;
		}
	}
}