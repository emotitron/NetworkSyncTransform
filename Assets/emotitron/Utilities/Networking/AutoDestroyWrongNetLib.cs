//Copyright 2018, Davin Carten, All rights reserved

using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace emotitron.Utilities.Networking
{
	public class AutoDestroyWrongNetLib : MonoBehaviour
	{
		public enum NetLib { UNET = 0, PUN = 1, PUN2 = 2, PUNAndPUN2 = 3 }

		[SerializeField]
		public NetLib netLib;

		// Use this for initialization
		void Awake()
		{
#if PUN_2_OR_NEWER

			if ((netLib & NetLib.PUN2) == 0)
				Destroy(gameObject);
#else
			if (netLib > 0)
				Destroy(gameObject);
#endif
		}
	}

#if UNITY_EDITOR

	[CustomEditor(typeof(AutoDestroyWrongNetLib))]
	[CanEditMultipleObjects]
	public class AutoDestroyWrongNetLibEditor : Editor
	{
		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();
			EditorGUILayout.HelpBox("Destroys this object if network library being used does not match the specified NetLib value.",
				MessageType.None);
		}
	}

#endif
}
