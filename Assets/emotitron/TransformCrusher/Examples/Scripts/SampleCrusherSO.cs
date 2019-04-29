using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using emotitron.Compression;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace emotitron.Compression.Sample
{

	/// <summary>
	/// An example of TransformCrusher being used in a SciptableObject.
	/// </summary>
	[CreateAssetMenu()]
	public class SampleCrusherSO : ScriptableObject
	{
		
		/// <summary>
		/// An example of a TransformCrusher in an SO. Creating crushers in Scriptable Objects is a good alternative to instancing crushers with prefabs,
		/// as every prefab will create its own exact same crusher. 
		/// </summary>
		public TransformCrusher globalTransformCrusher = new TransformCrusher()
		{

			PosCrusher = new ElementCrusher(TRSType.Position, false)
			{
				XCrusher = new FloatCrusher(12, -5f, 5f, Axis.X, TRSType.Position, true),
				YCrusher = new FloatCrusher(10, -4f, 4f, Axis.Y, TRSType.Position, true),
				ZCrusher = new FloatCrusher(10, -4f, 4f, Axis.Z, TRSType.Position, true)

				/// Values can also be set after construction if the constructor overloads are too restrictive
				//zcrusher = new FloatCrusher(10, -4f, 4f, Axis.Z, TRSType.Position, true)
				//{
				//	Bits = 10,
				//	Min = -4,
				//	Max = 4,
				//	axis = Axis.Z,
				//	TRSType = TRSType.Position,
				//	showEnableToggle = true,
				//	AccurateCenter = false,
				//	LimitRange = false,
				//}

			},
			RotCrusher = new ElementCrusher(TRSType.Euler, false)
			{
				XCrusher = new FloatCrusher(12, -45f, 45f, Axis.X, TRSType.Euler, true),
				YCrusher = new FloatCrusher(Axis.Y, TRSType.Euler, true) { Bits = 12 },
				ZCrusher = new FloatCrusher(8, -45f, 45f, Axis.Z, TRSType.Euler, true)
			}
		};
		/// <summary>
		/// Get the singleton for this Sameple SO. Use the Singleton (uppercase) getter instead of the backing field singleton (lowercase)
		/// during startup to be certain the singleton references has been assigned.
		/// </summary>
		public static SampleCrusherSO singleton;
		/// <summary>
		/// Get the singleton for this Sample SO. Will check to see if the singleton is null first, and if so a Resources.Load() will attempt to
		/// loacate and load the SO. Use this getter during startup, in edit mode, or any time there is a chance that the SO may not have yet run OnEnable().
		/// </summary>
		public static SampleCrusherSO Singleton
		{
			get
			{
				if (singleton == null)
					singleton = Resources.Load<SampleCrusherSO>("SampleCrusherSO");

				return singleton;
			}
		}

		/// <summary>
		/// Point the static singleton to this SO, and enforce the singleton pattern.
		/// </summary>
		private void OnEnable()
		{
			if (singleton && singleton != this)
			{
				Debug.LogError("Multiple instnaces of " + this.GetType().Name + " exist in project. '" + this.GetType().Name + ".singleton' will reference the first found.");
				return;
			}

			singleton = this;
		}

		
	}

#if UNITY_EDITOR

	[CustomEditor(typeof(ScriptableObject))]
	public class SampleCrusherSOEditor : Editor
	{

	}
#endif

}
