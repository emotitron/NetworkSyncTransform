//Copyright 2018, Davin Carten, All rights reserved

using UnityEngine.Networking;

#pragma warning disable CS0618 // UNET is obsolete - we know.

namespace emotitron.Compression.Sample
{

	/// <summary>
	/// A VERY basic compressed sync example using UNET. Expect jitter - since we are not doing any kind of buffering or interpolation.
	/// This is an example of extracting fragments from a CompressedMatrix for use as RPC and Command arguments.
	/// </summary>
	public class Basic_Example : NetworkBehaviour
	{
		// None of this initialization stuff is needed. Simply declaring a public crusher is all you need to do.
		// These particular settings are just so this example comes up with the sizes described.
		public TransformCrusher crusher = new TransformCrusher()
		{
			RotCrusher = new ElementCrusher(TRSType.Quaternion, false)
			{
				QCrusher = new QuatCrusher(CompressLevel.uint32Med, true, false)
			}
		};

		private void Awake()
		{
			if (crusher.cached_total[0] > 72)
				UnityEngine.Debug.LogError("The demo scene has been altered, and the TransformCrusher now is compressing " + crusher.cached_total[0] + " bits rather than 72. This will result in corruption.");
		}

		/// -------------------------------------------------------------------------------------------------------------------------------------------
		/// 1. Owner sends compressed transform updates FixedUpdate tick
		/// -------------------------------------------------------------------------------------------------------------------------------------------

		private void FixedUpdate()
		{
			if (!hasAuthority)
				return;
			
			// We compress the transform, which returns a CompressedMatrix.
			var cm = crusher.Compress(transform);

			// Convert the CompressedMatrix to a ulong[] array. Note the explicit cast.
			var fragments = (ulong[])cm;

			/// Our compression settings add up to a compressed value of 72, but the largest primitive we can send with RPCs is a 64 bits
			/// so we need to break the CompressedMatrix into multiple compressed values.
			/// We send the first 64 bits as the first argument, which is in the first array element fragment[0].
			/// The second array element fragment[1] contains the remaining 8 bits and that becomes the second argument.
			/// Rather than sending the whole 64 bits of the second fragment, we should instead just cast down to (byte),
			/// since the upper/left bits of the second fragment will always be zeros, and we only need the first 8 bytes.
			/// Frag[0] = QQQQQQQQ-QQQQQQQQ-QQQQQQQQ-QQQQQQQQ-ZZZZZZZZ-ZZYYYYYY-YYYYXXXX-XXXXXXXX
			/// Frag[1] = ........-........-........-........-........-........-........-SSSSSSSS

			// Send the fragments to the server using a command rpc
			CmdServerRPC(fragments[0], (byte)fragments[1]);
		}

		/// -------------------------------------------------------------------------------------------------------------------------------------------
		/// 2. Command is used to send info from the owner to the server. 
		///    Clients call Commands, and they are executed on the Server.
		/// -------------------------------------------------------------------------------------------------------------------------------------------

		/// This server RPC only fires in UNET. PUN has no server.
		[Command(channel = Channels.DefaultUnreliable)]
		void CmdServerRPC(ulong frag0, byte frag1)
		{
			// The server mirrors out this Command to all clients using ClientRpc
			RpcClientRPC(frag0, frag1);

			// Have the Server reconstruct, decode and apply the incoming fragments.
			ApplyUpdate(frag0, frag1);
		}

		/// -------------------------------------------------------------------------------------------------------------------------------------------
		/// 3. All clients receive serialized CompressedMatrix (the compressed transform).
		/// -------------------------------------------------------------------------------------------------------------------------------------------

		[ClientRpc(channel = Channels.DefaultUnreliable)]
		void RpcClientRPC(ulong frag0, byte frag1)
		{
			if (hasAuthority && !isServer)
				return;

			// Have the client reconstruct, decode and apply the incoming fragments.
			ApplyUpdate(frag0, frag1);
		}

		/// Apply the incoming compressed transform to the local object
		void ApplyUpdate(ulong frag0, byte frag1)
		{
			// If this transform data originated from this object, exit.
			if (hasAuthority)
				return;

			// The crusher can take the fragments and directly reassemble them into a CompressedMatrix, decode that into a Matrix,
			// and apply it to a transform or rigidbody - as shown here.
			crusher.Apply(transform, frag0, frag1);
		}
	}
}
#pragma warning restore CS0618 // UNET is obsolete - we know.

