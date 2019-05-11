//Copyright 2018, Davin Carten, All rights reserved

#if PUN_2_OR_NEWER || MIRROR || !UNITY_2019_1_OR_NEWER


using UnityEngine;
using emotitron.Utilities.SmartVars;
using emotitron.Utilities.BitUtilities;
using emotitron.Compression;
using emotitron.Debugging;
using System.Text;

namespace emotitron.NST
{
	/// <summary>
	/// The Circular Buffer of Frames used by every NST. Every NST has one FrameBuffer and every Framebuffer is attached to one NST.
	/// This class contains an array of Frame[] as the buffer object.
	/// </summary>
	public class FrameBuffer
	{
		public NetworkSyncTransform nst; // the owner of this buffer
		public NSTElementsEngine nstElementsEngine;

		public Frame[] frames;

		private readonly bool checkSceneIndex;

		public int nstFrameCount;
		public int halfFrameCount;
		public int quaterFrameCount;
		public ulong validFrameMask;

		public Frame currentFrame;
		public Frame prevAppliedFrame;

		public float bufferAverageSize;
		public int numberOfSamplesInBufferAvg;

		// Number of frames current can get behind in the buffer before jumping forward for resync. 
		//This exists because teleports clear the valid flag mask, which will trigger resyncs on larger buffers
		private readonly int jumpForwardThreshold; 

		// Construct
		public FrameBuffer(NetworkSyncTransform nst, Vector3 pos, Quaternion rot)
		{
			this.nst = nst;
			//nstElementsEngine = _nstElementsEngine;

			nstFrameCount = NSTMaster.FRAME_COUNT / nst.sendEveryXTick;
			halfFrameCount = (nstFrameCount) / 2;
			quaterFrameCount = nstFrameCount / 3;

			// Extra frame is the offtick frame
			frames = new Frame[nstFrameCount + 1];

			CompressedElement compPos = new CompressedElement();
			WorldBoundsSO.defaultWorldBoundsCrusher.Compress(compPos, pos);

			checkSceneIndex = HeaderSettings.Single.includeSceneIndex;

			int count = nstFrameCount + 1;
			for (int i = 0; i < count; ++i)
			{
				frames[i] = new Frame(nst, i, pos, compPos);
			}

			currentFrame = frames[nstFrameCount];
			prevAppliedFrame = frames[nstFrameCount];

			frames[nstFrameCount].ModifyFrame(UpdateType.Regular, BitCullingLevel.NoCulling, pos, rot, Time.time);

			jumpForwardThreshold = (int)(nst.desiredBufferMS / nst.frameUpdateInterval) + 1;
		}
		

		/// <summary>
		/// Used to create the next frame when it hasn't arrived in time over the network.
		/// </summary>
		/// <param name="svrWaitingForTeleportConfirm"></param>
		public void ExtrapolateNextFrame(bool svrWaitingForTeleportConfirm) { Extrapolate(currentFrame, prevAppliedFrame, NextFrame, svrWaitingForTeleportConfirm); }

		int extrapolationCount;

