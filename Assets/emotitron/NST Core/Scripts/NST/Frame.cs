//Copyright 2018, Davin Carten, All rights reserved

#if PUN_2_OR_NEWER || MIRROR || !UNITY_2019_1_OR_NEWER

using UnityEngine;
using emotitron.Utilities.SmartVars;
using UnityEngine.Events;
using emotitron.Compression;

namespace emotitron.NST
{
	public class HistoryFrame
	{
		public int frameid;
		public float endTime;
		public Vector3 rootPosition;

		public HistoryFrame(int _frameid, Vector3 _pos, Quaternion _rot)
		{
			frameid = _frameid;
			rootPosition = _pos;
		}
	}

	public class Frame : UnityEvent<Frame>
	{
		public readonly NetworkSyncTransform nst;
		public readonly NSTElementsEngine ee;
		public readonly int frameid;
		public readonly TransformElement.ElementFrame rootRotElementFrame;

		private readonly bool checkSceneIndex;
		public int sceneIndex;

		public float packetArriveTime;
		public float appliedTime;
		public float endTime;
		public UpdateType updateType;
		public BitCullingLevel rootBitCullLevel;

		[System.NonSerialized]
		public CompressedElement compPos = new CompressedElement();
		[System.NonSerialized]
		public ElementCrusher rootPosCrusher;

		public State state;

		// Reference to ElementsEngine.transformElements[]
		//private readonly TransformElement[] tes;

		public Vector3 rootPos;
		public byte[] customData;
		public int customMsgSize;
		public int customMsgPtr;

		/// <summary>
		/// Sets both the root position and compressed root position values with one call.
		/// </summary>
		public Vector3 RootPos
		{
			get { return rootPos; }
			set
			{
				var crusher = (NSTSceneManager.CurrentSceneIndex == sceneIndex) ? rootPosCrusher : null;
				crusher.Compress(compPos, value);
				/// TODO: decide once and for all if we want to store rootPos as lossy or raw.
				//OverwriteRootPos(crusher.Decompress(compPos));
				OverwriteRootPos(value);
				//rootPos = value;
			}
		}
		/// <summary>
		/// Replace only the values of rootPos that are subject to change (based on includedAxes)
		/// </summary>
		/// <param name="pos"></param>
		public void OverwriteRootPos(Vector3 pos)
		{
			if ((nst.includedAxes & IncludedAxes.X) != 0)
				rootPos.x = pos.x;
			if ((nst.includedAxes & IncludedAxes.Y) != 0)
				rootPos.y = pos.y;
			if ((nst.includedAxes & IncludedAxes.Z) != 0)
				rootPos.z = pos.z;
		}

		/// <summary>
		/// Sets both the root position and compressed root position values with one call.
		/// </summary>
		public CompressedElement CompRootPos
		{
			get { return compPos;  }
			set
			{
				if (value == null)
				{
					Debug.LogError("Attempting to write a null CompressedElement to CompRootPos.");
					return;
				}

				compPos.CopyFrom(value);
				OverwriteRootPos((Vector3)compPos.Decompress());
			}
		}

		// TODO: Cache the reference to the root rotation so we can eleminate this long dereference
		/// <summary>
		/// Accesses the Root Rotation from the elements engine
		/// </summary>
		public GenericX RootRot
		{
			get { return rootRotElementFrame.xform; }
			set { rootRotElementFrame.xform = value; }
		}

		public CompressedElement CompRootRot
		{
			get { return rootRotElementFrame.compXform; }
			set { rootRotElementFrame.compXform.CopyFrom(value); }
		}

		// Construct
		public Frame(NetworkSyncTransform nst, int frameid, Vector3 pos, CompressedElement compPos) //, PositionElement[] positionElements, RotationElement[] rotationElements)
		{
			this.rootPosCrusher = WorldBoundsSO.single.worldBoundsGroups[0].crusher;
			this.nst = nst;
			this.ee = nst.nstElementsEngine;

			this.rootPos = pos;
			this.compPos.CopyFrom(compPos);
			this.state = nst.State;
			this.frameid = frameid;
			this.customData = new byte[128];  //TODO: Make this size a user setting

			this.checkSceneIndex = HeaderSettings.Single.includeSceneIndex;
			this.sceneIndex = NSTSceneManager.CurrentSceneIndex;
			this.rootBitCullLevel = BitCullingLevel.NoCulling;

			this.rootRotElementFrame = nst.rootRotationElement.frames[frameid];
		}

