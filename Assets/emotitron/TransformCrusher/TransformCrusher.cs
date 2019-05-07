//Copyright 2018, Davin Carten, All rights reserved

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace emotitron.Compression
{
	/// <summary>
	/// VERY basic interface, just to make it easy to find transform crusher in another example component.
	/// </summary>
	public interface IHasTransformCrusher
	{
		TransformCrusher TC { get; }
	}

	[System.Serializable]
	public class TransformCrusher : Crusher<TransformCrusher>, /*IOnElementCrusherChange,*/ ICrusherCopy<TransformCrusher>
	{
		public const int VersionMajor = 3;
		public const int VersionMinor = 5;
		public const int VersionRevision = 3;
		public const int Build = 3503;

		#region Static Crushers

		public static Dictionary<int, TransformCrusher> staticTransformCrushers = new Dictionary<int, TransformCrusher>();

		/// <summary>
		/// See if a crusher with these exact settings exists in the static crushers list. If so, return that already
		/// cataloged crusher. You may allow the crusher given as an argument be garbage collected. NOTE: Any changes to static crushers
		/// will break things. Currently there are no safeguards against this.
		/// </summary>
		/// <param name="tc"></param>
		/// <returns></returns>
		public static TransformCrusher CheckAgainstStatics(TransformCrusher tc, bool CheckElementCrusherAsWell = true)
		{
			if (ReferenceEquals(tc, null))
				return null;

			if (CheckElementCrusherAsWell)
			{
				tc.posCrusher = ElementCrusher.CheckAgainstStatics(tc.posCrusher);
				tc.rotCrusher = ElementCrusher.CheckAgainstStatics(tc.rotCrusher);
				tc.sclCrusher = ElementCrusher.CheckAgainstStatics(tc.sclCrusher);
			}

			int hash = tc.GetHashCode();
			if (staticTransformCrushers.ContainsKey(hash))
			{
				return staticTransformCrushers[hash];
			}

			staticTransformCrushers.Add(hash, tc);
			return tc;
		}

		#endregion

		[HideInInspector]
		[System.Obsolete("Default Transform breaks crusher sharing across multiple instances and is now deprecated.")]
		[Tooltip("This is the default assumed transform when no transform or gameobject is given to methods.")]
		public Transform defaultTransform;

		// Set up the default Crushers so they add up to 64 bits
		[SerializeField] protected ElementCrusher posCrusher;
		[SerializeField] protected ElementCrusher rotCrusher;
		[SerializeField] protected ElementCrusher sclCrusher;


		/// <summary>
		/// Sets the position crusher to the assigned reference, and reruns CacheValues().
		/// </summary>
		public ElementCrusher PosCrusher
		{
			get { return posCrusher; }
			set
			{
				if (ReferenceEquals(posCrusher, value))
					return;

				if (posCrusher != null)
					posCrusher.OnRecalculated -= OnCrusherChange;

				posCrusher = value;

				if (posCrusher != null)
					posCrusher.OnRecalculated += OnCrusherChange;

				CacheValues();
			}
		}
		/// <summary>
		/// Sets the scale crusher to the assigned reference, and reruns CacheValues().
		/// </summary>
		public ElementCrusher RotCrusher
		{
			get { return rotCrusher; }
			set
			{
				if (ReferenceEquals(rotCrusher, value))
					return;

				if (rotCrusher != null)
					rotCrusher.OnRecalculated -= OnCrusherChange;

				rotCrusher = value;

				if (rotCrusher != null)
					rotCrusher.OnRecalculated += OnCrusherChange;

				CacheValues();
			}
		}
		/// <summary>
		/// Sets the scale crusher to the assigned reference, and reruns CacheValues().
		/// </summary>
		public ElementCrusher SclCrusher
		{
			get { return sclCrusher; }
			set
			{
				if (ReferenceEquals(sclCrusher, value))
					return;

				if (sclCrusher != null)
					sclCrusher.OnRecalculated -= OnCrusherChange;

				sclCrusher = value;

				if (sclCrusher != null)
					sclCrusher.OnRecalculated += OnCrusherChange;

				CacheValues();
			}
		}

		/// <summary>
		/// Callback fired whenever a component ElementCrusher is changed.
		/// </summary>
		/// <param name="crusher"></param>
		public void OnCrusherChange(ElementCrusher crusher)
		{
			CacheValues();
		}


		public TransformCrusher()
		{
			ConstructDefault(false);
		}
		/// <summary>
		/// Default constructor for TransformCrusher.
		/// </summary>
		/// <param name="isStatic">Set this as true if this crusher is not meant to be serialized. Static crushers are created in code, and are not meant to be modified after creation.
		/// This allows them to be indexed by their hashcodes and reused.
		/// </param>
		public TransformCrusher(bool isStatic = false)
		{
			ConstructDefault(isStatic);
		}

		protected virtual void ConstructDefault(bool isStatic = false)
		{
			if (isStatic)
			{
				// Statics initialize all crushers as null.
			}
			else
			{
				PosCrusher = new ElementCrusher(TRSType.Position, false);
				RotCrusher = new ElementCrusher(TRSType.Euler, false)
				{
					XCrusher = new FloatCrusher(BitPresets.Bits12, -90f, 90f, Axis.X, TRSType.Euler, true),
					YCrusher = new FloatCrusher(BitPresets.Bits12, -180f, 180f, Axis.Y, TRSType.Euler, true),
					ZCrusher = new FloatCrusher(BitPresets.Disabled, -180f, 180f, Axis.Z, TRSType.Euler, true)
				};
				SclCrusher = new ElementCrusher(TRSType.Scale, false)
				{
					uniformAxes = ElementCrusher.UniformAxes.XYZ,
					UCrusher = new FloatCrusher(8, 0f, 2f, Axis.Uniform, TRSType.Scale, true)
				};
			}
		}

		public override void OnBeforeSerialize() { }

		public override void OnAfterDeserialize()
		{
			CacheValues();
		}


#if UNITY_EDITOR
#pragma warning disable 0414
		[SerializeField]
		protected bool isExpanded = true;
#pragma warning restore 0414
#endif


		/// Temporary CompressedMatrix used internally when a non-alloc is not provided and no return CM or M is required.
		//public static readonly CompressedMatrix CompressedMatrix.reusable = new CompressedMatrix();
		//public static readonly Matrix Matrix.reusable = new Matrix();


		#region Cached compression values


		[NonSerialized] protected readonly int[] cached_pBits = new int[4];
		[NonSerialized] protected readonly int[] cached_rBits = new int[4];
		[NonSerialized] protected readonly int[] cached_sBits = new int[4];
		[NonSerialized] protected readonly int[] _cached_total = new int[4];
		public ReadOnlyCollection<int> cached_total;

		protected bool cached;

		public virtual void CacheValues()
		{
			for (int i = 0; i < 4; ++i)
			{
				cached_pBits[i] = (posCrusher == null) ? 0 : posCrusher.Cached_TotalBits[i];
				cached_rBits[i] = (rotCrusher == null) ? 0 : rotCrusher.Cached_TotalBits[i];  //TallyBits((BitCullingLevel)i);
				cached_sBits[i] = (sclCrusher == null) ? 0 : sclCrusher.Cached_TotalBits[i];  //TallyBits((BitCullingLevel)i);
				_cached_total[i] = cached_pBits[i] + cached_rBits[i] + cached_sBits[i];
				cached_total = Array.AsReadOnly(_cached_total);
			}

			//TODO: cached likely no longer needed with bootstrapping and callbacks
			cached = true;
		}

		#endregion

		#region Array Writers

		public void Write(CompressedMatrix cm, byte[] buffer, ref int bitposition, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			if (!cached)
				CacheValues();

			if (cached_pBits[(int)bcl] > 0)
				posCrusher.Write(cm.cPos, buffer, ref bitposition, IncludedAxes.XYZ, bcl);
			if (cached_rBits[(int)bcl] > 0)
				rotCrusher.Write(cm.cRot, buffer, ref bitposition, IncludedAxes.XYZ, bcl);
			if (cached_sBits[(int)bcl] > 0)
				sclCrusher.Write(cm.cScl, buffer, ref bitposition, IncludedAxes.XYZ, bcl);
		}

		public void Write(CompressedMatrix cm, uint[] buffer, ref int bitposition, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			if (!cached)
				CacheValues();

			if (cached_pBits[(int)bcl] > 0)
				posCrusher.Write(cm.cPos, buffer, ref bitposition, IncludedAxes.XYZ, bcl);
			if (cached_rBits[(int)bcl] > 0)
				rotCrusher.Write(cm.cRot, buffer, ref bitposition, IncludedAxes.XYZ, bcl);
			if (cached_sBits[(int)bcl] > 0)
				sclCrusher.Write(cm.cScl, buffer, ref bitposition, IncludedAxes.XYZ, bcl);
		}

		public void Write(CompressedMatrix cm, ulong[] buffer, ref int bitposition, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			if (!cached)
				CacheValues();

			if (cached_pBits[(int)bcl] > 0)
				posCrusher.Write(cm.cPos, buffer, ref bitposition, IncludedAxes.XYZ, bcl);
			if (cached_rBits[(int)bcl] > 0)
				rotCrusher.Write(cm.cRot, buffer, ref bitposition, IncludedAxes.XYZ, bcl);
			if (cached_sBits[(int)bcl] > 0)
				sclCrusher.Write(cm.cScl, buffer, ref bitposition, IncludedAxes.XYZ, bcl);
		}

		[System.Obsolete("Default Transform is being removed, and all operations that require a transform need to explicitly supply one. Default Transform breaks the ability to share crushers across multiple objects.")]
		public Bitstream Write(byte[] buffer, ref int bitposition, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			Debug.Assert(defaultTransform, transformMissingError);

			Write(CompressedMatrix.reusable, defaultTransform, buffer, ref bitposition, bcl);
			return CompressedMatrix.reusable.ExtractBitstream();
		}

		public void Write(CompressedMatrix nonalloc, Transform transform, byte[] buffer, ref int bitposition, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			if (!cached)
				CacheValues();

			nonalloc.crusher = this;
			if (cached_pBits[(int)bcl] > 0)
				posCrusher.Write(nonalloc.cPos, transform, buffer, ref bitposition, bcl);
			if (cached_rBits[(int)bcl] > 0)
				rotCrusher.Write(nonalloc.cRot, transform, buffer, ref bitposition, bcl);
			if (cached_sBits[(int)bcl] > 0)
				sclCrusher.Write(nonalloc.cScl, transform, buffer, ref bitposition, bcl);
		}
		public void Write(Transform transform, byte[] buffer, ref int bitposition, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			Write(CompressedMatrix.reusable, transform, buffer, ref bitposition, bcl);
		}
		//[System.Obsolete("Removing the return value.")]
		//public Bitstream Write(Transform transform, byte[] buffer, ref int bitposition, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		//{
		//	Write(CompressedMatrix.reusable, transform, buffer, ref bitposition, bcl);
		//	return CompressedMatrix.reusable.ExtractBitstream();
		//}



		#endregion

		#region Array Readers

		//[System.Obsolete()]
		public Matrix ReadAndDecompress(ulong[] array, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			int bitposition = 0;
			return ReadAndDecompress(array, ref bitposition, bcl);
		}

		public Matrix ReadAndDecompress(uint[] array, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			int bitposition = 0;
			return ReadAndDecompress(array, ref bitposition, bcl);
		}

		public Matrix ReadAndDecompress(byte[] array, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			int bitposition = 0;
			return ReadAndDecompress(array, ref bitposition, bcl);
		}

		// Skips intermediate step of creating a compressedMatrx
		public void ReadAndDecompress(Matrix nonalloc, ulong[] array, ref int bitposition, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			Read(CompressedMatrix.reusable, array, ref bitposition, bcl);
			Decompress(nonalloc, CompressedMatrix.reusable);
		}
		public void ReadAndDecompress(Matrix nonalloc, uint[] array, ref int bitposition, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			Read(CompressedMatrix.reusable, array, ref bitposition, bcl);
			Decompress(nonalloc, CompressedMatrix.reusable);
		}
		public void ReadAndDecompress(Matrix nonalloc, byte[] array, ref int bitposition, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			Read(CompressedMatrix.reusable, array, ref bitposition, bcl);
			Decompress(nonalloc, CompressedMatrix.reusable);
		}

		public Matrix ReadAndDecompress(ulong[] array, ref int bitposition, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			ReadAndDecompress(Matrix.reusable, array, ref bitposition, bcl);
			return Matrix.reusable;
		}

		public Matrix ReadAndDecompress(uint[] array, ref int bitposition, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			ReadAndDecompress(Matrix.reusable, array, ref bitposition, bcl);
			return Matrix.reusable;
		}

		public Matrix ReadAndDecompress(byte[] array, ref int bitposition, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			ReadAndDecompress(Matrix.reusable, array, ref bitposition, bcl);
			return Matrix.reusable;
		}

		// UNTESTED
		public void Read(CompressedMatrix nonalloc, byte[] array, ref int bitposition, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			if (!cached)
				CacheValues();

			nonalloc.crusher = this;
			if (cached_pBits[(int)bcl] > 0)
				posCrusher.Read(nonalloc.cPos, array, ref bitposition, IncludedAxes.XYZ, bcl);
			if (cached_rBits[(int)bcl] > 0)
				rotCrusher.Read(nonalloc.cRot, array, ref bitposition, IncludedAxes.XYZ, bcl);
			if (cached_sBits[(int)bcl] > 0)
				sclCrusher.Read(nonalloc.cScl, array, ref bitposition, IncludedAxes.XYZ, bcl);
		}
		// UNTESTED
		//[System.Obsolete("Bitstream is slated to be removed. Use the Array and Primitive serializers instead.")]
		public CompressedMatrix Read(ulong[] array, ref int bitposition, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			Read(CompressedMatrix.reusable, array, ref bitposition, bcl);
			return CompressedMatrix.reusable;
		}
		public CompressedMatrix Read(uint[] array, ref int bitposition, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			Read(CompressedMatrix.reusable, array, ref bitposition, bcl);
			return CompressedMatrix.reusable;
		}
		public CompressedMatrix Read(byte[] array, ref int bitposition, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			Read(CompressedMatrix.reusable, array, ref bitposition, bcl);
			return CompressedMatrix.reusable;
		}

		public CompressedMatrix Read(ulong[] array, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			int bitposition = 0;
			Read(CompressedMatrix.reusable, array, ref bitposition, bcl);
			return CompressedMatrix.reusable;
		}
		public CompressedMatrix Read(uint[] array, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			int bitposition = 0;
			Read(CompressedMatrix.reusable, array, ref bitposition, bcl);
			return CompressedMatrix.reusable;
		}
		public CompressedMatrix Read(byte[] array, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			int bitposition = 0;
			Read(CompressedMatrix.reusable, array, ref bitposition, bcl);
			return CompressedMatrix.reusable;
		}

		// UNTESTED
		public void Read(CompressedMatrix nonalloc, ulong[] buffer, ref int bitposition, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			if (!cached)
				CacheValues();

			nonalloc.crusher = this;
			if (cached_pBits[(int)bcl] > 0)
				posCrusher.Read(nonalloc.cPos, buffer, ref bitposition, IncludedAxes.XYZ, bcl);
			if (cached_rBits[(int)bcl] > 0)
				rotCrusher.Read(nonalloc.cRot, buffer, ref bitposition, IncludedAxes.XYZ, bcl);
			if (cached_sBits[(int)bcl] > 0)
				sclCrusher.Read(nonalloc.cScl, buffer, ref bitposition, IncludedAxes.XYZ, bcl);
		}

		// UNTESTED
		public void Read(CompressedMatrix nonalloc, uint[] buffer, ref int bitposition, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			if (!cached)
				CacheValues();

			nonalloc.crusher = this;
			if (cached_pBits[(int)bcl] > 0)
				posCrusher.Read(nonalloc.cPos, buffer, ref bitposition, IncludedAxes.XYZ, bcl);
			if (cached_rBits[(int)bcl] > 0)
				rotCrusher.Read(nonalloc.cRot, buffer, ref bitposition, IncludedAxes.XYZ, bcl);
			if (cached_sBits[(int)bcl] > 0)
				sclCrusher.Read(nonalloc.cScl, buffer, ref bitposition, IncludedAxes.XYZ, bcl);
		}


		#endregion

		#region ULong Buffer Writers

		/// <summary>
		/// Compress a transform using this crusher, store the compressed results in a supplied CompressedMatrix, and serialize the compressed values to the buffer.
		/// </summary>
		/// <param name="nonalloc">Populate this CompressedMatrix with the results of the cmopression operation.</param>
		/// <param name="transform">The transform to compress.</param>
		/// <param name="buffer">The write target.</param>
		/// <param name="bitposition">The write position for the buffer.</param>
		/// <param name="bcl"></param>
		public void Write(CompressedMatrix nonalloc, Transform transform, ref ulong buffer, ref int bitposition, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			if (!cached)
				CacheValues();

			nonalloc.crusher = this;
			if (cached_pBits[(int)bcl] > 0)
				posCrusher.Write(nonalloc.cPos, transform, ref buffer, ref bitposition, bcl);
			if (cached_rBits[(int)bcl] > 0)
				rotCrusher.Write(nonalloc.cRot, transform, ref buffer, ref bitposition, bcl);
			if (cached_sBits[(int)bcl] > 0)
				sclCrusher.Write(nonalloc.cScl, transform, ref buffer, ref bitposition, bcl);
		}

		/// <summary>
		/// Compress and write all of the components of transform, without creating any intermediary CompressedMatrix or Bitstream. This is the most efficient way to
		/// compress and write a transform, but it will not return any compresed values for you to store or compare.
		/// </summary>
		/// <param name="transform">The transform to compress.</param>
		/// <param name="buffer">The write target.</param>
		/// <param name="bitposition">The write position for the buffer.</param>
		/// <param name="bcl"></param>
		public void Write(Transform transform, ref ulong buffer, ref int bitposition, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			if (!cached)
				CacheValues();

			if (cached_pBits[(int)bcl] > 0)
				posCrusher.Write(transform, ref buffer, ref bitposition, bcl);
			if (cached_rBits[(int)bcl] > 0)
				rotCrusher.Write(transform, ref buffer, ref bitposition, bcl);
			if (cached_sBits[(int)bcl] > 0)
				sclCrusher.Write(transform, ref buffer, ref bitposition, bcl);
		}

		/// <summary>
		/// Serialize a CompressedMatrix to a bitstream.
		/// </summary>
		/// <param name="cm">Results of a previously compressed Transform Matrix.</param>
		/// <param name="bitstream">The write target.</param>
		/// <param name="bcl"></param>
		[System.Obsolete("Bitstream is slated to be removed. Use the Array and Primitive serializers instead.")]
		public void Write(CompressedMatrix cm, ref Bitstream bitstream, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			if (cached_pBits[(int)bcl] > 0)
				posCrusher.Write(cm.cPos, ref bitstream, bcl);
			if (cached_rBits[(int)bcl] > 0)
				rotCrusher.Write(cm.cRot, ref bitstream, bcl);
			if (cached_sBits[(int)bcl] > 0)
				sclCrusher.Write(cm.cScl, ref bitstream, bcl);
		}

		/// <summary>
		/// Serialize a CompressedMatrix to a bitstream.
		/// </summary>
		/// <param name="cm">Results of a previously compressed Transform Matrix.</param>
		/// <param name="buffer">The write target.</param>
		/// <param name="bcl"></param>
		public void Write(CompressedMatrix cm, ref ulong buffer, ref int bitposition, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			if (cached_pBits[(int)bcl] > 0)
				posCrusher.Write(cm.cPos, ref buffer, ref bitposition, bcl);
			if (cached_rBits[(int)bcl] > 0)
				rotCrusher.Write(cm.cRot, ref buffer, ref bitposition, bcl);
			if (cached_sBits[(int)bcl] > 0)
				sclCrusher.Write(cm.cScl, ref buffer, ref bitposition, bcl);
		}

		/// <summary>
		/// Compress a transform using this crusher, store the compressed results in a supplied CompressedMatrix, and serialize the compressed values to the bitstream.
		/// </summary>
		/// <param name="nonalloc">Populate this CompressedMatrix with the results of the cmopression operation.</param>
		/// <param name="transform">The transform to compress.</param>
		/// <param name="bitstream">The write target.</param>
		/// <param name="bcl"></param>
		public void Write(CompressedMatrix nonalloc, Transform transform, ref ulong bitstream, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			if (!cached)
				CacheValues();

			Compress(nonalloc, transform);

			if (cached_pBits[(int)bcl] > 0)
				posCrusher.Write(nonalloc.cPos, ref bitstream, bcl);
			if (cached_rBits[(int)bcl] > 0)
				rotCrusher.Write(nonalloc.cRot, ref bitstream, bcl);
			if (cached_sBits[(int)bcl] > 0)
				sclCrusher.Write(nonalloc.cScl, ref bitstream, bcl);
		}

		/// <summary>
		/// Compress a transform using this crusher, store the compressed results in a supplied CompressedMatrix, and serialize the compressed values to the bitstream.
		/// </summary>
		/// <param name="nonalloc">Populate this CompressedMatrix with the results of the cmopression operation.</param>
		/// <param name="transform">The transform to compress.</param>
		/// <param name="bitstream">The write target.</param>
		/// <param name="bcl"></param>
		[System.Obsolete("Bitstream is slated to be removed. Use the Array and Primitive serializers instead.")]
		public void Write(CompressedMatrix nonalloc, Transform transform, ref Bitstream bitstream, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			if (!cached)
				CacheValues();

			Compress(nonalloc, transform);

			if (cached_pBits[(int)bcl] > 0)
				posCrusher.Write(nonalloc.cPos, ref bitstream, bcl);
			if (cached_rBits[(int)bcl] > 0)
				rotCrusher.Write(nonalloc.cRot, ref bitstream, bcl);
			if (cached_sBits[(int)bcl] > 0)
				sclCrusher.Write(nonalloc.cScl, ref bitstream, bcl);
		}
		/// <summary>
		/// Compress a transform using this crusher, and serialize the compressed values to the bitstream.
		/// </summary>
		/// <param name="transform">The transform to compress.</param>
		/// <param name="bitstream">The write target.</param>
		/// <param name="bcl"></param>
		[System.Obsolete("Bitstream is slated to be removed. Use the Array and Primitive serializers instead.")]
		public void Write(Transform transform, ref Bitstream bitstream, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			if (!cached)
				CacheValues();

			CompressedMatrix.reusable.crusher = this;
			Compress(CompressedMatrix.reusable, transform);

			if (cached_pBits[(int)bcl] > 0)
				posCrusher.Write(CompressedMatrix.reusable.cPos, ref bitstream, bcl);
			if (cached_rBits[(int)bcl] > 0)
				rotCrusher.Write(CompressedMatrix.reusable.cRot, ref bitstream, bcl);
			if (cached_sBits[(int)bcl] > 0)
				sclCrusher.Write(CompressedMatrix.reusable.cScl, ref bitstream, bcl);
		}

		#endregion

		#region Read and Decompress

		public void ReadAndDecompress(Matrix nonalloc, ulong buffer, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			int bitposition = 0;
			ReadAndDecompress(nonalloc, buffer, ref bitposition, bcl);
		}
		[System.Obsolete("Use the nonalloc overload instead and supply a target Matrix. Matrix is now a class rather than a struct")]
		public Matrix ReadAndDecompress(ulong buffer, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			int bitposition = 0;
			return ReadAndDecompress(buffer, ref bitposition, bcl);
		}

		public void ReadAndDecompress(Matrix nonalloc, ulong buffer, ref int bitposition, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			Read(CompressedMatrix.reusable, buffer, ref bitposition, bcl);
			Decompress(nonalloc, CompressedMatrix.reusable);
		}
		[System.Obsolete("Use the nonalloc overload instead and supply a target Matrix. Matrix is now a class rather than a struct")]
		public Matrix ReadAndDecompress(ulong buffer, ref int bitposition, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			Read(CompressedMatrix.reusable, buffer, ref bitposition, bcl);
			return Decompress(CompressedMatrix.reusable);
		}

		[System.Obsolete("Bitstream is slated to be removed. Use the Array and Primitive serializers instead.")]
		public void ReadAndDecompress(Matrix nonalloc, ref Bitstream bitstream, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			Read(CompressedMatrix.reusable, ref bitstream, bcl);
			Decompress(nonalloc, CompressedMatrix.reusable);
		}
		[System.Obsolete("Use the nonalloc overload instead and supply a target Matrix. Matrix is now a class rather than a struct")]
		public Matrix ReadAndDecompress(ref Bitstream bitstream, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			Read(CompressedMatrix.reusable, ref bitstream, bcl);
			return Decompress(CompressedMatrix.reusable);
		}


		#endregion

		#region ReadAndApply

		/// <summary>
		/// Read the compressed value from a buffer, decompress it, and apply it to the target transform.
		/// </summary>
		/// <param name="target"></param>
		/// <param name="bitstream"></param>
		/// <param name="bcl"></param>
		[System.Obsolete("Bitstream is slated to be removed. Use the Array and Primitive serializers instead.")]
		public void ReadAndApply(Transform target, ref Bitstream bitstream, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			Read(CompressedMatrix.reusable, ref bitstream, bcl);
			Apply(target, CompressedMatrix.reusable);
		}

		/// <summary>
		/// Read the compressed value from a buffer, decompress it, and apply it to the target transform.
		/// </summary>
		/// <param name="target"></param>
		/// <param name="bitstream"></param>
		/// <param name="bcl"></param>
		public void ReadAndApply(Transform target, byte[] buffer, ref int bitposition, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			Read(CompressedMatrix.reusable, buffer, ref bitposition, bcl);
			Apply(target, CompressedMatrix.reusable);
		}

		#endregion

		#region Fragments Reader

		public static ulong[] reusableArray64 = new ulong[5];

		/// <summary>
		/// Reconstruct a CompressedMatrix from fragments.
		/// </summary>
		//[System.Obsolete("Bitstream is slated to be removed. Use the Array and Primitive serializers instead.")]
		public void Read(CompressedMatrix nonalloc, ulong frag0, ulong frag1 = 0, ulong frag2 = 0, ulong frag3 = 0, ulong frag4 = 0, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			if (!cached)
				CacheValues();

			int bitposition = 0;
			reusableArray64.Write(frag0, ref bitposition, 64);
			reusableArray64.Write(frag1, ref bitposition, 64);
			reusableArray64.Write(frag2, ref bitposition, 64);
			reusableArray64.Write(frag3, ref bitposition, 64);
			reusableArray64.Write(frag4, ref bitposition, 64);

			bitposition = 0;
			Read(nonalloc, reusableArray64, ref bitposition, bcl);
		}

		//[System.Obsolete("Bitstream is slated to be removed. Use the Array and Primitive serializers instead.")]
		public void ReadAndDecompress(Matrix nonalloc, ulong frag0, ulong frag1 = 0, ulong frag2 = 0, ulong frag3 = 0, ulong frag4 = 0, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			Read(CompressedMatrix.reusable, frag0, frag1, frag2, frag3, frag4, bcl);
			Decompress(nonalloc, CompressedMatrix.reusable);
		}

		public CompressedMatrix Read(ulong frag0, ulong frag1 = 0, ulong frag2 = 0, ulong frag3 = 0, uint frag4 = 0, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			Read(CompressedMatrix.reusable, frag0, frag1, frag2, frag3, frag4, bcl);
			return CompressedMatrix.reusable;
		}

		/// <summary>
		/// Read compressed data from a Bitstream, and populates the suppled CompressedMatrix with the results.
		/// </summary>
		/// <param name="bitstream">Bitstream to read from.</param>
		/// <returns></returns>
		[System.Obsolete("Bitstream is slated to be removed. Use the Array and Primitive serializers instead.")]
		public void Read(CompressedMatrix nonalloc, ref Bitstream bitstream, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			if (!cached)
				CacheValues();

			nonalloc.crusher = this;
			if (cached_pBits[(int)bcl] > 0)
				posCrusher.Read(nonalloc.cPos, ref bitstream, bcl);
			if (cached_rBits[(int)bcl] > 0)
				rotCrusher.Read(nonalloc.cRot, ref bitstream, bcl);
			if (cached_sBits[(int)bcl] > 0)
				sclCrusher.Read(nonalloc.cScl, ref bitstream, bcl);
		}

		#endregion

		/// <summary>
		/// Deserialize the bitstream into the internal reusable CompressedMatrix, and return a bitstream representing that CM's serialized data.
		/// </summary>
		/// <param name="bitstream"></param>
		/// <param name="bcl"></param>
		/// <returns></returns>
		[System.Obsolete("This probably had a use at some point... can't think of any now.")]
		public Bitstream Read(ref Bitstream bitstream, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			Read(CompressedMatrix.reusable, ref bitstream, bcl);
			return CompressedMatrix.reusable.ExtractBitstream();
		}

		#region ULong Buffer Readers

		/// <summary>
		/// Extract a CompressedMatrix from a primitive buffer. Results will overwrite the supplied CompressedMatrix non-alloc.
		/// </summary>
		/// <param name="nonalloc">Target of the Read.</param>
		/// <param name="buffer">Serialized source.</param>
		/// <param name="bcl"></param>
		public void Read(CompressedMatrix nonalloc, ulong buffer, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			int bitposition = 0;
			Read(nonalloc, buffer, ref bitposition, bcl);
		}

		//public CompressedMatrix Read(ulong buffer, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		//{
		//	int bitposition = 0;
		//	Read(CompressedMatrix.reusable, buffer, ref bitposition, bcl);
		//	return CompressedMatrix.reusable;
		//}

		/// <summary>
		/// Extract a CompressedMatrix from a primitive buffer. Results will overwrite the supplied CompressedMatrix non-alloc.
		/// </summary>
		/// <param name="nonalloc">Target of the Read.</param>
		/// <param name="buffer">Serialized source.</param>
		/// <param name="bitposition">The read start position of the buffer. This value will be incremented by the number of bits read.</param>
		/// <param name="bcl"></param>
		public void Read(CompressedMatrix nonalloc, ulong buffer, ref int bitposition, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			if (!cached)
				CacheValues();

			nonalloc.crusher = this;

			if (cached_pBits[(int)bcl] > 0)
				posCrusher.Read(nonalloc.cPos, buffer, ref bitposition, bcl);
			if (cached_rBits[(int)bcl] > 0)
				rotCrusher.Read(nonalloc.cRot, buffer, ref bitposition, bcl);
			if (cached_sBits[(int)bcl] > 0)
				sclCrusher.Read(nonalloc.cScl, buffer, ref bitposition, bcl);
		}
		//[System.Obsolete("Bitstream is slated to be removed. Use the Array and Primitive serializers instead.")]
		public CompressedMatrix Read(ulong buffer, ref int bitposition, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			Read(CompressedMatrix.reusable, buffer, ref bitposition, bcl);
			return CompressedMatrix.reusable;
		}


		#endregion

		#region Compress

		/// <summary>
		/// Compress the transform of the default gameobject. (Only avavilable if this crusher is serialized in the editor).
		/// </summary>
		/// <returns></returns>
		[System.Obsolete("Supply the transform to compress. Default Transform has be deprecated.")]
		public void Compress(CompressedMatrix nonalloc)
		{
			Debug.Assert(defaultTransform, transformMissingError);

			Compress(nonalloc, defaultTransform);
		}
		[System.Obsolete("Supply the transform to compress. Default Transform has be deprecated.")]
		public CompressedMatrix Compress()
		{
			Debug.Assert(defaultTransform, transformMissingError);

			return Compress(defaultTransform);
		}

		public void Compress(CompressedMatrix nonalloc, Matrix matrix)
		{
			if (!cached)
				CacheValues();

			nonalloc.crusher = this;
			if (cached_pBits[0] > 0) posCrusher.Compress(nonalloc.cPos, matrix.position); else nonalloc.cPos.Clear();
			if (cached_rBits[0] > 0) rotCrusher.Compress(nonalloc.cRot, matrix.rotation); else nonalloc.cRot.Clear();
			if (cached_sBits[0] > 0) sclCrusher.Compress(nonalloc.cScl, matrix.scale); else nonalloc.cScl.Clear();

		}
		[System.Obsolete("Bitstream is slated to be removed. Use the Array and Primitive serializers instead.")]
		public Bitstream Compress(Matrix matrix)
		{
			Compress(CompressedMatrix.reusable, matrix);
			return CompressedMatrix.reusable.ExtractBitstream();
		}

		public void Compress(CompressedMatrix nonalloc, Transform transform)
		{
			if (!cached)
				CacheValues();

			nonalloc.crusher = this;
			if (cached_pBits[0] > 0) posCrusher.Compress(nonalloc.cPos, transform); else nonalloc.cPos.Clear();
			if (cached_rBits[0] > 0) rotCrusher.Compress(nonalloc.cRot, transform); else nonalloc.cRot.Clear();
			if (cached_sBits[0] > 0) sclCrusher.Compress(nonalloc.cScl, transform); else nonalloc.cScl.Clear();

		}
		//[System.Obsolete("Use the nonalloc overload instead and supply a target CompressedMatrix. CompressedMatrix is now a class rather than a struct")]
		//[System.Obsolete("Bitstream is slated to be removed. Use the Array and Primitive serializers instead.")]

		/// <summary>
		/// Compressed to an internally reused CompressedMatrix. WARNING: Be sure to use the contents of this CompressedMatrix immediately, as its values will be overwritten often.
		/// If you need to hold these values, use the nonalloc overload and supply a CompressedMatrix.
		/// </summary>
		public CompressedMatrix Compress(Transform transform)
		{
			Compress(CompressedMatrix.reusable, transform);
			return CompressedMatrix.reusable;
		}

#if !DISABLE_PHYSICS

		/// <summary>
		/// Compressed to an internally reused CompressedMatrix. WARNING: Be sure to use the contents of this CompressedMatrix immediately, as its values will be overwritten often.
		/// If you need to hold these values, use the nonalloc overload and supply a CompressedMatrix.
		/// </summary>
		public CompressedMatrix Compress(Rigidbody rb)
		{
			Compress(CompressedMatrix.reusable, rb);
			return CompressedMatrix.reusable;
		}

		public void Compress(CompressedMatrix nonalloc, Rigidbody rb)
		{
			if (!cached)
				CacheValues();

			nonalloc.crusher = this;
			if (cached_pBits[0] > 0) posCrusher.Compress(nonalloc.cPos, rb.position);
			else nonalloc.cPos.Clear();

			if (rotCrusher.TRSType == TRSType.Quaternion)
			{
				if (cached_rBits[0] > 0) rotCrusher.Compress(nonalloc.cRot, rb.rotation);
				else nonalloc.cRot.Clear();
			}
			else
			{
				if (cached_rBits[0] > 0) rotCrusher.Compress(nonalloc.cRot, rb.rotation.eulerAngles);
				else nonalloc.cRot.Clear();
			}

			if (cached_sBits[0] > 0) sclCrusher.Compress(nonalloc.cScl, rb.transform);
			else nonalloc.cScl.Clear();

		}
#endif

		/// <summary>
		/// CompressAndWrite doesn't produce any temporary 40byte bitstream structs, but rather will compress and write directly to the supplied bitstream.
		/// Use this rather than Write() and Compress() when you don't need the lossy or compressed value returned.
		/// </summary>
		[System.Obsolete("Supply the transform to compress. Default Transform has be deprecated.")]
		public void CompressAndWrite(ref Bitstream bitstream)
		{
			if (!cached)
				CacheValues();

			Debug.Assert(defaultTransform, transformMissingError);

			if (cached_pBits[0] > 0)
				posCrusher.CompressAndWrite(defaultTransform, ref bitstream);
			if (cached_rBits[0] > 0)
				rotCrusher.CompressAndWrite(defaultTransform, ref bitstream);
			if (cached_sBits[0] > 0)
				sclCrusher.CompressAndWrite(defaultTransform, ref bitstream);
		}

		/// <summary>
		/// CompressAndWrite doesn't produce any temporary 40byte bitstream structs, but rather will compress and write directly to the supplied bitstream.
		/// Use this rather than Write() and Compress() when you don't need the lossy or compressed value returned.
		/// </summary>
		/// <param name="matrix"></param>
		/// <param name="bitstream"></param>
		[System.Obsolete("Bitstream is slated to be removed. Use the Array and Primitive serializers instead.")]
		public void CompressAndWrite(Matrix matrix, ref Bitstream bitstream)
		{
			if (!cached)
				CacheValues();

			if (cached_pBits[0] > 0)
				posCrusher.CompressAndWrite(matrix.position, ref bitstream);
			if (cached_rBits[0] > 0)
				rotCrusher.CompressAndWrite(matrix.rotation, ref bitstream);
			if (cached_sBits[0] > 0)
				sclCrusher.CompressAndWrite(matrix.scale, ref bitstream);
		}
		/// <summary>
		/// CompressAndWrite doesn't produce any temporary 40byte bitstream structs, but rather will compress and write directly to the supplied bitstream.
		/// Use this rather than Write() and Compress() when you don't need the lossy or compressed value returned.
		/// </summary>
		/// <param name="matrix"></param>
		/// <param name="bitstream"></param>
		public void CompressAndWrite(Matrix matrix, byte[] buffer, ref int bitposition)
		{
			if (!cached)
				CacheValues();

			if (cached_pBits[0] > 0)
				posCrusher.CompressAndWrite(matrix.position, buffer, ref bitposition);
			if (cached_rBits[0] > 0)
				rotCrusher.CompressAndWrite(matrix.rotation, buffer, ref bitposition);
			if (cached_sBits[0] > 0)
				sclCrusher.CompressAndWrite(matrix.scale, buffer, ref bitposition);
		}

		/// <summary>
		/// CompressAndWrite doesn't produce any temporary 40byte bitstream structs, but rather will compress and write directly to the supplied bitstream.
		/// Use this rather than Write() and Compress() when you don't need the lossy or compressed value returned.
		/// </summary>
		[System.Obsolete("Bitstream is slated to be removed. Use the Array and Primitive serializers instead.")]
		public void CompressAndWrite(Transform transform, ref Bitstream bitstream)
		{
			if (!cached)
				CacheValues();

			if (cached_pBits[0] > 0)
				posCrusher.CompressAndWrite(transform, ref bitstream);
			if (cached_rBits[0] > 0)
				rotCrusher.CompressAndWrite(transform, ref bitstream);
			if (cached_sBits[0] > 0)
				sclCrusher.CompressAndWrite(transform, ref bitstream);
		}

		/// <summary>
		/// CompressAndWrite doesn't produce any temporary 40byte bitstream structs, but rather will compress and write directly to the supplied bitstream.
		/// Use this rather than Write() and Compress() when you don't need the lossy or compressed value returned.
		/// </summary>
		public void CompressAndWrite(Transform transform, byte[] buffer, ref int bitposition)
		{
			if (!cached)
				CacheValues();

			if (cached_pBits[0] > 0)
				posCrusher.CompressAndWrite(transform, buffer, ref bitposition);
			if (cached_rBits[0] > 0)
				rotCrusher.CompressAndWrite(transform, buffer, ref bitposition);
			if (cached_sBits[0] > 0)
				sclCrusher.CompressAndWrite(transform, buffer, ref bitposition);
		}

#if !DISABLE_PHYSICS

		/// <summary>
		/// CompressAndWrite doesn't produce any temporary 40byte bitstream structs, but rather will compress and write directly to the supplied bitstream.
		/// Use this rather than Write() and Compress() when you don't need the lossy or compressed value returned.
		/// </summary>
		public void CompressAndWrite(Rigidbody rb, byte[] buffer, ref int bitposition)
		{
			if (!cached)
				CacheValues();

			if (cached_pBits[0] > 0)
				posCrusher.CompressAndWrite(rb.position, buffer, ref bitposition);
			if (cached_rBits[0] > 0)
			{
				if (rotCrusher.TRSType == TRSType.Quaternion)
					rotCrusher.CompressAndWrite(rb.rotation, buffer, ref bitposition);
				else
					rotCrusher.CompressAndWrite(rb.rotation.eulerAngles, buffer, ref bitposition);
			}
			if (cached_sBits[0] > 0)
				sclCrusher.CompressAndWrite(rb.transform, buffer, ref bitposition);
		}
#endif

		#endregion

		#region Decompress

		public void Decompress(Matrix nonalloc, ulong[] buffer, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			int bitposition = 0;
			Read(CompressedMatrix.reusable, buffer, ref bitposition, bcl);
			Decompress(nonalloc, CompressedMatrix.reusable);
		}

		public void Decompress(Matrix nonalloc, uint[] buffer, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			int bitposition = 0;
			Read(CompressedMatrix.reusable, buffer, ref bitposition, bcl);
			Decompress(nonalloc, CompressedMatrix.reusable);
		}

		public void Decompress(Matrix nonalloc, ulong compressed, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			Read(CompressedMatrix.reusable, compressed, bcl);
			Decompress(nonalloc, CompressedMatrix.reusable);
		}

		public Matrix Decompress(ulong[] buffer, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			int bitposition = 0;
			Read(CompressedMatrix.reusable, buffer, ref bitposition, bcl);
			Decompress(Matrix.reusable, CompressedMatrix.reusable);
			return Matrix.reusable;
		}

		public Matrix Decompress(uint[] buffer, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			int bitposition = 0;
			Read(CompressedMatrix.reusable, buffer, ref bitposition, bcl);
			Decompress(Matrix.reusable, CompressedMatrix.reusable);
			return Matrix.reusable;
		}

		public Matrix Decompress(byte[] buffer, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			int bitposition = 0;
			Read(CompressedMatrix.reusable, buffer, ref bitposition, bcl);
			Decompress(Matrix.reusable, CompressedMatrix.reusable);
			return Matrix.reusable;
		}

		public Matrix Decompress(ulong compressed, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			Read(CompressedMatrix.reusable, compressed, bcl);
			Decompress(Matrix.reusable, CompressedMatrix.reusable);
			return Matrix.reusable;
		}

		public void Decompress(Matrix nonalloc, CompressedMatrix compMatrix)
		{
			if (!cached)
				CacheValues();

			nonalloc.Set(
				this,
				(cached_pBits[0] > 0) ? (Vector3)posCrusher.Decompress(compMatrix.cPos) : new Vector3(),
				(cached_rBits[0] > 0) ? rotCrusher.Decompress(compMatrix.cRot) : new Element(),
				(cached_sBits[0] > 0) ? (Vector3)sclCrusher.Decompress(compMatrix.cScl) : new Vector3()
				);
		}
		[System.Obsolete("Use the nonalloc overload instead and supply a target Matrix. Matrix is now a class rather than a struct")]
		public Matrix Decompress(CompressedMatrix compMatrix)
		{
			if (!cached)
				CacheValues();

			return new Matrix(
				this,
				(cached_pBits[0] > 0) ? (Vector3)posCrusher.Decompress(compMatrix.cPos) : new Vector3(),
				(cached_rBits[0] > 0) ? rotCrusher.Decompress(compMatrix.cRot) : new Element(),
				(cached_sBits[0] > 0) ? (Vector3)sclCrusher.Decompress(compMatrix.cScl) : new Vector3()
				);
		}

		#endregion


		#region Rigidbody Set/Move
#if !DISABLE_PHYSICS

		/// <summary>
		/// Set Rigidbody to values of CompressedMatrix using rb.position and rb.rotation. 
		/// <para>Any axes not included in the Crusher are left as is. Scale uses rb.transform (rb doesn't handle scaling).</para>
		/// </summary>
		public void Set(Rigidbody rb, CompressedMatrix cmatrix)
		{
			if (cached_pBits[0] > 0)
				posCrusher.Set(rb, cmatrix.cPos);
			if (cached_rBits[0] > 0)
				rotCrusher.Set(rb, cmatrix.cRot);
			if (cached_sBits[0] > 0)
				sclCrusher.Apply(rb.transform, cmatrix.cScl);
		}
		/// <summary>
		/// Set Rigidbody to values of CompressedMatrix using rb.position and rb.rotation. 
		/// <para>Any axes not included in the Crusher are left as is. Scale uses rb.transform (rb doesn't handle scaling).</para>
		/// </summary>
		public void Set(Rigidbody rb, Matrix matrix)
		{
			if (cached_pBits[0] > 0)
				posCrusher.Set(rb, matrix.position);
			if (cached_rBits[0] > 0)
				rotCrusher.Set(rb, matrix.rotation);
			if (cached_sBits[0] > 0)
				sclCrusher.Apply(rb.transform, matrix.scale);
		}
		/// <summary>
		/// Set Rigidbody to values of CompressedMatrix using rb.position and rb.rotation. 
		/// <para>Any axes not included in the Crusher are left as is. Scale uses rb.transform (rb doesn't handle scaling).</para>
		/// </summary>
		public void Set(Rigidbody rb, ulong frag0, ulong frag1 = 0, ulong frag2 = 0, ulong frag3 = 0, ulong frag4 = 0)
		{
			Read(CompressedMatrix.reusable, frag0, frag1, frag2, frag3, frag4);
			Set(rb, CompressedMatrix.reusable);
		}
		/// <summary>
		/// Set Rigidbody to values of CompressedMatrix using rb.position and rb.rotation. 
		/// <para>Any axes not included in the Crusher are left as is. Scale uses rb.transform (rb doesn't handle scaling).</para>
		/// </summary>
		public void Set(Rigidbody rb, ulong[] buffer, ref int bitposition, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			Read(CompressedMatrix.reusable, buffer, ref bitposition, bcl);
			Set(rb, CompressedMatrix.reusable);
		}
		/// <summary>
		/// Set Rigidbody to values of CompressedMatrix using rb.position and rb.rotation. 
		/// <para>Any axes not included in the Crusher are left as is. Scale uses rb.transform (rb doesn't handle scaling).</para>
		/// </summary>
		public void Set(Rigidbody rb, byte[] buffer, ref int bitposition, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			Read(CompressedMatrix.reusable, buffer, ref bitposition, bcl);
			Set(rb, CompressedMatrix.reusable);
		}

		/// <summary>
		/// Move Rigidbody to values of CompressedMatrix using rb.MovePosition and rb.MoveRotation.
		/// <para>Any axes not included in the Crusher are left as is. Scale uses rb.transform (rb doesn't handle scaling).</para>
		/// </summary>
		public void Move(Rigidbody rb, CompressedMatrix cmatrix)
		{
			Move(rb, cmatrix.Decompress());
			if (cached_pBits[0] > 0)
				posCrusher.Move(rb, cmatrix.cPos);
			if (cached_rBits[0] > 0)
				rotCrusher.Move(rb, cmatrix.cRot);
			if (cached_sBits[0] > 0)
				sclCrusher.Apply(rb.transform, cmatrix.cScl);
		}
		/// <summary>
		/// Move Rigidbody to values of CompressedMatrix using rb.MovePosition and rb.MoveRotation.
		/// <para>Any axes not included in the Crusher are left as is. Scale uses rb.transform (rb doesn't handle scaling).</para>
		/// </summary>
		public void Move(Rigidbody rb, Matrix matrix)
		{
			if (cached_pBits[0] > 0)
				posCrusher.Move(rb, matrix.position);
			if (cached_rBits[0] > 0)
				rotCrusher.Move(rb, matrix.rotation);
			if (cached_sBits[0] > 0)
				sclCrusher.Apply(rb.transform, matrix.scale);
		}
		/// <summary>
		/// Move Rigidbody to values of CompressedMatrix using rb.MovePosition and rb.MoveRotation.
		/// <para>Any axes not included in the Crusher are left as is. Scale uses rb.transform (rb doesn't handle scaling).</para>
		/// </summary>
		public void Move(Rigidbody rb, ulong frag0, ulong frag1 = 0, ulong frag2 = 0, ulong frag3 = 0, ulong frag4 = 0)
		{
			Read(CompressedMatrix.reusable, frag0, frag1, frag2, frag3, frag4);
			Move(rb, CompressedMatrix.reusable);
		}
		/// <summary>
		/// Move Rigidbody to values of CompressedMatrix using rb.MovePosition and rb.MoveRotation.
		/// <para>Any axes not included in the Crusher are left as is. Scale uses rb.transform (rb doesn't handle scaling).</para>
		/// </summary>
		public void Move(Rigidbody rb, ulong[] buffer, ref int bitposition, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			Read(CompressedMatrix.reusable, buffer, ref bitposition, bcl);
			Move(rb, CompressedMatrix.reusable);
		}
		/// <summary>
		/// Move Rigidbody to values of CompressedMatrix using rb.MovePosition and rb.MoveRotation.
		/// <para>Any axes not included in the Crusher are left as is. Scale uses rb.transform (rb doesn't handle scaling).</para>
		/// </summary>
		public void Move(Rigidbody rb, byte[] buffer, ref int bitposition, BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			Read(CompressedMatrix.reusable, buffer, ref bitposition, bcl);
			Move(rb, CompressedMatrix.reusable);
		}

		/// <summary>
		/// Apply the CompressedMatrix to a Rigidbody. Any axes not included in the Crusher are left as is.
		/// </summary>
		[System.Obsolete("Apply for Rigidbody has been replaced with Move and Set, to indicate usage of MovePosition/Rotation vs rb.position/rotation.")]
		public void Apply(Rigidbody rb, CompressedMatrix cmatrix)
		{
			if (cached_pBits[0] > 0)
				posCrusher.Apply(rb, cmatrix.cPos);
			if (cached_rBits[0] > 0)
				rotCrusher.Apply(rb, cmatrix.cRot);
			if (cached_sBits[0] > 0)
				sclCrusher.Apply(rb.transform, cmatrix.cScl);
		}

		/// <summary>
		/// Apply the TRS matrix to a transform. Any axes not included in the Crusher are left as is.
		/// </summary>
		[System.Obsolete("Apply for Rigidbody has been replaced with Move and Set, to indicate usage of MovePosition/Rotation vs rb.position/rotation.")]
		public void Apply(Rigidbody rb, Matrix matrix)
		{
			if (cached_pBits[0] > 0)
				posCrusher.Apply(rb, matrix.position);
			if (cached_rBits[0] > 0)
				rotCrusher.Apply(rb, matrix.rotation);
			if (cached_sBits[0] > 0)
				sclCrusher.Apply(rb.transform, matrix.scale);
		}
#endif
		#endregion

		#region Apply

		const string transformMissingError = "The 'defaultTransform' is null and has not be set in the inspector. " +
				"For non-editor usages of TransformCrusher you need to pass the target transform to this method.";

		[System.Obsolete("Supply the transform to compress. Default Transform has be deprecated.")]
		public void Apply(ulong cvalue)
		{
			Debug.Assert(defaultTransform, transformMissingError);

			Apply(defaultTransform, cvalue);
		}

		public void Apply(Transform t, ulong cvalue)
		{
			Decompress(Matrix.reusable, cvalue);
			Apply(t, Matrix.reusable);
		}

		[System.Obsolete("Supply the transform to compress. Default Transform has be deprecated.")]
		public void Apply(ulong u0, ulong u1, ulong u2, ulong u3, uint u4)
		{
			Debug.Assert(defaultTransform, transformMissingError);

			Apply(defaultTransform, u0, u1, u2, u3, u4);
		}

		public void Apply(Transform t, ulong frag0, ulong frag1 = 0, ulong frag2 = 0, ulong frag3 = 0, ulong frag4 = 0)
		{
			Read(CompressedMatrix.reusable, frag0, frag1, frag2, frag3, frag4);
			Apply(t, CompressedMatrix.reusable);
		}

		/// <summary>
		/// Apply the CompressedMatrix to a transform. Any axes not included in the Crusher are left as is.
		/// </summary>
		[System.Obsolete("Supply the transform to Apply to. Default Transform has be deprecated.")]
		public void Apply(CompressedMatrix cmatrix)
		{
			Debug.Assert(defaultTransform, transformMissingError);

			Apply(defaultTransform, cmatrix);
		}

		/// <summary>
		/// Apply the CompressedMatrix to a transform. Any axes not included in the Crusher are left as is.
		/// </summary>
		public void Apply(Transform t, CompressedMatrix cmatrix)
		{
			if (cached_pBits[0] > 0)
				posCrusher.Apply(t, cmatrix.cPos);
			if (cached_rBits[0] > 0)
				rotCrusher.Apply(t, cmatrix.cRot);
			if (cached_sBits[0] > 0)
				sclCrusher.Apply(t, cmatrix.cScl);
		}


		/// <summary>
		/// Apply the TRS matrix to a transform. Any axes not included in the Crusher are left as is.
		/// </summary>
		[System.Obsolete("Supply the transform to Apply to. Default Transform has be deprecated.")]
		public void Apply(Matrix matrix)
		{
			Debug.Assert(defaultTransform, transformMissingError);

			Apply(defaultTransform, matrix);
		}

		/// <summary>
		/// Apply the TRS matrix to a transform. Any axes not included in the Crusher are left as is.
		/// </summary>
		public void Apply(Transform transform, Matrix matrix)
		{
			if (cached_pBits[0] > 0)
				posCrusher.Apply(transform, matrix.position);
			if (cached_rBits[0] > 0)
				rotCrusher.Apply(transform, matrix.rotation);
			if (cached_sBits[0] > 0)
				sclCrusher.Apply(transform, matrix.scale);
		}


		#endregion

		/// <summary>
		/// Capture the values of a Rigidbody. Applies the lossy decompressed value to the Matrix.
		/// </summary>
		/// <param name="m">Lossy decompressed value is stored.</param>
		public void Capture(Rigidbody rb, CompressedMatrix cm, Matrix m)
		{
			Compress(cm, rb);
			Decompress(m, cm);
		}

		/// <summary>
		/// Capture the values of a Rigidbody.
		/// </summary>
		/// <param name="m">Lossy decompressed value is stored.</param>
		public void Capture(Transform tr, CompressedMatrix cm, Matrix m)
		{
			// pos
			posCrusher.Compress(cm.cPos, tr.position);
			m.position = (Vector3)posCrusher.Decompress(cm.cPos);

			// rot
			rotCrusher.Compress(cm.cRot, tr);
			m.rotation = rotCrusher.Decompress(cm.cRot);

			// scl
			sclCrusher.Compress(cm.cScl, tr);
			m.scale = (Vector3)sclCrusher.Decompress(cm.cScl);

		}


		/// <summary>
		/// Get the total number of bits this Transform is set to write.
		/// </summary>
		public int TallyBits(BitCullingLevel bcl = BitCullingLevel.NoCulling)
		{
			int p = posCrusher != null ? posCrusher.TallyBits(bcl) : 0;
			int r = posCrusher != null ? rotCrusher.TallyBits(bcl) : 0;
			int s = posCrusher != null ? sclCrusher.TallyBits(bcl) : 0;
			return p + r + s;
		}

		public void CopyFrom(TransformCrusher source)
		{
			posCrusher.CopyFrom(source.posCrusher);
			rotCrusher.CopyFrom(source.rotCrusher);
			sclCrusher.CopyFrom(source.sclCrusher);

			CacheValues();
		}

		public override bool Equals(object obj)
		{
			return Equals(obj as TransformCrusher);
		}

		public bool Equals(TransformCrusher other)
		{
			return other != null &&
				//EqualityComparer<Transform>.Default.Equals(defaultTransform, other.defaultTransform) &&
				(posCrusher == null ? other.posCrusher == null : posCrusher.Equals(other.posCrusher)) &&
				(rotCrusher == null ? other.rotCrusher == null : rotCrusher.Equals(other.rotCrusher)) &&
				(sclCrusher == null ? other.sclCrusher == null : sclCrusher.Equals(other.sclCrusher));
		}

		public override int GetHashCode()
		{
			var hashCode = -453726296;
			//hashCode = hashCode * -1521134295 + EqualityComparer<Transform>.Default.GetHashCode(defaultTransform);
			hashCode = hashCode * -1521134295 + ((posCrusher == null) ? 0 : posCrusher.GetHashCode());
			hashCode = hashCode * -1521134295 + ((rotCrusher == null) ? 0 : rotCrusher.GetHashCode());
			hashCode = hashCode * -1521134295 + ((sclCrusher == null) ? 0 : sclCrusher.GetHashCode());
			return hashCode;
		}


		public static bool operator ==(TransformCrusher crusher1, TransformCrusher crusher2)
		{
			return EqualityComparer<TransformCrusher>.Default.Equals(crusher1, crusher2);
		}

		public static bool operator !=(TransformCrusher crusher1, TransformCrusher crusher2)
		{
			return !(crusher1 == crusher2);
		}
	}

}
