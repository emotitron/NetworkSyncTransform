//Copyright 2018, Davin Carten, All rights reserved

using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using emotitron.Debugging;
using emotitron.Utilities.Networking;

#if PUN_2_OR_NEWER
using Photon.Pun;
//#elif MIRROR
//using Mirror;
#else
using UnityEngine.Networking;
#endif

#pragma warning disable CS0618 // UNET is obsolete

namespace emotitron.Compression.Sample
{
	/// <summary>
	/// A VERY basic compressed sync example using UNET. There is no interpolation, buffering or extrapoltion in this example - 
	/// this is NOT an example of good networking. This is only to demonstrate the usage of the crushers.
	/// </summary>
	public class Bitstream_Example :
#if PUN_2_OR_NEWER
		Photon.Pun.MonoBehaviourPunCallbacks,
#else
		NetworkBehaviour,
#endif
		IHasTransformCrusher
	{
		/// The lookup table for finding which net object incoming updates belong to.
		public static Dictionary<int, Bitstream_Example> players = new Dictionary<int, Bitstream_Example>();

		public const byte SND_ID = 223;

		/// Reference to the crusher we are using for the IHasTransformCrusher interface.
		/// This is used by the BasicController to get the bounds of our crusher to clamp movement.
		public TransformCrusher TC { get { return sharedCrusher; } }

		/// SharedTransformCrusher is used here instead of TransformCrusher. 
		/// The Shared wrappers (SharedTransformCrusher, SharedElementCrusher and SharedFloatCrusher do a bunch of work 
		/// behind the scenes to share a single crusher instance (which serialized objects tend not to like in Unity).
		/// The Crusher can be initialized once using the Crusher property. It will only construct once with this initializer,
		/// and all future initialization by new instances of this component will use the existing instance 
		/// (which is stored in the SharedCrusherSO scriptable object singleton).
		public SharedTransformCrusher sharedCrusher = new SharedTransformCrusher();

		/// Preallocate the classes used for storing the compressed values returned from TransformCrusher
		private CompressedMatrix compMatrix = new CompressedMatrix();
		private CompressedMatrix sentCompMatrix = new CompressedMatrix();

		/// The Float Crusher and value holder for the little circle meter. In this example it is set to private so that it won't be serialized,
		/// and does not appear in the inspector. All settings are done in code.
		private float meterval;

		/// Cached references to some of the animated parts of this player object
		private Image meterImage;
		private GameObject flasher;

		public FloatCrusher meterCrusher = new SharedFloatCrusher(ShareByCommon.ComponentAndFieldName)
		{
			Crusher = new FloatCrusher(BitPresets.Bits8, 0f, 1f)
		};

		private bool flasherIsOn;
		/// The Float Crusher and value holder for color. We are using only one float crusher, which we will reuse for all 3 color channels.
		/// In this example it is set to private so that it won't be serialized, and does not appear in the inspector. All settings are done in code.
		private Color color;
		public FloatCrusher colorCrusher = new SharedFloatCrusher()
		{
			Crusher = new FloatCrusher(BitPresets.Bits10, 0f, 1f)
		};
		/// The mesh rendered for the player. This is what is changing color.
		MeshRenderer mr;

		/// reusable bitstream.
		public static byte[] buffer = new byte[48];

		void Awake()
		{
			/// Be a good human, and cache your components.
			mr = GetComponent<MeshRenderer>();
			meterImage = GetComponentInChildren<Image>();

			if (flasher == null)
				flasher = transform.Find("Flasher").gameObject;

			/// Register our methods as Unet Msg Receivers
			NetMsgCallbacks.RegisterHandler(SND_ID, OnRcv);
		}

#if !PUN_2_OR_NEWER
		public override void OnStartServer() { NetMsgCallbacks.RegisterHandler(SND_ID, OnRcv); }
		public override void OnStartClient() { NetMsgCallbacks.RegisterHandler(SND_ID, OnRcv); }
#endif

		private void Start()
		{
			

			/// Add this component to the dictionary of netobjects. Netid is used as the key.
#if PUN_2_OR_NEWER
			players.Add((int)photonView.ViewID, this);
#else
			players.Add(NetId, this);
#endif
		}

		private void OnDestroy()
		{
			if (players.ContainsKey(NetId))
				players.Remove(NetId);
			
			//NetMsgCallbacks.UnregisterHandler(CLIENT_SND_ID, OnServerRcv);
			NetMsgCallbacks.UnregisterHandler(SND_ID, OnRcv);
		}

		/// ------------------------------------------------------------------------------------------------------
		/// 1. Owner generates some values for us to network. In this an animated color, and a milliseconds value
		/// ------------------------------------------------------------------------------------------------------

		private void Update()
		{
			if (!IsMine)
				return;

			/// Generate our changes to the values that will be networked
			color = new Color(
				Mathf.Sin(Time.time) * .5f + .5f,
				Mathf.Sin(Time.time * .9f + .3f) * .5f,
				Mathf.Sin(Time.time * .7f + .6f) * .5f + .5f);

			meterval = (Time.time * .25f) % 1;

			flasherIsOn = (meterval > .5f);

			/// Apply them locally (Since we are the owner)
			ApplyValues();
		}

		/// ------------------------------------------------------------------------------------------------------
		/// 2. Owner sends compressed transform updates to the server every physics tick
		/// ------------------------------------------------------------------------------------------------------

		private void FixedUpdate()
		{
			if (!IsMine)
				return;

			SendUpdate();
		}

		/// ------------------------------------------------------------------------------------------------------
		/// 3. The Server/client receives the packet, unpacks it, and applies the values. 
		/// ------------------------------------------------------------------------------------------------------
		private static void OnRcv(byte[] bitstream)
		{
			//Debug.Log("Svr Rcv");
			var player = ReceiveUpdate(bitstream);

			if (!AsServer)
				return;

			if (!player || player.IsMine)
				return;

			/// This instance of Bitstream_Example on the server sends its update to all clients.
			player.SendUpdate();
		}
		
		private void SendUpdate()
		{
			int writepos = 0;
			SerializeValuesToBitstream(buffer, ref writepos);

			/// Serialize and send out this bitstream.
			NetMsgSends.Send(buffer, writepos, SND_ID, ReceiveGroup.Others);
		}

		/// <summary>
		/// Write/Pack all of the values we want to network into a bitstream
		/// </summary>
		/// <param name="bitstream"></param>
		public void SerializeValuesToBitstream(byte[] bitstream, ref int writepos)
		{

			/// Write the NetId of the player this update belongs to.
			bitstream.Write((ulong)NetId, ref writepos, 32);

			/// Compress(crush) and serialize the values of the meter disk and the player color.
			meterCrusher.Write(meterval, bitstream, ref writepos);

			colorCrusher.Write(color.r, bitstream, ref writepos);
			colorCrusher.Write(color.g, bitstream, ref writepos);
			colorCrusher.Write(color.b, bitstream, ref writepos);

			/// Compress this objects current transform to the CompressedMatrix.
			sharedCrusher.Crusher.Compress(compMatrix, transform);

			/// For this example we compare the new CompressedMatrix with the previously sent one to see if anything has changed.
			/// If the compressed value has not changed, we will not send a compressed transform this tick. The == operator is overloaded
			/// for CompressedElement and CompressedMatrix, so == actually is running a.Equals(b) behind the scenes letting you compare the
			/// classes as if they were structs.
			if (compMatrix != sentCompMatrix)
			{
				/// Single true bit flag to indicate a compressed transform follows
				bitstream.WriteBool(true, ref writepos);

				/// Pass the bitstream to the TransformCrusher to serialize in the value of compMatrix.
				sharedCrusher.Crusher.Write(compMatrix, bitstream, ref writepos);

				/// Copy the values of of cm for comparison next tick.
				sentCompMatrix.CopyFrom(compMatrix);
			}
			else
			{
				/// Single false bit flag to indicate a compressed transform doesn't follow
				bitstream.WriteBool(false, ref writepos);
			}

			// as long as the bitstream isn't full, you can append other data as well onto the bitstream.
			// Bools only use one bit of traffic.
			bitstream.WriteBool(flasherIsOn, ref writepos);
		}

		/// Deserialize, Uppack, and Decompress the values
		private static Bitstream_Example ReceiveUpdate(byte[] bitstream)
		{
			int readpos = 0;
			/// Get the ID for which player this belongs to, and use the lookup table we created to get the correct player instance.
			int id = (int)bitstream.Read(ref readpos, 32);

			if (!players.ContainsKey(id))
				return null;

			var player = players[id];

			// Don't apply these values if we own this.
			if (player.IsMine)
				return player;

			/// Pass the bitstream to the player instance, which will read out the compressed values
			player.ReadValues(bitstream, ref readpos);

			/// Tell the player to apply those values.
			player.ApplyValues();

			return player;
		}

		/// Upack and Decompress the values from our bitstream.
		private void ReadValues(byte[] bitstream, ref int readpos)
		{
			/// ------------------------------------------------------------------ ///
			/// ---  Unpack all of our compressed values from the bitstream  ----- ///
			/// ------------------------------------------------------------------ ///

			meterval = meterCrusher.Read(bitstream, ref readpos).Decompress();

			//// Read our packed bits out and restore them
			color = new Color(
				colorCrusher.Read(bitstream, ref readpos).Decompress(),
				colorCrusher.Read(bitstream, ref readpos).Decompress(),
				colorCrusher.Read(bitstream, ref readpos).Decompress()
				);

			if (bitstream.ReadBool(ref readpos))
			{
				/// Read the compressed transform values in from the bitstream.
				/// We pass it our destination CompressedMatrix class to hold the compressed version of the Matrix.
				sharedCrusher.Crusher.Read(compMatrix, bitstream, ref readpos);
			}

			flasherIsOn = bitstream.ReadBool(ref readpos);

			/// ------------------------------------------------------------------ ///
			/// ----------- End reading from the bitstream ----------------------- ///
			/// ------------------------------------------------------------------ ///
		}

		/// Apply the received values to our GameObjects
		private void ApplyValues()
		{
			/// The TransformCrusher and ElementCrushers have an apply method that will apply changes without overwriting the values
			/// that were not networked. For example if your object only moves on the x axis, y and z will not change from their current
			/// values when Apply() is used.
			if (!IsMine)
				compMatrix.Apply(transform);

			mr.material.color = color;
			meterImage.fillAmount = meterval;
			flasher.SetActive(flasherIsOn);
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
//#elif MIRROR
//				return (int)netId;
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
#pragma warning restore CS0618 // UNET is obsolete
