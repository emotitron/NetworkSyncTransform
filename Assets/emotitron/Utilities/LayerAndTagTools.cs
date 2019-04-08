using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LayerAndTagTools : MonoBehaviour {

	public static void SetLayerRecursively(GameObject obj, int newLayer)
	{
		if (null == obj)
		{
			return;
		}

		obj.layer = newLayer;
		foreach (Transform child in obj.transform)
		{
			if (null == child)
			{
				continue;
			}
			SetLayerRecursively(child.gameObject, newLayer);
		}
	}

	public static List<GameObject> FindChildrenWithTags(GameObject par, string tag){
		List<GameObject> taggedGOs = new List<GameObject> ();
		Transform t = par.transform;
		foreach(Transform tr in t)
		{
			if(tr.CompareTag(tag))
			{
				taggedGOs.Add(tr.gameObject);
			}
		}
		return taggedGOs;
	}

	public static void FindGOwithTagAndRecursivelySetAllChildrenToLayer(GameObject par, string tag, int newLayer)
	{
		foreach (Transform tr in par.transform) {
			if(tr.CompareTag(tag)){
				SetLayerRecursively (tr.gameObject, newLayer);
			}
		}
	}
}
