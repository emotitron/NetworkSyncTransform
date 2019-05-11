using UnityEngine;
using emotitron.Compression;

#if PUN_2_OR_NEWER
using Photon.Pun;
#elif MIRROR
using Mirror;
#else
using UnityEngine.Networking;
#endif

namespace emotitron.Utilities.Example
{

#pragma warning disable CS0618 // UNET obsolete


	/// <summary>
	/// Basic automatic transform mover for objects for network testing. Will only run if object has local authority.
	/// </summary>
	public class SimpleMover : MonoBehaviour
	{

#if PUN_2_OR_NEWER
		private PhotonView pv;
#elif !UNITY_2019_1_OR_NEWER || MIRROR
		private NetworkIdentity ni;
#endif

		private TransformCrusher tc;

		public enum TType { Position, Rotation, Scale }
		public enum Axis { X = 1, Y = 2, XY = 3, Z = 4, XZ = 5, YZ = 6, XYZ = 7 }

		public TType changeWhat = TType.Rotation;
		public Vector3 addVector = new Vector3(0, 0, 0);

		public bool local = true;
		public bool clampToCrusher = true;

		public bool oscillate;

		public Axis oscillateAxis = Axis.X;

		public float oscillateStart;
		public float oscillateEnd;
		private float oscillateRange;
		private float oscillateHalfRange;
		public float oscillateRate;

		private Rigidbody rb;

		private void Awake()
		{
#if PUN_2_OR_NEWER
			pv = transform.root.GetComponent<Photon.Pun.PhotonView>();
#elif !UNITY_2019_1_OR_NEWER || MIRROR
			ni = transform.root.GetComponent<NetworkIdentity>();
#endif
			var itc = transform.root.GetComponent<IHasTransformCrusher>();

			if (itc != null)
				tc = itc.TC;
		}

		private void Start()
		{
			rb = GetComponent<Rigidbody>();
			oscillateRange = oscillateEnd - oscillateStart;
			oscillateHalfRange = oscillateRange * .5f;
		}

		void Update()
		{
#if PUN_2_OR_NEWER
			if (pv && !pv.IsMine)
				return;
#elif !UNITY_2019_1_OR_NEWER || MIRROR
			if (ni && !ni.hasAuthority)
				return;
#endif
			if (oscillate)
			{
				float val = ((Mathf.Sin(Time.time * oscillateRate) + 1)) * oscillateHalfRange + oscillateStart;

				Vector3 currentv3 =
				(changeWhat == TType.Position) ? transform.localPosition :
				(changeWhat == TType.Rotation) ? transform.localRotation.eulerAngles :
				transform.localScale;

				Vector3 newv3 = new Vector3(
					((oscillateAxis & Axis.X) != 0) ? val : currentv3.x,
					((oscillateAxis & Axis.Y) != 0) ? val : currentv3.y,
					((oscillateAxis & Axis.Z) != 0) ? val : currentv3.z);


				if (changeWhat == TType.Rotation)
					transform.localRotation = Quaternion.Euler(newv3);

				else if (changeWhat == TType.Position)
				{
					if (transform.parent == null)
						if (rb)
							rb.MovePosition(newv3);
						else
							transform.position = newv3;
					else
						transform.localPosition = newv3;
				}

				else
				{
					transform.localScale = newv3; ;
				}
			}

			else
			{
				if (changeWhat == TType.Rotation)
					transform.localRotation *= Quaternion.Euler(addVector * Time.deltaTime);

				else if (changeWhat == TType.Position)
				{
					var newpos = transform.localPosition + Time.deltaTime *
						(local ? transform.rotation * addVector : addVector);

					if (clampToCrusher && tc != null && tc.PosCrusher != null)
						newpos = tc.PosCrusher.Clamp(newpos);
					transform.localPosition = newpos;
				}

				else
					transform.localScale += addVector * Time.deltaTime;
			}
		}
	}

}

#pragma warning restore CS0618 // UNET obsolete
