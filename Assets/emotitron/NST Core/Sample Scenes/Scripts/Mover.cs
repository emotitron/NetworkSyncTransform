
#if PUN_2_OR_NEWER || MIRROR || !UNITY_2019_1_OR_NEWER

using UnityEngine;
using emotitron.NST;
using emotitron.Utilities.GUIUtilities;

/// <summary>
/// Basic automatic transform mover for objects for network testing. Will only run if object has local authority.
/// </summary>
public class Mover : NSTComponent , /*INstStart,*/ INstPreUpdate
{
	public enum TType { Position, Rotation, Scale }
	public enum Axis { X = 1, Y = 2, Z = 4 }

	public TType changeWhat = TType.Rotation;
	public Vector3 addVector = new Vector3(0, 0, 0);
	private Vector3 initialPos;
	private Vector3 initialRot;
	private Vector3 initialScl;

	[Help("Oscillate overrides the addVector and will instead lerp between the two range values.")]
	public bool oscillate;

	[EnumMask]
	public Axis oscillateAxis = Axis.X;

	public float oscillateStart;
	public float oscillateEnd;
	private float oscillateRange;
	private float oscillateHalfRange;

	public float oscillateRate;

	[HideInInspector] private Rigidbody rb;

	public override void OnNstPostAwake()
	{
		base.OnNstPostAwake();
		Initialize();
	}

	private void Initialize()
	{
		///// Mover is only meant to exist on the owner - destroy Mover if this is not the owner.
		//if (/*MasterNetAdapter.NetworkLibrary == NetworkLibrary.UNET && */!nst.na.IsMine)
		//{
		//	Destroy(this);
		//	nst.iNstUpdate.Remove(this);
		//}

		initialPos = cachedTransform.localPosition;
		initialRot = cachedTransform.localEulerAngles;
		initialScl = cachedTransform.localScale;

		rb = GetComponent<Rigidbody>();

		oscillateRange = oscillateEnd - oscillateStart;
		oscillateHalfRange = oscillateRange * .5f;
	}
	
	public void OnNstPreUpdate()
	{
		/// This escape may not be needed with the destroy(this) in OnStart
		if (!nst.na.IsMine)
			return;

		if (oscillate)
		{
			float val = ((Mathf.Sin(Time.time * oscillateRate) + 1)) * oscillateHalfRange + oscillateStart;

			Vector3 newv3 = new Vector3
				(
				((oscillateAxis & Axis.X) != 0) ? val : 0,
				((oscillateAxis & Axis.Y) != 0) ? val : 0,
				((oscillateAxis & Axis.Z) != 0) ? val : 0);

			if (changeWhat == TType.Rotation)
			{
				cachedTransform.localRotation = Quaternion.Euler(initialRot + newv3);
			}

			else if (changeWhat == TType.Position)
			{
				if (cachedTransform.parent == null)
					if (rb)
						rb.MovePosition(initialPos + newv3);
					else
						cachedTransform.position = initialPos + newv3;
				else
					cachedTransform.localPosition = newv3;
			}

			else
			{

				Vector3 scalev3 = new Vector3 (
					((oscillateAxis & Axis.X) != 0) ? val : initialScl.x,
					((oscillateAxis & Axis.Y) != 0) ? val : initialScl.y,
					((oscillateAxis & Axis.Z) != 0) ? val : initialScl.z);

				cachedTransform.localScale = scalev3;

			}
		}

		else
		{
			if (changeWhat == TType.Rotation)
				cachedTransform.localRotation *= Quaternion.Euler(addVector * Time.deltaTime);

			else if (changeWhat == TType.Position)
				cachedTransform.localPosition += addVector * Time.deltaTime;

			else
				cachedTransform.localScale += addVector * Time.deltaTime;
		}
	}

}

#endif
