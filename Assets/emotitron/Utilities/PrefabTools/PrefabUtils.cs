
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif


namespace emotitron.Utilities
{
	public static class PrefabUtils
	{
		public static T AddComponentToPrefab<T>(this GameObject prefab) where T : Component
		{
			T component = null;

#if UNITY_EDITOR

			if (prefab == null)
				prefab = Selection.activeGameObject;

			if (prefab == null)
				return null;

#if UNITY_2018_3_OR_NEWER

			if (PrefabUtility.IsPartOfPrefabAsset(prefab))
			{
				var path = AssetDatabase.GetAssetPath(prefab);
				var prefabRoot = PrefabUtility.LoadPrefabContents(path);
				try
				{
					component = prefabRoot.AddComponent<T>();
					PrefabUtility.SaveAsPrefabAsset(prefabRoot, path);
				}
				finally
				{
					PrefabUtility.UnloadPrefabContents(prefabRoot);
				}
			}
			
#else
			component = prefab.AddComponent<T>();
#endif

#else
			if (prefab != null)
				component = prefab.AddComponent<T>();
#endif


			return component;
		}
	}
}

