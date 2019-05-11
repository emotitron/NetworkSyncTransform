//Copyright 2018, Davin Carten, All rights reserved

#if PUN_2_OR_NEWER || MIRROR || !UNITY_2019_1_OR_NEWER

using System.Collections.Generic;
using UnityEngine;
using emotitron.Utilities.SmartVars;
using emotitron.Compression;
using emotitron.NST.Rewind;
using emotitron.Debugging;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace emotitron.NST
{
	/// <summary>
	/// Root NST Component that collects all NSTRotation and NSTPosition components on fires them accordingly to the callback interfaces of the NST.
	/// </summary>
	public class NSTElementsEngine : NSTRootSingleton<NSTElementsEngine>, INstBitstreamInjectFirst, INstOnExtrapolate, INstOnReconstructMissing, INstOnStartInterpolate, INstOnInterpolate, //, INstOnSndUpdate, INstOnRcvUpdate, INstOnEndInterpolate
			 INstOnSndUpdate, IRewind, IRewindGhostsToFrame, INstOnSnapshotToRewind, ICreateRewindGhost, INstTeleportApply
	{
		[HideInInspector] public TransformElement[] transformElements;

		// TODO hash these strings or String.Intern or something ?
		public Dictionary<string, int> elementIdLookup;
		public Dictionary<string, TransformElement> elementLookup;

		// cache some values
		[System.NonSerialized] public int elementCount;

		[HideInInspector] public int frameCount;

		public GenericX GetRewind(int elementId, int frameid)
		{
			return transformElements[elementId].history[frameid];
		}

		private void Awake()
		{
			Initialize();
		}

		//public override void OnNstPostAwake()
		//{
		//	base.OnNstPostAwake();
		//	//Initialize();

		//}

		[System.NonSerialized]
		private bool initialized;
		[System.NonSerialized]
		public bool[] cache_elementIsEnabled;

		public NSTElementsEngine Initialize()
		{
			if (initialized)
				return this;

			initialized = true;

			// This is redundant with the base class, but with initialize in Awake() rather than NSTAwake not assurance currently that it is set. 
			nst = GetComponent<NetworkSyncTransform>();

			frameCount = NSTMaster.FRAME_COUNT / nst.sendEveryXTick;
			
			// Collect all of the transform elements
			INSTTransformElement[] iTransElement = GetComponentsInChildren<INSTTransformElement>(true);
			elementCount = iTransElement.Length;

			elementIdLookup = new Dictionary<string, int>(elementCount);
			elementLookup = new Dictionary<string, TransformElement>(elementCount);

			transformElements = new TransformElement[elementCount];
			cache_elementIsEnabled = new bool[elementCount];

			for (int i = 0; i < elementCount; ++i)
			{
				TransformElement te = iTransElement[i].TransElement;

				cache_elementIsEnabled[i] =
					te.sendCullMask != 0 &&
					te.keyRate != 0 &&
					te.crusher.Enabled;

				if (elementIdLookup.ContainsKey(te.name))
				{
					XDebug.LogError(!XDebug.logErrors ? null : 
						("Multiple child elements with the same name on '" + nst.name + "'. Check the names of Rotation and Positon elements for any repeats and be sure they all have unique names."));
				}
				else
				{
					elementIdLookup.Add(te.name, i);
					elementLookup.Add(te.name, te);
				}

				
				//// Make note of which of the transforms belongs to the NST root rotation
				//if (System.Object.ReferenceEquals(te, nst.rootRotationElement))
				//{
				//	Debug.Log("ROOT NST ROTATION FOUND");
				//	//transformElements[0] = transformElements[i];
				//}

				transformElements[i] = te;
				transformElements[i].index = i;

				if (transformElements[i].gameobject == null)
					transformElements[i].gameobject = iTransElement[i].SrcGameObject;

				transformElements[i].Initialize(nst, iTransElement[i]);

				//Debug.Log(nst.rootRotationElement.frames[0]);

				//// TODO: Questionable and stupid hack that replaces NST rootRotationElement with the found interface version (should be the same but arent)
				//if (iTransElement[i] is NetworkSyncTransform)
				//	nst.rootRotationElement = transformElements[i] as RotationElement;
			}


			// init the list
			//history = new GenericX[frameCount + 1][];
			//for (int frameid = 0; frameid < history.Length; frameid++)
			//{
			//	history[frameid] = new GenericX[transformElements.Length];
			//	for (int elementid = 0; elementid < transformElements.Length; elementid++)
			//	{
			//		history[frameid][elementid] = new GenericX();
			//	}
			//}

			//int numbOfElements = transformElements.Length;

			//elements = new List<XElement>[frameCount + 1];
			//for (int fid = 0; fid < elements.Length; fid++)
			//{
			//	elements[fid] = new List<XElement>(numbOfElements);
			//	List<XElement> frameElements = elements[fid];

			//	for (int eid = 0; eid < numbOfElements; eid++)
			//	{
			//		frameElements.Add(new XElement(
			//			transformElements[eid].Localized,
			//			transformElements[eid].Compress(),
			//			false,
			//			transformElements[eid]
			//			));
			//	}
			//}
			//elements = new List<XElement>(numbOfElements);


			return this;
		}

		public void NSTBitstreamOutgoingFirst(Frame frame, ref UdpBitStream bitstream)
		{
			for (int eid = 0; eid < elementCount; ++eid)
			{
				TransformElement te = transformElements[eid];

				if (!cache_elementIsEnabled[eid])
					continue;

				
				te.Write(ref bitstream, frame);

				//// Write to the local buffer
				//te.frames[frame.frameid].xform = te.Localized;
			}
		}

		// callback from NST, extract transform elements
		public void NSTBitstreamIncomingFirst(Frame frame, Frame currFrame, ref UdpBitStream bitstream, bool isServer)
		{
			for (int eid = 0; eid < elementCount; ++eid)
			{
				TransformElement te = transformElements[eid];

				if (!cache_elementIsEnabled[eid])
					continue;

				te.frames[frame.frameid].hasChanged = te.Read(ref bitstream, frame, currFrame);
			}
		}

		public void NSTBitstreamMirrorFirst(Frame frame, ref UdpBitStream outstream, bool waitingForTeleportConfirm)
		{

			for (int eid = 0; eid < elementCount; ++eid)
			{
				TransformElement te = transformElements[eid];

				if (!cache_elementIsEnabled[eid])
					continue;

				te.MirrorToClients(ref outstream, frame, te.frames[frame.frameid].hasChanged); // masks[eid].GetBitInMask(frame.frameid));
			}
		}

		//public void OnSvrTeleportCmd()
		//{
		//	for (int eid = 0; eid < elementCount; ++eid)
		//	{
		//		TransformElement te = transformElements[eid];
		//		if (te.teleportOverride)
		//			te.Teleport();
		//	}

		//}

		public void OnTeleportApply(Frame frame)
		{
			for (int eid = 0; eid < elementCount; ++eid)
			{
				TransformElement te = transformElements[eid];

				if (!cache_elementIsEnabled[eid])
					continue;

				//TODO this should be checking for the elements mask?
				// TODO: Uncertain about this check for null - should be testing for whether or not this element has any info to teleport with.
				if (te.teleportOverride)
				{
					te.Teleport(frame);
				}
			}
		}

		/// <summary>
		/// Teleport all elements that are flagged with teleportOverride = true;
		/// </summary>
		public void OnRcvSvrTeleportCmd(Frame frame)
		{
			for (int eid = 0; eid < elementCount; ++eid)
			{
				TransformElement te = transformElements[eid];

				if (!cache_elementIsEnabled[eid])
					continue;

				// TODO: this likely is only wired to work correctly with offtick
				if (te.teleportOverride)
				{
					te.Teleport(frame);
				}
			}
		}

		public void OnStartInterpolate(Frame frame, bool lateArrival = false, bool midTeleport = false)
		{
			// Don't apply the transform for frame offtick updates. Those are for teleports and weapon fire.
			//TODO: Likely no longer needed, frame 0 shoud only fire onrcv
			//if (frame.frameid == 0)// && !frame.updateType.IsTeleport())
			//	return;

			for (int eid = 0; eid < elementCount; ++eid)
			{
				TransformElement te = transformElements[eid];

				if (!cache_elementIsEnabled[eid])
					continue;

				// Don't overwrite mid interpolation if this is a teleport override element, and we are mid teleport.
				if (lateArrival && midTeleport && te.teleportOverride)
					continue;

				// Don't modify elements with late arriving data if it is null.
				if (lateArrival && te.frames[frame.frameid].xform.type == XType.NULL)
				{
					XDebug.Log(!XDebug.logInfo ? null :
						(Time.time + " <b>Null Late Arrival - NOTE if you keep seeing this davin - remove this test otherwise </b> " + te.snapshotFrameId + " " + te.targetFrameId));

					continue;
				}

				te.Snapshot(frame, lateArrival, midTeleport);
			}
		}

		public void OnInterpolate(float t)
		{
			for (int eid = 0; eid < elementCount; ++eid)
			{
				TransformElement te = transformElements[eid];

				if (!cache_elementIsEnabled[eid])
					continue;

				te.UpdateInterpolation(t);
			}
		}
		
		/// <summary>
		/// Extrapolate is used when the buffer is empty.
		/// </summary>
		public void OnExtrapolate(Frame targFr, Frame currFr, int extrapolationCount, bool svrWaitingForTeleportConfirm)
		{
			for (int eid = 0; eid < elementCount; ++eid)
			{
				TransformElement te = transformElements[eid];

				if (!cache_elementIsEnabled[eid])
					continue;

				bool currIsNull = te.frames[currFr.frameid].xform.type == XType.NULL;

				// repeat teleportoverride if this is server and is waiting for a teleport confirm
				bool svrWaiting = svrWaitingForTeleportConfirm && te.teleportOverride; // || currFr.updateType.IsTeleport() && te.teleportOverride;

				// Don't extrapolate if this was a teleport or if we are exceeded the max number of sequential extrapolates
				bool dontExtrapolate = (currFr.updateType.IsTeleport() || extrapolationCount >= te.maxExtrapolates);

				te.frames[targFr.frameid].xform =
					svrWaiting ? te.lastSentTransform :
					dontExtrapolate ? currIsNull ? te.Localized : te.frames[currFr.frameid].xform : // If the current frame we are extrapolating was a teleport... just copy
					te.Extrapolate();

				var cxf = te.frames[targFr.frameid].compXform;
				if (svrWaiting)
					cxf.CopyFrom(te.lastSentCompressed);
				else if (dontExtrapolate)
					cxf.CopyFrom(te.frames[currFr.frameid].compXform);
				else
					te.Compress(cxf, te.frames[targFr.frameid].xform);

				//te.frames[targFr.frameid].compXform = 
				//	svrWaiting ? te.lastSentCompressed :
				//	dontExtrapolate ? te.frames[currFr.frameid].compXform : // If the current frame we are extrapolating was a teleport... just copy
				//	te.Compress(te.frames[targFr.frameid].xform);

			}
		}

		/// <summary>
		/// Unlike Extrapolate - Reconstruct is used when the buffer isn't empty, but rather we are dealing with a lost packet while there is a future frame in the buffer.
		/// </summary>
		public void OnReconstructMissing(Frame nextFrame, Frame currentFrame, Frame nextValidFrame, float t, bool svrWaitingForTeleportConfirm)
		{
			//List<XElement> currFrameElements = elements[currentFrame.frameid];
			//List<XElement> nextFrameElements = elements[nextFrame.frameid];
			//List<XElement> nextValidFrameElements = elements[nextValidFrame.frameid];

			// Reconstruct missing frames
			for (int eid = 0; eid < elementCount; ++eid)
			{
				TransformElement te = transformElements[eid];

				if (!cache_elementIsEnabled[eid])
					continue;

				TransformElement.ElementFrame ce = te.frames[currentFrame.frameid]; // currFrameElements[eid];
				TransformElement.ElementFrame ne = te.frames[nextFrame.frameid]; // nextFrameElements[eid];
				TransformElement.ElementFrame nve = te.frames[nextValidFrame.frameid];


				//TODO are these if's needed for the null checking? Keep an eye on this.
				// Eliminate any Null genericX values = they indicate no changes
				if (ce.xform.type == XType.NULL)
				{
					ce.xform = te.Localized;
					te.Compress(ce.compXform, ce.xform);

					Debug.Log("Current element is null");
				}

				if (nve.xform.type == XType.NULL)
				{
					nve.xform = ce.xform;
					//Debug.LogError("nextvalid element is null");
				}


				// If server his holding for teleport confirm, keep using the same teleport value
				if (svrWaitingForTeleportConfirm && te.teleportOverride)
				{
					ne.xform = te.lastSentTransform;
					ne.compXform.CopyFrom(te.lastSentCompressed);
				}
				// There is a future frame to use as a guess target
				else if (nve.xform.type != XType.NULL) // nst.buffer.masks[eid].GetBitInMask(nextValidFrame.frameid))
				{
					ne.xform = te.Lerp(ce.xform, nve.xform, t);
					te.Compress(ne.compXform, ne.xform);
				}
				// There is no future frame.
				else
				{
					if (ce.xform.type == XType.NULL)
						Debug.Log("Houston we have a null here.");

					ne.xform = ce.xform;
					ne.compXform.CopyFrom(ce.compXform);
				}
			}
		}

		public void OnRewindGhostsToFrame(Frame frame)
		{
			for (int eid = 0; eid < elementCount; ++eid)
			{
				TransformElement te = transformElements[eid];

				if (!cache_elementIsEnabled[eid])
					continue;

				te.Apply(te.frames[frame.frameid].xform, te.ghostGO);
			}
		}

		/// <summary>
		/// If a Rewind request has been made, this callback interface is called on all registered elements. Each element will populate its history[0] frame with the resuts of the requested rewind time.
		/// If applyToGhost is true, it will also apply its rewound result to its element on the rewindGhost for this NST.
		/// </summary>
		public void OnRewind(HistoryFrame fe, int startFrameid, int endFrameId, float timeBeforeSnapshot, float remainder, bool applyToGhost)
		{
			for (int eid = 0; eid < elementCount; ++eid)
			{
				TransformElement te = transformElements[eid];

				if (!cache_elementIsEnabled[eid])
					continue;

				//TODO: this needs to slerp for rotation types
				te.history[frameCount] = (timeBeforeSnapshot > 0) ?
					Vector3.Lerp(te.history[startFrameid], te.history[endFrameId], remainder) :
					Vector3.Lerp(te.history[startFrameid], te.Localized, -remainder);

				if (applyToGhost)
				{
					te.Apply(te.history[frameCount], te.ghostGO);
				}
			}
		}

		// Snapshot local auth objects on send, since they don't interpolate. 
		public void OnSnd(Frame frame)
		{
			if (/*MasterNetAdapter.ServerIsActive*/ na.IAmActingAuthority && frame.frameid != nst.buffer.nstFrameCount)
			{
				OnSnapshotToRewind(frame);
			}
		}

		public void OnSnapshotToRewind(Frame frame)
		{
			for (int eid = 0; eid < elementCount; ++eid)
			{
				TransformElement te = transformElements[eid];

				if (!cache_elementIsEnabled[eid])
					continue;

				te.history[frame.frameid] = te.Localized;
			}
		}

		public void OnCreateGhost(GameObject srcGO, GameObject ghostGO)
		{
			for (int eid = 0; eid < elementCount; ++eid)
			{
				TransformElement te = transformElements[eid];

				if (!cache_elementIsEnabled[eid])
					continue;

				if (srcGO == te.gameobject)
					te.ghostGO = ghostGO;
			}
		}
	}


#if UNITY_EDITOR

	[CustomEditor(typeof(NSTElementsEngine))]
	[CanEditMultipleObjects]
	public class NSTElementsEngineEditor : NSTHeaderEditorBase
	{
		public override void OnEnable()
		{
			headerName = HeaderElementsEngineName;
			headerColor = HeaderEngineColor;
			base.OnEnable();
		}
	}
#endif
}



#endif