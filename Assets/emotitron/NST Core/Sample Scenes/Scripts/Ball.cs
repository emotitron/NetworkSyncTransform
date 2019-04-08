using UnityEngine;

/// <summary>
/// Adds random force to a object to keep it bouncing randomly. NST objects that aren't owner will be isKinematic so force will be ignored.
/// </summary>
public class Ball : MonoBehaviour {

	Rigidbody rb;

	private void Awake()
	{
		rb = GetComponent<Rigidbody>();
	}
	
	private void OnCollisionEnter(Collision collision)
	{
		rb.velocity = rb.velocity.normalized * 15f + Vector3.up * Random.value * 5;
		rb.AddTorque(new Vector3(Random.value, Random.value, Random.value) * 40);
	}
}
