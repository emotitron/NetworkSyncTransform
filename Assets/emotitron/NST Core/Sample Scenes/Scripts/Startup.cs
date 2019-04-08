using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using emotitron.NST;

#if PUN_2_OR_NEWER
// using Photon.Pun;
#elif MIRROR
using Mirror;
#else
using UnityEngine.Networking;
#endif

#pragma warning disable CS0618 // UNET obsolete

/// <summary>
/// Just some setup stuff used for the sample scene, nothing that is required.
/// </summary>
public class Startup : MonoBehaviour
{
	public int targetFramerate = 60;
	public Text framerateValueText;
	public Slider framerateSlider;

	private void Awake()
	{
		// Only show the framerate silder if vsync if off... it does nothing if it is on.
		if (QualitySettings.vSyncCount != 0)
		{
			if (framerateSlider)
				framerateSlider.gameObject.SetActive(false);
			SetFrameRate(targetFramerate);
		}

	}
	void Start ()
	{

		// Make sure screen is big enough on mobile to mess with the network buttons.
		if (Screen.width > 1440)
			Screen.SetResolution(Screen.width / 3, Screen.height / 3, false);
	}

	//// Teleport shortcuts
	//private void Update()
	//{
	//	if (Input.GetKeyDown("0"))
	//		NSTTools.allNsts[0].Teleport(MasterNetAdapter.UNET_GetPlayerSpawnPoint());

	//	if (Input.GetKeyDown("9"))
	//		NSTTools.allNsts[1].Teleport(MasterNetAdapter.UNET_GetPlayerSpawnPoint());

	//	if (Input.GetKeyDown("8"))
	//		NSTTools.allNsts[2].Teleport(MasterNetAdapter.UNET_GetPlayerSpawnPoint());

	//}

	public void SetFrameRate (Single rate)
	{
		Application.targetFrameRate = (int)rate;
		if (framerateValueText)
			framerateValueText.text = ((int)rate).ToString();
		if (framerateSlider)
			framerateSlider.value = rate;
	}

}
#pragma warning restore CS0618 // UNET obsolete

