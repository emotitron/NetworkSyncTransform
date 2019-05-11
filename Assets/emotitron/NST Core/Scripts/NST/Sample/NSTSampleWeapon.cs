//Copyright 2018, Davin Carten, All rights reserved

#if PUN_2_OR_NEWER || MIRROR || !UNITY_2019_1_OR_NEWER

using System.Collections.Generic;
using UnityEngine;
using emotitron.InputSystem;
using emotitron.Compression;
using emotitron.Utilities.Pooling;
using emotitron.NST.Weapon;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace emotitron.NST.Sample
{
	[AddComponentMenu("NST/Sample Code/NST Sample Weapon")]

	/// <summary>
	/// A very basic weapon example. Note that this uses the NetworkSyncTransform's 'SendCustomEvent' call. You can add your own custom data to this and it will be sent along
	/// in the next NST message. It will be receieved on all clients and generates the OnNstCustomMessageEvent event - which will contain a copy of that custom data.
	/// </summary>
	public class NSTSampleWeapon : NSTComponent, INstOnSndUpdate, INstOnRcvUpdate, INstOnEndInterpolate, INstStart
	{
		// Reusable temp list for GetComponents
		public static List<NSTSampleWeapon> weapons = new List<NSTSampleWeapon>();

		/// Sample custom message struct, you can create your own to send to the NST. 
		/// This is a convenience method only - all values will be sent over the network uncompressed.
		private struct PlayerFireCustomMsg
		{
			public byte weaponId;
			public Color color;
		}

		// Inspector Vars
		public GameObject originGO;
		public GameObject projPrefab;
		[Range(0,20)]
		public float projVelocity = 5f;
		[Range(0,10)]
		public float lifespanSecs = 2f;

		// Runtime vars
		//private bool hasAuthority;


		[Header("Input Triggers")]
		public InputSelectors inputSelectors = new InputSelectors(KeyCode.Space);

		[HideInInspector] public int weaponId = -1;

		public GameObject OriginGO { get { return originGO; } }

		public override void OnNstPostAwake()
		{
			base.OnNstPostAwake();

			// Make sure the weapons have not been initialized in the inspector (dev related)
			weaponId = -1;


			if (originGO == null)
			{
				originGO = gameObject;
			}
		}

		public void OnNstStart()
		{
			//if (nst != null)
			//	hasAuthority = nst.na.HasAuthority;

			// Collect all weapons on this NST and give them IDs based on their order in the heirarchy.
			// We use GetComponents since Awake() order cannot be trusted.
			// Test for -1 to see if this has already been run on this NST
			if (weaponId == -1)
			{
				nst.GetComponentsInChildren(true, weapons);

				int count = weapons.Count;
				for (int i = 0; i < count; ++i)
					weapons[i].weaponId = i;
			}

			if (projPrefab == null)
				projPrefab = CreatePlaceholderProjectile(Vector3.zero, Quaternion.identity, false);

			Pool.AddPrefabToPool(projPrefab, 16, 8, typeof(NSTSampleProjectile));
		}

		void Update()
		{
			if (!nst.na.IsMine)
				return;
			if (inputSelectors.Test())
			{
				PlayerFire();
			}
		}


		/// <summary>
		/// Player with local authority fires by calling this. This tells the NST to create a custom message and attach your data to it.
		/// </summary>
		/// <param name="wid"></param>
		public void PlayerFire()
		{

			PlayerFireCustomMsg customMsg = new PlayerFireCustomMsg
			{
				weaponId = (byte)weaponId,
				color = new Color(Random.value,
				Random.value, Random.value),
			};

			nst.SendCustomEvent(customMsg);
		}

		/// <summary>
		/// OnSnd fires on the originating client when a custom event is sent. The position and rotation information will contain the same
		/// lossy rounding errors/ranges that are being sent to the network. Useful for ensuring that your local events use the exact same pos/rot data
		/// the server and clients will be using (such as projectile vectors).
		/// </summary>
		public void OnSnd(Frame frame)
		{

			if (frame.updateType != UpdateType.Cust_Msg)
				return;

			PlayerFireCustomMsg weaponFireMsg = frame.customData.DeserializeToStruct<PlayerFireCustomMsg>();

			if (weaponFireMsg.weaponId != weaponId)
				return;

			FireBullet(frame, weaponFireMsg);
		}

		/// <summary>
		/// Offtick frames will never interpolate and are expected to be reacted to instantly.
		/// </summary>
		/// <param name="frame"></param>
		public void OnRcv(Frame frame)
		{
			if (frame.frameid == nst.buffer.nstFrameCount)
				OnEndInterpolate(frame);
		}

		/// <summary>
		/// When a custom message is taken from the buffer and applied for interpolation, the OnCustomMsgRcvEvent is fired. Note that the rotations 
		/// will only be correct if you have the NST set to update rotations on events. If it is set to 'changes only' these rotations values will be zero.
		/// </summary>
		public void OnEndInterpolate(Frame frame)
		{
			if (frame.updateType != UpdateType.Cust_Msg)
				return;

			// TODO: find a way to deserialize this only once
			PlayerFireCustomMsg weaponFireMsg = frame.customData.DeserializeToStruct<PlayerFireCustomMsg>();

			if (weaponFireMsg.weaponId != weaponId)
				return;

			// Don't call the fire graphics if this is the owner client - it already fired on send.
			if (frame.nst.na.IsLocalPlayer)
				return;

			FireBullet(frame, weaponFireMsg);

		}

		/// <summary>
		/// This is the weapon fire code created for this example. It instantiates a cosmetic (not network synced) projectile.
		/// For a real project you should considered using pooled objects for projectiles.
		/// </summary>
		private void FireBullet(Frame frame, PlayerFireCustomMsg msg)
		{
			Vector3 originPos;
			Quaternion originRot;

			originPos = originGO.transform.position;
			originRot = originGO.transform.rotation;

			Pool poolProj = Pool.Spawn(projPrefab, originPos, originRot, lifespanSecs);

			INstProjectile nstProj = poolProj.gameObject.GetComponent<INstProjectile>();

			if (nstProj != null)
				nstProj.OwnerNst = nst;

			poolProj.rb.velocity = poolProj.gameObject.transform.forward * projVelocity;

			// Changes color to color sent over the network as an example of how to use the Custom Message
			poolProj.GetComponentInChildren<MeshRenderer>().material.color = msg.color;
		}


		/// <summary>
		/// If no projectile prefab was entered in the inspector, Instantiate a sphere as a placeholder.
		/// </summary>
		private GameObject CreatePlaceholderProjectile(Vector3 pos, Quaternion rot, bool useGravity)
		{
			GameObject proj;
			proj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
			proj.SetActive(false);
			proj.transform.position = pos;
			proj.transform.rotation = rot;

			proj.GetComponent<Collider>().isTrigger = true;
			proj.transform.localScale = new Vector3(.3f, .3f, .3f);

			proj.AddComponent<NSTSampleProjectile>();

			Rigidbody rb = proj.AddComponent<Rigidbody>();
			rb.useGravity = useGravity;
			rb.interpolation = RigidbodyInterpolation.Interpolate;
			/// Since these aren't real prefabs, the source will vanish on scene changes making pool growth break. 
			/// Make these placeholder prefabs scene permanment.
			DontDestroyOnLoad(proj);

			return proj;
		}

	}

#if UNITY_EDITOR

	[CustomEditor(typeof(NSTSampleWeapon))]
	[CanEditMultipleObjects]
	public class NSTSampleWeaponEditor : NSTSampleHeader
	{


	}
#endif
}


#endif