		/// <summary>
		/// Extrapolates position/rotation data from a start and end frame, and writes the results to the target frame.
		/// </summary>
		public void Extrapolate(Frame currFr, Frame prevFr, Frame targetFr, bool svrWaitingForTeleportConfirm)
		{
			extrapolationCount++;

			targetFr.state = currFr.state;

			bool isTeleport = currFr.updateType.IsTeleport();
			
			// Extrapolate if the current frame the server waiting for a teleport confirmation.
			if (svrWaitingForTeleportConfirm)
			{
				/// TODO possible issue with scene change and mismatched globalcompressor
				targetFr.RootPos = nst.lastSentPos;
				//targetFr.sceneIndex = NSTSceneManager.CurrentSceneIndex;
			}
			// Reached the extrapolation limit, or this is a teleport... just copy the previous value.
			else if (extrapolationCount > nst.maxExtrapolates || isTeleport)
			{
				targetFr.rootPos = currFr.rootPos;

				if (currFr.sceneIndex != NSTSceneManager.CurrentSceneIndex)
				{
					/// Source frame was compressed with a different scene, we have to recompress the position with the current scene crusher values.
					WorldBoundsSO.defaultWorldBoundsCrusher.Compress(targetFr.compPos, targetFr.rootPos);
					targetFr.sceneIndex = NSTSceneManager.CurrentSceneIndex;
				}
				else
				{
					targetFr.compPos.CopyFrom(currFr.compPos);
					targetFr.sceneIndex = currFr.sceneIndex;
				}

				XDebug.Log(!XDebug.logInfo ? null :
						(Time.time + " " + nst.NstId + " <color=red><b>Extrapolate Limit Reached (or isTeleport)</b></color> Copying Prev Frame"
						+ targetFr.frameid + " sn:" + targetFr.sceneIndex + " c:" + targetFr.compPos + " u:<b>" + targetFr.rootPos +
						"</b>\nCurr:" + currFr.frameid + " sn:" + currFr.sceneIndex + " " + currFr.compPos + " " + currFr.rootPos));
			}
			else
			{
				bool extrapolateAsUncompressed =
					currFr.compPos.crusher == null ||
					(checkSceneIndex && (NSTSceneManager.CurrentSceneIndex != prevFr.sceneIndex || currFr.sceneIndex != prevFr.sceneIndex));

				/// We need to extrapolate with uncompressed positions, since the compressed values are likely not compatible.
				if (extrapolateAsUncompressed)
				{
					var lerpedPos = Vector3.LerpUnclamped(prevFr.rootPos, currFr.rootPos, 1 + nst.extrapolation);

					if (checkSceneIndex && targetFr.sceneIndex == NSTSceneManager.CurrentSceneIndex)
					{
						targetFr.RootPos = lerpedPos;
					}
					else
					{
						Debug.Log(nst.name + " " + targetFr.frameid + " Extrapolate Clearing CompPos " + targetFr.rootBitCullLevel);
						targetFr.rootPos = lerpedPos;
						targetFr.rootBitCullLevel = BitCullingLevel.DropAll;
						targetFr.compPos.Clear();
					}
					
					//Debug.Log( 
					XDebug.Log(!XDebug.logInfo ? null :
						(Time.time + " " + nst.NstId + " <b>Extrapolate</b> Missing Next Frame with Uncompressed values:"
						+ targetFr.frameid + " c:" + targetFr.compPos + " u:<b>" + targetFr.rootPos +
						"</b>\nCurr:" + currFr.frameid + " sn:" + currFr.sceneIndex + " " + currFr.compPos + " " + currFr.rootPos +
						"\nPrev" + prevFr.frameid + " sn:" + prevFr.sceneIndex + " " + prevFr.compPos + " " + prevFr.rootPos));
				}
				/// If we have valid compressedPositions to work with, use those to extrapolate (reduces jitter)
				else
				{
					CompressedElement.Extrapolate(targetFr.compPos, currFr.compPos, prevFr.compPos, nst.extrapolationDivisor);
					targetFr.OverwriteRootPos((Vector3)targetFr.compPos.Decompress());

					//Debug.Log( 
					XDebug.Log(!XDebug.logInfo ? null :
						(Time.time + " " + nst.NstId + " <b>Extrapolate</b> Missing Next Frame with compressed values:" 
						+ targetFr.frameid + " c:" + targetFr.compPos + " u:<b>" + targetFr.rootPos + 
						"</b>\nCurr:" +currFr.frameid + " sn:" + currFr.sceneIndex + " " + currFr.compPos + " " + currFr.rootPos + 
						"\nPrev" + prevFr.frameid + " sn:" + prevFr.sceneIndex + " " + prevFr.compPos + " " + prevFr.rootPos));
				}
			}
			// Carry over the teleport type to generated update.
			targetFr.updateType = isTeleport ? UpdateType.Teleport : UpdateType.Regular;

			// Extrapolate elements
			foreach (INstOnExtrapolate callbacks in nst.iNstOnExtrapolate)
				callbacks.OnExtrapolate(targetFr, currFr, extrapolationCount, svrWaitingForTeleportConfirm);
		}

		public void SetBitInValidFrameMask(int bit, bool b)
		{
			bit.SetBitInMask(ref validFrameMask, b);
		}

		public bool GetBitInValidFrameMask(int bit)
		{
			return validFrameMask.GetBitInMask(bit);
		}

