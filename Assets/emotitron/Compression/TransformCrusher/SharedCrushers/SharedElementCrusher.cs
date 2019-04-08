//Copyright 2018, Davin Carten, All rights reserved

using UnityEngine;
using System.Reflection;
using System.Linq.Expressions;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace emotitron.Compression
{
	
	/// <summary>
	/// Wrapper class for ElementCrusher that allows sharing of a common crusher between instances, types and field names. To work correctly, be sure that this
	/// field is serialized, byt setting it to Public or using the [SerializeField] attribute.
	/// </summary>
	[System.Serializable]
	public class SharedElementCrusher : SharedCrusherBase<ElementCrusher>
	{
		///// <summary>
		///// Default constructor determines the type of the object that called the constructer using a StackTrace, and uses a common crusher for all instances of that Type and crusher Field.
		///// </summary>
		//public SharedElementCrusher() : base() { }

		/// <summary>
		/// Constructor that uses a supplied hashcode for identifying a shared crusher. If a crusher with this hascode has been constructed,
		/// then the existing crusher will be used. If not a new one will be instantiated and used.
		/// </summary>
		/// <param name="hashcode"></param>
		public SharedElementCrusher(int hashcode) : base(hashcode) { }

		/// <summary>
		/// Constructor that uses a supplied name as a hashcode for identifying a shared crusher. If a crusher with this hascode has been constructed,
		/// then the existing crusher will be used. If not a new one will be instantiated and used.
		/// </summary>
		/// <param name="hashcode"></param>
		public SharedElementCrusher(string name) : base(name) { }

		/// <summary>
		/// Constructor that uses StackTrace and SerializedProperty to determined which instances are common and should share a crusher instance.
		/// \nPrefab : All instances of a prefab will share a crusher instance.
		/// \nFieldName : All crushers with this same field name will share a crusher instance, even across components and scenes.
		/// \nComponent and FieldName : All instances of this component will share a common crusher for this field.
		/// </summary>
		public SharedElementCrusher(ShareByCommon shareBy = ShareByCommon.Prefab) : base(shareBy) { }


	}


#if UNITY_EDITOR

	[CustomPropertyDrawer(typeof(SharedElementCrusher))]
	[CanEditMultipleObjects]

	public class SharedElementCrusherDrawer : SharedCrusherBaseDrawer<SharedElementCrusher, ElementCrusher>
	{
		//public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		//{
		//	base.OnGUI(position, property, label);
		//}

		//public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		//{
		//	return base.GetPropertyHeight(property, label);
		//}
	}

#endif

}
