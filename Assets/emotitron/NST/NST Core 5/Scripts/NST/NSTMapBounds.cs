//Copyright 2018, Davin Carten, All rights reserved

using System.Collections.Generic;
using UnityEngine;
using emotitron.Utilities;
using emotitron.Compression;
using emotitron.Debugging;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace emotitron.NST
{

	public enum FactorBoundsOn { EnableDisable, AwakeDestroy }
	/// <summary>
	/// Put this object on the root of a game map. It needs to encompass all of the areas the player is capable of moving to.
	/// The object must contain a MeshRenderer in order to get the bounds.
	/// Used by the NetworkSyncTransform to scale Vector3 position floats into integers for newtwork compression.
	/// </summary>
	//[ExecuteInEditMode]
	[System.Obsolete("Use WorldBounds Component instead.")]
	[HelpURL("https://docs.google.com/document/d/1nPWGC_2xa6t4f9P0sI7wAe4osrg4UP0n_9BVYnr5dkQ/edit#heading=h.4n2gizaw79m0")]
	[AddComponentMenu("Network Sync Transform/NST Map Bounds")]
	public class NSTMapBounds : MonoBehaviour
	{

		#region Statics

		public static ElementCrusher boundsPosCrusher;

		static NSTMapBounds()
		{
			//int res = WorldCompressionSettings.Single.minPosResolution;
			boundsPosCrusher = new ElementCrusher(TRSType.Position, false)
			{
				enableLocalSelector = false,
				XCrusher = new FloatCrusher(-100f, 100f, 100, Axis.X, TRSType.Position),
				YCrusher = new FloatCrusher(-20f, 20f, 100, Axis.Y, TRSType.Position),
				ZCrusher = new FloatCrusher(-100f, 100f, 100, Axis.Z, TRSType.Position)
			};
		}
		/// <summary>
		/// All enabled/active NSTMapBounds combined.
		/// </summary>
		public static Bounds combinedWorldBounds;
		///// <summary>
		///// Returns the composite _combinedWorldBounds value, unless there are no active bounds (essentially null)
		///// </summary>
		//public static Bounds CombinedWorldBounds
		//{
		//	get
		//	{
		//		//if (activeMapBoundsObjects == null || ActiveBoundsObjCount == 0)
		//		//	return WorldCompressionSettings.Single.globalPosCrusher.Bounds; // defaultWorldBounds;

		//		return _combinedWorldBounds;
		//	}
		//}

		private static List<NSTMapBounds> activeMapBoundsObjects = new List<NSTMapBounds>();
		public static int ActiveBoundsObjCount { get { return activeMapBoundsObjects.Count; } }
		public static bool muteMessages;


		public static void ResetActiveBounds()
		{
			activeMapBoundsObjects.Clear();
		}

		private static bool warnedOnce;
		/// <summary>
		/// Whenever an instance of NSTMapBounds gets removed, the combinedWorldBounds needs to be rebuilt with this.
		/// </summary>
		public static void RecalculateWorldCombinedBounds()
		{
			// dont bother with any of this if we are just shutting down.
			if (isShuttingDown)
				return;

			if (activeMapBoundsObjects.Count == 0)
			{
				XDebug.LogWarning("There are now no active NSTMapBounds components in the scene.", !warnedOnce);
				warnedOnce = true;
				combinedWorldBounds = new Bounds();
				return;
			}

			warnedOnce = false;

			combinedWorldBounds = activeMapBoundsObjects[0].myBounds;
			for (int i = 1; i < activeMapBoundsObjects.Count; i++)
			{
				combinedWorldBounds.Encapsulate(activeMapBoundsObjects[i].myBounds);
			}


			boundsPosCrusher.XCrusher.Resolution = (ulong)WorldCompressionSettings.Single.minPosResolution;
			boundsPosCrusher.YCrusher.Resolution = (ulong)WorldCompressionSettings.Single.minPosResolution;
			boundsPosCrusher.ZCrusher.Resolution = (ulong)WorldCompressionSettings.Single.minPosResolution;
			boundsPosCrusher.Bounds = combinedWorldBounds;

		}

		public static void UpdateWorldBounds(bool mute = false)
		{
			// No log messages if commanded, if just starting up, or just shutting down.
			WorldCompressionSettings.SetWorldRanges(combinedWorldBounds, muteMessages || mute);
		}

		#endregion



		//public enum BoundsTools { Both, MeshRenderer, Collider }
		public bool includeChildren = true;

		[Tooltip("Awake/Destroy will consider a map element into the world size as long as it exists in the scene (You may need to wake it though). Enable/Disable only factors it in if it is active.")]
		[HideInInspector]
		public BoundsTools.BoundsType factorIn = BoundsTools.BoundsType.Both;

		// sum of all bounds (children included)
		[HideInInspector] public Bounds myBounds;
		[HideInInspector] public int myBoundsCount;


		void Awake()
		{
			// When mapobjects are waking up, this likely means we are seeing a map change. Silence messages until Start().
			muteMessages = true;
			CollectMyBounds();
		}

		public void CollectMyBounds()
		{
			myBounds = BoundsTools.CollectMyBounds(gameObject, factorIn, out myBoundsCount, true, false);

			if (myBoundsCount > 0 && enabled)
			{
				if (!activeMapBoundsObjects.Contains(this))
					activeMapBoundsObjects.Add(this);
			}
			else
			{
				if (activeMapBoundsObjects.Contains(this))
					activeMapBoundsObjects.Remove(this);
			}

		}
		private void Start()
		{
			muteMessages = false;
		}

		private void OnEnable()
		{
			FactorInBounds(true);
		}

		private static bool isShuttingDown;

		void OnApplicationQuit()
		{
			muteMessages = true;
			isShuttingDown = true;
		}

		private void OnDisable()
		{
			FactorInBounds(false);
		}

		private void FactorInBounds(bool b)
		{
			if (this == null)
				return;

			if (b)
			{
				if (!activeMapBoundsObjects.Contains(this))
					activeMapBoundsObjects.Add(this);
			}
			else
			{
				activeMapBoundsObjects.Remove(this);
			}

			RecalculateWorldCombinedBounds();

			// Notify affected classes of the world size change.
			//if (isInitialized && Application.isPlaying)
			UpdateWorldBounds(); // isInitialized is to silence startup log messages
		}

	}

#if UNITY_EDITOR
	[System.Obsolete()]
	[CustomEditor(typeof(NSTMapBounds))]
	[CanEditMultipleObjects]
	public class NSTMapBoundsEditor : NST.NSTHelperEditorBase
	{
		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();

			var _target = (NSTMapBounds)target;

			_target.factorIn = (BoundsTools.BoundsType)EditorGUILayout.EnumPopup("Factor In", _target.factorIn);

			EditorGUILayout.HelpBox(
				"Contains " + _target.myBoundsCount + " bound(s) objects:\n" +
				"Center: " + _target.myBounds.center + "\n" +
				"Size: " + _target.myBounds.size,
				MessageType.None);

			WorldCompressionSettings.Single.DrawGui(target, true, false, true);
		}

		
	}

#endif
}

