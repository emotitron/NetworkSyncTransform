using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace emotitron.Utilities
{
	[ExecuteInEditMode]
	public class TogglePhysics2dDefine : MonoBehaviour
	{
		public bool disablePhysics2D = false;
		public bool setOnAwake = false;

#if UNITY_EDITOR
		public const string DISABLE_PHYSICS_2D = "DISABLE_PHYSICS_2D";

		private void Set()
		{
			if (isActiveAndEnabled)
			{
#if !DISABLE_PHYSICS_2D
				if (disablePhysics2D)
					DISABLE_PHYSICS_2D.AddDefineSymbol();
#else
				if (!disablePhysics2D)
					DISABLE_PHYSICS_2D.RemoveDefineSymbol();
#endif
			}
		}

		private void OnValidate()
		{
			Set();
		}


		private void Awake()
		{
			if (setOnAwake)
			{
				Set();
			}
			else
#if !DISABLE_PHYSICS_2D
				disablePhysics2D = false;
#else
				disablePhysics2D = true;
#endif
		}

#endif
	}

#if UNITY_EDITOR

	[CustomEditor(typeof(TogglePhysics2dDefine))]
	public class TogglePhysics2dDefineEditor : Editor
	{
		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();
			EditorGUILayout.HelpBox("Collider2D and Rigidbody2D support are enabled by default, and can be disabled by adding '"
				+ TogglePhysics2dDefine.DISABLE_PHYSICS_2D + ";' to Define Symbols in PlayerSettings.\n\n This component will add/remove the symbol for you.", MessageType.None);
		}
	}

#endif

}

