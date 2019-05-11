//Copyright 2018, Davin Carten, All rights reserved

#if PUN_2_OR_NEWER || MIRROR || !UNITY_2019_1_OR_NEWER

using UnityEngine;
using emotitron.Utilities.SmartVars;
using emotitron.Controller;
using emotitron.Compression;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace emotitron.NST.Sample
{

	[AddComponentMenu("NST/Sample Code/NST Sample Controller")]

	/// <summary>
	/// A VERY basic and generic example of a controller for getting started. 
	/// Feel free to augment this, replace this, or borrow parts from this as needed.
	/// If the gameobject is a non-kinematic rigidbody it will use force to move the object, otherwise it will translate it each update. 
	/// Setting the Restrict To NST Ranges to True will restrict translates to the ranges of the NST and any NST Elements (add-on).
	/// Physics objects (Rigidbodies) can't be range restricted since they use forces, and should be constrained by world colliders.
	/// </summary>
	public class NSTSampleController : NSTComponent, INstStart
	{
#region Inspector Fields

		[Tooltip("Leave empty to use the game object this component is attached to.")]
		public GameObject _gameObject;

		[Tooltip("Automatically add percentage of force/rate to forward direction. This is just here to make testing easier, by making things move on their own.")]
		[Range(0, 1)]
		public float autoForward = 0;

		[Tooltip("Automatically add rotation of force/rate. This is just here to make testing easier, by making things move on their own.")]
		[Range(0, 1)]
		public float autoRotate = 0;

		[Tooltip("If true, will find PostitionElements and RotationElements on the gameobject and restrict movement to their data ranges.")]
		public bool restrictToNstRange = true;

		[Tooltip("This complex sounding switch just indicates whether rigidbodies set 'isKinematic=true' are moved using the transform translate, or with rb.MovePosition/rb.MoveRotation. Moving them by translate is more immediate, but may have some undesired effects on physics.")]
		public bool translateKinematic = false;


		[Header("Force (Non-Kinematic RBs only)")]
		public float moveForce = 300f;
		public float turnForce = 30f;


		[Header("Rates (Kinematic and non-RBs)")]
		public float moveRate = 2f;
		public float turnRate = 50f;

		[Space]
		[Tooltip("Normalize accumulated movement inputs into a final vector magnitude of 1")]
		public bool clampMove = true;

		[Header("Key Inputs")]
		public NSTControllerPresets keyPresets = (NSTControllerPresets)(-1);
		public ControllerKeyMap mapping = new ControllerKeyMap();


		[Header("Mouse Inputs")]
		public MouseInputAxis mousePitch = new MouseInputAxis { axisId = 0 };
		public MouseInputAxis mouseYaw = new MouseInputAxis { axisId = 1 };
		public MouseInputAxis mouseRoll = new MouseInputAxis { axisId = 2 };

		public bool enableBasicTouch = false;

#endregion

		private bool hasAuthority;
		private Rigidbody rb;

		// restrict axis bools
		private bool[] allowMove = new bool[3] { true, true, true };
		private bool[] allowTurn = new bool[3] { true, true, true };

		private bool moveWithForce;
		private bool turnWithForce;
		private bool isQuat = true;
		private bool isRootGO;

		private IPositionElement pe;
		private IRotationElement re;

		public override void OnNstPostAwake()
		{
			base.OnNstPostAwake();

			if (_gameObject == null)
				_gameObject = gameObject;

			isRootGO = (_gameObject == transform.root.gameObject);

			// If this is the root, try to get the RB
			if (isRootGO && nst.rb)
				rb = nst.rb;
		}

		public void OnNstStart()
		{

			if (nst != null)
				hasAuthority = nst.na.IsMine;

			// Find position and rotation elements that are defined for this child (assumes there are no more than one per, since there shouldn't be)
			if (nst != null && nst.nstElementsEngine != null)
				foreach (TransformElement te in nst.nstElementsEngine.transformElements)
					if (te.gameobject == _gameObject)
					{
						if (te is IPositionElement)
							pe = te as IPositionElement;
						else if (te is IRotationElement)
							re = te as IRotationElement;
					}

			isQuat = (re != null && (re as TransformElement).crusher.TRSType == emotitron.Compression.TRSType.Quaternion);

			turnWithForce = isRootGO && rb != null && !rb.isKinematic && isQuat;
			moveWithForce = isRootGO && rb != null && !rb.isKinematic;

			// Set the restriction bools based on the restrict switch and the enabled/disabled axes
			if (restrictToNstRange)
			{
				if (pe != null)
					for (int i = 0; i < 3; ++i)
						allowMove[i] = (((int)pe.IncludedAxes & 1 << i) != 0);

				if (re != null && !isQuat)
					for (int i = 0; i < 3; ++i)
						allowTurn[i] = (((int)re.IncludedAxes & 1 << i) != 0);
			}
		}

		// Input Accumulators
		float[] move = new float[3];
		float[] turn = new float[3];

		void Update()
		{

			// Only accept input for the local player
			if (!hasAuthority)
				return;


			// Reset the accumulators to zero
			for (int i = 0; i < 3; i++)
			{
				move[i] = 0;
				turn[i] = 0;
			}

			// Add autoforward if used.
			move[2] += autoForward;
			turn[1] += autoRotate;

			AccumulateKeyInputs();
			AccumulateMouse();

			if (enableBasicTouch)
				AccumulateTouchInputs();

			// Make the accumulators into vectors
			Vector3 turns = new Vector3(turn[0] * Time.deltaTime, turn[1] * Time.deltaTime, turn[2] * Time.deltaTime);

			Vector3 moves = (clampMove) ?
				Vector3.ClampMagnitude(new Vector3(move[0] * Time.deltaTime, move[1] * Time.deltaTime, move[2] * Time.deltaTime), 1):
									   new Vector3(move[0] * Time.deltaTime, move[1] * Time.deltaTime, move[2] * Time.deltaTime);

			ApplyRotation(turns);
			ApplyPosition(moves);

		}

		float _lastMouseX;
		float _lastMouseY;

		private void AccumulateMouse()
		{
			for (int i = 0; i < 3; i++)
			{
				MouseInputAxis m = (i == 0) ? mousePitch : (i == 1) ? mouseYaw : mouseRoll;

				if (m.mouseAxis != MouseAxes.None)
					turn[i] += (
						(m.mouseAxis == MouseAxes.MouseX) ? (_lastMouseX - Input.mousePosition.x) :
						(m.mouseAxis == MouseAxes.MouseY) ? (_lastMouseY - Input.mousePosition.y) :
						(m.mouseAxis == MouseAxes.ScrollX) ? Input.mouseScrollDelta.x :
						Input.mouseScrollDelta.y
						)
						* (m.invert ? -m.sensitivity : m.sensitivity);
			}
			_lastMouseX = Input.mousePosition.x;
			_lastMouseY = Input.mousePosition.y;
		}
		private void AccumulateKeyInputs()
		{
			if (allowMove[0])
			{
				if (Input.GetKey(mapping.keyMaps[(int)InputAxis.MoveRight]))
					move[0] += 1;

				if (Input.GetKey(mapping.keyMaps[(int)InputAxis.MoveLeft]))
					move[0] += -1;
			}

			if (allowMove[1])
			{
				if (Input.GetKey(mapping.keyMaps[(int)InputAxis.MoveUp]))
					move[1] += 1;

				if (Input.GetKey(mapping.keyMaps[(int)InputAxis.MoveDown]))
					move[1] += -1;
			}

			if (allowMove[2])
			{
				if (Input.GetKey(mapping.keyMaps[(int)InputAxis.MoveForward]))
					move[2] += 1;

				if (Input.GetKey(mapping.keyMaps[(int)InputAxis.MoveBack]))
					move[2] += -1;
			}

			if (allowTurn[0])
			{
				if (Input.GetKey(mapping.keyMaps[(int)InputAxis.PitchDown]))
					turn[0] += 1;

				if (Input.GetKey(mapping.keyMaps[(int)InputAxis.PitchUp]))
					turn[0] += -1;
			}

			if (allowTurn[1])
			{
				if (Input.GetKey(mapping.keyMaps[(int)InputAxis.YawRight]))
					turn[1] += 1;

				if (Input.GetKey(mapping.keyMaps[(int)InputAxis.YawLeft]))
					turn[1] += -1;
			}

			if (allowTurn[2])
			{

				if (Input.GetKey(mapping.keyMaps[(int)InputAxis.RollLeft]))
					turn[2] += 1;

				if (Input.GetKey(mapping.keyMaps[(int)InputAxis.RollRight]))
					turn[2] += -1;

			}
		}
		private void AccumulateTouchInputs()
		{
			// Some touch inputs for testing on mobile.
			for (var i = 0; i < Input.touchCount; ++i)
			{
				Touch touch = Input.GetTouch(i);
				if (touch.phase == TouchPhase.Stationary || touch.phase == TouchPhase.Began)
				{
					if (allowMove[1])
					{
						if (touch.position.x < (Screen.width * .33f))
						{
							turn[1] = -1;
						}
						else if (touch.position.x > (Screen.width * .667f))
						{
							turn[1] = 1;
						}
					}

					if (allowMove[2])
					{
						if (touch.position.y < (Screen.height * .33f))
						{
							move[2] += -1;
						}
						else if (touch.position.y > (Screen.height * .66f))
						{
							move[2] += 1;
						}
					}
				}
			}
		}


		/// <summary>
		/// Apply the position inputs, respecting if the moved gameobject is a rigidbody or not. Moves non-kinematic rigidbodies with force, 
		/// moves all others with translation. restrictToNstRanges=true will clamp the ranges to the NST and NST Element ranges set for that object, 
		/// if any exist.
		/// </summary>
		/// <param name="moves"></param>
		private void ApplyPosition(Vector3 moves)
		{
			bool local = (pe == null || (pe as TransformElement).crusher.local);
			// Position
			// Non kinematic rigidbodies can't really be ranged (without an insane amount of code) and this will be the root object in all cases (rbs can't be on children)
			if (moveWithForce)
			{
				rb.AddRelativeForce(moves * moveForce);
			}
			else
			{
				// TODO: this use of rotation seems very wrong

				Vector3 unclamped =
					(isRootGO || !local) ?
					_gameObject.transform.position + _gameObject.transform.rotation * (moves * moveRate) :
					_gameObject.transform.localPosition +  moves * moveRate;

				Vector3 clamped =
					(restrictToNstRange && pe != null) ? pe.GetCorrectedForOutOfBounds(unclamped) :
					(restrictToNstRange && isRootGO) ? WorldBoundsSO.defaultWorldBoundsCrusher.Clamp(unclamped) : unclamped; // WorldCompressionSettings.ClampAxes(unclamped) : unclamped;

				if (!isRootGO && pe != null)
					pe.Apply(clamped);
				// If this is the root object, we will want to move this
				else if (rb == null || translateKinematic)
				{
					if (local)
						_gameObject.transform.localPosition = clamped;
					else
						_gameObject.transform.position = clamped;
				}
					
				else
					rb.MovePosition(clamped);
			}
		}

		/// <summary>
		/// Apply the rotation inputs, respecting if the moved gameobject is a rigidbody or not. Moves non-kinematic rigidbodies with force, 
		/// moves all others with translation. restrictToNstRanges=true will clamp the ranges to the NST and NST Element ranges set for that object, 
		/// if any exist. Additionally, even if the objects is a non-kinematic RB, this will still rotate using translate if the rotation type is Euler.
		/// </summary>
		private void ApplyRotation(Vector3 turns)
		{
			bool local = (re == null || (re as RotationElement).crusher.local);

			// Turn with force only if is a nonKinematic RB and rotation is of the Quat type - otherwise must be moved as euler angles
			if (turnWithForce)
			{
				rb.AddRelativeTorque(turns * turnForce);
				return;
			}

			GenericX unclamped =
				(isRootGO || !local) ?
				_gameObject.transform.eulerAngles + turns * turnRate :
				_gameObject.transform.localEulerAngles + turns * turnRate;

			// Non-Physics-based rotation
			GenericX clamped = (restrictToNstRange && re != null && !isQuat) ?
				re.GetCorrectedForOutOfBounds(unclamped) :
				(Vector3)unclamped;


			if (!isRootGO && re != null)
				re.Apply(clamped);
			// isKinematic ... moverotation otherwise it will studder
			else if (rb == null || translateKinematic)
			{
				if (local)
					_gameObject.transform.localRotation = clamped;
				else
					_gameObject.transform.rotation = clamped;
			}

			else
				rb.MoveRotation(clamped);
		}

		public void ApplyPreset(ControllerKeyMap target, NSTControllerPresets copyFrom)
		{
			ControllerKeyMap source = ControllerKeyMap.presets[(int)copyFrom];

			for (int i = 0; i < mapping.keyMaps.Length; i++)
				mapping.keyMaps[i] = source.keyMaps[i];

			keyPresets = copyFrom;
		}


		public bool CompareToPreset(NSTControllerPresets selected)
		{
			ControllerKeyMap selectedPreset = ControllerKeyMap.presets[(int)selected];

			// return false if any key mapping doesn't match
			for (int i = 0; i < mapping.keyMaps.Length; i++)
				if (mapping.keyMaps[i] != selectedPreset.keyMaps[i])
					return false;

			return true;
		}
	}


#if UNITY_EDITOR
	[CustomEditor(typeof(NSTSampleController))]
	[CanEditMultipleObjects]
	public class NSTSampleControllerEditor : NSTSampleHeader
	{
		int lastSelectedPreset;

		public override void OnEnable()
		{
			base.OnEnable();

			NSTSampleController _target = (NSTSampleController)target;
			bool isRoot = _target.transform.parent == null;

			// If targetPreset == -1 then this has been run before
			if ((int)_target.keyPresets == -1) //NSTSampleController.NSTControllerPresets.None)
			{
				_target.ApplyPreset(_target.mapping, isRoot ? NSTControllerPresets.FreeController : NSTControllerPresets.Secondary);

			}
			lastSelectedPreset = (int)_target.keyPresets;
		}

		public override void OnInspectorGUI()
		{
			NSTSampleController _target = (NSTSampleController)target;
			
			// If the preset selection has changed... apply that presets values (unless its Custom ... custom will not overwrite user settings)
			if (lastSelectedPreset != (int)_target.keyPresets && _target.keyPresets != NSTControllerPresets.Custom)
			{
				_target.ApplyPreset(_target.mapping, _target.keyPresets);
			}

			// if there are differences in the input mappings from the preset, switch the selection to Custom so they don't keep getting overwritten.
			else if (_target.keyPresets != NSTControllerPresets.Custom && _target.CompareToPreset(_target.keyPresets) == false)
			{
				_target.keyPresets = NSTControllerPresets.Custom;
			}

			lastSelectedPreset = (int)_target.keyPresets;

			base.OnInspectorGUI();
		}
	}
	
#endif
}

#endif