		/// <summary>
		/// Notifies the FrameBuffer that a frame has been populated with a new update and is now a valid frame.
		/// </summary>
		public void FlagFrameAsValid(Frame frame) // UpdateType updateType, RootSendType rootPosType, CompressedElement compPos, int frameid)
		{
			int numOfFramesFromCurrent = CountFrames(CurrentIndex, frame.frameid);
			// is this frame still a future event for interpolation, or has it already just guessed it?
			bool isStillPending = numOfFramesFromCurrent < halfFrameCount && numOfFramesFromCurrent > 0;

			// Set as valid if 1. is not the frame currently rendering 2. is not in the past, unless the buffer is empty then we need to rewind
			SetBitInValidFrameMask(frame.frameid, /*!isCurrentFrame && */(isStillPending || validFrameMask == 0));
		}

		/// <summary>
		/// Determine the difference in count between two packet counts - accounting for the range being 0-X
		/// </summary>
		/// <returns> </returns>
		public int CountFrames(int firstIndex, int secondIndex)
		{
			// highest fid is reserved for indicating a teleport/fire event
			if (secondIndex == nstFrameCount || firstIndex == nstFrameCount)
				return 1;

			// if the new index is lower, convert it to what it would have been had it not wrapped back around.
			if (secondIndex < firstIndex)
				secondIndex += nstFrameCount;

			int numOfIndexes = secondIndex - firstIndex;

			return numOfIndexes;
		}

		public const int AVG_BUFFER_MAX_SAMPLES = 5;

		/// <summary>
		/// Factors a new buffer size (in seconds) into the running buffer average.
		/// </summary>
		/// <param name="newTime"></param>
		public void AddTimeToBufferAverage(float newTime)
		{
			bufferAverageSize = (bufferAverageSize * numberOfSamplesInBufferAvg + newTime) / (numberOfSamplesInBufferAvg + 1);
			numberOfSamplesInBufferAvg = Mathf.Min(numberOfSamplesInBufferAvg + 1, AVG_BUFFER_MAX_SAMPLES);
		}

		/// <summary>
		/// Factor the current buffer size into the running average.
		/// </summary>
		public void UpdateBufferAverage()
		{
			if (CurrentIndex == 0)
				return;

			AddTimeToBufferAverage(CurrentBufferSize);
		}

		/// <summary>
		/// Get the current size of the buffer. Accounts for number of frames in the buffer + remaining interpolation on current frame.
		/// </summary>
		public float CurrentBufferSize
		{
			get
			{
				int numOfTrueBits = validFrameMask.CountTrueBits();
				int steps;

				// Don't bother with the complex check for numb of frames in buffer if we have less than 2 bits
				if (numOfTrueBits <= 1)
				{
					steps = numOfTrueBits;
				}
				// loss tolerent check for number of frames in buffer.
				else
				{
					int oldest = 0;
					int newest = 0;
					for (int i = quaterFrameCount - 1; i > 0; --i)
					{
						if (GetBitInValidFrameMask(Increment(CurrentIndex, i)))
						{
							newest = i;
							break;
						}
					}
					for (int i = -(quaterFrameCount - 1); i <= 0; ++i)
					{
						if (GetBitInValidFrameMask(Increment(CurrentIndex, i)))
						{
							oldest = i;
							break;
						}
					}
					steps = 1 + newest - oldest;
				}
				// TODO: clamp likely unneeded
				return steps * nst.frameUpdateInterval + Mathf.Clamp(currentFrame.endTime - Time.time, 0, nst.frameUpdateInterval);
			}
		}

		public int CurrentIndex
		{
			get { return currentFrame.frameid; }
		}

		/// <summary>
		/// Get the current frame index + 1
		/// </summary>
		public int GetNextIndex
		{
			get
			{
				int next = currentFrame.frameid + 1;
				if (next >= nstFrameCount)
					next -= nstFrameCount;

				return next;
			}
		}

		/// <summary>
		/// Get the current frame index - 1
		/// </summary>
		public int GetPrevIndex
		{
			get
			{
				int previndex = currentFrame.frameid - 1;
				if (previndex < 0)
					previndex = nstFrameCount - previndex;

				return previndex;
			}
		}
		
