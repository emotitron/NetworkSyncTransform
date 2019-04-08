//Copyright 2018, Davin Carten, All rights reserved

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

