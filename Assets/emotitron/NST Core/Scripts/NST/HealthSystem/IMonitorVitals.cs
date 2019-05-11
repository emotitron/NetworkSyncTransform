//Copyright 2018, Davin Carten, All rights reserved

#if PUN_2_OR_NEWER || MIRROR || !UNITY_2019_1_OR_NEWER

namespace emotitron.NST.HealthSystem
{
	/// <summary>
	/// Callback interface for the healthsystem.
	/// </summary>
	public interface IMonitorVitals
	{
		void OnVitalsChange(IVitals vitals);
	}
}

#endif

