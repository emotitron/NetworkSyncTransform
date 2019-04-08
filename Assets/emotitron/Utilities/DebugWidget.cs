using UnityEngine;
using System.Diagnostics;
using System.Collections.Generic;

public class DebugWidget : MonoBehaviour
{
	private static Dictionary<GameObject, GameObject> crosses = new Dictionary<GameObject, GameObject>();

	[Conditional("DEBUG"), Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
	public static void CreateDebugCross(GameObject srcGO, bool createThis)
	{
		if (!createThis)
			return;

		GameObject crossGO = new GameObject();
		CreateAxisLine(new Vector3(5f, .1f, .1f), Color.red, crossGO.transform);
		CreateAxisLine(new Vector3(.1f, 5f, .1f), Color.green, crossGO.transform);
		CreateAxisLine(new Vector3(.1f, .1f, 5f), Color.blue, crossGO.transform);

		crossGO.name = "DebugCross - " + srcGO.name;
		crossGO.transform.parent = srcGO.transform.parent;
		crossGO.transform.localPosition = srcGO.transform.localPosition;
		crossGO.transform.localRotation = srcGO.transform.localRotation;

		crosses.Add(srcGO, crossGO);
	}

	[Conditional("DEBUG"), Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
	public static void RemoveDebugCross(GameObject srcGO)
	{
		crosses.Remove(srcGO);
	}

	private static GameObject CreateAxisLine(Vector3 size, Color color, Transform par = null)
	{
		GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
		go.transform.localScale = size;

		go.GetComponent<MeshRenderer>().material.color = color;
		Collider collider = go.GetComponent<Collider>();
		collider.isTrigger = true; // prevent this from colliding with objects prior to destroy.
		collider.enabled = false; // prevent this from colliding with objects prior to destroy.
		Destroy(collider);

		go.transform.parent = par;
		go.transform.localPosition = new Vector3(0, 0, 0);
		go.transform.localRotation = new Quaternion(0, 0, 0, 1);
		return go;
	}

	[Conditional("DEBUG"), Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
	public static void Move(GameObject srcGO, Vector3 pos, Quaternion rot, int istype, int iftype, bool moveThis = true)
	{
		if (!moveThis)
			return;

		if (istype != iftype)
			return;

		GameObject debugGO;
		if (crosses.TryGetValue(srcGO, out debugGO))
		{
			debugGO.transform.position = pos;
			debugGO.transform.rotation = rot;
		}

	}

}
