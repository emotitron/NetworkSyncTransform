
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Super quick and dirty console to screen for use with mobile or running outside of the editor.
/// </summary>
public class DebugText : MonoBehaviour {

	private Text textui;
	static DebugText single;

	void Awake ()
	{
		textui = GetComponent<Text>();
		single = this;
	}

	public static void Log(string str, bool clear = false)
	{
		if (!single)
			return;

		if (clear)
			single.textui.text = "";

		single.textui.text += str;
	}
}
