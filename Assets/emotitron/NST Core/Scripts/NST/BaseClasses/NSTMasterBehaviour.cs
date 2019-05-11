//Copyright 2018, Davin Carten, All rights reserved

#if PUN_2_OR_NEWER || MIRROR || !UNITY_2019_1_OR_NEWER

using UnityEngine;

namespace emotitron.NST
{
	/// <summary>
	/// Base class for making components that are tied to the timing of the NSTMaster rather than using a NetworkBehavior.
	/// This will be expanded greatly over time to replace NetworkBehavior and PhotonBehavior.
	/// </summary>
	public abstract class NSTMasterBehaviour : MonoBehaviour
	{
		protected virtual void OnEnable()
		{
			MasterNetAdapter.RegisterCallbackInterfaces(this);
		}

		protected virtual void OnDisable()
		{
			MasterNetAdapter.UnregisterCallbackInterfaces(this);
		}
	}
}
#endif

