//Copyright 2018, Davin Carten, All rights reserved

using UnityEngine;
using System.Collections.Generic;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif

#if UNITY_EDITOR
namespace emotitron.Utilities.GUIUtilities
{
	public static class EditorUtils
	{


		public static void CreateErrorIconF(float xmin, float ymin, string _tooltip)
		{
			GUIContent errorIcon = EditorGUIUtility.IconContent("CollabError");
			errorIcon.tooltip = _tooltip;
			EditorGUI.LabelField(new Rect(xmin, ymin, 16, 16), errorIcon);

		}

		/// <summary>
		/// If this gameobject is a clone of a prefab, will return that prefab source. Otherwise just returns the go that was supplied.
		/// </summary>
		public static GameObject GetPrefabSourceOfGameObject(this GameObject go)
		{
#if UNITY_2018_2_OR_NEWER
			return (PrefabUtility.GetCorrespondingObjectFromSource(go) as GameObject);
#else
			return (PrefabUtility.GetPrefabParent(go) as GameObject);
#endif
		}

		/// <summary>
		/// A not so efficient find of all instances of a prefab in a scene.
		/// </summary>
		public static List<GameObject> FindAllPrefabInstances(GameObject prefabParent)
		{
			//Debug.Log("Finding all instances of prefab '" + prefabParent.name +"' in scene");
			List<GameObject> result = new List<GameObject>();
			GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>();
			foreach (GameObject GO in allObjects)
			{
#if UNITY_2018_2_OR_NEWER
				var parPrefab = PrefabUtility.GetCorrespondingObjectFromSource(GO) as GameObject;
#else
				var parPrefab = PrefabUtility.GetPrefabParent(GO);
#endif


				if (parPrefab == prefabParent)
				{
					//Debug.Log("Found prefab instance: " + GO.name);

					UnityEngine.Object GO_prefab = parPrefab;
					if (prefabParent == GO_prefab)
						result.Add(GO);
				}
			}
			return result;
		}

		/// <summary>
		/// Add missing components, Ideally adds to the prefab master, so it appears on any scene versions and doesn't require Apply.
		/// </summary>
		public static T EnsureRootComponentExists<T>(this GameObject go, bool isExpanded = true) where T : Component
		{
#if UNITY_2018_2_OR_NEWER
			GameObject parPrefab = PrefabUtility.GetCorrespondingObjectFromSource(go) as GameObject;
#else
			GameObject parPrefab = PrefabUtility.GetPrefabParent(go) as GameObject;
#endif

			T component;

			if (parPrefab)
			{
				component = parPrefab.GetComponent<T>();

				// Remove the NI from a scene object before we add it to the prefab
				if (component == null)
				{
					List<GameObject> clones = FindAllPrefabInstances(parPrefab);

					// Delete all instances of this root component in all instances of the prefab, so when we add to the prefab source they all get it - without repeats
					foreach (GameObject clone in clones)
					{
						T[] comp = clone.GetComponents<T>();
						foreach (T t in comp)
						{
							Debug.Log("Destroy " + t);
							GameObject.DestroyImmediate(t);

						}
					}
					component = parPrefab.AddComponent<T>();
					UnityEditorInternal.InternalEditorUtility.SetIsInspectorExpanded(component, isExpanded);
				}
			}

			// this is has no prefab source
			else
			{
				component = go.GetComponent<T>();
				if (!component)
				{
					component = go.AddComponent<T>();
					UnityEditorInternal.InternalEditorUtility.SetIsInspectorExpanded(component, isExpanded);
				}
			}

			return component;
		}

		/// <summary>
		/// Add missing components, Ideally adds to the prefab master, so it appears on any scene versions and doesn't require Apply.
		/// </summary>
		public static Component EnsureRootComponentExists(this GameObject go, Type type, bool isExpanded = true)
		{
			if (type == null)
				return null;

#if UNITY_2018_2_OR_NEWER
			GameObject parPrefab = PrefabUtility.GetCorrespondingObjectFromSource(go) as GameObject;
#else
			GameObject parPrefab = PrefabUtility.GetPrefabParent(go) as GameObject;
#endif
			Component component;

			if (parPrefab)
			{
				component = parPrefab.GetComponent(type);
				// Remove the NI from a scene object before we add it to the prefab
				if (component == null)
				{
					List<GameObject> clones = FindAllPrefabInstances(parPrefab);

					// Delete all instances of this root component in all instances of the prefab, so when we add to the prefab source they all get it - without repeats
					foreach (GameObject clone in clones)
					{
						Component[] comp = clone.GetComponents(type);
						foreach (Component t in comp)
							GameObject.DestroyImmediate(t);
					}
					component = parPrefab.AddComponent(type);
					UnityEditorInternal.InternalEditorUtility.SetIsInspectorExpanded(component, isExpanded);
				}
			}

			// this is has no prefab source
			else
			{

				component = go.GetComponent(type);

				if (!component)
				{
					component = go.AddComponent(type);
					UnityEditorInternal.InternalEditorUtility.SetIsInspectorExpanded(component, isExpanded);
				}
			}

			return component;
		}

		static List<Component> components = new List<Component>();

		/// <summary>
		/// DestroyImmediate a component on the parent prefab of this object if possible, otherwise on this object.
		/// </summary>
		public static void DeleteComponentAtSource(this GameObject go, Type type)
		{
			if (type == null)
				return;

#if UNITY_2018_2_OR_NEWER
			GameObject parPrefab = PrefabUtility.GetCorrespondingObjectFromSource(go) as GameObject;
#else
			GameObject parPrefab = PrefabUtility.GetPrefabParent(go) as GameObject;
#endif

			if (parPrefab)
			{
				List<GameObject> clones = FindAllPrefabInstances(parPrefab);

				// Delete all instances of this root component in all instances of the prefab, so when we add to the prefab source they all get it - without repeats
				foreach (GameObject clone in clones)
				{
					clone.GetComponents(type, components);
					foreach (Component t in components)
						GameObject.DestroyImmediate(t);
				}
			}

			// this is has no prefab source
			else
			{
				go.GetComponents(type, components);
				foreach (Component t in components)
					GameObject.DestroyImmediate(t);
			}

		}

	}
}
#endif


