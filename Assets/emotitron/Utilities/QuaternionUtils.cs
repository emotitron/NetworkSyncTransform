using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QuaternionUtils {

	//TODO: This method is suspect and not fully tested
	public static Quaternion ExtrapolateQuaternion(Quaternion a, Quaternion b, float t)
	{
		//Debug.Log(Time.time +" Extrapolate Quat missing ");
		Quaternion rot = b * Quaternion.Inverse(a);

		float ang = 0.0f;

		Vector3 axis = Vector3.zero;

		rot.ToAngleAxis(out ang, out axis);

		if (ang > 180)
			ang -= 360;

		ang = ang * t % 360;

		return Quaternion.AngleAxis(ang, axis) * a;
	}
}
