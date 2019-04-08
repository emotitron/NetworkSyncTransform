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
