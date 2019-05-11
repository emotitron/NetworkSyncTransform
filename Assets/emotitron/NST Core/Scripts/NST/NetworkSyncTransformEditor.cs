//Copyright 2018, Davin Carten, All rights reserved

#if PUN_2_OR_NEWER || MIRROR || !UNITY_2019_1_OR_NEWER


#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using emotitron.Compression;

namespace emotitron.NST
{
	[CustomEditor(typeof(NetworkSyncTransform))]
	[CanEditMultipleObjects]
	public class NSTEditor : NSTHeaderEditorBase
	{
		NetworkSyncTransform nst;

		public override void OnEnable()
		{
			nst = (NetworkSyncTransform)target;

			headerName = HeaderNSTName;
			headerColor = HeaderNSTColor;
			base.OnEnable();
		}
		

		public override void OnInspectorGUI()
		{

			serializedObject.Update();
			base.OnInspectorGUI();
			serializedObject.ApplyModifiedProperties();

			nst.EnsureAllNSTDependencies(serializedObject, true);

			/// TODO: this may no longer be needed or the case
			// Make sure the object is active, to prevent users from spawning inactive gameobjects (which will break things)
			if (!Application.isPlaying && !nst.gameObject.activeSelf && MasterNetAdapter.NetLib == NetworkLibrary.UNET)// && AssetDatabase.Contains(target))
			{
				Debug.LogWarning("Prefabs with NetworkSyncTransform MUST be enabled. If you are trying to disable this so it isn't in your scene when you test it, no worries - NST destroys all scene objects with the NST component at startup.");
				nst.gameObject.SetActive(true);
			}

			EditorGUILayout.Space();

			Rect r = EditorGUILayout.GetControlRect();
			GUI.Label(r, "Summary", "BoldLabel");
			EditorGUILayout.HelpBox(
				//"Summary:\n" +
				"Approx max seconds of buffer " + ((1 << 7) - 2) * Time.fixedDeltaTime * nst.sendEveryXTick * HeaderSettings.Single.TickEveryXFixed * 0.5f + " \n" +
				"sendEveryXTick = " + nst.sendEveryXTick + "\n" +
				"NSTSettings.bitsForPacketCount = " + 7 + "\n" +
				"Time.fixedDeltaTime = " + Time.fixedDeltaTime
				,
				MessageType.None);

			HeaderSettings.Single.DrawGui(target, true, false);
			WorldBoundsSO.Single.DrawGui(target, true, false);
			//WorldCompressionSettings.Single.DrawGui(target, true, false);

		}

	}
}

#endif

#endif