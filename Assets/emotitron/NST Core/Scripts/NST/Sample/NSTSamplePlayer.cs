//Copyright 2018, Davin Carten, All rights reserved

#if PUN_2_OR_NEWER || MIRROR || !UNITY_2019_1_OR_NEWER

using UnityEngine;
using System.Collections;
using emotitron.NST.HealthSystem;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace emotitron.NST.Sample
{
	[AddComponentMenu("NST/Sample Code/NST Sample Player")]

	/// <summary>
	/// Sample code that interacts with the NetworkSyncTransform on a networked object. 
	/// This sample automatically ressurects players after X seconds by monitoring the INstState of the NST.
	/// for finding and managing your player and nonplayer objects.
	/// Note the use of interface callbacks such as OnNstStartLocalPlayer. These interfaces automatically register themselves
	/// with the root NST object, and are called at the indicated time segement when it occurs in the root NST, allowing for better control
	/// of order of execution.
	/// </summary>
	/// 
	public class NSTSamplePlayer : NSTComponent, INstOnStartLocalPlayer, INstStart, INstOnDestroy, INstState, INstPostUpdate // NetworkSyncTransform
	{
		public static Camera defaultCam;
		public static bool defaultCamActive = true;

		public Camera playerCamera;
		public GameObject playerMesh;
		//private Renderer rend;

		private GameObject myTeleportButton;
		[HideInInspector] public static NSTSamplePlayer localPlayer;
		private float timeSinceDeath;
		private bool alreadyBorn;


		/// <summary>
		/// Called after the NST on this gameobject completes running Awake().
		/// </summary>
		public override void OnNstPostAwake()
		{
			base.OnNstPostAwake();

			SceneManager.activeSceneChanged -= OnActiveSceneChanged;
			SceneManager.activeSceneChanged += OnActiveSceneChanged;

			// Find the various cameras
			if (playerCamera == null)
				playerCamera = GetComponentInChildren<Camera>(true);

			// Be sure the player cam is off so it doesn't accidently get mistaken for a main camera
			if (playerCamera)
				playerCamera.gameObject.SetActive(false);

			// default cam is static, so the first player that joins will find the camera that is 'on' call it the default.
			if (defaultCam == null)
				defaultCam = Camera.main;

			//if (playerMesh != null)
			//	rend = playerMesh.GetComponent<MeshRenderer>();
		}

		public static bool registeredSceneChangeListener = false;

		public static void OnActiveSceneChanged(Scene current, Scene next)
		{
			defaultCam = Camera.main;
			defaultCam.gameObject.SetActive(defaultCamActive);
		}

		/// <summary>
		/// Called after the NST on this gameobject completes running its OnStartLocalPlayer.
		/// </summary>
		public void OnNstStartLocalPlayer()
		{
			localPlayer = this;
		}

		/// <summary>
		/// Called after the NST on this gameobject completes running its OnStart.
		/// </summary>
		public void OnNstStart()
		{
			ApplyState(nst.State);
		}

		// Determine alive state AFTER damage is applied (interpolation finished) and BEFORE updates are sent out.
		/// <summary>
		/// Called before the NST on this gameobject sends out its frame update. Only owners send out updates
		/// </summary>
		public void OnNstPostUpdate()
		{
			if (na.IAmActingAuthority)
			{
				timeSinceDeath += Time.deltaTime;
				// Svr ressurect dead player after 2 seconds
				if (timeSinceDeath > 2f && nst.State != State.Alive)
				{
					nst.State = State.Alive;
				}
			}
		}

		private void SetCamera(bool enablePlayerCam)
		{
			if (playerCamera != null)
			{
				playerCamera.gameObject.SetActive(enablePlayerCam);

				if (defaultCam != null)
				{
					defaultCam.gameObject.SetActive(!enablePlayerCam);
					defaultCamActive = !enablePlayerCam;
				}
			}
		}

		public void OnNstDestroy()
		{
			if (localPlayer == this)
				SetCamera(false);
		}

		/// <summary>
		/// Callback from the root NST when alive status changes.
		/// </summary>
		/// <param name="newState"></param>
		public void OnNstState(State newState, State oldState)
		{
			bool isalive = newState.IsAlive();
			bool wasalive = oldState.IsAlive();

			// if this is a ressurection, move to a spawn point. Don't move if this is giving birth (first run) - the NM just assigned this location already.
			if (isalive && !wasalive && alreadyBorn)
			{
				if (na.IAmActingAuthority)
				{
					Transform tr = MasterNetAdapter.UNET_GetPlayerSpawnPoint();

					// if no spawn returned from the engine, lets look for a sample spawn point.
					if (!tr)
						tr = NSTSamplePlayerSpawn.GetRandomSpawnPoint();

					Vector3 pos = (tr) ? tr.position : Vector3.zero;
					Quaternion rot = (tr) ? tr.rotation : Quaternion.identity ;
					
					nst.Teleport(pos, rot);

				}
			}
			ApplyState(newState);

			// note time of death for respawn methods
			if (!isalive && wasalive)
				timeSinceDeath = 0;
		}

		public void ApplyState(State state)
		{
			bool isalive = state.IsAlive();

			if (playerMesh != null)
				playerMesh.SetActive(state.IsVisible());

			if (localPlayer == this)
				SetCamera(isalive);

			// note that this object now exists in the world, and will need to be moved to spawn points when it ressurects.
			if (isalive)
				alreadyBorn = true;
		}


#if UNITY_EDITOR

		[CustomEditor(typeof(NSTSamplePlayer))]
		[CanEditMultipleObjects]
		public class NSTSamplePlayerEditor : NSTSampleHeader
		{

		}
#endif

	}
}

#endif