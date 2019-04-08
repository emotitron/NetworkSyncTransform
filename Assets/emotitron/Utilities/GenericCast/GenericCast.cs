//Copyright 2018, Davin Carten, All rights reserved

using UnityEngine;

namespace emotitron.Utilities.GenericCast
{
	public enum CastType { Raycast, SphereCast, CapsuleCast, BoxCast, OverlapSphere, OverlapCapsule, OverlapBox }

	/// <summary>
	/// Extension of the CastType Enum for testing the type of cast/overlap being used.
	/// </summary>
	public static class CastTypeExt
	{
		public static bool IsCast(this CastType casttype)
		{
			return ((int)casttype < 4);
		}
		public static bool IsOverlap(this CastType casttype)
		{
			return ((int)casttype > 3);
		}
		public static bool UsesRadius(this CastType casttype)
		{
			return casttype == CastType.SphereCast || casttype == CastType.CapsuleCast || casttype == CastType.OverlapSphere || casttype == CastType.OverlapCapsule;
		}
		public static bool IsBox(this CastType casttype)
		{
			return (casttype == CastType.BoxCast) || (casttype == CastType.OverlapBox);
		}
		public static bool IsCapsule(this CastType casttype)
		{
			return (casttype == CastType.CapsuleCast) || (casttype == CastType.OverlapCapsule);
		}
	}

	/// <summary>
	/// Utility class that contains methods to create a unified argument string for calling all of the Raycast/Overlap cast calls. Used by the Rewind Engine.
	/// </summary>
	public static class GenericCastExt
	{

		public static int GenericCastNonAlloc(this Transform srcT, Collider[] hits, RaycastHit[] rayhits, float distance, float radius, int mask, Quaternion orientation, bool useOffset, Vector3 offset1, Vector3 offset2, CastType casttype)
		{
			int hitcount;
			Vector3 srcPos = (useOffset) ? (srcT.position + srcT.TransformDirection(offset1)) : srcT.position;
			//Vector3 srcPos = (useOffset) ? srcT.TransformDirection(offset1) : srcT.position;

			switch (casttype)
			{
				case CastType.Raycast:
					hitcount = Physics.RaycastNonAlloc(new Ray(srcPos, srcT.forward), rayhits, distance, mask);
					//Debug.DrawRay(srcPos, srcT.forward * 20, Color.blue, 5f);

					break;

				case CastType.SphereCast:
					hitcount = Physics.SphereCastNonAlloc(new Ray(srcPos, srcT.forward), radius, rayhits, distance, mask);
					break;

				case CastType.BoxCast:
					hitcount = Physics.BoxCastNonAlloc(srcPos, offset2, srcT.forward, rayhits, orientation, distance, mask);
					break;

				case CastType.CapsuleCast:
					//hitcount = Physics.CapsuleCastNonAlloc(srcPos, srcT.TransformDirection(offset2), radius, srcT.forward, rayhits, distance, mask);
					hitcount = Physics.CapsuleCastNonAlloc(srcT.TransformPoint(offset1), srcT.TransformPoint(offset2), radius, srcT.forward, rayhits, distance, mask);
					break;

				case CastType.OverlapSphere:
					hitcount = Physics.OverlapSphereNonAlloc(srcPos, radius, hits, mask);
					break;

				case CastType.OverlapBox:
					hitcount = Physics.OverlapBoxNonAlloc(srcPos, offset2, hits, orientation, mask);
					break;

				case CastType.OverlapCapsule:
					hitcount = Physics.OverlapCapsuleNonAlloc(srcT.TransformPoint(offset1), srcT.TransformPoint(offset2), radius, hits, mask);
					break;

				default:
					hitcount = 0;
					break;
			}

			// Convert the raycasthits to colliders[] if this was a cast and not an overlap
			if (casttype.IsCast())
				for (int i = 0; i < hitcount; i++)
					hits[i] = rayhits[i].collider;

			return hitcount;
		}
	}
}




