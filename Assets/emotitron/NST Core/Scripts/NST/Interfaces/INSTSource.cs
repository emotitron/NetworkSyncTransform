#if PUN_2_OR_NEWER || MIRROR || !UNITY_2019_1_OR_NEWER


using UnityEngine;

namespace emotitron.NST
{
	/// <summary>
	/// Interface that allows RewindEngine to treat RewindGhosts and actual NST objects the same.
	/// </summary>
	public interface INstSource
	{
		GameObject SrcGameObject { get; }
		NetworkSyncTransform Nst { get; }
		uint NstId { get; }
	}
}

#endif
