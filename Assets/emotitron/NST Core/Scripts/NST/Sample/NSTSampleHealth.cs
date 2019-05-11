//Copyright 2018, Davin Carten, All rights reserved

#if PUN_2_OR_NEWER || MIRROR || !UNITY_2019_1_OR_NEWER

using System.Collections.Generic;
using UnityEngine;
using emotitron.Compression;
using emotitron.NST.HealthSystem;
using emotitron.Utilities.BitUtilities;
using emotitron.Debugging;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace emotitron.NST.Sample
{
	[AddComponentMenu("NST/Sample Code/NST Sample Health")]

	[DisallowMultipleComponent]
	/// <summary>
	/// Example code of how to piggyback player information, such as health on the back of the regular updates.
	/// EXTRA CAUTION should be taken when reading and writing to the bitstreams as done here. While these are the most efficient way
	/// to pack data, they offer NO protection against the developer not matching up their reads and writes.
	/// If you write 5 bits to the stream, you MUST read 5 bits from the stream on the receiving end or else all following
	/// reads will be reading from the wrong place and will return corrupt values.
	/// It can be very difficult to find bugs like this as well, as the corruption will show up as errors in other components that
	/// read from the stream after this.
	/// </summary>
	public class NSTSampleHealth : NSTComponent, IVitals, IMonitorVitals, INstBitstreamInjectFirst, INstOnStartLocalPlayer, INstPostUpdate, INstState
	{
		public static NSTSampleHealth lclPlyVitals;

		// Callbacks for IMonitorVitals interface that wants notifications about changes to the local player vitals
		public static List<IMonitorVitals> lclPlayerMonitors = new List<IMonitorVitals>();

		//[SerializeField][HideInInspector]
		//private AuthorityModel authorityModel;
		//public AuthorityModel AuthorityModel { get { return authorityModel; } }

		private List<IMonitorVitals> iMonitors;
		// cache some stuff
		int vitalsCount;
		bool iAmPlayerAvatar;

		[Range(0, 15)]
		[Tooltip("Sends every X update of the NST.")]
		public int updateRate = 5;

		[HideInInspector]
		public List<Vital> vitals;
		public List<Vital> Vitals
		{
			get { return vitals; }
		}

		[HideInInspector]
		public List<float> hitGroupModifers = new List<float>();

		[HideInInspector]
		public NSTNetAdapter NA { get { return na; } }

		void Reset()
		{
			// Feel free to change these starting values
			vitals = new List<Vital>(3)
			{
				new Vital(100, 50, 1f, 5f, 1f, "Health", 7),
				new Vital(100, 50, .667f, 0, 0, "Armor", 7),
				new Vital(250, 50, 1f, 1f, 15f, "Shield", 8)

			};
		}

		private int frameOffset = 2;

		public bool UpdateDue(int frameId)
		{
			return
				((frameId + frameOffset) % updateRate == 0) &&
				frameId < nst.frameCount;
		}

		// Indexer that returns the spedified health stat
		public Vital this [int vitalType]
		{
			get
			{
				return (vitalType < vitalsCount) ? vitals[vitalType] : null;
			}
		}

		public override void OnNstPostAwake()
		{
			base.OnNstPostAwake();

			vitalsCount = vitals.Count;

			// Collect all interface callbacks
			iMonitors = new List<IMonitorVitals>();
			nst.GetComponentsInChildren(true, iMonitors); //<IMonitorHealth>(true);

			int hitGroupCount = HitGroupSettings.Single.hitGroupTags.Count;

			// Make sure the size of the hitgroup list is correct, could be more elegant but will do for now.
			while (hitGroupModifers.Count > hitGroupCount)
				hitGroupModifers.RemoveAt(hitGroupModifers.Count - 1);
			while (hitGroupModifers.Count < hitGroupCount)
				hitGroupModifers.Add(1);
		}
		
		public void Start()
		{
			// Ensure that stats are initialized, since State change may not be triggered in some cases on startup.
			if (na.IAmActingAuthority && nst.State.IsAlive())
				ResetStats();
		}

		public void OnNstStartLocalPlayer()
		{
			lclPlyVitals = this;
			iAmPlayerAvatar = GetComponentInChildren<NSTSamplePlayer>() != null;
		}

		public void OnNstPostUpdate()
		{
			if (!na.IAmActingAuthority)// MasterNetAdapter.ServerIsActive)
				return;


			// if this networked object is alive, test for vitals regeneration.
			if (nst.State == State.Alive)
			{
				timeSinceLastDmgTaken += Time.deltaTime;

				for (int i = 0; i < vitalsCount; ++i)
					if (vitals[i].regenRate != 0)
						if (timeSinceLastDmgTaken > vitals[i].regenDelay)
							AddToVital(vitals[i].regenRate * Time.deltaTime, i, false);

				UpdateMonitors();
			}
		}

		/// <summary>
		/// Clients receive reports about health as part of their incoming streams. The server will have added them to its outgoing/mirror streams.
		/// </summary>
		public void NSTBitstreamIncomingFirst(Frame frame, Frame currFrame, ref UdpBitStream bitstream, bool isServer)
		{
			bool iAmActingAuthority = na.IAmActingAuthority;
			// If this is the server and has authority, it will receieve its own outgoings... account for that.
			// TODO: Cache this down the line with callbacks from the net engine to notify of ownership changes
			bool noIncomingExpected = 
				MasterNetAdapter.ServerIsActive && 
				iAmActingAuthority &&
				(NetLibrarySettings.single.defaultAuthority == DefaultAuthority.ServerAuthority || MasterNetAdapter.NET_MODEL == NetworkModel.ServerClient) && 
				!na.IsMine;

			// if we are authority, any incoming messages from others will not include a health. Only the authority should write health.
			if (noIncomingExpected)
				return;

			if (UpdateDue(frame.frameid))
			{
				for (int i = 0; i < vitalsCount; ++i)
				{
					Vital v = vitals[i];
					int incval = bitstream.ReadInt(v.bitsForStat);
					if (!iAmActingAuthority)
						v.Value = incval;
				}

				if (!iAmActingAuthority)
					UpdateMonitors();
			}

		}

		/// <summary>
		/// Server adds health info to altered outgoing streams that have come in from players.
		/// </summary>
		public void NSTBitstreamMirrorFirst(Frame frame, ref UdpBitStream outstream, bool waitingForTeleportConfirm)
		{
			if (UpdateDue(frame.frameid))
			{
				for (int i = 0; i < vitalsCount; ++i)
					outstream.WriteInt((int)vitals[i].Value, vitals[i].bitsForStat);
			}
		}
		
		/// <summary>
		/// Acting authority of objects report their health to others here.
		/// </summary>
		public void NSTBitstreamOutgoingFirst(Frame frame, ref UdpBitStream bitstream)
		{
			// TODO: Is this test needed?
			if (!na.IAmActingAuthority)
				return;

			if (UpdateDue(frame.frameid))
			{
				for (int i = 0; i < vitalsCount; ++i)
					bitstream.WriteInt((int)vitals[i].Value, vitals[i].bitsForStat);
			}
		}

		private float ModifyDamageForHitGroup(float dmg, int hitGroupMask)
		{
			// start with the default (usually should be 1f)
			float modifer = 0;

			// get the modifer with the greatest value of all of the flagged hitgroups
			for (int i = 0; i < hitGroupModifers.Count; i++)
				if ((hitGroupMask & (1 << i)) != 0)
					modifer = (Mathf.Max(modifer, hitGroupModifers[i]));

			return modifer * dmg;
		}

		float timeSinceLastDmgTaken;

		public void ApplyDamage(float dmg, int hitGroupMask = 0)
		{
			if (!na.IAmActingAuthority)
				return;

			if (dmg == 0)
				return;

			float modifiedDmg = ModifyDamageForHitGroup(dmg, hitGroupMask);

			XDebug.Log(!XDebug.logInfo ? null :
				(Time.time + " Apply Dmg: " + dmg + " Modified Dmg: " + modifiedDmg + " hitgroup mask:" + BitTools.PrintBitMask((uint)hitGroupMask)));

			timeSinceLastDmgTaken = 0;

			// Subtract damage. Start with highest index and pass mitigated damage down
			for (int i = vitalsCount - 1; i >= 0; i--)
			{
				float mitigatedDmg = modifiedDmg * vitals[i].absorption;
	
				// mitigated damage exceeds the entirety of this vital - take all of it.
				if (mitigatedDmg > vitals[i].Value)
				{
					modifiedDmg -= vitals[i].Value;
					vitals[i].Value = 0;
				}
				else
				{
					modifiedDmg -= mitigatedDmg;
					vitals[i].Value -= mitigatedDmg;

					// no more damage to recurse to next lower vital - we are done
					if (modifiedDmg == 0)
						break;
				}
			}

			UpdateMonitors();
		}

		public void ResetStats()
		{
			for (int i = 0; i < vitalsCount; ++i)
			{
				vitals[i].Value = vitals[i].startValue;
			}

			UpdateMonitors();
		}

		public void SetVital(float value, int vitalIndex, bool updateMonitors = false)
		{
			if (!na.IAmActingAuthority)
				return;

			vitals[vitalIndex].Value = value;

			if (updateMonitors)
				UpdateMonitors();
		}

		public void AddToVital(float value, int vitalIndex, bool updateMonitors = false)
		{
			if (!na.IAmActingAuthority)
				return;

			vitals[vitalIndex].Value += value;

			if (updateMonitors)
				UpdateMonitors();
		}

		public void UpdateMonitors()
		{
			foreach (IMonitorVitals cb in iMonitors)
				cb.OnVitalsChange(this);

			if (na.IsLocalPlayer && iAmPlayerAvatar)
			{
				foreach (IMonitorVitals cb in lclPlayerMonitors)
					cb.OnVitalsChange(this);
			}
		}

		public void OnNstState(State newState, State oldState)
		{
			if (newState.IsAlive() && !oldState.IsAlive())
			{
				ResetStats();
			}
		}

		public void AddCallback(IMonitorVitals iMonitorVitals)
		{
			if (!lclPlayerMonitors.Contains(iMonitorVitals))
				lclPlayerMonitors.Add(iMonitorVitals);
		}

		public void RemoveCallback(IMonitorVitals iMonitorVitals)
		{
			lclPlayerMonitors.Remove(iMonitorVitals);
		}

		// NSTSampleHealth is responsible for killing. Ideally this would be part of the Player Manager, but in trying to create dependencies between
		// the Sample classes SampleHealth is responsble for killing players here ... and SamplePlayer is responsible for respawning them currently.
		public void OnVitalsChange(IVitals vitals)
		{
			
			if (na.IAmActingAuthority && vitals[0].Value <= 0)
			{
				nst.State = State.Dead;
			}
		}
	}

#if UNITY_EDITOR

	[CustomEditor(typeof(NSTSampleHealth))]
	[CanEditMultipleObjects]
	public class NSTSampleHealthEditor : NSTSampleHeader
	{
		public override void OnInspectorGUI()
		{

			base.OnInspectorGUI();

			serializedObject.Update();

			EditorGUILayout.Space();

			var _t = (NSTSampleHealth)target;
			HitGroupSettings hgSettings = HitGroupSettings.Single;

			//EditorGUILayout.PropertyField(serializedObject.FindProperty("authorityModel"));

			var vitals = serializedObject.FindProperty("vitals");

			for (int i = 0; i < vitals.arraySize; i++)
				EditorGUILayout.PropertyField(vitals.GetArrayElementAtIndex(i));


			int count = hgSettings.hitGroupTags.Count;

			// Resize the array if it is invalid
			while (_t.hitGroupModifers.Count > count)
				_t.hitGroupModifers.RemoveAt(_t.hitGroupModifers.Count - 1);

			while (_t.hitGroupModifers.Count < count)
				_t.hitGroupModifers.Add(1f);

			EditorGUILayout.LabelField("Hit Box Group Modifiers", (GUIStyle)"BoldLabel");
			EditorGUILayout.BeginVertical("HelpBox");

			Rect r = EditorGUILayout.GetControlRect();
			EditorGUI.LabelField(r, "Hit Box Group", (GUIStyle)"BoldLabel");
			r.xMin += EditorGUIUtility.labelWidth;
			EditorGUI.LabelField(r, "Dmg Multiplier", (GUIStyle)"BoldLabel");

			for (int i = 0; i < count; i++)
			{
				_t.hitGroupModifers[i] = EditorGUILayout.FloatField(hgSettings.hitGroupTags[i], _t.hitGroupModifers[i]);
			}

			EditorGUILayout.EndVertical();

			serializedObject.ApplyModifiedProperties();

			HitGroupSettings.Single.DrawGui(target, true, false, true);
		}
	}

#endif
}

#endif