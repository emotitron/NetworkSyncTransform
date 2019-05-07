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

using emotitron.Debugging;
using System;
using UnityEngine;

namespace emotitron.Compression
{
	public struct UdpBitPosition
	{
		internal readonly int Ptr;
		internal UdpBitPosition(int p) { Ptr = p; }
	}

	public struct UdpBitStream
	{
		public static UdpBitStream nullstream = new UdpBitStream();

		internal int ptr;
		internal int Length;  // Length is in bits, not bytes
		internal readonly byte[] Data;

		public bool Done
		{
			get { return ptr == Length; }
		}

		// Added by emotitron
		public int BytesUsed
		{
			get { return (ptr >> 3) + ((ptr % 8 == 0) ? 0 : 1); }
		}

		public bool Overflowing
		{
			get { return ptr > Length; }
		}

		// added by emotitron
		internal UdpBitStream(byte[] arr)
		{
			ptr = 0;
			Data = arr;
			Length = arr.Length << 3;
		}
		internal UdpBitStream(byte[] arr, int size)
		{
			ptr = 0;
			Data = arr;
			Length = size << 3;
		}

		internal UdpBitStream(byte[] arr, int size, int offset)
		{
			ptr = offset;
			Data = arr;
			Length = size << 3;
		}

		public bool CanWrite(int bits)
		{
			return ptr + bits <= Length;
		}

		public UdpBitPosition SavePosition()
		{
			return new UdpBitPosition(ptr);
		}

		public void LoadPosition(UdpBitPosition position)
		{
			ptr = position.Ptr;
		}

		// emotitron
		public void Write(UdpByteConverter src, int bits)
		{
			WriteULong(src.Unsigned64, bits);
		}


		public void WriteBool(bool value)
		{
			WriteByte(value ? (byte)1 : (byte)0, 1);
		}

		public bool ReadBool()
		{
			return ReadByte(1) == 1;
		}

		public void WriteByte(byte value, int bits)
		{
			if (bits <= 0)
				return;

			value = (byte)(value & (0xFF >> (8 - bits)));

			int p = ptr >> 3;
			int bitsUsed = ptr & 0x7;
			int bitsFree = 8 - bitsUsed;
			int bitsLeft = bitsFree - bits;

			if (bitsLeft >= 0)
			{
				int mask = (0xFF >> bitsFree) | (0xFF << (8 - bitsLeft));
				Data[p] = (byte)((Data[p] & mask) | (value << bitsUsed));
			}
			else
			{
				Data[p] = (byte)((Data[p] & (0xFF >> bitsFree)) | (value << bitsUsed));
				Data[p + 1] = (byte)((Data[p + 1] & (0xFF << (bits - bitsFree))) | (value >> bitsFree));
			}

			ptr += bits;
		}

		public byte ReadByte(int bits)
		{
			if (bits <= 0)
				return 0;

			XDebug.LogError(!XDebug.logErrors ? null : ("Attemping to read past end of bitstream byte[] buffer."), (ptr + bits > Length));

			byte value;
			int p = ptr >> 3;
			int bitsUsed = ptr % 8;

			if (bitsUsed == 0 && bits == 8)
			{
				value = Data[p];
			}
			else
			{
				int first = Data[p] >> bitsUsed;
				int remainingBits = bits - (8 - bitsUsed);

				if (remainingBits < 1)
				{
					value = (byte)(first & (0xFF >> (8 - bits)));
				}
				else
				{
					int second = Data[p + 1] & (0xFF >> (8 - remainingBits));
					value = (byte)(first | (second << (bits - remainingBits)));
				}
			}

			ptr += bits;
			return value;
		}

		public void WriteByte(byte value)
		{
			WriteByte(value, 8);
		}

		public byte ReadByte()
		{
			return ReadByte(8);
		}

		public void WriteSByte(sbyte value, int bits)
		{
			WriteByte((byte)value, bits);
		}

		public sbyte ReadSByte(int bits)
		{
			return (sbyte)ReadByte(bits);
		}

		public void WriteSByte(sbyte value)
		{
			WriteSByte(value, 8);
		}

		public sbyte ReadSByte()
		{
			return ReadSByte(8);
		}

		public void WriteUShort(ushort value, int bits)
		{
			if (bits <= 8)
			{
				WriteByte((byte)(value & 0xFF), bits);
			}
			else
			{
				WriteByte((byte)(value & 0xFF), 8);
				WriteByte((byte)(value >> 8), bits - 8);
			}
		}

		public ushort ReadUShort(int bits)
		{
			if (bits <= 8)
			{
				return ReadByte(bits);
			}
			else
			{
				return (ushort)(ReadByte(8) | (ReadByte(bits - 8) << 8));
			}
		}

		public void WriteUShort(ushort value)
		{
			WriteUShort(value, 16);
		}

