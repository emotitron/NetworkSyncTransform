using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class FindObjects
{
	/// Use this method to get all loaded objects of some type, including inactive objects. 
	/// This is an alternative to Resources.FindObjectsOfTypeAll (returns project assets, including prefabs), and GameObject.FindObjectsOfTypeAll (deprecated).
	public static List<T> FindObjectsOfTypeAllInScene<T>(bool checkForIsLoaded = false)
	{
		List<T> results = new List<T>();

		Scene s = SceneManager.GetActiveScene();
	
		if (!checkForIsLoaded || s.isLoaded)
		{
			var allGameObjects = s.GetRootGameObjects();
			for (int j = 0; j < allGameObjects.Length; j++)
			{
				var go = allGameObjects[j];
				results.AddRange(go.GetComponentsInChildren<T>(true));
			}
		}
		return results;
	}
}

