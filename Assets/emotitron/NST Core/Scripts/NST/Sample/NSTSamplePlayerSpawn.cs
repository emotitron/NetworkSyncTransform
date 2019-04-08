//Copyright 2018, Davin Carten, All rights reserved

using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif


namespace emotitron.NST
{


	public class NSTSamplePlayerSpawn : MonoBehaviour
	{
		[Header("Spawn Point Blocked Check")]
		[Tooltip("Select the physics layers for colliders to test against. If 'avoidCollision' is true and any colliders on these layers " +
			"are blocking the spawn point, will attempt to find the next spawn point that isn't blocked.")]
		public LayerMask layerMask;
		public float blockedCheckRadius = 2f;
		
		void OnEnable()
		{
			spawns.Add(this);
			MasterNetAdapter.UNET_RegisterStartPosition(transform);
		}

		void OnDisable()
		{
			spawns.Remove(this);
			MasterNetAdapter.UNET_UnRegisterStartPosition(transform);
		}

		public bool IsBlocked
		{
			get
			{
				int count = Physics.OverlapSphereNonAlloc(transform.position, blockedCheckRadius, reusable, layerMask);
				return (count == 0) ? false : true;
			}
		}
		// Statics

		public static List<NSTSamplePlayerSpawn> spawns = new List<NSTSamplePlayerSpawn>();
		private static int lastPicked;
		private static Collider[] reusable = new Collider[8];

		public static Transform GetRandomSpawnPoint(bool avoidCollision = true)
		{
			if (spawns.Count == 0)
				return null;

			int startindex = Random.Range(0, spawns.Count - 1);

			// Try to find a spawn that doesn't conflict
			if(avoidCollision)
				for (int i = 0; i < spawns.Count; i++)
					if (!spawns[(i + startindex) % spawns.Count].IsBlocked)
						return spawns[(i + startindex) % spawns.Count].transform;

			// No collision free spawn found
			return spawns[startindex].transform;
		}

		public static Transform GetNextSpawnPoint(bool avoidCollision = true)
		{
			if (spawns.Count == 0)
				return null;

			lastPicked = (lastPicked + 1) % spawns.Count;

			// Try to find a spawn that doesn't conflict
			if (avoidCollision)
				for (int i = 0; i < spawns.Count; i++)
				{
					int next = (i + lastPicked) % spawns.Count;
					if (!spawns[next].IsBlocked)
					{
						lastPicked = next;
						break;
					}
				}

			return spawns[lastPicked].transform;
		}


		void OnDrawGizmosSelected()
		{
			Gizmos.color = Color.yellow;
			Gizmos.DrawWireSphere(transform.position, blockedCheckRadius);
		}

	}

#if UNITY_EDITOR

	[CustomEditor(typeof(NSTSamplePlayerSpawn))]
	[CanEditMultipleObjects]
	public class NSTSamplePlayerSpawnEditor : NSTHelperEditorBase
	{
		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();

			EditorGUILayout.HelpBox("Replacement for UNET 'NetworkStartPosition' that will work with other network engines.", MessageType.None);
			//var _target = (NSTSamplePlayerSpawn)target;
			
		}
	}

#endif

}

