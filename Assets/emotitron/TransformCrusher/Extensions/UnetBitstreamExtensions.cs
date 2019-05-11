#if MIRROR
using Mirror;
#elif !UNITY_2019_1_OR_NEWER
using UnityEngine.Networking;
#endif

namespace emotitron.Compression
{
#if !UNITY_2019_1_OR_NEWER

	public static class UnetBitstreamExtensions
	{
#pragma warning disable 0618
		/// <summary>
		/// Write the used bytes (based on the writer position) to the NetworkWriter.
		/// </summary>
		public static void Write(this NetworkWriter writer, ref Bitstream bitstream)
		{
			// Write the packed bytes from the bitstream into the UNET writer.
			int count = bitstream.BytesUsed;
			for (int i = 0; i < count; ++i)
			{
				writer.Write(bitstream.ReadByte());
			}
		}

		/// <summary>
		/// Write the used bytes (based on the bitposition) to the NetworkWriter. Will copy the buffer to the writer starting
		/// at buffer zero.
		/// </summary>
		public static void Write(this NetworkWriter writer, byte[] buffer, ref int bitposition)
		{
			// Write the packed bytes from the bitstream into the UNET writer.
			int count = bitposition / 8;
			for (int i = 0; i < count; ++i)
			{
				writer.Write(buffer[i]);
			}
			int overflow = bitposition % 8;
			if (overflow > 0)
			{
				byte mask = (byte)(255 >> (8 - overflow));
				writer.Write(buffer[count] & mask);
			}
		}

		/// <summary>
		/// Read up to 40 bytes of a NetworkReader into a bitstream. This resets the bitstream before copying.
		/// </summary>
		/// <param name="reader"></param>
		/// <param name="bitstream"></param>
		public static void Read(this NetworkReader reader, ref Bitstream bitstream)
		{
			bitstream.Reset();
			// Copy the reader into our buffer so we can extra the packed bits. UNET uses a byte reader so we can't directly read bit fragments out of it.
			int count = System.Math.Min(40, reader.Length);
			for (int i = (int)reader.Position; i < count; ++i)
			{
				byte b = reader.ReadByte();
				bitstream.WriteByte(b);
			}
		}


		///// <summary>
		///// Read the entire contents of a NetworkReader into a byte[] buffer. Bitposition is reset to zero at start of write and will reflect the final write position after the copy.
		///// </summary>
		///// <param name="reader"></param>
		///// <param name="buffer"></param>
		///// <param name="bitposition"></param>
		///// <returns>Returns the used buffer, which may be a new reference if it was resized.</returns>
		//public static void Read(this UnityEngine.Networking.NetworkReader reader, ref byte[] buffer, ref int bitposition)
		//{
		//	bitposition = 0;
		//	// Copy the reader into our buffer so we can extra the packed bits. UNET uses a byte reader so we can't directly read bit fragments out of it.
		//	int count = reader.Length;
		//	int bufferlen = buffer.Length;
		//	if (count > bufferlen)
		//	{
		//		//emotitron.Debugging.XDebug.LogError("Supplied buffer is shorter than the NetworkReader buffer, so copy will be limited to " + bufferlen + " bytes");
		//		emotitron.Debugging.XDebug.LogError("Supplied buffer is shorter than the NetworkReader buffer, so copy will be limited to " + bufferlen + " bytes");
		//		System.Array.Resize(ref buffer, System.Math.Max(buffer.Length * 2, count));

		//		count = bufferlen;
		//	}
		//	for (int i = (int)reader.Position; i < count; ++i)
		//	{
		//		byte b = reader.ReadByte();
		//		buffer.Write(b, ref bitposition, 8);
		//	}
		//}

		/// <summary>
		/// Read the entire contents of a NetworkReader into a byte[] buffer. Bitposition is reset to zero at start of write and will reflect the final write position after the copy.
		/// </summary>
		/// <param name="reader"></param>
		/// <param name="buffer"></param>
		/// <param name="bitposition"></param>
		/// <returns>Returns the used buffer, which may be a new reference if it was resized.</returns>
		public static void Read(this NetworkReader reader, byte[] buffer, ref int bitposition)
		{
			bitposition = 0;
			// Copy the reader into our buffer so we can extra the packed bits. UNET uses a byte reader so we can't directly read bit fragments out of it.
			int count = reader.Length;
			int bufferlen = buffer.Length;
			if (count > bufferlen)
			{
				//emotitron.Debugging.XDebug.LogError("Supplied buffer is shorter than the NetworkReader buffer, so copy will be limited to " + bufferlen + " bytes");
				Debugging.XDebug.LogError(!Debugging.XDebug.logErrors ? null : ("Supplied buffer is shorter than the NetworkReader buffer, so copy will be limited to " + bufferlen + " bytes"));
			}
			for (int i = (int)reader.Position; i < count; ++i)
			{
				byte b = reader.ReadByte();
				buffer.Write(b, ref bitposition, 8);
			}
		}
#pragma warning restore 0618

	}
#endif
}

