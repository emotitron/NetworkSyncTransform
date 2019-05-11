//Copyright 2018, Davin Carten, All rights reserved

#if PUN_2_OR_NEWER || MIRROR || !UNITY_2019_1_OR_NEWER

using UnityEngine;
using emotitron.Utilities.SmartVars;
using emotitron.Compression;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace emotitron.NST
{
	// TODO: Replace all usage of this with the TransfromCrusher TRSType
	//public enum RotationType { Euler = 1, Quaternion = 2 }

	[System.Serializable]
	public class RotationElement : TransformElement, IRotationElement
	{
		//public RotationType RotationType { get { return (RotationType)crusher.TRSType; } }

		// Constructor
		public RotationElement(bool local = true)
		{
			crusher = new ElementCrusher(TRSType.Quaternion, false) { local = local };
			//crusher.qcrusher.Bits = 40;
			crusher.hideFieldName = true;
		}

		// Shorthand to the rotation that accounts for local vs global rotation
		public override GenericX Localized
		{
			get
			{
				if (crusher.TRSType == TRSType.Quaternion)
					return (crusher.local) ? gameobject.transform.localRotation : gameobject.transform.rotation;
				else
					return (crusher.local) ? gameobject.transform.localEulerAngles : gameobject.transform.eulerAngles;
			}
			set
			{
				Apply(value, gameobject);
			}
		}


		public override GenericX Extrapolate(GenericX curr, GenericX prev)
		{
			if (curr.type == XType.NULL)
			{
				Debug.Log("Extrap pos element NULL !! Try to eliminate these Davin");
				return Localized;
			}
			if (crusher.TRSType == TRSType.Quaternion)
			{
				return new GenericX(
					(extrapolation == 0) ? (Quaternion)curr : QuaternionUtils.ExtrapolateQuaternion(prev, curr, 1 + extrapolation),	curr.type);
			}
			else
			{
				if (extrapolation == 0)
					return curr;

				Quaternion extrapolated = QuaternionUtils.ExtrapolateQuaternion(prev, curr, 1 + extrapolation);

				// Test for the rare nasty (NaN, Nan, Nan, NaN) occurance... and deal with it
				if (float.IsNaN(extrapolated[0]))
					return curr;

				return new GenericX(extrapolated.eulerAngles, curr.type);
			}
		}

	}

#if UNITY_EDITOR

	[CustomPropertyDrawer(typeof(RotationElement))]
	[CanEditMultipleObjects]

	public class RotationElementDrawer : TransformElementDrawer
	{
		
	}

#endif

}


#endif