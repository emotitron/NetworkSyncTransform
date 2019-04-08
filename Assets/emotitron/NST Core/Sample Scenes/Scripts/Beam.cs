using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Beam : MonoBehaviour {

	public LineRenderer lr;
	[Range(0, 5)]
	public float lifespan = 1f;

	private float aliveTime;

	private void OnEnable()
	{
		aliveTime = Time.time;
	}

	// Update is called once per frame
	void Update ()
	{
		float elapsed = Time.time - aliveTime;

		// Disable to return to pool
		if (elapsed > lifespan)
			gameObject.SetActive(false);

	}
}
