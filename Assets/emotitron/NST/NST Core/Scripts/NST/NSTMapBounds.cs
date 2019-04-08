//Copyright 2018, Davin Carten, All rights reserved

using UnityEngine;
using emotitron.Compression;

#if UNITY_EDITOR
using UnityEditor;
#endif
namespace emotitron.NST
{

	[HelpURL("https://docs.google.com/document/d/1nPWGC_2xa6t4f9P0sI7wAe4osrg4UP0n_9BVYnr5dkQ/edit#heading=h.4n2gizaw79m0")]
	[System.Obsolete("Use WorldBounds Component instead. NST now uses the WorldBounds from TransformCrusher.")]
	[AddComponentMenu("Network Sync Transform/NST Map Bounds")]
	public class NSTMapBounds : WorldBounds
	{

	}


	#if UNITY_EDITOR
	[System.Obsolete()]
	[CustomEditor(typeof(NSTMapBounds))]
	[CanEditMultipleObjects]
	public class NSTMapBoundsEditor : WorldBoundsEditor
	{

	}
	#endif

}


