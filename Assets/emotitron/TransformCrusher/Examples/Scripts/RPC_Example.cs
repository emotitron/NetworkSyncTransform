//Copyright 2018, Davin Carten, All rights reserved

using UnityEngine;
using emotitron.Compression;

#if PUN_2_OR_NEWER
using Photon.Pun;
#elif MIRROR
using Mirror;
#else
using UnityEngine.Networking;
#endif

#pragma warning disable CS0618 // UNET is obsolete - we know.

namespace emotitron.Compression.Sample
{
	/// <summary>
	/// A VERY basic compressed sync example using UNET. Expect jitter - since we are not doing any kind of buffering or interpolation.
	/// This is an example of extracting fragments from a CompressedTransform for use as RPC and Command arguments.
	/// </summary>
	public class RPC_Example :
#if PUN_2_OR_NEWER
		MonoBehaviourPunCallbacks,
#else
		NetworkBehaviour,
#endif
		 IHasTransformCrusher
	{
		// None of this initialization stuff is needed. Simply declaring a public crusher is all you need to do.
		// These particular settings are just so this example comes up with the sizes described.
		public SharedTransformCrusher sharedCrusher = new SharedTransformCrusher(ShareByCommon.ComponentAndFieldName)
		{
			/// With SharedTransformCrusher, Crusher can be set once like this to allow for this kind of post construction
			/// instantiation of a custom crusher. After one call though it will be locked, and all future instances will use the
			/// same crusher as the first SharedCrusher instance. This allow for a initialization as demonstrated below.
			Crusher = new TransformCrusher()
			{
				PosCrusher = new ElementCrusher(TRSType.Position, false)
				{
					XCrusher = new FloatCrusher(12, -5f, 5f, Axis.X, TRSType.Position, true),
					YCrusher = new FloatCrusher(10, -4f, 4f, Axis.Y, TRSType.Position, true),
					ZCrusher = new FloatCrusher(10, -4f, 4f, Axis.Z, TRSType.Position, true)
				},
				RotCrusher = new ElementCrusher(TRSType.Euler, false)
				{
					XCrusher = new FloatCrusher(10, -45f, 45f, Axis.X, TRSType.Euler, true),
					YCrusher = new FloatCrusher(Axis.Y, TRSType.Euler, true) { Bits = 12 },
					ZCrusher = new FloatCrusher(10, -45f, 45f, Axis.Z, TRSType.Euler, true)
				}
			}
		};

		public TransformCrusher TC { get { return sharedCrusher; } }

		/// pre-allocated classes for storing our compressed results
		/// Be sure to preallocate and reuse CompessedMatrix and Matrix classes to avoid garbage collection.
		private readonly CompressedMatrix sentCompMatrix = new CompressedMatrix();
		private readonly CompressedMatrix compMatrix = new CompressedMatrix();
		private readonly Matrix matrix = new Matrix();

		private void Awake()
		{
			if (sharedCrusher.Crusher.cached_total[0] > 72)
				UnityEngine.Debug.LogError("The demo scene has been altered, and the TransformCrusher now is compressing " + sharedCrusher.Crusher.cached_total[0] + " bits rather than 72. This will result in corruption.");
		}

		/// -------------------------------------------------------------------------------------------------------------------------------------------
		/// 1. Owner sends compressed transform updates every x physics tick
		/// -------------------------------------------------------------------------------------------------------------------------------------------

		const int SEND_EVERY = 3;
		const int KEY_EVERY = 9;
		int fixedCount = -1;

		private void FixedUpdate()
		{
			if (!IsMine)
				return;

			fixedCount++;

			/// For this example we are sending every X fixed update to produce a lower net tick rate.
			if (((fixedCount % SEND_EVERY) != 0) && ((fixedCount % KEY_EVERY) != 0))
				return;

			// We compress the transform, which returns a CompressedMatrix.
			sharedCrusher.Crusher.Compress(compMatrix, transform);

			/// For this example, we are testing to see if the new CompressedMatrix value has changed from the last.
			/// Only send if there has been a change, or if this is a keyframe.
			if (compMatrix != sentCompMatrix || fixedCount % KEY_EVERY == 0)
			{

				/// The largest primitive we can send with most RPCs is a 64 bit ulong, so we need to break the CompressedMatrix
				/// (Which is a collection of compressed floats and possibly a compressed quaternion) into serializable primitives.
				/// Our total bits for this example is 72, so we send 64 of those bits as the first argument from our first fragment.
				/// The second fragment of the contains the remaining 8 bits and that becomes the second argument.
				/// Rather than sending the whole 64 bit second fragment, we can instead just cast down to (byte),
				/// since the upper/left bits of the second fragment will always be zeros.
				/// 00000000-00000000-00000000-XXXXXXXX

				/// Casting a CompressedMatrix to ULong lets us break the compressed object into primitives for use as Syncvars or RPC arguments.
				/// We only need to make use of the first two returned array elements here.
				/// CompressedMatrix and CompressedElements implicitly can be cast to arrays as shown below.
				ulong[] fragments = (ulong[])compMatrix;

				/// Send the RPC from the owner
				/// PUN can't serialize most unsigned types (ushort, uint, ulong) for some strange reason - so we are using ByteConverter here serialize our ulong as long
				/// We will use ByteConverter again after deserialization to convert the long back to a ulong.
#if PUN_2_OR_NEWER
				photonView.RPC("RpcClientRPC", RpcTarget.Others, (long)(ByteConverter)fragments[0], (byte)fragments[1]);
#else
				CmdServerRPC((ByteConverter)fragments[0], (byte)fragments[1]);
#endif

				/// We can Store the CompressedMatrix we just sent for comparison on next fixed update, to check for changes
				sentCompMatrix.CopyFrom(compMatrix);
			}
		}

		/// -------------------------------------------------------------------------------------------------------------------------------------------
		/// 2. (UNET Only) Command is used to send info from the owner to the server. Clients call Commands, and they are executed on the Server.
		/// -------------------------------------------------------------------------------------------------------------------------------------------

		/// This server RPC only fires in UNET. PUN has no server.
#if !PUN_2_OR_NEWER
		[Command(channel = Channels.DefaultUnreliable)]
#endif
		void CmdServerRPC(long frag0, byte frag1)
		{
			/// The server mirrors out this Command to all clients using ClientRpc
			RpcClientRPC(frag0, frag1);

			/// Have the Server reconstruct, decode and apply the incoming fragments.
			ApplyUpdate((ByteConverter)frag0, frag1);
		}

		/// -------------------------------------------------------------------------------------------------------------------------------------------
		/// 3. All clients receive serialized CompressedMatrix (the compressed transform).
		/// -------------------------------------------------------------------------------------------------------------------------------------------

#if PUN_2_OR_NEWER
		[Photon.Pun.PunRPC]
#else
		[ClientRpc(channel = Channels.DefaultUnreliable)]
#endif
		void RpcClientRPC(long frag0, byte frag1)
		{
			if (IsMine && !AsServer)
				return;

			/// Have the client reconstruct, decode and apply the incoming fragments.
			ApplyUpdate((ByteConverter)frag0, frag1);
		}

		/// Apply the incoming compressed transform to the local object
		void ApplyUpdate(ulong frag0, byte frag1)
		{
			/// If this transform data originated from this object, exit.
			if (IsMine)
				return;

			/// Reconstruct and apply the transform values

			// Read fragments to reconstruct a compressedMatrix
			sharedCrusher.Crusher.Read(compMatrix, frag0, frag1);
			// Decompress the CompressedMatrix into a Matrix. This is the uncompressed store of transform values.
			sharedCrusher.Crusher.Decompress(matrix, compMatrix);
			// Apply the Matrix values to a transform
			sharedCrusher.Crusher.Apply(transform, matrix);

			/// The crusher can take the fragments and directly reassemble them into a CompressedMatrix, decode that into a Matrix,
			/// and apply it to a transform or rigidbody - as shown here. Merging all of the steps above into one call. This however
			/// does not let you store previous Compressed and Decompressed values like the example above.
			//sharedCrusher.Crusher.Apply(transform, frag0, frag1);
		}

		#region Net Library Abstractions

		private bool IsMine
		{
			get
			{
#if PUN_2_OR_NEWER
				return photonView.IsMine;
#else
				return hasAuthority;
#endif
			}
		}

		private int NetId
		{
			get
			{
#if PUN_2_OR_NEWER
				return (photonView) ? (int)photonView.ViewID : 0;
#elif MIRROR
				return (int)netId;
#else
				return (int)netId.Value;
#endif
			}
		}

		private static bool UsingPUN
		{
			get
			{
#if PUN_2_OR_NEWER
				return true;
#else
				return false;
#endif
			}
		}

		private static bool AsServer
		{
			get
			{
#if PUN_2_OR_NEWER
				return false;
#else
				return NetworkServer.active;
#endif
			}
		}

		#endregion
	}

}
#pragma warning restore CS0618 // UNET is obsolete - we know.

