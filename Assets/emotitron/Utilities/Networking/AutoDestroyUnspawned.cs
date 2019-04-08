//Copyright 2018, Davin Carten, All rights reserved

using UnityEngine;
using UnityEngine.SceneManagement;

#if PUN_2_OR_NEWER
#endif

#if MIRROR
using Mirror;
#else
using UnityEngine.Networking;
#endif

#if UNITY_EDITOR
using UnityEditor;
#endif

#pragma warning disable CS0618 // UNET obsolete

namespace emotitron.Utilities.Networking
{
	/// <summary>
	/// Destroys they exist in the scene during startup.
	/// This allows prefab copies to exist in the scene while editing, without having to delete them every time you build out.
	/// </summary>
	public class AutoDestroyUnspawned : MonoBehaviour
	{

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
		public static void DestroyUnspawned()
		{
			AutoDestroyUnspawned[] nsts = Resources.FindObjectsOfTypeAll<AutoDestroyUnspawned>();

			for (int i = 0; i < nsts.Length; i++)
			{
				var obj = nsts[i];
				if (obj.gameObject.scene == SceneManager.GetActiveScene())
				{
					Object.Destroy(obj.gameObject);
				}
			}
		}
	}


#if UNITY_EDITOR

	[CustomEditor(typeof(AutoDestroyUnspawned))]
	[CanEditMultipleObjects]
	public class AutoDestroyUnspawnedEditor : Editor
	{
		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();
			EditorGUILayout.HelpBox("Destroys this gameobject if it exists in the scene at scene load. " +
				"Allows network prefabs to be left in scene at build/play time, as a development convenience.",
				MessageType.None);
		}
	}

#endif
}

#pragma warning restore CS0618 // UNET obsolete
