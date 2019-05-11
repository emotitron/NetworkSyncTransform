
#if PUN_2_OR_NEWER || MIRROR || !UNITY_2019_1_OR_NEWER

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace emotitron.NST.Sample
{
	public class ChangeScene : MonoBehaviour
	{
		public KeyCode scene2Key = KeyCode.Alpha8;
		public KeyCode scene3Key = KeyCode.Alpha9;
		public KeyCode scene4Key = KeyCode.Alpha0;

		// Update is called once per frame
		void Update()
		{
			if (Input.GetKeyDown(scene2Key))
				MasterNetAdapter.ServerChangeScene("Scene2");
			if (Input.GetKeyDown(scene3Key))
				MasterNetAdapter.ServerChangeScene("Scene3");
			if (Input.GetKeyDown(scene4Key))
				MasterNetAdapter.ServerChangeScene("Scene4");
		}
	}
}

#endif
