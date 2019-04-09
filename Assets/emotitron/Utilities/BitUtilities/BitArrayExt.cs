//Copyright 2018, Davin Carten, All rights reserved

using System.Collections;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace emotitron.Utilities
{
	public static class BitArrayExt
	{
#if DEBUG || UNITY_EDITOR || DEVELOPMENT_BUILD

		private static StringBuilder str = new StringBuilder(512);

		public static StringBuilder PrintMask(this BitArray ba, int hiliteBit = -1)
		{
			str.Length = 0;
			str.Append("[");
			for (int i = ba.Count - 1; i >= 0; --i)
			{

				if (i == hiliteBit)
					str.Append("<b>").Append((ba[i] ? 1 : 0)).Append("</b>");
				else
					str.Append(ba[i] ? 1 : 0);

				if (i % 32 == 0)
					str.Append((i == 0) ? "]" : "] [");
				else if (i % 8 == 0 && i != 0)
					str.Append(":");
			}

			return str;
		}
#else
	public static StringBuilder PrintMask(this BitArray ba, int hiliteBit = -1)
	{
		return null;
	}

#endif

	}

}
