//Copyright 2018, Davin Carten, All rights reserved

using emotitron.Utilities.BitUtilities;

namespace emotitron.NST
{
	/// <summary>
	/// Two bit state... [Active][Visibile] = currently only used as 00 Dead and 11 Alive, but here for future expansion to
	/// allow for Frozen (visible but not being network updated) and Invisible (not visible but still being updated)
	/// </summary>
	public enum State { Dead, Frozen, Invisible, Alive }

	/// <summary>
	/// Convenience extensions for the State enum
	/// </summary>
	public static class StateExtensions
	{
		public static bool IsAlive(this State s) { return s == State.Alive; }
		public static bool IsActive(this State s) { return ((int)s & 2) != 0; }
		public static bool IsVisible(this State s) { return ((int)s & 1) != 0; }
		public static State SetActive(this State s, bool b) { return (State)BitTools.SetBitInInt((int)s, 1, b); }
		public static State SetVisible(this State s, bool b) { return (State)BitTools.SetBitInInt((int)s, 0, b); }
	}

	/// <summary>
	/// Flag that indicates a frame apply timing. 
	/// OnReceive is immediately when a client/server gets a network update. 
	/// OnStartInterpolate is when the update comes off of the circular frame buffer and interpolation begins.
	/// OnEndInterpolate is when interpolation for the frame ends, which is when the graphics will be in sync with the frame.
	/// </summary>
	public enum ApplyTiming { OnReceiveUpdate, OnStartInterpolate, OnEndInterpolate }

	/// <summary>
	/// Flag to indicate what the Debug widget should be displaying.
	/// </summary>
	public enum DebugXform { None, LocalSend, RawReceive, Uninterpolated, HistorySnapshot }

	/// <summary>
	/// Enum mask flags for Network Updates to indicate additional information that is being included or conveyed.
	/// </summary>

	public enum UpdateType { Regular = 0, RewindCast = 1, Cust_Msg = 2, Teleport = 4 }
	/// <summary>
	/// Convenience extensions for the UpdateType enum
	/// </summary>
	public static class UpdateTypeExtensions
	{
		public static bool IsCustom(this UpdateType m)
		{
			return ((m & UpdateType.Cust_Msg) != 0);
		}

		public static bool IsRewindCast(this UpdateType m)
		{
			return ((m & UpdateType.RewindCast) != 0);
		}

		public static bool IsTeleport(this UpdateType m)
		{
			return ((m & UpdateType.Teleport) != 0);
		}
	}

	/// <summary>
	/// Flag that indicates what kind of culling if any is applied to the Root Position of an Update.
	/// </summary>

	//public enum RootSendType { NoCulling, DropThird, DropTopHalf, DropAll }
	///// <summary>
	///// Convenience extensions for the RootSendType enum
	///// </summary>
	//public static class RootSendTypeExtensions
	//{
	//	public static bool IsLBits(this RootSendType m)
	//	{
	//		return m == RootSendType.DropTopHalf || m == RootSendType.DropThird;
	//	}
	//}
}
