using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace emotitron.NST
{
	/// <summary>
	/// Base component for all Transform Element components that go on networked objects.
	/// </summary>
	public abstract class NSTElementComponent : NSTComponent
	{
		[HideInInspector] public NSTElementsEngine nstElementsEngine;
		public abstract TransformElement TransElement { get; }

		public override void OnNstPostAwake()
		{
			base.OnNstPostAwake();
			nstElementsEngine = nst.nstElementsEngine;
		}

		///// <summary>
		///// This awake base inherits from NSTComponent, and ensures that NSTRewindEngine exists and caches its reference.
		///// </summary>
		//protected override void Awake()
		//{
		//	//base.Awake();

		//	//// null nst means this gameobject isnt a real NST and is being destroyed at startup
		//	//if (nst == null)
		//	//	return;

		//	//nstElementsEngine = NSTElementsEngine.EnsureExistsOnRoot(transform);
		//}
	}


#if UNITY_EDITOR

	[CustomEditor(typeof(NSTElementComponent))]
	[CanEditMultipleObjects]
	public abstract class NSTElementComponentEditor : NSTHeaderEditorBase
	{
		protected NSTElementComponent _target;
		protected NSTElementsEngine nstElementsEngine;
		protected NetworkSyncTransform nst;


		public override void OnEnable()
		{
			headerName = HeaderElementAddonName;
			headerColor = HeaderElementAddonColor;
			base.OnEnable();

			_target = (NSTElementComponent)target;
			_target.nstElementsEngine = NSTElementsEngine.EnsureExistsOnRoot(_target.transform, false);
			nstElementsEngine = _target.nstElementsEngine;


			// First make sure this is unique for this gameobject
			MakeAllNamesUnique(_target.gameObject);
		}

		static List<string> uniqueNames = new List<string>(32);
		
		/// <summary>
		/// Make all TE names uqique... with a priority of changing the newly created one first.
		/// </summary>
		public static void MakeAllNamesUnique(GameObject go, TransformElement targetTe = null)
		{
			uniqueNames.Clear();

			// First add the selected gameobjects names to list - all but the last (should be the last added)
			INSTTransformElement[] iTransElement = go.GetComponents<INSTTransformElement>();

			// Determine if the last element has the name Unnamed... and presume it is a new item if so.
			TransformElement newElement =
				(targetTe != null) ? targetTe :
				(iTransElement.Length > 0 && iTransElement[iTransElement.Length - 1].TransElement.name == "Unnamed") ?
				iTransElement[iTransElement.Length - 1].TransElement :
				null;

			string holdname = (newElement != null) ? newElement.name : "";

			// Renmae it temporarily to make sure it doesn't conflict already.
			if (newElement != null)
				newElement.name = "CRAZYPLACEHOLDERNAME";

			// Then add all te names to the list - making them unique if any somehow failed to be
			iTransElement = go.transform.root.GetComponentsInChildren<INSTTransformElement>(true);

			for (int i = 0; i < iTransElement.Length; i++)
			{
				TransformElement te = iTransElement[i].TransElement;

				// Disallow empty names
				if (te.name == "")
					te.name = "Unnamed";

				// Add zeros until the name is unique
				while (uniqueNames.Contains(te.name))
					te.name += "0";

				uniqueNames.Add(te.name);
			}

			// Name the new element back to Unnamed, and add zeros until it is unique
			if (newElement != null)
			{
				newElement.name = holdname;
				while (uniqueNames.Contains(newElement.name))
					newElement.name += "0";
			}
		}
	}
#endif


}