		public void ModifyFrame(UpdateType _updateType, BitCullingLevel _rootSendType, Vector3 _pos, Quaternion _rot, float _packetArrivedTime)
		{
			updateType = _updateType;
			rootBitCullLevel = _rootSendType;

			rootPos = _pos;
			rootPosCrusher.Compress(compPos, _pos);

			if (nst.rootRotationElement.crusher.TRSType == TRSType.Euler)
				RootRot = _rot.eulerAngles;
			else
				RootRot = _rot;

			//nst.rootRotationElement.Compress(rot, _rot);
			packetArriveTime = _packetArrivedTime;
		}

		
		/// <summary>
		/// Reconstruct the compPos, and then decompress the rootPos from that.
		/// </summary>
		public void CompletePosition(Frame prevCompleteFrame)
		{
			int currentSceneId = NSTSceneManager.CurrentSceneIndex;
			var prevRootPos = prevCompleteFrame.rootPos;

			/// If current scene is different scene than this frame was created in, the codecs may not match and we cannot decompress.
			/// Copy the uncompressed position, and invalidate the compPos (invalidate likely not needed, doing it for easier debugging)
			if (checkSceneIndex && (sceneIndex != currentSceneId))
			{
				OverwriteRootPos(prevRootPos);
				if (currentSceneId == prevCompleteFrame.sceneIndex)
					compPos.CopyFrom(prevCompleteFrame.compPos);
				else
					compPos.crusher = null;

				return;
			}

			CompressedElement prevCompPos = prevCompleteFrame.compPos;

			/// no new position is part of this update - copy the old
			if (rootBitCullLevel == BitCullingLevel.DropAll)
			{
				OverwriteRootPos(prevRootPos);
				compPos.CopyFrom(prevCompPos);
				return;
			}

			/// this crusher has been cleared, meaning no data in it can be trusted. This should no longer happen as this 
			/// should always be DropAll and covered by the previous
			if (compPos.crusher == null)
			{
				Debug.LogError(nst.NstId + " " + frameid + " Eliminate this - Davin " + rootBitCullLevel);
				compPos.CopyFrom(prevCompleteFrame.compPos);
				OverwriteRootPos(prevRootPos);
				return;
			}

			/// This arrived as a complete frame. Just decompress the pos and we are done.
			if (rootBitCullLevel == BitCullingLevel.NoCulling)
			{
				rootPos = (Vector3)compPos.Decompress();
				return;
			}
			
			/// Any futher cases involve bitculling... reconstruction is needed.


			/// if previous compPos is invalid, we cannot reconstruct. Rebuild from the previous uncompressed.
			if (prevCompPos.crusher == null || (checkSceneIndex && sceneIndex != prevCompleteFrame.sceneIndex))
			{
				RootPos = prevRootPos;
				return;
			}

			/// edge cases from scene changes are now all handled, we should be good to reconstruct normally now.


			/// We need to reconstruct upperbits from previous
			else if (rootBitCullLevel > BitCullingLevel.NoCulling)
			{
				/// Rebuild the missing upperbits
				compPos.OverwriteUpperBits(prevCompPos, rootBitCullLevel);
			}

			

			OverwriteRootPos((Vector3)compPos.Decompress());
		}



		/// <summary>
		/// Apply all of the current transforms to this frame's stored transforms.
		/// </summary>
		public void CaptureCurrentTransforms()
		{
			updateType = UpdateType.Teleport;
			rootBitCullLevel = BitCullingLevel.NoCulling;

			RootPos = nst.cachedTransform.position;

			TransformElement[] tes = ee.transformElements;
			int count = tes.Length;
			for (int eid = 0; eid < count; eid++)
			{
				TransformElement te = tes[eid];

				te.frames[frameid].xform = te.Localized;
				te.Compress(te.frames[frameid].compXform);
			}
		}

		public override string ToString()
		{
			string e = " Changed Elements: ";

			TransformElement[] tes = ee.transformElements;
			int count = tes.Length;
			for (int eid = 0; eid < tes.Length; eid++)
				if (tes[eid].frames[frameid].hasChanged)
					e += eid + " ";

			return
				"FrameID: " + frameid + " ut:" + updateType + "  rst:" + rootBitCullLevel + " " + state + "  " +  e + "\n" + 
				"compPos: " + compPos + " pos: " + rootPos + "\n" +
				"compRot: " + CompRootRot + " rot: " + RootRot;
		}
	}
}

#endif