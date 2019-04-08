using UnityEngine;
using emotitron.NST;

/// <summary>
/// Rotates and object if it has as NetworkSyncTransform on it, and if it has authority. Used for demonstration only.
/// </summary>
public class Rotator : NSTComponent, INstPostUpdate
{
	public float speed = 20;
	public float timePassed;

	// Runs on the NST Update, so that this doesn't disable.
	public void OnNstPostUpdate()
	{
		// Only objects with authority should be moving things.
		if (this != null && na != null && na.IsMine)
		{
			timePassed += Time.deltaTime;
			transform.localEulerAngles = new Vector3(0, 0, timePassed * speed % 360);
		}
	}
}
