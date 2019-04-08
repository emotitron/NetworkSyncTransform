//Copyright 2018, Davin Carten, All rights reserved

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
	
