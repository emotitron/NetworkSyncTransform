#if PUN_2_OR_NEWER || MIRROR || !UNITY_2019_1_OR_NEWER

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace emotitron.NST
{
	public static class NSTTools
	{

		/// <summary>
		/// For max NST settings of 64 or higher, we use a dictionary to for our nstid to NST lookup.
		/// </summary>
		public static Dictionary<uint, NetworkSyncTransform> nstIdToNSTLookup = new Dictionary<uint, NetworkSyncTransform>();
		/// <summary>
		/// For max NST setting of 32 or less, we use an array for our nstid to NST lookup.
		/// </summary>
		public static NetworkSyncTransform[] NstIds;
		/// <summary>
		/// The NST that represents the localPlayer. For PUN there is no actual localPlayer defined, so this is just the first NST created with local Authority.
		/// </summary>
		public static List<NetworkSyncTransform> localNSTs = new List<NetworkSyncTransform>();
		public static List<NetworkSyncTransform> allNsts = new List<NetworkSyncTransform>();
		public static List<NetworkSyncTransform> allNstsWithOfftick = new List<NetworkSyncTransform>();

		// TODO move to helper
		public static void GetNstIdAndSetSyncvar(NetworkSyncTransform nst, NSTNetAdapter na, int assignValue = -1)
		{

			// Server needs to set the syncvar for the NstId
			if (na.IsServer && MasterNetAdapter.NetworkLibrary == NetworkLibrary.UNET)
			{
				if (HeaderSettings.Single.BitsForNstId < 6)
					// If an ID value hasn't been passed, we need to get a free one
					na.NstIdSyncvar = (assignValue != -1) ? (uint)assignValue : (uint)GetFreeNstId();
				else
					na.NstIdSyncvar = na.NetId;
			}
			else
			{
				//TEST should use a method in na to convert the viewid to a smaller nstid
				if (NSTNetAdapter.NetLibrary == NetworkLibrary.PUN || NSTNetAdapter.NetLibrary == NetworkLibrary.PUN2)
				{
					int clientId = (int)na.NetId / 1000;
					int entityId = (int)na.NetId % 1000;
					na.NstIdSyncvar = (uint)((clientId << HeaderSettings.single.bitsForPUNClients) | entityId);
				}
			}

			RegisterNstId(na.NstIdSyncvar, nst);
		}

		public static void RegisterNstId(uint nstid, NetworkSyncTransform nst)
		{
			// If the nstid array is null - it needs to be created. Leave the nst array null for unlimited - that uses the dictionary.
			if (NstIds == null && HeaderSettings.Single.BitsForNstId < 6)
				NstIds = new NetworkSyncTransform[HeaderSettings.Single.MaxNSTObjects];


			if (HeaderSettings.Single.BitsForNstId < 6)
				NstIds[nstid] = nst;

			else if (!nstIdToNSTLookup.ContainsKey(nstid))
				nstIdToNSTLookup.Add(nstid, nst);

			allNsts.Add(nst);

			if (nst.allowOfftick)
				allNstsWithOfftick.Add(nst);
		}

		public static void UnregisterNstId(NetworkSyncTransform nst, NSTNetAdapter na)
		{
			//uint nstid = nst.NstId;
			// Don't try to remove this nst from lookups/arrays if it was never added.
			if (!allNsts.Contains(nst))
				return;

			if (na != null)
			{
				uint nstid = na.NstIdSyncvar;
				if (nstIdToNSTLookup != null && nstIdToNSTLookup.ContainsKey(nstid))
					nstIdToNSTLookup.Remove(nstid);

				if (NstIds != null)
					NstIds[nstid] = null;
			}

			if (allNsts != null && allNsts.Contains(nst))
				allNsts.Remove(nst);

			if (allNstsWithOfftick != null && allNstsWithOfftick.Contains(nst))
				allNstsWithOfftick.Remove(nst);

			if (localNSTs != null && localNSTs.Contains(nst))
				localNSTs.Remove(nst);
		}


		// Public methods for looking up game objects by the NstID
		public static NetworkSyncTransform GetNstFromId(uint id)
		{
//#if UNITY_EDITOR
//			// this test won't be needed at runtime since NSTSettings will already be up and running
//			if (MasterNetAdapter.NetworkLibrary == NetworkLibrary.UNET)
//				NSTSettings.EnsureExistsInScene(NSTSettings.DEFAULT_GO_NAME);
//#endif
			// 5 bits (32 objects) is the arbitrary cutoff point for using an array for the lookup. For greater numbers the dictionary is used instead.
			if (HeaderSettings.single.BitsForNstId > 5)
			{
				return (nstIdToNSTLookup.ContainsKey(id)) ? nstIdToNSTLookup[id] : null;
			}
			else
			{
				return (NstIds == null || id >= NstIds.Length) ? null : NstIds[(int)id];
			}
		}

		// Save the last new dictionary opening found to avoid retrying to same ones over and over when finding new free keys.
		private static int nstDictLastCheckedPtr;
		/// <summary>
		/// Return an unused NstId. Used by the server when mapping network identities to indexed values.
		/// </summary>
		/// <returns></returns>
		public static int GetFreeNstId()
		{
			if (HeaderSettings.single.BitsForNstId < 6)
			{
				// If the nstid array is null - this has been called before any nsts have initialized. Need to create the array.
				if (NstIds == null)
					NstIds = new NetworkSyncTransform[HeaderSettings.single.MaxNSTObjects];

				for (int i = 0; i < NstIds.Length; i++)
				{
					if (NstIds[i] == null)
						return i;
				}
			}
			else
			{
				for (int i = 0; i < 64; i++)
				{
					int offseti = (int)((i + nstDictLastCheckedPtr + 1) % HeaderSettings.single.MaxNSTObjects);
					if (!nstIdToNSTLookup.ContainsKey((uint)offseti) || nstIdToNSTLookup[(uint)offseti] == null)
					{
						nstDictLastCheckedPtr = offseti;
						return offseti;
					}
				}
			}

			Debug.LogError("No more available NST ids. Increase the number Max Nst Objects in NST Settings, or your game will be VERY broken.");
			return -1;
		}

		/// <summary>
		/// Internal method used at startup to destroy unintentional NST scene objects if nst.destroyUnspawned == true. Network objects in the scene for UNET and PUN
		/// become server objects, which may not be desired. This is useful for example if you are testing settings of your Player prefab... and would like to edit it in the scene,
		/// but you don't actually want it in the scene at startup, and don't want to have to delete it every time you hit Play.
		/// </summary>
		public static void DestroyAllNSTsInScene()
		{
			DestroyAllNSTsInScene(SceneManager.GetActiveScene());
		}
		/// <summary>
		/// Internal method used at startup to destroy unintentional NST scene objects if nst.destroyUnspawned == true. Network objects in the scene for UNET and PUN
		/// become server objects, which may not be desired. This is useful for example if you are testing settings of your Player prefab... and would like to edit it in the scene,
		/// but you don't actually want it in the scene at startup, and don't want to have to delete it every time you hit Play.
		/// </summary>
		public static void DestroyAllNSTsInScene(Scene scene)
		{
			NetworkSyncTransform[] nsts = Resources.FindObjectsOfTypeAll<NetworkSyncTransform>();

			for (int i = 0; i < nsts.Length; i++)
			{
				NetworkSyncTransform nst = nsts[i];
				if (nst.destroyUnspawned && nst.gameObject.scene == scene)
				{
					nst.hasBeenDestroyed = true;
					Object.Destroy(nst.gameObject);
				}
			}
		}

		public static List<Component> reusableComponentList = new List<Component>();
		public static void CollectCallbackInterfaces(NetworkSyncTransform nst)
		{
			/// Collect all components on this NST, and scour them for Interface Callbacks
			nst.GetComponentsInChildren(true, reusableComponentList);

			int cnt = reusableComponentList.Count;
			for (int i = 0; i < cnt; ++i)
			{
				var c = reusableComponentList[i];
				RegisterCallbackInterfaces(nst, c, true);
			}
		}

		private static void AddCallback<T>(List<T> cb, Component c, bool skipExistingCheck = false, bool unregister = false) where T : class
		{
			if (unregister)
			{
				if (c is T && (cb.Contains(c as T)))
					cb.Remove(c as T);

			}
			else
			{
				if (c is T && (skipExistingCheck || !cb.Contains(c as T)))
					cb.Add(c as T);
			}
			
		}

		/// <summary>
		/// Register any NST Callbacks on a component. Use this if the component was created AFTER NST startup, and it wasn't automatically found.
		/// </summary>
		/// <param name="nst"></param>
		/// <param name="c"></param>
		public static void RegisterCallbackInterfaces(NetworkSyncTransform nst, Component c, bool skipExistingCheck = true, bool skipAddingAwake = false)
		{
			/// Skip adding awake when this was called by something running off the NSTAwake timing... will cause list out of sync errors.
			if (!skipAddingAwake)
			{
				AddCallback(nst.iNstAwake, c, skipExistingCheck);
			}

			AddCallback(nst.iOfftickSrc, c, skipExistingCheck);
			AddCallback(nst.iNstState, c, skipExistingCheck);
			AddCallback(nst.iNstUpdate, c, skipExistingCheck);
			AddCallback(nst.iNstPreUpdate, c, skipExistingCheck);
			AddCallback(nst.iNstPostUpdate, c, skipExistingCheck);
			AddCallback(nst.iNstPreLateUpdate, c, skipExistingCheck);
			AddCallback(nst.iNstPostLateUpdate, c, skipExistingCheck);
			AddCallback(nst.iNstPrePollForUpdate, c, skipExistingCheck);
			AddCallback(nst.iNstStart, c, skipExistingCheck);
			AddCallback(nst.iNstOnStartServer, c, skipExistingCheck);
			AddCallback(nst.iNstOnStartClient, c, skipExistingCheck);
			AddCallback(nst.iNstOnStartLocalPlayer, c, skipExistingCheck);
			AddCallback(nst.iNstOnNetworkDestroy, c, skipExistingCheck);
			AddCallback(nst.iNstOnDestroy, c, skipExistingCheck);
			AddCallback(nst.iBitstreamInjectFirst, c, skipExistingCheck);
			AddCallback(nst.iBitstreamInjectSecond, c, skipExistingCheck);
			AddCallback(nst.iBitstreamInjectsLate, c, skipExistingCheck);
			AddCallback(nst.iGenerateUpdateType, c, skipExistingCheck);
			AddCallback(nst.iNstOnExtrapolate, c, skipExistingCheck);
			AddCallback(nst.iNstOnReconstructMissing, c, skipExistingCheck);
			AddCallback(nst.iNstOnSndUpdate, c, skipExistingCheck);
			AddCallback(nst.iNstOnRcvUpdate, c, skipExistingCheck);
			//AddCallback(nst.iNstOnOwnerIncomingRoot, c, skipExistingCheck);
			AddCallback(nst.iNstOnSvrOutgoingRoot, c, skipExistingCheck);
			AddCallback(nst.iNstOnSnapshotToRewind, c, skipExistingCheck);
			AddCallback(nst.iNstOnStartInterpolate, c, skipExistingCheck);
			AddCallback(nst.iNstOnInterpolate, c, skipExistingCheck);
			AddCallback(nst.iNstOnEndInterpolate, c, skipExistingCheck);
			AddCallback(nst.iNstOnSvrInterpRoot, c, skipExistingCheck);
			AddCallback(nst.iNstTeleportApply, c, skipExistingCheck);
			AddCallback(nst.iNstOnTeleportApply, c, skipExistingCheck);
			AddCallback(nst.iNstOnFirstAppliedFrameZero, c, skipExistingCheck);
		}

		public static void UnregisterCallbackInterfaces()
		{

		}
	}
}

#endif