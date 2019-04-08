//Copyright 2018, Davin Carten, All rights reserved

using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif
namespace emotitron
{
	/// <summary>
	/// Base class for Editor lists of unique names. Will resolve naming conflicts and intelligently guess
	/// the correct selection when names are changes or number of items change.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	[System.Serializable]
	[ExecuteInEditMode] // So that OnDestroy fires when components are deleted
	public abstract class UniqueNamesList<T> : MonoBehaviour where T : Component
	{
		// Dictionary is used to help remap indexes to renamed items.
		public static Dictionary<string, UniqueNamesList<T>> lookup = new Dictionary<string, UniqueNamesList<T>>();
		public static List<UniqueNamesList<T>> list = new List<UniqueNamesList<T>>();
		public static List<string> names = new List<string>();

		public string itemName = "TEMPHOLDERNAME";

		[HideInInspector]
		public int index;

		private void Reset()
		{
			MakeNameUnique("Unnamed");
			GetAllInScene();
		}

		void Awake()
		{
			GetAllInScene();
		}

		private void OnDestroy()
		{
			if (lookup.ContainsKey(itemName))
				lookup.Remove(itemName);

			GetAllInScene();
		}

		public void MakeNameUnique(string defaultName)
		{
			itemName = defaultName;
			//// if this name is already used, attempt a rename.
			while (lookup.ContainsKey(itemName))
			{
				//Keep adding 0 to the end of the name until it is unique.
				Debug.LogWarning("Two UIZones have the same name of " + itemName + ". Only one will be used until all others with the same name are renamed to unique names. Will attempt to rename, but this may lead to unpredictable results.");
				itemName += "0";
			}
		}

		public static List<UniqueNamesList<T>> GetAllInScene()
		{
			lookup.Clear();
			list.Clear();
			names.Clear();

			Object[] objs = Resources.FindObjectsOfTypeAll(typeof(T));

			for (int i = 0; i < objs.Length; i++)
			{
				UniqueNamesList<T> zone = (UniqueNamesList<T>)objs[i];

				//TODO this is suspect as to whether or not it will be finding prefabs at runtime.
#if UNITY_EDITOR
				// ignore prefabs
				if (EditorUtility.IsPersistent(zone.gameObject))
					continue;
#endif
				zone.index = i;
				if (zone != null)
				{
					if (lookup.ContainsKey(zone.itemName))
						zone.MakeNameUnique(zone.itemName);

					list.Add(zone);
					names.Add(zone.itemName);
					lookup.Add(zone.itemName, zone);
				}
			}

			return list;
		}
	}
}
