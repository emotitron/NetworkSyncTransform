//Copyright 2018, Davin Carten, All rights reserved

#if PUN_2_OR_NEWER || MIRROR || !UNITY_2019_1_OR_NEWER

using UnityEngine;

namespace emotitron.NST.Rewind
{
	/// <summary>
	/// Callbacks from the RewindEngine.
	/// </summary>
	public interface IRewind
	{
		/// <summary>
		///  Used by the rewind engine to order all rewind related objects to populate their history[0] with the state at time of rewind.
		/// </summary>
		void OnRewind(HistoryFrame frameElements, int startFrameId, int endFrameId, float timeBeforeSnapshot, float remainder, bool applyToGhost);
	}

	public interface IRewindGhostsToFrame
	{
		/// <summary>
		/// Rewind engine callback when objects are to be rewound to the state of things as they are in the supplied frame.
		/// </summary>
		void OnRewindGhostsToFrame(Frame frame);
	}

	
	public interface ICreateRewindGhost
	{
		/// <summary>
		/// Callback from rewind engine every time a GhostGO (parent and children) is created during the ghost creation process.
		/// </summary>
		void OnCreateGhost(GameObject srcGO, GameObject ghostGO);
	}
}

#endif