		public ushort ReadUShort()
		{
			return ReadUShort(16);
		}

		public void WriteShort(short value, int bits)
		{
			WriteUShort((ushort)value, bits);
		}

		public short ReadShort(int bits)
		{
			return (short)ReadUShort(bits);
		}

		public void WriteShort(short value)
		{
			WriteShort(value, 16);
		}

		public short ReadShort()
		{
			return ReadShort(16);
		}

		public void WriteChar(char value, int bits)
		{
			UdpByteConverter bytes = value;
			WriteUShort(bytes.Unsigned16, bits);
		}

		public char ReadChar(int bits)
		{
			UdpByteConverter bytes = ReadUShort(bits);
			return bytes.Char;
		}

		public void WriteChar(char value)
		{
			WriteChar(value, 16);
		}

		public char ReadChar()
		{
			return ReadChar(16);
		}

		public void WriteUInt(uint value, int bits)
		{
			byte
				a = (byte)(value >> 0),
				b = (byte)(value >> 8),
				c = (byte)(value >> 16),
				d = (byte)(value >> 24);

			switch ((bits + 7) / 8)
			{
				case 1:
					WriteByte(a, bits);
					break;

				case 2:
					WriteByte(a, 8);
					WriteByte(b, bits - 8);
					break;

				case 3:
					WriteByte(a, 8);
					WriteByte(b, 8);
					WriteByte(c, bits - 16);
					break;

				case 4:
					WriteByte(a, 8);
					WriteByte(b, 8);
					WriteByte(c, 8);
					WriteByte(d, bits - 24);
					break;
			}
		}

		public uint ReadUInt(int bits)
		{
			int
				a = 0,
				b = 0,
				c = 0,
				d = 0;

			switch ((bits + 7) / 8)
			{
				case 1:
					a = ReadByte(bits);
					break;

				case 2:
					a = ReadByte(8);
					b = ReadByte(bits - 8);
					break;

				case 3:
					a = ReadByte(8);
					b = ReadByte(8);
					c = ReadByte(bits - 16);
					break;

				case 4:
					a = ReadByte(8);
					b = ReadByte(8);
					c = ReadByte(8);
					d = ReadByte(bits - 24);
					break;
			}

			return (uint)(a | (b << 8) | (c << 16) | (d << 24));
		}

		public void WriteUInt(uint value)
		{
			WriteUInt(value, 32);
		}

		public uint ReadUInt()
		{
			return ReadUInt(32);
		}

		// emotitron
		public void WriteIntAtPos(int value, int bits, int _Ptr)
		{
			int holdPtr = ptr;
			ptr = _Ptr;
			WriteInt(value, bits);
			ptr = holdPtr;
		}

		// emotitron
		public void WriteUIntAtPos(uint value, int bits, int _Ptr)
		{
			int holdPtr = ptr;
			ptr = _Ptr;
			WriteUInt(value, bits);
			ptr = holdPtr;
		}

		// emotitron
		public void WriteLongAtPos(long value, int bits, int _Ptr)
		{
			int holdPtr = ptr;
			ptr = _Ptr;
			WriteLong(value, bits);
			ptr = holdPtr;
		}

		// emotitron
		public void WriteULongAtPos(ulong value, int bits, int _Ptr)
		{
			int holdPtr = ptr;
			ptr = _Ptr;
			WriteULong(value, bits);
			ptr = holdPtr;
		}

		public void WriteInt(int value, int bits)
		{
			WriteUInt((uint)value, bits);
		}

		public int ReadInt(int bits)
		{
			return (int)ReadUInt(bits);
		}

		public void WriteInt(int value)
		{
			WriteInt(value, 32);
		}

		public int ReadInt()
		{
			return ReadInt(32);
		}

		/*
        public void wEnum32<T> (T value, int bits) where T : struct {
            wS32(udpUtils.enumToInt(value), bits);
        }

        public T rEnum32<T> (int bits) where T : struct {
            return udpUtils.intToEnum<T>(rS32(bits));
        }
        */

		public void WriteULong(ulong value, int bits)
		{
			if (bits <= 32)
			{
				WriteUInt((uint)(value & 0xFFFFFFFF), bits);
			}
			else
			{
				WriteUInt((uint)(value), 32);
				WriteUInt((uint)(value >> 32), bits - 32);
			}
		}

		public ulong ReadULong(int bits)
		{
			if (bits <= 32)
			{
				return ReadUInt(bits);
			}
			else
			{
				ulong a = ReadUInt(32);
				ulong b = ReadUInt(bits - 32);
				return a | (b << 32);
			}
		}

		public void WriteULong(ulong value)
		{
			WriteULong(value, 64);
		}

		public ulong ReadULong()
		{
			return ReadULong(64);
		}

