//Copyright 2018, Davin Carten, All rights reserved

#if PUN_2_OR_NEWER || MIRROR || !UNITY_2019_1_OR_NEWER

using UnityEngine;
using emotitron.Compression;

namespace emotitron.NST
{
	public enum CallbackTiming { OnPostStreamInject, OnSend, OnReceive, OnLateArrival, OnExtrapolate, OnSnapshotToRewind, OnStartInterpolation, OnEndInterpolation, OnFixedUpateStart, OnFixedUpdateEnd, OnEndUpdate }

	
	public interface INstState
	{
		void OnNstState(State newState, State oldState);
	}
	/// <summary>
	/// All instances of this interface on an object are called when the NST completes its Awake(). Note: NST will not complete its awake
	/// if the NetworkServer and NetworkClient are both inactive, as this indicates that the object was not spawned and is just an unspawned
	/// prefab still in the scene that needs to be removed at startup.
	/// </summary>
	public interface INstAwake
	{
		void OnNstPostAwake();
	}

	public interface INstUpdate
	{
		void OnNstUpdate();
	}

	public interface INstPreUpdate
	{
		void OnNstPreUpdate();
	}

	public interface INstPostUpdate
	{
		void OnNstPostUpdate();
	}

	public interface INstPreLateUpdate
	{
		void OnNstPreLateUpdate();
	}

	public interface INstPostLateUpdate
	{
		void OnNstPostLateUpdate();
	}

	public interface INstPrePollForUpdate
	{
		void OnNstPrePollForUpdate();
	}

	public interface INstStart
	{
		void OnNstStart();
	}
	public interface INstOnStartServer
	{
		void OnNstStartServer();
	}
	public interface INstOnStartClient
	{
		void OnNstStartClient();
	}
	public interface INstOnStartLocalPlayer
	{
		void OnNstStartLocalPlayer();
	}

	public interface INstOnNetworkDestroy
	{
		void OnNstNetworkDestroy();
	}

	public interface INstOnDestroy
	{
		void OnNstDestroy();
	}

	public interface INstOnFirstAppliedFrameZero
	{
		void OnFirstAppliedFrameZero(Frame frame);
	}

	//public interface INstOnFullRootPos
	//{
	//	void OnNstFullRootPos(Frame frame);
	//}

	public interface INstGenerateUpdateType
	{
		void OnGenerateUpdateType(Frame frame, ref UdpBitStream bitstream, ref bool forceKey);
	}

	public interface INstBitstreamInjectFirst
	{
		void NSTBitstreamOutgoingFirst(Frame frame, ref UdpBitStream bitstream);
		void NSTBitstreamIncomingFirst(Frame frame, Frame currFrame, ref UdpBitStream bitstream, bool isServer);
		void NSTBitstreamMirrorFirst(Frame frame, ref UdpBitStream outstream, bool waitingForTeleportConfirm);
	}

	public interface INstBitstreamInjectSecond
	{
		void NSTBitstreamOutgoingSecond(Frame frame, ref UdpBitStream bitstream);
		void NSTBitstreamIncomingSecond(Frame frame, Frame currFrame, ref UdpBitStream bitstream, ref UdpBitStream outstream, bool isServer, bool waitingForTeleportConfirm);
	}

	public interface INstBitstreamInjectThird
	{
		void NSTBitstreamOutgoingThird(Frame frame, ref UdpBitStream bitstream);
		void NSTBitstreamIncomingThird(Frame frame, ref UdpBitStream bitstream, ref UdpBitStream outstream, bool isServer);
		//void NSTBitstreamMirrorThird(Frame frame, ref UdpBitStream outstream, bool isServer);
	}

	/// <summary>
	/// The NetworkSyncTransform collects all instances of this interface on its prefab when it is spawned.
	/// This is called when the frame is about to be sent. Used by server to trigger Snapshot to Rewind history.
	/// Can be used trigger Custom events like weapon fire, as it will contain the exact sent postition/rotation of the gameobjects,
	/// with the fire delay (due to waiting for the next outgoing update) and rounding errors (due to network compression).
	/// </summary>
	public interface INstOnSndUpdate
	{
		void OnSnd(Frame frame);
	}