		public Frame NextFrame { get { return frames[GetNextIndex]; } }
		public Frame PrevFrame { get { return frames[GetPrevIndex]; } }
		public Frame IncrementFrame(int startingId, int increment) { return frames[Increment(startingId, increment)]; }

		/// <summary>
		/// Get the frame X increments before or after another frame.
		/// </summary>
		public int Increment(int startIndex, int increment)
		{
			int newIndex = startIndex + increment;

			while (newIndex >= nstFrameCount)
				newIndex -= nstFrameCount;

			while (newIndex < 0)
				newIndex += nstFrameCount;

			return newIndex;
		}

		/// <summary>
		/// Find frame in buffer x increments from the given frame.
		/// </summary>
		public Frame IncrementFrame(Frame startingFrame, int increment)
		{
			return IncrementFrame(startingFrame.frameid, increment);
		}

		/// <summary>
		/// Returns the previous keyframe closest to the specified frame, if none can be found returns the current frame.
		/// </summary>
		/// <param name="index"></param>
		/// <returns>Returns current frame if no keyframes are found.</returns>
		public Frame BestPreviousKeyframe(int index)
		{
			//// First try to get best keyframe
			for (int off = 1; off < halfFrameCount; ++off)
			{
				int offsetIndex = index - off;
				offsetIndex = (offsetIndex < 0) ? offsetIndex + nstFrameCount : offsetIndex;
				Frame frame = frames[offsetIndex];

				if (frame.rootBitCullLevel == BitCullingLevel.NoCulling &&
					Time.time - frame.packetArriveTime < nst.frameUpdateInterval * (off + quaterFrameCount)) // rough estimate that the frame came in this round and isn't a full buffer cycle old
				{
					return frame;
				}
			}

			//Debug.Log(
			XDebug.Log(!XDebug.logInfo ? null : 
				(Time.time + " NST:" + nst.NstId  + " Could not find a recent keyframe in the buffer history, likely very bad internet loss is responsible. " +
					"Some erratic player movement is possible. " + nst.name));

			return currentFrame;
		}

