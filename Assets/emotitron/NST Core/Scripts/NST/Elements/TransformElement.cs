//Copyright 2018, Davin Carten, All rights reserved

#if PUN_2_OR_NEWER || MIRROR || !UNITY_2019_1_OR_NEWER

using UnityEngine;
using emotitron.Utilities.SmartVars;
using emotitron.Compression;
using emotitron.Utilities.GUIUtilities;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace emotitron.NST
{
	public enum ElementType { Position, Rotation, Scale }
	public enum Compression { None, HalfFloat, LocalRange }

	[System.Serializable]
	public abstract class TransformElement
	{
		[Tooltip("Elements need a name in order to be called in code by name. You can identify elements using the NSTTransformElement.elementIDLookup dictionary, but it is easier to use the NSTTransformElement.elementLookup using the name, and caching the element returned.")]
		public string name;
		public bool isRoot;

		public IncludedAxes IncludedAxes
		{
			get
			{
				/// TODO: This test doesn't accurately return the case of a uniform axis - may need to revist later
				if (crusher.TRSType == TRSType.Scale && crusher.uniformAxes != ElementCrusher.UniformAxes.NonUniform)
					return (IncludedAxes)crusher.uniformAxes;

				if (crusher.TRSType == TRSType.Quaternion)
					return crusher.QCrusher.Enabled ? IncludedAxes.XYZ : IncludedAxes.None;

				return (IncludedAxes)((crusher.XCrusher.Enabled ? 1 : 0) | (crusher.YCrusher.Enabled ? 2 : 0) | (crusher.ZCrusher.Enabled ? 4 : 0));
			}
		}

		[SerializeField]
		public ElementCrusher crusher;

		public int index;
		public float drawerHeight;
		public NetworkSyncTransform nst;
		public NSTNetAdapter na;
		public INSTTransformElement nstElement;

#region Inspector Values

		[Tooltip("Which events will cause full updates to be sent on the next tick.")]
		public SendCullMask sendCullMask = SendCullMask.OnChanges | SendCullMask.OnTeleport | SendCullMask.OnCustomMsg | SendCullMask.OnRewindCast;

		[Tooltip("The spacing of keyframes.")]
		[Range(0, 32)]
		public int keyRate = 5;

		[Tooltip("Assign this if you want this to element sync to apply to a different gameobject than the one this component is attached to. Leave this empty to default to this child gameobject.")]
		public GameObject gameobject;

		[Tooltip("0 = No extrapolation. 1 = Full extrapolation. Extrapolation occurs when the buffer runs out of frames. Without extrapolation the object will freeze if no new position updates have arrived in time. With extrapolation the object will continue in the direction it was heading as of the last update until a new update arrives.")]
		[Range(0, 1)]
		public float extrapolation = .5f;


		[Tooltip("The max number of sequential frames that will be extrapolated. Too large a value and objects will wander too far during network hangs, too few and network objects will freeze when the buffer empties. Extrapolation should not be occurring often - if at all, so a smaller number is ideal (default = 1 frame).")]
		[Range(0, 16)]
		public int maxExtrapolates = 2;

		[Tooltip("Teleport command from server will force the server state of this element on the owner.")]
		public bool teleportOverride = true;

#endregion


		[HideInInspector] public GameObject ghostGO;
		[HideInInspector] public float lastSentKeyTime;
		[HideInInspector] public int snapshotFrameId;
		[HideInInspector] public int targetFrameId;
		[HideInInspector] public CompressedElement lastTeleportValue = new CompressedElement(); // CompressedElement.Empty;


		public CompressedElement lastSentCompressed = new CompressedElement();
		public GenericX lastSentTransform;
		//private CompressedElement tempCE = new CompressedElement();

		protected bool hasReceivedInitial;

		public abstract GenericX Localized { get; set; }

		[HideInInspector] public ElementFrame[] frames;
		[HideInInspector] public GenericX[] history;

		// cached values
		private int frameCount;

		public class ElementFrame
		{
			public GenericX xform;
			public CompressedElement compXform = new CompressedElement();
			public bool hasChanged;
			public TransformElement transformElement;

			/// <summary>
			/// Copy CompressedElement to compXForm, and apply Decompress(value) to xForm.
			/// </summary>
			public CompressedElement CompXForm
			{
				get { return compXform; }
				set
				{
					compXform.CopyFrom(value);
					xform = transformElement.Decompress(value);
				}
			}

			public ElementFrame(GenericX xform, CompressedElement compXform, bool hasChanged, TransformElement transformElement)
			{
				this.xform = xform;
				this.compXform.CopyFrom(compXform);
				this.hasChanged = hasChanged;
				this.transformElement = transformElement;
			}
		}


		public void Initialize(NetworkSyncTransform _nst, INSTTransformElement _nstElement)
		{
			nst = _nst;
			nstElement = _nstElement;

			na = nst.na ? nst.na : (nst.na = nst.GetComponent<NSTNetAdapter>());

			frameCount = NSTMaster.FRAME_COUNT / nst.sendEveryXTick;

			//// TODO: Would rather not have to do this here - but the automation for this isn't firing in time
			//crusher.CacheValues();

			frames = new ElementFrame[frameCount + 1];
			for (int i = 0; i < frames.Length; i++)
			{
				var ce = new CompressedElement();
				Compress(ce);
				frames[i] = new ElementFrame(Localized,	ce, false, this);
			}

			history = new GenericX[frameCount + 1];
			for (int i = 0; i < history.Length; i++)
				history[i] = new GenericX();

			Compress(lastSentCompressed);
			lastSentTransform = Localized;
		}

		public void Snapshot(Frame newTargetFrame, bool lateUpdate = false, bool midTeleport = false)
		{
			ElementFrame newte = frames[newTargetFrame.frameid];
			ElementFrame oldte = frames[targetFrameId];

			bool isNull = newte.xform.type == XType.NULL;
	
			// If the element carried no useful information for this frame, use the last updates value.
			if (isNull || (HeaderSettings.single.includeSceneIndex && (newTargetFrame.sceneIndex != NSTSceneManager.CurrentSceneIndex)))
			{
				bool oldTargetIsNull = oldte == null || oldte.xform.type == XType.NULL;

				newte.xform = oldTargetIsNull ? Localized : oldte.xform;

				if (oldTargetIsNull)
					Compress(newte.compXform, newte.xform);
				else
					newte.compXform.CopyFrom(oldte.compXform);
			}

			// First run set both target and snapshot to the incoming.
			if (hasReceivedInitial == false)
			{
				targetFrameId = newTargetFrame.frameid;
				hasReceivedInitial = true;
			}
			snapshotFrameId = targetFrameId; // LocalizedRot;
			targetFrameId = newTargetFrame.frameid;

		}


		public void  Compress(CompressedElement target)
		{
			Compress(target, Localized);
		}

		public void Compress(CompressedElement target, GenericX uncompressed)
		{
			if (crusher.TRSType == TRSType.Quaternion)
			{
				crusher.Compress(target, (Quaternion)uncompressed);
			}
			else
			{
				crusher.Compress(target, (Vector3)uncompressed);
			}
		}

		public GenericX Decompress(CompressedElement comp)
		{
			if (crusher.TRSType == TRSType.Quaternion)
			{
				return (Quaternion)crusher.Decompress(comp);
			}
			else
			{
				return (Vector3)crusher.Decompress(comp);
			}
		}

		public bool Write(ref UdpBitStream bitstream, Frame frame)
		{
			ElementFrame e = frames[frame.frameid];
			CompressedElement compXform = e.compXform;

			Compress(compXform);
			e.xform = Decompress(compXform); // Localized;

			/// Experimental - Apply the outgoing lossy values to the GhostGO so owner/authority of object can use it to replicate lossy events like weapon fire.
			if (ghostGO)
				Apply(e.xform, ghostGO);

			if (!nstElement.Enabled)
			{
				bitstream.WriteBool(false);
				return false;
			}

			bool forceUpdate = IsUpdateForced(frame);

			// For frames between forced updates, we need to first send a flag bit for if this element is being sent
			if (!forceUpdate)
			{
				bool hasChanged = !compXform.Equals(lastSentCompressed) && sendCullMask.OnChanges();
				bitstream.WriteBool(hasChanged);

				// if no changes have occured we are done.
				if (!hasChanged)
					return false;

			}
			else
				bitstream.WriteBool(true);

			crusher.Write(compXform, bitstream.Data, ref bitstream.ptr);

			lastSentCompressed.CopyFrom(compXform);
			lastSentTransform = e.xform;

			return true;
		}


		public bool Read(ref UdpBitStream bitstream, Frame frame, Frame currentFrame)
		{
			ElementFrame e = frames[frame.frameid];

			//bool forcedUpdate = IsUpdateForced(frame);
			bool applyToGhost = ShouldApplyToGhost(frame);
			bool isCurrentFrame = frame == currentFrame;

			// Only read for the sent bit if not forced, there is no check bit for forced updates (since all clients and server know it is forced)
			bool hasChanged = /*forcedUpdate || */bitstream.ReadBool();

			/// If no info was included, or if we have a scene mismatch, indicate this transform update as invalid.
			if (!hasChanged/* || frame.sceneIndex != NSTSceneManager.CurrentSceneIndex*/)
			{
				// Leave the transform as is if this is the current frame and hasn't changed - it has already been extrapolated and is mid-lerp
				// So leave it alone. Otherwise sete it to GenericX.NULL just to make debugging easier. Eventually can remove this.
				if (!isCurrentFrame)
				{
					//Debug.LogError(name + " NotCurrentFrame!");
					e.xform = GenericX.NULL;
					e.compXform.Clear(); // = CompressedElement.Empty;
				}
				return false;
			}

			crusher.Read(e.compXform, bitstream.Data, ref bitstream.ptr);

			e.xform = Decompress(e.compXform);

			if (applyToGhost)
				Apply(e.xform, ghostGO);

			return true;
		}

		public void MirrorToClients(ref UdpBitStream outstream, Frame frame, bool hasChanged)
		{
			//// Write the used flag (if this is not a forced update) and determine if an update needs to be written.
			//if (WriteUpdateFlag(ref outstream, frame, hasChanged) == false)
			//	return;

			outstream.WriteBool(hasChanged);

			if (!hasChanged)
				return;

			ElementFrame e = frames[frame.frameid];

			crusher.Write(e.compXform, outstream.Data, ref outstream.ptr);
		}


		public void Apply(GenericX val)
		{
			Apply(val, gameobject);
		}
		/// <summary>
		/// Apply a rotation to a gameobject, respecting this elements useLocal and axis restrictions
		/// </summary>
		public void Apply(GenericX val, GameObject targetGO)
		{
			if (crusher.TRSType == TRSType.Quaternion)
			{
				crusher.Apply(targetGO.transform, new Element((Quaternion)val));
			}
			else
				crusher.Apply(targetGO.transform, (Vector3)val);
		}


		///// <summary>
		///// Write the flag bool if this is not a forced update, and return true if the element should be written to the stream (the value of the flag).
		///// </summary>
		//protected bool WriteUpdateFlag(ref UdpBitStream outstream, Frame frame, bool hasChanged)
		//{
		//	bool forcedUpdate = IsUpdateForced(frame);

		//	// For non-forced updates we need to set the used flag.
		//	if (!forcedUpdate)
		//		outstream.WriteBool(hasChanged);

		//	// exit if we are not writing a compressed value.
		//	if (!hasChanged && !forcedUpdate)
		//		return false;

		//	return true;
		//}

		/// <summary>
		/// This is the logic for when a frame must be sent using info available to all clients/server, so in these cases elements do not need to send a "used" bit
		/// ahead of each element, since an update is required.
		/// </summary>
		protected bool IsUpdateForced(Frame frame) // UpdateType updateType, int frameid)
		{
			UpdateType updateType = frame.updateType;
			int frameid = frame.frameid;
			bool hasAuthority = na.IsMine;
			
			bool isOfftick = frameid == frameCount;
			return
				(sendCullMask.EveryTick() && !isOfftick) ||
				(sendCullMask.OnCustomMsg() && updateType.IsCustom()) ||
				(sendCullMask.OnTeleport() && updateType.IsTeleport()) ||
				(sendCullMask.OnRewindCast() && updateType.IsRewindCast()) ||
				(updateType.IsTeleport() && teleportOverride) || // teleport flagged updates must send all teleport elements
				(!isOfftick && keyRate != 0 && frameid % keyRate == 0); // keyrate mod, but not for offtick frame, since that is a special case.
		}

		protected bool ShouldApplyToGhost(Frame frame)
		{
			return (na.IAmActingAuthority && frame.updateType.IsRewindCast());
		}

		public GenericX Extrapolate()
		{
			return Extrapolate(frames[targetFrameId].xform, frames[snapshotFrameId].xform);
		}
		
		public void Teleport(Frame frame)
		{
			int frameid = frame.frameid;
			Rigidbody rb = nst.rb;

			CompressedElement compressed = frames[frameid].compXform;
			GenericX decompressed = frames[frameid].xform;

			// if this is rootRotation, and has an rb that is not isKinematic, make it kinematic temporarily for this teleport
			bool setKinematic = (index == 0 && rb != null && !rb.isKinematic);
			bool wasKinematic;

			if (setKinematic)
			{
				wasKinematic = rb.isKinematic;
				rb.isKinematic = true;
			}
			else
				wasKinematic = false;

			Localized = decompressed;

			lastSentCompressed.CopyFrom(compressed);
			lastSentTransform = decompressed;

			frames[snapshotFrameId].xform = decompressed;
			compressed.CopyFrom(compressed);

			frames[targetFrameId].xform = decompressed;
			frames[targetFrameId].compXform.CopyFrom(compressed);

			if (setKinematic)
				rb.isKinematic = wasKinematic;

			//snapshotFrameId = frameid;
			//targetFrameId = frameid;
		}

		// TODO: Make non-static once axisRanges exist for rotation
		public static IncludedAxes GetIncludedAxisEnumFromRanges(FloatCrusher[] crusher)
		{
			return (IncludedAxes)(
				(crusher[0].Enabled ? 1 : 0) | 
				(crusher[1].Enabled ? 2 : 0) | 
				(crusher[2].Enabled ? 4 : 0) | 
				// TODO: this currently is meaningless
				(crusher.Length == 4 && crusher[3].Enabled ? 8 : 0));
		}

		public Vector3 GetCorrectedForOutOfBounds(Vector3 value)
		{
			return new Vector3(
				crusher.XCrusher.OutOfBoundsCorrect(value[0]),
				crusher.YCrusher.OutOfBoundsCorrect(value[1]),
				crusher.ZCrusher.OutOfBoundsCorrect(value[2])
				);
		}

		public abstract GenericX Extrapolate(GenericX curr, GenericX prev);

		public void UpdateInterpolation(float t)
		{
			if (!hasReceivedInitial)
				return;

			if (crusher.TRSType == TRSType.Quaternion)
				Apply(Quaternion.Slerp(frames[snapshotFrameId].xform, frames[targetFrameId].xform, t));
			else if(crusher.TRSType == TRSType.Euler)
				Apply(Quaternion.Slerp(frames[snapshotFrameId].xform, frames[targetFrameId].xform, t).eulerAngles);
			else
				Apply(Vector3.Lerp(frames[snapshotFrameId].xform, frames[targetFrameId].xform, t));
		}

		public GenericX Lerp(GenericX start, GenericX end, float t)
		{
			if (crusher.TRSType == TRSType.Quaternion)
				return Quaternion.Slerp(start, end, t);
			else if (crusher.TRSType == TRSType.Euler)
				return Quaternion.Slerp(start, end, t).eulerAngles;
			else
				return Vector3.Lerp(start, end, t);
		}
	}


#if UNITY_EDITOR

	[CustomPropertyDrawer(typeof(TransformElement))]
	[CanEditMultipleObjects]

	public abstract class TransformElementDrawer : PropertyDrawer
	{
		public static Color positionHeaderBarColor =new Color(0.545f, 0.305f, 0.062f);
		public static Color rotationHeaderBarColor = new Color(0.447f, 0.184f, 0.529f);
		public static Color scaleHeaderBarColor = new Color(0.447f, 0.529f, 0.184f);

		public static Color gray = EditorGUIUtility.isProSkin ? new Color(.4f, .4f, .4f) : new Color(.7f, .7f, .7f);
		public static Color red = EditorGUIUtility.isProSkin ? new Color(.5f, .4f, .4f) : new Color(.7f, .6f, .6f);
		public static Color green = EditorGUIUtility.isProSkin ? new Color(.4f, .5f, .4f) : new Color(.6f, .7f, .6f);
		public static Color blue = EditorGUIUtility.isProSkin ? new Color(.4f, .4f, .5f) : new Color(.6f, .6f, .7f);
		public static Color purple = new Color(.3f, .2f, .3f);
		public static Color orange = new Color(.3f, .25f, .2f);

		protected const int LINEHEIGHT = 16;
		protected float rows = 16;

		protected float left;
		protected float margin;
		protected float realwidth;
		protected float colwidths;
		protected float currentLine;
		protected int savedIndentLevel;

		protected bool isPos;
		protected bool isRot;

		//protected bool noUpdates;

		protected SerializedProperty name;
		protected SerializedProperty isRoot;
		protected SerializedProperty keyRate;
		protected SerializedProperty sendCullMask;
		protected SerializedProperty gameobject;
		protected SerializedProperty extrapolation;
		protected SerializedProperty maxExtrapolates;

		protected SerializedProperty teleportOverride;

		public static GUIStyle lefttextstyle = new GUIStyle
		{
			alignment = TextAnchor.UpperLeft,
			richText = true
		};

		public override void OnGUI(Rect r, SerializedProperty property, GUIContent label)
		{
			EditorGUI.BeginProperty(r, label, property);

			property.serializedObject.ApplyModifiedProperties();
			property.serializedObject.Update();

			var par = PropertyDrawerUtility.GetParent(property);

			GameObject parGO;

			// the parent may be an NST or a TransformElement
			if (par is NSTElementComponent)
				parGO = (par as NSTElementComponent).gameObject;
			else
				parGO = (par as NetworkSyncTransform).gameObject;

			TransformElement te = PropertyDrawerUtility.GetActualObjectForSerializedProperty<TransformElement>(fieldInfo, property);

			name = property.FindPropertyRelative("name");
			isRoot = property.FindPropertyRelative("isRoot");
			keyRate = property.FindPropertyRelative("keyRate");
			sendCullMask = property.FindPropertyRelative("sendCullMask");
			gameobject = property.FindPropertyRelative("gameobject");
			extrapolation = property.FindPropertyRelative("extrapolation");
			maxExtrapolates = property.FindPropertyRelative("maxExtrapolates");
			teleportOverride = property.FindPropertyRelative("teleportOverride");

			isPos = (te is IPositionElement); 
			isRot = (te is IRotationElement);

			string typeLabel = (isPos) ? "Position" :  (isRot) ? "Rotation" : "Scale";

			margin = 4;
			realwidth = r.width + 16 - 4;
			colwidths = realwidth / 4f;

			colwidths = Mathf.Max(colwidths, 65); // limit the smallest size so things like sliders aren't shrunk too small to draw.

			currentLine = r.yMin + margin * 2;

			Color headerblockcolor = (isPos ? positionHeaderBarColor : isRot ? rotationHeaderBarColor : scaleHeaderBarColor);

			if (!isRoot.boolValue)
			{
				EditorGUI.DrawRect(new Rect(margin + 3, r.yMin + 2 + 2, realwidth - 6, LINEHEIGHT + 8), headerblockcolor);
			}

			savedIndentLevel = EditorGUI.indentLevel;

			EditorGUI.indentLevel = 0;
			if (!isRoot.boolValue)
			{
				string headerLabel = typeLabel + " Element";

				EditorGUI.LabelField(new Rect(r.xMin, currentLine, colwidths * 4, LINEHEIGHT), new GUIContent(headerLabel), "WhiteBoldLabel");

				NSTElementComponentEditor.MakeAllNamesUnique(parGO, te);

				EditorGUI.PropertyField(new Rect(r.xMin, currentLine, r.width - 4, LINEHEIGHT), name, new GUIContent(" "));

				currentLine += LINEHEIGHT + 8;
			}
			// The only element that will be found on the root (the actual NST component) is rotation
			else
			{
				EditorGUI.LabelField(new Rect(r.xMin, currentLine, r.width, LINEHEIGHT), new GUIContent("Root Rotation Updates"), "BoldLabel");
				currentLine += LINEHEIGHT + 4;
			}
			EditorGUI.indentLevel = 0;

			// Section for Send Culling enum flags

			left = 13;
			realwidth -= 16;

#if UNITY_2017_3_OR_NEWER
			sendCullMask.intValue = System.Convert.ToInt32(EditorGUI.EnumFlagsField(new Rect(left, currentLine, realwidth, LINEHEIGHT), new GUIContent("Send On Events:"), (SendCullMask)sendCullMask.intValue));
#else
			sendCullMask.intValue = System.Convert.ToInt32(EditorGUI.EnumMaskField(new Rect(left, currentLine, realwidth, LINEHEIGHT), new GUIContent("Send On Events:"), (SendCullMask)sendCullMask.intValue));
#endif


			currentLine += LINEHEIGHT + 4;

			if (!isRoot.boolValue)
			{
				EditorGUI.PropertyField(new Rect(left, currentLine, realwidth, LINEHEIGHT), gameobject, new GUIContent("GameObject:"));
				currentLine += LINEHEIGHT + 4;
			}

			if (((SendCullMask)sendCullMask.intValue).EveryTick() == false)
			{
				EditorGUI.PropertyField(new Rect(left, currentLine, realwidth, LINEHEIGHT), keyRate, new GUIContent("Key Every:"));
				currentLine += LINEHEIGHT + 2;
			}

			if (keyRate.intValue == 0 && sendCullMask.intValue == 0)
			{
				//noUpdates = true;
				EditorGUI.HelpBox(new Rect(left, currentLine, realwidth, 48), "Element Disabled. Select one or more 'Send On Events' event to trigger on, and/or set Key Every to a number greater than 0.", MessageType.Warning);
				currentLine += 50;

				property.serializedObject.ApplyModifiedProperties();
				//return;
			}
			else
			{
				//noUpdates = false;

				EditorGUI.PropertyField(new Rect(left, currentLine, realwidth, LINEHEIGHT), extrapolation, new GUIContent("Extrapolation:"));
				currentLine += LINEHEIGHT + 2;

				EditorGUI.PropertyField(new Rect(left, currentLine, realwidth, LINEHEIGHT), maxExtrapolates, new GUIContent("Max Extrapolations:"));
				currentLine += LINEHEIGHT + 2;

				EditorGUI.PropertyField(new Rect(left, currentLine, realwidth, LINEHEIGHT), teleportOverride, new GUIContent("Teleport Override:"));
				currentLine += LINEHEIGHT + 4;

				SerializedProperty crusher = property.FindPropertyRelative("crusher");
				float ch = EditorGUI.GetPropertyHeight(crusher);
				EditorGUI.PropertyField(new Rect(r.xMin, currentLine - 2, r.width, ch), crusher);
				currentLine += ch;
			}
			//property.serializedObject.ApplyModifiedProperties();
			//property.serializedObject.Update();

			//SerializedProperty crusher = property.FindPropertyRelative("crusher");
			//float ch = EditorGUI.GetPropertyHeight(crusher);
			//EditorGUI.PropertyField(new Rect(r.xMin, currentLine - 2, r.width, ch), crusher);
			//currentLine += ch;

			//property.serializedObject.ApplyModifiedProperties();


			SerializedProperty drawerHeight = property.FindPropertyRelative("drawerHeight");

			// revert to original indent level.
			EditorGUI.indentLevel = savedIndentLevel;

			// Record the height of this instance of drawer
			if (drawerHeight.floatValue != currentLine - r.yMin)
			{
				drawerHeight.floatValue = currentLine - r.yMin;
				property.serializedObject.ApplyModifiedProperties();
			}

			EditorGUI.EndProperty();
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{

			SerializedProperty drawerHeight = property.FindPropertyRelative("drawerHeight");

			if (drawerHeight.floatValue > 1)
				return drawerHeight.floatValue;

			return base.GetPropertyHeight(property, label) * rows;  // assuming original is one row
		}
	}
#endif
		}


#endif