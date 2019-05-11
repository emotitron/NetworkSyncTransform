#if PUN_2_OR_NEWER || MIRROR || !UNITY_2019_1_OR_NEWER

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace emotitron.NST
{
	/// <summary>
	/// Interface for any engine component that can queue offtick events (Custom and Casts currently). OffticksPending can be polled to see
	/// if any offtick events are waiting to be sent.
	/// </summary>
	public interface IOfftickSrc
	{
		int OffticksPending { get; }
		NetworkSyncTransform Nst { get; }
	}

}

#endif