		public void WriteLong(long value, int bits)
		{
			WriteULong((ulong)value, bits);
		}

		public long ReadLong(int bits)
		{
			return (long)ReadULong(bits);
		}

		public void WriteLong(long value)
		{
			WriteLong(value, 64);
		}

		public long ReadLong()
		{
			return ReadLong(64);
		}

		public void WriteHalf(float value)
		{
			WriteUShort(SlimMath.HalfUtilities.Pack(value), 16);
		}

		public float ReadHalf()
		{
			return SlimMath.HalfUtilities.Unpack(ReadUShort(16));
		}

		public void WriteFloat(float value)
		{
			UdpByteConverter bytes = value;
			WriteByte(bytes.Byte0, 8);
			WriteByte(bytes.Byte1, 8);
			WriteByte(bytes.Byte2, 8);
			WriteByte(bytes.Byte3, 8);
		}

		public float ReadFloat()
		{
			UdpByteConverter bytes = default(UdpByteConverter);
			bytes.Byte0 = ReadByte(8);
			bytes.Byte1 = ReadByte(8);
			bytes.Byte2 = ReadByte(8);
			bytes.Byte3 = ReadByte(8);
			return bytes.Float32;
		}

		public void WriteDouble(double value)
		{
			UdpByteConverter bytes = value;
			WriteByte(bytes.Byte0, 8);
			WriteByte(bytes.Byte1, 8);
			WriteByte(bytes.Byte2, 8);
			WriteByte(bytes.Byte3, 8);
			WriteByte(bytes.Byte4, 8);
			WriteByte(bytes.Byte5, 8);
			WriteByte(bytes.Byte6, 8);
			WriteByte(bytes.Byte7, 8);
		}

		public double ReadDouble()
		{
			UdpByteConverter bytes = default(UdpByteConverter);
			bytes.Byte0 = ReadByte(8);
			bytes.Byte1 = ReadByte(8);
			bytes.Byte2 = ReadByte(8);
			bytes.Byte3 = ReadByte(8);
			bytes.Byte4 = ReadByte(8);
			bytes.Byte5 = ReadByte(8);
			bytes.Byte6 = ReadByte(8);
			bytes.Byte7 = ReadByte(8);
			return bytes.Float64;
		}

		// emotitron
		/// <summary>
		/// Reads bits from src stream and writes into this stream.
		/// </summary>
		/// <param name="bits">Number of bits to copy</param>
		public void WriteBitsFromStream(ref UdpBitStream srcbitstream, int bits)
		{
			int firstbits = 8 - srcbitstream.ptr % 8;
			int fullbytes = (bits - firstbits) >> 3;
			int lastbits = bits - (fullbytes << 3) - firstbits;

			WriteByte(srcbitstream.ReadByte(firstbits), firstbits);

			for (int i = 0; i < fullbytes; i ++)
				WriteByte(srcbitstream.ReadByte());

			if (lastbits > 0)
				WriteByte(srcbitstream.ReadByte(lastbits), lastbits);
		}

		public void WriteByteArray(byte[] from)
		{
			WriteByteArray(from, 0, from.Length);
		}

		public void WriteByteArray(byte[] from, int count)
		{
			WriteByteArray(from, 0, count);
		}

		public void WriteByteArray(byte[] from, int offset, int count)
		{
			int p = ptr >> 3;
			int bitsUsed = ptr % 8;
			int bitsFree = 8 - bitsUsed;

			if (bitsUsed == 0)
			{
				Buffer.BlockCopy(from, offset, Data, p, count);
			}
			else
			{
				for (int i = 0; i < count; ++i)
				{
					byte value = from[offset + i];

					Data[p] &= (byte)(0xFF >> bitsFree);
					Data[p] |= (byte)(value << bitsUsed);

					p += 1;

					Data[p] &= (byte)(0xFF << bitsUsed);
					Data[p] |= (byte)(value >> bitsFree);
				}
			}

			ptr += (count * 8);
		}

		public void ReadByteArray(byte[] to)
		{
			ReadByteArray(to, 0, to.Length);
		}

		public void ReadByteArray(byte[] to, int count)
		{
			ReadByteArray(to, 0, count);
		}

		public void ReadByteArray(byte[] to, int offset, int count)
		{
			int p = ptr >> 3;
			int bitsUsed = ptr % 8;

			if (bitsUsed == 0)
			{
				Buffer.BlockCopy(Data, p, to, offset, count);
			}
			else
			{
				int bitsNotUsed = 8 - bitsUsed;

				for (int i = 0; i < count; ++i)
				{
					int first = Data[p] >> bitsUsed;

					p += 1;

					int second = Data[p] & (255 >> bitsNotUsed);
					to[offset + i] = (byte)(first | (second << bitsNotUsed));
				}
			}

			ptr += (count * 8);
		}
	}
}