		/// <summary>
		/// This is a hot path. Runs at the completion interpolation, and attempts to find/reconstruct the next suitable frame for interpolation.
		/// </summary>
		public Frame DetermineAndPrepareNextFrame(bool svrWaitingForTeleportConfirm)
		{
			// buffer is empty, no point looking for any frames - we need to extrapolate the next frame
			if (validFrameMask == 0)
			{
			
				ExtrapolateNextFrame(svrWaitingForTeleportConfirm);

				XDebug.Log(!XDebug.logInfo ? null :
				//Debug.Log(
				(Time.time + " NST:" + nst.NstId + " <b> Empty buffer</b>, (likely packetloss) copying current frame to " + NextFrame.frameid + " " + nst.name +
					"\nCurrentFrame: " + currentFrame.frameid + " scn:" + currentFrame.sceneIndex + " " + currentFrame.compPos + " " + currentFrame.rootPos +
					"\nNextFrame: " + NextFrame.frameid + " scn:" + NextFrame.sceneIndex + " " + NextFrame.compPos + " " + NextFrame.rootPos));

				return NextFrame;
			}

			extrapolationCount = 0;

			// First see if there is a future frame ready - ignoring late arrivles that may have backfilled behind the current frame
			Frame nextValid = GetFirstFutureValidFrame();

			// if not see if there is an older frame that arrived late, if so we will jump back to that as current
			if (nextValid == null)
			{
				nextValid = GetOldestPastValidFrame() ?? GetOldestValidFrame();

				// The only valid frames are only in the past, we need to jump back to the oldest to get our current frame in a better ballpark
				if (nextValid != null)
				{
				
					nextValid.CompletePosition(currentFrame);
					//Debug.Log(
					XDebug.Log(!XDebug.logInfo ? null :
						(Time.time + " NST:" + nst.NstId + " <b> Skipping back </b>(likely packetloss) to frame " + nextValid.frameid +
						" from current frame " + CurrentIndex + " " + nst.name +
						"\nOnly frames in buffer were in the past, so seems that we are getting ahead of the buffer. Should see these rarely." +
						"\nCurrentFrame: " + currentFrame.frameid + " scn:" + currentFrame.sceneIndex + " " + currentFrame.compPos + " " + currentFrame.rootPos +
						"\nNextValid: " + nextValid.frameid + " scn:" + nextValid.sceneIndex + " " + nextValid.compPos + " " + nextValid.rootPos));

					return nextValid;
				}
			}
			
			// Find out how far in the future the next valid frame is, need to know this for the reconstruction lerp.
			int stepsFromLast = CountFrames(CurrentIndex, nextValid.frameid);

			// The next frame is the next valid... not much thinking required... just use it.
			if (stepsFromLast == 1)
			{

				InvalidateOldFrames(NextFrame); // LIKELY UNEEDED
				NextFrame.CompletePosition(currentFrame);

				//Debug.Log(
				//XDebug.Log(!XDebug.logInfo ? null :
				//	(Time.time + " NST:" + nst.NstId + " <b>Normal Next</b> from " + CurrentIndex + " to " + NextFrame.frameid + "  (likely packetloss) from expected frame. " + nst.name +
				//	"\nCurrentFrame: " + currentFrame.frameid + " scn:" + currentFrame.sceneIndex + " " + currentFrame.compPos + " " + currentFrame.rootPos +
				//	"\nNextFrame: " + NextFrame.frameid + " scn:" + NextFrame.sceneIndex + " " + NextFrame.compPos + " " + NextFrame.rootPos));

				return NextFrame;
			}

			// if next frame on the buffer is a couple ahead of current, jump forward
			if (stepsFromLast > jumpForwardThreshold)
			{
				//Debug.Log(
				XDebug.Log(!XDebug.logInfo ? null :
					(Time.time + " NST:" + nst.NstId + " <b>Jumping forward</b> from " + CurrentIndex + " to " + nextValid.frameid + "  (likely packetloss) from expected frame. " + nst.name +
					"\nCurrentFrame: " + currentFrame.frameid + " " + currentFrame.compPos + " " + currentFrame.rootPos +
					"\nNextValidFrame: " + nextValid.frameid + " " + nextValid.compPos + " " + nextValid.rootPos));

				InvalidateOldFrames(nextValid);
				nextValid.CompletePosition(currentFrame);
				return nextValid;
			}

			//All other cases we Reconstruct missing next frame using the current frame and a future frame

			NextFrame.state = currentFrame.state;

			float t = 1f / stepsFromLast;

			nextValid.CompletePosition(currentFrame);

			
			Vector3 lerpedPos = Vector3.Lerp(currentFrame.rootPos, nextValid.rootPos, t);

			float lerpedStartTime = Mathf.Lerp(currentFrame.packetArriveTime, nextValid.packetArriveTime, t);

			NextFrame.ModifyFrame(currentFrame.updateType, currentFrame.rootBitCullLevel, lerpedPos, GenericX.NULL, lerpedStartTime);

			//Debug.Log(
			XDebug.Log(!XDebug.logInfo ? null :
				(Time.time + " NST:" + nst.NstId + " <b>Reconstructing frame " + NextFrame.frameid + "</b> (likely packetloss) from current frame and future frame " 
				+ NextFrame.compPos + " <b>" + NextFrame.rootPos + "</b> " + nst.name +
				"\nCurrentFrame: " + currentFrame.frameid + " scn:" + currentFrame.sceneIndex + " " + currentFrame.compPos + " " + currentFrame.rootPos +
				"\nNextValidFrame: " + nextValid.frameid + " scn:" + nextValid.sceneIndex + " " + nextValid.compPos + " " + nextValid.rootPos) + " " + nextValid.rootBitCullLevel);


			//XDebug.Log(!XDebug.logInfo ? null : 
			//	(Time.time + "fid" + NextFrame.frameid + " <color=red><b> RECONSTRUCT ELEMENTS </b></color> " + NextFrame.RootRot + " " + currentFrame.RootRot + " " + nextValid.RootRot));

			// Notify all interested components that they need to reconstruct a missing frame (elements and such)
			foreach (INstOnReconstructMissing callbacks in nst.iNstOnReconstructMissing)
				callbacks.OnReconstructMissing(NextFrame, currentFrame, nextValid, t, svrWaitingForTeleportConfirm);

			return NextFrame;
		}

