using System.Runtime.InteropServices;
using System.Collections.Generic;
using UnityEngine;

namespace emotitron.Compression
{
	public static class StructToByteArray
	{
		/// <summary>
		/// This version creates a new byte[] array of the correct size from a structure. This is VERY experimental with .net - it MAY not work for you.
		/// </summary>
		public static byte[] SerializeToByteArray<T>(this T sourcestruct)
			where T : struct
		{
			var size = Marshal.SizeOf(typeof(T));

			if (reusableByteArrays.Length <= size)
				reusableByteArrays = new byte[size + 1][];

			var array = reusableByteArrays[size];
			// if an array of this size doesn't exist yet, add it to the assortedByteArrays[] collection
			if (array == null)
				array = new byte[size];

			var ptr = Marshal.AllocHGlobal(size);
			Marshal.StructureToPtr(sourcestruct, ptr, true);
			Marshal.Copy(ptr, array, 0, size);
			Marshal.FreeHGlobal(ptr);

			return array;
		}

		// The first index is the number of bytes used in the second index, which remains uninitialized until needed.
		private static byte[][] reusableByteArrays = new byte[4][];

		/// <summary>
		/// Convert a byte array back into a structure. Supply a size if a reusable byte[] was used that isn't the same size as the payload.
		/// </summary>
		/// <typeparam name="T">A struct of basic primatives.</typeparam>
		/// <returns></returns>
		public static T DeserializeToStruct<T>(this byte[] array)
			where T : struct
		{
			int size = Marshal.SizeOf(typeof(T));

			var ptr = Marshal.AllocHGlobal(size);
			Marshal.Copy(array, 0, ptr, size);
			var s = (T)Marshal.PtrToStructure(ptr, typeof(T));
			Marshal.FreeHGlobal(ptr);
			return s;
		}
	}
}

