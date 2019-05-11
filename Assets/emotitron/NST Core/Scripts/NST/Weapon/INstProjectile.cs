//Copyright 2018, Davin Carten, All rights reserved

#if PUN_2_OR_NEWER || MIRROR || !UNITY_2019_1_OR_NEWER

namespace emotitron.NST.Weapon
{
	/// <summary>
	/// Interface use by weapons to pass information to projectiles
	/// </summary>
	public interface INstProjectile
	{
		NetworkSyncTransform OwnerNst { set; }

	}
}

#endif


