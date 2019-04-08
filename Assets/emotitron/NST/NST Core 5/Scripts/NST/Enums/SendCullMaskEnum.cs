//Copyright 2018, Davin Carten, All rights reserved

using emotitron.Utilities.BitUtilities;

namespace emotitron.NST
{
	[System.Flags]
	public enum SendCullMask { EveryTick = 1, OnChanges = 2, OnTeleport = 4, OnCustomMsg = 8, OnRewindCast = 16 }

	public static class SendCullExtensions
	{
		public static bool EveryTick(this SendCullMask mask)
		{
			return BitTools.GetBitInMask((int)mask, 0);
		}
		public static bool OnChanges(this SendCullMask mask)
		{
			return BitTools.GetBitInMask((int)mask, 1);
		}
		public static bool OnTeleport(this SendCullMask mask)
		{
			return BitTools.GetBitInMask((int)mask, 2);
		}
		public static bool OnCustomMsg(this SendCullMask mask)
		{
			return BitTools.GetBitInMask((int)mask, 3);
		}
		public static bool OnRewindCast(this SendCullMask mask)
		{
			return BitTools.GetBitInMask((int)mask, 4);
		}
	}


}

