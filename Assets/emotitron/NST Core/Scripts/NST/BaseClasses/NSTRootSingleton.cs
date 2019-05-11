//Copyright 2018, Davin Carten, All rights reserved

#if PUN_2_OR_NEWER || MIRROR || !UNITY_2019_1_OR_NEWER

using UnityEngine;
using emotitron.Utilities.GUIUtilities;
using emotitron.Debugging;

namespace emotitron.NST
{

	[DisallowMultipleComponent]
	public abstract class NSTRootSingleton<T> : NSTComponent where T : Component
	{

		public override void OnNstPostAwake()
		{
			base.OnNstPostAwake();

			// Remove if this is not on the root
			if (transform != transform.root)
			{
				XDebug.LogError(!XDebug.logErrors ? null : 
					("Removing '" + typeof(T) + "' from child '" + name + "' of gameobject " + transform.root.name + 
					". This component should only exist on the root of a networked object with a NetworkSyncTranform component."));

				Destroy(this);
			}
		}

		public static T EnsureExistsOnRoot(Transform trans, bool isExpanded = true)
		{
			T found;

//#if !UNITY_EDITOR
			
			//// this is an unspawned NST object in the scene at start, and will be deleted.
			//if (A !MasterNetAdapter.ServerIsActive && !MasterNetAdapter.ClientIsActive)
			//{
			//	Destroy(trans.root.gameObject);
			//	return null;
			//}

			found = trans.root.GetComponent<T>();

			if (!found)
			{
				if (Application.isPlaying)
				{
					found = trans.root.gameObject.AddComponent<T>(); // (typeof(T));
				}
#if UNITY_EDITOR
				else
				{
					found = trans.root.gameObject.EnsureRootComponentExists<T>(isExpanded);
				}
#endif
			}

			return found;
		}
	}
}

#endif