	public interface INstOnOwnerIncomingRootPos
	{
		void OnOwnerIncomingRootPos(Frame updateframe, CompressedElement sentCompPos, Vector3 sentPos);
	}

	public interface INstOnSvrOutgoingRootPos
	{
		Vector3 OnSvrOutgoingRootPos(out bool outOfSync);
	}

	/// <summary>
	/// The NetworkSyncTransform collects all instances of this interface on its prefab when it is spawned.
	/// This is called when the frame has just arrived.
	/// </summary>
	public interface INstOnRcvUpdate
	{
		void OnRcv(Frame frame);
	}

	/// <summary>
	/// The NetworkSyncTransform collects all instances of this interface on its prefab when it is spawned.
	/// This is called when the frame has completed interpolation, and is now being recored to rewind history. (Server Only)
	/// </summary>
	public interface INstOnSnapshotToRewind
	{
		void OnSnapshotToRewind(Frame frame);
	}

	/// <summary>
	/// The NetworkSyncTransform collects all instances of this interface on its prefab when it is spawned.
	/// This is called when the frame has started its interpolation. Any events attached to this frame, like weapon fire will appear to fire ahead of the gameobject.
	/// </summary>
	public interface INstOnStartInterpolate
	{
		void OnStartInterpolate(Frame frame, bool lateArrival = false, bool midTeleport = false);
	}

	/// <summary>
	/// The NetworkSyncTransform collects all instances of this interface on its prefab when it is spawned.
	/// This is called every update, and is used to notify NST Elements to interpolate.
	/// </summary>
	public interface INstOnInterpolate
	{
		void OnInterpolate(float t);
	}

	/// <summary>
	/// The NetworkSyncTransform collects all instances of this interface on its prefab when it is spawned.
	/// This is called when the frame has completed its interpolation. Any events attached to this frame, like weapon fire will be in sync with the game object.
	/// </summary>
	public interface INstOnEndInterpolate
	{
		void OnEndInterpolate(Frame frame);
	}

	/// <summary>
	/// The NetworkSyncTransform collects all instances of this interface on its prefab when it is spawned.
	/// This interface is specifically for the Server Authority add-on.
	/// </summary>
	public interface INstOnSvrInterpolateRoot
	{
		Vector3 OnSvrInterpolateRoot(Frame frame, Vector3 startPos, Vector3 targetPos, float lerpTime);
	}

	public interface INstOnExtrapolate
	{
		void OnExtrapolate(Frame frame, Frame prevFrame, int extrapolationCount, bool svrWaitingForTeleportConfirm);
	}

	///// <summary>
	///// The NetworkSyncTransform collects all instances of this interface on its prefab when it is spawned.
	///// This is called when at the start of every FixedUpdate. Used for applying Server Authority enforcement on owners.
	///// </summary>
	//public interface INstPreFixedUpdate
	//{
	//	void OnNstPreFixedUpdate(NetworkSyncTransform nst, int currentFrameId, int lastRcvdFrameId);
	//}

	///// <summary>
	///// The NetworkSyncTransform collects all instances of this interface on its prefab when it is spawned.
	///// This is called when at the end of every FixedUpdate. Used for applying Server Authority max velocity enforcement.
	///// </summary>
	//public interface INstPostFixedUpdate
	//{
	//	void OnNstPostFixedUpdate(NetworkSyncTransform nst, int currentFrameId, int lastRcvdFrameId);
	//}

	public interface INstOnReconstructMissing
	{
		void OnReconstructMissing(Frame nextFrame, Frame currentFrame, Frame nextValid, float t, bool svrWaitingForTeleportConfirm);
	}

	public interface INstTeleportApply
	{
		void OnTeleportApply(Frame frame);
		void OnRcvSvrTeleportCmd(Frame frame);
	}

	public interface INstOnTeleportApply
	{
		void OnTeleportApply (Frame frame);
	}

}


#endif