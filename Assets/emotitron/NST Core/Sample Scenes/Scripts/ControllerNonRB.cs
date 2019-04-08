//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using emotitron.NST;

//public class ControllerNonRB : MonoBehaviour {

//	NetworkSyncTransform nst;
//	NSTNetAdapter na;

//	public float velocity = 2;
//	public float turnRate = 40;
		
//	void Awake ()
//	{
//		nst = GetComponent<NetworkSyncTransform>();	
//		na = GetComponent<NSTNetAdapter>();

//	}
	
//	void Update ()
//	{
//		if (!na.HasAuthority)
//			return;


//		if (Input.GetKey("q"))
//			transform.localEulerAngles = new Vector3(transform.localEulerAngles.x, transform.localEulerAngles.y + -Time.deltaTime * turnRate, transform.localEulerAngles.z);

//		if (Input.GetKey("e"))
//			transform.localEulerAngles = new Vector3(transform.localEulerAngles.x, transform.localEulerAngles.y + Time.deltaTime * turnRate, transform.localEulerAngles.z);


//		if (Input.GetKey("w"))
//			transform.position += transform.forward * Time.deltaTime * velocity;

//		if (Input.GetKey("s"))
//			transform.position += -transform.forward * Time.deltaTime * velocity;

//		if (Input.GetKey("d"))
//			transform.position += transform.right * Time.deltaTime * velocity;

//		if (Input.GetKey("a"))
//			transform.position += -transform.right * Time.deltaTime * velocity;
//	}
//}
