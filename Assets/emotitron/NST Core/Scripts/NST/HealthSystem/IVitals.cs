//Copyright 2018, Davin Carten, All rights reserved

#if PUN_2_OR_NEWER || MIRROR || !UNITY_2019_1_OR_NEWER

using System.Collections.Generic;

namespace emotitron.NST.HealthSystem
{
	/// <summary>
	/// NST interface for standardized health vitals (armor, shields, etc)
	/// </summary>
	public interface IVitals
	{
		/// The NST object this health interface is attached to
		NetworkSyncTransform NST { get; }
		NSTNetAdapter NA { get; }
		List<Vital> Vitals { get; }
		void ApplyDamage(float dmg, int hitgroupMask);
		void SetVital(float value, int VitalIndex, bool updateMonitors);
		void AddToVital(float value, int VitalIndex, bool updateMonitors);

		// indexer
		Vital this[int vitalType] { get; }
	}
}

#endif
	