		/// <summary>
		/// Marks all frames before the startingFrame as invalid
		/// </summary>
		public void InvalidateOldFrames(Frame startingframe)
		{
			for (int i = -quaterFrameCount; i < 0; ++i)
			{
				SetBitInValidFrameMask(IncrementFrame(startingframe, i).frameid, false);
			}
		}

		/// <summary>
		/// Checks ENTIRE buffer for the oldest arriving frame. Used for the starting up.
		/// </summary>
		/// <returns>Returns null if no valid frames are found.</returns>
		public Frame GetOldestValidFrame()
		{
			if (validFrameMask == 0)
				return null;

			float timetobeat = Time.time;
			int winnerwinnerchickendinner = 0;

			// First look forward
			for (int i = 1; i < nstFrameCount; ++i)
			{
				if (GetBitInValidFrameMask(i) && frames[i].packetArriveTime < timetobeat)
				{
					winnerwinnerchickendinner = i;
					timetobeat = frames[i].packetArriveTime;
				}
			}
			return frames[winnerwinnerchickendinner];
		}

		/// <summary>
		/// Looks for farthest back valid frame before the current frame, starting with a quater buffer length behind working up to the current frame.
		/// </summary>
		/// <returns>Returns null if none found.</returns>
		public Frame GetOldestPastValidFrame()
		{
			int count = -quaterFrameCount;
			for (int i = count; i < 0; ++i)
			{
				Frame testframe = IncrementFrame(CurrentIndex, i);
				if (GetBitInValidFrameMask(testframe.frameid))
				{
					return testframe;
				}
			}
			return null;
		}

		/// <summary>
		/// Get the first valid frame BEFORE the current frame.
		/// </summary>
		public Frame GetNewestPastValidFrame()
		{
			int count = -quaterFrameCount;
			for (int i = -1; i >= count; --i)
			{
				Frame testframe = IncrementFrame(CurrentIndex, i);
				if (GetBitInValidFrameMask(testframe.frameid))
				{
					return testframe;
				}
			}
			return null;
		}

		/// <summary>
		/// Looks for first valid frame AFTER the current frame.
		/// </summary>
		/// <returns>Returns null if none found.</returns>
		public Frame GetFirstFutureValidFrame()
		{
			for (int i = 1; i <= quaterFrameCount; ++i)
			{
				Frame testframe = IncrementFrame(CurrentIndex, i);
				if (GetBitInValidFrameMask(testframe.frameid))
				{
					return testframe;
				}
			}
			return null;
		}

		public Frame GetFurthestFutureValidFrame(Frame startingframe = null)
		{
			if (startingframe == null)
				startingframe = currentFrame;

			for (int i = quaterFrameCount; i > 0; ++i)
			{
				Frame testframe = IncrementFrame(startingframe, i);
				if (GetBitInValidFrameMask(testframe.frameid))
				{
					return testframe;
				}
			}
			return null;
		}

		public static StringBuilder strb;
		public string PrintBufferMask(int hilitebit = -1)
		{
			if (strb == null)
				strb = new StringBuilder(1024);
			else
				strb.Length = 0;

			return PrintBufferMask(strb, hilitebit).ToString();

		}
		public StringBuilder PrintBufferMask(StringBuilder strb, int hilitebit = -1)
		{

			for (int i = nstFrameCount - 1; i >= 0; --i)
			{
				bool isvalid = (validFrameMask & (ulong)1 << i) != 0;
				bool iscurrent = CurrentIndex == i;

				if (frames[i].rootBitCullLevel == BitCullingLevel.DropAll)
					strb.Append("<color=blue>");
				else if (frames[i].rootBitCullLevel > BitCullingLevel.NoCulling)
					strb.Append("<color=maroon>");
				else
					strb.Append("<color=black>");

				if (iscurrent)
					strb.Append("<b>");
				strb.Append(isvalid ? 1 : 0);
				if (iscurrent)
					strb.Append("</b>");

				strb.Append("</color>");


				if (i % 4 == 0)
					strb.Append(" <color=silver>" + i + "</color> ");
			}
			strb.Append(" orng=lbits, blue=no_pos " + strb.Length);
			return strb;
		}
	}
}

#endif