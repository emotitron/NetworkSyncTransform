//Copyright 2018, Davin Carten, All rights reserved

#if PUN_2_OR_NEWER || MIRROR || !UNITY_2019_1_OR_NEWER

using UnityEngine;
using emotitron.NST.HealthSystem;
using emotitron.NST.Weapon;
using emotitron.Debugging;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace emotitron.NST.Sample
{
	/// <summary>
	/// Sample code for a projectile that uses the INstProjectile interface and transmits damage to any classes with the IVitals interface (NSTSampleHealth for example).
	/// For actual use, be sure to assign your projectiles to a Physics Layer that ignores collisions with itself. For this demo I cannot save the PhysicsManager
	/// settings or create layers, so projectiles will collider with one another.
	/// </summary>
	[AddComponentMenu("NST/Sample Code/NST Sample Projectile")]
	public class NSTSampleProjectile : MonoBehaviour, INstProjectile
	{
		public float damage = 20f;
		public GameObject terminationPrefab;
		private Collider projCollider;

		// INstProjectile interface items
		private NetworkSyncTransform ownerNst;
		public NetworkSyncTransform OwnerNst { set { ownerNst = value; } }

		//// Use this for initialization
		void Awake()
		{
			if (projCollider == null)
				projCollider = transform.root.GetComponentInChildren<Collider>();

			// Disable this if there is no collider, and warn the developer.
			if (projCollider == null)
			{
				XDebug.LogError(!XDebug.logErrors ? null : 
					(name + " is a projectile, but it has no collider - be sure to add one (or collisions can't be detected) or remove this " + this.GetType() + " component."));

				enabled = false;
			}
		}

		// If set as a bouncy collider, register hit but don't terminate
		private void OnCollisionEnter(Collision collision)
		{
			// If this is not a trigger, bounce of anything except enemies.
			if (Hit(collision.collider) == HitType.Enemy)
				Terminate();
		}

		// If set as trigger collider, register hit and terminate. 
		private void OnTriggerEnter(Collider hitCollider)
		{
			// This is a trigger type. Terminate when hitting anything but self.
			if (Hit(hitCollider) != HitType.Self)
				Terminate();
		}

		enum HitType { Self, Friendly, Enemy, Other }
		/// <summary>
		/// Return if this collided with a health object. Checks to see if the object this collided with has a health interface.
		/// If so, calls the ApplyDamage method on it.
		/// </summary>
		private HitType Hit(Collider hitCollider)
		{
			// Ignore self-collision
			if (hitCollider.transform.root.gameObject == ownerNst.gameObject)
			{
				return HitType.Self;
			}

			IVitals health = hitCollider.transform.root.GetComponent<IVitals>();
			NSTHitGroupAssign hgs = hitCollider.GetComponent<NSTHitGroupAssign>();

			int hitGroupId = (hgs != null) ? hgs.hitGroupSelector.hitGroupTagId : 0;

			if (health != null)
			{
				// Register the hit if this is the server
				if (health.NA.IAmActingAuthority) // MasterNetAdapter.ServerIsActive)
					if (health != null)
					{
						health.ApplyDamage(damage, 1 << hitGroupId);
						return HitType.Enemy;
					}
			}
			return HitType.Other;
		}

		public void Terminate()
		{
			// This should be replaced by a pool item. Be sure the termination GO also self terminates
			if (terminationPrefab != null)
				Instantiate(terminationPrefab);

			// Disabling triggers the Pool component to despawn this.
			gameObject.SetActive(false);
		}
	}


#if UNITY_EDITOR

	[CustomEditor(typeof(NSTSampleProjectile))]
	[CanEditMultipleObjects]
	public class NSTSampleProjectileEditor : NSTSampleHeader
	{


	}
#endif

}

#endif