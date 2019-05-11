//Copyright 2018, Davin Carten, All rights reserved

#if PUN_2_OR_NEWER || MIRROR || !UNITY_2019_1_OR_NEWER

using System;
using UnityEngine;
using System.Collections.Generic;
using emotitron.Compression;
using emotitron.Debugging;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace emotitron.NST
{
	public enum KeyType { AnyUpdate, RootPosKey, FrameZero }

	[AddComponentMenu("NST/Network Sync Transform")]

	[DisallowMultipleComponent]
	[HelpURL("https://docs.google.com/document/d/1nPWGC_2xa6t4f9P0sI7wAe4osrg4UP0n_9BVYnr5dkQ/edit#heading=h.xct6xseu49aa")]
	//[NetworkSettings(sendInterval = 0)]
	public class NetworkSyncTransform : MonoBehaviour,
		INstOnFirstAppliedFrameZero, IOfftickSrc, INstSource, INSTTransformElement, INetEvents, INstGenerateUpdateType
	{
		public const int VersionMajor = 5;
		public const int VersionMinor = 7;
		public const int VersionRevision = 8;
		public const int Build = 5708;

		// Interface requirements
		public GameObject SrcGameObject { get { return gameObject; } }
		public TransformElement TransElement { get { return rootRotationElement; } }
		public NetworkSyncTransform Nst { get { return this; } }
		public bool Enabled { get { return this.enabled; } }

		// IOfftick interface
		public int OffticksPending { get { return customEventQueue.Count; } }

		[HideInInspector] public NSTNetAdapter na;
		[HideInInspector] public GameObject ghostGO;

		[SerializeField]
		private State _state = State.Invisible;
		public State State
		{
			get { return _state; }
			set
			{
				// For Network Models where late arrivals are not given the correct position, we stay dead while waiting for it.
				if (ignoringStateChanges)
					return;

				if (_state == value)
					return;

				foreach (INstState cb in iNstState)
					cb.OnNstState(value, _state);

				_state = value;
			}
		}

		[Tooltip("Because NST uses a lot of partial updates for the synced root and children elements, " +
			"it is very possible when joining a game for some of the elements to not yet have received a complete update, " +
			"resulting in some erratic position, rotation or scale changes upon joining. " +
			"Holding the state until a complete update has arrived will keep the NSTs state at its default value until the designated type of update arrives. " +
			"Frame Zero will be the most complete update with nearly every synced component treating that as a keyframe.")]
		[SerializeField]
		private KeyType ignoreStateUntil = KeyType.FrameZero;
		private bool ignoringStateChanges = true;

		// useable by LaggedAction()
		private void SetStateAlive() { State = State.Alive; }
		private void SetStateDead() { State = State.Dead; }

#region Lagged Actions

		//private struct LaggedAction
		//{
		//	public float applyTime;
		//	public Action action;
		//	public LaggedAction(float applyTime, Action action) { this.applyTime = applyTime; this.action = action; }
		//}
		//private Queue<LaggedAction> laggedAction = new Queue<LaggedAction>();

		//private void PollForDelayedActions()
		//{
		//	while (laggedAction.Count > 0 && laggedAction.Peek().applyTime < Time.time)
		//	{
		//		XDebug.Log("Found lagged action " + laggedAction.Peek());
		//		laggedAction.Dequeue().action.Invoke();
		//	}
		//}

		///// <summary>
		///// Server Only. Will execute an action in Half the Round Trip Time between the owner and server.
		///// </summary>
		//private void QueueLaggedAction(Action action)
		//{
		//	float delay = Time.time + MasterRTT.GetRTT(na) * .5f;
		//	laggedAction.Enqueue( new LaggedAction(delay, action));
		//	Debug.Log("Que action " + action + " by " + delay + " secs");
		//}

#endregion

#region Inspector Vars

		[Range(1, 6)]
		[Tooltip("1 is the default. 1 sends updates every Network Tick (set in NSTSettings). 3 every 3rd.")]
		public int sendEveryXTick = 1;

		[Range(0f, .5f)]
		[Tooltip("Target number of milliseconds to buffer. Higher creates more induced latency, lower smoother handling of network jitter and loss.")]
		public float desiredBufferMS = .1f;

		[Range(0f, 1f)]
		[Tooltip("How aggressively to try and maintain the desired buffer size.")]
		public float bufferDriftCorrectAmt = .1f;

		[Tooltip("Let NST guess the best settings for isKinematic and interpolation for your rigidbodies on server/client/localplayer. Turn this off if you want to set them yourself in your own code.")]
		[SerializeField]
		private bool autoKinematic = true;

		[Tooltip("Offitck allows 'AddCustomEventToQueue()' calls to be sent out from owners immediately to reduce latency, but will increase data usage. " +
			"Also, note that since these immediate updates aren't buffered or interpolated, positions and rotations involved may differ from the rendered world.")]
		public bool allowOfftick = true;

		[Header("Root Position Updates")]

		[Utilities.GUIUtilities.EnumMask]
		public SendCullMask sendOnEvent = SendCullMask.OnChanges | SendCullMask.OnTeleport | SendCullMask.OnCustomMsg | SendCullMask.OnRewindCast;

		[XYZSwitchMask]
		public IncludedAxes includedAxes = (IncludedAxes)7;

		[Range(0f, 16f)]
		[Tooltip("How often to force a position keyframe. These ensure that with network errors or newly joined players objects will not remain out of sync long.")]
		public int keyEvery = 5;

		[Tooltip("0 = No extrapolation. 1 = Full extrapolation. Extrapolation occurs when the buffer runs out of frames. Without extrapolation the object will freeze if no new position updates have arrived in time. With extrapolation the object will continue in the direction it was heading as of the last update until a new update arrives.")]
		[Range(0, 1)]
		public float extrapolation = .5f;
		[HideInInspector] public int extrapolationDivisor; // cached invert of extrapolate (1 / extrapolate) for int math

		[Tooltip("The max number of sequential frames that will be extrapolated. Too large a value and objects will wander too far during network hangs, too few and network objects will freeze when the buffer empties. Extrapolation should not be occurring often - if at all, so a smaller number is ideal (default = 1 frame).")]
		[Range(0,4)]
		public int maxExtrapolates = 2;

		[Tooltip("A change in postion greater than this distance in units will treat the move as a teleport. This means the object will move to that location without any tweening.")]
		[SerializeField]
		private float teleportThreshold = 2;

		[Header("Root Position Upper Bit Culling")]
		[Tooltip("Enabling this reduces position data by not sending the higher order bits of the compressed positions unless they have changed. This can greatly reduce data usage on larger maps (such as a battlefield), and is not recommended for small maps (such as Pong). It does introduce the possibility of odd behavior from bad connections though.")]

		public BitCullingLevel bitCullingLevel = BitCullingLevel.NoCulling;

		[Tooltip("When using upper bit culling, this value dictates how many full frames in a row will be sent after upper bits have changed. The higher this number the lower the risk of lost packets creating mayhem. Too high and you will end up with nothing but keyframes.")]
		[SerializeField]
		[Range(1, 10)]
		private int sequentialKeys = 5;

		public RotationElement rootRotationElement = new RotationElement(false) { isRoot = true, name = "Root Rotation" };

		[Header("Debugging")]
		[SerializeField]
		public DebugXform debugXform;
		//[HideInInspector] public GameObject debugXformGO;

		[Header("Cleanup")]
		[Tooltip("Sets this gameobject to not destory on scene changes, which will allow players to retain their pos/rot/scale through scene changes.")]
		public bool dontDestroyOnLoad = true;

		[Tooltip("Destroy this GameObject at runtime if it wasn't spawned. This is a convenience option for development, so you can leave objects in the scene for editing without them becoming server objects at runtime.")]
		[SerializeField]
		public bool destroyUnspawned = true; 

#endregion

#region NstId Syncvar and methods

		// The network ID actually is synced using the network adaptor, since that is network library dependent.
		public uint NstId
		{
			// TODO remove this null check once adapter fully worked out
			get { return na.NstIdSyncvar; }
			private set { na.NstIdSyncvar = value; }
		}

		//private Frame CurrentFrame
		//{
		//	get { return buffer.currentFrame; }
		//	set { buffer.currentFrame = value; }
		//}

		private int CurrentIndex
		{
			get { return buffer.currentFrame.frameid; }
		}
		private Frame OfftickFrame;

		[NonSerialized]
		public int frameCount;

#endregion

#region Callback Interfaces

		[HideInInspector] public List<INstAwake> iNstAwake = new List<INstAwake>();

		[HideInInspector] public List<IOfftickSrc> iOfftickSrc = new List<IOfftickSrc>();
		[HideInInspector] public List<INstState> iNstState = new List<INstState>();
		[HideInInspector] public List<INstUpdate> iNstUpdate = new List<INstUpdate>();
		[HideInInspector] public List<INstPreUpdate> iNstPreUpdate = new List<INstPreUpdate>();
		[HideInInspector] public List<INstPostUpdate> iNstPostUpdate = new List<INstPostUpdate>();
		[HideInInspector] public List<INstPreLateUpdate> iNstPreLateUpdate = new List<INstPreLateUpdate>();
		[HideInInspector] public List<INstPostLateUpdate> iNstPostLateUpdate = new List<INstPostLateUpdate>();
		[HideInInspector] public List<INstPrePollForUpdate> iNstPrePollForUpdate = new List<INstPrePollForUpdate>();
		[HideInInspector] public List<INstStart> iNstStart = new List<INstStart>();
		[HideInInspector] public List<INstOnStartServer> iNstOnStartServer = new List<INstOnStartServer>();
		[HideInInspector] public List<INstOnStartClient> iNstOnStartClient = new List<INstOnStartClient>();
		[HideInInspector] public List<INstOnStartLocalPlayer> iNstOnStartLocalPlayer = new List<INstOnStartLocalPlayer>();
		[HideInInspector] public List<INstOnNetworkDestroy> iNstOnNetworkDestroy = new List<INstOnNetworkDestroy>();
		[HideInInspector] public List<INstOnDestroy> iNstOnDestroy = new List<INstOnDestroy>();
		[HideInInspector] public List<INstBitstreamInjectFirst> iBitstreamInjectFirst = new List<INstBitstreamInjectFirst>();
		[HideInInspector] public List<INstBitstreamInjectSecond> iBitstreamInjectSecond = new List<INstBitstreamInjectSecond>();
		[HideInInspector] public List<INstBitstreamInjectThird> iBitstreamInjectsLate = new List<INstBitstreamInjectThird>();
		[HideInInspector] public List<INstGenerateUpdateType> iGenerateUpdateType = new List<INstGenerateUpdateType>();
		[HideInInspector] public List<INstOnExtrapolate> iNstOnExtrapolate = new List<INstOnExtrapolate>();
		[HideInInspector] public List<INstOnReconstructMissing> iNstOnReconstructMissing = new List<INstOnReconstructMissing>();
		[HideInInspector] public List<INstOnSndUpdate> iNstOnSndUpdate = new List<INstOnSndUpdate>();
		[HideInInspector] public List<INstOnRcvUpdate> iNstOnRcvUpdate = new List<INstOnRcvUpdate>();
		//[HideInInspector] public List<INstOnOwnerIncomingRootPos> iNstOnOwnerIncomingRoot = new List<INstOnOwnerIncomingRootPos>();
		[HideInInspector] public List<INstOnSvrOutgoingRootPos> iNstOnSvrOutgoingRoot = new List<INstOnSvrOutgoingRootPos>();
		[HideInInspector] public List<INstOnSnapshotToRewind> iNstOnSnapshotToRewind = new List<INstOnSnapshotToRewind>();
		[HideInInspector] public List<INstOnStartInterpolate> iNstOnStartInterpolate = new List<INstOnStartInterpolate>();
		[HideInInspector] public List<INstOnEndInterpolate> iNstOnEndInterpolate = new List<INstOnEndInterpolate>();
		[HideInInspector] public List<INstOnInterpolate> iNstOnInterpolate = new List<INstOnInterpolate>();
		[HideInInspector] public List<INstOnSvrInterpolateRoot> iNstOnSvrInterpRoot = new List<INstOnSvrInterpolateRoot>();
		[HideInInspector] public List<INstTeleportApply> iNstTeleportApply = new List<INstTeleportApply>();
		[HideInInspector] public List<INstOnTeleportApply> iNstOnTeleportApply = new List<INstOnTeleportApply>();
		[HideInInspector] public List<INstOnFirstAppliedFrameZero> iNstOnFirstAppliedFrameZero = new List<INstOnFirstAppliedFrameZero>();

#endregion

#region Startup and Initialization

		// Cached Components
		[HideInInspector] public Rigidbody rb;
		[NonSerialized]
		[HideInInspector] public NSTElementsEngine nstElementsEngine;
		[HideInInspector] public GameObject cachedGameObject;
		[HideInInspector] public Transform cachedTransform;

#if UNITY_EDITOR
		public void Reset()
		{
			this.EnsureAllNSTDependencies(new SerializedObject(this));
		}
#endif
		[NonSerialized]

		public bool hasBeenDestroyed;
		public /*virtual*/ void Awake()
		{
			if (NSTMaster.Single == null)
				NSTMaster.EnsureExistsInScene(NSTMaster.DEFAULT_GO_NAME);

			AwakeInitialization();

			foreach (INstAwake cb in iNstAwake)
				cb.OnNstPostAwake();
		}

		bool hasAwoke;
		public void AwakeInitialization()
		{

			if (hasAwoke)
				return;

			hasAwoke = true;

			// Has been destroyed but still may try to awake - don't let it.
			if (hasBeenDestroyed)
				return;

			// NSTs that exist in the scene and are flagged with destroyUnspawned need to get destroyed
			// They are only in the scene as a developer convenience, so that these objects don't have to be 
			// removed before hitting play or building every time
			if (destroyUnspawned && !MasterNetAdapter.Connected)
			{
				Destroy(transform.root.gameObject);
				return;
			}

			if (dontDestroyOnLoad)
				DontDestroyOnLoad(gameObject);


			frameCount = NSTMaster.FRAME_COUNT / sendEveryXTick;

			// Cache components
			cachedGameObject = gameObject;
			cachedTransform = transform;

			na = GetComponent<NSTNetAdapter>();
			if (!na)
				na = gameObject.AddComponent<NSTNetAdapter>();

			if (na == null)
				XDebug.LogError(!XDebug.logErrors ? null : ("No Network Library adapter found for " + name));

			rb = GetComponent<Rigidbody>();

			//this.AddRewindEngine();
			
			// Ensure core Singletons exist in case the idiot proofing missed it somehow.
			nstElementsEngine = NSTElementsEngine.EnsureExistsOnRoot(transform, false);
			//nstElementsEngine.Initialize(this);

			NSTTools.CollectCallbackInterfaces(this);

			// determine the update interval based on the current physics clock rate.
			frameUpdateInterval = Time.fixedDeltaTime * sendEveryXTick * HeaderSettings.Single.TickEveryXFixed;
			invFrameUpdateInterval = 1f / frameUpdateInterval;

			// Don't allow target buffer size to be smaller than the frameUpdateInterval, or else nudge starts to cause constant resyncs trying to acheive the impossibly small.
			desiredBufferMS = Mathf.Max(desiredBufferMS, frameUpdateInterval);

			extrapolationDivisor = (int)(1f / extrapolation);
		}

		public void OnConnect(ServerClient svrclnt)
		{
			AwakeInitialization();
			Initialize();
		}

		public void OnStartAuthority()
		{
			ApplyAutoKinematic();
		}

		public void OnStopAuthority()
		{
			ApplyAutoKinematic();
		}

		private bool initialized;
		private void Initialize()
		{

			if (initialized)
				return;

			initialized = true;
			
			// Has been destroyed but still may try to awake - don't let it.
			if (hasBeenDestroyed)
				return;

			//// Be sure that the elements count exists before doing the buffer init.
			//nstElementsEngine.Initialize(this);

			// Moved this from awake to init to give elements time to initialize element[0] (root rotation)
			buffer = new FrameBuffer(this, transform.position, transform.rotation);
			OfftickFrame = buffer.frames[buffer.nstFrameCount];
			lastRcvdFrame = OfftickFrame;

			// For Unet we assign the NstId when the server instantiates the networked object. 
			// We don't set it when instantiate is called, because Unet might be doing that
			// automatically through the NetworkManager.PlayerPrefab at start.
			NSTTools.GetNstIdAndSetSyncvar(this, na);

			// Create a DebugWidget for this NST
			DebugWidget.CreateDebugCross(gameObject, 
				(debugXform == DebugXform.LocalSend && na.IsMine) ||
				(debugXform == DebugXform.RawReceive && !na.IsMine) ||
				(debugXform == DebugXform.Uninterpolated && !na.IsMine) ||
				(debugXform == DebugXform.HistorySnapshot && MasterNetAdapter.ServerIsActive && NetLibrarySettings.Single.defaultAuthority == DefaultAuthority.ServerAuthority) 
				);
		}

		public void OnStartLocalPlayer()
		{
			// Store locally owned NSTs in their own list.
			if (!NSTTools.localNSTs.Contains(this))
				NSTTools.localNSTs.Add(this);

			foreach (INstOnStartLocalPlayer cb in iNstOnStartLocalPlayer)
				cb.OnNstStartLocalPlayer();
		}

		/// <summary>
		/// Replacement for Start(), that is called by the NstNetAdapter after it performs its startup and validity checks.
		/// </summary>
		public void OnStart()
		{
			AwakeInitialization();
			Initialize();

			ApplyAutoKinematic();

			if (/*(MasterNetAdapter.NET_MODEL == NetworkModel.ServerClient && na.IAmActingAuthority) || */na.IsMine)
			{
				ApplyTeleportLocally(OfftickFrame);
				ignoringStateChanges = false;
				State = State.Alive;
			}

			foreach (INstStart cb in iNstStart)
				cb.OnNstStart();
		}

		public void OnFirstAppliedFrameZero(Frame frame)
		{
			ApplyTeleportLocally(frame);
		}

		/// <summary>
		/// Automatically determine what the kinematic and interpolations settings should be. Rerun this if ownership of an NST changes.
		/// </summary>
		public virtual void ApplyAutoKinematic()
		{
			if (autoKinematic && rb != null)
			{

#if UNITY_2018_3_OR_NEWER
				// For 2018.3 new physics, just set this to Speculative to avoid constant warnings.
				if (rb.collisionDetectionMode != CollisionDetectionMode.ContinuousSpeculative)
					rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
#endif

				// if the prefab rb isn't set to isKinematic, set to to kinematic for dumb clients. Leave it as is for owner
				// We are assuming how it is set is how the developer wanted it to be for the owner.
				if (!rb.isKinematic)
				{
					rb.isKinematic = (!na.IsMine && !MasterNetAdapter.ServerIsActive);
				}

				rb.interpolation = (!na.IsMine || rb.isKinematic) ? RigidbodyInterpolation.None : RigidbodyInterpolation.Interpolate;
			}
		}

		public virtual void OnDestroy()
		{
			Shutdown();
			
			if (iNstOnDestroy != null)
				foreach (INstOnDestroy cb in iNstOnDestroy)
					cb.OnNstDestroy();
		}

		public void OnNetworkDestroy()
		{
			Shutdown();

			if (iNstOnNetworkDestroy != null)
				foreach (INstOnNetworkDestroy cb in iNstOnNetworkDestroy)
					cb.OnNstNetworkDestroy();
		}

		private void Shutdown()
		{
			NSTTools.UnregisterNstId(this, na);

			DebugWidget.RemoveDebugCross(gameObject);
			Destroy(ghostGO);
		}

#endregion

#region Updates

		/// <summary>
		/// NST does its Update() work on the MasterNSTUpdate() timing, but we still offer NST's Update as a basic update manager
		/// for components that also use NST's Awake/Start timing instead of their own. Prevents ugly situations where Update() can
		/// fire before OnNstStart()
		/// </summary>
		public void Update()
		{
			foreach (INstUpdate cb in iNstUpdate)
				cb.OnNstUpdate();
		}
		/// <summary>
		/// Called when the NSTMaster passes along its Update() to owned NST objects.
		/// </summary>
		public void MasterUpdate()
		{
			//// TODO not sure the best timing for this.
			//PollForDelayedActions();

			foreach (INstPreUpdate cb in iNstPreUpdate)
				cb.OnNstPreUpdate();

			if (!na.IsMine)
				InterpolateTransform();

			foreach (INstPostUpdate cb in iNstPostUpdate)
				cb.OnNstPostUpdate();
		}

		public void MasterLateUpdate()
		{
			foreach (INstPreLateUpdate cb in iNstPreLateUpdate)
				cb.OnNstPreLateUpdate();

			foreach (INstPostLateUpdate cb in iNstPostLateUpdate)
				cb.OnNstPostLateUpdate();
		}

		
		public bool PollForUpdate(ref UdpBitStream bitstream, int updateCounter, bool isOfftick)
		{
			foreach (INstPrePollForUpdate cb in iNstPrePollForUpdate)
				cb.OnNstPrePollForUpdate();

			if (na.IsMine)
			{
				// If this is on tick, but is a skipped frame for this NST - turn it into an offtick.
				isOfftick |= updateCounter % sendEveryXTick != 0;
				// TODO: Cache this division
				updateCounter = isOfftick ? frameCount : (updateCounter / sendEveryXTick);

				if (!isOfftick)
				{
					GenerateUpdate(ref bitstream, updateCounter, isOfftick);
					//skippedUpdates = 1;
					return true;
				}
				
				// if update is not due but there is an offtick item on one of the queues, send an offtick update (if NSTSettings allow for that)
				//TODO Make this nstREwind check use a public bool rather than exposing the entire queue

				// See if this nst has any offtick generators with queued items waiting to send
				foreach (IOfftickSrc cb in iOfftickSrc)
				{
					if (cb.OffticksPending > 0)
					{
						GenerateUpdate(ref bitstream, updateCounter, isOfftick);
						return true;
					}
				}
			}

			// no update this time
			return false;
		}

#endregion


#region Interpolation

		public FrameBuffer buffer;

		// calculated at startup based on number of skipped frames
		[HideInInspector] public float frameUpdateInterval;
		[HideInInspector] public float invFrameUpdateInterval;
		private bool waitingForFirstFrame = true;

		// interpolation vars
		private Vector3 posSnapshot;
		public CompressedElement lastSentCompPos = new CompressedElement();
		public CompressedElement lastSentUpperBits = new CompressedElement();
		//public CompressedElement lastRcvdRootCompPos = new CompressedElement();
		public Frame lastRcvdFrame;

		[HideInInspector] public Vector3 lastSentPos;

		float nudge = 0;

		private Vector3 targetVelocity;
		private Vector3 expectedPosNextUpdate;

		//cached interpolation values
		private Frame _currFrame;
		private Frame _prevFrame;
		private float _prevEndTime;
		private float _currEndTime;

		private void InterpolateTransform()
		{
			_currFrame = buffer.currentFrame;
			_prevFrame = buffer.prevAppliedFrame;
			// If we need a new Frame (lerped to end of last one or waiting at start)
			if (Time.time >= _currFrame.endTime)
			{
				// Still no Frame has arrived - nothing to do yet
				if (waitingForFirstFrame == true && CurrentIndex == 0 && buffer.validFrameMask <= 1)
				{
					XDebug.Log(!XDebug.logInfo ? null : 
						(Time.time + " " + name + " <color=black>Still waiting for first frame update." + "</color>" + "  buffer..." + buffer.validFrameMask));

					_currFrame.endTime = Time.time;
					return;
				}

				waitingForFirstFrame = false;

				foreach(INstOnEndInterpolate cb in iNstOnEndInterpolate)
					cb.OnEndInterpolate(_currFrame);

				// Testing for very low framerates - play catch-up if the framerate is causing a backlog.
				int numOfFramesOverdue = (int)((Time.time - _currFrame.endTime) * invFrameUpdateInterval);

				if (numOfFramesOverdue > 1)
				{
					// Get the real number of frames we seem to be overdue, which is the current buffer size - the desired size.
					numOfFramesOverdue = Mathf.Max(0, (int)((buffer.CurrentBufferSize - desiredBufferMS) / frameUpdateInterval));
					_currFrame.endTime = Time.time;
				}
				
				// For loop is to catch up on frames if the screen update is slower than the fixed update.
				for (int overduecount = numOfFramesOverdue; overduecount >= 0; --overduecount)
				{
					//Debug.Log(
					XDebug.Log(!XDebug.logInfo ? null :
						(Time.time + " fid: " + CurrentIndex + " <color=black><b>Finding Next Frame To Interpolate.</b></color> " + " NST:" + NstId + " " + name + 
						"\nValid Frames: " + buffer.PrintBufferMask(CurrentIndex)));

					// THIS IS THE MAIN FIND SECTION WHERE NEW FRAMES ARE FOUND

					foreach (INstOnSnapshotToRewind cb in iNstOnSnapshotToRewind)
						cb.OnSnapshotToRewind(_currFrame);

					DebugWidget.Move(gameObject, _currFrame.rootPos, _currFrame.RootRot, (int)debugXform, (int)DebugXform.HistorySnapshot);

					// Find and prepare the next frame
					Frame next = buffer.DetermineAndPrepareNextFrame(svrWaitingForOwnerTeleportConfirm);

					buffer.prevAppliedFrame = _currFrame;
					_prevFrame = _currFrame; // local cached ref

					buffer.currentFrame = next;
					_currFrame = next;


					int frameid = CurrentIndex;
					
					// Later joiners may be opting to not display this object until a complete update arrives
					if (ignoringStateChanges)
					{
						if ((ignoreStateUntil == KeyType.AnyUpdate) ||
							(ignoreStateUntil == KeyType.FrameZero && frameid == 0) ||
							(ignoreStateUntil == KeyType.RootPosKey && _currFrame.rootBitCullLevel == BitCullingLevel.NoCulling))
						{
							ignoringStateChanges = false;
							
							// TODO: this is assuming that the client state is the authority here, even though it may not be the acting auth?
							State = _currFrame.state;

							foreach (INstOnFirstAppliedFrameZero cb in iNstOnFirstAppliedFrameZero)
								cb.OnFirstAppliedFrameZero(_currFrame);
						}
					}

					//Dumb clients apply the teleport now that is is coming off the buffer. Don't teleport twice in a row.
					if (_currFrame.updateType.IsTeleport() && !_prevFrame.updateType.IsTeleport() && !na.IAmActingAuthority)// !na.IsServer)
					{
						ApplyTeleportLocally(_currFrame, true);
					}
					
					//Debug.Log(
					XDebug.Log(!XDebug.logInfo ? null : 
						(Time.time + " NST:" + NstId + " <b> Last Frame was " + _prevFrame.frameid + ".  New Frame is: " + _currFrame.frameid + "</b> type:" + _currFrame.updateType + " " +
						_currFrame.rootBitCullLevel +	" " + _currFrame.compPos + "   " + _currFrame.rootPos + " rot: " + _currFrame.RootRot));

					posSnapshot = _prevFrame.rootPos;

					if (sendOnEvent != 0) //  syncRootPosition != RootPositionSync.Disabled)
					{
						// Treat a move greater than the threshold apply a soft teleport.
						if (_currFrame.rootBitCullLevel != BitCullingLevel.DropAll)
						{
							if ((_currFrame.rootPos - posSnapshot).SqrMagnitude(includedAxes) > (teleportThreshold * teleportThreshold))
							{
								XDebug.LogWarning(!XDebug.logWarnings ? null :
									(Time.time + " " + name + " nid:" + NstId + "<color=red><b> Automatic teleport</b></color> due to threshold exceeded (dist:" + Vector3.Distance(_currFrame.rootPos, posSnapshot) + ") This may be caused be lost packets or by too low a teleport threshold on your NST. If you are seeing this and didn't expect a teleport, " +
									"increase the teleport threshold on NetworkSyncTransform for object " + name + ".\n" +
									"Curr: " + _currFrame +"\n" +
									"Snap: " + posSnapshot), true, false);

								ApplyTeleportLocally(_currFrame, false);
							}
						}

						DebugWidget.Move(gameObject, _currFrame.rootPos, _currFrame.RootRot, (int)debugXform, (int)DebugXform.Uninterpolated);
					}

					foreach (INstOnStartInterpolate cb in iNstOnStartInterpolate) 
						cb.OnStartInterpolate(_currFrame);

					// Recalculate the buffer size (This may need to happen AFTER the valid flag is set to false to be accurate) - Not super efficient doing this test too often
					buffer.UpdateBufferAverage();

					// nudge UNLESS we are running through a backlogged frames OR the buffer is empty (nudging an empty buffer will result in a lot of resyncs)
					nudge = (overduecount > 0) ? 0 :
						(desiredBufferMS - buffer.bufferAverageSize) * bufferDriftCorrectAmt;
					
					XDebug.Log(!XDebug.logInfo ? null : 
						("nudge " + nudge + "buffer.bufferAverageSize " + buffer.bufferAverageSize + "  desiredms: " + desiredBufferMS));

					_currFrame.endTime = _prevFrame.endTime + frameUpdateInterval + nudge;

					// Mark the current frame as no longer pending in the mask
					buffer.SetBitInValidFrameMask(CurrentIndex, false);

					if (!na.IAmActingAuthority)// && CurrentFrame.updateType.IsTeleport())
						State = _currFrame.state;
				}

				// Cache frame end times for interpolation
				_prevEndTime = _prevFrame.endTime;
				_currEndTime = _currFrame.endTime;
			}

			/// End getting next frame from buffer... now do the Interpolating.

			//float t = Mathf.InverseLerp(posSnapshotTime, CurrentFrame.endTime, Time.time - Time.deltaTime);
			float t = Mathf.InverseLerp(_prevEndTime, _currEndTime, Time.time);

			// If any root motion is possible, run the root pos interpolation.
			if (sendOnEvent != 0)
			{
				expectedPosNextUpdate = gameObject.Lerp(posSnapshot, _currFrame.rootPos, includedAxes, t);

				Vector3 errorForgiveAmt = new Vector3(0, 0, 0);
				
				// Apply any server interventions through this callback before applying the root lerp
				if (MasterNetAdapter.ServerIsActive && !na.IsMine)
					foreach (INstOnSvrInterpolateRoot cb in iNstOnSvrInterpRoot)
						errorForgiveAmt += cb.OnSvrInterpolateRoot(_currFrame, posSnapshot, expectedPosNextUpdate, t - Time.deltaTime);

				//Interpolate the root position
				// if there are no physics, we aren't moving this with the physics push.

				//if (!rb || rb.isKinematic)
					gameObject.transform.position = expectedPosNextUpdate;
				//else
				//	rb.MovePosition(expectedPosNextUpdate);

			}

			foreach (INstOnInterpolate cb in iNstOnInterpolate)
				cb.OnInterpolate(t);
		}

#endregion

#region Custom Events

		private Queue<byte[]> customEventQueue = new Queue<byte[]>();
		
		/// <summary>
		/// Tack your own data on the end of the NST syncs. This can be weapon fire or any other custom action.
		/// This will trigger the OnCustomMsgSndEvent on the sending machine and OnCustomMsgRcvEvent on receiving machines.
		/// </summary>
		/// <param name="userData"></param>
		public void SendCustomEvent(byte[] userData)
		{
			customEventQueue.Enqueue(userData);
		}

		/// <summary>
		/// This overlad will accept just about anything you put into a struct, so be careful. Limit your datatypes to JUST the smallest compressed primatives and don't include methods 
		/// or properties in your custom struct. Otherwise this could bloat your net traffic fast.
		/// This will trigger the OnCustomMsgSndEvent on the sending machine and OnCustomMsgRcvEvent on receiving machines.
		/// </summary>
		/// <typeparam name="T">A custom struct of your own making.</typeparam>
		public void SendCustomEvent<T>(T userData) where T : struct
		{
			customEventQueue.Enqueue(userData.SerializeToByteArray());
		}

#endregion


#region Teleport

		private bool svrWaitingForOwnerTeleportConfirm; // a teleport has occurred, next outgoing packet needs to indicate that.
		private bool ownrNeedsToSendTeleportConfirm;

		public void Teleport(Transform tr)
		{
			Teleport(tr.position, tr.rotation);
		}

		/// <summary>
		/// Public method for initiating a Teleport. Only can be initiated by the client with authority.
		/// </summary>
		public void Teleport(Vector3 pos, Quaternion rot)
		{
			if (na.IsMine)
			{
				XDebug.Log(!XDebug.logInfo ? null : ("TELEPORT " + name + " " + NstId));
				cachedTransform.position = pos;
				cachedTransform.rotation = rot;

				/// Set the offset ticks scene to match current, since it hasn't been getting any updates that would let it know about scene changes.
				OfftickFrame.sceneIndex = NSTSceneManager.CurrentSceneIndex;
				OfftickFrame.CaptureCurrentTransforms();

				if (!MasterNetAdapter.ServerIsActive)
				{
					svrWaitingForOwnerTeleportConfirm = true;
				}

				if (na.IsMine)
					ownrNeedsToSendTeleportConfirm = true;

				// 'Harden' current translate of teleportOveridden elements.
				ApplyTeleportLocally(OfftickFrame, true);
			}
			else
			{
				XDebug.LogWarning(!XDebug.logWarnings ? null : ("You are trying to teleport an object from a client that doesn't have authority over that object. Only the acting authority may initiate teleporting of NST objects."));
				return;
			}
		}
		
		/// <summary>
		/// Move root object on this client only without lerping or interpolation. Clears all buffers and snapshots. 
		/// HardTeleport will clear the buffer, soft teleport just disables the RB to avoid lerping.
		/// </summary>
		private bool ApplyTeleportLocally(Frame frame, bool hardTeleport = true)
		{
			Vector3 pos = (Vector3)frame.compPos.Decompress();

			//Debug.Log(
			XDebug.Log(!XDebug.logInfo ? null :
				(Time.time + " nid:"  + NstId + " fid:" + frame.frameid + " <color=red><b>Teleport</b></color> hard:" + hardTeleport + " new: " +
				((rb != null) ? (" rbVel:" + rb.velocity * 100f) : "" ) + " go.pos " + cachedTransform.position + "  " +
				frame.compPos + " / " + pos + " + Distance: " + Vector3.Distance(pos, cachedTransform.position) + "\n" + frame));
			
			bool wasKinematic = false;

			if (rb != null)
			{
				rb.Sleep();
				wasKinematic = rb.isKinematic;
				rb.velocity = new Vector3(0, 0, 0);
				rb.isKinematic = true;
			}

			// Clear ALL old frame buffer items to stop warping. They are all invalid now anyway.
			if (hardTeleport)
			{
				buffer.prevAppliedFrame.CompRootPos = frame.compPos;
				buffer.currentFrame.CompRootPos = frame.compPos;

				lastRcvdFrame = frame;
				//lastRcvdRootCompPos.CopyFrom(frame.compPos);
				lastSentCompPos.CopyFrom(frame.compPos);
				lastSentPos = pos;
				buffer.validFrameMask = 0;
			}

			posSnapshot = pos;

			gameObject.SetPosition(pos, includedAxes); //transform.position = pos;

			// Notify applicable elements of the incoming teleport
			foreach (INstTeleportApply cb in iNstTeleportApply)
				cb.OnTeleportApply(frame);

			if (rb != null)
				rb.isKinematic = wasKinematic;

			return true;
		}

#endregion

#region Message Transmission

		public void OnGenerateUpdateType(Frame frame, ref UdpBitStream bitstream, ref bool forceKey)
		{
			if (customEventQueue.Count > 0)
			{
				frame.updateType |= UpdateType.Cust_Msg;
				byte[] customMsg = customEventQueue.Dequeue();
				Buffer.BlockCopy(customMsg, 0, frame.customData, 0, customMsg.Length);
				frame.customMsgSize = customMsg.Length;
				forceKey |= (sendOnEvent.OnCustomMsg() && frame.updateType.IsCustom());
			}
		}

		private int sequentialRootPosKeyCount = 0;
		private int sequentialTeleportCount = 0;
		private BitCullingLevel forcedBitCullLevel = BitCullingLevel.DropAll;

		//PLAYER WITH AUTHORITY RUNS THIS EVERY TICK
		/// <summary>Determine which msgType is needed and then call Generate using that type</summary>
		private void GenerateUpdate(ref UdpBitStream bitstream, int frameid, bool isOfftick = false)
		{
			// Get new packetID and write to bitstream
			Frame frame = buffer.frames[frameid];

			// for the local player, ref the current frame for other uses.
			if (na.IsMine)
				buffer.currentFrame = frame;

			if (isOfftick)
			{
				WorldBoundsSO.defaultWorldBoundsCrusher.Compress(frame.compPos, cachedTransform.position);
			}
			else
			{
				lastSentPos = (rb) ? rb.position : cachedTransform.position;
				WorldBoundsSO.defaultWorldBoundsCrusher.Compress(frame.compPos, lastSentPos);
			}

			bool forceKey = false;
			
			frame.updateType = 0;

			// TODO these flags can likely be moved to write now
			// If a teleport or state change has occurred, indicate that with next position update.
			if (ownrNeedsToSendTeleportConfirm)
			{
				sequentialTeleportCount = sequentialKeys;

				// If I am the authority, rather than waiting for server confirm to clear the teleport needed flag, we clear it outself.
				if (na.IAmActingAuthority)
					ownrNeedsToSendTeleportConfirm = false;
			}
			if (sequentialTeleportCount > 0)
			{
				sequentialTeleportCount--;
				frame.updateType |= UpdateType.Teleport;
				forceKey |= (sendOnEvent.OnTeleport());
			}

			// Let Offticks and such (like CustomData and Rewind) flag the update type and add their payload to the frame
			// if they have something to say.
			foreach (INstGenerateUpdateType cb in iGenerateUpdateType)
				cb.OnGenerateUpdateType(frame, ref bitstream, ref forceKey);

			frame.rootBitCullLevel = BitCullingLevel.DropAll;

			// TODO some of this doesn't need to happen every time. First check that a pos will be needed
			// Check for what kind of msg needs to be sent
			if (sendOnEvent != 0)  //(syncRootPosition != RootPositionSync.Disabled)
			{
				forceKey |= IsKeyframe(frameid);

				BitCullingLevel bestCullLvl = bitCullingLevel;
				//test to make sure movement doesn't exceed keyframe limits - don't use a keyframe if to does

				if (bitCullingLevel != BitCullingLevel.NoCulling)
				{

					bestCullLvl = CompressedElement.FindBestBitCullLevel(lastSentCompPos, frame.compPos, bitCullingLevel);

					if (bestCullLvl < forcedBitCullLevel) //  bestCullLvl == BitCullingLevel.NoCulling) //  BitCullingLevel.DropThird ||  bcl == BitCullingLevel.DropTopHalf)
					{
						forcedBitCullLevel = bestCullLvl;
						sequentialRootPosKeyCount = sequentialKeys;
					}
				}
				// Brute force send x number of higher bit positions to make updates more loss resilient.
				if (sequentialRootPosKeyCount != 0)
				{
					bestCullLvl = forcedBitCullLevel;
					sequentialRootPosKeyCount--;
				}
				else
				{
					forcedBitCullLevel = bestCullLvl;
				}

				frame.rootBitCullLevel =
					forceKey ? BitCullingLevel.NoCulling :
					sendOnEvent.EveryTick() ? bestCullLvl :
					bestCullLvl != BitCullingLevel.DropAll && sendOnEvent.OnChanges() ? bestCullLvl :
					BitCullingLevel.DropAll;
			}

			// Generate an update with determined msgtype
			WriteUpdate(ref bitstream, frame);
		}

		private bool IsKeyframe(int frameid)
		{
			return (keyEvery != 0) && frameid != frameCount && (frameid % keyEvery == 0);
		}
		/// <summary>
		/// Serialize the appropriate data the NetworkWriter.
		/// </summary>
		private void WriteUpdate(ref UdpBitStream bitstream, Frame frame)
		{


			BandwidthUsage.Start(ref bitstream, BandwidthLogType.UpdateSend);
			BandwidthUsage.SetName(this);

			// Leading bit indicates this is an update and not eos
			bitstream.WriteBool(true); 
			BandwidthUsage.AddUsage(ref bitstream, "NotEOS");

			bitstream.WriteInt((int)frame.updateType, 3);
			BandwidthUsage.AddUsage(ref bitstream, "UpdateType");

			bitstream.WriteUInt(NstId, HeaderSettings.single.BitsForNstId);
			BandwidthUsage.AddUsage(ref bitstream, "NstId");

			// Store where our update length needs to be rewritten later - start counting bits used from here.
			bitstream.ptr += NSTMaster.UPDATELENGTH_BYTE_COUNT_SIZE;
			int startPtr = bitstream.ptr;
			BandwidthUsage.AddUsage(ref bitstream, "DataLength");

			IntegrityCheck.WritePosition(ref bitstream, frame.rootPos);

			bitstream.WriteInt((int)State, 2);
			frame.state = State;
			BandwidthUsage.AddUsage(ref bitstream, "State");

			//Write rootPos send type
			if (!IsKeyframe(frame.frameid))
				bitstream.WriteInt((int)frame.rootBitCullLevel, 2);

			BandwidthUsage.AddUsage(ref bitstream, "Root Pos Comp Type");

			// Send position key or lowerbits
			if (frame.rootBitCullLevel != BitCullingLevel.DropAll)
			{
				// TODO: This needs to convert rootsendtype to bitcullinglevel!!
				//frame.compPos.Serialize(bitstream.Data, ref bitstream.ptr, includedAxes, frame.rootBitCullLevel);
				WorldBoundsSO.defaultWorldBoundsCrusher.Write(frame.compPos, bitstream.Data, ref bitstream.ptr, includedAxes, frame.rootBitCullLevel);

				// Write the position to own frame for server auth functions later.
				frame.OverwriteRootPos((Vector3)frame.compPos.Decompress());

				/// Align own ghost to outgoing lossy position. Allows ghost to be used for replicating actual pos/rot of objects as the network sees them (useful for weapon fire)
				if (ghostGO)
					ghostGO.transform.position = frame.rootPos;

				lastSentCompPos.CopyFrom(frame.compPos);
				lastSentPos = frame.rootPos;

				// If this is a full pos, log it - lastSentCompPosKey is used to determine if upperbits have changed in future updates.
				if (frame.frameid != frameCount && frame.rootBitCullLevel == BitCullingLevel.NoCulling)
				{
					lastSentUpperBits.CopyFrom(frame.compPos);
				}
			}
			BandwidthUsage.AddUsage(ref bitstream, "Root Pos");

			// Inject Elements
			foreach (INstBitstreamInjectFirst cb in iBitstreamInjectFirst)
				cb.NSTBitstreamOutgoingFirst(frame, ref bitstream);

			BandwidthUsage.AddUsage(ref bitstream, "Elements");
			IntegrityCheck.WriteCheck(ref bitstream);

			// Inject Animator (and any other server independent pass through elements)
			foreach (INstBitstreamInjectSecond cb in iBitstreamInjectSecond)
				cb.NSTBitstreamOutgoingSecond(frame, ref bitstream);

			BandwidthUsage.AddUsage(ref bitstream, "Animator");
			IntegrityCheck.WriteCheck(ref bitstream);

			// Inject RewindCasts
			foreach (INstBitstreamInjectThird cb in iBitstreamInjectsLate)
				cb.NSTBitstreamOutgoingThird(frame, ref bitstream);

			BandwidthUsage.AddUsage(ref bitstream, "Casts");
			IntegrityCheck.WriteCheck(ref bitstream);

			// Inject user data
			if (frame.updateType.IsCustom())
			{
				bitstream.WriteByteArray(frame.customData, frame.customMsgSize);
			}

			// Notify all interfaces that we are about to send a frame update, Snapshots are applied on this timing.
			foreach (INstOnSndUpdate cb in iNstOnSndUpdate)
				cb.OnSnd(frame);

			IntegrityCheck.WriteCheck(ref bitstream);

			// Determine the length written rounded up, and determine the padding needed to make this an even byte
			int bitWritten = bitstream.ptr - startPtr;
			int evenBytes = (bitWritten >> 3) + ((bitWritten % 8 == 0) ? 0 : 1);
			int padding = (evenBytes << 3) - bitWritten;

			XDebug.LogError((!XDebug.logErrors || evenBytes <= NSTMaster.MAX_BYTES_PER_NST) ? null : 
				("NST on " + name + " is attempting to create an update that exceeds the max allowed bytes."), evenBytes > NSTMaster.MAX_BYTES_PER_NST, true);

			// jump back and rewrite the size now that we know it. Then forward to where we were, plus the padding needed to make this even bytes.
			bitstream.WriteIntAtPos(evenBytes, NSTMaster.UPDATELENGTH_BYTE_COUNT_SIZE, startPtr - NSTMaster.UPDATELENGTH_BYTE_COUNT_SIZE);
			bitstream.ptr += padding; // advance the ptr to make this an even number of bytes

			BandwidthUsage.AddUsage(ref bitstream, "Padding");
			BandwidthUsage.PrintSummary();


			XDebug.Log(!XDebug.logInfo ? null :
				(Time.time + " NST:" + NstId + " <b>Sent </b>" + frame));

			// Update the debug transform if it is being used
			DebugWidget.Move(gameObject, frame.rootPos, frame.RootRot, (int)debugXform, (int)DebugXform.LocalSend);
		}

#endregion

#region Message Reception


		private const int TEST_BITS_SIZE = 32;
		private const int TEST_VAL = 1044480;

		public Frame mostRecentRcvdFrame;
		CompressedElement ownrSentCompPos = new CompressedElement();

		/// <summary>
		/// Universal receiver for incoming frame updates. Both server and clients run through this same method. If it is the server 'asServer' will be true.
		/// Frames updates are read in from the master bitstream, and parsed to their appropriately numbered frame in the frame buffer. Frames with frameid of zero
		/// are flagged as 'immediate' - they are not added to the buffer but rather are used to pass through offtick updates such as weapon and teleport commands.
		/// </summary>
		/// <param name="bitstream">The master bitstream frame vars will be read from.</param>
		/// <param name="outstream">Only valid if this is flagged asServer. This is the modified bitstream forwarded to other clients after the server receives an update.</param>
		/// <param name="updateType">Enum that indicates if this update is a special case, such as a Teleport, rewind cast, or has an attached user custom message.</param>
		/// <param name="lengthInBytes">Specifies where this NST update ends in the master bitstream. Don't bother trying to figure out what it does.</param>
		/// <param name="asServer">Indicates that this update is being read as the server. This is needed for the server is a host, and this needs to run as both server and as client.</param>
		/// <returns></returns>
		public Frame ReadUpdate(
			ref UdpBitStream bitstream, ref UdpBitStream outstream, int frameid, bool isOfftick, UpdateType updateType, 
			int lengthInBytes, int sceneIndex, bool sceneOutOfSync, bool asServer)
		{
			// Log the bitstream pointer position
			int startPtr = bitstream.ptr;

			

			// Treat onticks that are supposed to be skipped by this nst as offticks
			isOfftick |= (frameid % sendEveryXTick != 0);
			frameid = isOfftick ? frameCount  : (frameid / sendEveryXTick);

			// Cache some stuff
			bool iskeyframe = IsKeyframe(frameid);
			bool iAmActingAuthority = na.IAmActingAuthority;
			bool isMine = na.IsMine;
			Frame frame = buffer.frames[frameid];
			frame.sceneIndex = sceneIndex;

			if (!isOfftick)
				mostRecentRcvdFrame = frame; // log most recently arrived frame - used by server authority

			IntegrityCheck.ReadPosition(ref bitstream, ref outstream, false, asServer, "realpos ");

			frame.state = (State)(bitstream.ReadInt(2));
			BandwidthUsage.AddUsage(ref bitstream, "State");

			frame.updateType = updateType;

			// Read in the root pos comp type, unless this is a key... then Full is assumed
			frame.rootBitCullLevel = (!iskeyframe) ? (BitCullingLevel)bitstream.ReadInt(2) : BitCullingLevel.NoCulling;
			BandwidthUsage.AddUsage(ref bitstream, "Root Culling Flag");

			
			// Store store the values that the owner originally sent before we start altering the frame
			ownrSentCompPos.CopyFrom(frame.compPos);
			//Vector3 ownrSentPos = frame.rootPos;

			// READ Compressed Root Position (if one was written)
			if (frame.rootBitCullLevel < BitCullingLevel.DropAll)
			{
				WorldBoundsSO.defaultWorldBoundsCrusher.Read(frame.compPos, bitstream.Data, ref bitstream.ptr, includedAxes, frame.rootBitCullLevel);
			}
			else
			{
				frame.compPos.Clear();
			}


			BandwidthUsage.AddUsage(ref bitstream, "Root Pos");

			// READ CHILD ELEMENTS (including root rotation) and animator parameters.
			foreach (INstBitstreamInjectFirst cb in iBitstreamInjectFirst)
				cb.NSTBitstreamIncomingFirst(frame, buffer.currentFrame, ref bitstream, asServer);

			BandwidthUsage.AddUsage(ref bitstream, "Elements");

			frame.CompletePosition(lastRcvdFrame);

			if (updateType.IsTeleport())
			{
				//Debug.Log(
				XDebug.Log(!XDebug.logInfo ? null :
					(Time.time + " <b>Inc Teleport</b>" + frame + " svrwaiting?: " + svrWaitingForOwnerTeleportConfirm));

				if (isMine && !iAmActingAuthority && !sceneOutOfSync)
				{
					//OwnerTeleportCommandHandler(frame);
					ApplyTeleportLocally(frame, true);
					// Set this AFTER OwnerTeleportCommandHandler, is used to check for repeated sends from server
					ownrNeedsToSendTeleportConfirm = true;
				}

				// Apply immediate teleports
				// TODO: Should all teleports be treated as immediate?
				if (isOfftick && !asServer)
				{
					ApplyTeleportLocally(frame, true);

					foreach (INstTeleportApply cb in iNstTeleportApply)
						cb.OnRcvSvrTeleportCmd(frame);
				}

				if (asServer)
					svrWaitingForOwnerTeleportConfirm = false;
			}
			// If this is not a teleport, and this is the owner without authority... this means the server is no longer requesting teleport confirm. Clear flag.
			else if (isMine && !iAmActingAuthority)
			{ 
				ownrNeedsToSendTeleportConfirm = false;
			}

			// If this is the server and we are waiting for a confirm... don't accept new updates for teleport overriden elements/root.
			if (svrWaitingForOwnerTeleportConfirm)
			{
				frame.rootBitCullLevel = BitCullingLevel.NoCulling;

				/// TODO: this is not compatible with scene changes/mapbounds changes
				frame.compPos.CopyFrom(lastSentCompPos);
				frame.rootPos = lastSentPos;

				//  Keep resending teleport values for elements until confirmed - Move this to interface callback
				TransformElement[] tes = nstElementsEngine.transformElements;
				int count = nstElementsEngine.transformElements.Length;
				for (int eid = 0; eid < count; ++eid)
				{
					TransformElement te = tes[eid];
					if (te.teleportOverride)
					{
						te.frames[frame.frameid].compXform.CopyFrom(te.lastSentCompressed);
						te.frames[frame.frameid].xform = te.lastSentTransform;
					}
				}
			}

			// used to let arrivals know that they shouldn't overwrite elements with TeleportOverride=true - as they may have just been overridden with a teleport value
			// and should not be changed back by outdated incoming updates.
			bool isMidTeleport = ownrNeedsToSendTeleportConfirm || !buffer.prevAppliedFrame.updateType.IsTeleport();

			// If this frame has already been extrapolated and is mid-interpolation, overwrite the lowerbits with the correct incoming ones
			// Don't overwrite if we appear to be mid teleport.
			if (!isMine && frameid == CurrentIndex && !isMidTeleport && !sceneOutOfSync) 
			{
				//TODO may need to fire a OnStartInterpolation callback somehwere how since this means any custom data and such that arrived late never will have fired.
				frame.CompletePosition(lastRcvdFrame);
				frame.OverwriteRootPos((Vector3)frame.compPos.Decompress());
			}

			// adjust teleport flag if server has changed it (server owned objects leave alone) teleport flag to indicate of server is still waiting for a confirm.
			if (asServer && !isMine)
			{
				if (svrWaitingForOwnerTeleportConfirm)
					frame.updateType |= UpdateType.Teleport;
				else
					frame.updateType &= ~UpdateType.Teleport;
			}

			///State Placeholder
			int outgoingStatePos = outstream.ptr;
			outstream.ptr += 2;

			/// Apply ServerAuthority offset - Adjust outgoing rootPos to server delta. This delta is a bit outdated, but better than nothing.
			if (asServer)// && frame.rootBitCullLevel.IsPosType())
			{
				if (!iskeyframe)
					outstream.WriteInt((int)frame.rootBitCullLevel, 2);

				//TODO: Make this handle all level of bitculling
				if (frame.rootBitCullLevel != BitCullingLevel.DropAll)
					//frame.compPos.Serialize(outstream.Data, ref outstream.ptr, includedAxes, frame.rootBitCullLevel);
					WorldBoundsSO.defaultWorldBoundsCrusher.Write(frame.compPos, outstream.Data, ref outstream.ptr, includedAxes, frame.rootBitCullLevel);
			}
			
			//Debug.Log(
			XDebug.Log(!XDebug.logInfo ? null :
				(Time.time + " NST:" + NstId + " <b>RCV</b> " + frame));

			// Callback used by server to forward child elements and animator params to clients. 
			if (asServer)
				foreach (INstBitstreamInjectFirst cb in iBitstreamInjectFirst)
					cb.NSTBitstreamMirrorFirst(frame, ref outstream, svrWaitingForOwnerTeleportConfirm);

			IntegrityCheck.ReadCheck(ref bitstream, ref outstream, name + " " + NstId + " Post First Inject asServer=" + asServer, asServer);

			// READ animator parameters.
			// TODO: outstream write needs to be decoupled
			foreach (INstBitstreamInjectSecond cb in iBitstreamInjectSecond)
				cb.NSTBitstreamIncomingSecond(frame, buffer.currentFrame, ref bitstream, ref outstream, asServer, svrWaitingForOwnerTeleportConfirm);

			BandwidthUsage.AddUsage(ref bitstream, "Animator");
			IntegrityCheck.ReadCheck(ref bitstream, ref outstream, name + " " + NstId + " Post Second Inject asServer=" + asServer, asServer);


			// Callback used by Rewind to read out any casts and apply to the rewind ghost for this object
			foreach (INstBitstreamInjectThird cb in iBitstreamInjectsLate)
				cb.NSTBitstreamIncomingThird(frame, ref bitstream, ref outstream, asServer);

			BandwidthUsage.AddUsage(ref bitstream, "Casts");

			IntegrityCheck.ReadCheck(ref bitstream, ref outstream, name + " " + NstId + " Post Third Inject asServer=" + asServer, asServer);

			DebugWidget.Move(gameObject, frame.rootPos, frame.RootRot, (int)debugXform, (int)DebugXform.RawReceive);

			// if this is a Custom message, pass it along as an event.
			if (updateType.IsCustom())
			{
				// measure what is left in this update buffer - sent that out as the custom message
				int remainingBits = (lengthInBytes << 3) - (bitstream.ptr - startPtr);

				//TODO: Questionable rounding here....
				int remainingBytes = (remainingBits >> 3); // + ((remainingBits % 8 == 0) ? 0 : 1);
				// Read the custom data directly into the frame buffer to skip some steps. 
				bitstream.ReadByteArray(frame.customData, remainingBytes);

				if (asServer)
					outstream.WriteByteArray(frame.customData, remainingBytes);

				frame.customMsgSize = remainingBytes;
			}

			BandwidthUsage.AddUsage(ref bitstream, "Custom");

			BandwidthUsage.PrintSummary();

			foreach (INstOnRcvUpdate cb in iNstOnRcvUpdate)
				cb.OnRcv(frame);

			//This belongs AFTER all alterations to incoming pos/rot have been made
			// Store the final adjusted rootCompPos for next update to use for upperbits guessing.
			//lastRcvdRootCompPos.CopyFrom(frame.compPos);
			lastRcvdFrame = frame;

			//TODO do I really want to allow offtick state changes?
			if (!iAmActingAuthority && (isMine || frame.frameid == frameCount)) //  !iAmAuthority )
				if (frame.state != _state)
					State = frame.state;

			// Overwrite the outgoing state now that all of the 'action' should have been processed and state may have changed.
			if (asServer)
				outstream.WriteIntAtPos((int)((iAmActingAuthority && !ignoringStateChanges) ? State : frame.state), 2, outgoingStatePos);


			// If this frame came late and is now mid-interpolation... notify interface callbacks (authority items will always be current - ignore them)
			if (frameid == CurrentIndex)
			{
				if (!na.IsMine)
					foreach (INstOnStartInterpolate cb in iNstOnStartInterpolate)
						cb.OnStartInterpolate(frame, true, isMidTeleport);

				return frame;
			}

			if (isOfftick)
				return frame;


			buffer.FlagFrameAsValid(frame);
			return frame;
		}
#endregion
	}
}

#endif